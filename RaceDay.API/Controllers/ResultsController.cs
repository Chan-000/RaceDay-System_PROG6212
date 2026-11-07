using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.Contracts;
using RaceDay.API.Models;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace RaceDay.API.Controllers
{
    //Allowws organisers to capture race results and particpants to view their persona results

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
            return HttpContext.Session.GetInt32("UserID") != null
                && HttpContext.Session.GetString("Role") == "Organiser";
        }

        [HttpPost]
        public async Task<IActionResult> CreateResult(CreateResultRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return Unauthorized("Please log in first.");

            if (!IsOrganiser())
                return StatusCode(403, "You do not have permission to perform this action.");

            if (request.FinishTime == null || request.FinishTime <= TimeSpan.Zero)
            {
                return BadRequest("Finish time must be greater than zero.");
            }

            if (request.FinishPosition == null || request.FinishPosition < 1)
            {
                return BadRequest("Finishing position must be at least 1.");
            }

            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .SingleOrDefaultAsync(e => e.EnrolmentID == request.EnrolmentID);

            if (enrolment == null)
                return NotFound("Enrolment not found.");

            if (enrolment.Event?.OrganiserID != userId.Value)
            {
                return BadRequest("You do not have permission to record a result for this event.");
            }

            if (enrolment.EnrolmentStatus != "Confirmed")
            {
                return BadRequest("A result can only be recorded for a confirmed enrolment.");
            }

            bool resultExists = await _context.Results.AnyAsync(r => r.EnrolmentID == request.EnrolmentID);

            if (resultExists)
            {
                return Conflict("A result already exists for this enrolment.");
            }

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

            if (userId == null)
                return Unauthorized("Please log in first");

            if (!IsOrganiser())
            {
                return StatusCode(403, "You do not have permission to perform this action.");
            }

            if (request.FinishTime == null ||
                request.FinishTime <= TimeSpan.Zero)
            {
                return BadRequest(
                    "Finish time must be greater than zero.");
            }

            if (request.FinishPosition == null ||
                request.FinishPosition < 1)
            {
                return BadRequest(
                    "Finishing position must be at least 1.");
            }

            var result = await _context.Results
                .Include(r => r.Enrolment!)
                    .ThenInclude(e => e.Event)
                .SingleOrDefaultAsync(r => r.ResultID == id);

            if (result == null)
                return NotFound();

            if (result.Enrolment?.Event?.OrganiserID != userId)
                return StatusCode(403, "You do not have permission to perform this action.");

            if (result.EnrolmentID != request.EnrolmentID)
            {
                return BadRequest("The result must remain to its orginal enrolment.");
            }

            result.FinishTime = request.FinishTime;
            result.FinishPosition = request.FinishPosition;

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
                return StatusCode(403, "You do not have permission to perform this action.");

            var results = await _context.Results
                .Where(r => r.Enrolment != null && r.Enrolment.ParticipantID == userId.Value)
                .Select(r => new ResultDto
                {
                    ResultID = r.ResultID,
                    EnrolmentID = r.EnrolmentID,
                    ParticipantID = userId.Value,

                    ParticipantName = r.Enrolment != null && r.Enrolment.Participant != null
                        ? r.Enrolment.Participant.FirstName + " " + r.Enrolment.Participant.LastName
                        : null,
                    EventID = r.Enrolment != null ? r.Enrolment.EventID : 0,

                    EventName = r.Enrolment != null && r.Enrolment.Event != null
                        ? r.Enrolment.Event.EventName
                        : null,
                    EventDate = r.Enrolment != null && r.Enrolment.Event != null
                        ? r.Enrolment.Event.EventDate
                        : null,

                    CategoryName = r.Enrolment != null && r.Enrolment.Category != null
                        ? r.Enrolment.Category.CategoryName
                        : null,
                    FinishTime = r.FinishTime,
                    FinishPosition = r.FinishPosition
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/results/{id} - for Organiser edit screen
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetResult(int id)
        {
            var result = await _context.Results
                .Where(r => r.ResultID == id)
                .Select(r => new ResultDto
                {
                    ResultID = r.ResultID,
                    EnrolmentID = r.EnrolmentID,
                    ParticipantID = r.Enrolment != null ? r.Enrolment.ParticipantID : 0,

                    ParticipantName = r.Enrolment != null && r.Enrolment.Participant != null
                        ? r.Enrolment.Participant.FirstName + " " + r.Enrolment.Participant.LastName
                        : null,
                    EventID = r.Enrolment != null ? r.Enrolment.EventID : 0,

                    EventName = r.Enrolment != null && r.Enrolment.Event != null
                        ? r.Enrolment.Event.EventName
                        : null,
                    EventDate = r.Enrolment != null && r.Enrolment.Event != null
                        ? r.Enrolment.Event.EventDate
                        : null,

                    CategoryName = r.Enrolment != null && r.Enrolment.Category != null
                        ? r.Enrolment.Category.CategoryName
                        : null,
                    FinishTime = r.FinishTime,
                    FinishPosition = r.FinishPosition
                })
                .SingleOrDefaultAsync();

            if (result == null) return NotFound("Result not found.");
            return Ok(result);
        }


        // GET: api/results/event/{eventId}
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetResultsByEvent(int eventId)
        {
            var results = await _context.Results
                .Where(r => r.Enrolment != null && r.Enrolment.EventID == eventId)
                .Select(r => new ResultDto
                {
                    ResultID = r.ResultID,
                    EnrolmentID = r.EnrolmentID,
                    ParticipantID = r.Enrolment != null ? r.Enrolment.ParticipantID : 0,

                    ParticipantName = r.Enrolment != null && r.Enrolment.Participant != null
                        ? r.Enrolment.Participant.FirstName + " " + r.Enrolment.Participant.LastName
                        : null,
                    EventID = r.Enrolment != null ? r.Enrolment.EventID : 0,

                    EventName = r.Enrolment != null && r.Enrolment.Event != null
                        ? r.Enrolment.Event.EventName
                        : null,
                    EventDate = r.Enrolment != null && r.Enrolment.Event != null
                        ? r.Enrolment.Event.EventDate
                        : null,
                    CategoryName = r.Enrolment != null && r.Enrolment.Category != null
                        ? r.Enrolment.Category.CategoryName
                        : null,
                    FinishTime = r.FinishTime,
                    FinishPosition = r.FinishPosition,

                })
                .OrderBy(r => r.FinishPosition)
                .ToListAsync();

            return Ok(results);
        }
    }
}
