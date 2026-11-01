using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Mvc.Controllers
{
    public class OrganiserController : Controller
    {
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
                return StatusCode(403, "Access denied. This page is for organisers only.");
            }

            ViewBag.DisplayName = HttpContext.Session.GetString("DisplayName");
            
            return View();
        }
    }
}
