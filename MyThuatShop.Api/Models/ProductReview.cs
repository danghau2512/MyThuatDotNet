using System;
using System.Collections.Generic;

namespace MyThuatShop.Api.Models;

public partial class ProductReview
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreateAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
