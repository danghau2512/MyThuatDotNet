using System.Text.Json.Serialization;

namespace MyThuatShop.Api.Dtos.Auth;

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = "";   
    public string Role { get; set; } = "Customer";
}
