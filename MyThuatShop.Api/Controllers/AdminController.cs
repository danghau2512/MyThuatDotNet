using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos.Admin;

namespace MyThuatShop.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly MyThuatDotNetContext _db;

    public AdminController(MyThuatDotNetContext db)
    {
        _db = db;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<AdminOverviewDto>> Overview()
    {
        // tìm id trạng thái "Hoàn thành" (nếu không có thì fallback = 3 giống JSP)
        int completedId = await _db.OrderStatuses.AsNoTracking()
            .Where(s => s.StatusName == "Hoàn thành")
            .Select(s => s.Id)
            .FirstOrDefaultAsync();

        if (completedId == 0) completedId = 3;

        var totalUsers = await _db.Users.AsNoTracking().CountAsync();
        var totalProducts = await _db.Products.AsNoTracking().CountAsync();

        var totalOrders = await _db.Orders.AsNoTracking()
            .CountAsync(o => o.OrderStatusId == completedId);

        var totalRevenue = await _db.Orders.AsNoTracking()
            .Where(o => o.OrderStatusId == completedId)
            .SumAsync(o => (decimal?)(o.TotalPrice - (o.Discount ?? 0m))) ?? 0m;

        // latestOrders (lấy giống JSP: hiển thị sản phẩm, trạng thái, tổng tiền...)
        var latestOrderEntities = await _db.Orders.AsNoTracking()
            .Include(o => o.OrderStatus)
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .OrderByDescending(o => o.CreateAt)
            .Take(10)
            .ToListAsync();

        var latestOrders = latestOrderEntities.Select(o => new LatestOrderDto
        {
            Id = o.Id,
            FullName = o.FullName,
            PhoneNumber = o.PhoneNumber,
            CreateAt = o.CreateAt,
            Address = o.Address,
            TotalPrice = o.TotalPrice,
            StatusName = o.OrderStatus?.StatusName,
            ProductNames = string.Join(", ",
                (o.OrderDetails ?? new List<Models.OrderDetail>())
                .Select(od => od.Product?.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
            )
        }).ToList();

        // topProductsMonth (theo tháng hiện tại)
        var now = DateTime.Now;
        var start = new DateTime(now.Year, now.Month, 1);
        var end = start.AddMonths(1);

        var top = await _db.OrderDetails.AsNoTracking()
            .Where(od =>
                od.Order != null &&
                od.Order.CreateAt >= start && od.Order.CreateAt < end &&
                od.Order.OrderStatusId == completedId
            )
            .GroupBy(od => od.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Qty)
            .Take(10)
            .ToListAsync();

        var prodIds = top.Select(x => x.ProductId).ToList();

        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => prodIds.Contains(p.Id))
            .ToListAsync();

        var map = products.ToDictionary(p => p.Id, p => p);

        var topProductsMonth = top.Select(x =>
        {
            map.TryGetValue(x.ProductId, out var p);
            return new TopProductMonthDto
            {
                Id = p?.Id ?? x.ProductId,
                Name = p?.Name,
                CategoryName = p?.Category?.CategoryName,
                Price = p?.Price ?? 0m,
                CreateAt = p?.CreateAt,
                SoldQty = x.Qty
            };
        }).ToList();

        return Ok(new AdminOverviewDto
        {
            TotalUsers = totalUsers,
            TotalProducts = totalProducts,
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            LatestOrders = latestOrders,
            TopProductsMonth = topProductsMonth
        });
    }
}
