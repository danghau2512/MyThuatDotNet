using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Services;

namespace MyThuatShop.Controllers
{
    [Route("admin/categories")]
    public class AdminCategoryController : Controller
    {
        private readonly AdminCategoryApiService _api;

        public AdminCategoryController(AdminCategoryApiService api)
        {
            _api = api;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var categories = await _api.GetAllAsync();
            return View("~/Views/Admin/Categories.cshtml", categories);
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post(
            [FromForm] string action,
            [FromForm] int id,
            [FromForm] int isActive,
            [FromForm] string categoryName,
            [FromForm] int removeThumbnail,   // ✅ NEW: nhận cờ xóa ảnh
            IFormFile? thumbnail)
        {
            bool ok = true;
            action = (action ?? "").Trim();

            if (action == "create")
            {
                ok = await _api.CreateAsync(categoryName, thumbnail);
            }
            else if (action == "update")
            {
                ok = await _api.UpdateAsync(id, categoryName, thumbnail, removeThumbnail == 1);
            }
            else if (action == "toggleActive")
            {
                ok = await _api.ToggleActiveAsync(id, isActive); // gửi current
            }

            if (!ok) TempData["ErrorMsg"] = "Gọi API thất bại. Kiểm tra API có chạy và đúng route.";

            return RedirectToAction(nameof(Index));
        }
    }
}
