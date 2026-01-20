namespace MyThuatShop.Api.Dtos.Users;

public class AdminUserItemDto
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? CreatedAt { get; set; }  // để hiện giống ảnh: 2026-01-10T10:01:17
    public string? Dob { get; set; }        // để hiện giống ảnh: 2025-12-25
    public string? Role { get; set; }       // USER/ADMIN
    public bool? IsActive { get; set; }     // true = đang hoạt động, false = bị khóa
}
