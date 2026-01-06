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
    private readonly IVnPayService _vnPayService;

    public CheckoutController(OrderApiService orderApi, IVnPayService vnPayService)
    {
        _orderApi = orderApi;
        _vnPayService = vnPayService;
    }

    [HttpGet("/checkout")]
    public IActionResult Index()
    {
        if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

        var cart = HttpContext.Session.GetObject<Cart>("cart");
        if (cart == null || cart.Carts.Count == 0) return RedirectToAction("Index", "Cart");

        var fullName = HttpContext.Session.GetString("FullName") ?? "";
        var currentUser = HttpContext.Session.GetObject<object>("currentUser"); 

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
        if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
        var userId = HttpContext.Session.GetInt32("UserId");
        var cart = HttpContext.Session.GetObject<Cart>("cart");
        if (cart == null || cart.Carts.Count == 0) return RedirectToAction("Index", "Cart");

        if (!ModelState.IsValid)
        {
            vm.Cart = cart;
            vm.TotalAmount = cart.TotalAmount();
            return View("Payment", vm);
        }

        // Tạo payload
        var payload = new
        {
            userId = userId.Value,
            fullName = vm.FullName,
            email = vm.Email,
            phoneNumber = vm.PhoneNumber,
            address = vm.Address,
            note = vm.Note,
            paymentName = vm.PaymentMethod, // "COD" hoặc "VNPAY" lấy từ radio button
            shippingFee = 0,
            discount = vm.DiscountAmount,
            voucherId = vm.DiscountAmount,
            items = cart.Carts.Values.Select(i => new { productId = i.ProductId, quantity = i.Quantity }).ToList()
        };

        // 1. Gọi API Tạo đơn hàng
        // Lưu ý: Do logic ở OrdersController đã sửa, nếu là VNPAY thì Order được tạo nhưng kho chưa trừ
        var (ok, orderId, message) = await _orderApi.CreateAsync(payload);

        if (!ok || orderId == null)
        {
            ModelState.AddModelError("", message ?? "Đặt hàng thất bại.");
            vm.Cart = cart;
            vm.TotalAmount = cart.TotalAmount();
            return View("Payment", vm);
        }

        // 2. Phân nhánh xử lý Payment
        if (vm.PaymentMethod == "VNPAY")
        {
            // Nếu là VNPay, chuyển hướng sang trang thanh toán
            vm.TotalAmount = cart.TotalAmount();
            var url = _vnPayService.CreatePaymentUrl(HttpContext, vm, orderId.Value);
            return Redirect(url);
        }

        // Nếu là COD (thì API đã trừ kho rồi), xóa session và xong
        HttpContext.Session.Remove("cart");
        HttpContext.Session.SetInt32("cartCount", 0);
        return RedirectToAction("Success", new { orderId = orderId.Value });
    }

    [HttpGet]
    public async Task<IActionResult> PaymentCallback()
    {
        var response = _vnPayService.PaymentExecute(Request.Query);

        if (response.Success && response.VnPayResponseCode == "00")
        {
            // Thanh toán thành công -> Gọi API Confirm để trừ kho và update status
            var orderId = int.Parse(response.OrderId);
            var result = await _orderApi.ConfirmPaymentAsync(orderId);

            if (result)
            {
                // Xóa Session giỏ hàng
                HttpContext.Session.Remove("cart");
                HttpContext.Session.SetInt32("cartCount", 0);

                TempData["Message"] = "Thanh toán VNPay thành công!";
                return RedirectToAction("Success", new { orderId = orderId });
            }
        }

        // Nếu thất bại hoặc chữ ký sai
        TempData["Error"] = "Thanh toán thất bại hoặc có lỗi xảy ra.";
        // Có thể redirect về trang Cart hoặc trang báo lỗi tùy bạn
        return RedirectToAction("Index", "Cart");
    }
    [HttpGet("/checkout/success/{orderId:int}")]
    public async Task<IActionResult> Success(int orderId)
    {
        if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

        var order = await _orderApi.GetAsync(orderId);
        if (order == null) return RedirectToAction("Index", "Home");

        return View(order); // Views/Checkout/Success.cshtml
    }
    [HttpGet]
    public async Task<IActionResult> ApplyVoucher(string code)
    {
        var cart = HttpContext.Session.GetObject<Cart>("cart");
        if (cart == null) return Json(new { success = false, message = "Giỏ hàng trống" });

        decimal totalOrder = cart.TotalAmount(); // Tổng tiền hàng
        var result = await _orderApi.CheckVoucherAsync(code, totalOrder);

        return Json(new
        {
            success = result.success,
            message = result.message,
            discount = result.discount,
            voucherId = result.voucherId,
            newTotal = totalOrder - result.discount // Trả về tổng tiền mới để JS update
        });
    }

    private bool IsLoggedIn()
        => HttpContext.Session.GetObject<object>("currentUser") != null;
}
