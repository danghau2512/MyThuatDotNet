using System.Net.Http.Json;

namespace MyThuatShop.Services;

public class AdminOverviewApiService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public AdminOverviewApiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    private string BaseUrl =>
        (_config["ApiBaseUrl"] ?? "https://localhost:7090").TrimEnd('/');

    public async Task<AdminOverviewDto?> GetAsync(int latest = 10, int top = 10)
    {
        var url = $"{BaseUrl}/api/admin/overview?latest={latest}&top={top}";
        return await _http.GetFromJsonAsync<AdminOverviewDto>(url);
    }

    // DTO bên MVC (copy giống API)
    public class AdminOverviewDto
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<OverviewOrderRowDto> LatestOrders { get; set; } = new();
        public List<OverviewTopProductRowDto> TopProductsMonth { get; set; } = new();
    }

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

    public class OverviewTopProductRowDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? CategoryName { get; set; }
        public decimal Price { get; set; }
        public DateTime? CreateAt { get; set; }
        public int SoldQty { get; set; }
    }
}
