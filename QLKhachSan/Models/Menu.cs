using System;
using System.Collections.Generic;

namespace QLKhachSan.Models;

public partial class Menu
{
    public int MenuId { get; set; }

    public string? MenuName { get; set; }

    public string? Url { get; set; }

    public string? Icon { get; set; }

    public string? Role { get; set; }

    public int? SortOrder { get; set; }

    public bool? IsActive { get; set; }
}
