using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Extensions;
using MyThuatShop.Services;
using MyThuatShop.ViewModels.Auth;

namespace MyThuatShop.Controllers;

public class AccountController : Controller
{
    private readonly AccountApiService _accountApi;

    public AccountController(AccountApiService accountApi)
    {
        _accountApi = accountApi;
    }

    // ===== LOGIN =====
    [HttpGet]
    public IActionResult Login(int? expired)
    {
        if (expired == 1)
            ViewBag.SessionExpired = true;

        return View(new LoginVm());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var res = await _accountApi.LoginAsync(vm.Email, vm.Password);
        if (res == null)
        {
            ModelState.AddModelError("", "Sai email hoặc mật khẩu.");
            return View(vm);
        }

        HttpContext.Session.SetObject("currentUser", res.User);
        HttpContext.Session.SetInt32("UserId", res.User.Id);
        HttpContext.Session.SetString("FullName", res.User.FullName ?? "");
        HttpContext.Session.SetString("PhoneNumber", res.User.PhoneNumber ?? "");
        HttpContext.Session.SetString("Role", res.User.Role ?? "Customer");
        HttpContext.Session.SetString("Email", res.User.Email ?? "");


        return RedirectToAction("Index", "Home");
    }

    // ===== REGISTER =====
    //[HttpGet]
    //public IActionResult Register()
    //{
    //    return View(new RegisterVm());
    //}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var (ok, message) = await _accountApi.RegisterAsync(vm);
        if (!ok)
        {
            ModelState.AddModelError("", message);
            return View(vm);
        }

        TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }

    // ===== LOGOUT =====
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear(); 
        return RedirectToAction("Index", "Home");
    }

}
