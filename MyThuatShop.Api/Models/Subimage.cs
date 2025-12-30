using System;
using System.Collections.Generic;

namespace MyThuatShop.Api.Models;

public partial class Subimage
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string Image { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
