namespace MyThuatShop.Api.Dtos
{
    public class RelatedProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Thumbnail { get; set; }
        public decimal Price { get; set; }
        public int DiscountDefault { get; set; }
        public decimal FinalPrice { get; set; }
        public int SoldQuantity { get; set; }
        public bool IsActive { get; set; }

    }

    public class ProductDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int DiscountDefault { get; set; }
        public decimal FinalPrice { get; set; }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";

        public string? Thumbnail { get; set; }
        public string? Brand { get; set; }
        public string? Status { get; set; }
        public string? Content { get; set; }

        public int QuantityStock { get; set; }
        public int SoldQuantity { get; set; }

        public List<string> SubImages { get; set; } = new();
        public List<SpecificationDto> Specifications { get; set; } = new();

        public double AvgRating { get; set; }
        public int ReviewCount { get; set; }
        public List<ReviewDto> Reviews { get; set; } = new();

        public List<RelatedProductDto> RelatedProducts { get; set; } = new();
        public bool IsActive { get; set; }

    }
}
