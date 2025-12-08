using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Models;

namespace QLKhachSan.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly Hotel01Context _context;

        public HomeController(Hotel01Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Thống kê tổng quan
            var totalRooms = await _context.Rooms.CountAsync();
            var availableRooms = await _context.Rooms.CountAsync(r => r.Status == "Available");
            var bookedRooms = await _context.Rooms.CountAsync(r => r.Status == "Booked");
            var maintenanceRooms = await _context.Rooms.CountAsync(r => r.Status == "Maintenance");

            // Doanh thu tháng hiện tại
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var monthlyRevenue = await _context.Bookings
                .Where(b => b.CreatedAt.HasValue
                    && b.CreatedAt.Value.Month == currentMonth
                    && b.CreatedAt.Value.Year == currentYear
                    && (b.Status == "Confirmed" || b.Status == "Checked-in"))
                .SumAsync(b => b.TotalPrice ?? 0);

            // Số booking trong tháng
            var monthlyBookings = await _context.Bookings
                .Where(b => b.CreatedAt.HasValue
                    && b.CreatedAt.Value.Month == currentMonth
                    && b.CreatedAt.Value.Year == currentYear)
                .CountAsync();

            // Đặt phòng gần đây
            var recentBookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .ToListAsync();

            // Phòng cần bảo trì
            var maintenanceNeeded = await _context.RoomMaintenances
                .Include(m => m.Room)
                .Where(m => m.Status == "Pending" || m.Status == "In Progress")
                .OrderByDescending(m => m.MaintenanceDate)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalRooms = totalRooms;
            ViewBag.AvailableRooms = availableRooms;
            ViewBag.BookedRooms = bookedRooms;
            ViewBag.MaintenanceRooms = maintenanceRooms;
            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.MonthlyBookings = monthlyBookings;
            ViewBag.RecentBookings = recentBookings;
            ViewBag.MaintenanceNeeded = maintenanceNeeded;

            return View();
        }
    }
}