using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Services;

namespace MyThuatShop.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProductAPIService _productApi;

        public ProductController(ProductAPIService productApi)
        {
            _productApi = productApi;
        }

        // /Product/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var model = await _productApi.GetProductDetail(id);
            if (model == null) return NotFound();

            ViewData["Title"] = model.Name;
            return View("ProductDetail", model);
        }
    }
}
