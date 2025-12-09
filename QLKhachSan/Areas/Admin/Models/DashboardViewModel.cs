using QLKhachSan.Models;

namespace QLKhachSan.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        // Thống kê cơ bản
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int BookedRooms { get; set; }
        public int MaintenanceRooms { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int MonthlyBookings { get; set; }

        // Dữ liệu cho danh sách
        public List<Booking> RecentBookings { get; set; }
        public List<RoomMaintenance> MaintenanceNeeded { get; set; }

        // Dữ liệu lọc thời gian
        public int SelectedMonth { get; set; }
        public int SelectedYear { get; set; }

        // Dữ liệu BIỂU ĐỒ (Mới thêm)
        // 1. Biểu đồ doanh thu từng ngày trong tháng
        public List<string> RevenueLabels { get; set; } // Ngày 1, Ngày 2...
        public List<decimal> RevenueValues { get; set; } // Tiền tương ứng

        // 2. Biểu đồ trạng thái phòng (Pie Chart)
        public List<int> RoomStatusCounts { get; set; } // [Số phòng trống, Đã đặt, Bảo trì]
    }
}