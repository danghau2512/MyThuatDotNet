using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;

namespace MyThuatShop.Api.Controllers
{
    [ApiController]
    [Route("api/admin/categories")]
    public class AdminCategoriesController : ControllerBase
    {
        private readonly MyThuatDotNetContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminCategoriesController(MyThuatDotNetContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _db.Categories
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    id = x.Id,
                    categoryName = x.CategoryName,
                    thumbnail = x.Thumbnail,
                    // trả int 1/0 như JSP
                    isActive = (x.IsActive == true) ? 1 : 0
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Post(
            [FromForm] string? action,
            [FromForm] int id,
            [FromForm] int? isActive,
            [FromForm] string? categoryName,
            [FromForm] int? removeThumbnail,        // ✅ NEW: cờ xóa ảnh
            IFormFile? thumbnail)
        {
            action = (action ?? "").Trim();

            if (action == "create")
            {
                if (string.IsNullOrWhiteSpace(categoryName))
                    return BadRequest(new { ok = false, message = "categoryName is required" });

                var imgUrl = await SaveUpload(thumbnail, "uploads/categories");

                var entityType = _db.Categories.EntityType.ClrType;
                dynamic c = Activator.CreateInstance(entityType)!;

                c.CategoryName = categoryName;
                c.Thumbnail = imgUrl;
                c.IsActive = true;

                _db.Add(c);
                await _db.SaveChangesAsync();
                return Ok(new { ok = true });
            }

            if (action == "update")
            {
                if (id <= 0) return BadRequest(new { ok = false, message = "id is required" });
                if (string.IsNullOrWhiteSpace(categoryName))
                    return BadRequest(new { ok = false, message = "categoryName is required" });

                var old = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
                if (old == null) return NotFound(new { ok = false, message = "Category not found" });

                old.CategoryName = categoryName;

                // ✅ Logic y như JSP:
                // - Nếu removeThumbnail=1 và không có file mới -> set null
                // - Nếu có file mới -> upload và ghi đè
                var wantRemove = (removeThumbnail ?? 0) == 1;
                var hasNewFile = thumbnail != null && thumbnail.Length > 0;

                if (wantRemove && !hasNewFile)
                {
                    old.Thumbnail = null;
                }

                if (hasNewFile)
                {
                    var imgUrl = await SaveUpload(thumbnail, "uploads/categories");
                    old.Thumbnail = imgUrl;
                }

                await _db.SaveChangesAsync();
                return Ok(new { ok = true });
            }

            if (action == "toggleActive")
            {
                if (id <= 0) return BadRequest(new { ok = false, message = "id is required" });

                var old = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
                if (old == null) return NotFound(new { ok = false, message = "Category not found" });

                
                if (isActive.HasValue)
                    old.IsActive = isActive.Value == 1 ? false : true;
                else
                    old.IsActive = !(old.IsActive == true);

                await _db.SaveChangesAsync();
                return Ok(new { ok = true });
            }

            return BadRequest(new { ok = false, message = "Invalid action" });
        }

        private async Task<string?> SaveUpload(IFormFile? file, string folder)
        {
            if (file == null || file.Length == 0) return null;

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";

            var webRoot = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var targetDir = Path.Combine(webRoot, folder.Replace("/", Path.DirectorySeparatorChar.ToString()));
            Directory.CreateDirectory(targetDir);

            var fullPath = Path.Combine(targetDir, fileName);
            using var fs = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(fs);

            return "/" + folder.Trim('/') + "/" + fileName;
        }
    }
}
