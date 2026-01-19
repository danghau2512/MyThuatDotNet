using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos.Admin;

namespace MyThuatShop.Api.Controllers;

[ApiController]
[Route("api/admin/orders")]
public class AdminOrdersController : ControllerBase
{
    private readonly MyThuatDotNetContext _db;
    public AdminOrdersController(MyThuatDotNetContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null)
    {
        var q = _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderStatus)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
            .OrderByDescending(o => o.CreateAt);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim();
            q = q.Where(o => o.OrderStatus.StatusName == s)
                 .OrderByDescending(o => o.CreateAt);
        }

        var data = await q.Select(o => new AdminOrderRowDto
        {
            Id = o.Id,
            FullName = o.FullName,
            PhoneNumber = o.PhoneNumber,
            CreateAt = o.CreateAt,
            Address = o.Address,
            TotalPrice = o.TotalPrice,
            StatusId = o.OrderStatusId,
            StatusName = o.OrderStatus.StatusName,
                

            Items = o.OrderDetails.Select(d => new AdminOrderItemDto
            {
                ProductId = d.ProductId,
                ProductName = d.Product.Name,
                Quantity = d.Quantity,
                UnitPrice = d.Price
            }).ToList(),

            // fallback nếu cần
            ProductNames = string.Join(", ", o.OrderDetails.Select(d => d.Product.Name))
        }).ToListAsync();

        return Ok(data);
    }

    [HttpPost("update-info")]
    public async Task<IActionResult> UpdateInfo([FromBody] UpdateOrderInfoRequest req)
    {
        if (req.OrderId <= 0) return BadRequest("OrderId không hợp lệ.");

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == req.OrderId);
        if (order == null) return NotFound("Không tìm thấy đơn.");

        order.FullName = (req.FullName ?? "").Trim();
        order.PhoneNumber = (req.PhoneNumber ?? "").Trim();
        order.Address = (req.Address ?? "").Trim();

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
    [HttpPost("change-status")]
    public async Task<IActionResult> ChangeStatus([FromBody] ChangeOrderStatusRequest req)
    {
        if (req.OrderId <= 0) return BadRequest("OrderId không hợp lệ.");
        if (req.StatusId is < 1 or > 4) return BadRequest("StatusId không hợp lệ.");

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == req.OrderId);
        if (order == null) return NotFound("Không tìm thấy đơn.");

        var current = order.OrderStatusId;
        var target = req.StatusId;

        var ok =
            (current == 1 && (target == 2 || target == 4)) ||
            (current == 2 && target == 3);

        if (!ok) return BadRequest($"Không thể chuyển trạng thái từ {current} sang {target}.");

        // kiểm tra status tồn tại
        var st = await _db.OrderStatuses.FirstOrDefaultAsync(s => s.Id == target);
        if (st == null) return BadRequest("Trạng thái không tồn tại trong DB.");

        order.OrderStatusId = target;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
