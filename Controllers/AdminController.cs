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
        // TRANG CHÍNH: GỘP 3 TAB (TÀI KHOẢN, CẤU HÌNH, BÁO CÁO)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Admin(string searchUser = "", int? roleId = null, string reportType = "summary", DateTime? fromDate = null, DateTime? toDate = null)
        {
            // --- 1. LẤY DỮ LIỆU TAB TÀI KHOẢN ---
            var userQuery = _context.Users.Include(u => u.Role).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchUser))
            {
                userQuery = userQuery.Where(u => u.FullName.Contains(searchUser) || (u.Email != null && u.Email.Contains(searchUser)));
            }
            
            if (roleId.HasValue && roleId.Value > 0)
            {
                userQuery = userQuery.Where(u => u.RoleID == roleId.Value);
            }

            var users = await userQuery
                .Select(u => new UserViewModel
                {
                    UserID = u.UserID,
                    FullName = u.FullName,
                    Username = u.Username,
                    RoleName = u.Role.RoleName,
                    Email = u.Email ?? "",
                    Status = string.IsNullOrEmpty(u.PhoneNumber) ? "Chờ duyệt" : "Đang hoạt động"
                })
                .ToListAsync();

            var roles = await _context.Roles.ToListAsync();
            var userVM = new AdminUserViewModel { Users = users, Roles = roles };

            ViewBag.SearchUser = searchUser;
            ViewBag.RoleId = roleId;


            // --- 2. LẤY DỮ LIỆU TAB CẤU HÌNH ---
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SystemSetting(); // Default values
                _context.SystemSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            var configVM = new AdminConfigViewModel
            {
                LowStockThreshold = settings.LowStockThreshold,
                AutoBackup = settings.AutoBackup,
                EnableEmail = settings.EnableEmail,
                EnableSMS = settings.EnableSMS,
                Currency = settings.Currency,
                TimeZone = settings.TimeZone
            };


            // --- 3. LẤY DỮ LIỆU TAB BÁO CÁO (HỆ THỐNG) ---
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var reportVM = new AdminReportViewModel
            {
                RamUsageMB = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2),
                ThreadCount = process.Threads.Count,
                StartTime = process.StartTime.ToString("dd/MM/yyyy HH:mm:ss"),
                Uptime = (DateTime.Now - process.StartTime).ToString(@"dd\.hh\:mm\:ss"),
                CpuUsage = process.TotalProcessorTime.TotalSeconds.ToString("N2") + "s", // Total CPU time used by process
                EnvironmentInfo = $"{Environment.OSVersion}, {Environment.MachineName}",
                ProcessID = process.Id
            };

            // --- 4. GỘP CHUNG VÀ TRẢ VỀ VIEW ---
            var combinedModel = new AdminCombinedViewModel
            {
                UserVM = userVM,
                ConfigVM = configVM,
                ReportVM = reportVM
            };

            return View(combinedModel);
        }

        // =====================================================================
        // CÁC HÀM XỬ LÝ DỮ LIỆU (POST / API)
        // =====================================================================

        [HttpPost]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.PhoneNumber = "ACT-001"; 
            _context.Update(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã duyệt kích hoạt tài khoản {user.Username}!";
            
            // Redirect về trang Admin, nhảy vào tab #menu1
            return RedirectToAction("Admin", "Admin", new { hash = "#menu1" });
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == id);
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
            return RedirectToAction("Admin", "Admin", new { hash = "#menu1" });
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
            return RedirectToAction("Admin", "Admin", new { hash = "#menu1" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveConfig([Bind(Prefix = "ConfigVM")] AdminConfigViewModel configVM)
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null) 
            {
                settings = new SystemSetting();
                _context.SystemSettings.Add(settings);
            }

            settings.LowStockThreshold = configVM.LowStockThreshold;
            settings.AutoBackup = configVM.AutoBackup;
            settings.EnableEmail = configVM.EnableEmail;
            settings.EnableSMS = configVM.EnableSMS;
            settings.Currency = configVM.Currency;
            settings.TimeZone = configVM.TimeZone;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cấu hình hệ thống đã được cập nhật thành công!";
            
            // Redirect về trang Admin, nhảy vào tab #menu2
            return RedirectToAction("Admin", "Admin", new { hash = "#menu2" });
        }
    }
}