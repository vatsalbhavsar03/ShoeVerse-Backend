using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShoeVerse_WebAPI.DTO;
using ShoeVerse_WebAPI.Models;

namespace ShoeVerse_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ShoeVersedbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(ShoeVersedbContext context,IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/Roles
        [HttpGet("GetRole")]
        public async Task<IActionResult> GetRole()
        {
            var roles = await _context.Roles.Select(r => new { r.RoleId, r.Rname }).ToListAsync();
            return Ok(new
            {
                success = true,
                Message = "Roles retrieved successfully",
                Data = roles
            });

        }

        //GET: api/Users
        [HttpGet("GetUser")]
        public async Task<IActionResult> GetUser()
        {
            var users = await _context.Users
                .Where(u => u.RoleId == 2)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Email,
                    u.ProfileImage,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                Message = "Users retrieved successfully",
                Data = users
            });
        }

        //GET: api/Admin
        [HttpGet("GetAdmin")]
        public async Task<IActionResult> GetAdmin()
        {
            var admins = await _context.Users
                .Where(u => u.RoleId == 1)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Email,
                    u.PhoneNo,
                    u.ProfileImage,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .ToListAsync();
            return Ok(new
            {
                success = true,
                Message = "Admins retrieved successfully",
                Data = admins
            });
        }

        //GET: api/User/5
        [HttpGet("GetUserById/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users
                .Where(u => u.UserId == id && u.RoleId == 2)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Email,
                    u.ProfileImage,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    Message = "User not found"
                });
            }
            return Ok(new
            {
                success = true,
                Message = "User retrieved successfully",
                Data = user
            });
        }


        private string GenerateJwtToken(User user)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);


            string roleName = user.Role?.Rname ?? "User";

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim("UserId", user.UserId.ToString()),
                new Claim("Role", roleName),
                new Claim("RoleId", user.RoleId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        //POST: api/Register
        [HttpPost("Register")]
        public async Task<ActionResult> Register([FromForm] RegisterUserDTO registerUserDto, IFormFile? profileImage)
        {
            try
            {
                // Check if email already exists
                if (_context.Users.Any(u => u.Email == registerUserDto.Email))
                {
                    return BadRequest(new { success = false, message = "Email already exists." });
                }

                // Handle image upload
                string? imageUrl = null;
                if (profileImage != null && profileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(profileImage.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }

                    imageUrl = $"{Request.Scheme}://{Request.Host}/uploads/profile/{uniqueFileName}";
                }

                // Get Indian time
                TimeZoneInfo indianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime indianTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indianTimeZone);

                // Create user
                var user = new User
                {
                    Username = registerUserDto.Name,
                    Email = registerUserDto.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(registerUserDto.Password),
                    PhoneNo = registerUserDto.PhoneNo,
                    RoleId = 2,
                    ProfileImage = imageUrl,
                    CreatedAt = indianTime,
                    UpdatedAt = indianTime
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);

                return Ok(new
                {
                    success = true,
                    message = "Successfully registered",
                    token,
                    user = new
                    {
                        user.UserId,
                        user.RoleId,
                        user.Username,
                        user.Email,
                        user.PhoneNo,
                        user.ProfileImage,
                        user.CreatedAt,
                        user.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        // Send Email
        private async Task<bool> SendEmail(string email, string otp)
        {
            try
            {
                string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "OtpTemplate.html");
                string emailBody = await System.IO.File.ReadAllTextAsync(templatePath);

                emailBody = emailBody.Replace("{{OTP}}", otp);

                using var smtp = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("rutvikvariya3222@gmail.com", "dejx ktgd gooz rvrg"),
                    EnableSsl = true,
                };

                var mailMsg = new MailMessage
                {
                    From = new MailAddress("rutvikvariya3222@gmail.com"),
                    Subject = "Your OTP code for Registration.",
                    Body = emailBody,
                    IsBodyHtml = true,

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


        [HttpPost("SendOTP")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpDto sendOtpDto)
        {
            var otp = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString("otp", otp);
            HttpContext.Session.SetString("otpEmail", sendOtpDto.Email);

            bool emailSent = await SendEmail(sendOtpDto.Email, otp);

            if (emailSent)
            {
                return Ok(new { success = true, message = "OTP sent successfully." });
            }

            return BadRequest(new { success = false, message = "Failed to send OTP." });
        }


        // Verify OTP
        [HttpPost("VerifyOTP")]
        public async Task<ActionResult> VerifyOtp([FromBody] VerifyOtpDto verifyOtpDto)
        {
            var sessionOtp = HttpContext.Session.GetString("otp");
            var sessionEmail = HttpContext.Session.GetString("otpEmail");

            if (sessionOtp == null || sessionEmail == null || sessionOtp != verifyOtpDto.Otp || sessionEmail != verifyOtpDto.Email)
            {
                return BadRequest(new { success = false, message = "Invalid Or Expired OTP." });
            }

            HttpContext.Session.Remove("otp");
            HttpContext.Session.Remove("otpEmail");

            return Ok(new { success = true, message = "OTP Verified Successfully." });
        }


        // GET: api/Users/5
        private string GetRedirectUrl(int roleId)
        {
            return roleId switch
            {
                1 => "/admin/dashboard",
                2 => "/user/dashboard",
                _ => "/"
            };
        }

        // POST: api/Users/Login
        [HttpPost("login")]
        public async Task<ActionResult> login([FromBody] LoginUserDTO loginUserDto)
        {
            var user = _context.Users.Include(u => u.Role).Where(u => u.Email == loginUserDto.Email).FirstOrDefault();
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginUserDto.Password, user.Password))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid Email and Password"
                });
            }

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                success = true,
                message = "Login Successfull.",
                token = token,
                roleId = user.RoleId,
                redirectUrl = GetRedirectUrl(user.RoleId)
            });
        }


        //POST: api/Users/ForgotPassword
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO forgotPasswordDTO)
        {
            var data = await _context.Users.FirstOrDefaultAsync(fp => fp.Email == forgotPasswordDTO.Email);
            if (data == null)
            {
                return NotFound(new { success = false, message = "User not found." });
            }

            var oldPasswordHash = data.Password;

            if (BCrypt.Net.BCrypt.Verify(forgotPasswordDTO.Password, oldPasswordHash))
            {
                return BadRequest(new { success = false, message = "New password cannot be same as old password." });
            }

            try
            {
                data.Password = BCrypt.Net.BCrypt.HashPassword(forgotPasswordDTO.Password);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error in student: {ex.InnerException}");
            }
        }


        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("AdminUpdateUser/{id}")]
        public async Task<IActionResult> AdminUpdateUser(int id, [FromBody] User updatedUser)
        {
            if (id != updatedUser.UserId)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "User ID mismatch."
                });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found."
                });
            }

            if (_context.Users.Any(u => u.Email == updatedUser.Email && u.UserId != id))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email already exists."
                });
            }

            user.Username = updatedUser.Username;
            user.Email = updatedUser.Email;
            user.PhoneNo = updatedUser.PhoneNo;
            user.RoleId = updatedUser.RoleId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating the user."
                });
            }

            return Ok(new
            {
                success = true,
                message = "User updated successfully.",
                user = new
                {
                    user.UserId,
                    user.Username,
                    user.Email,
                    user.PhoneNo,
                    user.RoleId
                }
            });
        }


        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
