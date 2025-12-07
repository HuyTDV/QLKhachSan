using System;
using System.Collections.Generic;

namespace QLKhachSan.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? BookingId { get; set; }

    public decimal? Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? TransactionCode { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? Notes { get; set; }

    public virtual Booking? Booking { get; set; }
}
