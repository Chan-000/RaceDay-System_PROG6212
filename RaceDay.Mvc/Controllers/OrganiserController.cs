using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
using RaceDay.Contracts;
using RaceDay.Mvc.ViewModels;
using Microsoft.AspNetCore.Identity;

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

        private IActionResult? CheckOrganiserAccess()
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
            return null;
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
            var accessResult = CheckOrganiserAccess();
            if (accessResult != null)
                return accessResult;

            ViewBag.DisplayName = HttpContext.Session.GetString("DisplayName");
            
            return View();
        }

        //Events
        public async Task<IActionResult> Events()
        {
            var accessResult = CheckOrganiserAccess();
            if (accessResult != null)
                return accessResult;

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
            var accessResult = CheckOrganiserAccess();
            if (accessResult != null)
                return accessResult;

            var model = new EventFormVm();
            await LoadEventTypes(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(EventFormVm model)
        {
            var accessResult = CheckOrganiserAccess();
            if (accessResult != null)
                return accessResult;

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

        //edit events
        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var accessResult = CheckOrganiserAccess();
            if (accessResult != null)
                return accessResult;

            using var response = await _api.SendAsync(HttpMethod.Get, "api/events/" + id);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Could not find the event you want to edit.";

                return RedirectToAction(nameof(Events));
            }

            var eventItem = await _api.ReadAsync<EventDto>(response);

            if (eventItem == null)
            {
                TempData["Error"] = "The event details could not be loaded";

                return RedirectToAction(nameof(Events));
            }

            var model = new EventFormVm
            {
                EventID = eventItem.EventID,
                EventName = eventItem.EventName,
                Description = eventItem.Description,
                EventDate = eventItem.EventDate,
                Location = eventItem.Location,
                DistanceKm = eventItem.DistanceKm,
                MaxParticipants = eventItem.MaxParticipants,
                EventTypeID = eventItem.EventTypeID
            };

            await LoadEventTypes(model);
            return View(model);
        }

        //Save changes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, EventFormVm model)
        {
            var accessResult = CheckOrganiserAccess();
            if (accessResult != null)
                return accessResult;

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

            using var response = await _api.SendAsync(HttpMethod.Put, "api/events/" + id, request);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await response.Content.ReadAsStringAsync());

                await LoadEventTypes(model);

                return View(model);
            }

            TempData["Success"] = "Event updated successfully!";
            return RedirectToAction(nameof(Events));
        }

        //delete event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var accessResult = CheckOrganiserAccess();
            if (accessResult != null)
                return accessResult;

            using var response = await _api.SendAsync(HttpMethod.Delete, "api/events/" + id);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await response.Content.ReadAsStringAsync();

                return RedirectToActionPermanent(nameof(Events));
            }

            TempData["Success"] = "Event deleted successfully!";
            return RedirectToAction(nameof(Events));
        }

        [HttpGet]
        public async Task<IActionResult> Categories(int eventId)
        {
            var accessResult = CheckOrganiserAccess();
            if (accessResult != null)
                return accessResult;

            var eventResponse = await _api.SendAsync(HttpMethod.Get, "api/events/" + eventId);
            if (!eventResponse.IsSuccessStatusCode)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction(nameof(Events));
            }
            var eventItem = await _api.ReadAsync<EventDto>(eventResponse);

            var response = await _api.SendAsync(HttpMethod.Get, "api/categories/events/" + eventId);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Could not load categories";
                return View(new List<CategoryDto>());
            }
            var categories = await _api.ReadAsync<List<CategoryDto>>(response) ?? new List<CategoryDto>();

            ViewBag.EventId = eventId;
            ViewBag.EventName = eventItem?.EventName ?? "Event";

            return View(categories);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCategory(int eventId)
        {
            var accessResult = CheckOrganiserAccess();
            if (accessResult != null)
                return accessResult;

            using var response = await _api.SendAsync(HttpMethod.Get, "api/events/" + eventId);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Event not found.";
                return Redirect(nameof(Events));
            }

            var eventItem = await _api.ReadAsync<EventDto>(response);
            
            ViewBag.EventId = eventId;
            ViewBag.EventName = eventItem?.EventName ?? "Event";

            return View(new CreateCategoryRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(int eventId, CreateCategoryRequest model)
        {
            var accessResult = CheckOrganiserAccess();
            if (accessResult != null)
                return accessResult;

            if (!ModelState.IsValid)
            {
                ViewBag.EventId = eventId;
                return View(model);
            }

            using var response = await _api.SendAsync(HttpMethod.Post, "api/categories/events/" + eventId, model);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", error);
                ViewBag.EventId = eventId;
                return View(model);
            }

            TempData["Success"] = "Category created successfully!";
            return RedirectToAction(nameof(Categories), new {eventId});
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id, int eventId)
        {
            var accessResult = CheckOrganiserAccess();
            if (accessResult != null)
                return accessResult;

            using var response = await _api.SendAsync(HttpMethod.Get, "api/categories/events/" + eventId);

            if (!response.IsSuccessStatusCode)
                return NotFound("Could not load categories");

            var categories = await _api.ReadAsync<List<CategoryDto>>(response) ?? new List<CategoryDto>();

            var category = categories.FirstOrDefault(c => c.CategoryID == id);

            if (category == null)
                return NotFound("Category not found.");

            var model = new CreateCategoryRequest
            {
                CategoryName = category.CategoryName,
                AgeMin = category.AgeMin,
                AgeMax = category.AgeMax,
                EntryFee = category.EntryFee
            };

            ViewBag.EventId = eventId;
            ViewBag.CategoryId = id;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, int eventId, CreateCategoryRequest model)
        {
            var accessResult = CheckOrganiserAccess();
            if (accessResult != null)
                return accessResult;

            if (!ModelState.IsValid)
            {
                ViewBag.EventId = eventId;
                ViewBag.CategoryId = id;

                return View(model);
            }

            var response = await _api.SendAsync(HttpMethod.Put, "api/categories/" + id, model);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", error);
                ViewBag.EventId = eventId;
                ViewBag.CategoryId = id;

                return View(model);
            }

            TempData["Success"] = "Category updated successfully";

            return RedirectToAction(nameof(Categories), new { eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id, int eventId)
        {
            var accessResult = CheckOrganiserAccess();
            if (accessResult != null)
                return accessResult;

            var response = await _api.SendAsync(HttpMethod.Delete, "api/categories/" + id);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Category deleted successfully";
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();

                TempData["Error"] = error;
            }
            return RedirectToAction(nameof(Categories), new {eventId});
        }
    }
}
