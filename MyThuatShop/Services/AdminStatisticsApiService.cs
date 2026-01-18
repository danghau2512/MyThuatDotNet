using System.Net.Http.Json;
using System.Text.Json;
using MyThuatShop.Dtos.Admin;

namespace MyThuatShop.Services;

public class AdminStatisticsApiService
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AdminStatisticsApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<(AdminStatisticsDto? data, string? err)> GetStatisticsAsync(int noSaleMonths)
    {
        try
        {
            var resp = await _http.GetAsync($"/api/admin/statistics?noSaleMonths={noSaleMonths}");
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return (null, string.IsNullOrWhiteSpace(body) ? resp.ReasonPhrase : body);
            }

            var data = await resp.Content.ReadFromJsonAsync<AdminStatisticsDto>(_json);
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }
}
