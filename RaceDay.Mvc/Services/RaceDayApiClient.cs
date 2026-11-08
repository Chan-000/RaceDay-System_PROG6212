using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Mvc.Services
{
    public class RaceDayApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public RaceDayApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        private HttpClient CreateClient()
        {
            return _httpClientFactory.CreateClient("RaceDayApi");
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);

            string? apiCookie = _httpContextAccessor.HttpContext?.Session.GetString("ApiSessionCookie");
            
            if (!string.IsNullOrWhiteSpace(apiCookie))
            {
                request.Headers.TryAddWithoutValidation("Cookie", apiCookie);
            }
            return request;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
        {
            var client = CreateClient();
            using var request = CreateRequest(method, url);

            if (body != null)
            {
                request.Content = JsonContent.Create(body);
            }
            return await client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> UploadFileAsync(string url, IFormFile file)
        {
            var client = CreateClient();

            using var request = CreateRequest(HttpMethod.Post, url);

            using var multipartContent = new MultipartFormDataContent();

            using var fileStream = file.OpenReadStream();

            using var fileContent = new StreamContent(fileStream);

            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            multipartContent.Add(fileContent, "file", file.FileName);

            request.Content = multipartContent;

            return await client.SendAsync(request);
        }

        public async Task<T?> ReadAsync<T>(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }

        public static string? ExtractSessionCookie(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            {
                return null;
            }

            string? sessionHeader = values.FirstOrDefault(v =>
                v.StartsWith(".AspNetCore.Session=", StringComparison.OrdinalIgnoreCase));

            return sessionHeader?.Split(';', 2)[0];
        }
    }
}
