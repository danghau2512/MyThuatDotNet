using System.Net.Http.Json;

namespace MyThuatShop.Services;

public class AdminVoucherApiService
{
    private readonly HttpClient _http;
    public AdminVoucherApiService(HttpClient http) => _http = http;

    public class PagedResultVm<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<T> Items { get; set; } = new();
    }

    public class AdminVoucherRowVm
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal VoucherCash { get; set; }
        public decimal MinOrderValue { get; set; }
        public int Quantity { get; set; }
        public int QuantityUsed { get; set; }
        public int IsActive { get; set; } // 1/0
    }

    public async Task<PagedResultVm<AdminVoucherRowVm>?> GetAsync(string? keyword, int page, int pageSize)
    {
        var url = $"api/admin/vouchers?keyword={Uri.EscapeDataString(keyword ?? "")}&page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<PagedResultVm<AdminVoucherRowVm>>(url);
    }

    public async Task<(bool ok, string msg)> PostAsync(Dictionary<string, string> form)
    {
        var res = await _http.PostAsync("api/admin/vouchers", new FormUrlEncodedContent(form));
        var msg = await res.Content.ReadAsStringAsync();
        return (res.IsSuccessStatusCode, string.IsNullOrWhiteSpace(msg) ? "OK" : msg);
    }
}
