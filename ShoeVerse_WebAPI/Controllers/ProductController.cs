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

        // GET: api/Product/GetAllProducts
        [HttpGet("GetAllProducts")]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductColors)
                    .ThenInclude(c => c.ProductImages)
                .Include(p => p.ProductColors)
                    .ThenInclude(c => c.ProductSizes)
                .ToListAsync();

            return Ok(new { success = true, products });
        }

        // GET: api/Product/GetProductById/{id}
        
        [HttpGet("GetProductById/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductColors)
                    .ThenInclude(c => c.ProductImages)
                .Include(p => p.ProductColors)
                    .ThenInclude(c => c.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound(new { success = false, message = "Product not found" });

            return Ok(new { success = true, product });
        }

        // POST: api/Product/AddProduct
        [HttpPost("AddProduct")]
        public async Task<IActionResult> AddProduct([FromForm] ProductDTO dto, [FromForm] List<IFormFile>? files)
        {
            if (dto == null) return BadRequest(new { success = false, message = "Invalid payload" });

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadPath = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(uploadPath);

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

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync(); // get ProductId

                if (dto.Colors != null && dto.Colors.Any())
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
                        await _context.SaveChangesAsync(); // get ColorId

                        // sizes
                        if (colorDto.Sizes != null && colorDto.Sizes.Any())
                        {
                            foreach (var sizeDto in colorDto.Sizes)
                            {
                                var size = new ProductSize
                                {
                                    ColorId = color.ColorId,
                                    SizeName = sizeDto.SizeName,
                                    Stock = sizeDto.Stock,
                                    IsAvailable = sizeDto.IsAvailable
                                };
                                _context.ProductSizes.Add(size);
                            }
                        }

                        // images
                        if (colorDto.Images != null && colorDto.Images.Any())
                        {
                            foreach (var imgDto in colorDto.Images)
                            {
                                string imageUrl = imgDto.ImageUrl ?? string.Empty;
                                var file = files?.FirstOrDefault(f => f.FileName == Path.GetFileName(imgDto.ImageUrl));
                                if (file != null)
                                {
                                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                                    var path = Path.Combine(uploadPath, fileName);
                                    await using (var stream = new FileStream(path, FileMode.Create))
                                    {
                                        await file.CopyToAsync(stream);
                                    }
                                    imageUrl = $"/uploads/{fileName}";
                                }

                                var pimg = new ProductImage
                                {
                                    ProductId = product.ProductId,
                                    ColorId = color.ColorId,
                                    ImageUrl = imageUrl,
                                    IsMainImage = imgDto.IsMainImage
                                };
                                _context.ProductImages.Add(pimg);
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Product added successfully", productId = product.ProductId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // PUT: api/Product/UpdateProduct/{id}
        [HttpPut("UpdateProduct/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductDTO dto, [FromForm] List<IFormFile>? files)
        {
            if (dto == null) return BadRequest(new { success = false, message = "Invalid payload" });

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadPath = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(uploadPath);

            var product = await _context.Products
                .Include(p => p.ProductColors)
                    .ThenInclude(c => c.ProductImages)
                .Include(p => p.ProductColors)
                    .ThenInclude(c => c.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound(new { success = false, message = "Product not found" });

            // update basic fields
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

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Remove colors not present in DTO
                var incomingColorIds = dto.Colors?.Where(c => c.Color_ID.HasValue).Select(c => c.Color_ID!.Value).ToHashSet() ?? new HashSet<int>();
                var colorsToRemove = product.ProductColors.Where(c => !incomingColorIds.Contains(c.ColorId)).ToList();
                foreach (var color in colorsToRemove)
                {
                    // delete files
                    foreach (var img in color.ProductImages)
                    {
                        if (!string.IsNullOrWhiteSpace(img.ImageUrl))
                        {
                            var imgPath = Path.Combine(webRoot, img.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                            if (System.IO.File.Exists(imgPath))
                                System.IO.File.Delete(imgPath);
                        }
                    }
                    _context.ProductImages.RemoveRange(color.ProductImages);
                    _context.ProductSizes.RemoveRange(color.ProductSizes);
                    _context.ProductColors.Remove(color);
                }

                await _context.SaveChangesAsync();

                // Update or add colors
                foreach (var colorDto in dto.Colors ?? Enumerable.Empty<ProductColorDTO>())
                {
                    ProductColor colorEntity;

                    if (colorDto.Color_ID.HasValue)
                    {
                        colorEntity = product.ProductColors.FirstOrDefault(c => c.ColorId == colorDto.Color_ID.Value);
                        if (colorEntity == null)
                            return BadRequest(new { success = false, message = $"Color with id {colorDto.Color_ID.Value} not found for this product." });

                        colorEntity.ColorName = colorDto.ColorName;
                        colorEntity.HexCode = colorDto.HexCode;
                        colorEntity.Stock = colorDto.Stock;
                        colorEntity.Price = colorDto.Price ?? dto.Price;
                    }
                    else
                    {
                        colorEntity = new ProductColor
                        {
                            ProductId = product.ProductId,
                            ColorName = colorDto.ColorName,
                            HexCode = colorDto.HexCode,
                            Stock = colorDto.Stock,
                            Price = colorDto.Price ?? dto.Price,
                            IsActive = true
                        };
                        _context.ProductColors.Add(colorEntity);
                        await _context.SaveChangesAsync();
                    }

                    // Sizes
                    if (colorDto.Sizes != null)
                    {
                        var incomingSizeIds = colorDto.Sizes.Where(s => s.Size_ID.HasValue).Select(s => s.Size_ID!.Value).ToHashSet();
                        var existingSizes = await _context.ProductSizes.Where(s => s.ColorId == colorEntity.ColorId).ToListAsync();

                        var sizesToRemove = existingSizes.Where(s => !incomingSizeIds.Contains(s.SizeId)).ToList();
                        _context.ProductSizes.RemoveRange(sizesToRemove);

                        foreach (var sizeDto in colorDto.Sizes)
                        {
                            if (sizeDto.Size_ID.HasValue)
                            {
                                var sizeEntity = existingSizes.FirstOrDefault(s => s.SizeId == sizeDto.Size_ID.Value);
                                if (sizeEntity == null)
                                    return BadRequest(new { success = false, message = $"Size with id {sizeDto.Size_ID} not found for color {colorEntity.ColorId}" });

                                sizeEntity.SizeName = sizeDto.SizeName;
                                sizeEntity.Stock = sizeDto.Stock;
                                sizeEntity.IsAvailable = sizeDto.IsAvailable;
                            }
                            else
                            {
                                var newSize = new ProductSize
                                {
                                    ColorId = colorEntity.ColorId,
                                    SizeName = sizeDto.SizeName,
                                    Stock = sizeDto.Stock,
                                    IsAvailable = sizeDto.IsAvailable
                                };
                                _context.ProductSizes.Add(newSize);
                            }
                        }
                    }

                    // Images
                    if (colorDto.Images != null)
                    {
                        var existingImages = await _context.ProductImages.Where(i => i.ColorId == colorEntity.ColorId).ToListAsync();
                        var incomingImgIds = colorDto.Images.Where(i => i.Image_ID.HasValue).Select(i => i.Image_ID!.Value).ToHashSet();

                        var imgsToRemove = existingImages.Where(i => !incomingImgIds.Contains(i.ImageId)).ToList();
                        foreach (var rem in imgsToRemove)
                        {
                            if (!string.IsNullOrWhiteSpace(rem.ImageUrl))
                            {
                                var imgPath = Path.Combine(webRoot, rem.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                                if (System.IO.File.Exists(imgPath))
                                    System.IO.File.Delete(imgPath);
                            }
                        }
                        _context.ProductImages.RemoveRange(imgsToRemove);

                        foreach (var imgDto in colorDto.Images)
                        {
                            string imageUrl = imgDto.ImageUrl ?? string.Empty;
                            var file = files?.FirstOrDefault(f => f.FileName == Path.GetFileName(imgDto.ImageUrl));
                            if (file != null)
                            {
                                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                                var path = Path.Combine(uploadPath, fileName);
                                await using (var stream = new FileStream(path, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }
                                imageUrl = $"/uploads/{fileName}";
                            }

                            if (imgDto.Image_ID.HasValue)
                            {
                                var existing = existingImages.FirstOrDefault(i => i.ImageId == imgDto.Image_ID.Value);
                                if (existing != null)
                                {
                                    existing.ImageUrl = imageUrl;
                                    existing.IsMainImage = imgDto.IsMainImage;
                                }
                                else
                                {
                                    _context.ProductImages.Add(new ProductImage
                                    {
                                        ProductId = product.ProductId,
                                        ColorId = colorEntity.ColorId,
                                        ImageUrl = imageUrl,
                                        IsMainImage = imgDto.IsMainImage
                                    });
                                }
                            }
                            else
                            {
                                _context.ProductImages.Add(new ProductImage
                                {
                                    ProductId = product.ProductId,
                                    ColorId = colorEntity.ColorId,
                                    ImageUrl = imageUrl,
                                    IsMainImage = imgDto.IsMainImage
                                });
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return Ok(new { success = true, message = "Product updated successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/Product/DeleteProduct/{id}
        [HttpDelete("DeleteProduct/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductColors)
                    .ThenInclude(c => c.ProductImages)
                .Include(p => p.ProductColors)
                    .ThenInclude(c => c.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound(new { success = false, message = "Product not found" });

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            foreach (var color in product.ProductColors)
            {
                foreach (var img in color.ProductImages)
                {
                    if (!string.IsNullOrWhiteSpace(img.ImageUrl))
                    {
                        var imgPath = Path.Combine(webRoot, img.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(imgPath))
                            System.IO.File.Delete(imgPath);
                    }
                }
                _context.ProductImages.RemoveRange(color.ProductImages);
                _context.ProductSizes.RemoveRange(color.ProductSizes);
            }

            _context.ProductColors.RemoveRange(product.ProductColors);
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Product deleted successfully" });
        }

        // PATCH: api/Product/ToggleActive/{id}
        [HttpPatch("ToggleActive/{id}")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { success = false, message = "Product not found" });

            product.IsActive = !product.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, status = product.IsActive });
        }

        // GET: api/Product/Search
        [HttpGet("Search")]
        public async Task<ActionResult> SearchProducts(string? name, int? categoryId, int? brandId, string? gender, decimal? minPrice, decimal? maxPrice)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductColors)
                    .ThenInclude(c => c.ProductImages)
                .AsQueryable();

            if (!string.IsNullOrEmpty(name))
                query = query.Where(p => p.Name.Contains(name));
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);
            if (brandId.HasValue)
                query = query.Where(p => p.BrandId == brandId);
            if (!string.IsNullOrEmpty(gender))
                query = query.Where(p => p.Gender == gender);
            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice);

            var products = await query.ToListAsync();
            return Ok(new { success = true, count = products.Count, products });
        }

        // GET: api/Product/GetStockBySizeColor?productId=1&colorId=2
        [HttpGet("GetStockBySizeColor")]
        public async Task<IActionResult> GetStockBySizeColor(int productId, int colorId)
        {
            try
            {
                var color = await _context.ProductColors
                    .FirstOrDefaultAsync(c => c.ColorId == colorId && c.ProductId == productId);

                if (color == null)
                    return NotFound(new { success = false, message = "Color not found for the specified product." });

                var stockData = await _context.ProductSizes
                    .Where(ps => ps.ColorId == colorId)
                    .Select(ps => new
                    {
                        sizeId = ps.SizeId,
                        sizeName = ps.SizeName,
                        stock = ps.Stock,
                        isAvailable = ps.IsAvailable
                    })
                    .ToListAsync();

                return Ok(new { success = true, stockData });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
