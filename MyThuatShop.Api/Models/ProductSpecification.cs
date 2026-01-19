using System.ComponentModel.DataAnnotations;

namespace MyThuatShop.Api.Models
{
    public class ProductSpecification
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string? Size { get; set; }
        public string? Standard { get; set; }
        public string? MadeIn { get; set; }
        public string? Warning { get; set; }

        public Product? Product { get; set; }
    }
}
