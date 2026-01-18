using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Dtos.Admin;
using MyThuatShop.Filters;
using MyThuatShop.Services;

namespace MyThuatShop.Controllers;

[RequireAdmin]
public class AdminStatisticsController : Controller
{
    private readonly AdminStatisticsApiService _api;

    public AdminStatisticsController(AdminStatisticsApiService api)
    {
        _api = api;
    }

    [HttpGet("/admin/statistics")]
    public async Task<IActionResult> Index([FromQuery] int noSaleMonths = 1)
    {
        noSaleMonths = Math.Clamp(noSaleMonths, 1, 12);

        ViewData["Title"] = "Thống kê";
        ViewData["ActiveMenu"] = "statistics";

        var (data, err) = await _api.GetStatisticsAsync(noSaleMonths);

        if (!string.IsNullOrWhiteSpace(err))
            TempData["ErrorMsg"] = "Không tải được dữ liệu thống kê: " + err;

        data ??= new AdminStatisticsDto();
        data.NoSaleMonths = noSaleMonths;

        return View("~/Views/Admin/Statistic.cshtml", data);
    }
}
