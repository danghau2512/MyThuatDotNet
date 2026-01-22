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


    [HttpGet("/AddToCart")]
    public IActionResult Get([FromQuery] string? action, [FromQuery] int? productId)
    {
  
        if (!IsLoggedIn())
            return RedirectToAction("Login", "Account");

        if (action == "remove" && productId.HasValue)
        {
            var cart = GetOrCreateCart();
            cart.Remove(productId.Value);
            SaveCart(cart);
            return RedirectToAction("Index", "Cart");
        }

        return RedirectToAction("Index", "Cart");
    }


    [HttpPost("/AddToCart")]
    public async Task<IActionResult> Post(
    [FromQuery] string? action,
    [FromForm] int? productId,
    [FromForm] int? quantity)
    {
        if (!IsLoggedIn())
        {
            if (IsAjaxRequest())
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập" });

            return RedirectToAction("Login", "Account", new { returnUrl = Request.Headers.Referer.ToString() });
        }

        // action nào cũng cần productId
        if (!productId.HasValue || productId.Value <= 0)
        {
            if (IsAjaxRequest())
                return BadRequest(new { success = false, message = "Thiếu productId" });

            return RedirectToRefererOr("/home");
        }

        var pid = productId.Value;

        // ✅ XÓA không cần quantity
        if (action == "ajaxRemove")
            return AjaxRemove(pid);

        // các action còn lại mới cần quantity
        var qty = quantity ?? 1;
        if (qty < 1) qty = 1;

        if (action == "ajaxUpdate")
            return await AjaxUpdate(pid, qty);

        if (action == "update")
            return RedirectToAction("Index", "Cart");

        // default: add
        return await Add(pid, qty);
    }


    // ===== ADD 1 PRODUCT =====
    private async Task<IActionResult> Add(int productId, int quantity)
    {
        var p = await _productApi.GetProductForCart(productId);

        if (p == null || !p.IsActive)
        {
            if (IsAjaxRequest())
                return NotFound(new { success = false, message = "Sản phẩm đã ngừng bán" });

            return RedirectToRefererOr("/home");
        }

        if (p.QuantityStock <= 0)
        {
            if (IsAjaxRequest())
                return BadRequest(new { success = false, message = "Sản phẩm đã hết hàng" });

            return RedirectToRefererOr("/home");
        }

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

        return RedirectToRefererOr("/home");
    }

    //AJAX UPDATE
    private async Task<IActionResult> AjaxUpdate(int productId, int quantity)
    {
        var cart = GetOrCreateCart();

        if (quantity < 1) quantity = 1;

        var pCheck = await _productApi.GetProductForCart(productId);

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

        if (quantity > pCheck.QuantityStock) quantity = pCheck.QuantityStock;
        if (quantity < 1) quantity = 1;

        cart.UpdateQuantity(productId, quantity);

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

            if (item.Quantity > p.QuantityStock) item.Quantity = p.QuantityStock;
            if (item.Quantity < 1) item.Quantity = 1;

            item.Name = p.Name;
            item.Price = p.Price;
            item.DiscountDefault = p.DiscountDefault;
            item.Thumbnail = p.Thumbnail;

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
    //  AJAX REMOVE
    private IActionResult AjaxRemove(int productId)
    {
        var cart = GetOrCreateCart();

        // nếu không có sản phẩm thì vẫn trả về ok
        cart.Remove(productId);

        SaveCart(cart);

        // trả về JSON 
        return Json(new
        {
            success = true,
            cartCount = cart.TotalQuantity(),
            totalAmount = cart.TotalAmount()
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

    private bool IsLoggedIn()
    {
        var u = HttpContext.Session.GetObject<object>("currentUser");
        return u != null;
    }
}
