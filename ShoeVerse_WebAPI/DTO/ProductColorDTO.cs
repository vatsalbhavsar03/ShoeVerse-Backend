namespace ShoeVerse_WebAPI.DTO
{
    public class ProductColorDTO
    {
        public int? Color_ID { get; set; }


        public string ColorName { get; set; }

        public string? HexCode { get; set; }

        public int Stock { get; set; }

        public decimal? Price { get; set; }

        public List<ProductImageDTO> Images { get; set; } = new();

        // ✅ Added Sizes
        public List<ProductSizeDTO> Sizes { get; set; } = new();
    }
}