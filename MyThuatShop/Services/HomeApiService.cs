using System.Net.Http.Json;
using System.Text.Json;
using MyThuatShop.ViewModels;

namespace MyThuatShop.Services;

public class HomeApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<HomeApiService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ✅ Typed client: nhận HttpClient
    public HomeApiService(HttpClient http, ILogger<HomeApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<CategorySectionVm>> GetIndexSections(int takePerCategory = 5)
    {
        try
        {
            var url = $"/api/home/index?takePerCategory={takePerCategory}";
            _logger.LogInformation("Calling API: {Url}", url);

            var data = await _http.GetFromJsonAsync<List<CategorySectionVm>>(url, _jsonOptions);

            _logger.LogInformation("API returned {Count} categories", data?.Count ?? 0);
            return data ?? new List<CategorySectionVm>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when calling API");
            return new List<CategorySectionVm>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling API");
            return new List<CategorySectionVm>();
        }
    }
}
