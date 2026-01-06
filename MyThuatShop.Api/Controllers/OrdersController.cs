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
        // --- KHỐI VALIDATE DỮ LIỆU ĐẦU VÀO (GIỮ NGUYÊN) ---
        if (req.UserId <= 0) return BadRequest("UserId không hợp lệ.");
        if (req.Items == null || req.Items.Count == 0) return BadRequest("Giỏ hàng trống.");
        if (string.IsNullOrWhiteSpace(req.FullName)) return BadRequest("Thiếu họ tên.");

        var userExists = await _db.Users.AnyAsync(u => u.Id == req.UserId);
        if (!userExists) return BadRequest("User không tồn tại.");

        // [NEW] 1. Xác định xem có phải là thanh toán VNPay không
        // Giả sử client gửi lên chuỗi "VNPAY" hoặc "ThanhToanVnPay" trong req.PaymentName
        bool isVnPay = req.PaymentName?.ToUpper().Contains("VNPAY") == true;

        // 2. Xử lý Payment (fallback)
        var paymentName = string.IsNullOrWhiteSpace(req.PaymentName) ? "COD" : req.PaymentName.Trim();

        // Nếu là VNPay nhưng trong DB chưa có payment tên đó, cẩn thận fallback về COD sẽ sai logic
        // Tốt nhất nên đảm bảo DB bảng Payments đã có dòng "VNPAY"
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.PaymentName == paymentName)
                       ?? await _db.Payments.FirstOrDefaultAsync(p => p.PaymentName == "COD")
                       ?? await _db.Payments.FirstOrDefaultAsync();

        if (payment == null) return BadRequest("Chưa có dữ liệu Payments trong DB.");

        var status = await _db.OrderStatuses.FirstOrDefaultAsync(s => s.StatusName == "Đang xử lý")
                  ?? await _db.OrderStatuses.FirstOrDefaultAsync();

        if (status == null) return BadRequest("Chưa có dữ liệu Order_Statuses trong DB.");

        // 3. Load sản phẩm
        var ids = req.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        if (products.Count != ids.Count) return Conflict("Có sản phẩm không tồn tại.");

        // 4. Validate tồn + Tính tiền
        decimal itemsTotal = 0m;
        var details = new List<OrderDetail>();

        foreach (var it in req.Items)
        {
            var p = products.First(x => x.Id == it.ProductId);

            if (!(p.IsActive ?? false))
                return Conflict($"Sản phẩm '{p.Name}' đã ngừng bán.");

            var stock = p.QuantityStock ?? 0;

            // Vẫn phải kiểm tra tồn kho kể cả VNPay để đảm bảo lúc bấm nút mua là còn hàng
            if (stock <= 0)
                return Conflict($"Sản phẩm '{p.Name}' đã hết hàng.");

            if (it.Quantity < 1) it.Quantity = 1;
            if (it.Quantity > stock)
                return Conflict($"Sản phẩm '{p.Name}' chỉ còn {stock}.");

            var discount = p.DiscountDefault ?? 0;
            var unitPrice = p.Price * (1m - discount / 100m);
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

        // 5. Transaction tạo đơn
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // [NEW] 2. Set trạng thái thanh toán ban đầu
            // Nếu là VNPay: "Chờ thanh toán VNPay"
            // Nếu là COD: "Chưa thanh toán"
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

                VoucherId = req.VoucherId,
                Discount = req.Discount,
                ShippingFee = req.ShippingFee,

                TotalPrice = total,
                PaymentStatus = initialPaymentStatus, // [UPDATED]
                CreateAt = DateTime.Now
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(); // Lưu để có OrderId

            foreach (var d in details)
            {
                d.OrderId = order.Id;
                _db.OrderDetails.Add(d);

                // [NEW] 3. Logic trừ kho có điều kiện
                // Nếu KHÔNG phải VNPay (tức là COD) -> Trừ kho ngay lập tức
                // Nếu LÀ VNPay -> Giữ nguyên kho, chờ callback confirm mới trừ
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
        catch
        {
            await tx.RollbackAsync();
            throw;
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
