namespace MyThuatShop.ViewModels.Checkout;

public class CheckoutVm
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Address { get; set; } = "";
    public string? Note { get; set; }

    // show summary
    public MyThuatShop.Models.Cart? Cart { get; set; }
    public decimal TotalAmount { get; set; }
}
