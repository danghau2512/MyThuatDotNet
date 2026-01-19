using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos.Admin;
using MyThuatShop.Api.Services;

namespace MyThuatShop.Api.Controllers;

[ApiController]
[Route("api/admin/contacts")]
public class AdminContactsController : ControllerBase
{
    private readonly MyThuatDotNetContext _db;
    private readonly IEmailSender _emailSender;

    public AdminContactsController(MyThuatDotNetContext db, IEmailSender emailSender)
    {
        _db = db;
        _emailSender = emailSender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _db.Contacts.AsNoTracking()
            .OrderByDescending(c => c.CreateAt)
            .Select(c => new AdminContactRowDto
            {
                Id = c.Id,
                UserId = c.UserId,
                FullName = c.FullName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                Message = c.Message,
                Status = c.Status,
                CreateAt = (DateTime)c.CreateAt,
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpPost("reply")]
    public async Task<IActionResult> Reply([FromBody] ReplyContactRequest req)
    {
        if (req.ContactId <= 0) return BadRequest("ContactId không hợp lệ.");
        if (string.IsNullOrWhiteSpace(req.ReplyMessage)) return BadRequest("Nội dung phản hồi không được rỗng.");

        var contact = await _db.Contacts.FirstOrDefaultAsync(c => c.Id == req.ContactId);
        if (contact == null) return NotFound("Không tìm thấy liên hệ.");

        if (string.IsNullOrWhiteSpace(contact.Email))
            return BadRequest("Liên hệ này không có email.");

        // Gửi email (HTML)
        var subject = string.IsNullOrWhiteSpace(req.Subject) ? "Phản hồi liên hệ" : req.Subject.Trim();
        var html = $@"
            <div style='font-family:Roboto,Arial,sans-serif'>
              <h3>Phản hồi từ cửa hàng</h3>
              <p><b>Xin chào {System.Net.WebUtility.HtmlEncode(contact.FullName)}</b>,</p>
              <p>{System.Net.WebUtility.HtmlEncode(req.ReplyMessage).Replace("\n", "<br/>")}</p>
              <hr/>
              <p style='color:#666'><b>Nội dung bạn đã gửi:</b><br/>
              {System.Net.WebUtility.HtmlEncode(contact.Message ?? "").Replace("\n", "<br/>")}</p>
            </div>";

        await _emailSender.SendHtmlAsync(contact.Email!, subject, html); // dùng SmtpEmailSender của bạn :contentReference[oaicite:2]{index=2}

        contact.Status = "Đã phản hồi";
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, contactId = req.ContactId, status = contact.Status });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var contact = await _db.Contacts.FirstOrDefaultAsync(c => c.Id == id);
        if (contact == null) return NotFound("Không tìm thấy liên hệ.");

        _db.Contacts.Remove(contact);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, contactId = id });
    }
}
