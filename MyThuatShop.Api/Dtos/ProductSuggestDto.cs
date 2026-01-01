namespace MyThuatShop.Api.Dtos
{
    public class ProductSuggestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string? ThumbnailUrl { get; set; }
    }
}
