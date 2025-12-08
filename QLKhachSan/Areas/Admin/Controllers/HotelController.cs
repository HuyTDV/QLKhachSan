using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models;

namespace QLKhachSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HotelController : Controller
    {
        private readonly Hotel01Context _context;

        public HotelController(Hotel01Context context)
        {
            _context = context;
        }

        // GET: Admin/Hotel
        public async Task<IActionResult> Index()
        {
            var hotels = await _context.Hotels
                .Include(h => h.HotelBranches)
                .ToListAsync();
            return View(hotels);
        }

        // GET: Admin/Hotel/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hotel = await _context.Hotels
                .Include(h => h.HotelBranches)
                .FirstOrDefaultAsync(m => m.HotelId == id);

            if (hotel == null)
            {
                return NotFound();
            }

            return View(hotel);
        }

        // GET: Admin/Hotel/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Hotel/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("HotelId,HotelName,Rating,Description,Hotline,Email")] Hotel hotel)
        {
            if (ModelState.IsValid)
            {
                hotel.CreatedAt = DateTime.Now;
                _context.Add(hotel);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm khách sạn thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(hotel);
        }

        // GET: Admin/Hotel/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null)
            {
                return NotFound();
            }
            return View(hotel);
        }

        // POST: Admin/Hotel/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("HotelId,HotelName,Rating,Description,Hotline,Email,CreatedAt")] Hotel hotel)
        {
            if (id != hotel.HotelId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(hotel);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật thông tin khách sạn thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HotelExists(hotel.HotelId))
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
            return View(hotel);
        }

        // GET: Admin/Hotel/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hotel = await _context.Hotels
                .FirstOrDefaultAsync(m => m.HotelId == id);
            if (hotel == null)
            {
                return NotFound();
            }

            return View(hotel);
        }

        // POST: Admin/Hotel/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel != null)
            {
                _context.Hotels.Remove(hotel);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa khách sạn thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool HotelExists(int id)
        {
            return _context.Hotels.Any(e => e.HotelId == id);
        }
    }
}