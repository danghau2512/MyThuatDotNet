using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Extensions;
using MyThuatShop.Services;
using MyThuatShop.ViewModels.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

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


        // ✅ redirect theo role
        var role = HttpContext.Session.GetString("Role");
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect("/admin/overview");
            // hoặc RedirectToAction("Index","AdminDashboard")
        }

        // người dùng thường
        return RedirectToAction("Index", "Home");
    }
    // ===== LOGIN GOOGLE =====
    [HttpGet]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        returnUrl ??= Url.Action("Index", "Home")!;

        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action("GoogleCallback", "Account", new { returnUrl })
        };

        return Challenge(props, "Google");
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
    {
        returnUrl ??= Url.Action("Index", "Home")!;

        // lấy thông tin Google từ cookie External
        var external = await HttpContext.AuthenticateAsync("External");
        if (!external.Succeeded || external.Principal == null)
            return RedirectToAction("Login");

        var email = external.Principal.FindFirstValue(ClaimTypes.Email);
        var fullName = external.Principal.FindFirstValue(ClaimTypes.Name) ?? "";

        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction("Login");

        // ✅ gọi API để tạo/đăng nhập user theo email Google
        var res = await _accountApi.GoogleLoginAsync(email, fullName);
        if (res == null)
        {
            TempData["Error"] = "Đăng nhập Google thất bại.";
            return RedirectToAction("Login");
        }

        // set session giống login thường
        HttpContext.Session.SetObject("currentUser", res.User);
        HttpContext.Session.SetInt32("UserId", res.User.Id);
        HttpContext.Session.SetString("FullName", res.User.FullName ?? "");
        HttpContext.Session.SetString("Role", res.User.Role ?? "user");

        // clear External cookie
        await HttpContext.SignOutAsync("External");

        var role = HttpContext.Session.GetString("Role");
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect("/admin/overview");

        }

        return RedirectToAction("Index", "Home");
    }

    //===== REGISTER =====
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterVm());
    }

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
