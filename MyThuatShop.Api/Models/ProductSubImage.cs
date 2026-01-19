using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyThuatShop.Api.Models
{
    public class ProductSubImage
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [MaxLength(500)]
        public string Image { get; set; } = ""; 

        public Product? Product { get; set; }
    }
}
