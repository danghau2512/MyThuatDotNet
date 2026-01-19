using Microsoft.AspNetCore.Hosting;

namespace MyThuatShop.Api.Helpers
{
    public static class FileUploadHelper
    {
        public static async Task<string?> SaveAsync(IFormFile? file, IWebHostEnvironment env, string folder)
        {
            if (file == null || file.Length == 0) return null;

            var ext = Path.GetExtension(file.FileName);
            var savedName = $"{Guid.NewGuid():N}{ext}";

            var dir = Path.Combine(env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(dir);

            var fullPath = Path.Combine(dir, savedName);
            await using var fs = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(fs);

            return $"/uploads/{folder}/{savedName}";
        }

        public static void TryDeleteIfLocal(IWebHostEnvironment env, string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            if (!url.StartsWith("/uploads/")) return;

            var relative = url.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var full = Path.Combine(env.WebRootPath, relative);

            if (File.Exists(full)) File.Delete(full);
        }
    }
}
