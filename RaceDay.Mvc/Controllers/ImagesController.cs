using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public class ImagesController : Controller
    {
        private readonly RaceDayApiClient _api;

        public ImagesController(RaceDayApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> EventBanner(int id)
        {
            if (id <= 0)
                return BadRequest();

            using var response = await _api.SendAsync(
                HttpMethod.Get,
                "api/events/" + id + "/banner-image"
            );

            if (!response.IsSuccessStatusCode)
                return NotFound();

            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

            string contentType =
                response.Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream";

            return File(imageBytes, contentType);
        }

        public async Task<IActionResult> ProfilePicture()
        {
            using var response = await _api.SendAsync(
                HttpMethod.Get,
                "api/users/me/profile-image"
            );

            if (!response.IsSuccessStatusCode)
                return NotFound();

            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

            string contentType =
                response.Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream";

            return File(imageBytes, contentType);
        }
    }
}
