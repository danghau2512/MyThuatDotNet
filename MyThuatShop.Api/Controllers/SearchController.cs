using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos;

namespace MyThuatShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly MyThuatDotNetContext _context;

        public SearchController(MyThuatDotNetContext context)
        {
            _context = context;
        }

        // GET: /api/search/suggest?keyword=abc&take=8
        [HttpGet("suggest")]
        public async Task<ActionResult<List<ProductSuggestDto>>> Suggest(
            [FromQuery] string keyword,
            [FromQuery] int take = 8)
        {
            keyword = (keyword ?? "").Trim();

            if (keyword.Length < 2)
                return Ok(new List<ProductSuggestDto>());

            take = Math.Clamp(take, 1, 20);

            var query = _context.Products
                .AsNoTracking()
                .Where(p => p.Name.Contains(keyword))
                .OrderBy(p => p.Name)
                .Select(p => new ProductSuggestDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price * (1 - (p.DiscountDefault ?? 0) / 100m),
                    ThumbnailUrl = p.Thumbnail 
                })
                .Take(take);

            var data = await query.ToListAsync();
            return Ok(data);
        }
    }
}
