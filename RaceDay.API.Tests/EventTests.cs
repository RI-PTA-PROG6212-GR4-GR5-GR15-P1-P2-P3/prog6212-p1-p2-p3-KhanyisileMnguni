using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;

namespace RaceDay.API.Tests;

public class EventTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        var organiser = await factory.OrganiserClient();
        var res = await organiser.PostAsJsonAsync("/api/events", Api.EventBody("Sunrise Run"));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.True((await res.Content.ReadFromJsonAsync<Api.IdBody>())!.EventId > 0);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_Returns400()
    {
        var organiser = await factory.OrganiserClient();
        var res = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = "Bad",
            eventType = "Swim",
            eventDate = Api.FutureDate,
            location = "Pretoria"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_Unauthenticated_Returns401()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/events", Api.EventBody());
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Public_CanBrowseEventsWithoutLogin()
    {
        var organiser = await factory.OrganiserClient();
        var id = await Api.CreateEvent(organiser);

        var res = await factory.CreateClient().GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var events = await res.Content.ReadFromJsonAsync<List<EventResponse>>();
        Assert.Contains(events!, e => e.EventId == id);
    }

    [Fact]
    public async Task GetEvent_IncludesItsCategories()
    {
        var organiser = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(organiser);
        await Api.CreateCategory(organiser, eventId);

        var res = await factory.CreateClient().GetAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var detail = await res.Content.ReadFromJsonAsync<EventDetailResponse>();
        Assert.Single(detail!.Categories);
        Assert.Equal("Run", detail.EventType);
    }

    [Fact]
    public async Task GetEvent_Missing_Returns404()
    {
        var res = await factory.CreateClient().GetAsync("/api/events/999999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Owner_CanUpdateEvent()
    {
        var organiser = await factory.OrganiserClient();
        var id = await Api.CreateEvent(organiser);

        var res = await organiser.PutAsJsonAsync($"/api/events/{id}", new
        {
            eventName = "Renamed",
            eventType = "Walk",
            eventDate = Api.FutureDate,
            location = "Midrand"
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var updated = await res.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Renamed", updated!.EventName);
        Assert.Equal("Walk", updated.EventType);
    }

    [Fact]
    public async Task OtherOrganiser_CannotUpdateOrDeleteEvent_Returns403()
    {
        var owner = await factory.OrganiserClient();
        var other = await factory.OrganiserClient();
        var id = await Api.CreateEvent(owner);

        var put = await other.PutAsJsonAsync($"/api/events/{id}", Api.EventBody("Hijacked"));
        var del = await other.DeleteAsync($"/api/events/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, del.StatusCode);
    }

    [Fact]
    public async Task Owner_CanDeleteEvent_Returns204_ThenItIsGone()
    {
        var organiser = await factory.OrganiserClient();
        var id = await Api.CreateEvent(organiser);
        await Api.CreateCategory(organiser, id);

        Assert.Equal(HttpStatusCode.NoContent, (await organiser.DeleteAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.GetAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithEnrolments_Returns409()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var eventId = await Api.CreateEvent(organiser);
        var categoryId = await Api.CreateCategory(organiser, eventId);
        await Api.Enrol(participant, categoryId);

        Assert.Equal(HttpStatusCode.Conflict, (await organiser.DeleteAsync($"/api/events/{eventId}")).StatusCode);
    }

    [Fact]
    public async Task Owner_CanUpdateAndDeleteCategory()
    {
        var organiser = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(organiser);
        var categoryId = await Api.CreateCategory(organiser, eventId);

        var put = await organiser.PutAsJsonAsync($"/api/categories/{categoryId}", new
        {
            categoryName = "21km",
            distanceKM = 21.1,
            maxParticipants = 50,
            entryFee = 200
        });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal("21km", (await put.Content.ReadFromJsonAsync<CategoryResponse>())!.CategoryName);

        Assert.Equal(HttpStatusCode.NoContent, (await organiser.DeleteAsync($"/api/categories/{categoryId}")).StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotAddCategory_Returns403()
    {
        var owner = await factory.OrganiserClient();
        var other = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(owner);

        var res = await other.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "5km",
            distanceKM = 5,
            maxParticipants = 10,
            entryFee = 50
        });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
