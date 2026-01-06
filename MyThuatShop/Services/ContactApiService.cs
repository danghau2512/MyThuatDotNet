using System.Net.Http;
using System.Net.Http.Json;

namespace MyThuatShop.Services
{
    public class ContactApiService
    {
        private readonly HttpClient _http;

        // Nếu bạn muốn, có thể chuyển sang đọc từ appsettings.
        private const string BaseUrl = "https://localhost:7090";

        public ContactApiService(HttpClient http)
        {
            _http = http;
        }

        // ===== DTOs =====
        public class ContactCreateRequestDto
        {
            public int UserId { get; set; }
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string PhoneNumber { get; set; } = "";
            public string Message { get; set; } = "";
        }

        public class ContactReplyRequestDto
        {
            public string Subject { get; set; } = "";
            public string ReplyMessage { get; set; } = "";
        }

        public class ContactDto
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string PhoneNumber { get; set; } = "";
            public string Message { get; set; } = "";
            public string Status { get; set; } = "";
            public DateTime CreateAt { get; set; }
        }

        // ===== API calls =====

        public async Task<(bool ok, string? err)> CreateAsync(ContactCreateRequestDto req)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/contacts", req);
                if (resp.IsSuccessStatusCode) return (true, null);

                var body = await resp.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(body) ? resp.ReasonPhrase : body);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(List<ContactDto> items, string? err)> GetAllAsync()
        {
            try
            {
                var resp = await _http.GetAsync($"{BaseUrl}/api/contacts");
                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    var err = string.IsNullOrWhiteSpace(body) ? resp.ReasonPhrase : body;
                    return (new List<ContactDto>(), err);
                }

                var data = await resp.Content.ReadFromJsonAsync<List<ContactDto>>();
                return (data ?? new List<ContactDto>(), null);
            }
            catch (Exception ex)
            {
                return (new List<ContactDto>(), ex.Message);
            }
        }

        public async Task<(bool ok, string? err)> DeleteAsync(int id)
        {
            try
            {
                var resp = await _http.DeleteAsync($"{BaseUrl}/api/contacts/{id}");
                if (resp.IsSuccessStatusCode) return (true, null);

                var body = await resp.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(body) ? resp.ReasonPhrase : body);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        // FIX: gọi đúng route API: /api/contacts/{id}/reply
        public async Task<(bool ok, string? err)> ReplyAsync(int id, string subject, string replyMessage)
        {
            try
            {
                var payload = new ContactReplyRequestDto
                {
                    Subject = subject ?? "",
                    ReplyMessage = replyMessage ?? ""
                };

                var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/contacts/{id}/reply", payload);
                if (resp.IsSuccessStatusCode) return (true, null);

                var body = await resp.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(body) ? resp.ReasonPhrase : body);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
