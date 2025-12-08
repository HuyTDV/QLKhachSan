using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models;

namespace QLKhachSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RoomMaintenanceController : Controller
    {
        private readonly Hotel01Context _context;

        public RoomMaintenanceController(Hotel01Context context)
        {
            _context = context;
        }

        // GET: Admin/RoomMaintenance
        public async Task<IActionResult> Index()
        {
            var maintenances = await _context.RoomMaintenances
                .Include(r => r.Room)
                .Include(r => r.Staff)
                .OrderByDescending(r => r.MaintenanceDate)
                .ToListAsync();
            return View(maintenances);
        }

        // GET: Admin/RoomMaintenance/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenance = await _context.RoomMaintenances
                .Include(r => r.Room)
                .Include(r => r.Staff)
                .FirstOrDefaultAsync(m => m.MaintenanceId == id);

            if (maintenance == null)
            {
                return NotFound();
            }

            return View(maintenance);
        }

        // GET: Admin/RoomMaintenance/Create
        public IActionResult Create()
        {
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomNumber");
            ViewData["StaffId"] = new SelectList(_context.Users.Where(u => u.Role == "Staff"), "UserId", "FullName");
            return View();
        }

        // POST: Admin/RoomMaintenance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaintenanceId,RoomId,Description,StaffId,Status")] RoomMaintenance maintenance)
        {
            if (ModelState.IsValid)
            {
                maintenance.MaintenanceDate = DateTime.Now;
                maintenance.Status = maintenance.Status ?? "Pending";
                _context.Add(maintenance);

                // Cập nhật trạng thái phòng
                var room = await _context.Rooms.FindAsync(maintenance.RoomId);
                if (room != null && maintenance.Status == "In Progress")
                {
                    room.Status = "Maintenance";
                    _context.Update(room);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm bảo trì thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomNumber", maintenance.RoomId);
            ViewData["StaffId"] = new SelectList(_context.Users, "UserId", "FullName", maintenance.StaffId);
            return View(maintenance);
        }

        // GET: Admin/RoomMaintenance/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenance = await _context.RoomMaintenances.FindAsync(id);
            if (maintenance == null)
            {
                return NotFound();
            }
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomNumber", maintenance.RoomId);
            ViewData["StaffId"] = new SelectList(_context.Users, "UserId", "FullName", maintenance.StaffId);
            return View(maintenance);
        }

        // POST: Admin/RoomMaintenance/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaintenanceId,RoomId,Description,MaintenanceDate,StaffId,Status")] RoomMaintenance maintenance)
        {
            if (id != maintenance.MaintenanceId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(maintenance);

                    // Nếu hoàn thành bảo trì, cập nhật trạng thái phòng
                    if (maintenance.Status == "Completed")
                    {
                        var room = await _context.Rooms.FindAsync(maintenance.RoomId);
                        if (room != null)
                        {
                            room.Status = "Available";
                            _context.Update(room);
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật bảo trì thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaintenanceExists(maintenance.MaintenanceId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomNumber", maintenance.RoomId);
            ViewData["StaffId"] = new SelectList(_context.Users, "UserId", "FullName", maintenance.StaffId);
            return View(maintenance);
        }

        // GET: Admin/RoomMaintenance/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenance = await _context.RoomMaintenances
                .Include(r => r.Room)
                .Include(r => r.Staff)
                .FirstOrDefaultAsync(m => m.MaintenanceId == id);
            if (maintenance == null)
            {
                return NotFound();
            }

            return View(maintenance);
        }

        // POST: Admin/RoomMaintenance/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var maintenance = await _context.RoomMaintenances.FindAsync(id);
            if (maintenance != null)
            {
                _context.RoomMaintenances.Remove(maintenance);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa bảo trì thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MaintenanceExists(int id)
        {
            return _context.RoomMaintenances.Any(e => e.MaintenanceId == id);
        }
    }
}