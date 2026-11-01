using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
using RaceDay.Contracts;

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
    }
}
