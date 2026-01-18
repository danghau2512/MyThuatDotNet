using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyThuatShop.Models
{
    [Table("categories")]
    public class Category
    {
        [Key]
        [Column("ID")] // nếu DB của bạn là ID (hoa)
        public int Id { get; set; }

        [Column("categoryName")]
        public string CategoryName { get; set; } = "";

        [Column("thumbnail")]
        public string? Thumbnail { get; set; }

        [Column("isActive")]
        public sbyte IsActive { get; set; } = 1; // tinyint(1)
    }
}
