using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models;
using System.Security.Claims;

[Authorize(Roles = "Customer")]
public class PaymentController : Controller
{
    private readonly Hotel01Context _context;

    public PaymentController(Hotel01Context context)
    {
        _context = context;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(int bookingId, decimal totalPrice)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId);

        if (booking == null)
            return NotFound();

        booking.TotalPrice = totalPrice;
        booking.Status = "Paid";

        await _context.SaveChangesAsync();

        return RedirectToAction("Detail", "MyBooking", new { id = bookingId });
    }
}
