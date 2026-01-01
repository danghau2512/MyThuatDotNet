namespace MyThuatShop.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }            // giá gốc
        public int DiscountDefault { get; set; }      // %
        public string? Thumbnail { get; set; }
        public int Quantity { get; set; }

        public decimal GetPriceAfterDiscount()
        {
            return Price * (1 - (DiscountDefault / 100m));
        }

        public decimal SubTotal => GetPriceAfterDiscount() * Quantity;
    }
}
