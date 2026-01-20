using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Extensions;
using MyThuatShop.Services;
using MyThuatShop.ViewModels.Auth;
using System.Security.Claims;
using MyThuatShop.ViewModels.Account;


namespace MyThuatShop.Controllers;

public class AccountController : Controller
{
    private readonly AccountApiService _accountApi;

    public AccountController(AccountApiService accountApi)
    {
        _accountApi = accountApi;
    }
    // return url
    [HttpGet]
    public IActionResult Login(int? expired, string? returnUrl = null)
    {
        if (expired == 1) ViewBag.SessionExpired = true;

        
        returnUrl ??= Request.Query["returnUrl"].ToString();

        
        var vm = new LoginVm { ReturnUrl = returnUrl };

        ViewBag.ReturnUrl = returnUrl;
        return View(vm);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm, string? returnUrl = null)
    {
        // ✅ Ưu tiên returnUrl từ param, nếu null thì lấy từ vm.ReturnUrl, cuối cùng lấy query
        returnUrl ??= vm.ReturnUrl;
        returnUrl ??= Request.Query["returnUrl"].ToString();

        if (!ModelState.IsValid)
        {
            vm.ReturnUrl = returnUrl;
            ViewBag.ReturnUrl = returnUrl;
            return View(vm);
        }

        var res = await _accountApi.LoginAsync(vm.Email, vm.Password);
        if (res == null)
        {
            ModelState.AddModelError("", "Sai email hoặc mật khẩu.");
            vm.ReturnUrl = returnUrl;
            ViewBag.ReturnUrl = returnUrl;
            return View(vm);
        }

        HttpContext.Session.SetObject("currentUser", res.User);
        HttpContext.Session.SetInt32("UserId", res.User.Id);
        HttpContext.Session.SetString("FullName", res.User.FullName ?? "");
        HttpContext.Session.SetString("PhoneNumber", res.User.PhoneNumber ?? "");
        HttpContext.Session.SetString("Role", res.User.Role ?? "user");
        HttpContext.Session.SetString("Email", res.User.Email ?? "");

        //  admin luôn vào overview
        if (!string.IsNullOrWhiteSpace(res.User.Role) &&
            string.Equals(res.User.Role, "ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect("/admin/overview");
        }

        //  user quay lại trang cũ
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
            return RedirectToAction("Login", new { returnUrl });

        var email = external.Principal.FindFirstValue(ClaimTypes.Email);
        var fullName = external.Principal.FindFirstValue(ClaimTypes.Name) ?? "";

        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction("Login", new { returnUrl });

        var res = await _accountApi.GoogleLoginAsync(email, fullName);
        if (res == null)
        {
            TempData["Error"] = "Đăng nhập Google thất bại.";
            return RedirectToAction("Login", new { returnUrl });
        }

        HttpContext.Session.SetObject("currentUser", res.User);
        HttpContext.Session.SetInt32("UserId", res.User.Id);
        HttpContext.Session.SetString("FullName", res.User.FullName ?? "");
        HttpContext.Session.SetString("Role", res.User.Role ?? "user");
        HttpContext.Session.SetString("Email", res.User.Email ?? email);
        HttpContext.Session.SetString("PhoneNumber", res.User.PhoneNumber ?? "");

        await HttpContext.SignOutAsync("External");

        if (!string.IsNullOrWhiteSpace(res.User.Role) &&
            string.Equals(res.User.Role, "ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect("/admin/overview");
        }

        //  chỉ redirect nếu local
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login", "Account");
    }



    [HttpGet]
    public async Task<IActionResult> Profile(bool? success = null)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToAction("Login", new { returnUrl = Url.Action("Profile", "Account") });

        var dto = await _accountApi.GetProfileAsync(userId.Value);

        var vm = new ProfileVm
        {
            FullName = dto?.FullName ?? (HttpContext.Session.GetString("FullName") ?? ""),
            Email = dto?.Email ?? (HttpContext.Session.GetString("Email") ?? ""),
            PhoneNumber = dto?.PhoneNumber ?? (HttpContext.Session.GetString("PhoneNumber") ?? ""),
            Dob = dto?.Dob,
            Address = dto?.Address ?? ""
        };

        ViewBag.Success = success == true;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileVm vm)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToAction("Login", new { returnUrl = Url.Action("Profile", "Account") });

        if (string.IsNullOrWhiteSpace(vm.FullName))
            ModelState.AddModelError("", "Họ và tên không được để trống.");

        if (!ModelState.IsValid)
            return View(vm);

        var (ok, message) = await _accountApi.UpdateProfileAsync(userId.Value, new AccountApiService.UpdateProfileRequestDto
        {
            FullName = vm.FullName,
            PhoneNumber = vm.PhoneNumber,
            Dob = vm.Dob,
            Address = vm.Address
        });

        if (!ok)
        {
            ModelState.AddModelError("", message);
            return View(vm);
        }

        // update session giống JSP (để header/ sidebar đổi ngay)
        HttpContext.Session.SetString("FullName", vm.FullName.Trim());
        HttpContext.Session.SetString("PhoneNumber", vm.PhoneNumber?.Trim() ?? "");

        return RedirectToAction("Profile", new { success = "true" });
    }
    // đổi mk
    [HttpGet]
    public IActionResult ChangePassword(bool? success = null)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToAction("Login", new { returnUrl = Url.Action("ChangePassword", "Account") });

