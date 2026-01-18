using System.Net.Http.Json;
using System.Text.Json;
using MyThuatShop.Dtos.Admin;

namespace MyThuatShop.Services;

public class AdminOrdersApiService
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AdminOrdersApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<(List<AdminOrderRowDto>? data, string? err)> GetOrdersAsync(string? status = null)
    {
        try
        {
            var url = "/api/admin/orders";
            if (!string.IsNullOrWhiteSpace(status))
                url += "?status=" + Uri.EscapeDataString(status.Trim());

            var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return (null, string.IsNullOrWhiteSpace(body) ? resp.ReasonPhrase : body);
            }

            var data = await resp.Content.ReadFromJsonAsync<List<AdminOrderRowDto>>(_json);
            return (data ?? new(), null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public record UpdateOrderStatusRequest(int OrderId, string StatusName);

    public async Task<(bool ok, string? err)> UpdateStatusAsync(int orderId, string statusName)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync(
                "/api/admin/orders/status",
                new UpdateOrderStatusRequest(orderId, statusName ?? "")
            );

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(body) ? resp.ReasonPhrase : body);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
