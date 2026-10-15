using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.Contracts;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventsController(RaceDayDbContext context)
        {
            _context = context;
        }

        private bool IsOrganiser()
        {
            return HttpContext.Session.GetInt32("UserID") != null
                && HttpContext.Session.GetString("Role") == "Organiser";
        }

        // GET: api/events
        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _context.Events
                .Include(e => e.EventType)
                .Include(e => e.Categories)
                .ToListAsync();

            return Ok(events);
        }

        //GET: api/events/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvent(int id)
        {
            var item = await _context.Events
                .Include(e => e.EventType)
                .Include(e => e.Categories)
                .SingleOrDefaultAsync(e => e.EventID == id);

            return item == null ? NotFound() : Ok(item);
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMyEvents()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return Unauthorized("Please log in first;");

            if (HttpContext.Session.GetString("Role") != "Organiser")
                return Forbid();

            var events = await _context.Events
                .Where(e => e.OrganiserID == userId.Value)
                .Include(e => e.EventType)
                .ToListAsync();

            return Ok(events);
        }

        //POST: api/events
        [HttpPost]
        public async Task<IActionResult> CreateEvent(CreateEventRequest request)
        {
            if (!IsOrganiser())
                return Unauthorized("Only organiser can create events");

            if (!await _context.EventTypes.AnyAsync(
                t => t.EventTypeID == request.EventTypeID))
                return BadRequest("The requested types does not exist.");

            var item = new Event
            {
                EventName = request.EventName.Trim(),
                Description = request.Description,
                EventDate = request.EventDate,
                Location = request.Location.Trim(),
                DistanceKm = request.DistanceKm,
                MaxParticipants = request.MaxParticipants,
                EventTypeID = request.EventTypeID,
                OrganiserID = HttpContext.Session.GetInt32("UserID")!.Value
            };

            _context.Events.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEvent), new { id = item.EventID }, item);
        }

        //PUT: api/events/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEvent(int id, CreateEventRequest request)
        {
            if (!IsOrganiser())
                return Unauthorized("Only organisers can update events");

            var item = await _context.Events.FindAsync(id);

            if (item == null)
                return NotFound();

            var userId = HttpContext.Session.GetInt32("UserID")!.Value;

            if (item.OrganiserID != userId)
                return Forbid();

            if(!await _context.EventTypes.AnyAsync(
               t => t.EventTypeID == request.EventTypeID))
               return BadRequest("The selected event type does not exist.");

            item.EventName = request.EventName.Trim();
            item.Description = request.Description;
            item.EventDate = request.EventDate;
            item.Location = request.Location;
            item.DistanceKm = request.DistanceKm;
            item.MaxParticipants = request.MaxParticipants;
            item.EventTypeID = request.EventTypeID;

            await _context.SaveChangesAsync();
            return Ok(item);
        }

        //DELETE: api/events/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (!IsOrganiser())
                return Unauthorized("Only organisers can delete events.");

            var item = await _context.Events.FindAsync(id);

            if (item == null)
                return NotFound();

            var userId = HttpContext.Session.GetInt32("UserID")!.Value;

            if (item.OrganiserID != userId)
                return Forbid();

            _context.Events.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