        ViewBag.Success = success == true;
        return View(new ChangePasswordVm());
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordVm vm)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return RedirectToAction("Login", new { returnUrl = Url.Action("ChangePassword", "Account") });

        vm.CurrentPassword ??= "";
        vm.NewPassword ??= "";
        vm.ConfirmNewPassword ??= "";

        // ✅ Nếu cả 3 ô đều rỗng -> chỉ báo 1 dòng
        if (string.IsNullOrWhiteSpace(vm.CurrentPassword)
            && string.IsNullOrWhiteSpace(vm.NewPassword)
            && string.IsNullOrWhiteSpace(vm.ConfirmNewPassword))
        {
            ModelState.AddModelError("", "Vui lòng nhập mật khẩu hiện tại.");
            return View(vm);
        }

        // ✅ Validate theo thứ tự: thiếu cái nào báo cái đó (nhưng không bị nhiều dòng khi bấm rỗng)
        if (string.IsNullOrWhiteSpace(vm.CurrentPassword))
        {
            ModelState.AddModelError("", "Vui lòng nhập mật khẩu hiện tại.");
            return View(vm);
        }

        if (string.IsNullOrWhiteSpace(vm.NewPassword))
        {
            ModelState.AddModelError("", "Vui lòng nhập mật khẩu mới.");
            return View(vm);
        }
        // ✅ mật khẩu mới phải khác mật khẩu hiện tại
        if (vm.NewPassword == vm.CurrentPassword)
        {
            ModelState.AddModelError("", "Mật khẩu mới phải khác mật khẩu hiện tại.");
            return View(vm);
        }

        if (vm.NewPassword.Length < 8)
        {
            ModelState.AddModelError("", "Mật khẩu mới tối thiểu 8 ký tự.");
            return View(vm);
        }

        if (string.IsNullOrWhiteSpace(vm.ConfirmNewPassword))
        {
            ModelState.AddModelError("", "Vui lòng xác nhận mật khẩu mới.");
            return View(vm);
        }

        if (vm.NewPassword != vm.ConfirmNewPassword)
        {
            ModelState.AddModelError("", "Xác nhận mật khẩu mới không khớp.");
            return View(vm);
        }

        var (ok, message) = await _accountApi.ChangePasswordAsync(userId.Value,
            new AccountApiService.ChangePasswordRequestDto
            {
                CurrentPassword = vm.CurrentPassword,
                NewPassword = vm.NewPassword,
                ConfirmNewPassword = vm.ConfirmNewPassword
            });

        if (!ok)
        {
            ModelState.AddModelError("", message);
            return View(vm);
        }

        return RedirectToAction("ChangePassword", new { success = "true" });
    }


    // registe
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

        var (ok, message) = await _accountApi.RegisterAsync(new AccountApiService.RegisterRequestDto
        {
            FullName = vm.FullName.Trim(),
            Email = vm.Email.Trim(),
            PhoneNumber = vm.PhoneNumber?.Trim(),
            Password = vm.Password
        });

        if (!ok)
        {
            ModelState.AddModelError("", message);
            return View(vm);
        }

        TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }
    // quen mk
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var (ok, message) = await _accountApi.ForgotPasswordAsync(vm.Email);

        if (!ok)
        {
            ViewBag.Error = message;
            return View(vm);
        }

        ViewBag.Success = message;
        ModelState.Clear();                 // clear validation
        return View(new ForgotPasswordVm()); // clear input giống JSP
    }

}
