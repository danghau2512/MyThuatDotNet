using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;
using MyThuatShop.Api.Dtos.Orders;
using MyThuatShop.Api.Models;
using MyThuatShop.Api.Services;
using System.Net;
using System.Text;

namespace MyThuatShop.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{

    private readonly MyThuatDotNetContext _db;
    private readonly IEmailSender _email;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(MyThuatDotNetContext db, IEmailSender email, ILogger<OrdersController> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

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

            if (!p.IsActive)
                return Conflict($"Sản phẩm '{p.Name}' đã ngừng bán.");


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
            if (!isVnPay && !string.IsNullOrWhiteSpace(order.Email))
            {
                try
                {
                    var html = BuildOrderEmailHtml(order.Id, order.FullName, order.Email, order.PhoneNumber, order.Address,
                                                   details, products, itemsTotal, voucherDiscount, req.ShippingFee, finalTotal,
                                                   paymentName, order.PaymentStatus);

                    await _email.SendHtmlAsync(order.Email, $"Xác nhận đơn hàng DH{order.Id}", html);
                }
                catch (Exception ex)
                {
                    // KHÔNG làm fail đơn hàng nếu gửi mail lỗi
                    _logger.LogError(ex, "Send order email failed for OrderId={OrderId}", order.Id);
                }
            }

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
            try
            {
                var toEmail = string.IsNullOrWhiteSpace(order.Email) ? order.User.Email : order.Email;
                if (!string.IsNullOrWhiteSpace(toEmail))
                {
                    var itemsTotal = order.OrderDetails.Sum(d => d.Price * d.Quantity);
                    var voucherDiscount = order.Discount ?? 0m;

                    var html = BuildOrderEmailHtml(order.Id, order.FullName, toEmail, order.PhoneNumber, order.Address,
                                                   order.OrderDetails.ToList(), null,
                                                   itemsTotal, voucherDiscount, order.ShippingFee, order.TotalPrice,
                                                   order.Payment?.PaymentName ?? "VNPAY", order.PaymentStatus);

                    await _email.SendHtmlAsync(toEmail, $"Thanh toán thành công - DH{order.Id}", html);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Send VNPay success email failed for OrderId={OrderId}", order.Id);
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetByUser(int userId, [FromQuery] int? statusId = null)
    {
        var q = _db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderStatus)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
            .OrderByDescending(o => o.CreateAt);

        if (statusId.HasValue && statusId.Value > 0)
            q = q.Where(o => o.OrderStatusId == statusId.Value)
                 .OrderByDescending(o => o.CreateAt);

        var data = await q.Select(o => new
        {
            o.Id,
            o.CreateAt,
            o.TotalPrice,
            StatusId = o.OrderStatusId,
            StatusName = o.OrderStatus.StatusName,
            Items = o.OrderDetails.Select(d => new
            {
                d.ProductId,
                d.Quantity,
                UnitPrice = d.Price,
                ProductName = d.Product.Name,
                Thumbnail = d.Product.Thumbnail,
                LineTotal = d.Price * d.Quantity
            }).ToList()
        }).ToListAsync();

        return Ok(data);
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
    private static string BuildOrderEmailHtml(
    int orderId,
    string fullName,
    string toEmail,
    string? phone,
    string? address,
    IEnumerable<OrderDetail> details,
    List<Product>? products, // truyền null nếu details đã include Product
    decimal itemsTotal,
    decimal voucherDiscount,
    decimal shippingFee,
    decimal total,
    string paymentName,
    string paymentStatus)
    {
        string E(string? s) => WebUtility.HtmlEncode(s ?? "");

        var sb = new StringBuilder();
        sb.Append($@"
<div style='font-family:Roboto,Arial,sans-serif;line-height:1.5'>
  <h2 style='margin:0 0 10px 0'>Xác nhận đơn hàng DH{orderId}</h2>
  <p>Xin chào <b>{E(fullName)}</b>, cảm ơn bạn đã đặt hàng.</p>

  <p style='margin:8px 0'><b>Email:</b> {E(toEmail)}<br/>
     <b>SĐT:</b> {E(phone)}<br/>
     <b>Địa chỉ:</b> {E(address)}</p>

  <h3 style='margin:14px 0 8px 0'>Sản phẩm</h3>
  <table style='width:100%;border-collapse:collapse' border='1' cellpadding='8'>
    <tr>
      <th align='left'>Tên</th>
      <th align='right'>SL</th>
      <th align='right'>Đơn giá</th>
      <th align='right'>Thành tiền</th>
    </tr>
");

        foreach (var d in details)
        {
            var name = d.Product?.Name
                       ?? products?.FirstOrDefault(p => p.Id == d.ProductId)?.Name
                       ?? "Sản phẩm";
            var line = d.Price * d.Quantity;

            sb.Append($@"
    <tr>
      <td>{E(name)}</td>
      <td align='right'>{d.Quantity}</td>
      <td align='right'>{d.Price:N0} VNĐ</td>
      <td align='right'>{line:N0} VNĐ</td>
    </tr>");
        }

        sb.Append($@"
  </table>

  <p style='margin:12px 0 0 0'>
    <b>Tạm tính:</b> {itemsTotal:N0} VNĐ<br/>
    <b>Giảm giá:</b> {voucherDiscount:N0} VNĐ<br/>
    <b>Phí ship:</b> {shippingFee:N0} VNĐ<br/>
    <b>Tổng thanh toán:</b> {total:N0} VNĐ
  </p>

  <p style='margin:10px 0 0 0'>
    <b>Phương thức:</b> {E(paymentName)}<br/>
    <b>Trạng thái thanh toán:</b> {E(paymentStatus)}
  </p>

  <hr style='margin:16px 0;border:none;border-top:1px solid #ddd'/>
  <p style='color:#666;font-size:13px;margin:0'>MyThuatShop - Email tự động, vui lòng không trả lời email này.</p>
</div>");

        return sb.ToString();
    }

}
