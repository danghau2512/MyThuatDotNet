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

        // Split the complex statement to avoid ENC0046
        var result = await _api.GetOrdersAsync(status);
        var data = result.data;
        var err = result.err;

        if (!string.IsNullOrWhiteSpace(err))
            TempData["ErrorMsg"] = "Không tải được danh sách đơn hàng: " + err;

        return View("~/Views/Admin/Orders.cshtml", data ?? new());
    }

    [HttpPost("/admin/orders/update-info")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateInfo([FromForm] int orderId,
    [FromForm] string fullName, [FromForm] string phoneNumber, [FromForm] string address)
    {
        var result = await _api.UpdateInfoAsync(orderId, fullName, phoneNumber, address);
        var ok = result.ok;
        var err = result.err;
        TempData[ok ? "SuccessMsg" : "ErrorMsg"] = ok ? "Cập nhật thông tin thành công!" : ("Cập nhật thất bại: " + err);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/orders/change-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus([FromForm] int orderId, [FromForm] int statusId)
    {
        var result = await _api.ChangeStatusAsync(orderId, statusId);
        var ok = result.ok;
        var err = result.err;
        TempData[ok ? "SuccessMsg" : "ErrorMsg"] = ok ? "Đổi trạng thái thành công!" : ("Đổi trạng thái thất bại: " + err);
        return RedirectToAction(nameof(Index));
    }

}
