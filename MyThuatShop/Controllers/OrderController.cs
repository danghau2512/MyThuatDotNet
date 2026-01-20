using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Services;
using MyThuatShop.ViewModels.Order;

namespace MyThuatShop.Controllers;

public class OrderController : Controller
{
    private readonly OrderApiService _orderApi;
    private readonly IConfiguration _config;

    public OrderController(OrderApiService orderApi, IConfiguration config)
    {
        _orderApi = orderApi;
        _config = config;
    }

    [HttpGet("/order-history")]
    public async Task<IActionResult> History([FromQuery] string? status = "all")
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Account",
                new { returnUrl = Url.Action("History", "Order", new { status }) });
        }

        var s = (status ?? "all").Trim().ToLowerInvariant();
        if (s is not ("all" or "pending" or "shipping" or "completed" or "cancelled" or "canceled"))
            s = "all";
        if (s == "canceled") s = "cancelled";

        int? statusId = s switch
        {
            "pending" => 1,
            "shipping" => 2,
            "completed" => 3,
            "cancelled" => 4,
            _ => null
        };

        var orders = await _orderApi.GetByUserAsync(userId.Value, statusId) ?? new();

        ViewBag.ApiBaseUrl = (_config["ApiBaseUrl"] ?? "https://localhost:7090").TrimEnd('/');

        var vm = new OrderHistoryPageVm
        {
            FullName = HttpContext.Session.GetString("FullName") ?? "",
            CurrentStatus = s,
            Orders = orders
        };

        return View("~/Views/Order/OrderHistory.cshtml", vm);
    }

    [HttpGet("/order-detail")]
    public async Task<IActionResult> Detail([FromQuery] int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Detail", "Order", new { id }) });

        var order = await _orderApi.GetDetailAsync(id);
        if (order == null) return RedirectToAction(nameof(History));

        ViewBag.ApiBaseUrl = (_config["ApiBaseUrl"] ?? "https://localhost:7090").TrimEnd('/');

        return View("~/Views/Order/OrderDetail.cshtml", order);
    }
}
