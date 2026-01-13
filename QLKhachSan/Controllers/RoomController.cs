using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QLKhachSan.Controllers
{
    public class RoomController : Controller
    {
        private readonly Hotel01Context _context;

        public RoomController(Hotel01Context context)
        {
            _context = context;
        }

        // GET: Room/Index
        public async Task<IActionResult> Index(string priceRange = null, int? capacity = null, string viewType = null, string sortBy = null)
        {
            var roomsQuery = _context.Rooms
                .Include(r => r.HotelBranch)
                .Include(r => r.Reviews)
                .Where(r => r.Status == "Available");

            // Filter by price range
            if (!string.IsNullOrEmpty(priceRange))
            {
                var prices = priceRange.Split('-');
                if (prices.Length == 2)
                {
                    if (decimal.TryParse(prices[0], out decimal minPrice) &&
                        decimal.TryParse(prices[1], out decimal maxPrice))
                    {
                        roomsQuery = roomsQuery.Where(r => r.Price >= minPrice && r.Price <= maxPrice);
                    }
                }
                else if (priceRange == "300-500")
                {
                    // Handle "300+" range
                    roomsQuery = roomsQuery.Where(r => r.Price >= 300);
                }
            }

            // Filter by capacity
            if (capacity.HasValue)
            {
                roomsQuery = roomsQuery.Where(r => r.Capacity >= capacity.Value);
            }

            // Filter by view type (if room type contains the view)
            if (!string.IsNullOrEmpty(viewType))
            {
                roomsQuery = roomsQuery.Where(r => r.RoomType.Contains(viewType));
            }

            // Sort
            roomsQuery = sortBy switch
            {
                "price_asc" => roomsQuery.OrderBy(r => r.Price),
                "price_desc" => roomsQuery.OrderByDescending(r => r.Price),
                "capacity" => roomsQuery.OrderByDescending(r => r.Capacity),
                _ => roomsQuery.OrderBy(r => r.RoomId)
            };

            var rooms = await roomsQuery.ToListAsync();

            // Pass filter values to view
            ViewBag.PriceRange = priceRange;
            ViewBag.Capacity = capacity;
            ViewBag.ViewType = viewType;
            ViewBag.SortBy = sortBy;

            return View(rooms);
        }

        // GET: Room/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .Include(r => r.HotelBranch)
                    .ThenInclude(b => b.Hotel)
                .Include(r => r.Reviews)
                    .ThenInclude(rv => rv.User)
                .FirstOrDefaultAsync(m => m.RoomId == id);

            if (room == null)
            {
                return NotFound();
            }

            // Calculate average rating
            if (room.Reviews != null && room.Reviews.Any())
            {
                ViewBag.AverageRating = room.Reviews.Average(r => r.Rating ?? 0);
                ViewBag.TotalReviews = room.Reviews.Count;
            }
            else
            {
                ViewBag.AverageRating = 0;
                ViewBag.TotalReviews = 0;
            }

            return View(room);
        }

        // GET: Room/Book/5
        public async Task<IActionResult> Book(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .Include(r => r.HotelBranch)
                .FirstOrDefaultAsync(m => m.RoomId == id);

            if (room == null)
            {
                return NotFound();
            }

            if (room.Status != "Available")
            {
                TempData["Error"] = "This room is not available for booking.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            return View(room);
        }

        // POST: Room/Book/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int id, string checkIn, string checkOut, string servicesUsed = null)
        {
            var room = await _context.Rooms
                .Include(r => r.HotelBranch)
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (room == null)
            {
                return NotFound();
            }

            // Parse dates from string to DateTime then to DateOnly
            if (!DateTime.TryParse(checkIn, out DateTime checkInDateTime))
            {
                TempData["Error"] = "Invalid check-in date format.";
                return View(room);
            }

            if (!DateTime.TryParse(checkOut, out DateTime checkOutDateTime))
            {
                TempData["Error"] = "Invalid check-out date format.";
                return View(room);
            }

            // Convert to DateOnly
            DateOnly checkInDate = DateOnly.FromDateTime(checkInDateTime);
            DateOnly checkOutDate = DateOnly.FromDateTime(checkOutDateTime);
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            // Validate dates
            if (checkInDate < today)
            {
                TempData["Error"] = "Check-in date cannot be in the past.";
                return View(room);
            }

            if (checkOutDate <= checkInDate)
            {
                TempData["Error"] = "Check-out date must be after check-in date.";
                return View(room);
            }

            // Check for overlapping bookings
            var hasOverlap = await _context.Bookings
                .AnyAsync(b => b.RoomId == id &&
                              b.Status != "Cancelled" &&
                              ((b.CheckIn <= checkInDate && b.CheckOut > checkInDate) ||
                               (b.CheckIn < checkOutDate && b.CheckOut >= checkOutDate) ||
                               (b.CheckIn >= checkInDate && b.CheckOut <= checkOutDate)));

            if (hasOverlap)
            {
                TempData["Error"] = "This room is already booked for the selected dates.";
                return View(room);
            }

            // Get user ID from session/authentication
            // TODO: Replace with actual authentication
            int userId = 1; // Placeholder - should get from User.Identity or Session

            // Calculate total price
            int nights = checkOutDate.DayNumber - checkInDate.DayNumber;
            decimal roomTotal = room.Price.GetValueOrDefault(0) * nights;
            decimal serviceFee = 10;
            decimal tax = (roomTotal + serviceFee) * 0.1m;
            decimal totalPrice = roomTotal + serviceFee + tax;

            // Create booking
            var booking = new Booking
            {
                UserId = userId,
                RoomId = id,
                CheckIn = checkInDate,
                CheckOut = checkOutDate,
                TotalPrice = totalPrice,
                Status = "Pending",
                ServicesUsed = servicesUsed,
                CreatedAt = DateTime.Now
            };

            try
            {
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Booking created successfully! Total: ${totalPrice:F2}";

                // Redirect to booking confirmation or details page
                return RedirectToAction("BookingConfirmation", new { id = booking.BookingId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while creating the booking. Please try again.";
                // Log the exception
                // _logger.LogError(ex, "Error creating booking for room {RoomId}", id);
                return View(room);
            }
        }

        // GET: Room/BookingConfirmation/5
        public async Task<IActionResult> BookingConfirmation(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.HotelBranch)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: Room/Search
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return RedirectToAction(nameof(Index));
            }

            var rooms = await _context.Rooms
                .Include(r => r.HotelBranch)
                .Include(r => r.Reviews)
                .Where(r => r.Status == "Available" &&
                           (r.RoomType.Contains(searchTerm) ||
                            r.RoomNumber.Contains(searchTerm) ||
                            r.Amenities.Contains(searchTerm) ||
                            r.HotelBranch.BranchName.Contains(searchTerm)))
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            return View("Index", rooms);
        }

        // GET: Room/CheckAvailability
        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int roomId, string checkIn, string checkOut)
        {
            if (!DateTime.TryParse(checkIn, out DateTime checkInDateTime) ||
                !DateTime.TryParse(checkOut, out DateTime checkOutDateTime))
            {
                return Json(new { available = false, message = "Invalid date format" });
            }

            DateOnly checkInDate = DateOnly.FromDateTime(checkInDateTime);
            DateOnly checkOutDate = DateOnly.FromDateTime(checkOutDateTime);

            var hasOverlap = await _context.Bookings
                .AnyAsync(b => b.RoomId == roomId &&
                              b.Status != "Cancelled" &&
                              ((b.CheckIn <= checkInDate && b.CheckOut > checkInDate) ||
                               (b.CheckIn < checkOutDate && b.CheckOut >= checkOutDate) ||
                               (b.CheckIn >= checkInDate && b.CheckOut <= checkOutDate)));

            return Json(new { available = !hasOverlap, message = hasOverlap ? "Room is not available for selected dates" : "Room is available" });
        }
    }
}