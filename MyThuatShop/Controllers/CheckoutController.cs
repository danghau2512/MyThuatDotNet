using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Helpers;
using MyThuatShop.Models;
using MyThuatShop.Services;
using MyThuatShop.ViewModels;
using MyThuatShop.ViewModels.Checkout;

namespace MyThuatShop.Controllers;

public class CheckoutController : Controller
{
    private readonly OrderApiService _orderApi;

    public CheckoutController(OrderApiService orderApi) => _orderApi = orderApi;

    [HttpGet("/checkout")]
    public IActionResult Index()
    {
        if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

        var cart = HttpContext.Session.GetObject<Cart>("cart");
        if (cart == null || cart.Carts.Count == 0) return RedirectToAction("Index", "Cart");

        var fullName = HttpContext.Session.GetString("FullName") ?? "";
        var currentUser = HttpContext.Session.GetObject<object>("currentUser"); // nếu bạn muốn lấy email thì đổi object -> UserDto

        var vm = new CheckoutVm
        {
            FullName = fullName,
            Cart = cart,
            TotalAmount = cart.TotalAmount()
        };

        return View("Payment", vm);
    }

    [HttpPost("/checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutVm vm)
    {
        // 1. Kiểm tra đăng nhập
        if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
        var userId = HttpContext.Session.GetInt32("UserId");

        // 2. Lấy giỏ hàng lại từ Session để đảm bảo tính đúng đắn (không tin tưởng dữ liệu submit từ form về giá tiền)
        var cart = HttpContext.Session.GetObject<Cart>("cart");
        if (cart == null || cart.Carts.Count == 0) return RedirectToAction("Index", "Cart");

        // 3. Validate dữ liệu
        if (!ModelState.IsValid)
        {
            vm.Cart = cart;
            vm.TotalAmount = cart.TotalAmount();
            return View("Payment", vm); // Trả về View nếu lỗi
        }

        // 4. Tạo payload gửi sang API
        var payload = new
        {
            userId = userId.Value,
            fullName = vm.FullName,
            email = vm.Email,
            phoneNumber = vm.PhoneNumber,
            address = vm.Address,
            note = vm.Note,
            paymentName = vm.PaymentMethod,
            shippingFee = 0, // Có thể cập nhật logic phí ship sau
            discount = 0,    // Có thể cập nhật logic voucher sau
            voucherId = (int?)null,
            items = cart.Carts.Values.Select(i => new { productId = i.ProductId, quantity = i.Quantity }).ToList()
        };

        // 5. Gọi API
        var (ok, orderId, message) = await _orderApi.CreateAsync(payload);

        if (!ok || orderId == null)
        {
            ModelState.AddModelError("", message ?? "Đặt hàng thất bại.");
            vm.Cart = cart;
            vm.TotalAmount = cart.TotalAmount();
            return View("Payment", vm);
        }

        // 6. Thành công: Xóa giỏ hàng và chuyển trang
        HttpContext.Session.Remove("cart");
        HttpContext.Session.SetInt32("cartCount", 0);

        return RedirectToAction("Success", new { orderId = orderId.Value });
    }
    [HttpGet("/checkout/success/{orderId:int}")]
    public async Task<IActionResult> Success(int orderId)
    {
        if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

        var order = await _orderApi.GetAsync(orderId);
        if (order == null) return RedirectToAction("Index", "Home");

        return View(order); // Views/Checkout/Success.cshtml
    }

    private bool IsLoggedIn()
        => HttpContext.Session.GetObject<object>("currentUser") != null;
}
