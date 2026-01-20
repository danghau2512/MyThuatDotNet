namespace MyThuatShop.Dtos.Admin;

public class PagedResultDto<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public List<T> Items { get; set; } = new();
}

public class AdminUserRowDto
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateOnly? Dob { get; set; }
    public DateTime? CreateAt { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateUserAdminRequestDto
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateOnly? Dob { get; set; }
    public string Role { get; set; } = "USER";
}

public class UpdateUserAdminRequestDto
{
    public string FullName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateOnly? Dob { get; set; }
    public string Role { get; set; } = "USER";
}

public class SetUserActiveRequestDto
{
    public bool IsActive { get; set; }
}
