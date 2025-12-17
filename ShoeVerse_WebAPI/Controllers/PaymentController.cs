using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeVerse_WebAPI.DTO;
using ShoeVerse_WebAPI.Models;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace ShoeVerse_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ShoeVersedbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController>? _logger;

        public PaymentController(ShoeVersedbContext context, IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
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

            // Update order status
            if (dto.PaymentMethod.ToLower() == "cod")
            {
                order.Status = "Pending";
            }
            else
            {
                order.Status = dto.PaymentStatus;
            }
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send order confirmation email ASYNCHRONOUSLY
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendOrderConfirmationEmail(order.User.Email, order);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to send confirmation email for order {OrderId}", order.OrderId);
                }
            });

            return Ok(new
            {
                success = true,
                message = "Payment created successfully. Order confirmation email queued.",
                paymentId = payment.PaymentId
            });
        }



        

        private async Task<bool> SendOrderConfirmationEmail(string email, Order order)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("SendOrderConfirmationEmail called with empty email for order {OrderId}", order?.OrderId);
                return false;
            }

            try
            {
                // ensure order and navigations exist
                order = order ?? throw new ArgumentNullException(nameof(order));
                var username = order.User?.Username ?? "Customer";

                // Template file
                string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "OrderConfirmation.html");
                if (!System.IO.File.Exists(templatePath))
                {
                    _logger.LogWarning("Email template not found at {Path}", templatePath);
                    return false;
                }

                string emailBody = await System.IO.File.ReadAllTextAsync(templatePath);

                // basic replacements
                emailBody = emailBody
                    .Replace("{{UserName}}", WebUtility.HtmlEncode(username))
                    .Replace("{{OrderId}}", order.OrderId.ToString())
                    .Replace("{{OrderDate}}", (order.CreatedAt == default ? DateTime.UtcNow : order.CreatedAt).ToString("dd-MM-yyyy"));

                // currency formatting - use configuration or invariant culture as needed
                var culture = System.Globalization.CultureInfo.GetCultureInfo(_configuration["App:CurrencyCulture"] ?? "en-IN");
                emailBody = emailBody.Replace("{{TotalAmount}}", (order.TotalAmount).ToString("C", culture));

                // Build items HTML (defensive null checks)
                var itemsHtml = new StringBuilder();
                if (order.OrderItems != null)
                {
                    foreach (var item in order.OrderItems)
                    {
                        var productName = item.Product?.Name ?? "Product";
                        var qty = item.Quantity;
                        var price = (item.Price).ToString("C", culture);

                        itemsHtml.Append("<tr>")
                                 .AppendFormat("<td>{0}</td>", WebUtility.HtmlEncode(productName))
                                 .AppendFormat("<td style=\"text-align:center\">{0}</td>", qty)
                                 .AppendFormat("<td style=\"text-align:right\">{0}</td>", price)
                                 .Append("</tr>");
                    }
                }
                emailBody = emailBody.Replace("{{OrderItems}}", itemsHtml.ToString());

                // Read SMTP settings from configuration
                var smtpHost = _configuration["Smtp:Host"];
                var smtpPort = int.TryParse(_configuration["Smtp:Port"], out var port) ? port : 587;
                var smtpUser = _configuration["Smtp:User"];
                var smtpPass = _configuration["Smtp:Pass"];
                var fromEmail = _configuration["Smtp:FromEmail"] ?? smtpUser;
                var fromName = _configuration["Smtp:FromName"] ?? "Your Store";

                if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPass))
                {
                    _logger.LogWarning("SMTP configuration missing (Host/User/Pass). Email not sent for order {OrderId}", order.OrderId);
                    return false;
                }

                using var mail = new MailMessage()
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = $"Order Confirmation - #{order.OrderId}",
                    Body = emailBody,
                    IsBodyHtml = true
                };
                mail.To.Add(email);

                using var smtp = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(smtpUser, smtpPass)
                };

                // If you want a timeout:
                smtp.Timeout = 20000; // 20s

                await smtp.SendMailAsync(mail);
                _logger.LogInformation("Order confirmation email sent to {Email} for order {OrderId}", email, order.OrderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation email for order {OrderId}", order?.OrderId);
                return false;
            }
        }
    }

   
}
