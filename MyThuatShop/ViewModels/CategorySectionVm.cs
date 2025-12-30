namespace MyThuatShop.ViewModels
{
    public class CategorySectionVm
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public string? Thumbnail { get; set; }
        public List<ProductCardVm> Products { get; set; } = new();
    }
}
