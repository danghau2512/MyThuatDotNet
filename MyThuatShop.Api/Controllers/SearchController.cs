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

        [HttpGet("products")]
        public async Task<IActionResult> SearchProducts(
    [FromQuery] string q,
    [FromQuery] string sort = "all",
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 8)
        {
            q = (q ?? "").Trim();
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 60);

            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new AdminPagedResultDto<ProductCardDto>
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = 0,
                    TotalPages = 0,
                    Items = new()
                });
            }

            var query = _context.Products.AsNoTracking()
    .Where(p => p.IsActive && p.Name.Contains(q));


            query = sort switch
            {
                "bestseller" => query.OrderByDescending(p => p.SoldQuantity),
                "new" => query.OrderByDescending(p => p.CreateAt),
                "priceAsc" => query.OrderBy(p => p.Price * (100 - (p.DiscountDefault ?? 0)) / 100m),
                "priceDesc" => query.OrderByDescending(p => p.Price * (100 - (p.DiscountDefault ?? 0)) / 100m),
                _ => query.OrderBy(p => p.Name)
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductCardDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    DiscountDefault = p.DiscountDefault ?? 0,
                    Thumbnail = p.Thumbnail,
                    SoldQuantity = p.SoldQuantity ?? 0
                })
                .ToListAsync();

            return Ok(new AdminPagedResultDto<ProductCardDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = items
            });
        }

    }
}
