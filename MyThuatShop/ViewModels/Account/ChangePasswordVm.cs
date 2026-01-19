using System.ComponentModel.DataAnnotations;

namespace MyThuatShop.ViewModels.Account
{
    public class ChangePasswordVm
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(8, ErrorMessage = "Mật khẩu mới tối thiểu 8 ký tự.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Xác nhận mật khẩu mới không khớp.")]
        [DataType(DataType.Password)]
        public string ConfirmNewPassword { get; set; } = "";
    }
}
