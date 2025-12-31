using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyThuatShop.ViewModels;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyThuatShop.Services
{
    public class ProductAPIService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ProductAPIService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // ✅ constructor phải trùng tên class
        public ProductAPIService(IConfiguration config, ILogger<ProductAPIService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<ProductDetailVm?> GetProductDetail(int id)
        {
            var baseUrl = _config["ApiBaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogError("ApiBaseUrl is not configured in appsettings.json");
                return null;
            }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var client = new HttpClient(handler);

            var url = $"{baseUrl}/api/products/detail/{id}";
            _logger.LogInformation("Calling API: {Url}", url);

            try
            {
                return await client.GetFromJsonAsync<ProductDetailVm>(url, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed when calling API");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling API");
                return null;
            }

        }
        public async Task<bool> AddReview(int productId, int userId, int rating, string? comment)
        {
            var baseUrl = _config["ApiBaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogError("ApiBaseUrl is not configured in appsettings.json");
                return false;
            }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var client = new HttpClient(handler);

            var url = $"{baseUrl}/api/products/{productId}/reviews";

            try
            {
                var payload = new { userId, rating, comment };
                var resp = await client.PostAsJsonAsync(url, payload, _jsonOptions);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling API");
                return false;
            }
        }
    }
}
