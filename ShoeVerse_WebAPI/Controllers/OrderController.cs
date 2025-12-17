using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeVerse_WebAPI.Models;
using ShoeVerse_WebAPI.DTO;

namespace ShoeVerse_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly ShoeVersedbContext _context;
        private readonly ILogger<OrderController> _logger;

        public OrderController(ShoeVersedbContext context, ILogger<OrderController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Order/GetAllOrders
        [HttpGet("GetAllOrders")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Color)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Size)
                .Include(o => o.Payments)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(new { success = true, count = orders.Count, orders });
        }

        // POST: api/Order/CreateOrder
        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (request == null || request.UserId <= 0)
                return BadRequest(new { success = false, message = "Invalid order request." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Color)
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Size)
                    .FirstOrDefaultAsync(c => c.UserId == request.UserId);

                if (cart == null || !cart.CartItems.Any())
                    return BadRequest(new { success = false, message = "Cart is empty" });

                // 1) Validate stock for every cart item using ProductSizes (by SizeId & ColorId)
                foreach (var ci in cart.CartItems)
                {
                    var sizeEntity = await _context.ProductSizes
                        .FirstOrDefaultAsync(ps =>
                            ps.SizeId == ci.SizeId
                            && ((ps.ColorId == ci.ColorId) || (ps.ColorId == null && ci.ColorId == null)));

                    if (sizeEntity == null)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Product size not found for productId={ci.ProductId}, sizeId={ci.SizeId}"
                        });
                    }

                    var available = sizeEntity.Stock ?? 0;
                    if (available < ci.Quantity)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Insufficient stock for product '{ci.Product?.Name ?? ci.ProductId.ToString()}'. Requested: {ci.Quantity}, Available: {available}"
                        });
                    }
                }

                // 2) Create Order header
                var order = new Order
                {
                    UserId = request.UserId,
                    Status = "Pending",
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = cart.CartItems.Sum(ci => (ci.Product?.Price ?? 0) * ci.Quantity),
                    Phone = request.Phone,
                    Address = request.Address,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); 

                // 3) Create OrderItems and decrement ProductSize stock
                foreach (var ci in cart.CartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = ci.ProductId,
                        ColorId = ci.ColorId,
                        SizeId = ci.SizeId,
                        Quantity = ci.Quantity,
                        Price = ci.Product?.Price ?? 0
                    };
                    _context.OrderItems.Add(orderItem);

                    // Fetch the ProductSize row to update
                    var sizeEntity = await _context.ProductSizes
                        .FirstOrDefaultAsync(ps =>
                            ps.SizeId == ci.SizeId
                            && ((ps.ColorId == ci.ColorId) || (ps.ColorId == null && ci.ColorId == null)));

                    if (sizeEntity == null)
                        throw new InvalidOperationException($"ProductSize row missing for sizeId={ci.SizeId}");

                    sizeEntity.Stock = Math.Max(0, (sizeEntity.Stock ?? 0) - ci.Quantity);
                    _context.ProductSizes.Update(sizeEntity);

                    // decrement product-level stock if your Product entity contains a Stock property
                    if (ci.Product != null)
                    {
                        var productEntity = await _context.Products.FindAsync(ci.ProductId);
                        if (productEntity != null)
                        {
                            var prodStockProp = productEntity.GetType().GetProperty("Stock");
                            if (prodStockProp != null)
                            {
                                var currentP = (int)(prodStockProp.GetValue(productEntity) ?? 0);
                                prodStockProp.SetValue(productEntity, Math.Max(0, currentP - ci.Quantity));
                                _context.Products.Update(productEntity);
                            }
                        }
                    }
                }

                // 4) Clear cart items
                _context.CartItems.RemoveRange(cart.CartItems);

                // 5) Save all changes and commit
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Optionally return ordered items in response (frontend uses orderId and can dispatch stock event)
                var orderedItems = cart.CartItems.Select(ci => new
                {
                    productId = ci.ProductId,
                    colorId = ci.ColorId,
                    sizeId = ci.SizeId,
                    quantity = ci.Quantity
                }).ToList();

                return Ok(new { success = true, message = "Order created successfully", orderId = order.OrderId, items = orderedItems });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating order for user {UserId}", request.UserId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/Order/GetUserOrder/5
        [HttpGet("GetUserOrder/{userId}")]
        public async Task<IActionResult> GetUserOrder(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Color)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Size)
                .Include(o => o.Payments)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(new { success = true, count = orders.Count, orders });
        }

        // GET: api/Order/GetOrderDetails/5
        [HttpGet("GetOrderDetails/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Color)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Size)
                .Include(o => o.Payments)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound(new { success = false, message = "Order not found" });

            return Ok(new { success = true, order });
        }

        // PATCH: api/Order/CancelOrder/5
        [HttpPatch("CancelOrder/{orderId}")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound(new { success = false, message = "Order not found" });

            order.Status = "Cancelled";
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Order cancelled successfully" });
        }

        // GET: api/Order/GetOrdersByStatus?status=Pending
        [HttpGet("GetOrdersByStatus")]
        public async Task<IActionResult> GetOrdersByStatus(string status)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.Status.ToLower() == status.ToLower())
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(new { success = true, count = orders.Count, orders });
        }

        // PATCH: api/Order/UpdateOrderStatus
        [HttpPatch("UpdateOrderStatus")]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(request.OrderId);
            if (order == null)
                return NotFound(new { success = false, message = "Order not found" });

            order.Status = request.Status;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Order status updated successfully" });
        }

        // GET: api/Order/AllStatistics
        [HttpGet("AllStatistics")]
        public async Task<IActionResult> AllStatistics()
        {
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            var cancelledOrders = await _context.Orders.CountAsync(o => o.Status == "Cancelled");
            var processingOrders = await _context.Orders.CountAsync(o => o.Status == "Processing");

            return Ok(new
            {
                success = true,
                totalOrders,
                totalRevenue,
                pendingOrders,
                cancelledOrders,
                processingOrders
            });
        }
    }

    
}
