namespace MyThuatShop.Api.Dtos
{
    public class CategorySectionDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public string? Thumbnail { get; set; }
        public List<ProductCardDto> Products { get; set; } = new();
    }
}
