using System;
using System.Collections.Generic;

namespace QLKhachSan.Models;

public partial class HotelBranch
{
    public int BranchId { get; set; }

    public int? HotelId { get; set; }

    public string? BranchName { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? Country { get; set; }

    public int? ManagerId { get; set; }

    public string? Phone { get; set; }

    public virtual Hotel? Hotel { get; set; }

    public virtual User? Manager { get; set; }

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
