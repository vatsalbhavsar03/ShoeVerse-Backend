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

        // ✅ Get all products
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

        // ✅ Get single product
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

        // ✅ Add product
        [HttpPost("AddProduct")]
        public async Task<IActionResult> AddProduct([FromForm] ProductDTO dto, [FromForm] List<IFormFile> files)
        {
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

                    // 🔹 Add sizes
                    if (colorDto.Sizes != null)
                    {
                        foreach (var sizeDto in colorDto.Sizes)
                        {
                            _context.ProductSizes.Add(new ProductSize
                            {
                                ColorId = color.ColorId,
                                SizeName = sizeDto.SizeName,
                                Stock = sizeDto.Stock,
                                IsAvailable = sizeDto.IsAvailable
                            });
                        }
                    }

                    // 🔹 Add images
                    if (colorDto.Images != null)
                    {
                        foreach (var imgDto in colorDto.Images)
                        {
                            string? imageUrl = imgDto.ImageUrl;
                            var file = files.FirstOrDefault(f => f.FileName == Path.GetFileName(imgDto.ImageUrl));
                            if (file != null)
                            {
                                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                                var path = Path.Combine(uploadPath, fileName);
                                using var stream = new FileStream(path, FileMode.Create);
                                await file.CopyToAsync(stream);
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

        // ✅ Update product
        [HttpPut("UpdateProduct/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductDTO dto, [FromForm] List<IFormFile> files)
        {
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

            // 🔹 Handle colors update
            var incomingColorIds = dto.Colors?.Where(c => c.Color_ID.HasValue).Select(c => c.Color_ID.Value).ToList() ?? new();
            var toRemove = product.ProductColors.Where(c => !incomingColorIds.Contains(c.ColorId)).ToList();

            foreach (var color in toRemove)
            {
                foreach (var img in color.ProductImages)
                {
                    var imgPath = Path.Combine(webRoot, img.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imgPath))
                        System.IO.File.Delete(imgPath);
                }
                _context.ProductImages.RemoveRange(color.ProductImages);
                _context.ProductSizes.RemoveRange(color.ProductSizes);
                _context.ProductColors.Remove(color);
            }

            // 🔹 Update or add colors
            foreach (var colorDto in dto.Colors)
            {
                ProductColor color;
                if (colorDto.Color_ID.HasValue)
                {
                    color = product.ProductColors.FirstOrDefault(c => c.ColorId == colorDto.Color_ID) ?? new ProductColor();
                    color.ColorName = colorDto.ColorName;
                    color.HexCode = colorDto.HexCode;
                    color.Stock = colorDto.Stock;
                    color.Price = colorDto.Price ?? dto.Price;
                }
                else
                {
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
                    await _context.SaveChangesAsync();
                }

                // 🔹 Handle sizes
                if (colorDto.Sizes != null)
                {
                    var incomingSizeIds = colorDto.Sizes.Where(s => s.Size_ID.HasValue).Select(s => s.Size_ID.Value).ToList();
                    var sizesToRemove = color.ProductSizes.Where(s => !incomingSizeIds.Contains(s.SizeId)).ToList();
                    _context.ProductSizes.RemoveRange(sizesToRemove);

                    foreach (var sizeDto in colorDto.Sizes)
                    {
                        if (sizeDto.Size_ID.HasValue)
                        {
                            var size = color.ProductSizes.FirstOrDefault(s => s.SizeId == sizeDto.Size_ID);
                            if (size != null)
                            {
                                size.SizeName = sizeDto.SizeName;
                                size.Stock = sizeDto.Stock;
                                size.IsAvailable = sizeDto.IsAvailable;
                            }
                        }
                        else
                        {
                            _context.ProductSizes.Add(new ProductSize
                            {
                                ColorId = color.ColorId,
                                SizeName = sizeDto.SizeName,
                                Stock = sizeDto.Stock,
                                IsAvailable = sizeDto.IsAvailable
                            });
                        }
                    }
                }

                // 🔹 Handle images
                if (colorDto.Images != null)
                {
                    var incomingImgIds = colorDto.Images.Where(i => i.Image_ID.HasValue).Select(i => i.Image_ID.Value).ToList();
                    var imgsToRemove = color.ProductImages.Where(i => !incomingImgIds.Contains(i.ImageId)).ToList();

                    foreach (var img in imgsToRemove)
                    {
                        var imgPath = Path.Combine(webRoot, img.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(imgPath))
                            System.IO.File.Delete(imgPath);
                        _context.ProductImages.Remove(img);
                    }

                    foreach (var imgDto in colorDto.Images)
                    {
                        string imageUrl = imgDto.ImageUrl;
                        var file = files.FirstOrDefault(f => f.FileName == Path.GetFileName(imgDto.ImageUrl));

                        if (file != null)
                        {
                            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                            var path = Path.Combine(uploadPath, fileName);
                            using var stream = new FileStream(path, FileMode.Create);
                            await file.CopyToAsync(stream);
                            imageUrl = $"/uploads/{fileName}";
                        }

                        if (imgDto.Image_ID.HasValue)
                        {
                            var existing = color.ProductImages.FirstOrDefault(i => i.ImageId == imgDto.Image_ID);
                            if (existing != null)
                            {
                                existing.ImageUrl = imageUrl;
                                existing.IsMainImage = imgDto.IsMainImage;
                            }
                        }
                        else
                        {
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
            return Ok(new { success = true, message = "Product updated successfully" });
        }

        // ✅ Delete product
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

            foreach (var color in product.ProductColors)
            {
                foreach (var img in color.ProductImages)
                {
                    var imgPath = Path.Combine(_env.WebRootPath ?? "", img.ImageUrl?.TrimStart('/') ?? "");
                    if (System.IO.File.Exists(imgPath))
                        System.IO.File.Delete(imgPath);
                }
                _context.ProductImages.RemoveRange(color.ProductImages);
                _context.ProductSizes.RemoveRange(color.ProductSizes);
            }

            _context.ProductColors.RemoveRange(product.ProductColors);
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Product deleted successfully" });
        }

        // ✅ Toggle active
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

        // 🔹 GET: Search / Filter Products
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
    }
}
