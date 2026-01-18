namespace MyThuatShop.Dtos.Admin;

public class AdminOrderRowDto
{
    public int Id { get; set; }

    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? CreateAt { get; set; }
    public string? Address { get; set; }

    public decimal TotalPrice { get; set; }
    public string? StatusName { get; set; }

    // hiển thị list sản phẩm theo đơn (nếu có)
    public List<AdminOrderItemDto> Items { get; set; } = new();

    // fallback nếu API chưa có Items (giống Overview)
    public string? ProductNames { get; set; }
}

public class AdminOrderItemDto
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
