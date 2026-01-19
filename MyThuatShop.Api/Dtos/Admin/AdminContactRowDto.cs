namespace MyThuatShop.Api.Dtos.Admin;

public class AdminContactRowDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Message { get; set; }
    public string? Status { get; set; }
    public DateTime CreateAt { get; set; }
}

public record ReplyContactRequest(int ContactId, string Subject, string ReplyMessage);
