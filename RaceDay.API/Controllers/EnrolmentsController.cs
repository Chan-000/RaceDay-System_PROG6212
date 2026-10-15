using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.Contracts;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EnrolmentsController(RaceDayDbContext context)
        {
            _context = context;
        }

        [HttpPost("events/{eventId:int}/Enrolments")]
        public async Task<IActionResult> Enrol(int eventId, CreateEnrolmentRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null || role != "Participant")
                return Unauthorized("Only Partcipants can enrol in events");

            var eventItem = await _context.Events
                .SingleOrDefaultAsync(e => e.EventID == eventId);

            if (eventItem == null)
                return NotFound("Event not found.");

            if (eventItem.EventDate.Date < DateTime.UtcNow.Date)
                return BadRequest("You cannot enrol in a past event");

            var category = await _context.Categories
                .SingleOrDefaultAsync(c => 
                c.CategoryID == request.CategoryID &&
                c.EventID == eventId);

            if (category == null)
                return BadRequest("The selected does not belong to this event.");

            var alreadyEnrolled = await _context.Enrolments.AnyAsync(e =>
                e.ParticipantID == userId.Value &&
                e.EventID == eventId);

            if (alreadyEnrolled)
                return Conflict("You are already enrolled in this event.");

            var confirmedCount = await _context.Enrolments.CountAsync(e =>
                e.EventID == eventId &&
                e.EnrolmentStatus == "Confirmed");

            if (confirmedCount >= eventItem.MaxParticipants)
                return Conflict("This event has reached its maximum partcipant limit.");

            var enrolment = new Enrolment
            {
                ParticipantID = userId.Value,
                EventID = eventId,
                CategoryID = request.CategoryID,
                EnrolmentDate = DateTime.Today,
                EnrolmentStatus = "confirmed"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return StatusCode(201, enrolment);
        }

        //GET: api/enrolments/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return Unauthorized("Please log in first.");

            if (HttpContext.Session.GetString("Role") != "Participant")
                return Forbid();

            var enrolments = await _context.Enrolments
                .Where(e => e.ParticipantID == userId.Value)
                .Include(e => e.Event)
                .Include(e => e.Category)
                .ToListAsync();

            return Ok(enrolments);
        }
        //GET:api/enrolment/event/{eventId}
        [HttpGet("events/{eventId:int}/enrolments")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (HttpContext.Session.GetString("Role") != "Organiser")
                return Forbid();

            var eventExists = await _context.Events.AnyAsync(e =>
                e.EventID == eventId && e.OrganiserID == userId);

            if (!eventExists)
                return NotFound("Event not found or you do not own it");

            var enrolments = await _context.Enrolments
                .Where(e => e.EventID == eventId)
                .Include(e => e.Participant)
                .Include(e => e.Category)
                .ToListAsync();

            return Ok(enrolments);
        }
    }
}
