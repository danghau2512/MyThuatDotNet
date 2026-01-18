namespace MyThuatShop.Api.Dtos.Users;

public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public string ConfirmNewPassword { get; set; } = "";
}
