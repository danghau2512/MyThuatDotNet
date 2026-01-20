using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MyThuatShop.Services
{
    public class AdminProductApiService
    {
        private readonly HttpClient _http;

        public AdminProductApiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<ProductDto>> GetAllAsync()
        {
            var res = await _http.GetAsync("api/admin/products");
            if (!res.IsSuccessStatusCode) return new();

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ProductDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }

        public async Task<bool> CreateAsync(ProductUpsertVm vm, IFormFile? thumbnailMain, List<IFormFile>? thumbnailSubs)
        {
            using var form = BuildMultipart(vm, thumbnailMain, thumbnailSubs);
            var res = await _http.PostAsync("api/admin/products/create", form);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAsync(ProductUpsertVm vm, IFormFile? thumbnailMain, List<IFormFile>? thumbnailSubs)
        {
            using var form = BuildMultipart(vm, thumbnailMain, thumbnailSubs);
            var res = await _http.PostAsync("api/admin/products/update", form);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> ToggleActiveAsync(int id, bool isActive)
        {
           
            var payload = JsonSerializer.Serialize(new { id, isActive });
            var res = await _http.PostAsync("api/admin/products/setActive",
                new StringContent(payload, Encoding.UTF8, "application/json"));
            return res.IsSuccessStatusCode;
        }
        public async Task<bool> SetActiveAsync(int id, bool newIsActive)
        {
            var payload = JsonSerializer.Serialize(new { id, isActive = newIsActive });
            var res = await _http.PostAsync("api/admin/products/setActive",
                new StringContent(payload, Encoding.UTF8, "application/json"));
            return res.IsSuccessStatusCode;
        }


        private MultipartFormDataContent BuildMultipart(ProductUpsertVm vm, IFormFile? main, List<IFormFile>? subs)
        {
            var form = new MultipartFormDataContent();

            void Add(string key, string? value) => form.Add(new StringContent(value ?? ""), key);

            Add("Id", vm.Id.ToString());
            Add("CategoryId", vm.CategoryId.ToString());
            Add("Name", vm.Name);
            Add("Price", vm.Price.ToString());
            Add("DiscountDefault", vm.DiscountDefault.ToString());
            Add("QuantityStock", vm.QuantityStock.ToString());
            Add("Brand", vm.Brand);
            Add("Content", vm.Content);
            Add("Size", vm.Size);
            Add("Standard", vm.Standard);
            Add("MadeIn", vm.MadeIn);
            Add("Warning", vm.Warning);

            Add("RemoveThumbnail", vm.RemoveThumbnail ? "1" : "0");

            if (main != null && main.Length > 0)
            {
                var sc = new StreamContent(main.OpenReadStream());
                sc.Headers.ContentType = new MediaTypeHeaderValue(main.ContentType);
                form.Add(sc, "ThumbnailMain", main.FileName);
            }

            if (subs != null && subs.Count > 0)
            {
                foreach (var f in subs.Where(x => x != null && x.Length > 0))
                {
                    var sc = new StreamContent(f.OpenReadStream());
                    sc.Headers.ContentType = new MediaTypeHeaderValue(f.ContentType);
                    form.Add(sc, "ThumbnailSubs", f.FileName);
                }
            }

            return form;
        }
    }

    // ===== ViewModel post lên MVC controller =====
    public class ProductUpsertVm
    {
        public int Id { get; set; }               // update
        public int CategoryId { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int DiscountDefault { get; set; }
        public int QuantityStock { get; set; }
        public string? Brand { get; set; }
        public string? Content { get; set; }

        public string? Size { get; set; }
        public string? Standard { get; set; }
        public string? MadeIn { get; set; }
        public string? Warning { get; set; }

        public bool RemoveThumbnail { get; set; } // giống categories
    }

    // ===== DTO nhận từ API =====
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int? DiscountDefault { get; set; }
        public int CategoryId { get; set; }
        public string? Thumbnail { get; set; }
        public int? QuantityStock { get; set; }
        public int? SoldQuantity { get; set; }
        public string? Status { get; set; }
        public DateTime? CreateAt { get; set; }
        public string? Brand { get; set; }
        public bool IsActive { get; set; }
        public string? Content { get; set; }


        public List<SubimageDto> Subimages { get; set; } = new();
        public List<SpecificationDto> Specifications { get; set; } = new();
    }

    public class SubimageDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Image { get; set; } = "";
    }

    public class SpecificationDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? Size { get; set; }
        public string? Standard { get; set; }
        public string? MadeIn { get; set; }
        public string? Warning { get; set; }
    }
}
