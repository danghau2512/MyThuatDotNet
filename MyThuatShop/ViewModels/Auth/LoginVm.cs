using System.ComponentModel.DataAnnotations;

namespace MyThuatShop.ViewModels.Auth;

public class LoginVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}
