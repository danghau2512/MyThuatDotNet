using System.Text.Json;
using System.Net.Http.Json;
using MyThuatShop.Dtos.Admin;

namespace MyThuatShop.Services;

public class AdminOverviewApiService
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AdminOverviewApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<(AdminOverviewDto? data, string? err)> GetOverviewAsync()
    {
        try
        {
            var resp = await _http.GetAsync("/api/admin/overview");
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return (null, string.IsNullOrWhiteSpace(body) ? resp.ReasonPhrase : body);
            }

            var data = await resp.Content.ReadFromJsonAsync<AdminOverviewDto>(_json);
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }
}
