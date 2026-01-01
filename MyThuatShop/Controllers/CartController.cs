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
        // (nếu bạn muốn bắt login giống JSP thì mở comment)
        // var currentUser = HttpContext.Session.GetObject<object>("currentUser");
        // if (currentUser == null) return Redirect("/login");

        var cart = HttpContext.Session.GetObject<Cart>("cart");
        if (cart == null)
        {
            cart = new Cart();
            HttpContext.Session.SetObject("cart", cart);
        }

        // ✅ giống JSP: totalQuantity + totalAmount + cartSize
        var vm = new CartPageVm
        {
            CartItems = cart.Carts.Values,
            TotalQuantity = cart.TotalQuantity(),
            TotalAmount = cart.TotalAmount(),
            CartSize = cart.CartSize()
        };

        // ✅ giống JSP: lưu cartCount để badge header hiển thị
        HttpContext.Session.SetInt32("cartCount", vm.TotalQuantity);

        return View(vm);
    }
}
