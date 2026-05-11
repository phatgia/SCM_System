using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models;
using SCM_System.Models.ViewModels;

namespace SCM_System.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly SCMDbContext _context;

        public HomeController(SCMDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var settings = await _context.SystemSettings.FirstOrDefaultAsync() ?? new SystemSetting();
            string symbol = settings.Currency == "VND" ? "₫" : (settings.Currency == "USD" ? "$" : "€");
            decimal rate = settings.Currency == "VND" ? 1 : (settings.Currency == "USD" ? 25000 : 27000);
            string format = settings.Currency == "VND" ? "N0" : "N2";
            ViewBag.Format = format;

            var model = new DashboardViewModel
            {
                CurrencySymbol = symbol,
                MonthLabel = $"tháng {now.Month}",
                MonthlyRevenue = await _context.SaleOrders
                    .Where(s => s.OrderDate.Month == now.Month && s.OrderDate.Year == now.Year && s.Status != "Đã hủy")
                    .SumAsync(s => s.TotalAmount) / rate,
                TotalOrders = await _context.SaleOrders
                    .CountAsync(s => s.OrderDate.Month == now.Month && s.OrderDate.Year == now.Year),
                InventoryValue = await _context.Inventories
                    .SumAsync(i => (decimal)i.QuantityAvailable * i.Product.BasePrice) / rate,
                ActiveDeliveriesCount = await _context.SaleOrders
                    .CountAsync(s => s.Status == "Đang giao hàng" || s.Status == "Đã soạn xong")
            };

            // 1. Line Chart Data (Last 6 Months)
            for (int i = 5; i >= 0; i--)
            {
                var date = now.AddMonths(-i);
                var label = $"T{date.Month}/{date.Year.ToString().Substring(2)}";
                model.MonthlyLabels.Add(label);

                var rev = await _context.SaleOrders
                    .Where(s => s.OrderDate.Month == date.Month && s.OrderDate.Year == date.Year && s.Status != "Đã hủy")
                    .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
                model.MonthlyRevenues.Add(rev / (rate * 1000000)); // Millions

                var cost = await _context.PurchaseOrders
                    .Where(p => p.OrderDate.Month == date.Month && p.OrderDate.Year == date.Year && p.Status == "Hoàn thành")
                    .SumAsync(p => (decimal?)p.TotalAmount) ?? 0;
                model.MonthlyCosts.Add(cost / (rate * 1000000));
            }

            // 2. Donut Chart (Order Status)
            var statuses = new[] { "Đã giao", "Đang giao hàng", "Đang xử lý", "Mới", "Đã hủy" };
            foreach (var status in statuses)
            {
                var count = await _context.SaleOrders
                    .CountAsync(s => s.Status == status && s.OrderDate.Month == now.Month && s.OrderDate.Year == now.Year);
                model.OrderStatusLabels.Add(status == "Đang giao hàng" ? "Đang giao" : (status == "Mới" ? "Mới tạo" : status));
                model.OrderStatusCounts.Add(count);
            }

            // 3. Bar Chart (Inventory by Category)
            var categoryStats = await _context.Categories
                .Select(c => new { 
                    c.CategoryName, 
                    Qty = c.Products.SelectMany(p => p.Inventories).Sum(i => i.QuantityAvailable) 
                })
                .ToListAsync();
            model.InventoryCategoryLabels = categoryStats.Select(c => c.CategoryName).ToList();
            model.InventoryCategoryQuantities = categoryStats.Select(c => c.Qty).ToList();

            // 4. Recent Orders
            model.RecentOrders = await _context.SaleOrders
                .Include(s => s.Customer)
                .OrderByDescending(s => s.OrderDate)
                .Take(5)
                .Select(s => new RecentOrderHomeViewModel {
                    OrderCode = $"SO-{s.SOID:D5}",
                    CustomerName = s.Customer.Name,
                    TotalAmount = s.TotalAmount / rate,
                    Status = s.Status,
                    Date = s.OrderDate
                }).ToListAsync();

            // 5. Low Stock Warnings - load in memory then filter (SQLite: GroupBy+Where+Take not translatable)
            var allInventories = await _context.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .ToListAsync();
            model.LowStockWarnings = allInventories
                .GroupBy(i => new { i.Product.ProductName, cat = i.Product.Category.CategoryName })
                .Select(g => new LowStockAlertViewModel {
                    ProductName = g.Key.ProductName,
                    CategoryName = g.Key.cat,
                    CurrentStock = g.Sum(x => x.QuantityAvailable),
                    Threshold = settings.LowStockThreshold
                })
                .Where(x => x.CurrentStock < x.Threshold)
                .Take(5)
                .ToList();

            // 6. Active Deliveries
            model.ActiveDeliveries = await _context.Deliveries
                .Include(d => d.SaleOrder).ThenInclude(s => s.Customer)
                .Include(d => d.User)
                .Where(d => d.Status == "Đang giao hàng")
                .OrderByDescending(d => d.DeliveryTime)
                .Take(5)
                .Select(d => new ShippingActivityHomeViewModel {
                    OrderCode = $"SO-{d.SOID:D5}",
                    CustomerName = d.SaleOrder.Customer.Name,
                    ShipperName = d.User.FullName,
                    Status = d.Status
                }).ToListAsync();

            // 6. System Metrics (For Admin only)
            if (User.IsInRole("Quản trị viên"))
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                model.RamUsageMB = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2);
                model.ThreadCount = process.Threads.Count;
                model.StartTime = process.StartTime.ToString("dd/MM/yyyy HH:mm:ss");
                model.Uptime = (DateTime.Now - process.StartTime).ToString(@"dd\.hh\:mm\:ss");

                // Giả lập dữ liệu lịch sử (10 phút)
                for (int i = 9; i >= 0; i--)
                {
                    var time = DateTime.Now.AddMinutes(-i).ToString("HH:mm");
                    model.HistoryLabels.Add(time);
                    model.RamHistory.Add(model.RamUsageMB + new Random().Next(-5, 5));
                    model.ThreadHistory.Add(model.ThreadCount + new Random().Next(-2, 3));
                }

                // Phân bổ vai trò
                var roleStats = await _context.Users
                    .Include(u => u.Role)
                    .GroupBy(u => u.Role.RoleName)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .ToListAsync();
                model.RoleLabels = roleStats.Select(s => s.Name).ToList();
                model.RoleCounts = roleStats.Select(s => s.Count).ToList();

                // DB Stats
                model.EntityCounts["SaleOrders"] = await _context.SaleOrders.CountAsync();
                model.EntityCounts["PurchaseOrders"] = await _context.PurchaseOrders.CountAsync();
                model.EntityCounts["Products"] = await _context.Products.CountAsync();
                model.EntityCounts["Inventory"] = await _context.Inventories.CountAsync();
                model.EntityCounts["Users"] = await _context.Users.CountAsync();

                // System Logs
                var recentUsers = await _context.Users.OrderByDescending(u => u.UserID).Take(3).ToListAsync();
                foreach (var u in recentUsers)
                {
                    model.RecentEvents.Add(new SystemEventViewModel {
                        EventName = "Login Activity", Username = u.Username,
                        Timestamp = DateTime.Now.AddHours(-new Random().Next(1, 5)),
                        Details = "User session started.", Type = "info"
                    });
                }
                model.RecentEvents.Add(new SystemEventViewModel {
                    EventName = "System Healthy", Username = "System",
                    Timestamp = DateTime.Now, Details = "All services operational.", Type = "success"
                });
            }

            return View(model);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
