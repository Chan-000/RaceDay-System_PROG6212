using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
using RaceDay.Contracts;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly RaceDayApiClient _api;

        public OrganiserController(RaceDayApiClient api)
        {
            _api = api;
        }

        private bool IsLoggedIn()
        {
            return HttpContext.Session.GetInt32("UserId").HasValue;
        }

        private bool IsOrganiser()
        {
            return string.Equals(
                HttpContext.Session.GetString("Role"),
                "Organiser", StringComparison.OrdinalIgnoreCase);
        }

        private async Task LoadEventTypes(EventFormVm model)
        {
            using var response = await _api.SendAsync(HttpMethod.Get, "api/eventtypes");

            if (response.IsSuccessStatusCode)
            {
                model.EventTypes = await _api.ReadAsync<List<EventTypeOptionVm>>(response) 
                    ?? new List<EventTypeOptionVm>();
            }
        }
        public IActionResult Index()
        {

            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!IsOrganiser())
            {
                TempData["Error"] = "Access denied. Organisers only.";
                return RedirectToAction("Index", "Participant");
            }

            ViewBag.DisplayName = HttpContext.Session.GetString("DisplayName");
            
            return View();
        }

        //Events
        public async Task<IActionResult> Events()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!IsOrganiser())
            {
                TempData["Error"] = "Access denied. Organisers only.";
                return RedirectToAction("Index", "Participant");
            }

            using var response = await _api.SendAsync(HttpMethod.Get, "api/events/mine");

            if (!response.IsSuccessStatusCode)
            {
                TempData["error"] = await response.Content.ReadAsStringAsync();

                return View(new List<EventDto>());
            }

            var events = await _api.ReadAsync<List<EventDto>>(response) 
                ?? new List<EventDto>();

            return View(events);
        }

        [HttpGet]
        public async Task<IActionResult> CreateEvent()
        {

            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!IsOrganiser())
            {
                TempData["Error"] = "Access denied. Organisers only.";
                return RedirectToAction("Index", "Participant");
            }

            var model = new EventFormVm();
            await LoadEventTypes(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(EventFormVm model)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!IsOrganiser())
            {
                TempData["Error"] = "Access denied. Organisers only.";
                return RedirectToAction("Index", "Participant");
            }

            if (!ModelState.IsValid)
            {
                await LoadEventTypes(model);
                return View(model);
            }

            var request = new CreateEventRequest
            {
                EventName = model.EventName,
                Description = model.Description,
                EventDate = model.EventDate,
                Location = model.Location,
                DistanceKm = model.DistanceKm,
                MaxParticipants = model.MaxParticipants,
                EventTypeID = model.EventTypeID
            };

            using var response = await _api.SendAsync(HttpMethod.Post, "api/events", request);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await response.Content.ReadAsStringAsync());
                await LoadEventTypes(model);
                return View(model);
            }

            TempData["Success"] = "Event created successufully";
            return RedirectToAction(nameof(Events));
        }

    }
}
