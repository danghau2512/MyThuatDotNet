namespace MyThuatShop.ViewModels.Admin
{
    public class AdminOverviewDto
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        public List<AdminOverviewOrderRowDto> LatestOrders { get; set; } = new();
        public List<AdminOverviewTopProductRowDto> TopProductsMonth { get; set; } = new();
    }

    public class AdminOverviewOrderRowDto
    {
        public int Id { get; set; }
        public string? CustomerName { get; set; }
        public string? Email { get; set; }
        public string? StatusName { get; set; }
        public string? PaymentMethod { get; set; }

        public decimal TotalPrice { get; set; }
        public decimal Discount { get; set; }
        public DateTime? CreateAt { get; set; }
    }

    public class AdminOverviewTopProductRowDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string? Thumbnail { get; set; }

        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }
}
