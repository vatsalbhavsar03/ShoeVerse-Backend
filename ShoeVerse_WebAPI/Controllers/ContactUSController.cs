using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeVerse_WebAPI.Models;

namespace ShoeVerse_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactUSController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ShoeVersedbContext _context;
        public ContactUSController(IConfiguration configuration, ShoeVersedbContext context)
        {
            _configuration = configuration;
            _context = context;
        }


        // POST: api/ContactUS/Submit
        [HttpPost("AddContactUS")]
        public async Task<IActionResult> AddContactUS([FromBody] ContactU contactU)
        {
            if (contactU == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid contact us data"
                });
            }
            contactU.CreatedAt = DateTime.UtcNow;
            _context.ContactUs.Add(contactU);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                success = true,
                message = "Contact us submission added successfully",
                contactU = contactU
            });
        }

        // GET: api/ContactUS/GetAllSubmissions
        [HttpGet("GetAllSubmissions")]
        public async Task<IActionResult> GetAllSubmissions()
        {
            var submissions = await _context.ContactUs.ToListAsync();
            return Ok(new
            {
                success = true,
                message = "Contact us submissions fetched successfully",
                submissions = submissions
            });
        }
    }
}
