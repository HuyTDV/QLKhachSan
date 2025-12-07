using System;
using System.Collections.Generic;

namespace QLKhachSan.Models;

public partial class User
{
    public int UserId { get; set; }

    public string? FullName { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Role { get; set; }

    public string? Address { get; set; }

    public string? Avatar { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<HotelBranch> HotelBranches { get; set; } = new List<HotelBranch>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<RoomMaintenance> RoomMaintenances { get; set; } = new List<RoomMaintenance>();

    public virtual ICollection<UserLog> UserLogs { get; set; } = new List<UserLog>();
}
