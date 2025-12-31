using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Services;

namespace MyThuatShop.Controllers
{
    public class SearchController : Controller
    {
        private readonly SearchApiService _service;

        public SearchController(SearchApiService service)
        {
            _service = service;
        }

        // GET: /Search/Suggest?keyword=abc 
        [HttpGet]
        public async Task<IActionResult> Suggest(string keyword, int take = 8)
        {
            var data = await _service.SuggestAsync(keyword, take);
            return Json(data); // trả JSON cho AJAX
        }
    }
}
