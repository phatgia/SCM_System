using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SCM_System.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models;
using SCM_System.Models.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "Quản trị viên,Nhân viên vận chuyển,Quản lý kho")]
    public class DeliveryController : Controller
    {
        private readonly SCMDbContext _context;
        private readonly IHubContext<HandoverHub> _hubContext;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public DeliveryController(SCMDbContext context, IHubContext<HandoverHub> hubContext,IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _hubContext = hubContext;
            _env = env;
            _config = config;
        }

        // ─── HMAC helpers: bảo vệ QR ─────────────────────────────────────────
        private string GeneratePickupToken(int soId)
        {
            var secret = _config["QR:Secret"] ?? "scm-qr-fallback-2026";
            var payload = $"SO:{soId}"; // Token gắn với mã đơn hàng
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
        private bool ValidatePickupToken(int soId, string token)
            => string.Equals(GeneratePickupToken(soId), token,
                             StringComparison.OrdinalIgnoreCase);

        // ─── GET /Delivery/GenerateQR?soId=X ────────────────────────────
        // Thủ kho gọi API này → nhận signed URL → vẽ QR cho shipper quét
        [HttpGet]
        [Authorize(Roles = "Quản trị viên,Quản lý kho,Nhân viên vận chuyển")]
        public async Task<IActionResult> GenerateQR(int soId)
        {
            var order = await _context.SaleOrders
                .Include(so => so.Deliveries)
                .Include(so => so.Customer)
                .FirstOrDefaultAsync(so => so.SOID == soId);

            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn hàng." });

            // Cho phép tạo QR nếu đơn sẵn sàng bàn giao
            bool isReady = order.Status == "Đã soạn xong" 
                        || order.Status == "Đã soạn"
                        || order.Status == "Chờ lấy hàng"
                        || (order.Status == "Đang giao hàng" && order.Deliveries.Any(d => d.Status == "Chờ lấy hàng"));

            if (!isReady)
                return BadRequest(new { message = $"Đơn đang ở trạng thái '{order.Status}', chưa sẵn sàng để bàn giao. Cần soạn hàng xong trước." });

            var token = GeneratePickupToken(soId);
            // Sử dụng X-Forwarded-Proto để xác định đúng scheme khi deploy trên Railway (HTTPS proxy)
            var scheme = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? Request.Scheme;
            var url   = $"{scheme}://{Request.Host}/Delivery/ScanPickup"
                      + $"?soId={soId}&token={Uri.EscapeDataString(token)}";

            string shipperName = "Chưa có (Ai quét trước nhận đơn)";
            var delivery = order.Deliveries.FirstOrDefault(d => d.Status == "Chờ lấy hàng");
            if (delivery != null)
            {
                var assignedUser = await _context.Users.FindAsync(delivery.UserID);
                shipperName = assignedUser?.FullName ?? "N/A";
            }

            return Json(new
            {
                qrUrl       = url,
                shipperName = shipperName,
                orderCode   = $"SO-{order.OrderDate.Year}-{order.SOID:D3}"
            });
        }
        // =====================================================================
        // GET: /Delivery/Delivery  — Trang chính Vận chuyển
        // =====================================================================
        public async Task<IActionResult> Delivery(string? searchCode = null)
        {
            var vm = new DeliveryViewModel();

            var rawActiveDeliveries = await _context.Deliveries
                .Include(d => d.SaleOrder).ThenInclude(so => so.Customer)
                .Include(d => d.User)
                .ToListAsync(); 

            var activeDeliveries = rawActiveDeliveries.Select(d => new DeliveryListItem
            {
                DeliveryID    = d.DeliveryID,
                OrderCode     = $"SO-{d.SaleOrder.OrderDate.Year}-{d.SOID:D3}",
                CustomerName  = d.SaleOrder.Customer?.Name ?? "Khách lẻ",
                CustomerPhone = d.SaleOrder.Customer?.Phone ?? "",
                ShipperName   = d.User?.FullName ?? "Không rõ", 
                Address       = d.SaleOrder.Customer?.ShippingAddress ?? "",
                TotalAmount   = d.SaleOrder.TotalAmount,
                Status        = d.Status,
                DeliveryTime  = d.DeliveryTime
            }).ToList();

            var rawPendingOrders = await _context.SaleOrders
                .Include(so => so.Customer)
                .Where(so => so.Status == "Chờ lấy hàng" || so.Status == "Đã soạn xong" && !so.Deliveries.Any())
                .ToListAsync();

            var pendingDeliveries = rawPendingOrders.Select(so => new DeliveryListItem
            {
                DeliveryID    = 0, 
                OrderCode     = $"SO-{so.OrderDate.Year}-{so.SOID:D3}",
                CustomerName  = so.Customer?.Name ?? "Khách lẻ",
                CustomerPhone = so.Customer?.Phone ?? "",
                ShipperName   = "---", 
                Address       = so.Customer?.ShippingAddress ?? "",
                TotalAmount   = so.TotalAmount,
                Status        = "Chờ lấy hàng", 
                DeliveryTime  = so.OrderDate 
            }).ToList();

            vm.AllDeliveries = pendingDeliveries.Concat(activeDeliveries)
                .OrderByDescending(x => x.DeliveryTime)
                .ToList();

            var now = DateTime.Now;
            vm.PendingPickupCount      = vm.AllDeliveries.Count(d => d.Status == "Chờ lấy hàng");
            vm.InDeliveryCount         = vm.AllDeliveries.Count(d => d.Status == "Đang giao hàng" || d.Status == "Đang giao");
            vm.CompletedThisMonthCount = vm.AllDeliveries.Count(d =>
                d.Status == "Thành công" &&
                d.DeliveryTime.HasValue &&
                d.DeliveryTime.Value.Month == now.Month &&
                d.DeliveryTime.Value.Year  == now.Year);

            var shippers = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "Nhân viên vận chuyển")
                .ToListAsync();

            var activeDeliveryCount = await _context.Deliveries
                .Where(d => d.Status == "Đang giao" || d.Status == "Chờ lấy hàng")
                .GroupBy(d => d.UserID)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            vm.Shippers = shippers.Select(u => new ShipperItem
            {
                UserID           = u.UserID,
                FullName         = u.FullName,
                ActiveDeliveries = activeDeliveryCount.FirstOrDefault(x => x.Key == u.UserID)?.Count ?? 0
            }).ToList();

            // --- Biểu đồ 1: Trạng thái đơn hàng ---
            var statusGroups = vm.AllDeliveries
                .GroupBy(d => d.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            vm.StatusLabels = statusGroups.Select(g => g.Status).ToList();
            vm.StatusCounts = statusGroups.Select(g => g.Count).ToList();

            // --- Biểu đồ 2: Hiệu suất giao hàng 7 ngày qua ---
            var last7Days = Enumerable.Range(0, 7).Select(i => now.Date.AddDays(-i)).Reverse().ToList();
            var completedDeliveries = vm.AllDeliveries
                .Where(d => d.Status == "Thành công" && d.DeliveryTime.HasValue)
                .GroupBy(d => d.DeliveryTime!.Value.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var date in last7Days)
            {
                vm.DateLabels.Add(date.ToString("dd/MM"));
                vm.DeliveryCounts.Add(completedDeliveries.ContainsKey(date) ? completedDeliveries[date] : 0);
            }

            if (!string.IsNullOrEmpty(searchCode))
            {
                string idString = searchCode.Split('-').LastOrDefault() ?? "";
                int.TryParse(idString, out int searchSoId);

                ViewBag.SearchedDelivery = await _context.Deliveries
                    .Include(d => d.DeliveryTrackings)
                    .FirstOrDefaultAsync(d => d.SOID == searchSoId);
            }
            return View(vm);
        }

        // =====================================================================
        // POST: Phân công shipper cho đơn hàng
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Quản trị viên,Nhân viên vận chuyển")]
        public async Task<IActionResult> AssignShipper(int soid, int userId, string? note)
        {
            var order = await _context.SaleOrders.FindAsync(soid);
            var user  = await _context.Users.FindAsync(userId);

            if (order == null || user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng hoặc nhân viên.";
                return RedirectToAction("Delivery");
            }

            // Kiểm tra đã phân công chưa
            var existing = await _context.Deliveries.AnyAsync(d => d.SOID == soid);
            if (existing)
            {
                TempData["ErrorMessage"] = "Đơn hàng này đã được phân công rồi.";
                return RedirectToAction("Delivery");
            }

            var delivery = new Delivery
            {
                SOID         = soid,
                UserID       = userId,
                Status       = "Chờ lấy hàng",
                DeliveryTime = DateTime.Now,
                HandShakeProof = note
            };

            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã phân công đơn SO-{order.OrderDate.Year}-{soid:D3} cho {user.FullName}!";
            return RedirectToAction("Delivery");
        }

        // =====================================================================
        // POST: Xác nhận lấy hàng từ kho (Handshake 1)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPickup(int deliveryId)
        {
            var delivery = await _context.Deliveries.FindAsync(deliveryId);
            if (delivery == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chuyến giao hàng.";
                return RedirectToAction("Delivery");
            }

            delivery.Status = "Đang giao";
            _context.Update(delivery);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xác nhận lấy hàng. Trạng thái chuyển sang Đang giao.";
            return RedirectToAction("Delivery");
        }

        // =====================================================================
        // POST: Cập nhật trạng thái giao hàng
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int deliveryId, string status, string? note)
        {
            var delivery = await _context.Deliveries.FindAsync(deliveryId);
            if (delivery == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chuyến giao hàng.";
                return RedirectToAction("Delivery");
            }

            delivery.Status = status;
            if (!string.IsNullOrEmpty(note))
                delivery.HandShakeProof = note;

            _context.Update(delivery);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái: {status}.";
            return RedirectToAction("Delivery");
        }

        // =====================================================================
        // GET (AJAX): Tìm đơn hàng đang giao để bàn giao cho khách
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> GetDeliveryByOrder(string orderCode)
        {
            // orderCode format: SO-2026-039 → SOID = 39
            if (!int.TryParse(orderCode.Split('-').LastOrDefault(), out int soid))
                return NotFound(new { message = "Mã đơn không hợp lệ." });

            var delivery = await _context.Deliveries
                .Include(d => d.SaleOrder).ThenInclude(so => so.Customer)
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.SOID == soid && d.Status == "Đang giao");

            if (delivery == null)
                return NotFound(new { message = "Không tìm thấy đơn đang giao với mã này." });

            return Json(new
            {
                deliveryId    = delivery.DeliveryID,
                orderCode     = orderCode,
                customerName  = delivery.SaleOrder.Customer.Name,
                customerPhone = delivery.SaleOrder.Customer.Phone,
                address       = delivery.SaleOrder.Customer.ShippingAddress,
                totalAmount   = delivery.SaleOrder.TotalAmount.ToString("N0") + " ₫",
                shipperName   = delivery.User.FullName
            });
        }

        // =====================================================================
        // POST: Bàn giao cho khách hàng (Handshake 2)
        // =====================================================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteDelivery(int deliveryId, string result, IFormFile proofImage)
        {
            var delivery = await _context.Deliveries
                .Include(d => d.SaleOrder)
                .FirstOrDefaultAsync(d => d.DeliveryID == deliveryId);

            if (delivery == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng cần bàn giao!";
                return RedirectToAction("Delivery", "Delivery", null, "menu2");
            }

            string imagePath = "";
            if (proofImage != null && proofImage.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "pod");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = $"POD_SO-{delivery.SOID}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(proofImage.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await proofImage.CopyToAsync(fileStream);
                }

                imagePath = $"/uploads/pod/{uniqueFileName}";
            }

            string formattedOrderCode = $"SO-{DateTime.Now.Year}-{delivery.SOID:D3}";

            if (result == "Giao thành công")
            {
                delivery.Status = "Thành công";
                if(delivery.SaleOrder != null) delivery.SaleOrder.Status = "Thành công";
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Tuyệt vời! Đã giao thành công đơn {formattedOrderCode}.";
                
                return RedirectToAction("Delivery", "Delivery", null, "menu2");
            }
            else 
            {
                delivery.Status = "Giao thất bại"; 
                if(delivery.SaleOrder != null) delivery.SaleOrder.Status = "Giao thất bại";
                
                await _context.SaveChangesAsync();

                TempData["ErrorMessage"] = $"Khách từ chối nhận đơn {formattedOrderCode}. BẮT BUỘC ghi nhận lý do hoàn trả tại đây!";
                
                return RedirectToAction("Delivery", "Delivery", new { searchCode = formattedOrderCode }, "menu4");
            }
        }

        // =====================================================================
        // POST: Ghi nhận trả hàng (giao không thành công → về kho)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordReturn(string orderCode, string reason, string settlement)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                TempData["ErrorMessage"] = "Không xác định được người dùng.";
                return RedirectToAction("Delivery", "Delivery", null, "menu4");
            }

            string idString = orderCode.Split('-').LastOrDefault() ?? "0";
            int.TryParse(idString, out int soid);

            var order = await _context.SaleOrders.FindAsync(soid);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Delivery", "Delivery", null, "menu4");
            }

            // Tạo ReturnOrder
            var returnOrder = new ReturnOrder
            {
                SOID       = soid,
                UserID     = userId,
                Reason     = reason,
                Settlement = settlement,
                Status     = "Đang xử lý"
            };
            _context.ReturnOrders.Add(returnOrder);

            // Cập nhật Delivery status
            var delivery = await _context.Deliveries
                .FirstOrDefaultAsync(d => d.SOID == soid);
            if (delivery != null)
            {
                delivery.Status = "Hoàn hàng";
                _context.Update(delivery);
            }

            // Cập nhật đơn bán về trạng thái hoàn hàng
            order.Status = "Hoàn hàng";
            _context.Update(order);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã ghi nhận hoàn hàng cho đơn SO-{order.OrderDate.Year}-{soid:D3}.";
            return RedirectToAction("Delivery", "Delivery", null, "menu4");
        }
    
        // ─── GET /Delivery/ScanPickup ─────────────────────────────────────────
        // Shipper quét QR → nhận hàng từ kho
        // Logic mới: Hỗ trợ cả đơn đã phân công shipper và đơn "mở" (ai quét trước nhận đơn)
        [HttpGet]
        [Authorize(Roles = "Quản trị viên,Nhân viên vận chuyển")]
        public async Task<IActionResult> ScanPickup(int soId, string token)
        {
            // 1. Xác thực token HMAC
            if (string.IsNullOrEmpty(token) || !ValidatePickupToken(soId, token))
            {
                TempData["ErrorMessage"] = "Mã QR không hợp lệ!";
                return RedirectToAction("Delivery");
            }

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int currentUserId))
                return Unauthorized();

            // 2. Sử dụng Transaction để đảm bảo tính nguyên tử (Atomicity)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.SaleOrders
                    .Include(s => s.Deliveries)
                    .FirstOrDefaultAsync(s => s.SOID == soId);

                if (order == null) throw new Exception("Không tìm thấy đơn hàng.");

                var delivery = order.Deliveries.FirstOrDefault(d => d.Status == "Chờ lấy hàng");

                if (delivery != null)
                {
                    // TRƯỜNG HỢP 1: Đã phân công Shipper từ trước
                    if (delivery.UserID != currentUserId && !User.IsInRole("Quản trị viên"))
                    {
                        TempData["ErrorMessage"] = "Đơn này đã được chỉ định cho một Shipper khác!";
                        return RedirectToAction("Delivery");
                    }

                    // Cập nhật trạng thái
                    delivery.Status = "Đang giao hàng";
                    delivery.DeliveryTime = DateTime.Now;
                }
                else if (order.Status == "Đã soạn xong")
                {
                    // TRƯỜNG HỢP 2: Đơn "mở" - Người đầu tiên quét sẽ nhận đơn
                    var newDelivery = new Delivery
                    {
                        SOID = soId,
                        UserID = currentUserId,
                        Status = "Đang giao hàng",
                        DeliveryTime = DateTime.Now
                    };
                    _context.Deliveries.Add(newDelivery);
                }
                else
                {
                    TempData["ErrorMessage"] = $"Đơn hàng đang ở trạng thái '{order.Status}', không thể nhận.";
                    return RedirectToAction("Delivery");
                }

                order.Status = "Đang giao hàng";
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _hubContext.Clients.All.SendAsync("OrderHandedOver", soId);

                TempData["SuccessMessage"] = "Nhận đơn thành công! Bạn đã được gán làm người giao cho đơn này.";
                return RedirectToAction("Delivery");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Lỗi khi nhận đơn: " + ex.Message;
                return RedirectToAction("Delivery");
            }
        }

        [HttpGet]
        public IActionResult SearchDeliveryTimeline(string orderCode)
        {

            if (string.IsNullOrEmpty(orderCode))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mã đơn hàng để tìm kiếm!";
                return RedirectToAction("Delivery", "Delivery", null, "menu3");
            }

            string? idString = orderCode.Split('-').LastOrDefault(); 
            int.TryParse(idString, out int searchSoId);

            var delivery = _context.Deliveries
                .Include(d => d.DeliveryTrackings) 
                .FirstOrDefault(d => d.SOID == searchSoId); 

            if (delivery == null)
            {
                TempData["ErrorMessage"] = $"Không tìm thấy đơn hàng nào khớp với mã: {orderCode}";
                return RedirectToAction("Delivery", "Delivery", null, "menu3");
            }

            TempData["SearchedDeliveryId"] = delivery.DeliveryID;
            
            return RedirectToAction("Delivery", "Delivery", new { searchCode = orderCode }, "menu3");
        }


        [HttpPost]
        public async Task<IActionResult> AddTrackingEvent(int deliveryId, string statusEvent, string note)
        {
            var delivery = await _context.Deliveries.FindAsync(deliveryId);
            if (delivery == null) return NotFound();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int userId);

            var currentUser = await _context.Users.FindAsync(userId);
            string currentShipperName = currentUser != null ? currentUser.FullName : "Nhân viên hệ thống";

            var trackingNode = new DeliveryTracking
            {
                DeliveryID = deliveryId,
                StatusEvent = statusEvent,
                Note = note,
                EventTime = DateTime.Now,
                ShipperName = currentShipperName
            };

            _context.DeliveryTrackings.Add(trackingNode);
            
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã cập nhật nhật ký hành trình thành công!";
            
            string formattedOrderCode = $"SO-{DateTime.Now.Year}-{delivery.SOID:D3}";

            return RedirectToAction("Delivery", "Delivery", new { searchCode = formattedOrderCode }, "menu3");
        }
    }
}