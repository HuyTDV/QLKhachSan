using System;
using System.Collections.Generic;

namespace QLKhachSan.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public int? BranchId { get; set; }

    public string? RoomNumber { get; set; }

    public string? RoomType { get; set; }

    public int? Capacity { get; set; }

    public decimal? Price { get; set; }

    public string? Amenities { get; set; }

    public string? Status { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual HotelBranch? Branch { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<RoomMaintenance> RoomMaintenances { get; set; } = new List<RoomMaintenance>();
}
