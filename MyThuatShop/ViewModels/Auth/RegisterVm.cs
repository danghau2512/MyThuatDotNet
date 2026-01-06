using System.ComponentModel.DataAnnotations;

namespace MyThuatShop.ViewModels.Auth;

public class RegisterVm
{
    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ (vd: ten@gmail.com)")]
    public string Email { get; set; } = "";

    // Không bắt buộc nhập, nhưng nếu nhập thì phải đúng định dạng 10 số bắt đầu bằng 0
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "SĐT không hợp lệ (vd: 0912345678)")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*[^\w\s]).{8,}$",
        ErrorMessage = "Mật khẩu có ít nhất 8 ký tự, có chữ hoa, chữ thường và ký tự đặc biệt."
    )]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}
