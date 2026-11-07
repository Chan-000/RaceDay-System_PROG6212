using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

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

        //Event details
        public async Task<IActionResult> Details(int id)
        {
            var accessResult = CheckParticipantAccess();
            if (accessResult != null)
                return accessResult;

            var eventResponse = await _api.SendAsync(HttpMethod.Get, "api/events/" + id);

            if (!eventResponse.IsSuccessStatusCode)
            {
                TempData["Error"] = "The selected event could not be found.";
                return RedirectToAction(nameof(Index));
            }

            var eventItem = await _api.ReadAsync<EventDto>(eventResponse);

            if (eventItem == null)
            {
                TempData["Error"] = "Unable to load event details.";
                return RedirectToAction(nameof(Index));
            }

            var categoryResponse = await _api.SendAsync(HttpMethod.Get, "api/categories/events/" + id);

            if (!categoryResponse.IsSuccessStatusCode)
            {
                TempData["Error"] = "Unable to load event categories.";
                return RedirectToAction(nameof(Index));
            }

            var categories = await _api.ReadAsync<List<CategoryDto>>(categoryResponse) ?? new List<CategoryDto>();

            var model = new ParticipantEventDetailsVm
            {
                Event = eventItem,
                Categories = categories
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnterEvent(int eventId, int categoryId)
        {
            var accessResult = CheckParticipantAccess();
            if (accessResult != null)
                return accessResult;

            if (categoryId <= 0)
            {
                TempData["Error"] = "Please select a category before entering.";
                return RedirectToAction(nameof(Details), new {id = eventId});
            }

            var request = new CreateEnrolmentRequest
            {
                CategoryID = categoryId
            };

            var response = await _api.SendAsync(HttpMethod.Post, "api/enrolments/events/" + eventId + "/enrolments", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "You have successfully entered the event!.";
                return RedirectToAction(nameof(MyEvents));
            }

            var errorMessage = await response.Content.ReadAsStringAsync();

            TempData["Error"] = string.IsNullOrWhiteSpace(errorMessage)
                ? "Your event entry could not be completed. Please try again."
                : errorMessage;

            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        [HttpGet]
        public async Task<IActionResult> MyEvents()
        {
            var access = CheckParticipantAccess();
            if (access != null) return access;

            var response = await _api.SendAsync(HttpMethod.Get, "api/enrolments/me");
            var enrolments = await _api.ReadAsync<List<EnrolmentDto>>(response) ?? new List<EnrolmentDto>();

            return View(enrolments);
        }

    }
}
