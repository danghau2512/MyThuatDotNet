using System.Net.Http.Json;
using System.Text.Json;
using MyThuatShop.ViewModels;

namespace MyThuatShop.Services;

public class HomeApiService
{
    private readonly IHttpClientFactory _factory;
    private readonly IConfiguration _config;
    private readonly ILogger<HomeApiService> _logger;

    // Cấu hình JSON để deserialize camelCase từ API
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HomeApiService(IHttpClientFactory factory, IConfiguration config, ILogger<HomeApiService> logger)
    {
        _factory = factory;
        _config = config;
        _logger = logger;
    }

    public async Task<List<CategorySectionVm>> GetIndexSections(int takePerCategory = 5)
    {
        try
        {
            var baseUrl = _config["ApiBaseUrl"]?.TrimEnd('/');

            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogError("ApiBaseUrl is not configured in appsettings.json");
                return new List<CategorySectionVm>();
            }

            // Tạo HttpClientHandler để bỏ qua SSL certificate trong development
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var client = new HttpClient(handler);

            var url = $"{baseUrl}/api/home/index?takePerCategory={takePerCategory}";
            _logger.LogInformation("Calling API: {Url}", url);

            // Sử dụng JsonSerializerOptions để deserialize camelCase
            var data = await client.GetFromJsonAsync<List<CategorySectionVm>>(url, _jsonOptions);

            _logger.LogInformation("API returned {Count} categories", data?.Count ?? 0);

            // Log chi tiết để debug
            if (data != null)
            {
                foreach (var cat in data)
                {
                    _logger.LogInformation("Category: {Name}, Products: {Count}",
                        cat.CategoryName, cat.Products?.Count ?? 0);
                }
            }

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
