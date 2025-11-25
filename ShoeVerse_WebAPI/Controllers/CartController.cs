using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeVerse_WebAPI.Models;
using ShoeVerse_WebAPI.DTO;

namespace ShoeVerse_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ShoeVersedbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(ShoeVersedbContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get cart for a specific user
        /// </summary>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<CartResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCart(int userId)
        {
            try
            {
                // Eager-load product/colors/images and sizes so we can pick image URL server-side
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.ProductColors)
                                .ThenInclude(pc => pc.ProductImages)
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Color)
                            .ThenInclude(col => col.ProductImages)
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Size)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    return Ok(new ApiResponse<CartResponse>
                    {
                        Success = true,
                        Message = "Cart is empty",
                        Data = new CartResponse { UserId = userId, Items = new List<CartItemResponse>() }
                    });
                }

                var response = MapToCartResponse(cart);

                return Ok(new ApiResponse<CartResponse>
                {
                    Success = true,
                    Message = "Cart retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cart for user {UserId}", userId);
                return StatusCode(500, new ApiResponse<CartResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the cart"
                });
            }
        }

        /// <summary>
        /// Add item to cart
        /// </summary>
        [HttpPost("add")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                if (request.Quantity <= 0)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Quantity must be greater than zero" });

                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Product not found" });

                var color = await _context.ProductColors.FindAsync(request.ColorId);
                if (color == null)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Color not found" });

                if (request.SizeId.HasValue)
                {
                    var size = await _context.ProductSizes.FindAsync(request.SizeId.Value);
                    if (size == null)
                        return BadRequest(new ApiResponse<object> { Success = false, Message = "Size not found" });

                    if (size.IsAvailable == false)
                        return BadRequest(new ApiResponse<object> { Success = false, Message = "Selected size is not available" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == request.UserId);

                if (cart == null)
                {
                    cart = new Cart { UserId = request.UserId };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var existingItem = cart.CartItems.FirstOrDefault(ci =>
                    ci.ProductId == request.ProductId &&
                    ci.ColorId == request.ColorId &&
                    ci.SizeId == request.SizeId);

                if (existingItem != null)
                {
                    existingItem.Quantity += request.Quantity;
                    _context.CartItems.Update(existingItem);
                }
                else
                {
                    var newItem = new CartItem
                    {
                        CartId = cart.CartId,
                        ProductId = request.ProductId,
                        ColorId = request.ColorId,
                        SizeId = request.SizeId,
                        Quantity = request.Quantity
                    };
                    _context.CartItems.Add(newItem);
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Item added to cart successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart for user {UserId}", request.UserId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while adding item to cart"
                });
            }
        }

        /// <summary>
        /// Update cart item quantity
        /// </summary>
        [HttpPut("item/{cartItemId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCartItem(int cartItemId, [FromBody] UpdateCartItemRequest request)
        {
            try
            {
                if (request.Quantity <= 0)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Quantity must be greater than zero" });

                var item = await _context.CartItems.FindAsync(cartItemId);
                if (item == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Cart item not found" });

                item.Quantity = request.Quantity;
                _context.CartItems.Update(item);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object> { Success = true, Message = "Cart item updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item {CartItemId}", cartItemId);
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred while updating cart item" });
            }
        }

        /// <summary>
        /// Remove item from cart
        /// </summary>
        [HttpDelete("item/{cartItemId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveCartItem(int cartItemId)
        {
            try
            {
                var item = await _context.CartItems.FindAsync(cartItemId);
                if (item == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Cart item not found" });

                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object> { Success = true, Message = "Item removed from cart successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item {CartItemId}", cartItemId);
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred while removing item from cart" });
            }
        }

        /// <summary>
        /// Clear all items from user's cart
        /// </summary>
        [HttpDelete("user/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ClearCart(int userId)
        {
            try
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Cart not found" });

                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object> { Success = true, Message = "Cart cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred while clearing cart" });
            }
        }

        // Helper method to map Cart entity to CartResponse DTO
        private CartResponse MapToCartResponse(Cart cart)
        {
            var items = new List<CartItemResponse>();

            foreach (var ci in cart.CartItems)
            {
                // Preferred: color images (prefer IsMainImage)
                string? colorImg = ci.Color?
                    .ProductImages?
                    .OrderByDescending(pi => pi.IsMainImage ?? false)
                    .Select(pi => pi.ImageUrl)
                    .FirstOrDefault();

                // Fallback: product-level images (first found, prefer IsMainImage)
                string? productImg = ci.Product?
                    .ProductColors?
                    .SelectMany(pc => pc.ProductImages ?? new List<ProductImage>())
                    .OrderByDescending(pi => pi.IsMainImage ?? false)
                    .Select(pi => pi.ImageUrl)
                    .FirstOrDefault();

                var unitPrice = ci.Product?.Price ?? 0m;
                var totalPrice = unitPrice * ci.Quantity;

                items.Add(new CartItemResponse
                {
                    CartItemId = ci.CartItemId,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product?.Name,
                    ColorId = ci.ColorId,
                    ColorName = ci.Color?.ColorName,
                    SizeId = ci.SizeId,
                    SizeName = ci.Size?.SizeName,
                    Quantity = ci.Quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = totalPrice,
                    ColorImageUrl = string.IsNullOrWhiteSpace(colorImg) ? null : colorImg,     // relative path, e.g. "/uploads/..."
                    ProductImageUrl = string.IsNullOrWhiteSpace(productImg) ? null : productImg  // relative path
                });
            }

            return new CartResponse
            {
                CartId = cart.CartId,
                UserId = cart.UserId,
                Items = items,
                TotalAmount = items.Sum(i => i.TotalPrice)
            };
        }
    }
}
