namespace MyThuatShop.Api.Dtos.Contacts;

public class ContactDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Message { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? CreateAt { get; set; }
}
