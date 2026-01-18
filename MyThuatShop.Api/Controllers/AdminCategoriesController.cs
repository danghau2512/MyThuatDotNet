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
                    // ✅ FIX bool? -> int
                    isActive = (x.IsActive == true) ? 1 : 0
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Post(
            [FromForm] string action,
            [FromForm] int id,
            [FromForm] int isActive,
            [FromForm] string categoryName,
            IFormFile? thumbnail)
        {
            action = (action ?? "").Trim();

            if (action == "create")
            {
                var imgUrl = await SaveUpload(thumbnail, "uploads/categories");

                // ✅ FIX CS0246: không dùng new Category nữa
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
                var old = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
                if (old == null) return NotFound(new { ok = false, message = "Category not found" });

                old.CategoryName = categoryName;

                if (thumbnail != null && thumbnail.Length > 0)
                {
                    var imgUrl = await SaveUpload(thumbnail, "uploads/categories");
                    old.Thumbnail = imgUrl;
                }

                await _db.SaveChangesAsync();
                return Ok(new { ok = true });
            }

            if (action == "toggleActive")
            {
                var old = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
                if (old == null) return NotFound(new { ok = false, message = "Category not found" });

                // đảo bool? an toàn
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
