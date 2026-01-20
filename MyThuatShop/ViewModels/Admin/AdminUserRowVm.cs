namespace MyThuatShop.ViewModels.Admin;

public class AdminUserRowVm
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Address { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public string Dob { get; set; } = "";
    public string Role { get; set; } = "USER";
    public bool IsActive { get; set; } = true;
}
