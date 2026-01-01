namespace MyThuatShop.Api.Dtos
{
    public class CreateReviewRequestDto
    {
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
