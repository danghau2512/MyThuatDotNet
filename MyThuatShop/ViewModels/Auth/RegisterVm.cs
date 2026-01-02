using System.ComponentModel.DataAnnotations;

namespace MyThuatShop.ViewModels.Auth;

public class RegisterVm
{
    [Required]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? PhoneNumber { get; set; }
}
