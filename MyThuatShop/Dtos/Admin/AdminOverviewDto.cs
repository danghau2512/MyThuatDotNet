namespace MyThuatShop.Dtos.Admin;

public class AdminOverviewDto
{
    public int TotalUsers { get; set; }
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }

    public List<LatestOrderDto> LatestOrders { get; set; } = new();
    public List<TopProductMonthDto> TopProductsMonth { get; set; } = new();
}

public class LatestOrderDto
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? CreateAt { get; set; }
    public string? Address { get; set; }
    public decimal TotalPrice { get; set; }
    public string? StatusName { get; set; }
    public string? ProductNames { get; set; }
}

public class TopProductMonthDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? CategoryName { get; set; }
    public decimal Price { get; set; }
    public DateTime? CreateAt { get; set; }
    public int SoldQty { get; set; }
}
