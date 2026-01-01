namespace MyThuatShop.ViewModels
{
    public class CartProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int DiscountDefault { get; set; }
        public string? Thumbnail { get; set; }
        public int QuantityStock { get; set; }
        public bool IsActive { get; set; }
    }
}
