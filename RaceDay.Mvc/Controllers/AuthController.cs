using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers
{
    public class AuthController : Controller
    {
        private readonly RaceDayApiClient _api;

        public AuthController(RaceDayApiClient api)
        {
            _api = api;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVm vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var request = new RegisterRequest
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Email = vm.Email,
                Password = vm.Password,
                Role = vm.Role,
                PhoneNumber = vm.PhoneNumber
            };

            using var response = await _api.SendAsync(HttpMethod.Post, "api/auth/register", request);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await response.Content.ReadAsStringAsync());

                return View(vm);
            }

            TempData["Success"] = "Registration successful. Please log in.";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            using var response = await _api.SendAsync(HttpMethod.Post, "api/auth/login",
                new LoginRequest
                {
                    Email = vm.Email,
                    Password = vm.Password
                });

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(vm);
            }

            var auth = await _api.ReadAsync<AuthResponse>(response);

            if (auth == null)
            {
                ModelState.AddModelError("", "Login response could not be read.");
                return View(vm);
            }

            string? apiCookie = RaceDayApiClient.ExtractSessionCookie(response);

            if (!string.IsNullOrWhiteSpace(apiCookie))
            {
                HttpContext.Session.SetString("ApiSessionCookie", apiCookie);
            }

            HttpContext.Session.SetInt32("UserId", auth.UserID);
            HttpContext.Session.SetString("Role", auth.Role);
            HttpContext.Session.SetString("DisplayName", $"{auth.FirstName} {auth.LastName}".Trim());

            if (auth.Role.Equals("Organiser", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Organiser");
            }

            return RedirectToAction("Index", "Participant");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            using var response = await _api.SendAsync(HttpMethod.Post, "api/auth/logout");
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
