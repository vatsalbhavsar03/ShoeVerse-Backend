using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeVerse_WebAPI.DTO;
using ShoeVerse_WebAPI.Models;

namespace ShoeVerse_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ShoeVersedbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductController(ShoeVersedbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: All products
        [HttpGet("GetAllProducts")]
        public async Task<ActionResult> GetAllProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductColors)
                    .ThenInclude(c => c.ProductImages)
                .ToListAsync();

            return Ok(new { success = true, products });
        }

        // GET: Product by ID
        [HttpGet("GetProductById/{id}")]
        public async Task<ActionResult> GetProductById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductColors)
                    .ThenInclude(c => c.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound(new { success = false, message = "Product not found" });

            return Ok(new { success = true, product });
        }

        // POST: Add product with colors & images
        [HttpPost("AddProduct")]
        public async Task<ActionResult> AddProduct([FromForm] ProductDTO dto, [FromForm] List<IFormFile> files)
        {
            // 🔹 Null safety check for WebRootPath
            var webRootPath = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRootPath))
            {
                webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            // 🔹 Ensure uploads directory exists
            var uploadsPath = Path.Combine(webRootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var product = new Product
            {
                CategoryId = dto.Category_ID,
                BrandId = dto.Brand_ID,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                Gender = dto.Gender,
                Material = dto.Material,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            if (dto.Colors != null)
            {
                foreach (var colorDto in dto.Colors)
                {
                    var color = new ProductColor
                    {
                        ProductId = product.ProductId,
                        ColorName = colorDto.ColorName,
                        HexCode = colorDto.HexCode,
                        Stock = colorDto.Stock,
                        Price = colorDto.Price ?? dto.Price,
                        IsActive = true
                    };
                    _context.ProductColors.Add(color);
                    await _context.SaveChangesAsync();

                    if (colorDto.Images != null)
                    {
                        foreach (var imgDto in colorDto.Images)
                        {
                            IFormFile? file = files.FirstOrDefault(f => f.FileName == Path.GetFileName(imgDto.ImageUrl));
                            string? imageUrl = imgDto.ImageUrl;

                            if (file != null)
                            {
                                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                                var uploadPath = Path.Combine(uploadsPath, fileName);

                                using (var stream = new FileStream(uploadPath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }
                                imageUrl = $"/uploads/{fileName}";
                            }

                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId = product.ProductId,
                                ColorId = color.ColorId,
                                ImageUrl = imageUrl,
                                IsMainImage = imgDto.IsMainImage
                            });
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Product added successfully" });
        }

        [HttpPut("UpdateProduct/{id}")]
        public async Task<ActionResult> UpdateProduct(int id, [FromForm] ProductDTO dto, [FromForm] List<IFormFile> files)
        {
            // 🔹 Null safety check for WebRootPath
            var webRootPath = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRootPath))
            {
                webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var uploadsPath = Path.Combine(webRootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // 🔹 Fetch existing product with related entities
            var product = await _context.Products
                .Include(p => p.ProductColors)
                    .ThenInclude(c => c.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound(new { success = false, message = "Product not found" });

            // 🔹 Update basic product properties
            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.Material = dto.Material;
            product.Gender = dto.Gender;
            product.CategoryId = dto.Category_ID;
            product.BrandId = dto.Brand_ID;
            product.IsActive = dto.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            // 🔹 Handle color updates/additions
            if (dto.Colors != null && dto.Colors.Any())
            {
                // Track which colors to keep
                var incomingColorIds = dto.Colors
                    .Where(c => c.Color_ID.HasValue && c.Color_ID.Value > 0)
                    .Select(c => c.Color_ID.Value)
                    .ToList();

                // Remove colors that are not in the incoming list
                var colorsToRemove = product.ProductColors
                    .Where(c => !incomingColorIds.Contains(c.ColorId))
                    .ToList();

                foreach (var colorToRemove in colorsToRemove)
                {
                    // Delete associated images from file system
                    foreach (var img in colorToRemove.ProductImages)
                    {
                        if (!string.IsNullOrEmpty(img.ImageUrl))
                        {
                            var imagePath = Path.Combine(webRootPath, img.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                        }
                    }
                    _context.ProductImages.RemoveRange(colorToRemove.ProductImages);
                    _context.ProductColors.Remove(colorToRemove);
                }

                // Process each incoming color
                foreach (var colorDto in dto.Colors)
                {
                    ProductColor color;

                    if (colorDto.Color_ID.HasValue && colorDto.Color_ID.Value > 0)
                    {
                        // 🔹 Update existing color
                        color = product.ProductColors.FirstOrDefault(c => c.ColorId == colorDto.Color_ID.Value);

                        if (color != null)
                        {
                            color.ColorName = colorDto.ColorName;
                            color.HexCode = colorDto.HexCode;
                            color.Stock = colorDto.Stock;
                            color.Price = colorDto.Price ?? dto.Price;
                        }
                        else
                        {
                            continue; // Skip if color not found
                        }
                    }
                    else
                    {
                        // 🔹 Add new color
                        color = new ProductColor
                        {
                            ProductId = product.ProductId,
                            ColorName = colorDto.ColorName,
                            HexCode = colorDto.HexCode,
                            Stock = colorDto.Stock,
                            Price = colorDto.Price ?? dto.Price,
                            IsActive = true
                        };
                        _context.ProductColors.Add(color);
                        await _context.SaveChangesAsync(); // Save to get ColorId for images
                    }

                    // 🔹 Handle images for this color
                    if (colorDto.Images != null && colorDto.Images.Any())
                    {
                        // Track which images to keep
                        var incomingImageIds = colorDto.Images
                            .Where(i => i.Image_ID.HasValue && i.Image_ID.Value > 0)
                            .Select(i => i.Image_ID.Value)
                            .ToList();

                        // Remove images that are not in the incoming list
                        var imagesToRemove = color.ProductImages
                            .Where(i => !incomingImageIds.Contains(i.ImageId))
                            .ToList();

                        foreach (var imgToRemove in imagesToRemove)
                        {
                            // Delete from file system
                            if (!string.IsNullOrEmpty(imgToRemove.ImageUrl))
                            {
                                var imagePath = Path.Combine(webRootPath, imgToRemove.ImageUrl.TrimStart('/'));
                                if (System.IO.File.Exists(imagePath))
                                {
                                    System.IO.File.Delete(imagePath);
                                }
                            }
                            _context.ProductImages.Remove(imgToRemove);
                        }

                        // Process each incoming image
                        foreach (var imgDto in colorDto.Images)
                        {
                            string? imageUrl = imgDto.ImageUrl;

                            // Check if this is a new file upload
                            IFormFile? file = null;
                            if (!string.IsNullOrEmpty(imgDto.ImageUrl))
                            {
                                file = files.FirstOrDefault(f => f.FileName == Path.GetFileName(imgDto.ImageUrl));
                            }

                            if (file != null)
                            {
                                // 🔹 New file uploaded
                                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                                var uploadPath = Path.Combine(uploadsPath, fileName);

                                using (var stream = new FileStream(uploadPath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }
                                imageUrl = $"/uploads/{fileName}";

                                // If updating existing image, delete old file
                                if (imgDto.Image_ID.HasValue && imgDto.Image_ID.Value > 0)
                                {
                                    var existingImage = color.ProductImages.FirstOrDefault(i => i.ImageId == imgDto.Image_ID.Value);
                                    if (existingImage != null && !string.IsNullOrEmpty(existingImage.ImageUrl))
                                    {
                                        var oldImagePath = Path.Combine(webRootPath, existingImage.ImageUrl.TrimStart('/'));
                                        if (System.IO.File.Exists(oldImagePath))
                                        {
                                            System.IO.File.Delete(oldImagePath);
                                        }
                                    }
                                }
                            }

                            if (imgDto.Image_ID.HasValue && imgDto.Image_ID.Value > 0)
                            {
                                // 🔹 Update existing image
                                var existingImage = color.ProductImages.FirstOrDefault(i => i.ImageId == imgDto.Image_ID.Value);
                                if (existingImage != null)
                                {
                                    existingImage.ImageUrl = imageUrl ?? existingImage.ImageUrl;
                                    existingImage.IsMainImage = imgDto.IsMainImage;
                                }
                            }
                            else
                            {
                                // 🔹 Add new image
                                _context.ProductImages.Add(new ProductImage
                                {
                                    ProductId = product.ProductId,
                                    ColorId = color.ColorId,
                                    ImageUrl = imageUrl,
                                    IsMainImage = imgDto.IsMainImage
                                });
                            }
                        }
                    }
                }
            }
            else
            {
                // 🔹 If no colors provided, remove all existing colors and images
                foreach (var color in product.ProductColors.ToList())
                {
                    foreach (var img in color.ProductImages)
                    {
                        if (!string.IsNullOrEmpty(img.ImageUrl))
                        {
                            var imagePath = Path.Combine(webRootPath, img.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                        }
                    }
                    _context.ProductImages.RemoveRange(color.ProductImages);
                }
                _context.ProductColors.RemoveRange(product.ProductColors);
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Product updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error updating product: {ex.Message}" });
            }
        }

        // DELETE product
        [HttpDelete("DeleteProduct/{id}")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductColors)
                    .ThenInclude(c => c.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound(new { success = false, message = "Product not found" });

            foreach (var color in product.ProductColors)
            {
                _context.ProductImages.RemoveRange(color.ProductImages);
            }
            _context.ProductColors.RemoveRange(product.ProductColors);
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Product deleted successfully" });
        }

        // PATCH: Toggle active
        [HttpPatch("ToggleActive/{id}")]
        public async Task<ActionResult> ToggleActive(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { success = false, message = "Product not found" });

            product.IsActive = !product.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, status = product.IsActive });
        }
    }
}