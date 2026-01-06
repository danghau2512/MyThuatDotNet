using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Services;

namespace MyThuatShop.Controllers
{
    public class ContactController : Controller
    {
        private readonly ContactApiService _contactApi;

        public ContactController(ContactApiService contactApi)
        {
            _contactApi = contactApi;
        }

        [HttpGet("/contact")]
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Redirect("/login");

            ViewBag.FullName = HttpContext.Session.GetString("FullName") ?? "";
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "";
            ViewBag.PhoneNumber = HttpContext.Session.GetString("PhoneNumber") ?? "";

            return View("~/Views/Contact/Index.cshtml");
        }

        [HttpPost("/contact")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([FromForm] string message)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Redirect("/login");

            var fullName = HttpContext.Session.GetString("FullName") ?? "";
            var email = HttpContext.Session.GetString("Email") ?? "";
            var phone = HttpContext.Session.GetString("PhoneNumber") ?? "";

            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMsg"] = "Vui lòng nhập nội dung!";
                return RedirectToAction(nameof(Index));
            }

            var (ok, err) = await _contactApi.CreateAsync(new ContactApiService.ContactCreateRequestDto
            {
                UserId = userId.Value,
                FullName = fullName,
                Email = email,
                PhoneNumber = phone,
                Message = message
            });

            TempData["SuccessMsg"] = ok ? "Gửi liên hệ thành công!" : null;
            TempData["ErrorMsg"] = ok ? null : ("Gửi liên hệ thất bại: " + err);

            return RedirectToAction(nameof(Index));
        }
    }
}
