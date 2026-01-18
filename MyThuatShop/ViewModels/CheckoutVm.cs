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

    public string PaymentMethod { get; set; } = "COD";

    public Cart? Cart { get; set; }
    public decimal TotalAmount { get; set; }
    public int? VoucherId { get; set; } 
    public decimal DiscountAmount { get; set; } = 0; 
    public string? AppliedVoucherCode { get; set; } // Mã code để hiển thị lại nếu reload

    public decimal ShippingFee { get; set; } = 0;
}

