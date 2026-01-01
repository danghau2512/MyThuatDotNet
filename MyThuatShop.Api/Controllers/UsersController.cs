using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos.Auth;
using MyThuatShop.Api.Models; // chỉnh namespace theo DbContext của bạn
using System.Security.Cryptography;
using System.Text;


namespace MyThuatShop.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly MyThuatDotNetContext _db; // đổi theo DbContext thật của bạn

    public UsersController(MyThuatDotNetContext db)
    {
        _db = db;
    }
    private static string HashPassword(string password)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hashBytes = md5.ComputeHash(bytes);

        var sb = new StringBuilder();
        foreach (var b in hashBytes)
            sb.Append(b.ToString("X2")); // uppercase giống mẫu
        return sb.ToString();
    }


    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Email và mật khẩu không được để trống.");

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == req.Email);

        if (user == null)
            return Unauthorized("Sai email hoặc mật khẩu.");

        if (req.Password != user.Password)
            return Unauthorized("Sai email hoặc mật khẩu.");



        var res = new LoginResponseDto
        {
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role ?? "Customer"
            }
        };

        return Ok(res);
    }
}
