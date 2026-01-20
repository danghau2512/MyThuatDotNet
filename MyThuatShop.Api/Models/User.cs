using System;
using System.Collections.Generic;

namespace MyThuatShop.Api.Models;

public partial class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public DateOnly? Dob { get; set; }


    public string? Address { get; set; }

    public string? Role { get; set; }

    public DateTime? CreateAt { get; set; }

    public bool? IsActive { get; set; }

    public string? RandomKey { get; set; }


    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
}
