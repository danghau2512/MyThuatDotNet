using System.Text.Json;

namespace MyThuatShop.Services
{
    public class GhnService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GhnService> _logger;

        public GhnService(IConfiguration config, HttpClient httpClient, ILogger<GhnService> logger)
        {
            _config = config;
            _httpClient = httpClient;
            _logger = logger;

            var baseUrl = _config["Ghn:BaseUrl"];
            var token = _config["Ghn:Token"];
            var shopId = _config["Ghn:ShopId"];

            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(token))
                throw new Exception("Cấu hình GHN chưa đầy đủ trong appsettings.json");

            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Clear(); // Xóa header cũ nếu có để tránh trùng
            _httpClient.DefaultRequestHeaders.Add("Token", token);
            _httpClient.DefaultRequestHeaders.Add("ShopId", shopId);
        }

        public async Task<JsonElement> GetProvinces()
        {
            return await GetApi("master-data/province");
        }

        public async Task<JsonElement> GetDistricts(int provinceId)
        {
            // Dùng tên biến có gạch dưới trực tiếp để GHN hiểu
            return await PostApi("master-data/district", new { province_id = provinceId });
        }

        public async Task<JsonElement> GetWards(int districtId)
        {
            return await PostApi("master-data/ward", new { district_id = districtId });
        }

        // Hàm tự động chọn gói dịch vụ (quan trọng để tránh lỗi Service không hỗ trợ)
        public async Task<int> GetAvailableService(int toDistrictId)
        {
            try
            {
                var request = new
                {
                    shop_id = int.Parse(_config["Ghn:ShopId"]),
                    from_district = int.Parse(_config["Ghn:FromDistrictId"]), // <-- đổi tên field
                    to_district = toDistrictId                                // <-- đổi tên field
                };

                var response = await _httpClient.PostAsJsonAsync("v2/shipping-order/available-services", request);
                var content = await ReadResponse(response);

                if (content.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                {
                    return data[0].GetProperty("service_id").GetInt32();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không lấy được gói dịch vụ tự động: " + ex.Message);
            }

            return 53320;
        }
        public async Task<decimal> CalculateFee(int toDistrictId, string toWardCode, int insuranceValue)
        {
            int serviceId = await GetAvailableService(toDistrictId);

            // --- QUAN TRỌNG: Dùng object có tên biến y hệt JSON của GHN ---
            var request = new
            {
                service_id = serviceId,
                insurance_value = insuranceValue,
                coupon = (string)null,
                from_district_id = int.Parse(_config["Ghn:FromDistrictId"]),
                from_ward_code = _config["Ghn:FromWardCode"],   // <-- thêm dòng này
                to_district_id = toDistrictId,
                to_ward_code = toWardCode,
                height = int.Parse(_config["Ghn:DefaultHeight"]),
                length = int.Parse(_config["Ghn:DefaultLength"]),
                width = int.Parse(_config["Ghn:DefaultWidth"]),
                weight = int.Parse(_config["Ghn:DefaultWeight"])
            };
            // -------------------------------------------------------------

            var response = await _httpClient.PostAsJsonAsync("v2/shipping-order/fee", request);
            var content = await ReadResponse(response);

            if (content.TryGetProperty("data", out var data))
            {
                return data.GetProperty("total").GetDecimal();
            }
            return 0;
        }

        // Hàm helper gọi GET
        private async Task<JsonElement> GetApi(string url)
        {
            var response = await _httpClient.GetAsync(url);
            return await ReadResponse(response);
        }

        // Hàm helper gọi POST
        private async Task<JsonElement> PostApi(string url, object payload)
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload);
            return await ReadResponse(response);
        }

        // Hàm đọc kết quả và xử lý lỗi chung
        private async Task<JsonElement> ReadResponse(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                string errorMsg = body;
                try
                {
                    var json = JsonSerializer.Deserialize<JsonElement>(body);
                    if (json.TryGetProperty("code_message_value", out var msg)) errorMsg = msg.GetString();
                    else if (json.TryGetProperty("message", out var msg2)) errorMsg = msg2.GetString();
                }
                catch { }
                throw new Exception($"Lỗi GHN: {errorMsg}");
            }
            return JsonSerializer.Deserialize<JsonElement>(body);
        }
    }
}