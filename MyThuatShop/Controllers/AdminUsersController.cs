using Microsoft.AspNetCore.Mvc;
using MyThuatShop.Filters;
using MyThuatShop.Services;
using MyThuatShop.ViewModels;
using MyThuatShop.ViewModels.Admin;
using MyThuatShop.Dtos.Admin;

namespace MyThuatShop.Controllers;

[RequireAdmin]
[Route("admin/users")]
public class AdminUsersController : Controller
{
    private readonly AdminUserApiService _api;

    public AdminUsersController(AdminUserApiService api) => _api = api;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 10)
    {
        ViewData["Title"] = "Quản lý người dùng";
        ViewData["ActiveMenu"] = "users";

        var data = await _api.GetUsersAsync(q, page, pageSize) ?? new PagedResultDto<AdminUserRowDto>();

        var totalPages = (int)Math.Ceiling(data.TotalItems / (double)Math.Max(1, data.PageSize));

        var vm = new AdminUsersIndexVm
        {
            Q = q ?? "",
            Paged = new PagedResultVm<AdminUserRowVm>
            {
                Page = data.Page,
                PageSize = data.PageSize,
                TotalItems = data.TotalItems,
                TotalPages = totalPages <= 0 ? 1 : totalPages,
                Items = data.Items.Select(u => new AdminUserRowVm
                {
                    Id = u.Id,
                    FullName = u.FullName ?? "",
                    PhoneNumber = u.PhoneNumber ?? "",
                    Address = u.Address ?? "",
                    CreatedAt = u.CreateAt?.ToString("dd/MM/yyyy HH:mm") ?? "",
                    Dob = u.Dob?.ToString("yyyy-MM-dd") ?? "",
                    Role = (u.Role ?? "USER").ToUpper(),
                    IsActive = u.IsActive ?? true
                }).ToList()
            }
        };

        return View("~/Views/Admin/Users.cshtml", vm);
    }

    // ✅ GIỐNG JSP: 1 endpoint POST xử lý theo "action"
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(
        [FromForm] string action,
        [FromForm] int id,
        [FromForm] string fullName,
        [FromForm] string email,
        [FromForm] string? phoneNumber,
        [FromForm] string? address,
        [FromForm] DateOnly? dob,
        [FromForm] string role,
        [FromForm] string? q,
        [FromForm] int page = 1,
        [FromForm] int pageSize = 10)
    {
        action = (action ?? "").Trim().ToLower();

        if (action == "create")
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMsg"] = "Họ tên và email là bắt buộc.";
                return RedirectToAction(nameof(Index), new { q, page, pageSize });
            }

            var (ok, defaultPwd, msg) = await _api.CreateAsync(new CreateUserAdminRequestDto
            {
                FullName = fullName.Trim(),
                Email = email.Trim(),
                PhoneNumber = phoneNumber,
                Address = address,
                Dob = dob,
                Role = role
            });

            TempData[ok ? "SuccessMsg" : "ErrorMsg"] =
                ok ? $"Tạo user thành công. Mật khẩu mặc định: {defaultPwd}" : msg;
        }
        else if (action == "update")
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(fullName))
            {
                TempData["ErrorMsg"] = "Thiếu dữ liệu cập nhật.";
                return RedirectToAction(nameof(Index), new { q, page, pageSize });
            }

            var (ok, msg) = await _api.UpdateAsync(id, new UpdateUserAdminRequestDto
            {
                FullName = fullName.Trim(),
                PhoneNumber = phoneNumber,
                Address = address,
                Dob = dob,
                Role = role
            });

            TempData[ok ? "SuccessMsg" : "ErrorMsg"] = ok ? "Cập nhật thành công." : msg;
        }
        else if (action == "lock" || action == "unlock")
        {
            if (id <= 0)
            {
                TempData["ErrorMsg"] = "Thiếu id user.";
                return RedirectToAction(nameof(Index), new { q, page, pageSize });
            }

            var isActive = action == "unlock";
            var (ok, msg) = await _api.SetActiveAsync(id, isActive);

            TempData[ok ? "SuccessMsg" : "ErrorMsg"] =
                ok ? (isActive ? "Mở khóa thành công." : "Khóa thành công.") : msg;
        }

        return RedirectToAction(nameof(Index), new { q, page, pageSize });
    }
    [HttpGet("ajax")]
    public async Task<IActionResult> Ajax(string? q, int page = 1, int pageSize = 10)
    {
        var data = await _api.GetUsersAsync(q, page, pageSize) ?? new PagedResultDto<AdminUserRowDto>();

        var totalPages = (int)Math.Ceiling(data.TotalItems / (double)Math.Max(1, data.PageSize));
        if (totalPages <= 0) totalPages = 1;

        // trả về đúng format để JS render
        return Json(new
        {
            q = q ?? "",
            page = data.Page,
            pageSize = data.PageSize,
            totalItems = data.TotalItems,
            totalPages,
            items = data.Items.Select(u => new
            {
                id = u.Id,
                fullName = u.FullName ?? "",
                phoneNumber = u.PhoneNumber ?? "",
                address = u.Address ?? "",
                createdAt = u.CreateAt?.ToString("dd/MM/yyyy HH:mm") ?? "",
                dob = u.Dob?.ToString("yyyy-MM-dd") ?? "",
                role = (u.Role ?? "USER").ToUpper(),
                isActive = u.IsActive ?? true
            })
        });
    }
}
