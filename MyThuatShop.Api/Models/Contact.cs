using System;
using System.Collections.Generic;

namespace MyThuatShop.Api.Models;

public partial class Contact
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Message { get; set; }

    public string? Status { get; set; }

    public DateTime? CreateAt { get; set; }

    public virtual User? User { get; set; }
}
