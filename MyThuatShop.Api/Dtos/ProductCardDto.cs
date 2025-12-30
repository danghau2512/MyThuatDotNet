namespace MyThuatShop.Api.Dtos
{
    public class ProductCardDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int DiscountDefault { get; set; }
        public string? Thumbnail { get; set; }
        public int SoldQuantity { get; set; }
    }
}
