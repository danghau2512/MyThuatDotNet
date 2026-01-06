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
        // 1. VALIDATE CƠ BẢN
        if (req.UserId <= 0) return BadRequest("UserId không hợp lệ.");
        if (req.Items == null || req.Items.Count == 0) return BadRequest("Giỏ hàng trống.");
        if (string.IsNullOrWhiteSpace(req.FullName)) return BadRequest("Thiếu họ tên.");

        var userExists = await _db.Users.AnyAsync(u => u.Id == req.UserId);
        if (!userExists) return BadRequest("User không tồn tại.");

        // 2. XỬ LÝ PAYMENT & STATUS
        bool isVnPay = req.PaymentName?.ToUpper().Contains("VNPAY") == true;
        var paymentName = string.IsNullOrWhiteSpace(req.PaymentName) ? "COD" : req.PaymentName.Trim();

        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.PaymentName == paymentName)
                        ?? await _db.Payments.FirstOrDefaultAsync(p => p.PaymentName == "COD")
                        ?? await _db.Payments.FirstOrDefaultAsync();

        if (payment == null) return BadRequest("Chưa có dữ liệu Payments trong DB.");

        var status = await _db.OrderStatuses.FirstOrDefaultAsync(s => s.StatusName == "Đang xử lý")
                    ?? await _db.OrderStatuses.FirstOrDefaultAsync();

        if (status == null) return BadRequest("Chưa có dữ liệu Order_Statuses trong DB.");

        // 3. LOAD SẢN PHẨM VÀ TÍNH TIỀN HÀNG (itemsTotal)
        var ids = req.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        if (products.Count != ids.Count) return Conflict("Có sản phẩm không tồn tại.");

        decimal itemsTotal = 0m;
        var details = new List<OrderDetail>();

        foreach (var it in req.Items)
        {
            var p = products.First(x => x.Id == it.ProductId);

            if (!(p.IsActive ?? false)) return Conflict($"Sản phẩm '{p.Name}' đã ngừng bán.");

            var stock = p.QuantityStock ?? 0;
            if (stock <= 0) return Conflict($"Sản phẩm '{p.Name}' đã hết hàng.");

            if (it.Quantity < 1) it.Quantity = 1;
            if (it.Quantity > stock) return Conflict($"Sản phẩm '{p.Name}' chỉ còn {stock}.");

            var discountProduct = p.DiscountDefault ?? 0;
            var unitPrice = p.Price * (1m - discountProduct / 100m);
            itemsTotal += unitPrice * it.Quantity;

            details.Add(new OrderDetail
            {
                ProductId = p.Id,
                Quantity = it.Quantity,
                Price = unitPrice
            });
        }

        // 4. TRANSACTION: XỬ LÝ VOUCHER -> TẠO ĐƠN -> TRỪ KHO
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // --- [START] LOGIC VOUCHER MỚI ---
            decimal voucherDiscount = 0;

            // Nếu client có gửi VoucherId lên
            if (req.VoucherId.HasValue && req.VoucherId > 0)
            {
                var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.Id == req.VoucherId);

                // Validate kỹ lại ở Backend (đề phòng hack request)
                if (voucher == null) throw new Exception("Voucher không tồn tại.");
                if (!(voucher.IsActive ?? false)) throw new Exception("Voucher đang bị khóa.");

                var now = DateTime.Now;
                if (now < voucher.StartDate || now > voucher.EndDate)
                    throw new Exception("Voucher chưa bắt đầu hoặc đã hết hạn.");

                if (voucher.QuantityUsed >= voucher.Quantity)
                    throw new Exception("Voucher đã hết lượt sử dụng.");

                if (itemsTotal < voucher.MinOrderValue)
                    throw new Exception($"Đơn hàng chưa đủ {voucher.MinOrderValue.ToString("#,##0")}đ để dùng voucher này.");

                // Nếu thỏa mãn hết điều kiện:
                voucherDiscount = voucher.VoucherCash; // Lấy số tiền giảm

                // Tăng số lượt đã dùng lên 1
                voucher.QuantityUsed = voucher.QuantityUsed + 1;
            }
            // --- [END] LOGIC VOUCHER ---


            // Tính tổng tiền cuối cùng (Server tự tính, không tin tưởng req.Discount từ client)
            var finalTotal = itemsTotal - voucherDiscount + req.ShippingFee;
            if (finalTotal < 0) finalTotal = 0;

            string initialPaymentStatus = isVnPay ? "Chờ thanh toán VNPay" : "Chưa thanh toán";

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

                // Lưu thông tin Voucher vào đơn hàng
                VoucherId = (req.VoucherId.HasValue && req.VoucherId > 0) ? req.VoucherId : null,
                Discount = voucherDiscount, // Lưu số tiền thực tế server tính được
                ShippingFee = req.ShippingFee,

                TotalPrice = finalTotal,
                PaymentStatus = initialPaymentStatus,
                CreateAt = DateTime.Now
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(); // Lưu để sinh OrderId

            // Lưu chi tiết đơn hàng & Trừ kho (theo logic VNPay/COD)
            foreach (var d in details)
            {
                d.OrderId = order.Id;
                _db.OrderDetails.Add(d);

                // Nếu COD thì trừ kho ngay. 
                // Nếu VNPay thì CHƯA trừ kho (chờ confirm).
                if (!isVnPay)
                {
                    var p = products.First(x => x.Id == d.ProductId);
                    p.QuantityStock = (p.QuantityStock ?? 0) - d.Quantity;
                    p.SoldQuantity = (p.SoldQuantity ?? 0) + d.Quantity;
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { success = true, orderId = order.Id });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

            // Trả về lỗi chi tiết để hiện lên web
            return BadRequest(new { success = false, message = "Lỗi DB: " + innerMessage });
        }
    }
    [HttpPost("confirm-payment")]
    public async Task<IActionResult> ConfirmPayment([FromBody] int orderId)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Lấy thông tin đơn hàng và chi tiết
            var order = await _db.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound("Đơn hàng không tồn tại");

            // 2. Kiểm tra Idempotency (Tránh trừ kho 2 lần nếu gọi API nhiều lần)
            if (order.PaymentStatus == "Đã thanh toán")
            {
                return Ok(new { success = true, message = "Đơn hàng đã được xử lý trước đó" });
            }

            // 3. Cập nhật trạng thái thanh toán
            order.PaymentStatus = "Đã thanh toán";

            // 4. Thực hiện trừ kho (Bây giờ mới trừ)
            // Cần load lại Products từ DB để đảm bảo số lượng tồn kho mới nhất
            var productIds = order.OrderDetails.Select(x => x.ProductId).ToList();
            var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

            foreach (var detail in order.OrderDetails)
            {
                var p = products.FirstOrDefault(x => x.Id == detail.ProductId);
                if (p != null)
                {
                    // Kiểm tra lại tồn kho lần cuối
                    // Trường hợp rủi ro: Khách đặt xong treo máy 10 phút mới thanh toán,
                    // trong 10 phút đó có người khác mua mất hàng COD.
                    if ((p.QuantityStock ?? 0) < detail.Quantity)
                    {
                        // Tùy nghiệp vụ: 
                        // Cách 1: Throw lỗi để rollback (VNPay đã trừ tiền rồi thì phải hoàn tiền thủ công)
                        throw new Exception($"Sản phẩm {p.Name} đã hết hàng trong quá trình chờ thanh toán.");

                        // Cách 2: Vẫn cho âm kho rồi xử lý sau (để đơn hàng thành công vì tiền đã trừ)
                    }

                    p.QuantityStock = (p.QuantityStock ?? 0) - detail.Quantity;
                    p.SoldQuantity = (p.SoldQuantity ?? 0) + detail.Quantity;
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return BadRequest(new { success = false, message = ex.Message });
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
