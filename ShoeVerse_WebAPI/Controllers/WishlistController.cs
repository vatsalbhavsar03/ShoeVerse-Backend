using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeVerse_WebAPI.DTOs;
using ShoeVerse_WebAPI.Models;
using System;

namespace ShoeVerse_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WishlistController : ControllerBase
    {
        private readonly ShoeVersedbContext _context;

        public WishlistController(ShoeVersedbContext context)
        {
            _context = context;
        }

        // ADD TO WISHLIST
        [HttpPost("add")]
        public async Task<IActionResult> AddToWishlist(AddWishlistDto dto)
        {
            var exists = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == dto.UserId && w.ProductId == dto.ProductId);

            if (exists != null)
                return BadRequest(new { message = "Product already in wishlist" });

            var newItem = new Wishlist
            {
                UserId = dto.UserId,
                ProductId = dto.ProductId
            };

            await _context.Wishlists.AddAsync(newItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product added to wishlist" });
        }

        // REMOVE FROM WISHLIST
        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveFromWishlist(int userId, int productId)
        {
            var item = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (item == null)
                return NotFound(new { message = "Wishlist item not found" });

            _context.Wishlists.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product removed from wishlist" });
        }

        // GET USER WISHLIST
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetWishlist(int userId)
        {
            var wishlist = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Include(w => w.Product)
                    .ThenInclude(p => p.Brand)
                .Include(w => w.Product)
                    .ThenInclude(p => p.Category)
                .Include(w => w.Product)
                    .ThenInclude(p => p.ProductImages)
                .Select(w => new WishlistDto
                {
                    WishlistId = w.WishlistId,
                    ProductId = w.ProductId,

                    ProductName = w.Product.Name,
                    Price = w.Product.Price,

                    // MAIN IMAGE
                    MainImageUrl = w.Product.ProductImages
                        .Where(pi => pi.IsMainImage == true)
                        .Select(pi => pi.ImageUrl)
                        .FirstOrDefault() ?? "",

                    BrandName = w.Product.Brand != null ? w.Product.Brand.BrandName : null,
                    CategoryName = w.Product.Category != null ? w.Product.Category.CategoryName : null
                })
                .ToListAsync();

            return Ok(wishlist);
        }
    }
}
