using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos;
using MyThuatShop.Api.Models;

namespace MyThuatShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly MyThuatDotNetContext _db;
        public ProductsController(MyThuatDotNetContext db) => _db = db;

        // GET: /api/products?take=10
        // ✅ chỉ lấy sản phẩm đang active
        [HttpGet]
        public async Task<IActionResult> Get(int take = 10)
        {
            if (take <= 0) take = 10;

            var data = await _db.Products
                .AsNoTracking()
                .Where(p => p.IsActive)              
                .OrderByDescending(p => p.Id)
                .Take(take)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var p = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (p == null) return NotFound();
            return Ok(p);
        }

     
        [HttpGet("detail/{id:int}")]
        public async Task<IActionResult> DetailFull(int id)
        {
            var p = await _db.Products
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.Subimages)
                .Include(x => x.Specifications)
                .Include(x => x.ProductReviews).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive); 

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
                Content = p.Content,

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

    // sp cung loai 
            dto.RelatedProducts = await _db.Products
                .AsNoTracking()
                .Where(x => x.IsActive && x.CategoryId == p.CategoryId && x.Id != p.Id) 
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

        [HttpPost("{id:int}/reviews")]
        public async Task<IActionResult> AddReview(int id, [FromBody] CreateReviewRequestDto req)
        {
            if (!await _db.Products.AnyAsync(p => p.Id == id && p.IsActive))
                return NotFound("Product not found");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId);
            if (user == null)
                return BadRequest("User not found");

            var rating = req.Rating;
            if (rating < 1 || rating > 5) rating = 5;

            var review = new ProductReview
            {
                ProductId = id,
                UserId = req.UserId,
                Rating = rating,
                Comment = string.IsNullOrWhiteSpace(req.Comment) ? null : req.Comment.Trim(),
                CreateAt = DateTime.Now
            };

            _db.ProductReviews.Add(review);
            await _db.SaveChangesAsync();

            var dto = new ReviewDto
            {
                Id = review.Id,
                UserId = review.UserId,
                Rating = review.Rating,
                Comment = review.Comment,
                CreateAt = review.CreateAt,
                UserFullName = user.FullName
            };

            return Ok(dto);
        }

     
        [HttpGet("cart/{id:int}")]
        public async Task<IActionResult> GetForCart(int id)
        {
            var p = await _db.Products
                .AsNoTracking()
                .Where(x => x.Id == id && x.IsActive) 
                .Select(x => new CartProductDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Price = x.Price,
                    DiscountDefault = x.DiscountDefault ?? 0,
                    Thumbnail = x.Thumbnail,
                    QuantityStock = x.QuantityStock ?? 0,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();

            if (p == null) return NotFound();
            return Ok(p);
        }
    }
}
