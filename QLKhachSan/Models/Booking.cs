using System;
using System.Collections.Generic;

namespace QLKhachSan.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public int? UserId { get; set; }

    public int? RoomId { get; set; }

    public DateOnly? CheckIn { get; set; }

    public DateOnly? CheckOut { get; set; }

    public decimal? TotalPrice { get; set; }

    public string? Status { get; set; }

    public string? ServicesUsed { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Room? Room { get; set; }

    public virtual User? User { get; set; }
}
