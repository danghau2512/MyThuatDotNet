using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos;

namespace MyThuatShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly MyThuatDotNetContext _db;
        public HomeController(MyThuatDotNetContext db) => _db = db;

        // GET: /api/home/index?takePerCategory=10
        [HttpGet("index")]
        public async Task<IActionResult> Index(int takePerCategory = 10)
        {
  
            var sections = await _db.Categories
                .Where(c => c.IsActive == true) 
                .Select(c => new CategorySectionDto
                {
                    CategoryId = c.Id,
                    CategoryName = c.CategoryName,
                    Thumbnail = c.Thumbnail,
                    Products = c.Products
                        .Where(p => p.IsActive == true)
                        .OrderByDescending(p => p.CreateAt)
                        .Take(takePerCategory)
                        .Select(p => new ProductCardDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Price = p.Price,
                            DiscountDefault = p.DiscountDefault ?? 0,
                            Thumbnail = p.Thumbnail,
                            SoldQuantity = p.SoldQuantity ?? 0
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(sections);
        }
    }
}
