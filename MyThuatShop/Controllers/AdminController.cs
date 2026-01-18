using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Dtos.Admin;
using MyThuatShop.Filters;
using MyThuatShop.Services;

namespace MyThuatShop.Controllers;

[RequireAdmin]
public class AdminController : Controller
{
    private readonly AdminOverviewApiService _api;

    public AdminController(AdminOverviewApiService api)
    {
        _api = api;
    }

    [HttpGet("/admin")]
    public IActionResult Root() => Redirect("/admin/overview");

    [HttpGet("/admin/overview")]
    public async Task<IActionResult> Overview()
    {
        ViewData["Title"] = "Tổng quan";
        ViewData["ActiveMenu"] = "overview";

        var (data, err) = await _api.GetOverviewAsync();

        if (!string.IsNullOrWhiteSpace(err))
            TempData["ErrorMsg"] = "Không tải được dữ liệu tổng quan: " + err;

        return View("~/Views/Admin/Overview.cshtml", data ?? new AdminOverviewDto());
    }
}
