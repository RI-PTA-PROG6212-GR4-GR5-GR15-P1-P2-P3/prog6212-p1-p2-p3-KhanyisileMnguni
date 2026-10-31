using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;

namespace RaceDay.API.Tests;

/// <summary>Business rules: past events, cancelling, result validation and who may see what.</summary>
public class RuleTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly object GoodResult = new { finishTime = "00:45:10", position = 3, status = "Finished" };

    [Fact]
    public async Task Events_InThePast_AreHiddenByDefault_ButShownWithIncludePast()
    {
        var organiser = await factory.OrganiserClient();
        var pastId = await Api.CreateEventOn(organiser, "2020-01-01", "Old Run");
        var client = factory.CreateClient();

        var upcoming = await (await client.GetAsync("/api/events")).Content.ReadFromJsonAsync<List<EventResponse>>();
        var all = await (await client.GetAsync("/api/events?includePast=true")).Content.ReadFromJsonAsync<List<EventResponse>>();

        Assert.DoesNotContain(upcoming!, e => e.EventId == pastId);
        Assert.Contains(all!, e => e.EventId == pastId);
    }

    [Fact]
    public async Task Enrol_InAnEventThatAlreadyHappened_Returns400()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var pastEvent = await Api.CreateEventOn(organiser, "2020-01-01");
        var categoryId = await Api.CreateCategory(organiser, pastEvent);

        var res = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CanEnrolAgain_AfterCancelling()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var firstId = await Api.Enrol(participant, categoryId);

        Assert.Equal(HttpStatusCode.NoContent, (await participant.DeleteAsync($"/api/enrolments/{firstId}")).StatusCode);

        var again = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }

    [Fact]
    public async Task CancelEnrolment_AfterResultCaptured_Returns409()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var enrolmentId = await Api.Enrol(participant, categoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", GoodResult);

        Assert.Equal(HttpStatusCode.Conflict, (await participant.DeleteAsync($"/api/enrolments/{enrolmentId}")).StatusCode);
    }

    [Fact]
    public async Task CancelEnrolment_Missing_Returns404()
    {
        var participant = await factory.ParticipantClient();
        Assert.Equal(HttpStatusCode.NotFound, (await participant.DeleteAsync("/api/enrolments/999999")).StatusCode);
    }

    [Fact]
    public async Task Result_DidNotFinish_DoesNotNeedAFinishTime()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var enrolmentId = await Api.Enrol(participant, categoryId);

        var res = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new { status = "dnf" });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var result = await res.Content.ReadFromJsonAsync<ResultResponse>();
        Assert.Equal("DNF", result!.Status);
        Assert.Null(result.FinishTime);
    }

    [Fact]
    public async Task Result_WithUnknownStatus_Returns400()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var enrolmentId = await Api.Enrol(participant, categoryId);

        var res = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:40:00", position = 1, status = "Winner" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Result_WithPositionZero_Returns400()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var enrolmentId = await Api.Enrol(participant, categoryId);

        var res = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:40:00", position = 0, status = "Finished" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewResultsOfTheirOwnEvent_ButAnotherOrganiserCannot()
    {
        var owner = await factory.OrganiserClient();
        var other = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(owner, await Api.CreateEvent(owner));
        var enrolmentId = await Api.Enrol(participant, categoryId);
        await owner.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", GoodResult);

        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/enrolments/{enrolmentId}/result")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/enrolments/{enrolmentId}/result")).StatusCode);
    }

    [Fact]
    public async Task ViewResult_ForMissingEnrolment_Returns404()
    {
        var participant = await factory.ParticipantClient();
        Assert.Equal(HttpStatusCode.NotFound, (await participant.GetAsync("/api/enrolments/999999/result")).StatusCode);
    }

    [Fact]
    public async Task History_ShowsOnlyTheLoggedInParticipantsResults()
    {
        var organiser = await factory.OrganiserClient();
        var runner = await factory.ParticipantClient();
        var otherRunner = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var runnerEnrolment = await Api.Enrol(runner, categoryId);
        var otherEnrolment = await Api.Enrol(otherRunner, categoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{runnerEnrolment}/result", GoodResult);
        await organiser.PostAsJsonAsync($"/api/enrolments/{otherEnrolment}/result",
            new { finishTime = "01:10:00", position = 9, status = "Finished" });

        var history = await (await runner.GetAsync("/api/participants/me/history")).Content.ReadFromJsonAsync<List<HistoryItemResponse>>();

        var item = Assert.Single(history!);
        Assert.Equal(runnerEnrolment, item.EnrolmentId);
        Assert.Equal(3, item.Position);
    }

    [Fact]
    public async Task History_IsEmpty_ForAParticipantWithNoResults()
    {
        var participant = await factory.ParticipantClient();

        var history = await (await participant.GetAsync("/api/participants/me/history")).Content.ReadFromJsonAsync<List<HistoryItemResponse>>();

        Assert.Empty(history!);
    }

    [Fact]
    public async Task Event_CanBeDeleted_AfterItsCategoryHasNoEnrolments_AndCategoriesGoWithIt()
    {
        var organiser = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(organiser);
        var categoryId = await Api.CreateCategory(organiser, eventId);

        Assert.Equal(HttpStatusCode.NoContent, (await organiser.DeleteAsync($"/api/events/{eventId}")).StatusCode);

        // The category went with the event, so it can no longer be changed.
        var res = await organiser.PutAsJsonAsync($"/api/categories/{categoryId}", new
        {
            categoryName = "Gone",
            distanceKM = 5,
            maxParticipants = 5,
            entryFee = 5
        });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_Missing_Returns404()
    {
        var organiser = await factory.OrganiserClient();
        var res = await organiser.PutAsJsonAsync("/api/events/999999", Api.EventBody("Ghost"));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
