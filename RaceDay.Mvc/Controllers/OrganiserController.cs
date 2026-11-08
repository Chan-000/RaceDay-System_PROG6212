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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadEventBanner(int id, IFormFile file)
        {
            var accessResult = CheckOrganiserAccess();

            if (accessResult != null)
                return accessResult;

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select an image to upload.";
                return RedirectToAction(nameof(EditEvent), new { id });
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "The image must not be larger than 5MB.";
                return RedirectToAction(nameof(EditEvent), new { id });
            }

            using var response = await _api.UploadFileAsync("api/events/" + id + "/banner", file);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Event banner uploaded successfully!";
                return RedirectToAction(nameof(EditEvent), new { id });
            }

            string errorMessage = await response.Content.ReadAsStringAsync();

            TempData["Error"] = "Unable to upload the event banner. " + errorMessage;

            return RedirectToAction(nameof(EditEvent), new { id });
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

        
        // ====== ENROLMENTS =====

        // Display enrolments for all events owned by the organiser
        [HttpGet]
        public async Task<IActionResult> Enrolments()
        {
            var accessResult = CheckOrganiserAccess();

            if (accessResult != null)
                return accessResult;

            // Get the organiser's own events
            using var eventsResponse = await _api.SendAsync(
                HttpMethod.Get,
                "api/events/mine");

            if (!eventsResponse.IsSuccessStatusCode)
            {
                TempData["Error"] = "Could not load your events.";
                return View(new List<EventEnrolmentsVm>());
            }

            var events = await _api.ReadAsync<List<EventDto>>(eventsResponse)
                ?? new List<EventDto>();

            var result = new List<EventEnrolmentsVm>();

            foreach (var eventItem in events)
            {
                using var enrolmentsResponse = await _api.SendAsync(HttpMethod.Get,
                    "api/enrolments/events/" + eventItem.EventID + "/enrolments");
                    
                if (!enrolmentsResponse.IsSuccessStatusCode)
                {  
                    continue;
                }

                var enrolments = await _api.ReadAsync<List<EnrolmentDto>>(enrolmentsResponse)
                    ?? new List<EnrolmentDto>();

                result.Add(new EventEnrolmentsVm
                {
                    EventID = eventItem.EventID,
                    EventName = eventItem.EventName,
                    Enrolments = enrolments
                });
            }

            return View(result);
        }

        //====Results====
        [HttpGet]
        public async Task<IActionResult> CaptureResult( int eventId, int enrolmentId)
        {
            var accessResult = CheckOrganiserAccess();

            if (accessResult != null)
                return accessResult;

            using var response = await _api.SendAsync(HttpMethod.Get, "api/enrolments/events/" + eventId + "/enrolments");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Could not load the event enrolments.";
                return RedirectToAction("Enrolments");
            }

            var enrolments = await _api.ReadAsync<List<EnrolmentDto>>(response) ?? new List<EnrolmentDto>();

            var enrolment = enrolments.FirstOrDefault(e => e.EnrolmentID == enrolmentId);

            if (enrolment == null)
                return NotFound("Enrolment not found.");

            if (enrolment.EnrolmentStatus != "Confirmed")
            {
                TempData["Error"] =
                    "A result can only be recorded for a confirmed enrolment.";

                return RedirectToAction("Enrolments");
            }

            if (enrolment.ResultID != null)
            {
                TempData["Error"] =
                    "A result already exists. Use Edit Result instead.";

                return RedirectToAction("Enrolments");
            }

            var model = new CaptureResultVm
            {
                EnrolmentID = enrolment.EnrolmentID,
                ParticipantName = enrolment.ParticipantName ?? "Unknown participant",
                EventName = enrolment.EventName ?? "Unknown event",
                CategoryName = enrolment.CategoryName ?? "Unknown category",
                FinishTime = TimeSpan.Zero,
                FinishPosition = 1
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CaptureResult(CaptureResultVm model)
        {
            var accessResult = CheckOrganiserAccess();

            if (accessResult != null)
                return accessResult;

            if (model.FinishTime <= TimeSpan.Zero)
            {
                ModelState.AddModelError("FinishTime", "Finish time must be greater than zero.");
            }

            if (model.FinishPosition < 1)
            {
                ModelState.AddModelError("FinishPosition", "Finishing position must be at least 1.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var request = new CreateResultRequest
            {
                EnrolmentID = model.EnrolmentID,
                FinishTime = model.FinishTime,
                FinishPosition = model.FinishPosition
            };

            using var response = await _api.SendAsync(HttpMethod.Post, "api/results", request);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(string.Empty, "Could not save the result. " + error);

                return View(model);
            }

            TempData["Success"] = "The participant's result was successfully recorded.";

            return RedirectToAction("Enrolments");
        }

        [HttpGet]
        public async Task<IActionResult> EditResult(int id)
        {
            var accessResult = CheckOrganiserAccess();

            if (accessResult != null)
                return accessResult;

            using var response = await _api.SendAsync(
                HttpMethod.Get,
                "api/results/" + id);

            if (!response.IsSuccessStatusCode)
                return NotFound("Result not found.");

            var result = await _api.ReadAsync<ResultDto>(response);

            if (result == null)
                return NotFound("Result not found.");

            var model = new CaptureResultVm
            {
                EnrolmentID = result.EnrolmentID,
                ParticipantName = result.ParticipantName ?? "Unknown participant",
                EventName = result.EventName ?? "Unknown event",
                CategoryName = result.CategoryName ?? "Unknown category",
                FinishTime = result.FinishTime ?? TimeSpan.Zero,
                FinishPosition = result.FinishPosition ?? 1
            };

            ViewBag.ResultID = result.ResultID;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditResult(int id, CaptureResultVm model)
        {
            var accessResult = CheckOrganiserAccess();

            if (accessResult != null)
                return accessResult;

            if (model.FinishTime <= TimeSpan.Zero)
            {
                ModelState.AddModelError(
                    "FinishTime",
                    "Finish time must be greater than zero.");
            }

            if (model.FinishPosition < 1)
            {
                ModelState.AddModelError(
                    "FinishingPosition",
                    "Finishing position must be at least 1.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ResultID = id;
                return View(model);
            }

            var request = new CreateResultRequest
            {
                EnrolmentID = model.EnrolmentID,
                FinishTime = model.FinishTime,
                FinishPosition = model.FinishPosition
            };

            using var response = await _api.SendAsync(
                HttpMethod.Put,
                "api/results/" + id,
                request);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Could not update the result. " + error);

                ViewBag.ResultID = id;
                return View(model);
            }

            TempData["Success"] = "Result successfully updated.";

            return RedirectToAction("Results");
        }

        [HttpGet]
        public async Task<IActionResult> Results()
        {
            var accessResult = CheckOrganiserAccess();

            if (accessResult != null)
                return accessResult;

            using var eventsResponse = await _api.SendAsync(HttpMethod.Get, "api/events/mine");
            if (!eventsResponse.IsSuccessStatusCode)
            {
                TempData["Error"] = "Could not load your events.";

                return View(new List<EventResultsVm>());
            }

            var events = await _api.ReadAsync<List<EventDto>>(eventsResponse) ?? new List<EventDto>();

            var result = new List<EventResultsVm>();

            int totalCaptured = 0;
            int totalEnrolments = 0;

            foreach (var eventItem in events)
            {
                using var resultsResponse = await _api.SendAsync(HttpMethod.Get, "api/results/event/" + eventItem.EventID);
                var results = new List<ResultDto>();
                if (resultsResponse.IsSuccessStatusCode)
                    results = await _api.ReadAsync<List<ResultDto>>(resultsResponse) ?? new List<ResultDto>();

                using var enrolResponse = await _api.SendAsync(HttpMethod.Get, $"api/enrolments/events/{eventItem.EventID}/enrolments");
                if (enrolResponse.IsSuccessStatusCode)
                {
                    var enrols = await _api.ReadAsync<List<EnrolmentDto>>(enrolResponse) ?? new List<EnrolmentDto>();
                    totalEnrolments += enrols.Count(e => e.EnrolmentStatus == "Confirmed");
                }

                totalCaptured += results.Count;

                result.Add(new EventResultsVm
                {
                    EventID = eventItem.EventID,
                    EventName = eventItem.EventName,
                    Results = results
                });
            }

            ViewBag.Captured = totalCaptured;
            ViewBag.Awaiting = totalEnrolments - totalCaptured < 0 ? 0 : totalEnrolments - totalCaptured;

            return View(result);
        }

    }
}
