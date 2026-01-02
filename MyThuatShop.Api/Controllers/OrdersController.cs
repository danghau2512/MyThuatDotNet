using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos.Orders;
using MyThuatShop.Api.Models;

namespace MyThuatShop.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly MyThuatDotNetContext _db;
    public OrdersController(MyThuatDotNetContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequestDto req)
    {
        if (req.UserId <= 0) return BadRequest("UserId không hợp lệ.");
        if (req.Items == null || req.Items.Count == 0) return BadRequest("Giỏ hàng trống.");
        if (string.IsNullOrWhiteSpace(req.FullName)) return BadRequest("Thiếu họ tên.");

        // 1) kiểm tra user tồn tại
        var userExists = await _db.Users.AnyAsync(u => u.Id == req.UserId);
        if (!userExists) return BadRequest("User không tồn tại.");

        // 2) payment + status (fallback giống JSP)
        var paymentName = string.IsNullOrWhiteSpace(req.PaymentName) ? "COD" : req.PaymentName.Trim();
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.PaymentName == paymentName)
                   ?? await _db.Payments.FirstOrDefaultAsync(p => p.PaymentName == "COD")
                   ?? await _db.Payments.FirstOrDefaultAsync();

        if (payment == null) return BadRequest("Chưa có dữ liệu Payments trong DB.");

        var status = await _db.OrderStatuses.FirstOrDefaultAsync(s => s.StatusName == "Đang xử lý")
                  ?? await _db.OrderStatuses.FirstOrDefaultAsync();

        if (status == null) return BadRequest("Chưa có dữ liệu Order_Statuses trong DB.");

        // 3) load sản phẩm 1 lần
        var ids = req.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        if (products.Count != ids.Count) return Conflict("Có sản phẩm không tồn tại.");

        // 4) validate tồn + active + tính tiền
        decimal itemsTotal = 0m;
        var details = new List<OrderDetail>();

        foreach (var it in req.Items)
        {
            var p = products.First(x => x.Id == it.ProductId);

            if (!(p.IsActive ?? false))
                return Conflict($"Sản phẩm '{p.Name}' đã ngừng bán.");

            var stock = p.QuantityStock ?? 0;
            if (stock <= 0)
                return Conflict($"Sản phẩm '{p.Name}' đã hết hàng.");

            if (it.Quantity < 1) it.Quantity = 1;
            if (it.Quantity > stock)
                return Conflict($"Sản phẩm '{p.Name}' chỉ còn {stock}.");

            var discount = p.DiscountDefault ?? 0;
            var unitPrice = p.Price * (1m - discount / 100m); // giá sau giảm
            itemsTotal += unitPrice * it.Quantity;

            details.Add(new OrderDetail
            {
                ProductId = p.Id,
                Quantity = it.Quantity,
                Price = unitPrice
            });
        }

        var total = itemsTotal - req.Discount + req.ShippingFee;
        if (total < 0) total = 0;

        // 5) transaction + trừ kho + tạo order
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var order = new Order
            {
                UserId = req.UserId,
                FullName = req.FullName.Trim(),
                Email = req.Email?.Trim(),
                PhoneNumber = req.PhoneNumber?.Trim(),
                Address = req.Address?.Trim(),
                Note = req.Note?.Trim(),

                PaymentId = payment.Id,
                OrderStatusId = status.Id,

                VoucherId = req.VoucherId,
                Discount = req.Discount,
                ShippingFee = req.ShippingFee,

                TotalPrice = total,
                PaymentStatus = "Chưa thanh toán",
                CreateAt = DateTime.Now
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var d in details)
            {
                d.OrderId = order.Id;
                _db.OrderDetails.Add(d);

                // trừ kho + tăng sold
                var p = products.First(x => x.Id == d.ProductId);
                p.QuantityStock = (p.QuantityStock ?? 0) - d.Quantity;
                p.SoldQuantity = (p.SoldQuantity ?? 0) + d.Quantity;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { success = true, orderId = order.Id });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var order = await _db.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        return Ok(new
        {
            order.Id,
            order.FullName,
            order.Email,
            order.PhoneNumber,
            order.Address,
            order.TotalPrice,
            order.ShippingFee,
            order.Discount,
            order.PaymentStatus,
            order.CreateAt,
            Items = order.OrderDetails.Select(d => new
            {
                d.ProductId,
                d.Quantity,
                UnitPrice = d.Price,
                ProductName = d.Product.Name,
                Thumbnail = d.Product.Thumbnail
            })
        });
    }
}
