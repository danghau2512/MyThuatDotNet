namespace MyThuatShop.Api.Dtos.AdminOverview;

public class AdminOverviewDto
{
    public int TotalUsers { get; set; }
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }          // đơn hoàn thành (status=3)
    public decimal TotalRevenue { get; set; }     // sum(totalPrice - discount) status=3

    public List<OverviewOrderRowDto> LatestOrders { get; set; } = new();
    public List<OverviewTopProductRowDto> TopProductsMonth { get; set; } = new();
}
