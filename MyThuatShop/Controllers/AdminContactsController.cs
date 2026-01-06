using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Services;

namespace MyThuatShop.Controllers
{
    public class AdminContactsController : Controller
    {
        private readonly ContactApiService _contactApi;

        public AdminContactsController(ContactApiService contactApi)
        {
            _contactApi = contactApi;
        }

        private bool IsAdmin()
        {
            return (HttpContext.Session.GetString("Role") ?? "") == "Admin";
        }

        [HttpGet("/admin/contacts")]
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin()) return Redirect("/login");

            var (items, err) = await _contactApi.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(err))
            {
                TempData["ErrorMsg"] = "Không tải được danh sách liên hệ: " + err;
            }

            return View("~/Views/AdminContact/Index.cshtml", items);
        }

        [HttpPost("/admin/contacts/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] int id)
        {
            if (!IsAdmin()) return Redirect("/login");

            var (ok, err) = await _contactApi.DeleteAsync(id);

            TempData["SuccessMsg"] = ok ? "Xóa liên hệ thành công!" : null;
            TempData["ErrorMsg"] = ok ? null : ("Xóa liên hệ thất bại: " + err);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/admin/contacts/reply")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply([FromForm] int id, [FromForm] string subject, [FromForm] string replyMessage)
        {
            if (!IsAdmin()) return Redirect("/login");

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(replyMessage))
            {
                TempData["ErrorMsg"] = "Vui lòng nhập tiêu đề và nội dung phản hồi!";
                return RedirectToAction(nameof(Index));
            }

            var (ok, err) = await _contactApi.ReplyAsync(id, subject, replyMessage);

            TempData["SuccessMsg"] = ok ? "Phản hồi đã được gửi!" : null;
            TempData["ErrorMsg"] = ok ? null : ("Gửi phản hồi thất bại: " + err);

            return RedirectToAction(nameof(Index));
        }
    }
}
