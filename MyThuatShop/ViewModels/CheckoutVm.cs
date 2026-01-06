using MyThuatShop.Models;
using System.ComponentModel.DataAnnotations;

namespace MyThuatShop.ViewModels.Checkout;

public class CheckoutVm
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    public string PhoneNumber { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
    public string Address { get; set; } = "";
    public string? Note { get; set; }

    // Thêm trường này để hứng Radio button từ View
    public string PaymentMethod { get; set; } = "COD";

    // Dữ liệu hiển thị (chỉ đọc)
    public Cart? Cart { get; set; }
    public decimal TotalAmount { get; set; }
    public int? VoucherId { get; set; } // ID của voucher nếu áp dụng thành công
    public decimal DiscountAmount { get; set; } = 0; // Số tiền giảm
    public string? AppliedVoucherCode { get; set; } // Mã code để hiển thị lại nếu reload
}

