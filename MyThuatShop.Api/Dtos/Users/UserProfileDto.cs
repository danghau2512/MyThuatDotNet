namespace MyThuatShop.Api.Dtos.Users;

public class UserProfileDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public DateOnly? Dob { get; set; }
    public string? Address { get; set; }
    public string Role { get; set; } = "user";
}
