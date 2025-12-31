using Microsoft.AspNetCore.Mvc;

namespace MyThuatShop.Controllers;

public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }
}
