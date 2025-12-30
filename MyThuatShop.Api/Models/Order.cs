using System;
using System.Collections.Generic;

namespace MyThuatShop.Api.Models;

public partial class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public decimal TotalPrice { get; set; }

    public int PaymentId { get; set; }

    public int OrderStatusId { get; set; }

    public int? VoucherId { get; set; }

    public decimal? Discount { get; set; }

    public decimal ShippingFee { get; set; }

    public DateTime? CreateAt { get; set; }

    public string? Note { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual OrderStatus OrderStatus { get; set; } = null!;

    public virtual Payment Payment { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual Voucher? Voucher { get; set; }
}
