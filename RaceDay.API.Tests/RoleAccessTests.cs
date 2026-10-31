using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;

namespace RaceDay.API.Tests;

/// <summary>Organiser functionality must be closed to Participants, and vice versa.</summary>
public class RoleAccessTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var participant = await factory.ParticipantClient();
        var res = await participant.PostAsJsonAsync("/api/events", Api.EventBody());
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments_Returns403()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var eventId = await Api.CreateEvent(organiser);

        Assert.Equal(HttpStatusCode.Forbidden, (await participant.GetAsync($"/api/events/{eventId}/enrolments")).StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureResult_Returns403()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var enrolmentId = await Api.Enrol(participant, categoryId);

        var res = await participant.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:50:00", position = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var organiser = await factory.OrganiserClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));

        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.PostAsync($"/api/categories/{categoryId}/enrolments", null)).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewParticipantHistoryOrEnrolments_Returns403()
    {
        var organiser = await factory.OrganiserClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.GetAsync("/api/participants/me/history")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.GetAsync("/api/enrolments/customer")).StatusCode);
    }

    [Theory]
    [InlineData("/api/enrolments/customer")]
    [InlineData("/api/participants/me/history")]
    [InlineData("/api/profile")]
    [InlineData("/api/enrolments/1/result")]
    public async Task ProtectedEndpoints_WithoutLogin_Return401(string url)
    {
        var res = await factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
