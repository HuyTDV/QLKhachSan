using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models;

namespace QLKhachSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ServiceController : Controller
    {
        private readonly Hotel01Context _context;

        public ServiceController(Hotel01Context context)
        {
            _context = context;
        }

        // GET: Admin/Service
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services
                .OrderBy(s => s.ServiceName)
                .ToListAsync();

            return View(services);
        }

        // GET: Admin/Service/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dịch vụ!";
                return RedirectToAction(nameof(Index));
            }

            var service = await _context.Services
                .FirstOrDefaultAsync(m => m.ServiceId == id);

            if (service == null)
            {
                TempData["Error"] = "Dịch vụ không tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            return View(service);
        }

        // GET: Admin/Service/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Service/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceId,ServiceName,Description,Price")] Service service)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate service name
                    var existingService = await _context.Services
                        .FirstOrDefaultAsync(s => s.ServiceName.ToLower() == service.ServiceName.ToLower());

                    if (existingService != null)
                    {
                        ModelState.AddModelError("ServiceName", "Tên dịch vụ đã tồn tại!");
                        return View(service);
                    }

                    // Validate price
                    if (service.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Giá dịch vụ phải lớn hơn 0!");
                        return View(service);
                    }

                    _context.Add(service);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Thêm dịch vụ '{service.ServiceName}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi thêm dịch vụ: " + ex.Message);
                }
            }

            return View(service);
        }

        // GET: Admin/Service/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dịch vụ!";
                return RedirectToAction(nameof(Index));
            }

            var service = await _context.Services.FindAsync(id);

            if (service == null)
            {
                TempData["Error"] = "Dịch vụ không tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            return View(service);
        }

        // POST: Admin/Service/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ServiceId,ServiceName,Description,Price")] Service service)
        {
            if (id != service.ServiceId)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ!";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate service name (excluding current service)
                    var existingService = await _context.Services
                        .FirstOrDefaultAsync(s => s.ServiceName.ToLower() == service.ServiceName.ToLower()
                                                && s.ServiceId != service.ServiceId);

                    if (existingService != null)
                    {
                        ModelState.AddModelError("ServiceName", "Tên dịch vụ đã tồn tại!");
                        return View(service);
                    }

                    // Validate price
                    if (service.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Giá dịch vụ phải lớn hơn 0!");
                        return View(service);
                    }

                    _context.Update(service);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Cập nhật dịch vụ '{service.ServiceName}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(service.ServiceId))
                    {
                        TempData["Error"] = "Dịch vụ không tồn tại!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật: " + ex.Message);
                }
            }

            return View(service);
        }

        // GET: Admin/Service/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dịch vụ!";
                return RedirectToAction(nameof(Index));
            }

            var service = await _context.Services
                .FirstOrDefaultAsync(m => m.ServiceId == id);

            if (service == null)
            {
                TempData["Error"] = "Dịch vụ không tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            return View(service);
        }

        // POST: Admin/Service/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var service = await _context.Services.FindAsync(id);

                if (service == null)
                {
                    TempData["Error"] = "Dịch vụ không tồn tại!";
                    return RedirectToAction(nameof(Index));
                }

                // Check if service is being used in any bookings
                var isUsedInBookings = await _context.Bookings
                    .AnyAsync(b => b.ServicesUsed != null && b.ServicesUsed.Contains(service.ServiceName));

                if (isUsedInBookings)
                {
                    TempData["Error"] = $"Không thể xóa dịch vụ '{service.ServiceName}' vì đang được sử dụng trong các đặt phòng!";
                    return RedirectToAction(nameof(Index));
                }

                var serviceName = service.ServiceName;
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Xóa dịch vụ '{serviceName}' thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi xóa dịch vụ: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ServiceExists(int id)
        {
            return _context.Services.Any(e => e.ServiceId == id);
        }

        // API: Get service by ID (for AJAX calls)
        [HttpGet]
        public async Task<IActionResult> GetService(int id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
            {
                return NotFound(new { success = false, message = "Dịch vụ không tồn tại!" });
            }

            return Json(new
            {
                success = true,
                data = service
            });
        }

        // API: Search services (for AJAX calls)
        [HttpGet]
        public async Task<IActionResult> SearchServices(string keyword)
        {
            var services = await _context.Services
                .Where(s => string.IsNullOrEmpty(keyword) ||
                           s.ServiceName.Contains(keyword) ||
                           s.Description.Contains(keyword))
                .OrderBy(s => s.ServiceName)
                .ToListAsync();

            return Json(new
            {
                success = true,
                data = services
            });
        }
    }
}