namespace MyThuatShop.ViewModels
{
    public class ProductDetailVm
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

        public int QuantityStock { get; set; }
        public int SoldQuantity { get; set; }

        public bool IsActive { get; set; }
        public DateTime? CreateAt { get; set; }

        public List<string> SubImages { get; set; } = new();
        public List<SpecificationVm> Specifications { get; set; } = new();

        public double AvgRating { get; set; }
        public int ReviewCount { get; set; }
        public List<ReviewVm> Reviews { get; set; } = new();
        public List<RelatedProductVm> RelatedProducts { get; set; } = new();
    }

    public class SpecificationVm
    {
        public string? Brand { get; set; }
        public string? Size { get; set; }
        public string? Standard { get; set; }
        public string? MadeIn { get; set; }
        public string? Warning { get; set; }
    }

    public class ReviewVm
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreateAt { get; set; }
        public string? UserFullName { get; set; }
    
}

    public class RelatedProductVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Thumbnail { get; set; }
        public decimal Price { get; set; }
        public int DiscountDefault { get; set; }
        public decimal FinalPrice { get; set; }
        public int SoldQuantity { get; set; }
    }
}
