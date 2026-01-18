using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos.Admin;

namespace MyThuatShop.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminStatisticsController : ControllerBase
{
    private readonly MyThuatDotNetContext _db;
    private const int COMPLETED_STATUS_ID = 3; // y chang JSP

    public AdminStatisticsController(MyThuatDotNetContext db)
    {
        _db = db;
    }

    // GET: /api/admin/statistics?noSaleMonths=1
    [HttpGet("statistics")]
    public async Task<ActionResult<AdminStatisticsDto>> Statistics([FromQuery] int noSaleMonths = 1)
    {
        noSaleMonths = Math.Clamp(noSaleMonths, 1, 12);

        // ====== YEAR RANGE (JSP: MAKEDATE(YEAR(CURDATE()),1) .. MAKEDATE(YEAR(CURDATE())+1,1)) ======
        var now = DateTime.Now;
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0);
        var yearEnd = yearStart.AddYears(1);

        // ====== TOTAL YEAR: SUM(totalPrice) (JSP: totalRevenueThisYear) ======
        var totalYear = await _db.Orders.AsNoTracking()
            .Where(o => o.OrderStatusId == COMPLETED_STATUS_ID
                        && o.CreateAt >= yearStart
                        && o.CreateAt < yearEnd)
            .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;

        // ====== REV BY MONTH (JSP: LEFT JOIN 1..12 để đủ 12 tháng) ======
        var revAgg = await _db.Orders.AsNoTracking()
            .Where(o => o.OrderStatusId == COMPLETED_STATUS_ID
                        && o.CreateAt >= yearStart
                        && o.CreateAt < yearEnd)
            .GroupBy(o => o.CreateAt!.Value.Month)
            .Select(g => new { Month = g.Key, Revenue = g.Sum(x => x.TotalPrice) })
            .ToListAsync();

        var revDict = revAgg.ToDictionary(x => x.Month, x => x.Revenue);

        var revYear = Enumerable.Range(1, 12)
            .Select(m => new RevenueMonthDto
            {
                Month = m,
                Revenue = revDict.TryGetValue(m, out var v) ? v : 0m
            })
            .ToList();

        // ====== SOLD ALL TIME (status=3) ======
        var soldAll = await _db.OrderDetails.AsNoTracking()
            .Where(od => od.Order != null && od.Order.OrderStatusId == COMPLETED_STATUS_ID)
            .GroupBy(od => od.ProductId)
            .Select(g => new { ProductId = g.Key, SoldQty = g.Sum(x => x.Quantity) })
            .ToListAsync();

        var soldAllDict = soldAll.ToDictionary(x => x.ProductId, x => x.SoldQty);

        // ====== LOAD PRODUCT BASE (join category) ======
        var productBase = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Select(p => new
            {
                p.Id,
                p.Name,
                CategoryName = p.Category != null ? p.Category.CategoryName : null,
                p.Price,
                p.CreateAt
            })
            .ToListAsync();

        // ====== BEST TABLE (JSP: bestSellersAllTime ORDER BY soldQty DESC) ======
        var bestTable = productBase
            .Select(p => new BestSellerRowDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                CategoryName = p.CategoryName,
                Price = p.Price,
                CreateAt = p.CreateAt,
                SoldQty = soldAllDict.TryGetValue(p.Id, out var qty) ? qty : 0
            })
            .OrderByDescending(x => x.SoldQty)
            .ToList();

        // ====== BEST CHART TOP 5 (JSP: LIMIT 5 ORDER BY soldQty DESC) ======
        var bestChart = bestTable
            .Take(5)
            .Select(x => new BestSellerChartPointDto
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                SoldQty = x.SoldQty
            })
            .ToList();

        // ====== NO SALE RANGE (JSP: startOfMonthRange / endOfMonthRange) ======
        // startOfMonthRange(months): first day of THIS month - (months-1) months, at 00:00
        // endOfMonthRange(): first day of NEXT month at 00:00
        var firstOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
        var from = firstOfThisMonth.AddMonths(-(noSaleMonths - 1));
        var to = firstOfThisMonth.AddMonths(1);

        var soldRange = await _db.OrderDetails.AsNoTracking()
            .Where(od =>
                od.Order != null
                && od.Order.OrderStatusId == COMPLETED_STATUS_ID
                && od.Order.CreateAt >= from
                && od.Order.CreateAt < to
            )
            .GroupBy(od => od.ProductId)
            .Select(g => new { ProductId = g.Key, SoldQuantity = g.Sum(x => x.Quantity) })
            .ToListAsync();

        var soldRangeDict = soldRange.ToDictionary(x => x.ProductId, x => x.SoldQuantity);

        // noSaleProducts: HAVING soldQuantity=0 ORDER BY p.createAt DESC
        var noSaleTable = productBase
            .Select(p => new NoSaleRowDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                CategoryName = p.CategoryName,
                Price = p.Price,
                CreateAt = p.CreateAt,
                SoldQuantity = soldRangeDict.TryGetValue(p.Id, out var qty) ? qty : 0
            })
            .Where(x => x.SoldQuantity == 0)
            .OrderByDescending(x => x.CreateAt)
            .ToList();

        return Ok(new AdminStatisticsDto
        {
            TotalYear = totalYear,
            RevYear = revYear,
            BestTable = bestTable,
            BestChart = bestChart,
            NoSaleTable = noSaleTable
        });
    }
}
