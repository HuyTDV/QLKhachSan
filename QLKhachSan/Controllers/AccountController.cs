using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models;
using QLKhachSan.ViewModels;
using BCrypt.Net;
using Microsoft.AspNetCore.Http; // Cần thiết cho các extension Session
using Microsoft.AspNetCore.Authorization;

namespace QLKhachSan.Controllers
{
    public class AccountController : Controller
    {
        private readonly Hotel01Context _context;

        public AccountController(Hotel01Context context)
        {
            _context = context;
        }
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Dùng User.FindFirstValue để lấy Claim Role
                var role = User.FindFirstValue(ClaimTypes.Role);
                return RedirectToDefaultPage(role ?? string.Empty); // Fix Null
            }
            return View();
        }
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken] // Nên có
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user == null)
            {
                ViewBag.Message = "Không tìm thấy tài khoản.";
                return View(model);
            }

            // Kiểm tra mật khẩu HASH
            if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ViewBag.Message = "Mật khẩu không đúng.";
                return View(model);
            }

            // 1. TẠO CLAIMS
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role ?? "") // Fix Null: Nếu Role null, gán chuỗi rỗng
            };

            var claimsIdentity = new ClaimsIdentity( // FIX: Đã xóa 'new' thừa
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            // 2. TẠO AUTHENTICATION COOKIE
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Tùy chọn: Vẫn lưu FullName vào Session
            HttpContext.Session.SetString("FullName", user.FullName ?? string.Empty); // Fix Null

            // 3. ĐIỀU HƯỚNG CHUẨN THEO AREA
            return RedirectToDefaultPage(user.Role ?? string.Empty); // Fix Null
        }
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra username trùng
            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("", "Tên đăng nhập đã tồn tại");
                return View(model);
            }

            // Kiểm tra email trùng
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("", "Email đã tồn tại");
                return View(model);
            }

            // Tạo user mới
            var user = new User
            {
                FullName = model.FullName,
                Username = model.Username,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "Customer", // ⭐ GÁN CỨNG ROLE
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Sau khi đăng ký xong → chuyển về Login
            return RedirectToAction("Login");
        }


        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        // Hàm hỗ trợ chuyển hướng
        private IActionResult RedirectToDefaultPage(string role)
        {
            return role switch
            {
                "Admin" => RedirectToAction("Index", "Home", new { area = "Admin" }),
                "Customer" => RedirectToAction("Index", "Home", new { area = "" }),
                _ => RedirectToAction("Index", "Home", new { area = "" })
            };
        }
    }
}
