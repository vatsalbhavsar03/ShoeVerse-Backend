namespace ShoeVerse_WebAPI.DTO
{
    public class ProductSizeDTO
    {
        public int? Size_ID { get; set; }

        public string SizeName { get; set; }

        public int Stock { get; set; }

        public bool IsAvailable { get; set; } = true;
    }

}