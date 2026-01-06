namespace MyThuatShop.Api.Dtos.Auth;

public class GoogleLoginRequestDto
{
    public string Email { get; set; } = "";
    public string? FullName { get; set; }
}
