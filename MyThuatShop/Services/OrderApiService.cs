using System.Net.Http.Json;
using System.Text.Json;
using MyThuatShop.Dtos.Orders;


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
    public async Task<bool> ConfirmPaymentAsync(int orderId)
    {
        var baseUrl = _config["ApiBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) return false;

        using var client = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        var url = $"{baseUrl}/api/orders/confirm-payment";
        var resp = await client.PostAsJsonAsync(url, orderId);

        return resp.IsSuccessStatusCode;
    }
    public async Task<(bool success, decimal discount, int? voucherId, string message)> CheckVoucherAsync(string code, decimal total)
    {
        var baseUrl = _config["ApiBaseUrl"]?.TrimEnd('/');
        using var client = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator });

        var url = $"{baseUrl}/api/vouchers/check?code={code}&orderTotal={total}";
        var response = await client.GetAsync(url);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        if (response.IsSuccessStatusCode)
        {
            return (true, content.GetProperty("discount").GetDecimal(), content.GetProperty("voucherId").GetInt32(), "Áp dụng thành công");
        }
        else
        {
            // Lấy message lỗi từ API
            string msg = "Mã không hợp lệ";
            if (content.TryGetProperty("message", out var m)) msg = m.GetString() ?? msg;
            return (false, 0, null, msg);
        }
    }
    public async Task<List<OrderHistoryOrderDto>?> GetByUserAsync(int userId, int? statusId = null)
    {
        var baseUrl = _config["ApiBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) return null;

        using var client = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        var url = $"{baseUrl}/api/orders/user/{userId}";
        if (statusId.HasValue && statusId.Value > 0)
            url += $"?statusId={statusId.Value}";

        return await client.GetFromJsonAsync<List<OrderHistoryOrderDto>>(url, _jsonOptions);
    }
    public async Task<OrderDetailDto?> GetDetailAsync(int orderId)
    {
        var baseUrl = _config["ApiBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) return null;

        using var client = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        var url = $"{baseUrl}/api/orders/{orderId}";
        return await client.GetFromJsonAsync<OrderDetailDto>(url, _jsonOptions);
    }


}
