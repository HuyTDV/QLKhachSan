using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace QLKhachSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly Hotel01Context _context;

        public UserController(Hotel01Context context)
        {
            _context = context;
        }

        // GET: Admin/User
        public async Task<IActionResult> Index(string searchString, string roleFilter, int page = 1, int pageSize = 10)
        {
            ViewData["CurrentSearch"] = searchString;
            ViewData["RoleFilter"] = roleFilter;
            ViewData["PageSize"] = pageSize;

            var usersQuery = _context.Users.AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u =>
                    u.FullName.Contains(searchString) ||
                    u.Username.Contains(searchString) ||
                    u.Email.Contains(searchString) ||
                    (u.Phone != null && u.Phone.Contains(searchString)));
            }

            // Lọc theo vai trò
            if (!string.IsNullOrEmpty(roleFilter))
            {
                usersQuery = usersQuery.Where(u => u.Role == roleFilter);
            }

            // Đếm tổng số bản ghi
            var totalItems = await usersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Phân trang
            var users = await usersQuery
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserViewModel
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Username = u.Username,
                    Email = u.Email,
                    Phone = u.Phone,
                    Role = u.Role,
                    Address = u.Address,
                    Avatar = u.Avatar,
                    CreatedAt = u.CreatedAt,
                    BookingCount = u.Bookings.Count(),
                    ReviewCount = u.Reviews.Count()
                })
                .ToListAsync();

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalItems"] = totalItems;

            return View(users);
        }

        // GET: Admin/User/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Bookings)
                    .ThenInclude(b => b.Room)
                .Include(u => u.Reviews)
                    .ThenInclude(r => r.Room)
                .Include(u => u.UserLogs.OrderByDescending(l => l.ActionTime).Take(10))
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Admin/User/Create
        public IActionResult Create()
        {
            return View(new CreateUserViewModel());
        }

        // POST: Admin/User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra username trùng
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Username đã tồn tại!");
                    return View(model);
                }

                // Kiểm tra email trùng
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại!");
                    return View(model);
                }

                var user = new User
                {
                    FullName = model.FullName,
                    Username = model.Username,
                    PasswordHash = HashPassword(model.Password),
                    Email = model.Email,
                    Phone = model.Phone,
                    Role = model.Role,
                    Address = model.Address,
                    Avatar = model.Avatar,
                    CreatedAt = DateTime.Now
                };

                _context.Add(user);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Thêm người dùng thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Admin/User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditUserViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                Address = user.Address,
                Avatar = user.Avatar
            };

            return View(model);
        }

        // POST: Admin/User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditUserViewModel model)
        {
            if (id != model.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.Users.FindAsync(id);
                    if (user == null)
                    {
                        return NotFound();
                    }

                    // Kiểm tra email trùng (trừ chính nó)
                    if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.UserId != id))
                    {
                        ModelState.AddModelError("Email", "Email đã tồn tại!");
                        return View(model);
                    }

                    user.FullName = model.FullName;
                    user.Email = model.Email;
                    user.Phone = model.Phone;
                    user.Role = model.Role;
                    user.Address = model.Address;

                    if (!string.IsNullOrEmpty(model.Avatar))
                    {
                        user.Avatar = model.Avatar;
                    }

                    // Nếu có thay đổi password
                    if (!string.IsNullOrEmpty(model.NewPassword))
                    {
                        user.PasswordHash = HashPassword(model.NewPassword);
                    }

                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Cập nhật người dùng thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(model.UserId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(model);
        }

        // POST: Admin/User/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });
            }

            // Tạo mật khẩu mới ngẫu nhiên (8 ký tự)
            string newPassword = GenerateRandomPassword(8);
            user.PasswordHash = HashPassword(newPassword);

            await _context.SaveChangesAsync();

            // Trong thực tế, nên gửi email cho user
            return Json(new { success = true, message = "Reset mật khẩu thành công!", newPassword = newPassword });
        }

        // GET: Admin/User/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Bookings)
                .Include(u => u.Reviews)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users
                .Include(u => u.Bookings)
                .Include(u => u.Reviews)
                .Include(u => u.UserLogs)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user != null)
            {
                // Kiểm tra nếu user có dữ liệu liên quan
                if (user.Bookings.Any() || user.Reviews.Any())
                {
                    TempData["Error"] = "Không thể xóa người dùng này vì có dữ liệu liên quan (booking, review)!";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa người dùng thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/User/BulkDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(int[] userIds)
        {
            if (userIds == null || userIds.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn người dùng để xóa!" });
            }

            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .Include(u => u.Bookings)
                .Include(u => u.Reviews)
                .ToListAsync();

            var usersToDelete = users.Where(u => !u.Bookings.Any() && !u.Reviews.Any()).ToList();

            if (usersToDelete.Count == 0)
            {
                return Json(new { success = false, message = "Không có người dùng nào có thể xóa (tất cả đều có dữ liệu liên quan)!" });
            }

            _context.Users.RemoveRange(usersToDelete);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã xóa {usersToDelete.Count}/{userIds.Length} người dùng thành công!" });
        }

        // GET: Admin/User/Export
        public async Task<IActionResult> Export(string searchString, string roleFilter)
        {
            var usersQuery = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u =>
                    u.FullName.Contains(searchString) ||
                    u.Username.Contains(searchString) ||
                    u.Email.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                usersQuery = usersQuery.Where(u => u.Role == roleFilter);
            }

            var users = await usersQuery.OrderByDescending(u => u.CreatedAt).ToListAsync();

            // Tạo CSV với UTF-8 BOM để Excel hiển thị đúng tiếng Việt
            var csv = new StringBuilder();
            csv.Append('\uFEFF'); // UTF-8 BOM
            csv.AppendLine("ID,Họ tên,Username,Email,Điện thoại,Vai trò,Địa chỉ,Ngày tạo");

            foreach (var user in users)
            {
                csv.AppendLine($"\"{user.UserId}\",\"{EscapeCsv(user.FullName)}\",\"{EscapeCsv(user.Username)}\",\"{EscapeCsv(user.Email)}\",\"{EscapeCsv(user.Phone)}\",\"{EscapeCsv(user.Role)}\",\"{EscapeCsv(user.Address)}\",\"{user.CreatedAt:dd/MM/yyyy}\"");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"DanhSachNguoiDung_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.Replace("\"", "\"\"");
        }
    }

    // View Models
    public class UserViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public string Address { get; set; }
        public string Avatar { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int BookingCount { get; set; }
        public int ReviewCount { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100, ErrorMessage = "Họ tên không được quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập username")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username phải từ 3-50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_]{3,50}$", ErrorMessage = "Username chỉ chứa chữ, số và _ (3-50 ký tự)")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được quá 100 ký tự")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được quá 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        [Display(Name = "Vai trò")]
        public string Role { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không được quá 200 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [StringLength(200, ErrorMessage = "Tên avatar không được quá 200 ký tự")]
        [Display(Name = "Avatar")]
        public string Avatar { get; set; }
    }

    public class EditUserViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100, ErrorMessage = "Họ tên không được quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Display(Name = "Username")]
        public string Username { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được quá 100 ký tự")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được quá 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        [Display(Name = "Vai trò")]
        public string Role { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không được quá 200 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [StringLength(200, ErrorMessage = "Tên avatar không được quá 200 ký tự")]
        [Display(Name = "Avatar")]
        public string Avatar { get; set; }
    }
}