using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.Contracts;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public UsersController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/profile/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null) 
                return Unauthorized("Please log in first");

            var user = await _context.Users
                .Where(u => u.UserID == userId.Value)
                .Select(u => new ProfileDto
                {
                    UserID = u.UserID,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Role = u.Role,
                    PhoneNumber = u.PhoneNumber
                })
                .SingleOrDefaultAsync();

            return user == null ? NotFound() : Ok(user);
        }

        // PUT:/api/users/me
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(UpdateProfileRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return Unauthorized("Please log in first");

            var user = await _context.Users.FindAsync(userId.Value);

            if (user == null)
                return NotFound();

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.PhoneNumber = request.PhoneNumber;

            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                UserID = user.UserID,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber,
                Message = "Profile updated successfully"
            });
        }
    }
}
