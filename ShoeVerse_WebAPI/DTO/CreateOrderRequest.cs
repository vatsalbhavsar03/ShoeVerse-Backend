namespace ShoeVerse_WebAPI.DTO
{
    public class CreateOrderRequest
    {
        public int UserId { get; set; }
        public string PaymentMethod { get; set; } = "COD"; // COD or Razorpay
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;
    }
}
