namespace MyThuatShop.Api.Dtos.Orders;

public class CreateOrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CreateOrderRequestDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }

    public string? PaymentName { get; set; } = "COD";

    // basic: chưa voucher/ship
    public decimal ShippingFee { get; set; } = 0;
    public decimal Discount { get; set; } = 0;
    public int? VoucherId { get; set; }

    public List<CreateOrderItemDto> Items { get; set; } = new();
}
