using System;
using System.Collections.Generic;

namespace MyThuatShop.Api.Models;

public partial class Voucher
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string? Name { get; set; }

    public string? Description { get; set; }

    public decimal VoucherCash { get; set; }

    public decimal MinOrderValue { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int Quantity { get; set; }

    public int QuantityUsed { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
