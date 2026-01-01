namespace MyThuatShop.ViewModels
{
    public class ProductSuggestVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }

        public string? ThumbnailUrl { get; set; }
    }
}
