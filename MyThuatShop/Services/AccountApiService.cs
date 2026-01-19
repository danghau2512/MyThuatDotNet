using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MyThuatShop.ViewModels.Auth;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyThuatShop.Services;

public class AccountApiService
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;
    public string BaseUrl { get; }

    // Bạn chỉ cần đăng ký DI:
    // builder.Services.AddHttpClient<AccountApiService>();
    public AccountApiService(HttpClient http, IConfiguration config)
    {
        _http = http;

        BaseUrl = (config["ApiBaseUrl"] ?? "https://localhost:7090").TrimEnd('/');
        _http.BaseAddress = new Uri(BaseUrl + "/");

        // Json options (có converter DateOnly để chạy ổn cả .NET 6/7/8)
        _json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        _json.Converters.Add(new DateOnlyJsonConverter());
        _json.Converters.Add(new NullableDateOnlyJsonConverter());
    }

    // ===================== AUTH =====================

    // POST: /api/users/login
    public async Task<LoginResponseDto?> LoginAsync(string email, string password)
    {
        try
        {
            var req = new LoginRequestDto
            {
                Email = email?.Trim() ?? "",
                Password = password ?? ""
            };

            var resp = await _http.PostAsJsonAsync("api/users/login", req, _json);
            if (!resp.IsSuccessStatusCode) return null;

            return await resp.Content.ReadFromJsonAsync<LoginResponseDto>(_json);
        }
        catch
        {
            return null;
        }
    }

    // POST: /api/users/google-login
    public async Task<LoginResponseDto?> GoogleLoginAsync(string email, string fullName)
    {
        try
        {
            var req = new GoogleLoginRequestDto
            {
                Email = email?.Trim() ?? "",
                FullName = fullName ?? ""
            };

            var resp = await _http.PostAsJsonAsync("api/users/google-login", req, _json);
            if (!resp.IsSuccessStatusCode) return null;

            return await resp.Content.ReadFromJsonAsync<LoginResponseDto>(_json);
        }
        catch
        {
            return null;
        }
    }

    // ===================== PROFILE =====================

    // GET: /api/users/{id}
    public async Task<UserProfileDto?> GetProfileAsync(int userId)
    {
        try
        {
            var resp = await _http.GetAsync($"api/users/{userId}");
            if (!resp.IsSuccessStatusCode) return null;

            return await resp.Content.ReadFromJsonAsync<UserProfileDto>(_json);
        }
        catch
        {
            return null;
        }
    }

    // PUT: /api/users/{id}
    public async Task<(bool ok, string message)> UpdateProfileAsync(int userId, UpdateProfileRequestDto req)
    {
        try
        {
            var resp = await _http.PutAsJsonAsync($"api/users/{userId}", req, _json);
            if (resp.IsSuccessStatusCode) return (true, "");

            var msg = await resp.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(msg) ? "Cập nhật thất bại." : msg);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // ===================== DTOs =====================

    public class LoginRequestDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class GoogleLoginRequestDto
    {
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
    }

    public class LoginResponseDto
    {
        public UserDto User { get; set; } = new();
        public string? Message { get; set; } // nếu API có trả
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public DateOnly? Dob { get; set; }
        public string? Address { get; set; }
        public int? IsActive { get; set; } // nếu API có
    }

    // ===== 2 DTO bạn đưa (giữ nguyên) =====

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

    public class UpdateProfileRequestDto
    {
        public string FullName { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public DateOnly? Dob { get; set; }
        public string? Address { get; set; }
    }

    public class ChangePasswordRequestDto
    {
        public string CurrentPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public string ConfirmNewPassword { get; set; } = "";
    }
    // register
    public class RegisterRequestDto
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string Password { get; set; } = "";
    }

    // ===================== DateOnly Json Converters =====================

    private sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        private const string Format = "yyyy-MM-dd";

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // chấp nhận "yyyy-MM-dd"
            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s))
                return default;

            if (DateOnly.TryParse(s, out var d)) return d;

            // fallback: nếu server trả DateTime string
            if (DateTime.TryParse(s, out var dt)) return DateOnly.FromDateTime(dt);

            throw new JsonException($"Không parse được DateOnly từ '{s}'.");
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(Format));
    }

    private sealed class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
    {
        private const string Format = "yyyy-MM-dd";

        public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s)) return null;

            if (DateOnly.TryParse(s, out var d)) return d;
            if (DateTime.TryParse(s, out var dt)) return DateOnly.FromDateTime(dt);

            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
        {
            if (value == null) writer.WriteNullValue();
            else writer.WriteStringValue(value.Value.ToString(Format));
        }
    }
    public async Task<(bool ok, string message)> ChangePasswordAsync(int userId, ChangePasswordRequestDto req)
    {
        try
        {
            var resp = await _http.PutAsJsonAsync($"api/users/{userId}/change-password", req, _json);
            if (resp.IsSuccessStatusCode) return (true, "");

            var msg = await resp.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(msg) ? "Đổi mật khẩu thất bại." : msg);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
    // register
    public async Task<(bool ok, string message)> RegisterAsync(RegisterRequestDto req)
    {
        try
        {
            // ⚠️ SỬA đúng route API đăng ký của bạn
            var resp = await _http.PostAsJsonAsync("api/users/register", req, _json);

            if (resp.IsSuccessStatusCode) return (true, "");

            var msg = await resp.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(msg) ? "Đăng ký thất bại." : msg);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
    // quen mk
    //public async Task<(bool ok, string message)> ForgotPasswordAsync(string email)
    //{
    //    var res = await _http.PostAsJsonAsync("/api/account/forgot-password", new { Email = email });

    //    var msg = await res.Content.ReadAsStringAsync();
    //    if (res.IsSuccessStatusCode) return (true, string.IsNullOrWhiteSpace(msg) ? "Đã gửi mật khẩu mới về email." : msg);

    //    return (false, string.IsNullOrWhiteSpace(msg) ? "Không thể xử lý yêu cầu." : msg);
    //}
    public async Task<(bool ok, string message)> ForgotPasswordAsync(string email)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("/api/users/forgot-password",
                new { email = email?.Trim() });

            var msg = await res.Content.ReadAsStringAsync();

            if (res.IsSuccessStatusCode)
                return (true, string.IsNullOrWhiteSpace(msg) ? "Mật khẩu mới đã được gửi về email." : msg);

            return (false, string.IsNullOrWhiteSpace(msg) ? "Không thể xử lý yêu cầu." : msg);
        }
        catch (Exception ex)
        {
            return (false, "Lỗi gọi API: " + ex.Message);
        }
    }


}
