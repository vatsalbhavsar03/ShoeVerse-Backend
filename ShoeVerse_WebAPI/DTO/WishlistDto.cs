namespace ShoeVerse_WebAPI.DTOs
{
    public class WishlistDto
    {
        public int WishlistId { get; set; }
        public int ProductId { get; set; }

        public string ProductName { get; set; }
        public string MainImageUrl { get; set; }

        public decimal Price { get; set; }
        public string? BrandName { get; set; }
        public string? CategoryName { get; set; }
    }

    public class AddWishlistDto
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
    }
}
