using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Services;

namespace MyThuatShop.Controllers;

[Route("admin/overview")]
public class AdminOverviewController : Controller
{
    private readonly AdminOverviewApiService _service;

    public AdminOverviewController(AdminOverviewApiService service)
    {
        _service = service;
    }

    private bool IsAdmin()
        => string.Equals(HttpContext.Session.GetString("Role"), "Admin",
            StringComparison.OrdinalIgnoreCase);

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        if (!IsAdmin()) return Redirect("/login");

        var data = await _service.GetAsync(latest: 10, top: 10);
        data ??= new AdminOverviewApiService.AdminOverviewDto();
        return View(data);
    }
}
