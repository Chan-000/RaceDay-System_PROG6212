using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Mvc.Controllers
{
    public class ParticipantController : Controller
    {
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

        public IActionResult Index()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!IsParticipant())
            {
                return StatusCode(403, "Access denied. This page is for participants only.");
            }

            ViewBag.DisplayName = HttpContext.Session.GetString("DisplayName");

            return View();
        }
    }
}
