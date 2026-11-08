using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Services;
using RaceDay.Contracts;

namespace RaceDay.API.Controllers
{
    //Manage RaceDay events
    //Organisers can create,update, and delete their own events oth roles can view events

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
                .Select(e => new EventDto
                {
                    EventID = e.EventID,
                    EventName = e.EventName,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    BannerImageUrl = e.BannerImageUrl,
                    DistanceKm = e.DistanceKm,
                    MaxParticipants = e.MaxParticipants,
                    EventTypeID = e.EventTypeID,
                    EventTypeName = e.EventType != null ? e.EventType.TypeName : null,
                    OrganiserID = e.OrganiserID
                })
                .ToListAsync();

            return Ok(events);
        }

        //GET: api/events/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvent(int id)
        {
            var item = await _context.Events
                .Include(e => e.EventType)
                .Where(e => e.EventID == id)
                .Select(e => new EventDto
                {
                    EventID = e.EventID,
                    EventName = e.EventName,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    BannerImageUrl = e.BannerImageUrl,
                    DistanceKm = e.DistanceKm,
                    MaxParticipants = e.MaxParticipants,
                    EventTypeID = e.EventTypeID,
                    EventTypeName = e.EventType != null ? e.EventType.TypeName : null,
                    OrganiserID = e.OrganiserID
                })
                .FirstOrDefaultAsync();

            return item == null ? NotFound() : Ok(item);
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMyEvents()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return Unauthorized("Please log in first;");

            if (HttpContext.Session.GetString("Role") != "Organiser")
                return StatusCode(403, "You do not have permission to perform this action.");

            var events = await _context.Events
                .Include(e => e.EventType)
                .Where(e => e.OrganiserID == userId.Value)
                .Select(e => new EventDto
                {
                    EventID = e.EventID,
                    EventName = e.EventName,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    BannerImageUrl = e.BannerImageUrl,
                    DistanceKm = e.DistanceKm,
                    MaxParticipants = e.MaxParticipants,
                    EventTypeID = e.EventTypeID,
                    EventTypeName = e.EventType != null ? e.EventType.TypeName : null,
                    OrganiserID = e.OrganiserID
                })
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
                return StatusCode(403, "You do not have permission to perform this action.");

            if (!await _context.EventTypes.AnyAsync(
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


        [HttpPost("{id:int}/banner")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadEventBanner(
            int id,
            IFormFile file,
            [FromServices] BlobStorageService blobStorage)
        {
            if (!IsOrganiser())
                return Unauthorized("Only organisers can upload event banners.");

            var item = await _context.Events.FindAsync(id);

            if (item == null)
                return NotFound("Event not found.");

            var userId = HttpContext.Session.GetInt32("UserID")!.Value;

            if (item.OrganiserID != userId)
                return StatusCode(403, "You can only upload banners for your own events.");

            try
            {
                var imageUrl = await blobStorage.UploadAsync(file, "event-banners");

                item.BannerImageUrl = imageUrl;

                await _context.SaveChangesAsync();

                return Ok(new UploadResponse
                {
                    FileName = file.FileName,
                    Url = imageUrl,
                    Message = "Event banner uploaded successfully."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
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
                return StatusCode(403, "You do not have permission to perform this action.");

            try
            {
                _context.Events.Remove(item);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict("This event has related categories or enrolment." +
                    " Remove the related records before deleting it.");
            }
            
        }
    }
}
