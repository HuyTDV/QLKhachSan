using System.ComponentModel.DataAnnotations;

namespace QLKhachSan.Models
{
    public class BookingFilterViewModel
    {
        [Display(Name = "Chi nhánh")]
        public int? BranchId { get; set; }

        [Display(Name = "Trạng thái")]
        public string? Status { get; set; }

        [Display(Name = "Từ ngày")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "Đến ngày")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Tìm kiếm")]
        public string? SearchTerm { get; set; }

        // Kết quả sau khi filter
        public List<Booking>? Bookings { get; set; }
        public List<HotelBranch>? Branches { get; set; }
    }
}