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

    private static bool IsAjax(HttpRequest req)
    => req.Headers["X-Requested-With"] == "XMLHttpRequest";

    private static string StatusNameFromId(int id) => id switch
    {
        1 => "Đang xử lý",
        2 => "Đang vận chuyển",
        3 => "Hoàn thành",
        4 => "Đã hủy",
        _ => "Đang xử lý"
    };

    [HttpPost("/admin/orders/update-info")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateInfo([FromForm] int orderId,
        [FromForm] string fullName, [FromForm] string phoneNumber, [FromForm] string address)
    {
        var (ok, err) = await _api.UpdateInfoAsync(orderId, fullName, phoneNumber, address);

        if (IsAjax(Request))
        {
            return Json(new
            {
                ok,
                err,
                orderId,
                fullName,
                phoneNumber,
                address
            });
        }

        TempData[ok ? "SuccessMsg" : "ErrorMsg"] = ok ? "Cập nhật thông tin thành công!" : ("Cập nhật thất bại: " + err);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/orders/change-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus([FromForm] int orderId, [FromForm] int statusId)
    {
        var (ok, err) = await _api.ChangeStatusAsync(orderId, statusId);

        if (IsAjax(Request))
        {
            return Json(new
            {
                ok,
                err,
                orderId,
                statusId,
                statusName = StatusNameFromId(statusId)
            });
        }

        TempData[ok ? "SuccessMsg" : "ErrorMsg"] = ok ? "Đổi trạng thái thành công!" : ("Đổi trạng thái thất bại: " + err);
        return RedirectToAction(nameof(Index));
    }

}
