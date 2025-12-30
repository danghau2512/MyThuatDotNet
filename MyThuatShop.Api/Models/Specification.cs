using System;
using System.Collections.Generic;

namespace MyThuatShop.Api.Models;

public partial class Specification
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string? Brand { get; set; }

    public string? Size { get; set; }

    public string? Standard { get; set; }

    public string? MadeIn { get; set; }

    public string? Warning { get; set; }

    public virtual Product Product { get; set; } = null!;
}
