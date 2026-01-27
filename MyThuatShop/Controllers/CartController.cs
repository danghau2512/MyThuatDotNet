using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Helpers;
using MyThuatShop.Models;
using MyThuatShop.ViewModels;

namespace MyThuatShop.Controllers;

public class CartController : Controller
{
    [HttpGet("/cart")]
    public IActionResult Index()
    {
        var currentUser = HttpContext.Session.GetObject<object>("currentUser");
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        var cart = HttpContext.Session.GetObject<Cart>("cart");
        if (cart == null)
        {
            cart = new Cart();
            HttpContext.Session.SetObject("cart", cart);
        }

        var vm = new CartPageVm
        {
            CartItems = cart.Carts.Values,
            TotalQuantity = cart.TotalQuantity(),
            TotalAmount = cart.TotalAmount(),
            CartSize = cart.CartSize()
        };

        HttpContext.Session.SetInt32("cartCount", vm.TotalQuantity);

        return View("~/Views/Cart/Cart.cshtml", vm);
    }
}
