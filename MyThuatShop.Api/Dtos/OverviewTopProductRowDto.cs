namespace MyThuatShop.Api.Dtos.AdminOverview;

public class OverviewTopProductRowDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? CategoryName { get; set; }
    public decimal Price { get; set; }
    public DateTime? CreateAt { get; set; }
    public int SoldQty { get; set; }
}
