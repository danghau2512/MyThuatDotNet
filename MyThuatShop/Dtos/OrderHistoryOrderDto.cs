namespace MyThuatShop.Dtos.Orders;

public class OrderHistoryOrderDto
{
    public int Id { get; set; }
    public DateTime CreateAt { get; set; }
    public decimal TotalPrice { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = "";
    public List<OrderHistoryItemDto> Items { get; set; } = new();
}

public class OrderHistoryItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string ProductName { get; set; } = "";
    public string? Thumbnail { get; set; }
    public decimal LineTotal { get; set; }
}
