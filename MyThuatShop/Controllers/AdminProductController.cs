using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Services;

namespace MyThuatShop.Controllers
{
    [Route("admin/products")]
    public class AdminProductController : Controller
    {
        private readonly AdminProductApiService _api;
        private readonly AdminCategoryApiService _catApi;

        public AdminProductController(AdminProductApiService api, AdminCategoryApiService catApi)
        {
            _api = api;
            _catApi = catApi;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var products = await _api.GetAllAsync();
            var categories = await _catApi.GetAllAsync();

            ViewBag.Categories = categories;
            return View("~/Views/Admin/Products.cshtml", products);
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post(
            [FromForm] string action,

            // toggle
            [FromForm] int id,
            [FromForm] bool isActive,

            // product fields
            [FromForm] int categoryId,
            [FromForm] string name,
            [FromForm] decimal price,
            [FromForm] int discountDefault,
            [FromForm] int quantityStock,
            [FromForm] string? brand,
            [FromForm] string? content,


            // spec
            [FromForm] string? size,
            [FromForm] string? standard,
            [FromForm] string? madeIn,
            [FromForm] string? warning,

            // remove thumb like categories
            [FromForm] int removeThumbnail,
            [FromForm] int newIsActive,
            // files
            IFormFile? thumbnailMain,
            List<IFormFile>? thumbnailSubs
        )
        {
            bool ok = true;
            action = (action ?? "").Trim();

            if (action == "create")
            {
                ok = await _api.CreateAsync(new ProductUpsertVm
                {
                    CategoryId = categoryId,
                    Name = name,
                    Price = price,
                    DiscountDefault = discountDefault,
                    QuantityStock = quantityStock,
                    Brand = brand,
                    Size = size,
                    Standard = standard,
                    MadeIn = madeIn,
                    Warning = warning,
                    Content = content
                }, thumbnailMain, thumbnailSubs);
            }
            else if (action == "update")
            {
                ok = await _api.UpdateAsync(new ProductUpsertVm
                {
                    Id = id,
                    CategoryId = categoryId,
                    Name = name,
                    Price = price,
                    DiscountDefault = discountDefault,
                    QuantityStock = quantityStock,
                    Brand = brand,
                    Size = size,
                    Standard = standard,
                    MadeIn = madeIn,
                    Warning = warning,
                    Content = content,
                    RemoveThumbnail = removeThumbnail == 1
                }, thumbnailMain, thumbnailSubs);
            }
            else if (action == "toggleActive")
            {
                ok = await _api.SetActiveAsync(id, newIsActive == 1);
            }


            if (!ok) TempData["ErrorMsg"] = "Gọi API thất bại. Kiểm tra API có chạy và đúng route.";
            return RedirectToAction(nameof(Index));
        }
    }
}
