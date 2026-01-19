namespace MyThuatShop.Dtos.Admin
{
    public class AdminProductDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int DiscountDefault { get; set; }
        public int CategoryId { get; set; }
        public string? Thumbnail { get; set; }
        public int QuantityStock { get; set; }
        public string? Brand { get; set; }
        public int IsActive { get; set; }
    }

    public class AdminProductUpsertDto
    {
        public int CategoryId { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int DiscountDefault { get; set; }
        public int QuantityStock { get; set; }
        public string? Brand { get; set; }

        public string? Size { get; set; }
        public string? Standard { get; set; }
        public string? MadeIn { get; set; }
        public string? Warning { get; set; }
    }
}
