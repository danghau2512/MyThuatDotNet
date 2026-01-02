using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos.Auth;
using MyThuatShop.Api.Models;
using System.Security.Cryptography;
using System.Text;

namespace MyThuatShop.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly MyThuatDotNetContext _db;

    public UsersController(MyThuatDotNetContext db)
    {
        _db = db;
    }

    // MD5 hex lowercase
    private static string Md5Hex(string input)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(input ?? "");
        var hash = md5.ComputeHash(bytes);

        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto req)
    {
        if (req == null) return BadRequest("Body rỗng.");

        if (string.IsNullOrWhiteSpace(req.FullName) ||
            string.IsNullOrWhiteSpace(req.Email) ||
            string.IsNullOrWhiteSpace(req.Password))
        {
            return BadRequest("FullName, Email, Password là bắt buộc.");
        }

        var email = req.Email.Trim();

        var existed = await _db.Users.AnyAsync(x => x.Email == email);
        if (existed) return Conflict("Email đã tồn tại.");

        var user = new User
        {
            FullName = req.FullName.Trim(),
            Email = email,
            Password = Md5Hex(req.Password),  // ✅ lưu MD5

            PhoneNumber = string.IsNullOrWhiteSpace(req.PhoneNumber) ? null : req.PhoneNumber.Trim(),
           
            Dob = req.Dob,

            Role = "user",
            CreateAt = DateTime.Now,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Created("", new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.Role
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto req)
    {
        if (req == null) return BadRequest("Body rỗng.");

        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Email và mật khẩu không được để trống.");

        var email = req.Email.Trim();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user == null) return Unauthorized("Sai email hoặc mật khẩu.");

        // ✅ so MD5
        var inputHash = Md5Hex(req.Password);
        if (!string.Equals(inputHash, user.Password, StringComparison.OrdinalIgnoreCase))
            return Unauthorized("Sai email hoặc mật khẩu.");

        if (user.IsActive.HasValue && user.IsActive.Value == false)
            return Unauthorized("Tài khoản đang bị khóa.");

        return Ok(new LoginResponseDto
        {
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role ?? "user"
            }
        });
    }
}
