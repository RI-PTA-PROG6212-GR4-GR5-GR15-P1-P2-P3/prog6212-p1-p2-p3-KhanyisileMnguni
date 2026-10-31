using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;

namespace RaceDay.API.Tests;

public class EnrolmentTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Participant_CanEnrol_Returns201_AndEnrolmentIsRecorded()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var eventId = await Api.CreateEvent(organiser);
        var categoryId = await Api.CreateCategory(organiser, eventId);

        var res = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = await res.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.Equal(eventId, created!.EventId);
        Assert.Equal(categoryId, created.CategoryId);

        // The link between participant, event and category shows up in the participant's own list...
        var mine = await (await participant.GetAsync("/api/enrolments/customer")).Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.Contains(mine!, e => e.EnrolmentId == created.EnrolmentId && e.EventId == eventId && e.CategoryId == categoryId);

        // ...and in the organiser's list for that event.
        var forOrganiser = await (await organiser.GetAsync($"/api/events/{eventId}/enrolments")).Content.ReadFromJsonAsync<List<OrganiserEnrolmentResponse>>();
        Assert.Contains(forOrganiser!, e => e.EnrolmentId == created.EnrolmentId);
    }

    [Fact]
    public async Task Enrol_Twice_InSameCategory_Returns409()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));

        Assert.Equal(HttpStatusCode.Created, (await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null)).StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenCategoryFull_Returns409()
    {
        var organiser = await factory.OrganiserClient();
        var first = await factory.ParticipantClient();
        var second = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser), max: 1);

        Assert.Equal(HttpStatusCode.Created, (await first.PostAsync($"/api/categories/{categoryId}/enrolments", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await second.PostAsync($"/api/categories/{categoryId}/enrolments", null)).StatusCode);
    }

    [Fact]
    public async Task Enrol_MissingCategory_Returns404()
    {
        var participant = await factory.ParticipantClient();
        Assert.Equal(HttpStatusCode.NotFound, (await participant.PostAsync("/api/categories/999999/enrolments", null)).StatusCode);
    }

    [Fact]
    public async Task Enrol_Unauthenticated_Returns401()
    {
        var res = await factory.CreateClient().PostAsync("/api/categories/1/enrolments", null);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsOfAnotherOrganisersEvent_Returns403()
    {
        var owner = await factory.OrganiserClient();
        var other = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(owner);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/events/{eventId}/enrolments")).StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment_Returns204()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var enrolmentId = await Api.Enrol(participant, categoryId);

        Assert.Equal(HttpStatusCode.NoContent, (await participant.DeleteAsync($"/api/enrolments/{enrolmentId}")).StatusCode);

        var mine = await (await participant.GetAsync("/api/enrolments/customer")).Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.DoesNotContain(mine!, e => e.EnrolmentId == enrolmentId);
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEnrolment_Returns403()
    {
        var organiser = await factory.OrganiserClient();
        var owner = await factory.ParticipantClient();
        var other = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var enrolmentId = await Api.Enrol(owner, categoryId);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/enrolments/{enrolmentId}")).StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithEnrolments_Returns409()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        await Api.Enrol(participant, categoryId);

        Assert.Equal(HttpStatusCode.Conflict, (await organiser.DeleteAsync($"/api/categories/{categoryId}")).StatusCode);
    }
}
