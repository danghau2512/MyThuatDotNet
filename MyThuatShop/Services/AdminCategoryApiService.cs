using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using MyThuatShop.Dtos.Admin;

namespace MyThuatShop.Services
{
    public class AdminCategoryApiService
    {
        private readonly HttpClient _http;

        public AdminCategoryApiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<AdminCategoryDto>> GetAllAsync()
        {
            var data = await _http.GetFromJsonAsync<List<AdminCategoryDto>>("/api/admin/categories");
            return data ?? new List<AdminCategoryDto>();
        }

        public async Task<bool> CreateAsync(string categoryName, IFormFile? thumbnail)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("create"), "action");
            form.Add(new StringContent(categoryName ?? ""), "categoryName");

            await AddFileIfAnyAsync(form, "thumbnail", thumbnail);

            var res = await _http.PostAsync("/api/admin/categories", form);
            return res.IsSuccessStatusCode;
        }

        // ✅ NEW: thêm removeThumbnail
        public async Task<bool> UpdateAsync(int id, string categoryName, IFormFile? thumbnail, bool removeThumbnail)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("update"), "action");
            form.Add(new StringContent(id.ToString()), "id");
            form.Add(new StringContent(categoryName ?? ""), "categoryName");
            form.Add(new StringContent(removeThumbnail ? "1" : "0"), "removeThumbnail");

            await AddFileIfAnyAsync(form, "thumbnail", thumbnail);

            var res = await _http.PostAsync("/api/admin/categories", form);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> ToggleActiveAsync(int id, int isActive)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("toggleActive"), "action");
            form.Add(new StringContent(id.ToString()), "id");
            form.Add(new StringContent(isActive.ToString()), "isActive"); // current state gửi lên

            var res = await _http.PostAsync("/api/admin/categories", form);
            return res.IsSuccessStatusCode;
        }

        private static async Task AddFileIfAnyAsync(MultipartFormDataContent form, string fieldName, IFormFile? file)
        {
            if (file == null || file.Length <= 0) return;

            var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            var content = new StreamContent(ms);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType
            );

            form.Add(content, fieldName, file.FileName);
        }
    }
}
