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

    [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    public string ConfirmPassword { get; set; } = "";

    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
}
