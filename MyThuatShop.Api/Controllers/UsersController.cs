using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos.Auth;
using MyThuatShop.Api.Dtos.Users;
using MyThuatShop.Api.Models;
using MyThuatShop.Api.Services;   // ✅ IEmailSender
using MyThuatShop.Api.Utils;
using System.Security.Cryptography;
using System.Text;
using MyThuatShop.Api.Dtos;
using MyThuatShop.Api.Dtos.Users;

namespace MyThuatShop.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly MyThuatDotNetContext _db;
    private readonly IEmailSender _emailSender;

    public UsersController(MyThuatDotNetContext db, IEmailSender emailSender) // ✅ nhận emailSender
    {
        _db = db;
        _emailSender = emailSender; // ✅ gán đúng
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
    public class ForgotPasswordRequestDto
    {
        public string Email { get; set; } = "";
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Email))
            return BadRequest("Vui lòng nhập email.");

        var email = req.Email.Trim();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null) return BadRequest("Email không tồn tại.");

        // Nếu IsActive của bạn là int (0/1/3) thì dùng dòng này:
        // if (user.IsActive == 3) return BadRequest("Tài khoản đang bị khóa.");

        // Nếu IsActive của bạn là bool? thì dùng kiểu này:
        // if (user.IsActive != true) return BadRequest("Tài khoản chưa kích hoạt hoặc đang bị khóa.");

        var newPlain = GenerateTempPassword(10);

        user.RandomKey = MyUtils.keyGenerator(10);
        user.Password = MyUtils.ToMd5Hash(newPlain, user.RandomKey);

        await _db.SaveChangesAsync();

        var html = $@"
            <p>Mật khẩu mới của bạn là: <b>{newPlain}</b></p>
            <p>Vui lòng đăng nhập và đổi mật khẩu ngay sau khi đăng nhập.</p>";

        await _emailSender.SendHtmlAsync(user.Email, "Đặt lại mật khẩu - MyThuatShop", html);

        return Ok("Mật khẩu mới đã được gửi về email.");
    }

    private static string GenerateTempPassword(int len)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789@#$!";
        var sb = new StringBuilder(len);
        var rng = new Random();
        for (int i = 0; i < len; i++)
            sb.Append(chars[rng.Next(chars.Length)]);
        return sb.ToString();
    }
    // ql nguoi dung
    // GET: /api/users/admin?q=&page=1&pageSize=10
    [HttpGet("admin")]
    public async Task<ActionResult<PagedResultDto<AdminUserItemDto>>> AdminListUsers(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(u =>
                (u.FullName != null && u.FullName.Contains(q)) ||
                (u.Email != null && u.Email.Contains(q)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(q)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserItemDto
            {
                Id = u.Id,
                FullName = u.FullName,
                PhoneNumber = u.PhoneNumber,
                Address = u.Address,
                CreatedAt = u.CreateAt.HasValue ? u.CreateAt.Value.ToString("yyyy-MM-ddTHH:mm:ss") : null,
                Dob = u.Dob.HasValue ? u.Dob.Value.ToString("yyyy-MM-dd") : null,
                Role = (u.Role ?? "USER").ToUpper(),
                IsActive = u.IsActive
            })
            .ToListAsync();

        return Ok(new PagedResultDto<AdminUserItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        });
    }
    public class SetActiveDto
    {
        public bool IsActive { get; set; }
    }

    // PUT: /api/users/admin/{id}/set-active
    [HttpPut("admin/{id:int}/set-active")]
    public async Task<IActionResult> AdminSetActive(int id, [FromBody] SetActiveDto req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null) return NotFound("Không tìm thấy user.");

        user.IsActive = req.IsActive;
        await _db.SaveChangesAsync();
        return NoContent();
    }


}
