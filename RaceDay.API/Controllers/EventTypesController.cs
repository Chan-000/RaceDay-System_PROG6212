using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]
    public class EventTypesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventTypesController(RaceDayDbContext context)
        {
            _context = context;
        }

        //GET: api/eventtypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventType>>> GetEventTypes()
        {
            var eventTypes = await _context.EventTypes.ToListAsync();

            return Ok(eventTypes);
        }

    }
}
