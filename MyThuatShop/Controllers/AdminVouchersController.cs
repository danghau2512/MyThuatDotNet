using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Filters;
using MyThuatShop.Services;

namespace MyThuatShop.Controllers;

[RequireAdmin]
[Route("admin/vouchers")]
public class AdminVouchersController : Controller
{
    private readonly AdminVoucherApiService _api;
    public AdminVouchersController(AdminVoucherApiService api) => _api = api;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? keyword, int page = 1, int pageSize = 10)
    {
        ViewData["Title"] = "Quản lý khuyến mãi";
        ViewData["ActiveMenu"] = "vouchers";
        

        var data = await _api.GetAsync(keyword, page, pageSize);

        ViewBag.Keyword = keyword ?? "";
        ViewBag.Page = data?.Page ?? page;
        ViewBag.PageSize = data?.PageSize ?? pageSize;
        ViewBag.TotalPages = data?.TotalPages ?? 1;

        return View("~/Views/Admin/Vouchers.cshtml", data ?? new AdminVoucherApiService.PagedResultVm<AdminVoucherApiService.AdminVoucherRowVm>());
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(
        [FromForm] string action,
        [FromForm] int id,

        [FromForm] string code,
        [FromForm] string? name,
        [FromForm] string? description,

        [FromForm] DateTime startDate,
        [FromForm] DateTime endDate,

        [FromForm] decimal voucherCash,
        [FromForm] decimal minOrderValue,

        [FromForm] int quantity,
        [FromForm] int quantityUsed,
        [FromForm] int isActive,

        [FromForm] string? keyword,
        [FromForm] int page = 1,
        [FromForm] int pageSize = 10
    )
    {
        var form = new Dictionary<string, string>
        {
            ["action"] = action ?? "",
            ["id"] = id.ToString(),

            ["code"] = code ?? "",
            ["name"] = name ?? "",
            ["description"] = description ?? "",

            ["startDate"] = startDate.ToString("yyyy-MM-dd"),
            ["endDate"] = endDate.ToString("yyyy-MM-dd"),

            ["voucherCash"] = voucherCash.ToString(),
            ["minOrderValue"] = minOrderValue.ToString(),

            ["quantity"] = quantity.ToString(),
            ["quantityUsed"] = quantityUsed.ToString(),
            ["isActive"] = isActive.ToString()
        };

        var (ok, msg) = await _api.PostAsync(form);

        TempData[ok ? "SuccessMsg" : "ErrorMsg"] =
            ok ? "Thao tác thành công!" : ("Thao tác thất bại: " + msg);

        return RedirectToAction(nameof(Index), new { keyword, page, pageSize });
    }
    [HttpGet("ajax")]
    public async Task<IActionResult> Ajax(string? keyword, int page = 1, int pageSize = 10)
    {
        var data = await _api.GetAsync(keyword, page, pageSize)
                   ?? new AdminVoucherApiService.PagedResultVm<AdminVoucherApiService.AdminVoucherRowVm>();

        var totalPages = data.TotalPages;
        if (totalPages <= 0) totalPages = 1;

        return Json(new
        {
            keyword = keyword ?? "",
            page = data.Page,
            pageSize = data.PageSize,
            totalItems = data.TotalItems,
            totalPages,
            items = data.Items.Select(v => new
            {
                id = v.Id,
                code = v.Code ?? "",
                name = v.Name ?? "",
                description = v.Description ?? "",
                startDate = v.StartDate.ToString("yyyy-MM-dd"),
                endDate = v.EndDate.ToString("yyyy-MM-dd"),
                voucherCash = v.VoucherCash,
                minOrderValue = v.MinOrderValue,
                quantity = v.Quantity,
                quantityUsed = v.QuantityUsed,
                isActive = v.IsActive // 1/0
            })
        });
    }

}
