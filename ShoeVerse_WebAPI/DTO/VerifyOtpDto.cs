namespace ShoeVerse_WebAPI.DTO
{
    public class VerifyOtpDto
    {
        public string Email { get; set; } = null!;
        public string Otp { get; set; }
    }
}
