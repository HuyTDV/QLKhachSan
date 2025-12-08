using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models;

namespace QLKhachSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BookingController : Controller
    {
        private readonly Hotel01Context _context;

        public BookingController(Hotel01Context context)
        {
            _context = context;
        }

        // GET: Admin/Booking
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return View(bookings);
        }

        // GET: Admin/Booking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: Admin/Booking/Create
        public IActionResult Create()
        {
            ViewData["RoomId"] = new SelectList(_context.Rooms.Where(r => r.Status == "Available"), "RoomId", "RoomNumber");
            ViewData["UserId"] = new SelectList(_context.Users.Where(u => u.Role == "Customer"), "UserId", "FullName");
            return View();
        }

        // POST: Admin/Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookingId,UserId,RoomId,CheckIn,CheckOut,TotalPrice,Status,ServicesUsed")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                booking.CreatedAt = DateTime.Now;
                booking.Status = booking.Status ?? "Pending";
                _context.Add(booking);

                // Cập nhật trạng thái phòng
                var room = await _context.Rooms.FindAsync(booking.RoomId);
                if (room != null)
                {
                    room.Status = "Booked";
                    _context.Update(room);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đặt phòng thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomNumber", booking.RoomId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", booking.UserId);
            return View(booking);
        }

        // GET: Admin/Booking/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomNumber", booking.RoomId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", booking.UserId);
            return View(booking);
        }

        // POST: Admin/Booking/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,UserId,RoomId,CheckIn,CheckOut,TotalPrice,Status,ServicesUsed,CreatedAt")] Booking booking)
        {
            if (id != booking.BookingId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật booking thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.BookingId))
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
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomNumber", booking.RoomId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", booking.UserId);
            return View(booking);
        }

        // GET: Admin/Booking/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.BookingId == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Admin/Booking/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                // Cập nhật lại trạng thái phòng về Available
                var room = await _context.Rooms.FindAsync(booking.RoomId);
                if (room != null)
                {
                    room.Status = "Available";
                    _context.Update(room);
                }

                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa booking thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.BookingId == id);
        }
    }
}