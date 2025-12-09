using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKhachSan.Areas.Admin.Models;
using QLKhachSan.Models;
using System.Globalization;

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

        public async Task<IActionResult> Index(int? month, int? year)
        {
            var targetMonth = month ?? DateTime.Now.Month;
            var targetYear = year ?? DateTime.Now.Year;

            // 1. Lấy thống kê (Giữ nguyên logic)
            var rooms = await _context.Rooms.ToListAsync();
            var totalRooms = rooms.Count;
            var availableRooms = rooms.Count(r => r.Status == "Available");
            var bookedRooms = rooms.Count(r => r.Status == "Booked" || r.Status == "Checked-in");
            var maintenanceRooms = rooms.Count(r => r.Status == "Maintenance");

            // 2. Dữ liệu Booking trong tháng
            var bookingsInMonth = await _context.Bookings
                .Where(b => b.CheckIn.HasValue
                    && b.CheckIn.Value.Month == targetMonth
                    && b.CheckIn.Value.Year == targetYear
                    && (b.Status != "Cancelled"))
                .ToListAsync();

            var monthlyRevenue = bookingsInMonth.Sum(b => b.TotalPrice ?? 0);
            var monthlyBookingsCount = bookingsInMonth.Count;

            // 3. Xử lý dữ liệu cho BIỂU ĐỒ (MỚI)
            int daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);
            var revenueLabels = new List<string>();
            var revenueValues = new List<decimal>();

            for (int day = 1; day <= daysInMonth; day++)
            {
                revenueLabels.Add($"{day}");
                decimal dailyTotal = bookingsInMonth
                    .Where(b => b.CheckIn.Value.Day == day)
                    .Sum(b => b.TotalPrice ?? 0);
                revenueValues.Add(dailyTotal);
            }

            var recentBookings = await _context.Bookings
                .Include(b => b.User).Include(b => b.Room)
                .OrderByDescending(b => b.CreatedAt).Take(6).ToListAsync();

            var maintenanceNeeded = await _context.RoomMaintenances
                .Include(m => m.Room)
                .Where(m => m.Status == "Pending" || m.Status == "In Progress")
                .OrderByDescending(m => m.MaintenanceDate).Take(5).ToListAsync();

            // --- QUAN TRỌNG: Gán lại ViewBag để giao diện CŨ không bị lỗi ---
            ViewBag.TotalRooms = totalRooms;
            ViewBag.AvailableRooms = availableRooms;
            ViewBag.BookedRooms = bookedRooms;
            ViewBag.MaintenanceRooms = maintenanceRooms;
            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.MonthlyBookings = monthlyBookingsCount;
            ViewBag.RecentBookings = recentBookings;
            ViewBag.MaintenanceNeeded = maintenanceNeeded;
            ViewBag.SelectedMonth = targetMonth;
            ViewBag.SelectedYear = targetYear;

            // --- Gán dữ liệu cho BIỂU ĐỒ MỚI ---
            var model = new DashboardViewModel
            {
                RevenueLabels = revenueLabels,
                RevenueValues = revenueValues,
                RoomStatusCounts = new List<int> { availableRooms, bookedRooms, maintenanceRooms }
            };

            return View(model);
        }
    }
}