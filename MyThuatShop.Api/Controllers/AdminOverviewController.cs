using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos.AdminOverview;

namespace MyThuatShop.Api.Controllers;

[ApiController]
[Route("api/admin/overview")]
public class AdminOverviewController : ControllerBase
{
    private readonly MyThuatDotNetContext _db;

    public AdminOverviewController(MyThuatDotNetContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<AdminOverviewDto>> Get([FromQuery] int latest = 10, [FromQuery] int top = 10)
    {
        // Lưu ý: bạn cần có các DbSet tương ứng: Users, Products, Orders, OrderDetails, OrderStatuses, Categories...
        // Nếu tên entity/cột khác, bạn đổi lại cho khớp dự án bạn.

        var totalUsers = await _db.Users.AsNoTracking().CountAsync();
        var totalProducts = await _db.Products.AsNoTracking().CountAsync();

        const int COMPLETED = 3;

        var totalOrders = await _db.Orders.AsNoTracking()
            .CountAsync(o => o.OrderStatusId == COMPLETED);

        var totalRevenue = await _db.Orders.AsNoTracking()
            .Where(o => o.OrderStatusId == COMPLETED)
            .SumAsync(o => (decimal?)(o.TotalPrice - o.Discount)) ?? 0m;

        // ===== Latest orders (giống latestOrders + GROUP_CONCAT productNames) =====
        var latestOrdersEntities = await _db.Orders.AsNoTracking()
            .Include(o => o.OrderStatus) // navigation status
            .Include(o => o.OrderDetails)!.ThenInclude(od => od.Product)
            .OrderByDescending(o => o.CreateAt)
            .Take(latest)
            .ToListAsync();

        var latestOrders = latestOrdersEntities.Select(o => new OverviewOrderRowDto
        {
            Id = o.Id,
            FullName = o.FullName,
            PhoneNumber = o.PhoneNumber,
            CreateAt = o.CreateAt,
            Address = o.Address,
            TotalPrice = (decimal)o.TotalPrice,
            StatusName = o.OrderStatus?.StatusName,
            ProductNames = o.OrderDetails == null
                ? ""
                : string.Join(", ",
                    o.OrderDetails
                        .Select(od => od.Product?.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct())
        }).ToList();

        // ===== Top products this month (giống topProductsThisMonth) =====
        var now = DateTime.Now;
        var startMonth = new DateTime(now.Year, now.Month, 1);
        var startNextMonth = startMonth.AddMonths(1);

        var topSold = await _db.OrderDetails.AsNoTracking()
            .Where(od =>
                od.Order!.OrderStatusId == COMPLETED &&
                od.Order.CreateAt >= startMonth &&
                od.Order.CreateAt < startNextMonth)
            .GroupBy(od => od.ProductId)
            .Select(g => new { ProductId = g.Key, SoldQty = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.SoldQty)
            .Take(top)
            .ToListAsync();

        var productIds = topSold.Select(x => x.ProductId).ToList();

        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        var topProducts = topSold
            .Join(products,
                s => s.ProductId,
                p => p.Id,
                (s, p) => new OverviewTopProductRowDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    CategoryName = p.Category?.CategoryName,
                    Price = (decimal)p.Price,
                    CreateAt = p.CreateAt,
                    SoldQty = s.SoldQty
                })
            .ToList();

        return new AdminOverviewDto
        {
            TotalUsers = totalUsers,
            TotalProducts = totalProducts,
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            LatestOrders = latestOrders,
            TopProductsMonth = topProducts
        };
    }
}
