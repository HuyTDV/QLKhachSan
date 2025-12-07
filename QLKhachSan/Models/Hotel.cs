using System;
using System.Collections.Generic;

namespace QLKhachSan.Models;

public partial class Hotel
{
    public int HotelId { get; set; }

    public string HotelName { get; set; } = null!;

    public decimal? Rating { get; set; }

    public string? Description { get; set; }

    public string? Hotline { get; set; }

    public string? Email { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<HotelBranch> HotelBranches { get; set; } = new List<HotelBranch>();
}
