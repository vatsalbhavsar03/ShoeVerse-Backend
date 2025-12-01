using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeVerse_WebAPI.Models;
using ShoeVerse_WebAPI.DTO;

namespace ShoeVerse_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ShoeVersedbContext _context;

        public ReviewController(IConfiguration configuration, ShoeVersedbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/Review/GetAllReviews
        [HttpGet("GetAllReviews")]
        public async Task<IActionResult> GetAllReviews()
        {
            var reviews = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.User)
                .Select(r => new
                {
                    r.ReviewId,
                    r.UserId,
                    r.ProductId,
                    r.Rating,
                    r.ReviewText,
                    r.CreatedAt,
                    UserName = r.User != null ? r.User.Username : null
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // GET api/Review/GetReviewsByProductId/5
        [HttpGet("GetReviewsByProductId/{productId}")]
        public async Task<IActionResult> GetReviewsByProductId(int productId)
        {
            var reviews = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .Select(r => new
                {
                    r.ReviewId,
                    r.UserId,
                    r.ProductId,
                    r.Rating,
                    r.ReviewText,
                    r.CreatedAt,
                    UserName = r.User != null ? r.User.Username : null
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // GET api/Review/GetReviewById/5
        [HttpGet("GetReviewById/{reviewId}")]
        public async Task<IActionResult> GetReviewById(int reviewId)
        {
            var r = await _context.Reviews
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.ReviewId == reviewId);

            if (r == null) return NotFound();
            return Ok(new
            {
                r.ReviewId,
                r.UserId,
                r.ProductId,
                r.Rating,
                r.ReviewText,
                r.CreatedAt,
                UserName = r.User?.Username
            });
        }

        // POST api/Review/AddReview
        [HttpPost("AddReview")]
        public async Task<IActionResult> AddReview([FromBody] ReviewCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest(new { message = "Rating must be between 1 and 5." });

            // Optional: verify product and user exist
            var productExists = await _context.Products.AnyAsync(p => p.ProductId == dto.ProductId);
            if (!productExists) return BadRequest(new { message = "Product does not exist." });

            var userExists = await _context.Users.AnyAsync(u => u.UserId == dto.UserId);
            if (!userExists) return BadRequest(new { message = "User does not exist." });

            var review = new Review
            {
                UserId = dto.UserId,
                ProductId = dto.ProductId,
                Rating = dto.Rating,
                ReviewText = dto.ReviewText,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Replace with proper logging in production
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while saving the review." });
            }

            // Return a minimal response (not the tracked EF entity).
            var response = new
            {
                review.ReviewId,
                review.UserId,
                review.ProductId,
                review.Rating,
                review.ReviewText,
                review.CreatedAt
            };

            // IMPORTANT: use CreatedAtAction (not CreatedAtRoute) since we didn't define a route name.
            return CreatedAtAction(nameof(GetReviewById), new { reviewId = review.ReviewId }, response);
        }

        // PUT api/Review/UpdateReview/5
        [HttpPut("UpdateReview/{reviewId}")]
        public async Task<IActionResult> UpdateReview(int reviewId, [FromBody] ReviewUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest(new { message = "Rating must be between 1 and 5." });

            var existingReview = await _context.Reviews.FindAsync(reviewId);
            if (existingReview == null) return NotFound(new { message = "Review not found." });

            // Optionally check ownership/authorization here

            existingReview.Rating = dto.Rating;
            existingReview.ReviewText = dto.ReviewText;
            // do not change CreatedAt

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // If concurrency issue, check if the review still exists, otherwise return 404
                if (!await _context.Reviews.AnyAsync(r => r.ReviewId == reviewId))
                    return NotFound(new { message = "Review not found." });

                // otherwise return 409 Conflict
                return StatusCode(StatusCodes.Status409Conflict, new { message = "Concurrency error updating review. Please retry." });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while updating the review." });
            }

            return NoContent();
        }

        // DELETE api/Review/DeleteReview/5
        [HttpDelete("DeleteReview/{reviewId}")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return NotFound(new { message = "Review not found." });

            _context.Reviews.Remove(review);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while deleting the review." });
            }

            return NoContent();
        }
    }
}
