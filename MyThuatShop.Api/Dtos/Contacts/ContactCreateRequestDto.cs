namespace MyThuatShop.Api.Dtos.Contacts;

public class ContactCreateRequestDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Message { get; set; } = "";
}
