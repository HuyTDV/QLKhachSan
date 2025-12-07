using System;
using System.Collections.Generic;

namespace QLKhachSan.Models;

public partial class RoomMaintenance
{
    public int MaintenanceId { get; set; }

    public int? RoomId { get; set; }

    public string? Description { get; set; }

    public DateTime? MaintenanceDate { get; set; }

    public int? StaffId { get; set; }

    public string? Status { get; set; }

    public virtual Room? Room { get; set; }

    public virtual User? Staff { get; set; }
}
