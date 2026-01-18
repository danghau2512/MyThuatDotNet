using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Extensions;
using MyThuatShop.Services;
using MyThuatShop.ViewModels.Auth;
using System.Security.Claims;

namespace MyThuatShop.Controllers;

public class AccountController : Controller
{
    private readonly AccountApiService _accountApi;

    public AccountController(AccountApiService accountApi)
    {
        _accountApi = accountApi;
    }

    [HttpGet]
    public IActionResult Login(int? expired, string? returnUrl = null)
    {
        if (expired == 1) ViewBag.SessionExpired = true;

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(vm);
        }

        var res = await _accountApi.LoginAsync(vm.Email, vm.Password);
        if (res == null)
        {
            ModelState.AddModelError("", "Sai email hoặc mật khẩu.");
            ViewBag.ReturnUrl = returnUrl;
            return View(vm);
        }

        HttpContext.Session.SetObject("currentUser", res.User);
        HttpContext.Session.SetInt32("UserId", res.User.Id);
        HttpContext.Session.SetString("FullName", res.User.FullName ?? "");
        HttpContext.Session.SetString("PhoneNumber", res.User.PhoneNumber ?? "");
        HttpContext.Session.SetString("Role", res.User.Role ?? "user");
        HttpContext.Session.SetString("Email", res.User.Email ?? "");

        // ✅ admin luôn vào overview
        if (!string.IsNullOrWhiteSpace(res.User.Role) &&
            string.Equals(res.User.Role, "ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect("/admin/overview");
        }

        // ✅ user quay lại trang cũ
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

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

        var external = await HttpContext.AuthenticateAsync("External");
        if (!external.Succeeded || external.Principal == null)
            return RedirectToAction("Login");

        var email = external.Principal.FindFirstValue(ClaimTypes.Email);
        var fullName = external.Principal.FindFirstValue(ClaimTypes.Name) ?? "";

        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction("Login");

        var res = await _accountApi.GoogleLoginAsync(email, fullName);
        if (res == null)
        {
            TempData["Error"] = "Đăng nhập Google thất bại.";
            return RedirectToAction("Login");
        }

        HttpContext.Session.SetObject("currentUser", res.User);
        HttpContext.Session.SetInt32("UserId", res.User.Id);
        HttpContext.Session.SetString("FullName", res.User.FullName ?? "");
        HttpContext.Session.SetString("Role", res.User.Role ?? "user");

        await HttpContext.SignOutAsync("External");

        if (!string.IsNullOrWhiteSpace(res.User.Role) &&
            string.Equals(res.User.Role, "ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect("/admin/overview");
        }

        return LocalRedirect(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
