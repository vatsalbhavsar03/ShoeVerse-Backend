using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeVerse_WebAPI.DTO;
using ShoeVerse_WebAPI.Models;

namespace ShoeVerse_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ShoeVersedbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(ShoeVersedbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/Payment/GetAllPayments
        [HttpGet("GetAllPayments")]
        public async Task<IActionResult> GetAllPayments()
        {
            var payments = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.User)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new
                {
                    PaymentId = p.PaymentId,
                    OrderId = p.OrderId,
                    UserName = p.Order.User.Username,
                    PaymentMethod = p.PaymentMethod,
                    TransactionId = p.TransactionId,
                    PaymentStatus = p.PaymentStatus,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate
                })
                .ToListAsync();

            return Ok(new { success = true, payments });
        }

        // GET: api/Payment/GetPaymentById/{orderId}
        [HttpGet("GetPaymentById/{orderId}")]
        public async Task<IActionResult> GetPaymentById(int orderId)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.User)
                .FirstOrDefaultAsync(p => p.OrderId == orderId);

            if (payment == null)
                return NotFound(new { success = false, message = "Payment not found." });

            var result = new
            {
                PaymentId = payment.PaymentId,
                OrderId = payment.OrderId,
                PaymentMethod = payment.PaymentMethod,
                TransactionId = payment.TransactionId,
                Amount = payment.Amount,
                PaymentStatus = payment.PaymentStatus,
                PaymentDate = payment.PaymentDate,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt
            };

            return Ok(new { success = true, payment = result });
        }

        // POST: api/Payment/CreatePayment
        [HttpPost("CreatePayment")]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentDto dto)
        {
            if (dto == null || dto.OrderId <= 0 || string.IsNullOrEmpty(dto.PaymentMethod)
                || string.IsNullOrEmpty(dto.TransactionId) || dto.Amount <= 0
                || string.IsNullOrEmpty(dto.PaymentStatus))
            {
                return BadRequest(new { success = false, message = "Invalid payment data." });
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == dto.OrderId);

            if (order == null)
                return NotFound(new { success = false, message = "Order not found." });

            var payment = new Payment
            {
                OrderId = dto.OrderId,
                PaymentMethod = dto.PaymentMethod,
                TransactionId = dto.TransactionId,
                Amount = dto.Amount,
                PaymentStatus = dto.PaymentStatus,
                PaymentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            // Update order status based on payment method
            if (dto.PaymentMethod.ToLower() == "cod")
            {
                order.Status = "Pending"; // for COD
            }
            else
            {
                order.Status = dto.PaymentStatus; // e.g., "Paid" for Razorpay
            }
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send order confirmation email
            await SendOrderConfirmationEmail(order.User.Email, order);

            return Ok(new
            {
                success = true,
                message = "Payment created successfully and order confirmation sent via email.",
                paymentId = payment.PaymentId
            });
        }

        // Email sending
        private async Task<bool> SendOrderConfirmationEmail(string email, Order order)
        {
            try
            {
                string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "OrderConfirmation.html");
                string emailBody = await System.IO.File.ReadAllTextAsync(templatePath);

                emailBody = emailBody.Replace("{{UserName}}", order.User.Username)
                                     .Replace("{{OrderId}}", order.OrderId.ToString())
                                     .Replace("{{OrderDate}}", order.CreatedAt.ToString("dd-MM-yyyy"))
                                     .Replace("{{TotalAmount}}", order.TotalAmount.ToString("C"));

                string itemsHtml = "";
                foreach (var item in order.OrderItems)
                {
                    itemsHtml += $"<tr>" +
                                 $"<td>{item.Product.Name}</td>" +
                                 $"<td>{item.Quantity}</td>" +
                                 $"<td>{item.Price}</td>" +
                                 $"</tr>";
                }
                emailBody = emailBody.Replace("{{OrderItems}}", itemsHtml);

                using var smtp = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("bhavsarvatsal337@gmail.com", "dsms vopc kgoa teef"),
                    EnableSsl = true,
                };

                var mailMsg = new MailMessage
                {
                    From = new MailAddress("bhavsarvatsal337@gmail.com"),
                    Subject = $"Order Confirmation - #{order.OrderId}",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                mailMsg.To.Add(email);
                await smtp.SendMailAsync(mailMsg);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

   
}
