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

        public async Task<SearchPageVm> SearchProductsPagedAsync(string q, string sort = "all", int page = 1, int pageSize = 8)
        {
            q = (q ?? "").Trim();
            if (q.Length == 0) return new SearchPageVm { Keyword = q, Sort = sort, Page = page, PageSize = pageSize };

            var url = $"/api/search/products?q={Uri.EscapeDataString(q)}&sort={sort}&page={page}&pageSize={pageSize}";

            // nhận dạng object { page, pageSize, totalItems, totalPages, items }
            var res = await _http.GetFromJsonAsync<PagedResultVm<ProductCardVm>>(url);

            return new SearchPageVm
            {
                Keyword = q,
                Sort = sort,
                Page = res?.Page ?? page,
                PageSize = res?.PageSize ?? pageSize,
                TotalItems = res?.TotalItems ?? 0,
                TotalPages = res?.TotalPages ?? 0,
                Products = res?.Items ?? new()
            };
        }

        

    }
}
