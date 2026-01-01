using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Helpers;
using MyThuatShop.Models;
using MyThuatShop.Services;
using System.Linq;

namespace MyThuatShop.Controllers;

public class AddToCartController : Controller
{
    private readonly ProductAPIService _productApi;

    public AddToCartController(ProductAPIService productApi)
    {
        _productApi = productApi;
    }

    // ===== GET: remove item (giống JSP: /AddToCart?action=remove&productId=...) =====
    [HttpGet("/AddToCart")]
    public IActionResult Get([FromQuery] string? action, [FromQuery] int? productId)
    {
        // (tùy bạn) bắt buộc login giống JSP
        if (!IsLoggedIn())
            return Redirect("/login");

        if (action == "remove" && productId.HasValue)
        {
            var cart = GetOrCreateCart();
            cart.Remove(productId.Value);
            SaveCart(cart);
            return RedirectToAction("Index", "Cart");
        }

        return RedirectToAction("Index", "Cart");
    }

    // ===== POST: add / update / ajaxUpdate =====
    [HttpPost("/AddToCart")]
    public async Task<IActionResult> Post([FromQuery] string? action, [FromForm] int? productId, [FromForm] int? quantity)
    {
        // (tùy bạn) bắt buộc login giống JSP
        if (!IsLoggedIn())
            return Redirect("/login");

        // validate
        if (!productId.HasValue || productId.Value <= 0 || !quantity.HasValue)
            return RedirectToRefererOr("/home");

        var pid = productId.Value;
        var qty = quantity.Value;
        if (qty < 1) qty = 1;

        if (action == "ajaxUpdate")
            return await AjaxUpdate(pid, qty);

        if (action == "update")
            return RedirectToAction("Index", "Cart"); // nếu sau này làm update list kiểu form

        // default: add
        return await Add(pid, qty);
    }

    // ===== ADD 1 PRODUCT (giống JSP: check active + hết hàng, rồi add, set cartCount) =====
    private async Task<IActionResult> Add(int productId, int quantity)
    {
        var p = await _productApi.GetProductForCart(productId);

        // sản phẩm không tồn tại / ngừng bán
        if (p == null || !p.IsActive)
        {
            if (IsAjaxRequest())
                return NotFound(new { success = false, message = "Sản phẩm đã ngừng bán" });

            return RedirectToRefererOr("/home");
        }

        // hết hàng
        if (p.QuantityStock <= 0)
        {
            if (IsAjaxRequest())
                return BadRequest(new { success = false, message = "Sản phẩm đã hết hàng" });

            return RedirectToRefererOr("/home");
        }

        // NOTE: JSP của bạn không clamp quantity theo stock khi Add,
        // chỉ clamp trong ajaxUpdate. Nếu bạn muốn clamp ở đây, mở comment dưới:
        // if (quantity > p.QuantityStock) quantity = p.QuantityStock;

        var cart = GetOrCreateCart();

        cart.Add(new CartItem
        {
            ProductId = p.Id,
            Name = p.Name,
            Price = p.Price,
            DiscountDefault = p.DiscountDefault,
            Thumbnail = p.Thumbnail,
            Quantity = quantity
        });

        SaveCart(cart);

        if (IsAjaxRequest())
            return Json(new { success = true, cartCount = cart.TotalQuantity() });

        return RedirectToRefererOr("/cart");
    }

    // ===== AJAX UPDATE (Y HỆT LOGIC JSP BẠN GỬI) =====
    private async Task<IActionResult> AjaxUpdate(int productId, int quantity)
    {
        var cart = GetOrCreateCart();

        if (quantity < 1) quantity = 1;

        // 1) Check sản phẩm đang update
        var pCheck = await _productApi.GetProductForCart(productId);

        // ngừng bán -> remove khỏi cart + 404
        if (pCheck == null || !pCheck.IsActive)
        {
            cart.Remove(productId);
            SaveCart(cart);

            return NotFound(new
            {
                success = false,
                message = "Sản phẩm đã ngừng bán",
                cartCount = cart.TotalQuantity()
            });
        }

        // hết hàng -> remove + 400
        if (pCheck.QuantityStock <= 0)
        {
            cart.Remove(productId);
            SaveCart(cart);

            return BadRequest(new
            {
                success = false,
                message = "Sản phẩm đã hết hàng",
                cartCount = cart.TotalQuantity()
            });
        }

        // clamp qty theo tồn kho (đúng JSP)
        if (quantity > pCheck.QuantityStock) quantity = pCheck.QuantityStock;
        if (quantity < 1) quantity = 1;

        cart.UpdateQuantity(productId, quantity);

        // 2) TÍNH LẠI total giống JSP:
        //    - Duyệt toàn bộ cart
        //    - Re-fetch từng product active
        //    - Nếu null/!active -> remove
        //    - Nếu hết hàng -> remove
        //    - Clamp qty theo stock
        //    - Cập nhật lại Name/Price/Discount/Thumbnail theo product mới nhất
        //    - Tính itemSubtotal & totalAmount theo giá mới nhất
        decimal totalAmount = 0m;
        decimal itemSubtotal = 0m;

        var toRemove = new List<int>();

        foreach (var kv in cart.Carts.ToList())
        {
            var pid = kv.Key;
            var item = kv.Value;

            var p = await _productApi.GetProductForCart(pid);

            if (p == null || !p.IsActive)
            {
                toRemove.Add(pid);
                continue;
            }

            if (p.QuantityStock <= 0)
            {
                toRemove.Add(pid);
                continue;
            }

            // clamp theo tồn kho
            if (item.Quantity > p.QuantityStock) item.Quantity = p.QuantityStock;
            if (item.Quantity < 1) item.Quantity = 1;

            // cập nhật lại thông tin item theo product mới nhất
            item.Name = p.Name;
            item.Price = p.Price;
            item.DiscountDefault = p.DiscountDefault;
            item.Thumbnail = p.Thumbnail;

            // tính tiền theo giá mới nhất
            var priceAfterDiscount = item.Price * (1m - (item.DiscountDefault / 100m));
            var sub = priceAfterDiscount * item.Quantity;

            totalAmount += sub;

            if (pid == productId)
                itemSubtotal = sub;
        }

        foreach (var rid in toRemove)
            cart.Remove(rid);

        SaveCart(cart);

        return Json(new
        {
            success = true,
            itemSubtotal,
            totalAmount,
            cartCount = cart.TotalQuantity()
        });
    }

    // ===== helpers =====
    private Cart GetOrCreateCart()
    {
        return HttpContext.Session.GetObject<Cart>("cart") ?? new Cart();
    }

    private void SaveCart(Cart cart)
    {
        HttpContext.Session.SetObject("cart", cart);
        HttpContext.Session.SetInt32("cartCount", cart.TotalQuantity());
    }

    private bool IsAjaxRequest()
    {
        var xrw = Request.Headers["X-Requested-With"].ToString();
        if (string.Equals(xrw, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            return true;

        var accept = Request.Headers.Accept.ToString();
        if (!string.IsNullOrEmpty(accept) && accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return true;

        var contentType = Request.ContentType ?? "";
        if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private IActionResult RedirectToRefererOr(string fallback)
    {
        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer))
            return Redirect(referer);

        return Redirect(fallback);
    }

    // Login check giống JSP nhưng không phụ thuộc class Users (để khỏi lỗi build)
    private bool IsLoggedIn()
    {
        // Nếu bạn đang lưu currentUser bằng SetObject, dùng GetObject<object>
        var u = HttpContext.Session.GetObject<object>("currentUser");
        return u != null;
    }
}
