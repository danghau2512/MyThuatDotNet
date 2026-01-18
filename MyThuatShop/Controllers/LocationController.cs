using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Dtos;
using MyThuatShop.Services;

namespace MyThuatShop.Controllers
{
    [Route("api/location")]
    public class LocationController : Controller
    {
        private readonly GhnService _ghnService;
        private readonly ILogger<LocationController> _logger;
        private readonly IConfiguration _config;

        public LocationController(GhnService ghnService, ILogger<LocationController> logger, IConfiguration config)
        {
            _ghnService = ghnService;
            _logger = logger;
            _config = config;
        }

        [HttpGet("test-config")]
        public IActionResult TestConfig()
        {
            var config = new
            {
                BaseUrl = _config["Ghn:BaseUrl"],
                Token = _config["Ghn:Token"]?.Substring(0, 10) + "...", // Chỉ hiện 10 ký tự đầu
                ShopId = _config["Ghn:ShopId"],
                FromDistrictId = _config["Ghn:FromDistrictId"]
            };
            return Ok(config);
        }

        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            try
            {
                _logger.LogInformation("Getting provinces from GHN");
                var result = await _ghnService.GetProvinces();
                _logger.LogInformation("Successfully got provinces from GHN");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provinces from GHN");
                return StatusCode(500, new { 
                    success = false, 
                    message = "Không thể tải danh sách tỉnh/thành phố",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("districts/{provinceId}")]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            try
            {
                var result = await _ghnService.GetDistricts(provinceId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting districts from GHN for province {ProvinceId}", provinceId);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Không thể tải danh sách quận/huyện",
                    error = ex.Message 
                });
            }
        }

        [HttpGet("wards/{districtId}")]
        public async Task<IActionResult> GetWards(int districtId)
        {
            try
            {
                var result = await _ghnService.GetWards(districtId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wards from GHN for district {DistrictId}", districtId);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Không thể tải danh sách phường/xã",
                    error = ex.Message 
                });
            }
        }

        [HttpPost("get-fee")]
        public async Task<IActionResult> GetFee([FromBody] ShippingFeeRequest req)
        {
            try
            {
                var fee = await _ghnService.CalculateFee(req.DistrictId, req.WardCode, req.InsuranceValue);
                return Ok(new { fee = fee });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shipping fee from GHN");
                return StatusCode(500, new { 
                    success = false, 
                    message = "Không thể tính phí vận chuyển", 
                    fee = 0,
                    error = ex.Message 
                });
            }
        }
    }
}
