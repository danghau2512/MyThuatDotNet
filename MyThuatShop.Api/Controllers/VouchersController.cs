using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyThuatShop.Api.Data;

namespace MyThuatShop.Api.Controllers
{
    [Route("api/vouchers")]
    [ApiController]
    public class VouchersController : ControllerBase
    {
        private readonly MyThuatDotNetContext _db;

        public VouchersController(MyThuatDotNetContext db) => _db = db;

        [HttpGet("check")]
        public async Task<IActionResult> CheckVoucher(string code, decimal orderTotal)
        {
            var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.Code == code);

            // 1. Kiểm tra tồn tại và Active
            if (voucher == null || !(voucher.IsActive ?? false))
            {
                return BadRequest(new { message = "Mã giảm giá không tồn tại hoặc đã bị khóa." });
            }

            // 2. Kiểm tra thời gian (Hết hạn)
            var now = DateTime.Now;
            if (now < voucher.StartDate)
            {
                return BadRequest(new { message = "Mã giảm giá chưa đến đợt áp dụng." });
            }
            if (now > voucher.EndDate)
            {
                return BadRequest(new { message = "Mã giảm giá đã hết hạn sử dụng." });
            }

            // 3. Kiểm tra số lượng 
            if (voucher.QuantityUsed >= voucher.Quantity)
            {
                return BadRequest(new { message = "Mã giảm giá đã hết lượt sử dụng." });
            }

            // 4. Kiểm tra giá trị đơn hàng tối thiểu 
            if (orderTotal < voucher.MinOrderValue)
            {
                return BadRequest(new { message = $"Đơn hàng phải từ {voucher.MinOrderValue.ToString("#,##0")}đ mới được áp dụng." });
            }

            // 5. Thành công -> Trả về thông tin để hiển thị
            return Ok(new
            {
                success = true,
                voucherId = voucher.Id,
                code = voucher.Code,
                discount = voucher.VoucherCash,
                message = "Áp dụng mã thành công!"
            });
        }
    }
}
