using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MyThuatShop.Services;

namespace MyThuatShop.Controllers;

public class ProductController : Controller
{
    private readonly ProductAPIService _productApi;

    public ProductController(ProductAPIService productApi)
    {
        _productApi = productApi;
    }

    public async Task<IActionResult> Detail(int id)
    {
        var model = await _productApi.GetProductDetail(id);
        if (model == null) return NotFound();

        ViewData["Title"] = model.Name;
        return View("ProductDetail", model);
    }

    [HttpGet]
    public async Task<IActionResult> Reviews(int id)
    {
        var model = await _productApi.GetProductDetail(id);
        if (model == null) return NotFound();

        ViewData["Title"] = $"Đánh giá - {model.Name}";
        return View("ProductReviews", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reviews(int id, int rating, string? comment)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Account", new
            {
                returnUrl = Url.Action("Reviews", "Product", new { id })
            });
        }

        if (rating < 1 || rating > 5) rating = 5;
        comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

        await _productApi.AddReview(id, userId.Value, rating, comment);

        return RedirectToAction(nameof(Reviews), new { id });
    }
}
