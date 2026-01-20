using System.Net.Http.Json;
using MyThuatShop.Dtos.Admin;

namespace MyThuatShop.Services;

public class AdminUserApiService
{
    private readonly HttpClient _http;

    public AdminUserApiService(HttpClient http) => _http = http;

    public async Task<PagedResultDto<AdminUserRowDto>?> GetUsersAsync(string? q, int page, int pageSize)
    {
        var url = $"/api/admin/users?q={Uri.EscapeDataString(q ?? "")}&page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<PagedResultDto<AdminUserRowDto>>(url);
    }

    public async Task<(bool ok, string msg)> UpdateAsync(int id, UpdateUserAdminRequestDto req)
    {
        var res = await _http.PutAsJsonAsync($"/api/admin/users/{id}", req);
        var msg = await res.Content.ReadAsStringAsync();
        return (res.IsSuccessStatusCode, string.IsNullOrWhiteSpace(msg) ? "OK" : msg);
    }

    public async Task<(bool ok, string defaultPassword, string msg)> CreateAsync(CreateUserAdminRequestDto req)
    {
        var res = await _http.PostAsJsonAsync("/api/admin/users", req);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            return (false, "", string.IsNullOrWhiteSpace(err) ? "Tạo user thất bại." : err);
        }

        // API trả { id, defaultPassword }
        try
        {
            var obj = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            var pwd = obj != null && obj.TryGetValue("defaultPassword", out var v) ? (v?.ToString() ?? "") : "";
            return (true, pwd, "OK");
        }
        catch
        {
            return (true, "123456", "OK"); // fallback
        }
    }

    public async Task<(bool ok, string msg)> SetActiveAsync(int id, bool isActive)
    {
        var res = await _http.PostAsJsonAsync($"/api/admin/users/{id}/active", new SetUserActiveRequestDto
        {
            IsActive = isActive
        });

        var msg = await res.Content.ReadAsStringAsync();
        return (res.IsSuccessStatusCode, string.IsNullOrWhiteSpace(msg) ? "OK" : msg);
    }
}
