using MyThuatShop.Dtos.Orders;

namespace MyThuatShop.ViewModels.Order;

public class OrderHistoryPageVm
{
    public string FullName { get; set; } = "";
    public string CurrentStatus { get; set; } = "all"; // all|pending|shipping|completed|cancelled
    public List<OrderHistoryOrderDto> Orders { get; set; } = new();
}
