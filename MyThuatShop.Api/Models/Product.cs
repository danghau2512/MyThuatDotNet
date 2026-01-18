using System;
using System.Collections.Generic;

namespace MyThuatShop.Api.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // ✅ Price nên nullable nếu DB cho null, còn nếu DB NOT NULL thì để decimal
    public decimal Price { get; set; }

    public int? DiscountDefault { get; set; }

    public int CategoryId { get; set; }

    public string? Thumbnail { get; set; }

    public int? QuantityStock { get; set; }

    public int? SoldQuantity { get; set; }

    public string? Status { get; set; }

    public DateTime? CreateAt { get; set; }

    public string? Brand { get; set; }

    public bool IsActive { get; set; } = true;

    // ✅ Navigation: nếu CategoryId NOT NULL thì Category không null
    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public virtual ICollection<Specification> Specifications { get; set; } = new List<Specification>();

    public virtual ICollection<Subimage> Subimages { get; set; } = new List<Subimage>();
}
