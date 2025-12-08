using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models; // Đảm bảo namespace này đúng với project của bạn

namespace QLKhachSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RoomController : Controller
    {
        private readonly Hotel01Context _context;

        public RoomController(Hotel01Context context)
        {
            _context = context;
        }

        // GET: Admin/Room
        public async Task<IActionResult> Index()
        {
            var rooms = await _context.Rooms
                .Include(r => r.Branch)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(rooms);
        }

        // GET: Admin/Room/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .Include(r => r.Branch)
                .FirstOrDefaultAsync(m => m.RoomId == id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // GET: Admin/Room/Create
        public IActionResult Create()
        {
            ViewBag.BranchId = new SelectList(_context.HotelBranches, "BranchId", "BranchName");
            ViewBag.StatusList = new SelectList(new[]
            {
                "Available",
                "Booked",
                "Maintenance",
                "Cleaning"
            });
            return View();
        }

        // POST: Admin/Room/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomId,BranchId,RoomNumber,RoomType,Capacity,Price,Amenities,Status,ImageUrl")] Room room)
        {
            if (ModelState.IsValid)
            {
                room.CreatedAt = DateTime.Now;
                room.Status = room.Status ?? "Available";

                _context.Add(room);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm phòng thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.BranchId = new SelectList(_context.HotelBranches, "BranchId", "BranchName", room.BranchId);
            ViewBag.StatusList = new SelectList(new[] { "Available", "Booked", "Maintenance", "Cleaning" }, room.Status);
            return View(room);
        }

        // GET: Admin/Room/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            ViewBag.BranchId = new SelectList(_context.HotelBranches, "BranchId", "BranchName", room.BranchId);
            ViewBag.StatusList = new SelectList(new[] { "Available", "Booked", "Maintenance", "Cleaning" }, room.Status);
            return View(room);
        }

        // POST: Admin/Room/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RoomId,BranchId,RoomNumber,RoomType,Capacity,Price,Amenities,Status,ImageUrl,CreatedAt")] Room room)
        {
            if (id != room.RoomId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật phòng thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.RoomId))
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

            ViewBag.BranchId = new SelectList(_context.HotelBranches, "BranchId", "BranchName", room.BranchId);
            ViewBag.StatusList = new SelectList(new[] { "Available", "Booked", "Maintenance", "Cleaning" }, room.Status);
            return View(room);
        }

        // GET: Admin/Room/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .Include(r => r.Branch)
                .FirstOrDefaultAsync(m => m.RoomId == id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // POST: Admin/Room/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa phòng thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.RoomId == id);
        }

        // API: Lấy danh sách phòng theo chi nhánh
        [HttpGet]
        public async Task<JsonResult> GetRoomsByBranch(int branchId)
        {
            var rooms = await _context.Rooms
                .Where(r => r.BranchId == branchId)
                .Select(r => new { r.RoomId, r.RoomNumber, r.Status })
                .ToListAsync();

            return Json(rooms);
        }

        // API: Thống kê phòng
        public async Task<JsonResult> GetRoomStatistics()
        {
            var stats = new
            {
                Total = await _context.Rooms.CountAsync(),
                Available = await _context.Rooms.CountAsync(r => r.Status == "Available"),
                Booked = await _context.Rooms.CountAsync(r => r.Status == "Booked"),
                Maintenance = await _context.Rooms.CountAsync(r => r.Status == "Maintenance")
            };

            return Json(stats);
        }
    }
}