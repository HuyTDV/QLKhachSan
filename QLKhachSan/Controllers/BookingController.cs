using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKhachSan.Models;
using System.Security.Claims;
using QRCoder;
namespace QLKhachSan.Controllers
{
    [Authorize(Roles = "Customer")]
    public class BookingController : Controller
    {
        private readonly Hotel01Context _context;

        public BookingController(Hotel01Context context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int roomId,
            DateOnly checkIn,
            DateOnly checkOut,
            string services)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var booking = new Booking
            {
                UserId = userId,
                RoomId = roomId,
                CheckIn = checkIn,
                CheckOut = checkOut,
                ServicesUsed = services,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "MyBooking");
        }
        public IActionResult Qr(int id)
        {
            var content = $"BOOKING:{id}";

            var generator = new QRCodeGenerator();
            var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var qr = new PngByteQRCode(data);

            byte[] qrBytes = qr.GetGraphic(20);

            return File(qrBytes, "image/png");
        }

    }
}
