using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos.Contacts;
using MyThuatShop.Api.Services;
using MyThuatShop.Api.Models;
using MyThuatShop.Api.Dtos.Contacts;

namespace MyThuatShop.Api.Controllers;

[ApiController]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
    private readonly MyThuatDotNetContext _db;
    private readonly IEmailSender _email;

    public ContactsController(MyThuatDotNetContext db, IEmailSender email)
    {
        _db = db;
        _email = email;
    }

    // USER: tạo contact
    [HttpPost]
    public async Task<ActionResult<ContactDto>> Create([FromBody] ContactCreateRequestDto req)
    {
        if (req.UserId <= 0) return BadRequest("UserId không hợp lệ.");
        if (string.IsNullOrWhiteSpace(req.Message)) return BadRequest("Message không được rỗng.");

        var contact = new Contact
        {
            UserId = req.UserId,
            FullName = req.FullName ?? "",
            Email = req.Email ?? "",
            PhoneNumber = req.PhoneNumber ?? "",
            Message = req.Message ?? "",
            Status = "Chưa phản hồi",
            CreateAt = DateTime.Now,
        };

        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync();

        return Ok(new ContactDto
        {
            Id = contact.Id,
            UserId = contact.UserId ?? 0,
            FullName = contact.FullName,
            Email = contact.Email,
            PhoneNumber = contact.PhoneNumber,
            Message = contact.Message,
            Status = contact.Status ?? "",
            CreateAt = contact.CreateAt
        });
    }

    // ADMIN: list contacts
    [HttpGet]
    public async Task<ActionResult<List<ContactDto>>> GetAll()
    {
        var list = await _db.Contacts
            .AsNoTracking()
            .OrderByDescending(x => x.CreateAt)
            .Select(x => new ContactDto
            {
                Id = x.Id,
                UserId = x.UserId ?? 0,              
                FullName = x.FullName ?? "",
                Email = x.Email ?? "",
                PhoneNumber = x.PhoneNumber ?? "",   
                Message = x.Message ?? "",
                Status = x.Status ?? "",
                CreateAt = x.CreateAt
            })

            .ToListAsync();

        return Ok(list);
    }

    // ADMIN: delete
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Contacts.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();

        _db.Contacts.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ADMIN: reply -> send email + update status
    [HttpPost("{id:int}/reply")]
    public async Task<IActionResult> Reply(int id, [FromBody] ContactReplyRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.ReplyMessage))
            return BadRequest("Vui lòng nhập nội dung phản hồi!");

        var c = await _db.Contacts.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound("Không tìm thấy liên hệ.");

        if (string.IsNullOrWhiteSpace(c.Email))
            return BadRequest("Email rỗng. Không thể phản hồi.");

        var subject = string.IsNullOrWhiteSpace(req.Subject)
            ? "Phản hồi liên hệ - MyThuatShop"
            : req.Subject;

        await _email.SendHtmlAsync(c.Email, subject, req.ReplyMessage);

        c.Status = "Đã phản hồi";
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
