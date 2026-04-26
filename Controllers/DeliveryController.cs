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
    [Authorize(Roles = "Quản trị viên,Nhân viên vận chuyển")]
    public class DeliveryController : Controller
    {
        private readonly SCMDbContext _context;
        private readonly IHubContext<HandoverHub> _hubContext;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public DeliveryController(SCMDbContext context, IHubContext<HandoverHub> hubContext,
                                  IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _hubContext = hubContext;
            _env = env;
            _config = config;
        }

        // ─── HMAC helpers: bảo vệ QR ─────────────────────────────────────────
        private string GeneratePickupToken(int deliveryId, int userId)
        {
            var secret = _config["QR:Secret"] ?? "scm-qr-fallback-2026";
            var payload = $"{deliveryId}:{userId}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
        private bool ValidatePickupToken(int deliveryId, int userId, string token)
            => string.Equals(GeneratePickupToken(deliveryId, userId), token,
                             StringComparison.OrdinalIgnoreCase);

        // ─── GET /Delivery/GenerateQR?deliveryId=X ────────────────────────────
        // Thủ kho gọi API này → nhận signed URL → vẽ QR cho shipper quét
        [HttpGet]
        [Authorize(Roles = "Quản trị viên,Quản lý kho,Nhân viên vận chuyển")]
        public async Task<IActionResult> GenerateQR(int deliveryId)
        {
            var delivery = await _context.Deliveries
                .Include(d => d.User)
                .Include(d => d.SaleOrder)
                .FirstOrDefaultAsync(d => d.DeliveryID == deliveryId);

            if (delivery == null)
                return NotFound(new { message = "Không tìm thấy đơn hàng." });
            if (delivery.Status != "Chờ lấy hàng")
                return BadRequest(new { message = $"Đơn đang ở trạng thái '{delivery.Status}'." });

            var token = GeneratePickupToken(deliveryId, delivery.UserID);
            var url   = $"{Request.Scheme}://{Request.Host}/Delivery/ScanPickup"
                      + $"?deliveryId={deliveryId}&userId={delivery.UserID}"
                      + $"&token={Uri.EscapeDataString(token)}";

            return Json(new
            {
                qrUrl       = url,
                shipperName = delivery.User.FullName,
                orderCode   = $"SO-{delivery.SaleOrder.OrderDate.Year}-{delivery.SOID:D3}"
            });
        }
        // =====================================================================
        // GET: /Delivery/Delivery  — Trang chính Vận chuyển
        // =====================================================================
        public async Task<IActionResult> Delivery(string? searchCode = null)
        {
            var vm = new DeliveryViewModel();
            // ── Tab 2: Danh sách giao hàng ───────────────────────────────────
            var deliveries = await _context.Deliveries
                .Include(d => d.SaleOrder).ThenInclude(so => so.Customer)
                .Include(d => d.User)
                .OrderByDescending(d => d.DeliveryTime)
                .ToListAsync();

            vm.AllDeliveries = deliveries.Select(d => new DeliveryListItem
            {
                DeliveryID    = d.DeliveryID,
                OrderCode     = "SO-" + d.SaleOrder.OrderDate.Year + "-" + d.SOID.ToString("D3"),
                CustomerName  = d.SaleOrder.Customer.Name,
                CustomerPhone = d.SaleOrder.Customer.Phone ?? "",
                ShipperName   = d.User.FullName,
                Address       = d.SaleOrder.Customer.ShippingAddress ?? "",
                TotalAmount   = d.SaleOrder.TotalAmount,
                Status        = d.Status,
                DeliveryTime  = d.DeliveryTime
            }).ToList();

            var now = DateTime.Now;
            vm.PendingPickupCount      = deliveries.Count(d => d.Status == "Chờ lấy hàng");
            vm.InDeliveryCount         = deliveries.Count(d => d.Status == "Đang giao hàng");
            vm.CompletedThisMonthCount = deliveries.Count(d =>
                d.Status == "Thành công" &&
                d.DeliveryTime.HasValue &&
                d.DeliveryTime.Value.Month == now.Month &&
                d.DeliveryTime.Value.Year  == now.Year);

            // ── Danh sách shipper cho modal phân công ────────────────────────
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
        // Bảo mật: token HMAC + đúng shipper được phân công
        // Race condition: atomic ExecuteUpdateAsync (1 SQL, không thể bị 2 request cùng win)
        [HttpGet]
        [Authorize(Roles = "Quản trị viên,Nhân viên vận chuyển")]
        public async Task<IActionResult> ScanPickup(int deliveryId, int userId, string token)
        {
            // 1. Xác thực token HMAC
            if (string.IsNullOrEmpty(token) || !ValidatePickupToken(deliveryId, userId, token))
            {
                TempData["ErrorMessage"] = "⛔ Mã QR không hợp lệ hoặc đã bị giả mạo!";
                return RedirectToAction("Delivery");
            }

            // 2. Chỉ đúng shipper được phân công mới được quét
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int currentUserId))
                return Unauthorized();

            if (currentUserId != userId && !User.IsInRole("Quản trị viên"))
            {
                TempData["ErrorMessage"] = "⛔ Đơn hàng này được phân công cho shipper khác!";
                return RedirectToAction("Delivery");
            }

            // 3. Atomic UPDATE — chỉ 1 trong N request đồng thời thắng
            //    SQL: UPDATE Delivery SET Status=... WHERE DeliveryID=? AND Status='Chờ lấy hàng' AND UserID=?
            var rows = await _context.Deliveries
                .Where(d => d.DeliveryID == deliveryId
                         && d.Status    == "Chờ lấy hàng"
                         && d.UserID    == userId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.Status,       "Đang giao hàng")
                    .SetProperty(d => d.DeliveryTime, DateTime.Now));

            if (rows == 0)
            {
                var current = await _context.Deliveries.AsNoTracking()
                                  .FirstOrDefaultAsync(d => d.DeliveryID == deliveryId);
                TempData["ErrorMessage"] = current == null
                    ? "Đơn hàng không tồn tại."
                    : $"❌ Đơn đã được nhận bởi shipper khác (trạng thái: {current.Status}).";
                return RedirectToAction("Delivery");
            }

            // 4. Cập nhật SaleOrder + gửi SignalR (sau khi đã chiếm được Delivery)
            var claimed = await _context.Deliveries.AsNoTracking()
                              .FirstOrDefaultAsync(d => d.DeliveryID == deliveryId);
            if (claimed != null)
            {
                await _context.SaleOrders
                    .Where(so => so.SOID == claimed.SOID)
                    .ExecuteUpdateAsync(s => s.SetProperty(so => so.Status, "Đang giao hàng"));
            }

            await _hubContext.Clients.All.SendAsync("OrderHandedOver", deliveryId);

            TempData["SuccessMessage"] = "✅ Quét QR thành công! Đơn hàng đã được nhận về giao.";
            return RedirectToAction("Delivery");
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