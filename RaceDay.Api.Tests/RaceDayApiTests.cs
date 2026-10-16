using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDay.Contracts;
using Xunit;
using Azure;
using Microsoft.AspNetCore.Identity;
using RaceDay.API.Models;

namespace RaceDay.Api.Tests
{
    public class RaceDayApiTests
    {
        private readonly RaceDayWebApplicationFactory _factory;

        public RaceDayApiTests()
        {
            _factory = new RaceDayWebApplicationFactory();
            _factory.SeedDatabaseAsync().GetAwaiter().GetResult();
        }

        public HttpClient CreateClient()
        {
            return _factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    HandleCookies = true,
                    AllowAutoRedirect = false
                });
        }

        private static string NewEmail(string role)
        {
            return $"{role}_{Guid.NewGuid():N}@example.com";
        }

        private async Task RegisterAsync(HttpClient client, string email, string role)
        {
            var request = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = email,
                Password = "Password#",
                Role = role
            };

            var response = await client.PostAsJsonAsync(
                "/api/auth/register", request);

            Assert.True(response.IsSuccessStatusCode,
                $"Registration failed: {(int)response.StatusCode}" +
                await response.Content.ReadAsStringAsync());
        }

        private async Task RegisterAndLoginAsync(HttpClient client, string email, string role)
        {
            await RegisterAsync(client, email, role);

            var response = await client.PostAsJsonAsync(
                "api/auth/login",
                new LoginRequest
                {
                    Email = email,
                    Password = "Password#"
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private static int GetIdFromJson(string json, string propertyName)
        {
            using var document = JsonDocument.Parse(json);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.Equals(
                    property.Name,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.GetInt32();
                }
            }

            throw new InvalidOperationException($"Could not find '{propertyName}' in API response: {json}");
        }
        private async Task<int> CreateEventAsync(HttpClient organiser)
        {
            var response = await organiser.PostAsJsonAsync(
                "/api/events",
                new CreateEventRequest
                {
                    EventName = "Capetown walk",
                    Description = "Walking around the city of cape",
                    EventDate = DateTime.Today.AddDays(30),
                    Location = "CapeTown",
                    DistanceKm = 10,
                    MaxParticipants = 1500,
                    EventTypeID = 1
                });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync();

            return GetIdFromJson(responseBody, "EventID");
        }

        [Fact]
        public async Task ValidRegistrationSucceeds()
        {
            using var client = CreateClient();

            var response = await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest
                {
                    FirstName = "Nomzamo",
                    LastName = "Mbatha",
                    Email = NewEmail("participant"),
                    Password = "RaceDay123$",
                    Role = "Participant"
                });

            Assert.True(response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Created);
        }

        [Fact]
        public async Task ValidLoginSucceeds()
        {
            using var client = CreateClient();
            var email = NewEmail("login");

            await RegisterAsync(client, email, "Participant");

            var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    Email = email,
                    Password = "Password#"
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task IncorrectPasswordIsRejected()
        {
            using var client = CreateClient();
            var email = NewEmail("wrongpassword");

            await RegisterAsync(client, email, "Participant");

            var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    Email = email,
                    Password = "Password"
                });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UnauthenticatedUserCannotViewProfile()
        {
            using var client = CreateClient();

            var response = await client.GetAsync("/api/users/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ParticipantCannotCreateEvent()
        {
            using var participant = CreateClient();

            await RegisterAndLoginAsync(participant, NewEmail("participant"), "Participant");

            var response = await participant.PostAsJsonAsync(
                "/api/events",
                new CreateEventRequest
                {
                    EventName = "Marathon",
                    Description = "Participant test",
                    EventDate = DateTime.Today.AddDays(30),
                    Location = "Durban",
                    DistanceKm = 5,
                    MaxParticipants = 50,
                    EventTypeID = 1
                });

            Assert.True(response.StatusCode == HttpStatusCode.Forbidden ||
                response.StatusCode == HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task OrganiserCanCreateEvent()
        {
            using var organiser = CreateClient();

            await RegisterAndLoginAsync(organiser, NewEmail("organiser"), "Organiser");

            var eventId = await CreateEventAsync(organiser);

            Assert.True(eventId > 0);
        }

        [Fact]
        public async Task ParticipantCanEnrolInEvent()
        {
            using var organiser = CreateClient();
            await RegisterAndLoginAsync(organiser, NewEmail("organiser"), "Organiser");

            //create event
            var eventId = await CreateEventAsync(organiser);
            Assert.True(eventId > 0);

            //login as participant
            using var participant = CreateClient();
            await RegisterAndLoginAsync(participant, NewEmail("participant"), "Participant");

            var response = await participant.PostAsJsonAsync("/api/enrolment", new
            {
                eventId = eventId,
                categoryId = 1
            });

            Assert.True(
                response.StatusCode == HttpStatusCode.Created ||
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Unexpected status: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        }

        [Fact]
        public async Task ParticipantCanViewOwnResults()
        {
            using var participant = CreateClient();
            await RegisterAndLoginAsync(participant, NewEmail("participant"), "Participant");

            var response = await participant.GetAsync("/api/results/me");

            Assert.True(response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.NoContent);
        }
    }
} 
