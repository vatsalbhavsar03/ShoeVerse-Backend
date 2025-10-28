namespace ShoeVerse_WebAPI.DTO
{
    public class BrandDTO
    {
        public int BrandId { get; set; }

        public int? CategoryId { get; set; }

        public string BrandName { get; set; } = null!;
    }
}
