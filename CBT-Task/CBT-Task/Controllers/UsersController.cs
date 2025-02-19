using CBT_Task.Data;
using CBT_Task.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CBT_Task.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 1. Register a new user (New Customer).
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if email or phone is already used
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
            if (emailExists)
                return BadRequest("Email is already in use.");

            bool phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == model.PhoneNumber);
            if (phoneExists)
                return BadRequest("Phone number is already in use.");

            // Create new user
            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Password = model.Password, // Plain text for demo only
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate an OTP
            var otp = new Otp
            {
                UserId = user.Id,
                Code = GenerateRandomOtp(),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            };
            _context.Otps.Add(otp);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "User registered successfully. OTP generated.",
                UserId = user.Id,
                DemoOtp = otp.Code // For demonstration. In production, send via SMS/Email.
            });
        }

        /// <summary>
        /// 2. Verify OTP for a user.
        /// </summary>
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpVerificationModel model)
        {
            // Find user
            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null)
                return NotFound("User not found.");

            // Find valid OTP record
            var otpRecord = await _context.Otps
                .Where(o => o.UserId == model.UserId &&
                            o.Code == model.OtpCode &&
                            !o.IsUsed &&
                            o.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
                return BadRequest("Invalid or expired OTP.");

            // Mark user as verified
            user.IsVerified = true;
            _context.Users.Update(user);

            // Mark OTP as used
            otpRecord.IsUsed = true;
            _context.Otps.Update(otpRecord);

            await _context.SaveChangesAsync();

            return Ok("OTP verified successfully. User is now verified.");
        }

        /// <summary>
        /// 3. (Optional) Resend a new OTP if the old one expired or user requests it.
        /// </summary>
        [HttpPost("resend-otp/{userId}")]
        public async Task<IActionResult> ResendOtp(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            // Mark any existing OTPs as used
            var existingOtps = await _context.Otps
                .Where(o => o.UserId == userId && !o.IsUsed)
                .ToListAsync();

            existingOtps.ForEach(o => o.IsUsed = true);

            // Create a new OTP
            var newOtp = new Otp
            {
                UserId = user.Id,
                Code = GenerateRandomOtp(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            };

            _context.Otps.Add(newOtp);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "OTP resent successfully.",
                DemoOtp = newOtp.Code // For demonstration
            });
        }

        /// <summary>
        /// 4. Migrate an existing user from old system to new system.
        ///    This is a placeholder for any logic that you might need.
        /// </summary>
        [HttpPost("migrate")]
        public async Task<IActionResult> MigrateUser([FromBody] MigrateModel model)
        {
            // Validate the new user
            var newUser = await _context.Users.FindAsync(model.NewSystemUserId);
            if (newUser == null)
                return NotFound("New system user not found.");

            // Create a migration record
            var migrationRecord = new MigrationRecord
            {
                OldSystemUserId = model.OldSystemUserId,
                NewSystemUserId = model.NewSystemUserId,
                MigratedAt = DateTime.UtcNow
            };

            _context.Migrations.Add(migrationRecord);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "User migrated successfully.",
                OldSystemUserId = model.OldSystemUserId,
                NewSystemUserId = model.NewSystemUserId
            });
        }

        /// <summary>
        /// 5. (Optional) Example: Get all users (for debugging).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }

        // Helper method to generate a random 6-digit OTP
        private string GenerateRandomOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}
