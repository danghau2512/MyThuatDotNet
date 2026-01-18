namespace MyThuatShop.Api.Dtos.Users;

public class UpdateProfileRequestDto
{
    public string FullName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public DateOnly? Dob { get; set; }
    public string? Address { get; set; }
}
