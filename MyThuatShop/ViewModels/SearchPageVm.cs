namespace MyThuatShop.ViewModels
{
    public class SearchPageVm
    {
        public string Keyword { get; set; } = "";
        public string Sort { get; set; } = "all";
        public List<ProductCardVm> Products { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } =8;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }
}
