using MyThuatShop.ViewModels;

namespace MyThuatShop.Services
{
    public class SearchApiService
    {
        private readonly HttpClient _http;
        public SearchApiService(HttpClient http)
        {
            _http = http;
        }
        public async Task<List<ProductSuggestVm>> SuggestAsync(string keyword, int take = 8)
        {
            keyword = (keyword ?? "").Trim();
            if (keyword.Length < 2) return new List<ProductSuggestVm>();

            var url = $"/api/search/suggest?keyword={Uri.EscapeDataString(keyword)}&take={take}";
            var data = await _http.GetFromJsonAsync<List<ProductSuggestVm>>(url);
            return data ?? new List<ProductSuggestVm>();
        }
    }
}
