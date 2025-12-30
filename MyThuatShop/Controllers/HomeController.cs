using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Services;

public class HomeController : Controller
{
    private readonly HomeApiService _api;
    private readonly ILogger<HomeController> _logger;

    public HomeController(HomeApiService api, ILogger<HomeController> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {

        var sections = await _api.GetIndexSections(10);
        _logger.LogInformation("Loaded {Count} category sections", sections.Count);
        return View(sections);
    }
}
