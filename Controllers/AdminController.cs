using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models;
using SCM_System.Models.ViewModels;
using System.Globalization;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "Quản trị viên")]
    public class AdminController : Controller
    {
        private readonly SCMDbContext _context;

        public AdminController(SCMDbContext context)
        {
            _context = context;
        }

        // =====================================================================
        // USERS MANAGEMENT
        // =====================================================================
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .Select(u => new UserViewModel
                {
                    UserID = u.UserID,
                    FullName = u.FullName,
                    Username = u.Username,
                    RoleName = u.Role.RoleName,
                    Email = u.Email,
                    Status = string.IsNullOrEmpty(u.PhoneNumber) ? "Chờ duyệt" : "Đang hoạt động"
                })
                .ToListAsync();

            var roles = await _context.Roles.ToListAsync();

            var vm = new AdminUserViewModel
            {
                Users = users,
                Roles = roles
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.PhoneNumber = "ACT-001"; // Generic activation code to mark as active
            _context.Update(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã duyệt kích hoạt tài khoản {user.Username}!";
            return RedirectToAction("Users");
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == id);
            
            if (user == null) return NotFound();

            return Json(new {
                userId = user.UserID,
                fullName = user.FullName,
                username = user.Username,
                email = user.Email,
                phone = user.PhoneNumber,
                roleId = user.RoleID
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(int userId, string fullName, string email, string phone, int roleId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.FullName = fullName;
            user.Email = email;
            user.PhoneNumber = phone;
            user.RoleID = roleId;

            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật tài khoản thành công!";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa tài khoản thành công!";
            return RedirectToAction("Users");
        }

        // =====================================================================
        // CONFIGURATION
        // =====================================================================
        public async Task<IActionResult> Config()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SystemSetting(); // Default values
                _context.SystemSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            var vm = new AdminConfigViewModel
            {
                LowStockThreshold = settings.LowStockThreshold,
                WarrantyAlertDays = settings.WarrantyAlertDays,
                AutoBackup = settings.AutoBackup,
                EnableEmail = settings.EnableEmail,
                EnableSMS = settings.EnableSMS,
                Currency = settings.Currency,
                TimeZone = settings.TimeZone
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveConfig(AdminConfigViewModel model)
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null) settings = new SystemSetting();

            settings.LowStockThreshold = model.LowStockThreshold;
            settings.WarrantyAlertDays = model.WarrantyAlertDays;
            settings.AutoBackup = model.AutoBackup;
            settings.EnableEmail = model.EnableEmail;
            settings.EnableSMS = model.EnableSMS;
            settings.Currency = model.Currency;
            settings.TimeZone = model.TimeZone;

            _context.Update(settings);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cấu hình hệ thống đã được cập nhật thành công!";
            return RedirectToAction("Config");
        }

        // =====================================================================
        // REPORTS
        // =====================================================================
        public async Task<IActionResult> Reports(string reportType = "summary", DateTime? fromDate = null, DateTime? toDate = null)
        {
            // 0. Get Settings for Currency
            var settings = await _context.SystemSettings.FirstOrDefaultAsync() ?? new SystemSetting();
            string symbol = settings.Currency == "VND" ? "₫" : (settings.Currency == "USD" ? "$" : "€");

            // 1. Basic Stats (Filter by date if provided)
            var saleQuery = _context.SaleOrders.AsQueryable();
            var purchaseQuery = _context.PurchaseOrders.AsQueryable();
            var deliveryQuery = _context.Deliveries.AsQueryable();
            var returnQuery = _context.ReturnOrders.AsQueryable();

            if (fromDate.HasValue)
            {
                saleQuery = saleQuery.Where(o => o.OrderDate >= fromDate.Value);
                purchaseQuery = purchaseQuery.Where(o => o.OrderDate >= fromDate.Value);
                deliveryQuery = deliveryQuery.Where(d => d.DeliveryTime >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                saleQuery = saleQuery.Where(o => o.OrderDate <= toDate.Value);
                purchaseQuery = purchaseQuery.Where(o => o.OrderDate <= toDate.Value);
                deliveryQuery = deliveryQuery.Where(d => d.DeliveryTime <= toDate.Value);
            }

            var saleOrders = await saleQuery.ToListAsync();
            var purchaseOrders = await purchaseQuery.ToListAsync();
            var deliveries = await deliveryQuery.ToListAsync();
            var returns = await returnQuery.ToListAsync();

            decimal totalRevenue = saleOrders
                .Where(so => so.Status == "Hoàn thành" || so.Status == "Đã giao")
                .Sum(so => so.TotalAmount);

            decimal totalExpense = purchaseOrders
                .Where(po => po.Status == "Hoàn thành")
                .Sum(po => po.TotalAmount);

            int completedOrders = saleOrders.Count(so => so.Status == "Hoàn thành");

            double deliverySuccessRate = deliveries.Any() 
                ? (double)deliveries.Count(d => d.Status == "Thành công") / deliveries.Count * 100 
                : 100;

            double returnRate = saleOrders.Any() 
                ? (double)returns.Count / saleOrders.Count * 100 
                : 0;

            // 2. Chart Data (Current month and 5 previous ones)
            var chartLabels = new List<string>();
            var chartRevenue = new List<decimal>();
            var chartExpense = new List<decimal>();

            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                var label = $"T{date.Month}/{date.Year % 100}";
                chartLabels.Add(label);

                var monthlyRev = saleOrders
                    .Where(so => so.OrderDate.Month == date.Month && so.OrderDate.Year == date.Year)
                    .Sum(so => so.TotalAmount);
                
                var monthlyExp = purchaseOrders
                    .Where(po => po.OrderDate.Month == date.Month && po.OrderDate.Year == date.Year)
                    .Sum(po => po.TotalAmount);

                chartRevenue.Add(monthlyRev);
                chartExpense.Add(monthlyExp);
            }

            // 3. Currency conversion logic
            decimal rate = 1;
            if (settings.Currency == "USD") rate = 25000;
            else if (settings.Currency == "EUR") rate = 27000;

            // Apply rate to chart data
            var chartRevConverted = chartRevenue.Select(v => v / rate).ToList();
            var chartExpConverted = chartExpense.Select(v => v / rate).ToList();

            // Format strings for top cards
            string revStr, expStr;
            if (settings.Currency == "VND")
            {
                revStr = (totalRevenue / 1000000000).ToString("N2") + " tỷ";
                expStr = (totalExpense / 1000000000).ToString("N2") + " tỷ";
            }
            else
            {
                revStr = (totalRevenue / rate).ToString("N0");
                expStr = (totalExpense / rate).ToString("N0");
            }

            var vm = new AdminReportViewModel
            {
                TotalRevenue = revStr,
                TotalExpense = expStr,
                CompletedOrdersCount = completedOrders,
                DeliverySuccessRate = deliverySuccessRate.ToString("N1") + "%",
                ReturnRate = returnRate.ToString("N1") + "%",
                CurrencySymbol = symbol,
                ChartLabels = chartLabels,
                ChartDataRevenue = chartRevConverted,
                ChartDataExpense = chartExpConverted,
                ReportType = reportType
            };

            return View(vm);
        }
    }
}