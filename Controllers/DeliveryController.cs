using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SCM_System.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models;
using SCM_System.Models.ViewModels;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "Quản trị viên,Nhân viên vận chuyển")]
    public class DeliveryController : Controller
    {
        private readonly SCMDbContext _context;

        private readonly IHubContext<HandoverHub> _hubContext;
        public DeliveryController(SCMDbContext context, IHubContext<HandoverHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }
        // =====================================================================
        // GET: /Delivery/Delivery  — Trang chính Vận chuyển
        // =====================================================================
        public async Task<IActionResult> Delivery()
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteDelivery(int deliveryId, string result, string? proof)
        {
            var delivery = await _context.Deliveries
                .Include(d => d.SaleOrder)
                .FirstOrDefaultAsync(d => d.DeliveryID == deliveryId);

            if (delivery == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chuyến giao hàng.";
                return RedirectToAction("Delivery");
            }

            if (result == "success")
            {
                delivery.Status        = "Thành công";
                delivery.HandShakeProof = proof ?? "Đã xác nhận";
                delivery.DeliveryTime  = DateTime.Now;

                // Cập nhật trạng thái đơn hàng
                delivery.SaleOrder.Status = "Hoàn thành";

                TempData["SuccessMessage"] = "Giao hàng thành công! Đơn hàng đã hoàn thành.";
            }
            else
            {
                delivery.Status = "Khách từ chối";
                delivery.HandShakeProof = proof ?? "Khách từ chối nhận hàng";

                TempData["SuccessMessage"] = "Đã ghi nhận khách từ chối. Đơn hàng sẽ được hoàn về kho.";
            }

            _context.Update(delivery);
            _context.Update(delivery.SaleOrder);
            await _context.SaveChangesAsync();

            return RedirectToAction("Delivery");
        }

        // =====================================================================
        // POST: Ghi nhận trả hàng (giao không thành công → về kho)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordReturn(int soid, string reason, string settlement)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                TempData["ErrorMessage"] = "Không xác định được người dùng.";
                return RedirectToAction("Delivery");
            }

            var order = await _context.SaleOrders.FindAsync(soid);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Delivery");
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
            return RedirectToAction("Delivery");
        }
    
        [HttpGet]
        [Authorize] 
        public async Task<IActionResult> ScanPickup(int deliveryId)
        {
            var delivery = await _context.Deliveries
                .Include(d => d.SaleOrder)
                .FirstOrDefaultAsync(d => d.DeliveryID == deliveryId);

            if (delivery == null)
            {
                TempData["ErrorMessage"] = "Mã QR không hợp lệ hoặc đơn hàng không tồn tại!";
                return RedirectToAction("Delivery", new { hash = "#menu1" }); 
            }

            if (delivery.Status != "Chờ lấy hàng")
            {
                TempData["ErrorMessage"] = $"Đơn hàng này đang ở trạng thái '{delivery.Status}', không thể nhận hàng.";
                return RedirectToAction("Delivery", new { hash = "#menu1" });
            }
            delivery.Status = "Đang giao hàng";
            delivery.DeliveryTime = DateTime.Now; 

   
            if (delivery.SaleOrder != null)
            {
                delivery.SaleOrder.Status = "Đang giao hàng";
            }
            await _context.SaveChangesAsync();
                
            await _hubContext.Clients.All.SendAsync("OrderHandedOver", deliveryId);

            TempData["SuccessMessage"] = $"Quét QR thành công! Đã nhận đơn SO-{delivery.SOID:D5} từ kho.";
            
            return RedirectToAction("Delivery","Delivery", new { hash = "#menu1" }); 
        }
    }
}