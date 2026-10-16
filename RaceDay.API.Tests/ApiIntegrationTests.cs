using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.API.Tests;

public sealed class ApiIntegrationTests : IDisposable
{
    private readonly RaceDayApiFactory _factory = new();
    private readonly HttpClient _client;

    public ApiIntegrationTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task RegistrationAndLogin_Work_AndInvalidPasswordIsRejected()
    {
        var email = NewEmail("organiser");
        var registration = await RegisterAsync("Organiser", email);

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "CorrectPassword123!"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using var loginJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(loginJson.RootElement.GetProperty("token").GetString()));
        Assert.Equal("Organiser", loginJson.RootElement.GetProperty("role").GetString());

        var badLogin = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPassword123!"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, badLogin.StatusCode);
    }

    [Fact]
    public async Task ProtectedEventCreation_RejectsAnonymousAndParticipant_ButAllowsOrganiser()
    {
        var eventBody = NewEventBody();

        var anonymousResponse = await _client.PostAsJsonAsync("/api/events", eventBody);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var participantToken = await RegisterAndLoginAsync("Participant", NewEmail("participant"));
        using var participantClient = AuthenticatedClient(participantToken);
        var participantResponse = await participantClient.PostAsJsonAsync("/api/events", eventBody);
        Assert.Equal(HttpStatusCode.Forbidden, participantResponse.StatusCode);

        var organiserToken = await RegisterAndLoginAsync("Organiser", NewEmail("organiser"));
        using var organiserClient = AuthenticatedClient(organiserToken);
        var organiserResponse = await organiserClient.PostAsJsonAsync("/api/events", eventBody);
        Assert.Equal(HttpStatusCode.Created, organiserResponse.StatusCode);
    }

    [Fact]
    public async Task OrganiserCanManageOwnEvent_ButAnotherOrganiserCannotUpdateIt()
    {
        var ownerToken = await RegisterAndLoginAsync("Organiser", NewEmail("owner"));
        var otherToken = await RegisterAndLoginAsync("Organiser", NewEmail("other"));

        using var ownerClient = AuthenticatedClient(ownerToken);
        using var otherClient = AuthenticatedClient(otherToken);

        var createResponse = await ownerClient.PostAsJsonAsync("/api/events", NewEventBody());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var eventId = createdJson.RootElement.GetProperty("eventId").GetInt32();

        var updateBody = NewEventBody("Updated Race");
        var forbiddenUpdate = await otherClient.PutAsJsonAsync($"/api/events/{eventId}", updateBody);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenUpdate.StatusCode);

        var ownerUpdate = await ownerClient.PutAsJsonAsync($"/api/events/{eventId}", updateBody);
        Assert.Equal(HttpStatusCode.OK, ownerUpdate.StatusCode);
    }

    [Fact]
    public async Task ParticipantCanEnrol_EnrolmentIsRecorded_AndDuplicateIsRejected()
    {
        var organiserToken = await RegisterAndLoginAsync("Organiser", NewEmail("eventowner"));
        using var organiserClient = AuthenticatedClient(organiserToken);

        var eventResponse = await organiserClient.PostAsJsonAsync("/api/events", NewEventBody());
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
        using var eventJson = JsonDocument.Parse(await eventResponse.Content.ReadAsStringAsync());
        var eventId = eventJson.RootElement.GetProperty("eventId").GetInt32();

        var categoryResponse = await organiserClient.PostAsJsonAsync(
            $"/api/events/{eventId}/categories",
            new
            {
                categoryName = "10 KM",
                distanceKM = 10.0m,
                maxParticipants = 25,
                entryFee = 100.00m
            });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        using var categoryJson = JsonDocument.Parse(await categoryResponse.Content.ReadAsStringAsync());
        var categoryId = categoryJson.RootElement.GetProperty("categoryId").GetInt32();

        var participantToken = await RegisterAndLoginAsync("Participant", NewEmail("entrant"));
        using var participantClient = AuthenticatedClient(participantToken);

        var enrolResponse = await participantClient.PostAsJsonAsync(
            $"/api/categories/{categoryId}/enrolments", new { });
        Assert.Equal(HttpStatusCode.Created, enrolResponse.StatusCode);

        using var enrolJson = JsonDocument.Parse(await enrolResponse.Content.ReadAsStringAsync());
        Assert.Equal(categoryId, enrolJson.RootElement.GetProperty("categoryId").GetInt32());
        Assert.Equal("Registered", enrolJson.RootElement.GetProperty("status").GetString());

        var mineResponse = await participantClient.GetAsync("/api/enrolments/customer");
        Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);
        using var mineJson = JsonDocument.Parse(await mineResponse.Content.ReadAsStringAsync());
        Assert.Contains(mineJson.RootElement.EnumerateArray(),
            item => item.GetProperty("categoryId").GetInt32() == categoryId);

        var duplicateResponse = await participantClient.PostAsJsonAsync(
            $"/api/categories/{categoryId}/enrolments", new { });
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task RoleBasedEnrolmentEndpoints_RejectWrongRoleAndAnonymousRequests()
    {
        var anonymousEnrolment = await _client.PostAsJsonAsync("/api/categories/1/enrolments", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousEnrolment.StatusCode);

        var organiserToken = await RegisterAndLoginAsync("Organiser", NewEmail("roleorganiser"));
        using var organiserClient = AuthenticatedClient(organiserToken);
        var organiserEnrolment = await organiserClient.PostAsJsonAsync("/api/categories/1/enrolments", new { });
        Assert.Equal(HttpStatusCode.Forbidden, organiserEnrolment.StatusCode);

        var anonymousMine = await _client.GetAsync("/api/enrolments/customer");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousMine.StatusCode);

        var participantToken = await RegisterAndLoginAsync("Participant", NewEmail("roleparticipant"));
        using var participantClient = AuthenticatedClient(participantToken);
        var participantCannotCreateEvent = await participantClient.PostAsJsonAsync("/api/events", NewEventBody());
        Assert.Equal(HttpStatusCode.Forbidden, participantCannotCreateEvent.StatusCode);
    }

    private async Task<HttpResponseMessage> RegisterAsync(string role, string email)
    {
        if (role == "Participant")
        {
            return await _client.PostAsJsonAsync("/api/auth/register", new
            {
                name = "Test User",
                email,
                password = "CorrectPassword123!",
                role,
                dateOfBirth = "2000-01-01",
                emergencyContact = "0712345678"
            });
        }

        return await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Test User",
            email,
            password = "CorrectPassword123!",
            role,
            organisationName = "Test Running Club"
        });
    }

    private async Task<string> RegisterAndLoginAsync(string role, string email)
    {
        var registerResponse = await RegisterAsync(role, email);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "CorrectPassword123!"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var json = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("token").GetString()
               ?? throw new InvalidOperationException("Login response did not contain a JWT token.");
    }

    private HttpClient AuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static object NewEventBody(string eventName = "RaceDay Test Event") => new
    {
        eventName,
        eventType = "Run",
        eventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)).ToString("yyyy-MM-dd"),
        location = "Pretoria",
        description = "Integration test event"
    };

    private static string NewEmail(string prefix) => $"{prefix}.{Guid.NewGuid():N}@example.com";

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
