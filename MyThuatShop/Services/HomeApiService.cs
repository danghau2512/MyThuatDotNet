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

    // ✅ đổi đúng domain API của bạn
    private const string ApiBaseUrl = "https://localhost:7090";

    public HomeApiService(HttpClient http, ILogger<HomeApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    // ✅ helper: /uploads/... -> https://localhost:7090/uploads/...
    private static string? ToAbsoluteImageUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;

        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return path;

        if (!path.StartsWith("/")) path = "/" + path;

        return ApiBaseUrl.TrimEnd('/') + path;
    }

    private static void NormalizeSectionImages(List<CategorySectionVm> sections)
    {
        foreach (var sec in sections)
        {
            sec.Thumbnail = ToAbsoluteImageUrl(sec.Thumbnail);

            if (sec.Products != null && sec.Products.Count > 0)
            {
                foreach (var p in sec.Products)
                {
                    p.Thumbnail = ToAbsoluteImageUrl(p.Thumbnail);
                }
            }
        }
    }

    public async Task<List<CategorySectionVm>> GetIndexSections(int takePerCategory = 5)
    {
        try
        {
            var url = $"/api/home/index?takePerCategory={takePerCategory}";
            _logger.LogInformation("Calling API: {Url}", url);

            var data = await _http.GetFromJsonAsync<List<CategorySectionVm>>(url, _jsonOptions)
                       ?? new List<CategorySectionVm>();

           
            NormalizeSectionImages(data);

            _logger.LogInformation("API returned {Count} categories", data.Count);
            return data;
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
