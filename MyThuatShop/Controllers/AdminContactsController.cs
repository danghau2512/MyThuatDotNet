using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Filters;
using MyThuatShop.Services;

namespace MyThuatShop.Controllers;

[RequireAdmin]
public class AdminContactsController : Controller
{
    private readonly AdminContactsApiService _api;
    public AdminContactsController(AdminContactsApiService api) => _api = api;

    private static bool IsAjax(HttpRequest req) => req.Headers["X-Requested-With"] == "XMLHttpRequest";

    [HttpGet("/admin/contacts")]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Quản lý liên hệ";
        ViewData["ActiveMenu"] = "contacts";
        var (data, err) = await _api.GetAllAsync();
        if (!string.IsNullOrWhiteSpace(err)) TempData["ErrorMsg"] = "Không tải được liên hệ: " + err;
        return View("~/Views/Admin/Contacts.cshtml", data ?? new());
    }

    [HttpPost("/admin/contacts/reply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply([FromForm] int contactId, [FromForm] string subject, [FromForm] string replyMessage)
    {
        var (ok, err) = await _api.ReplyAsync(contactId, subject, replyMessage);

        if (IsAjax(Request))
            return Json(new { ok, err, contactId, status = ok ? "Đã phản hồi" : null });

        TempData[ok ? "SuccessMsg" : "ErrorMsg"] = ok ? "Đã gửi phản hồi!" : ("Gửi thất bại: " + err);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/contacts/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromForm] int contactId)
    {
        var (ok, err) = await _api.DeleteAsync(contactId);

        if (IsAjax(Request))
            return Json(new { ok, err, contactId });

        TempData[ok ? "SuccessMsg" : "ErrorMsg"] = ok ? "Đã xóa liên hệ!" : ("Xóa thất bại: " + err);
        return RedirectToAction(nameof(Index));
    }
}
