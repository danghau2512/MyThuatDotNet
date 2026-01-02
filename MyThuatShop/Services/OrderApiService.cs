using System.Net.Http.Json;
using System.Text.Json;

namespace MyThuatShop.Services;

public class OrderApiService
{
    private readonly IConfiguration _config;
    private readonly ILogger<OrderApiService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OrderApiService(IConfiguration config, ILogger<OrderApiService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<(bool ok, int? orderId, string? message)> CreateAsync(object payload)
    {
        var baseUrl = _config["ApiBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) return (false, null, "Thiếu ApiBaseUrl.");

        using var client = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        var url = $"{baseUrl}/api/orders";
        var resp = await client.PostAsJsonAsync(url, payload, _jsonOptions);

        if (!resp.IsSuccessStatusCode)
        {
            var msg = await resp.Content.ReadAsStringAsync();
            return (false, null, string.IsNullOrWhiteSpace(msg) ? "Đặt hàng thất bại." : msg);
        }

        var data = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        if (data.TryGetProperty("orderId", out var oid))
            return (true, oid.GetInt32(), null);

        return (false, null, "API không trả orderId.");
    }

    public async Task<JsonElement?> GetAsync(int orderId)
    {
        var baseUrl = _config["ApiBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) return null;

        using var client = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        var url = $"{baseUrl}/api/orders/{orderId}";
        return await client.GetFromJsonAsync<JsonElement>(url, _jsonOptions);
    }
}
