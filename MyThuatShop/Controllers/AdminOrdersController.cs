using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Filters;
using MyThuatShop.Services;

namespace MyThuatShop.Controllers;

[RequireAdmin]
public class AdminOrdersController : Controller
{
    private readonly AdminOrdersApiService _api;

    public AdminOrdersController(AdminOrdersApiService api)
    {
        _api = api;
    }

    [HttpGet("/admin/orders")]
    public async Task<IActionResult> Index([FromQuery] string? status = null)
    {
        ViewData["Title"] = "Quản lý đơn hàng";
        ViewData["ActiveMenu"] = "orders";
        ViewData["Layout"] = "~/Views/Shared/_AdminLayout.cshtml";

        var (data, err) = await _api.GetOrdersAsync(status);

        if (!string.IsNullOrWhiteSpace(err))
            TempData["ErrorMsg"] = "Không tải được danh sách đơn hàng: " + err;

        return View("~/Views/Admin/Orders.cshtml", data ?? new());
    }

    [HttpPost("/admin/orders/update-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus([FromForm] int orderId, [FromForm] string statusName)
    {
        var (ok, err) = await _api.UpdateStatusAsync(orderId, statusName);

        if (!ok)
            TempData["ErrorMsg"] = "Cập nhật trạng thái thất bại: " + (err ?? "Unknown error");
        else
            TempData["SuccessMsg"] = "Cập nhật trạng thái thành công!";

        return RedirectToAction(nameof(Index));
    }
}
