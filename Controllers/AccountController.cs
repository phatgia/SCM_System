using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SCM_System.Data;
using SCM_System.Models;
using SCM_System.Models.ViewModels;

using System.Security.Claims;

namespace SCM_System.Controllers
{
    public class AccountController : Controller
    {
        private readonly SCMDbContext _context;

        public AccountController(SCMDbContext context)
        {
            _context = context;
        }

        // =====================================================================
        // FORGOT PASSWORD
        // =====================================================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == model.Username.ToLower());

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy tài khoản với tên đăng nhập này.");
                return View(model);
            }

            // Chuyển sang bước 2: đặt mật khẩu mới
            return RedirectToAction("ResetPassword", new { username = user.Username });
        }

        [HttpGet]
        public IActionResult ResetPassword(string username)
        {
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("ForgotPassword");

            return View(new ResetPasswordViewModel { Username = username });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == model.Username.ToLower());

            if (user == null)
                return RedirectToAction("ForgotPassword");

            user.Password = model.NewPassword;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // =====================================================================
        // LOGIN
        // =====================================================================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Nếu đã đăng nhập rồi thì chuyển về Home
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            // Tìm user theo Username (không phân biệt hoa/thường)
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == model.Username.ToLower());

            if (user == null || user.Password != model.Password)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            // Tạo danh sách Claims (thông tin được lưu trong Cookie)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Role, user.Role.RoleName),
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // =====================================================================
        // LOGOUT
        // =====================================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // =====================================================================
        // REGISTER - CHỈ QUẢN TRỊ VIÊN
        // =====================================================================

        [HttpGet]
        [Authorize(Roles = "Quản trị viên")]
        public async Task<IActionResult> Register()
        {
            var roles = await _context.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "RoleID", "RoleName");
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Quản trị viên")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var roles = await _context.Roles.ToListAsync();
                ViewBag.Roles = new SelectList(roles, "RoleID", "RoleName");
                return View(model);
            }

            // Kiểm tra trùng Username
            bool usernameTaken = await _context.Users
                .AnyAsync(u => u.Username.ToLower() == model.Username.ToLower());

            if (usernameTaken)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập này đã được sử dụng.");
                var roles = await _context.Roles.ToListAsync();
                ViewBag.Roles = new SelectList(roles, "RoleID", "RoleName");
                return View(model);
            }

            var newUser = new User
            {
                FullName = model.FullName,
                Username = model.Username,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                RoleID = model.RoleID,
                Password = model.Password
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Tạo tài khoản \"{model.Username}\" thành công!";
            return RedirectToAction("Register");
        }

        // =====================================================================
        // PROFILE - THÔNG TIN CÁ NHÂN
        // =====================================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                UserID = user.UserID,
                RoleName = user.Role.RoleName,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId) || userId != model.UserID)
            {
                return Unauthorized();
            }

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return NotFound();

            // Khôi phục RoleName để View hiển thị nếu có lỗi
            model.RoleName = user.Role.RoleName;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra mật khẩu hiện tại nếu người dùng muốn đổi mật khẩu
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword) || user.Password != model.CurrentPassword)
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                    return View(model);
                }
                user.Password = model.NewPassword;
            }

            // Cập nhật thông tin
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            _context.Update(user);
            await _context.SaveChangesAsync();

            // Cập nhật lại Claims nếu FullName thay đổi
            if (user.FullName != User.FindFirst("FullName")?.Value)
            {
                var identity = (ClaimsIdentity)User.Identity!;
                var claim = identity.FindFirst("FullName");
                if (claim != null) identity.RemoveClaim(claim);
                identity.AddClaim(new Claim("FullName", user.FullName));
                
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            }

            TempData["Success"] = "Cập nhật thông tin cá nhân thành công!";
            return RedirectToAction("Profile");
        }
    }
}