namespace MyThuatShop.Api.Dtos.Admin;

public class AdminVoucherRowDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal VoucherCash { get; set; }
    public decimal MinOrderValue { get; set; }
    public int Quantity { get; set; }
    public int QuantityUsed { get; set; }
    public int IsActive { get; set; } // 1/0 cho giống JSP
}
