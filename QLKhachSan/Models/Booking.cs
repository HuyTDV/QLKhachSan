using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKhachSan.Models
{
    public partial class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Display(Name = "Khách hàng")]
        public int? UserId { get; set; }

        [Display(Name = "Phòng")]
        public int? RoomId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng")]
        [Display(Name = "Ngày nhận phòng")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateOnly? CheckIn { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng")]
        [Display(Name = "Ngày trả phòng")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateOnly? CheckOut { get; set; }

        [Display(Name = "Tổng tiền")]
        [Column(TypeName = "decimal(18, 2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = false)]
        public decimal? TotalPrice { get; set; }

        [StringLength(50)]
        [Display(Name = "Trạng thái")]
        public string? Status { get; set; }

        [StringLength(300)]
        [Display(Name = "Dịch vụ sử dụng")]
        public string? ServicesUsed { get; set; }

        [Display(Name = "Ngày đặt")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = false)]
        public DateTime? CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("RoomId")]
        [Display(Name = "Phòng")]
        public virtual Room? Room { get; set; }

        [ForeignKey("UserId")]
        [Display(Name = "Khách hàng")]
        public virtual User? User { get; set; }

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

        // Calculated properties
        [NotMapped]
        [Display(Name = "Số đêm")]
        public int NumberOfNights
        {
            get
            {
                if (CheckIn.HasValue && CheckOut.HasValue)
                {
                    return CheckOut.Value.DayNumber - CheckIn.Value.DayNumber;
                }
                return 0;
            }
        }

        [NotMapped]
        [Display(Name = "Tên trạng thái")]
        public string StatusDisplay
        {
            get
            {
                return Status switch
                {
                    "Pending" => "Chờ xác nhận",
                    "Confirmed" => "Đã xác nhận",
                    "CheckedIn" => "Đã nhận phòng",
                    "CheckedOut" => "Đã trả phòng",
                    "Cancelled" => "Đã hủy",
                    _ => Status ?? "Không xác định"
                };
            }
        }
    }
}