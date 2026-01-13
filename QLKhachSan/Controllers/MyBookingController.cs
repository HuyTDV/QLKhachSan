using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models;
using System.Security.Claims;

[Authorize(Roles = "Customer")]
public class MyBookingController : Controller
{
    private readonly Hotel01Context _context;

    public MyBookingController(Hotel01Context context)
    {
        _context = context;
    }

    // Danh sách lịch sử
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var bookings = await _context.Bookings
            .Include(b => b.Room)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(bookings);
    }

    // Chi tiết 1 booking
    public async Task<IActionResult> Detail(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var booking = await _context.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.BookingId == id && b.UserId == userId);

        if (booking == null)
            return NotFound();

        return View(booking);
    }
}
