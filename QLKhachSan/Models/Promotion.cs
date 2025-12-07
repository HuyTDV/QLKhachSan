using System;
using System.Collections.Generic;

namespace QLKhachSan.Models;

public partial class Promotion
{
    public int PromotionId { get; set; }

    public string? Code { get; set; }

    public string? Description { get; set; }

    public decimal? DiscountPercent { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
}
