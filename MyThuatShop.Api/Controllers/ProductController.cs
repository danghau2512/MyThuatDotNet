using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos;

namespace MyThuatShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly MyThuatDotNetContext _db;
        public ProductsController(MyThuatDotNetContext db) => _db = db;

        // ✅ GET: /api/products?take=10
        [HttpGet]
        public async Task<IActionResult> Get(int take = 10)
        {
            if (take <= 0) take = 10;

            var data = await _db.Products
                .OrderByDescending(p => p.Id)
                .Take(take)
                .ToListAsync();

            return Ok(data);
        }

        // ✅ GET: /api/products/5 (trả entity đơn giản)
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();
            return Ok(p);
        }

        // ✅ GET: /api/products/detail/5 (trả DTO đầy đủ)
        [HttpGet("detail/{id:int}")]
        public async Task<IActionResult> DetailFull(int id)
        {
            var p = await _db.Products
                .Include(x => x.Category)
                .Include(x => x.Subimages)
                .Include(x => x.Specifications)
                .Include(x => x.ProductReviews).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null) return NotFound();

            var dto = new ProductDetailDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                DiscountDefault = p.DiscountDefault ?? 0,
                FinalPrice = p.Price * (100 - (p.DiscountDefault ?? 0)) / 100,

                CategoryId = p.CategoryId,
                CategoryName = p.Category?.CategoryName ?? "",

                Thumbnail = p.Thumbnail,
                Brand = p.Brand,
                Status = p.Status,

                QuantityStock = p.QuantityStock ?? 0,
                SoldQuantity = p.SoldQuantity ?? 0,

                SubImages = p.Subimages
                    .OrderBy(si => si.Id)
                    .Select(si => si.Image)
                    .ToList(),

                Specifications = p.Specifications
                    .OrderBy(s => s.Id)
                    .Select(s => new SpecificationDto
                    {
                        Brand = s.Brand,
                        Size = s.Size,
                        Standard = s.Standard,
                        MadeIn = s.MadeIn,
                        Warning = s.Warning
                    })
                    .ToList(),

                AvgRating = p.ProductReviews.Any()
                    ? Math.Round(p.ProductReviews.Average(r => (double)r.Rating), 2)
                    : 0,

                ReviewCount = p.ProductReviews.Count(),

                Reviews = p.ProductReviews
                    .OrderByDescending(r => r.CreateAt)
                    .Take(10)
                    .Select(r => new ReviewDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreateAt = r.CreateAt,
                        UserFullName = r.User.FullName
                    })
                    .ToList()
            };

            
            if (string.IsNullOrWhiteSpace(dto.Thumbnail) && dto.SubImages.Any())
                dto.Thumbnail = dto.SubImages[0];

            
            dto.RelatedProducts = await _db.Products
                .Where(x => x.CategoryId == p.CategoryId && x.Id != p.Id)
                .OrderByDescending(x => x.Id)
                .Take(5)
                .Select(x => new RelatedProductDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Thumbnail = x.Thumbnail,
                    Price = x.Price,
                    DiscountDefault = x.DiscountDefault ?? 0,
                    FinalPrice = x.Price * (100 - (x.DiscountDefault ?? 0)) / 100,
                    SoldQuantity = x.SoldQuantity ?? 0
                })
                .ToListAsync();

            return Ok(dto);
        }
    }
}
