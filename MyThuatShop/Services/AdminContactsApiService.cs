using System.Net.Http.Json;
using System.Text.Json;
using MyThuatShop.Dtos.Admin;

namespace MyThuatShop.Services;

public class AdminContactsApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public AdminContactsApiService(HttpClient http) => _http = http;

    public async Task<(List<AdminContactRowDto>? data, string? err)> GetAllAsync()
    {
        try
        {
            var resp = await _http.GetAsync("/api/admin/contacts");
            if (!resp.IsSuccessStatusCode) return (null, await resp.Content.ReadAsStringAsync());
            var data = await resp.Content.ReadFromJsonAsync<List<AdminContactRowDto>>(_json);
            return (data ?? new(), null);
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public record ReplyRequest(int ContactId, string Subject, string ReplyMessage);

    public async Task<(bool ok, string? err)> ReplyAsync(int contactId, string subject, string replyMessage)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("/api/admin/contacts/reply",
                new ReplyRequest(contactId, subject ?? "", replyMessage ?? ""));
            if (!resp.IsSuccessStatusCode) return (false, await resp.Content.ReadAsStringAsync());
            return (true, null);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool ok, string? err)> DeleteAsync(int id)
    {
        try
        {
            var resp = await _http.DeleteAsync($"/api/admin/contacts/{id}");
            if (!resp.IsSuccessStatusCode) return (false, await resp.Content.ReadAsStringAsync());
            return (true, null);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }
}
