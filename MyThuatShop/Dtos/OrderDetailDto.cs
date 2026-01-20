namespace MyThuatShop.Dtos.Orders;

public class OrderDetailDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }

    public decimal TotalPrice { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal? Discount { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime CreateAt { get; set; }

    public List<OrderDetailItemDto> Items { get; set; } = new();
}

public class OrderDetailItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string ProductName { get; set; } = "";
    public string? Thumbnail { get; set; }
}
