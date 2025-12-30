using System;
using System.Collections.Generic;

namespace MyThuatShop.Api.Models;

public partial class Payment
{
    public int Id { get; set; }

    public string PaymentName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
