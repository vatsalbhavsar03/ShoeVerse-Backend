namespace ShoeVerse_WebAPI.DTO
{
    public class ReviewCreateDto
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? ReviewText { get; set; }
    }
}
