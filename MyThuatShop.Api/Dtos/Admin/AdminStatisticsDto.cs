namespace MyThuatShop.Api.Dtos.Admin;

public class AdminStatisticsDto
{
    public int NoSaleMonths { get; set; }
    public decimal TotalYear { get; set; }

    public List<RevenueMonthDto> RevYear { get; set; } = new();
    public List<BestSellerRowDto> BestTable { get; set; } = new();
    public List<BestSellerChartPointDto> BestChart { get; set; } = new();
    public List<NoSaleRowDto> NoSaleTable { get; set; } = new();
}

public class RevenueMonthDto
{
    public int Month { get; set; }
    public decimal Revenue { get; set; }
}

public class BestSellerRowDto
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? CategoryName { get; set; }
    public decimal? Price { get; set; }
    public DateTime? CreateAt { get; set; }
    public int SoldQty { get; set; }
}

public class BestSellerChartPointDto
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int SoldQty { get; set; }
}

public class NoSaleRowDto
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? CategoryName { get; set; }
    public decimal Price { get; set; }
    public DateTime? CreateAt { get; set; }
    public int SoldQuantity { get; set; }
}
