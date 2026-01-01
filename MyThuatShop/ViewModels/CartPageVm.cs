using System.Collections.Generic;
using MyThuatShop.Models;

namespace MyThuatShop.ViewModels;

public class CartPageVm
{
    public IEnumerable<CartItem> CartItems { get; set; } = new List<CartItem>();
    public int TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public int CartSize { get; set; }
}
