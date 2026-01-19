namespace MyThuatShop.ViewModels.Account;

public class ProfileVm
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public DateOnly? Dob { get; set; }
    public string? Address { get; set; }
}
