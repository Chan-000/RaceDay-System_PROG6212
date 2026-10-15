using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.Contracts;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        private async Task<bool> OwnsEvent (int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null ||
                HttpContext.Session.GetString("Role") != "Organiser")
                return false;

            return await _context.Events.AnyAsync(e =>
                e.EventID == eventId && e.OrganiserID == userId.Value);
        }

        //GET:/api/events/{eventId}/categories
        [HttpGet("events/{eventId:int}/categories")]
        public async Task<IActionResult> GetCategories(int eventId)
        {
            if (!await _context.Events.AnyAsync(e => e.EventID == eventId))
                return NotFound("Event not Found");

            var categories = await _context.Categories
                .Where(c => c.EventID == eventId)
                .ToListAsync();

            return Ok(categories);
        }

        //POST:/api/events/{eventId}/categories
        [HttpPost("events/{eventId:int}/categories")]
        public async Task<IActionResult> CreateCategory(int eventId, CreateCategoryRequest request)
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return Unauthorized("Please log in first.");

            if (!await OwnsEvent(eventId))
                return Forbid();

            if (request.AgeMin.HasValue && request.AgeMin < 0)
                return BadRequest("Minimum age cannot be negative.");

            if (request.AgeMax.HasValue && request.AgeMax < 0)
                return BadRequest("Maximum age cannot be negative.");

            if (request.AgeMin.HasValue && request.AgeMax.HasValue && 
                request.AgeMin > request.AgeMax)
                return BadRequest("Minimum age cannot exceed maximum age.");

            var category = new Category
            {
                EventID = eventId,
                CategoryName = request.CategoryName.Trim(),
                AgeMin = request.AgeMin,
                AgeMax = request.AgeMax,
                EntryFee = request.EntryFee,
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(category);
        }

        //PUT:/api/categories/{id}
        [HttpPut("events/{eventId:int}/categories/{categoryId:int}")]
        public async Task<IActionResult> UpdateCategory(int eventId, int categoryId, CreateCategoryRequest request)
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return Unauthorized("Please log in first.");

            if (!await OwnsEvent(eventId))
                return Forbid();

            if (request.AgeMin.HasValue && request.AgeMin < 0)
                return BadRequest("Minimum age cannot be negative.");

            if (request.AgeMax.HasValue && request.AgeMax < 0)
                return BadRequest("Maximum age cannot be negative.");

            if (request.AgeMin.HasValue && request.AgeMax.HasValue &&
                request.AgeMin > request.AgeMax)
                return BadRequest("Minimum age cannot exceed maximum age.");

            var category = await _context.Categories.SingleOrDefaultAsync(
                c => c.CategoryID == categoryId && c.EventID == eventId);

            if (category == null)
                return NotFound();

            category.CategoryName = request.CategoryName.Trim();
            category.AgeMin = request.AgeMin;
            category.AgeMax = request.AgeMax;
            category.EntryFee = request.EntryFee;

            await _context.SaveChangesAsync();
            return Ok(category);
        }

        //DELETE: /api/categories/{id}
        [HttpDelete("categories/{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return Unauthorized("Please log in first.");

            var category = await _context.Categories
                .Include(c => c.Event)
                .SingleOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound();

            if (!await OwnsEvent(category.EventID))
                return Forbid();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
