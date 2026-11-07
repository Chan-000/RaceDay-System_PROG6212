using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly RaceDayApiClient _api;

        public ParticipantController(RaceDayApiClient api)
        {
            _api = api;
        }

        private bool IsLoggedIn()
        {
            return HttpContext.Session.GetInt32("UserId").HasValue;
        }

        private bool IsParticipant()
        {
            return string.Equals(
                HttpContext.Session.GetString("Role"), 
                "Participant", StringComparison.OrdinalIgnoreCase);
        }

        private IActionResult? CheckParticipantAccess()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!IsParticipant())
            {
                TempData["Error"] = "Access denied. Participants only.";
                return RedirectToAction("Index", "Organiser");
            }
            return null;
        }

        //Participant Index
        public async Task<IActionResult> Index(string? type, string? search)
        {
            var accessResult = CheckParticipantAccess();
            if (accessResult != null)
                return accessResult;

            ViewBag.DisplayName = HttpContext.Session.GetString("DisplayName");

            var response = await _api.SendAsync(HttpMethod.Get, "api/events");
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Unable to load events. Please try again";
                return View(new List<EventDto>());
            }

            var events = await _api.ReadAsync<List<EventDto>>(response) ?? new List<EventDto>();

            ViewBag.EventTypes = events
                .Where(e => !string.IsNullOrWhiteSpace(e.EventTypeName))
                .Select(e => e.EventTypeName!)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            ViewBag.SelectedType = type;
            ViewBag.Search = search;

            var filteredEvents = events.Where(e => e.EventDate.Date >= DateTime.Today);

            if (!string.IsNullOrWhiteSpace(type))
            {
                filteredEvents = filteredEvents.Where(e => 
                        string.Equals(e.EventTypeName, type, StringComparison.OrdinalIgnoreCase ));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                filteredEvents = filteredEvents.Where(e =>
                       (e.EventName != null &&
                        e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase))
                       ||
                       (e.Location != null &&
                        e.Location.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            var upcomingEvents = filteredEvents
                .OrderBy(e => e.EventDate)
                .ToList();

            return View(upcomingEvents);

        }


    }
}
