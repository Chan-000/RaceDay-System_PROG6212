using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.Contracts;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        private bool IsOrganiser()
        {
            return HttpContext.Session.GetInt32("UsersID") != null
                && HttpContext.Session.GetString("Role") == "Organiser";
        }

        [HttpPost]
        public async Task<IActionResult> CreateResult(CreateResultRequest request)
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return Unauthorized("Please log in first.");

            if (!IsOrganiser())
                return Forbid();

            if (request.FinishTime < TimeSpan.Zero)
                return BadRequest("Finish time cannot be negative");

            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .SingleOrDefaultAsync(e => e.EnrolmentID == request.EnrolmentID);

            if (enrolment == null)
                return NotFound("Enrolment not found.");

            var userId = HttpContext.Session.GetInt32("UserID")!.Value;

            if (enrolment.Event?.OrganiserID != userId)
                return Forbid();

            if (enrolment.EnrolmentStatus != "Confirmed")
                return BadRequest("A result can only be recorded for a confirmed enrolment.");

            if (await _context.Results.AnyAsync(
                r => r.EnrolmentID == request.EnrolmentID))
                return Conflict("A result already exists for this enrolment");

            var result = new Result
            {
                EnrolmentID = request.EnrolmentID,
                FinishTime = request.FinishTime,
                FinishPosition = request.FinishPosition
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return StatusCode(201, result);
        }

        //PUT: api/results/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateResult(int id, CreateResultRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null || role != "Organiser")
                return Unauthorized();

            var result = await _context.Results
                .Include(r => r.Enrolment!)
                    .ThenInclude(e => e.Event)
                .SingleOrDefaultAsync(r => r.ResultID == id);

            if (result == null)
                return NotFound();

            if (result.Enrolment?.Event?.OrganiserID != userId)
                return Forbid();

            result.FinishTime = result.FinishTime;
            result.FinishPosition = result.FinishPosition;

            await _context.SaveChangesAsync();
            return Ok(result);
        }

        //GET: api/results/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMyResults()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized("Please log in first");

            if (role != "Participant")
                return Forbid();

            var results = await _context.Results
                .Include(r => r.Enrolment!)
                   .ThenInclude(e => e.Event)
                .Include(r => r.Enrolment!)
                   .ThenInclude(e => e.Category)
                .Where(r => r.Enrolment != null &&  r.Enrolment.ParticipantID == userId)
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/results/event/{eventId}
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetResultsByEvent(int eventId)
        {
            var results = await _context.Results
                .Include(r => r.Enrolment!)
                    .ThenInclude(e => e.Participant)
                    .Where(r => r.Enrolment != null && r.Enrolment.EventID == eventId)
                    .ToListAsync();

            return Ok(results);
        }
    }
}
