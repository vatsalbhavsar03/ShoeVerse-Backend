namespace ShoeVerse_WebAPI.DTO
{
    public class UpdateOrderStatusRequest
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = null!;
    }
}
