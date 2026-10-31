using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;

namespace RaceDay.API.Tests;

/// <summary>Results entry page (edit a result, see results beside enrolments) and "47th of 312" on My Results.</summary>
public class ResultFeatureTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static object Finished(string time, int position) => new { finishTime = time, position, status = "Finished" };

    private async Task<(HttpClient organiser, HttpClient participant, int eventId, int enrolmentId)> Setup()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var eventId = await Api.CreateEvent(organiser);
        var categoryId = await Api.CreateCategory(organiser, eventId);
        var enrolmentId = await Api.Enrol(participant, categoryId);
        return (organiser, participant, eventId, enrolmentId);
    }

    // ---------- Total finishers ----------

    [Fact]
    public async Task History_ShowsPositionOutOfTotalFinishers_AndDidNotFinishDoesNotCount()
    {
        var organiser = await factory.OrganiserClient();
        var first = await factory.ParticipantClient();
        var second = await factory.ParticipantClient();
        var third = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var firstEnrolment = await Api.Enrol(first, categoryId);
        var secondEnrolment = await Api.Enrol(second, categoryId);
        var thirdEnrolment = await Api.Enrol(third, categoryId);

        await organiser.PostAsJsonAsync($"/api/enrolments/{firstEnrolment}/result", Finished("00:40:00", 1));
        await organiser.PostAsJsonAsync($"/api/enrolments/{secondEnrolment}/result", Finished("00:45:00", 2));
        await organiser.PostAsJsonAsync($"/api/enrolments/{thirdEnrolment}/result", new { status = "DNF" });

        var history = await first.GetFromJsonAsync<List<HistoryItemResponse>>("/api/participants/me/history");
        var item = Assert.Single(history!);
        Assert.Equal(1, item.Position);
        Assert.Equal(2, item.TotalFinishers);        // two finished; the DNF is not counted
        Assert.Equal(categoryId, item.CategoryId);

        var dnfHistory = await third.GetFromJsonAsync<List<HistoryItemResponse>>("/api/participants/me/history");
        var dnf = Assert.Single(dnfHistory!);
        Assert.Equal("DNF", dnf.Status);
        Assert.Equal(2, dnf.TotalFinishers);
    }

    [Fact]
    public async Task TotalFinishers_AreCountedPerCategory_NotPerEvent()
    {
        var organiser = await factory.OrganiserClient();
        var runner = await factory.ParticipantClient();
        var other = await factory.ParticipantClient();
        var eventId = await Api.CreateEvent(organiser);
        var tenK = await Api.CreateCategory(organiser, eventId);
        var fiveK = (await (await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "5km", distanceKM = 5, maxParticipants = 50, entryFee = 80
        })).Content.ReadFromJsonAsync<Api.IdBody>())!.CategoryId;

        var runnerEnrolment = await Api.Enrol(runner, tenK);
        var otherEnrolment = await Api.Enrol(other, fiveK);
        await organiser.PostAsJsonAsync($"/api/enrolments/{runnerEnrolment}/result", Finished("00:50:00", 1));
        await organiser.PostAsJsonAsync($"/api/enrolments/{otherEnrolment}/result", Finished("00:25:00", 1));

        var history = await runner.GetFromJsonAsync<List<HistoryItemResponse>>("/api/participants/me/history");

        Assert.Equal(1, Assert.Single(history!).TotalFinishers);
    }

    // ---------- Organiser's enrolment list shows results ----------

    [Fact]
    public async Task OrganiserEnrolmentList_ShowsWhetherAResultHasBeenCaptured()
    {
        var (organiser, _, eventId, enrolmentId) = await Setup();

        var before = await organiser.GetFromJsonAsync<List<OrganiserEnrolmentResponse>>($"/api/events/{eventId}/enrolments");
        Assert.False(Assert.Single(before!).HasResult);

        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", Finished("00:48:15", 5));

        var after = await organiser.GetFromJsonAsync<List<OrganiserEnrolmentResponse>>($"/api/events/{eventId}/enrolments");
        var row = Assert.Single(after!);
        Assert.True(row.HasResult);
        Assert.Equal(5, row.Position);
        Assert.Equal(TimeSpan.Parse("00:48:15"), row.FinishTime);
        Assert.Equal("Completed", row.Status);
    }

    // ---------- Correcting a result (PUT) ----------

    [Fact]
    public async Task Organiser_CanCorrectAResult_AndTheParticipantSeesTheNewValues()
    {
        var (organiser, participant, _, enrolmentId) = await Setup();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", Finished("00:50:00", 9));

        var res = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/result", Finished("00:47:30", 7));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var seen = await participant.GetFromJsonAsync<ResultResponse>($"/api/enrolments/{enrolmentId}/result");
        Assert.Equal(7, seen!.Position);
        Assert.Equal(TimeSpan.Parse("00:47:30"), seen.FinishTime);
    }

    [Fact]
    public async Task CorrectingAResult_CanChangeItToDidNotFinish()
    {
        var (organiser, _, eventId, enrolmentId) = await Setup();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", Finished("00:50:00", 9));

        var res = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new { status = "dnf" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var row = Assert.Single((await organiser.GetFromJsonAsync<List<OrganiserEnrolmentResponse>>($"/api/events/{eventId}/enrolments"))!);
        Assert.Equal("DNF", row.ResultStatus);
        Assert.Equal("DNF", row.Status);
    }

    [Fact]
    public async Task CorrectingAResult_BeforeOneExists_Returns404()
    {
        var (organiser, _, _, enrolmentId) = await Setup();
        var res = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/result", Finished("00:50:00", 1));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task CorrectingAResult_ForAMissingEnrolment_Returns404()
    {
        var organiser = await factory.OrganiserClient();
        var res = await organiser.PutAsJsonAsync("/api/enrolments/999999/result", Finished("00:50:00", 1));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task AnotherOrganiser_CannotCorrectAResult_Returns403()
    {
        var (organiser, _, _, enrolmentId) = await Setup();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", Finished("00:50:00", 3));
        var other = await factory.OrganiserClient();

        var res = await other.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/result", Finished("00:10:00", 1));

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCorrectTheirOwnResult_Returns403()
    {
        var (organiser, participant, _, enrolmentId) = await Setup();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", Finished("00:50:00", 3));

        var res = await participant.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/result", Finished("00:10:00", 1));

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CorrectingAResult_WithFinishedButNoTime_Returns400()
    {
        var (organiser, _, _, enrolmentId) = await Setup();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", Finished("00:50:00", 3));

        var res = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new { position = 3, status = "Finished" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ACorrectedResult_StillCountsOnce_NotTwice()
    {
        var (organiser, participant, _, enrolmentId) = await Setup();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", Finished("00:50:00", 3));
        await organiser.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/result", Finished("00:49:00", 2));

        var history = await participant.GetFromJsonAsync<List<HistoryItemResponse>>("/api/participants/me/history");

        var item = Assert.Single(history!);
        Assert.Equal(1, item.TotalFinishers);
    }
}
