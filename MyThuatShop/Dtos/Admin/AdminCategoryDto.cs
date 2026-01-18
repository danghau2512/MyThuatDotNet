namespace MyThuatShop.Dtos.Admin
{
    public class AdminCategoryDto
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = "";
        public string? Thumbnail { get; set; }
        public int IsActive { get; set; }
    }
}
