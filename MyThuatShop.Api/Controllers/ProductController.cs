using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;

namespace MyThuatShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly MyThuatDotNetContext _db;
        public ProductsController(MyThuatDotNetContext db) => _db = db;

        // GET: /api/products?take=10
        [HttpGet]
        public async Task<IActionResult> Get(int take = 10)
        {
            var data = await _db.Products
                .OrderByDescending(p => p.Id)
                .Take(take)
                .ToListAsync();

            return Ok(data);
        }

        // GET: /api/products/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();
            return Ok(p);
        }
    }
}
