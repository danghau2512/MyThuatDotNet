using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos.Auth;
using MyThuatShop.Api.Models;
using System.Security.Cryptography;
using System.Text;
using MyThuatShop.Api.Utils;
using MyThuatShop.Api.Dtos.Users;
using MyThuatShop.Api.Dtos.Users;
using MyThuatShop.Api.Utils;
using Microsoft.EntityFrameworkCore;



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

        // ✅ THÊM: tạo randomKey
        var randomKey = MyUtils.keyGenerator();

        var user = new User
        {
            FullName = req.FullName.Trim(),
            Email = email,

            // ✅ ĐỔI: MD5(password + randomKey)
            Password = MyUtils.ToMd5Hash(req.Password, randomKey),
            RandomKey = randomKey,

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
            var hashedInput = MyUtils.ToMd5Hash(req.Password, user.RandomKey);

            if (!string.Equals(user.Password, hashedInput, StringComparison.OrdinalIgnoreCase))
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
    // ===== LOGIN GOOGLE =====
    [HttpPost("google-login")]
    public async Task<ActionResult<LoginResponseDto>> GoogleLogin([FromBody] GoogleLoginRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest("Email không hợp lệ.");

        var email = req.Email.Trim();

        // tìm user theo email
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);

        // nếu chưa có thì tạo mới
        if (user == null)
        {
            var randomPwd = Guid.NewGuid().ToString("N");
            var md5 = MD5.HashData(Encoding.UTF8.GetBytes(randomPwd));
            var pwdHash = Convert.ToHexString(md5).ToLower();

            user = new User
            {
                FullName = string.IsNullOrWhiteSpace(req.FullName) ? email : req.FullName.Trim(),
                Email = email,
                Password = "",          // Google login không cần password
                Role = "user",
                CreateAt = DateTime.Now,
                IsActive = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

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
    // profile
    // GET: api/users/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserProfileDto>> GetById(int id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (user == null) return NotFound("Không tìm thấy user.");

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Dob = user.Dob,
            Address = user.Address,
            Role = user.Role ?? "user"
        });
    }

    // PUT: api/users/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileRequestDto req)
    {
        if (req == null) return BadRequest("Body rỗng.");
        if (string.IsNullOrWhiteSpace(req.FullName)) return BadRequest("FullName là bắt buộc.");

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null) return NotFound("Không tìm thấy user.");

        user.FullName = req.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(req.PhoneNumber) ? null : req.PhoneNumber.Trim();
        user.Dob = req.Dob;
        user.Address = string.IsNullOrWhiteSpace(req.Address) ? null : req.Address.Trim();

        await _db.SaveChangesAsync();
        return NoContent();
    }
    // đổi mk
    // PUT: api/users/{id}/change-password
    [HttpPut("{id:int}/change-password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequestDto req)
    {
        if (req == null) return BadRequest("Body rỗng.");
        if (string.IsNullOrWhiteSpace(req.CurrentPassword)) return BadRequest("Mật khẩu hiện tại không được để trống.");
        if (string.IsNullOrWhiteSpace(req.NewPassword)) return BadRequest("Mật khẩu mới không được để trống.");
        if (req.NewPassword.Length < 6) return BadRequest("Mật khẩu mới tối thiểu 6 ký tự.");
        if (req.NewPassword != req.ConfirmNewPassword) return BadRequest("Xác nhận mật khẩu mới không khớp.");

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null) return NotFound("Không tìm thấy user.");

        // ✅ so MD5 + randomKey đúng y như login
        var currentHashed = MyUtils.ToMd5Hash(req.CurrentPassword, user.RandomKey);
        if (!string.Equals(user.Password, currentHashed, StringComparison.OrdinalIgnoreCase))
            return BadRequest("Mật khẩu hiện tại không đúng.");

        // ✅ đổi randomKey mới (khuyên dùng)
        user.RandomKey = MyUtils.keyGenerator(10);
        user.Password = MyUtils.ToMd5Hash(req.NewPassword, user.RandomKey);

        await _db.SaveChangesAsync();
        return NoContent();
    }


}
