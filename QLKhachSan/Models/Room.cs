using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKhachSan.Models
{
    public partial class Room
    {
        public int RoomId { get; set; }

        [Display(Name = "Chi nhánh")]
        public int? BranchId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số phòng")]
        [StringLength(50)]
        [Display(Name = "Số phòng")]
        public string? RoomNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại phòng")]
        [StringLength(100)]
        [Display(Name = "Loại phòng")]
        public string? RoomType { get; set; }

        [Display(Name = "Sức chứa")]
        [Range(1, 10, ErrorMessage = "Sức chứa từ 1-10 người")]
        public int? Capacity { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá phòng")]
        [Display(Name = "Giá phòng")]
        // Thuộc tính này được cấu hình trong OnModelCreating nên không cần [Column]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = false)]
        public decimal? Price { get; set; }

        [StringLength(300)]
        [Display(Name = "Tiện nghi")]
        public string? Amenities { get; set; }

        [StringLength(50)]
        [Display(Name = "Trạng thái")]
        public string? Status { get; set; }

        [StringLength(200)]
        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Ngày tạo")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = false)]
        public DateTime? CreatedAt { get; set; }

        // --- Navigation Property ---
        // Tên này PHẢI khớp với tên trong OnModelCreating: entity.HasOne(d => d.HotelBranch)
        [Display(Name = "Chi nhánh khách sạn")]
        public virtual HotelBranch? HotelBranch { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        public virtual ICollection<RoomMaintenance> RoomMaintenances { get; set; } = new List<RoomMaintenance>();
        [NotMapped] // Báo cho Database biết đây là biến ảo, không tạo cột trong SQL
        public virtual HotelBranch? Branch
        {
            get { return HotelBranch; }
            set { HotelBranch = value; }
        }
    }
}
