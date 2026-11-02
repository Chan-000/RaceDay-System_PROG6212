using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.Contracts;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    // Handles creating, viewing, upfating and deleting categories for events
    // Only the Organiser who owns the events can manage its categories

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
        [HttpGet("events/{eventId:int}")]
        public async Task<IActionResult> GetCategories(int eventId)
        {
            var exists = await _context.Events.AnyAsync(e => e.EventID == eventId);
            if (!exists)
                return NotFound("Event not Found");

            var categories = await _context.Categories
                .Where(c => c.EventID == eventId)
                .ToListAsync();

            return Ok(categories);
        }

        //POST:/api/events/{eventId}/categories
        [HttpPost("events/{eventId:int}")]
        public async Task<IActionResult> CreateCategory(int eventId, CreateCategoryRequest request)
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return Unauthorized("Please log in first.");

            if (!await OwnsEvent(eventId))
                return StatusCode(403, "You do not have permission to perform this action.");

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

            return StatusCode(201, category);
        }

        //PUT:/api/categories/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCategory(int id, CreateCategoryRequest request)
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return Unauthorized("Please log in first.");

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            if (!await OwnsEvent(category.EventID))
                return StatusCode(403, "You do not have permission to perform this action.");

            category.CategoryName = request.CategoryName.Trim();
            category.AgeMin = request.AgeMin;
            category.AgeMax = request.AgeMax;
            category.EntryFee = request.EntryFee;

            await _context.SaveChangesAsync();
            return Ok(category);
        }

        //DELETE: /api/categories/{id}
        [HttpDelete("{id:int}")]
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
                return StatusCode(403, "You do not have permission to perform this action.");

            try
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict("This category has enrolments." +
                    "Remove the related enrolments before deleting it.");
            }
            
        }
    }
}
