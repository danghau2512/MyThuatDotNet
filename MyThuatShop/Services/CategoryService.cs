using Microsoft.EntityFrameworkCore;
using MyThuatShop.Data;
using MyThuatShop.Models;

namespace MyThuatShop.Services
{
    public class CategoryService
    {
        private readonly AppDbContext _db;
        public CategoryService(AppDbContext db) => _db = db;

        public Task<List<Category>> GetAllAsync()
            => _db.Categories.OrderByDescending(x => x.Id).ToListAsync();

        public Task<Category?> GetByIdAsync(int id)
            => _db.Categories.FirstOrDefaultAsync(x => x.Id == id);

        public async Task<int> CreateAsync(Category c)
        {
            _db.Categories.Add(c);
            await _db.SaveChangesAsync();
            return c.Id;
        }

        public async Task<int> UpdateAsync(Category c)
        {
            var old = await _db.Categories.FirstOrDefaultAsync(x => x.Id == c.Id);
            if (old == null) return 0;

            old.CategoryName = c.CategoryName;
            old.Thumbnail = c.Thumbnail;
            await _db.SaveChangesAsync();
            return 1;
        }

        public async Task<int> ToggleActiveAsync(int id, int currentIsActive)
        {
            var old = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
            if (old == null) return 0;

            old.IsActive = (sbyte)(currentIsActive == 1 ? 0 : 1);
            await _db.SaveChangesAsync();
            return 1;
        }
    }
}
