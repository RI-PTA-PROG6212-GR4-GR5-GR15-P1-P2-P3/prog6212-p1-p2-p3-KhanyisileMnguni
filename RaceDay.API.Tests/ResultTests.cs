using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;

namespace RaceDay.API.Tests;

public class ResultTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly object GoodResult = new { finishTime = "00:52:30", position = 14, status = "Finished" };

    private async Task<(HttpClient organiser, HttpClient participant, int enrolmentId)> Setup()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var enrolmentId = await Api.Enrol(participant, categoryId);
        return (organiser, participant, enrolmentId);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_Returns201()
    {
        var (organiser, _, enrolmentId) = await Setup();

        var res = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", GoodResult);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var result = await res.Content.ReadFromJsonAsync<ResultResponse>();
        Assert.Equal(TimeSpan.Parse("00:52:30"), result!.FinishTime);
        Assert.Equal(14, result.Position);
    }

    [Fact]
    public async Task CaptureResult_MissingEnrolment_Returns404()
    {
        var organiser = await factory.OrganiserClient();
        var res = await organiser.PostAsJsonAsync("/api/enrolments/999999/result", GoodResult);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForAnotherOrganisersEvent_Returns403()
    {
        var (_, _, enrolmentId) = await Setup();
        var otherOrganiser = await factory.OrganiserClient();

        var res = await otherOrganiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", GoodResult);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var (organiser, _, enrolmentId) = await Setup();

        Assert.Equal(HttpStatusCode.Created, (await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", GoodResult)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", GoodResult)).StatusCode);
    }

    [Fact]
    public async Task CaptureResult_FinishedWithoutTime_Returns400()
    {
        var (organiser, _, enrolmentId) = await Setup();

        var res = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new { position = 3, status = "Finished" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewOwnResult()
    {
        var (organiser, participant, enrolmentId) = await Setup();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", GoodResult);

        var res = await participant.GetAsync($"/api/enrolments/{enrolmentId}/result");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(14, (await res.Content.ReadFromJsonAsync<ResultResponse>())!.Position);
    }

    [Fact]
    public async Task ViewResult_BeforeItIsCaptured_Returns404()
    {
        var (_, participant, enrolmentId) = await Setup();
        Assert.Equal(HttpStatusCode.NotFound, (await participant.GetAsync($"/api/enrolments/{enrolmentId}/result")).StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewSomeoneElsesResult_Returns403()
    {
        var (organiser, _, enrolmentId) = await Setup();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", GoodResult);
        var stranger = await factory.ParticipantClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/enrolments/{enrolmentId}/result")).StatusCode);
    }

    [Fact]
    public async Task History_ContainsCapturedResults_WithEventContext()
    {
        var (organiser, participant, enrolmentId) = await Setup();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", GoodResult);

        var res = await participant.GetAsync("/api/participants/me/history");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var history = await res.Content.ReadFromJsonAsync<List<HistoryItemResponse>>();
        var item = Assert.Single(history!);
        Assert.Equal("Test Run", item.EventName);
        Assert.Equal("10km", item.CategoryName);
        Assert.Equal(14, item.Position);
    }
}