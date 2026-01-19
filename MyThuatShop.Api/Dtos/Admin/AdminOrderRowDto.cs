namespace MyThuatShop.Api.Dtos.Admin;

public class AdminOrderRowDto
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? CreateAt { get; set; }
    public string? Address { get; set; }
    public decimal TotalPrice { get; set; }
    public string? StatusName { get; set; }

    public List<AdminOrderItemDto> Items { get; set; } = new();
    public string? ProductNames { get; set; }
    public int StatusId { get; set; }

}

public class AdminOrderItemDto
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public record ChangeOrderStatusRequest(int OrderId, int StatusId);
public record UpdateOrderInfoRequest(int OrderId, string FullName, string PhoneNumber, string Address);
