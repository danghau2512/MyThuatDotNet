using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos;
using MyThuatShop.Api.Dtos.Admin;

namespace MyThuatShop.Api.Controllers;

[ApiController]
[Route("api/admin/vouchers")]
public class AdminVouchersController : ControllerBase
{
    private readonly MyThuatDotNetContext _db;
    public AdminVouchersController(MyThuatDotNetContext db) => _db = db;

    // GET: /api/admin/vouchers?keyword=&page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<AdminPagedResultDto<AdminVoucherRowDto>>> Get(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var q = (keyword ?? "").Trim();

        var query = _db.Vouchers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(v =>
                (v.Code ?? "").Contains(q) ||
                (v.Name ?? "").Contains(q));
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        var items = await query
     .OrderByDescending(x => x.Id)
     .Skip((page - 1) * pageSize)
     .Take(pageSize)
     .Select(v => new AdminVoucherRowDto
     {
         Id = v.Id,
         Code = v.Code,
         Name = v.Name,
         Description = v.Description,
         StartDate = v.StartDate,
         EndDate = v.EndDate,
         VoucherCash = v.VoucherCash,
         MinOrderValue = v.MinOrderValue,
         Quantity = v.Quantity,
         QuantityUsed = v.QuantityUsed,
         IsActive = (v.IsActive ?? false) ? 1 : 0
     })
     .ToListAsync();

        return Ok(new AdminPagedResultDto<AdminVoucherRowDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = totalPages <= 0 ? 1 : totalPages,
            Items = items
        });

    }

    // POST: /api/admin/vouchers (action=create/update/delete)  -> giống JSP
    [HttpPost]
    public async Task<IActionResult> Post(
        [FromForm] string? action,
        [FromForm] int id,

        [FromForm] string? code,
        [FromForm] string? name,
        [FromForm] string? description,

        [FromForm] DateTime startDate,
        [FromForm] DateTime endDate,

        [FromForm] decimal voucherCash,
        [FromForm] decimal minOrderValue,

        [FromForm] int quantity,
        [FromForm] int quantityUsed,
        [FromForm] int isActive
    )
    {
        action = (action ?? "").Trim().ToLowerInvariant();

        if (action == "create")
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { ok = false, message = "Mã khuyến mãi không được để trống." });

            code = code.Trim();

            var exists = await _db.Vouchers.AnyAsync(x => x.Code == code);
            if (exists)
                return BadRequest(new { ok = false, message = "Mã khuyến mãi đã tồn tại." });

            if (endDate < startDate)
                return BadRequest(new { ok = false, message = "Ngày kết thúc phải >= ngày bắt đầu." });

            if (quantity < 0) quantity = 0;
            if (quantityUsed < 0) quantityUsed = 0;

            _db.Vouchers.Add(new MyThuatShop.Api.Models.Voucher
            {
                Code = code,
                Name = name,
                Description = description,
                StartDate = startDate,
                EndDate = endDate,
                VoucherCash = voucherCash,
                MinOrderValue = minOrderValue,
                Quantity = quantity,
                QuantityUsed = quantityUsed,
                IsActive = isActive == 1
            });

            await _db.SaveChangesAsync();
            return Ok(new { ok = true });
        }

        if (action == "update")
        {
            if (id <= 0) return BadRequest(new { ok = false, message = "Thiếu id." });
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { ok = false, message = "Mã khuyến mãi không được để trống." });

            code = code.Trim();

            var v = await _db.Vouchers.FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return NotFound(new { ok = false, message = "Không tìm thấy khuyến mãi." });

            var exists = await _db.Vouchers.AnyAsync(x => x.Code == code && x.Id != id);
            if (exists)
                return BadRequest(new { ok = false, message = "Mã khuyến mãi đã tồn tại." });

            if (endDate < startDate)
                return BadRequest(new { ok = false, message = "Ngày kết thúc phải >= ngày bắt đầu." });

            v.Code = code;
            v.Name = name;
            v.Description = description;
            v.StartDate = startDate;
            v.EndDate = endDate;
            v.VoucherCash = voucherCash;
            v.MinOrderValue = minOrderValue;
            v.Quantity = quantity;
            v.QuantityUsed = quantityUsed;
            v.IsActive = isActive == 1;

            await _db.SaveChangesAsync();
            return Ok(new { ok = true });
        }

        if (action == "delete")
        {
            if (id <= 0) return BadRequest(new { ok = false, message = "Thiếu id." });

            var v = await _db.Vouchers.FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return NotFound(new { ok = false, message = "Không tìm thấy khuyến mãi." });

            _db.Vouchers.Remove(v);

            try
            {
                await _db.SaveChangesAsync();
                return Ok(new { ok = true });
            }
            catch
            {
                return BadRequest(new { ok = false, message = "Không thể xóa vì voucher đang được dùng bởi đơn hàng." });
            }
        }

        return BadRequest(new { ok = false, message = "Invalid action" });
    }
}
