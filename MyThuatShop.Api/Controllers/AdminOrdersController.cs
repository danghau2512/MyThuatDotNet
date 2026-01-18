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

    [HttpPost("status")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateOrderStatusRequest req)
    {
        if (req.OrderId <= 0) return BadRequest("OrderId không hợp lệ.");
        if (string.IsNullOrWhiteSpace(req.StatusName)) return BadRequest("Thiếu StatusName.");

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == req.OrderId);
        if (order == null) return NotFound("Không tìm thấy đơn.");

        var st = await _db.OrderStatuses.FirstOrDefaultAsync(s => s.StatusName == req.StatusName.Trim());
        if (st == null) return BadRequest("Trạng thái không tồn tại trong DB.");

        order.OrderStatusId = st.Id;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
