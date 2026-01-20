using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos.Admin;
using MyThuatShop.Api.Models;
using MyThuatShop.Api.Utils;

namespace MyThuatShop.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly MyThuatDotNetContext _db;
    public AdminUsersController(MyThuatDotNetContext db) => _db = db;

    // GET: /api/admin/users?q=&page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AdminUserRowDto>>> GetUsers(
        [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(u =>
                (u.FullName ?? "").Contains(q) ||
                (u.Email ?? "").Contains(q) ||
                (u.PhoneNumber ?? "").Contains(q));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserRowDto
            {
                Id = u.Id,
                FullName = u.FullName,
                PhoneNumber = u.PhoneNumber,
                Address = u.Address,
                Dob = u.Dob,
                CreateAt = u.CreateAt,
                Role = u.Role,
                IsActive = u.IsActive
            })
            .ToListAsync();

        return Ok(new PagedResultDto<AdminUserRowDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            Items = items
        });
    }

    // PUT: /api/admin/users/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserAdminRequestDto req)
    {
        if (req == null) return BadRequest("Body rỗng.");
        if (string.IsNullOrWhiteSpace(req.FullName)) return BadRequest("Họ và tên là bắt buộc.");

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null) return NotFound("Không tìm thấy user.");

        user.FullName = req.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(req.PhoneNumber) ? null : req.PhoneNumber.Trim();
        user.Address = string.IsNullOrWhiteSpace(req.Address) ? null : req.Address.Trim();
        user.Dob = req.Dob;

        // role lưu giống hình: USER/ADMIN
        user.Role = string.IsNullOrWhiteSpace(req.Role) ? "USER" : req.Role.Trim();

        await _db.SaveChangesAsync();
        return NoContent();
    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserAdminRequestDto req)
    {
        if (req == null) return BadRequest("Body rỗng.");
        if (string.IsNullOrWhiteSpace(req.FullName)) return BadRequest("Họ và tên là bắt buộc.");
        if (string.IsNullOrWhiteSpace(req.Email)) return BadRequest("Email là bắt buộc.");

        var email = req.Email.Trim();

        var existed = await _db.Users.AnyAsync(x => x.Email == email);
        if (existed) return Conflict("Email đã tồn tại.");

        // ✅ GIỐNG BÊN JSP: modal không nhập password -> dùng password mặc định
        var defaultPassword = "123456";
        var randomKey = MyUtils.keyGenerator();

        var user = new User
        {
            FullName = req.FullName.Trim(),
            Email = email,
            PhoneNumber = string.IsNullOrWhiteSpace(req.PhoneNumber) ? null : req.PhoneNumber.Trim(),
            Address = string.IsNullOrWhiteSpace(req.Address) ? null : req.Address.Trim(),
            Dob = req.Dob,
            Role = string.IsNullOrWhiteSpace(req.Role) ? "USER" : req.Role.Trim(),
            CreateAt = DateTime.Now,
            IsActive = true,
            RandomKey = randomKey,
            Password = MyUtils.ToMd5Hash(defaultPassword, randomKey)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Trả về để MVC hiển thị "mật khẩu mặc định"
        return Ok(new { user.Id, DefaultPassword = defaultPassword });
    }

    // POST: /api/admin/users/{id}/active  (khóa/mở khóa)
    [HttpPost("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetUserActiveRequestDto req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null) return NotFound("Không tìm thấy user.");

        user.IsActive = req.IsActive;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
