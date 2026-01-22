using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos;
using MyThuatShop.Api.Helpers;
using MyThuatShop.Api.Models;

namespace MyThuatShop.Api.Controllers
{
    [ApiController]
    [Route("api/admin/products")]
    public class AdminProductsController : ControllerBase
    {
        private readonly MyThuatDotNetContext _db; 
        private readonly IWebHostEnvironment _env;

        public AdminProductsController(MyThuatDotNetContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetAll()
        {
            // admin: xem tất cả
            var list = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Subimages)
                .Include(p => p.Specifications)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] AdminProductUpsertForm form)
        {
            var mainUrl = await FileUploadHelper.SaveAsync(form.ThumbnailMain, _env, "products");

            var qty = form.QuantityStock ?? 0;
            var discount = form.DiscountDefault ?? 0;

            var p = new Product
            {
                Name = form.Name ?? "",
                Price = form.Price,
                DiscountDefault = discount,
                CategoryId = form.CategoryId,
                Thumbnail = mainUrl,
                QuantityStock = qty,
                SoldQuantity = 0,
                Status = qty > 0 ? "Còn hàng" : "Hết hàng",
                CreateAt = DateTime.Now,
                Brand = form.Brand,
                Content = form.Content,
                IsActive = true

            };

            _db.Products.Add(p);
            await _db.SaveChangesAsync();

            // subimages upload
            if (form.ThumbnailSubs != null && form.ThumbnailSubs.Count > 0)
            {
                foreach (var f in form.ThumbnailSubs.Where(x => x != null && x.Length > 0))
                {
                    var subUrl = await FileUploadHelper.SaveAsync(f, _env, "subimages");
                    if (!string.IsNullOrWhiteSpace(subUrl))
                    {
                        _db.Subimages.Add(new Subimage
                        {
                            ProductId = p.Id,
                            Image = subUrl
                        });
                    }
                }
                await _db.SaveChangesAsync();
            }

            // spec insert 1 record
            _db.Specifications.Add(new Specification
            {
                ProductId = p.Id,
                Size = form.Size,
                Standard = form.Standard ?? "",
                MadeIn = form.MadeIn,
                Warning = form.Warning
            });
            await _db.SaveChangesAsync();

            return Ok(new { ok = true, id = p.Id });
        }

        [HttpPost("update")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update([FromForm] AdminProductUpsertForm form)
        {
            if (form.Id <= 0) return BadRequest("Id invalid");

            var p = await _db.Products
                .Include(x => x.Subimages)
                .Include(x => x.Specifications)
                .FirstOrDefaultAsync(x => x.Id == form.Id);

            if (p == null) return NotFound();

            p.CategoryId = form.CategoryId;
            p.Name = form.Name ?? "";
            p.Price = form.Price;
            p.DiscountDefault = form.DiscountDefault ?? 0;
            p.QuantityStock = form.QuantityStock ?? 0;
            p.Brand = form.Brand;
            p.Content = form.Content;

            // remove thumbnail (giống categories)
            if (form.RemoveThumbnail == 1)
            {
                FileUploadHelper.TryDeleteIfLocal(_env, p.Thumbnail);
                p.Thumbnail = null;
            }

            // upload main new -> replace + delete old local
            if (form.ThumbnailMain != null && form.ThumbnailMain.Length > 0)
            {
                var old = p.Thumbnail;
                var newUrl = await FileUploadHelper.SaveAsync(form.ThumbnailMain, _env, "products");
                p.Thumbnail = newUrl;
                FileUploadHelper.TryDeleteIfLocal(_env, old);
            }

            // subimages: nếu upload => replace all
            if (form.ThumbnailSubs != null && form.ThumbnailSubs.Count > 0)
            {
                foreach (var s in p.Subimages)
                    FileUploadHelper.TryDeleteIfLocal(_env, s.Image);

                _db.Subimages.RemoveRange(p.Subimages);

                foreach (var f in form.ThumbnailSubs.Where(x => x != null && x.Length > 0))
                {
                    var subUrl = await FileUploadHelper.SaveAsync(f, _env, "subimages");
                    if (!string.IsNullOrWhiteSpace(subUrl))
                    {
                        _db.Subimages.Add(new Subimage
                        {
                            ProductId = p.Id,
                            Image = subUrl
                        });
                    }
                }
            }

            // spec upsert: dùng record đầu tiên trong ICollection
            var spec = p.Specifications.FirstOrDefault();
            if (spec == null)
            {
                spec = new Specification { ProductId = p.Id };
                _db.Specifications.Add(spec);
            }
            spec.Size = form.Size;
            spec.Standard = form.Standard ?? "";
            spec.MadeIn = form.MadeIn;
            spec.Warning = form.Warning;

            await _db.SaveChangesAsync();
            return Ok(new { ok = true });
        }

        [HttpPost("setActive")]
        public async Task<IActionResult> SetActive([FromBody] ToggleActiveDto dto)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == dto.Id);
            if (p == null) return NotFound();

            p.IsActive = dto.IsActive; // set thẳng true/false
            await _db.SaveChangesAsync();
            return Ok(new { ok = true });
        }


    }
}
