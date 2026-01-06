using System.ComponentModel.DataAnnotations;

namespace MyThuatShop.ViewModels.Auth;

public class LoginVm
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ (vd: ten@gmail.com)")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    public string Password { get; set; } = "";
}
