namespace MyThuatShop.Api.Dtos
{
    public class AdminProductUpsertForm
    {
        public int Id { get; set; } // update

        public int CategoryId { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int? DiscountDefault { get; set; }
        public int? QuantityStock { get; set; }
        public string? Brand { get; set; }
        public string? Content { get; set; } // ckeditor html

        public string? Size { get; set; }
        public string? Standard { get; set; }
        public string? MadeIn { get; set; }
        public string? Warning { get; set; }

        public int RemoveThumbnail { get; set; } // 1/0

        public IFormFile? ThumbnailMain { get; set; }
        public List<IFormFile>? ThumbnailSubs { get; set; }
    }

    public class ToggleActiveDto
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
    }
}
