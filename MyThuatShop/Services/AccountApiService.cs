using MyThuatShop.ViewModels.Auth;
using System.Net;
using System.Net.Http.Json;
using MyThuatShop.Dtos.Auth;

namespace MyThuatShop.Services;

public class AccountApiService
{
    private readonly HttpClient _http;

    // chỉnh baseUrl theo API của bạn
    private const string BaseUrl = "https://localhost:7090";

    public AccountApiService(HttpClient http)
    {
        _http = http;
    }
    // ===== LOGIN GOOGLE =====
    public async Task<LoginResponseDto?> GoogleLoginAsync(string email, string fullName)
    {
        var payload = new
        {
            email,
            fullName
        };

        // nhớ đúng base url của API bạn đang dùng
        var res = await _http.PostAsJsonAsync("api/users/google-login", payload);

        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<LoginResponseDto>();
    }

    public async Task<LoginResponseDto?> LoginAsync(string email, string password)
    {
        var url = $"{BaseUrl}/api/users/login";
        var resp = await _http.PostAsJsonAsync(url, new { email, password });

        if (!resp.IsSuccessStatusCode) return null;

        return await resp.Content.ReadFromJsonAsync<LoginResponseDto>();
    }

    public async Task<(bool ok, string message)> RegisterAsync(RegisterVm vm)
    {
        var url = $"{BaseUrl}/api/users/register";
        var resp = await _http.PostAsJsonAsync(url, new
        {
            fullName = vm.FullName,
            email = vm.Email,
            password = vm.Password,
            phoneNumber = vm.PhoneNumber,
           
        });

        if (resp.IsSuccessStatusCode)
            return (true, "Đăng ký thành công!");

        if (resp.StatusCode == HttpStatusCode.Conflict)
            return (false, "Email đã tồn tại.");

        var msg = await resp.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(msg) ? "Đăng ký thất bại." : msg);
    }

    // DTO dùng bên MVC (có thể đặt trong folder Dtos của MVC)
    public class LoginResponseDto
    {
        public UserDto User { get; set; } = new();
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Role { get; set; } = "Customer";
    }
}
