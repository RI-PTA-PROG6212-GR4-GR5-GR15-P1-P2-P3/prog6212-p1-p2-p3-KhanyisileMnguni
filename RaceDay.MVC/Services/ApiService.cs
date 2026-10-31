using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.MVC.Models;

namespace RaceDay.MVC.Services
{
    public class ApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient GetClient(string? token = null)
        {
            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        public async Task<LoginResponse?> LoginAsync(LoginViewModel model)
        {
            var client = GetClient();

            var response = await client.PostAsJsonAsync(
                "api/auth/login", model);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<LoginResponse>();
        }

        public async Task<bool> RegisterAsync(RegisterViewModel model)
        {
            var client = GetClient();

            var response = await client.PostAsJsonAsync(
                "api/auth/register", model);

            return response.IsSuccessStatusCode;
        }

        public async Task<List<EventViewModel>> GetEventsAsync()
        {
            var client = GetClient();

            return await client.GetFromJsonAsync<List<EventViewModel>>(
                "api/events") ?? new List<EventViewModel>();
        }

        public async Task<bool> CreateEventAsync(
            EventViewModel model, string token)
        {
            var client = GetClient(token);

            var request = new
            {
                eventName = model.EventName,
                eventType = model.EventType,
                eventDate = model.EventDate,
                location = model.Location,
                description = model.Description
            };

            var response = await client.PostAsJsonAsync(
                "api/events", request);

            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine("EVENT STATUS: " + response.StatusCode);
            Console.WriteLine("EVENT RESPONSE: " + responseBody);

            return response.IsSuccessStatusCode;
        }

        public async Task<List<CategoryViewModel>> GetCategoriesAsync(int eventId)
        {
            var client = GetClient();

            return await client.GetFromJsonAsync<List<CategoryViewModel>>(
                $"api/events/{eventId}/categories") ?? new List<CategoryViewModel>();
        }

        public async Task<bool> AddCategoryAsync(
            int eventId, CategoryViewModel model, string token)
        {
            var client = GetClient(token);

            var request = new
            {
                categoryName = model.CategoryName,
                distanceKM = model.DistanceKM,
                maxParticipants = model.MaxParticipants,
                entryFee = model.EntryFee
            };

            var response = await client.PostAsJsonAsync(
                $"api/events/{eventId}/categories", request);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> EnrolAsync(int categoryId, string token)
        {
            var client = GetClient(token);

            var response = await client.PostAsync(
                $"api/categories/{categoryId}/enrolments", null);

            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine("ENROL STATUS: " + response.StatusCode);
            Console.WriteLine("ENROL RESPONSE: " + responseBody);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteEventAsync(int eventId, string token)
        {
            var client = GetClient(token);

            var response = await client.DeleteAsync(
                $"api/events/{eventId}");

            return response.IsSuccessStatusCode;
        }
        public async Task<List<EnrolmentViewModel>> GetMyEnrolmentsAsync(string token)
        {
            var client = GetClient(token);

            var response = await client.GetAsync("api/enrolments/customer");

            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine("MY ENROLMENTS STATUS: " + response.StatusCode);
            Console.WriteLine("MY ENROLMENTS RESPONSE: " + responseBody);

            if (!response.IsSuccessStatusCode)
            {
                return new List<EnrolmentViewModel>();
            }

            return System.Text.Json.JsonSerializer.Deserialize<List<EnrolmentViewModel>>(
                responseBody,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<EnrolmentViewModel>();
        }
     

        public async Task<bool> CancelEnrolmentAsync(int enrolmentId, string token)
        {
            var client = GetClient(token);

            var response = await client.DeleteAsync(
                $"api/enrolments/{enrolmentId}");

            return response.IsSuccessStatusCode;
        }

        public async Task<List<HistoryViewModel>> GetMyHistoryAsync(string token)
        {
            var client = GetClient(token);

            return await client.GetFromJsonAsync<List<HistoryViewModel>>(
                "api/participants/me/history") ?? new List<HistoryViewModel>();
        }

        public async Task<List<OrganiserEnrolmentViewModel>> GetEventEnrolmentsAsync(int eventId, string token)
        {
            var client = GetClient(token);

            var response = await client.GetAsync($"api/events/{eventId}/enrolments");

            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine("ENROLMENTS STATUS: " + response.StatusCode);
            Console.WriteLine("ENROLMENTS RESPONSE: " + responseBody);

            if (!response.IsSuccessStatusCode)
            {
                return new List<OrganiserEnrolmentViewModel>();
            }

            return System.Text.Json.JsonSerializer.Deserialize<List<OrganiserEnrolmentViewModel>>(
                responseBody,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<OrganiserEnrolmentViewModel>();
        }

        public async Task<bool> CaptureResultAsync(int enrolmentId, ResultViewModel model, string token)
        {
            var client = GetClient(token);

            var response = await client.PostAsJsonAsync(
                $"api/enrolments/{enrolmentId}/result", model);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateResultAsync(int enrolmentId, ResultViewModel model, string token)
        {
            var client = GetClient(token);

            var response = await client.PutAsJsonAsync(
                $"api/enrolments/{enrolmentId}/result", model);

            return response.IsSuccessStatusCode;
        }
    }
}

    public class LoginResponse
    {
        public int UserId { get; set; }
        public string Role { get; set; } = "";
        public string Name { get; set; } = "";
        public string Token { get; set; } = "";
        public DateTime ExpiresAtUtc { get; set; }
    }

