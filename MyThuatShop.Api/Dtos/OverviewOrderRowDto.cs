namespace MyThuatShop.Api.Dtos.AdminOverview;

public class OverviewOrderRowDto
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? CreateAt { get; set; }
    public string? Address { get; set; }
    public string? ProductNames { get; set; }
    public decimal TotalPrice { get; set; }
    public string? StatusName { get; set; }
}
