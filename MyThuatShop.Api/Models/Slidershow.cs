using System;
using System.Collections.Generic;

namespace MyThuatShop.Api.Models;

public partial class Slidershow
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public string Thumbnail { get; set; } = null!;

    public bool? Status { get; set; }

    public int? IndexOrder { get; set; }

    public string? LinkTo { get; set; }
}
