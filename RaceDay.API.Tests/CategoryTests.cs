using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;

namespace RaceDay.API.Tests;

public class CategoryTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task AddCategory_WithZeroDistance_Returns400()
    {
        var organiser = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(organiser);

        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Broken",
            distanceKM = 0,
            maxParticipants = 10,
            entryFee = 50
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task AddCategory_ToMissingEvent_Returns404()
    {
        var organiser = await factory.OrganiserClient();

        var res = await organiser.PostAsJsonAsync("/api/events/999999/categories", new
        {
            categoryName = "5km",
            distanceKM = 5,
            maxParticipants = 10,
            entryFee = 50
        });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task GetCategories_IsPublic_AndShowsEnrolledCount()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var eventId = await Api.CreateEvent(organiser);
        var categoryId = await Api.CreateCategory(organiser, eventId, max: 50);
        await Api.Enrol(participant, categoryId);

        var res = await factory.CreateClient().GetAsync($"/api/events/{eventId}/categories");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var categories = await res.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        var category = Assert.Single(categories!);
        Assert.Equal(1, category.EnrolledCount);
        Assert.Equal(50, category.MaxParticipants);
    }

    [Fact]
    public async Task GetCategories_ForMissingEvent_Returns404()
    {
        var res = await factory.CreateClient().GetAsync("/api/events/999999/categories");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_BelowEnrolledCount_Returns400()
    {
        var organiser = await factory.OrganiserClient();
        var first = await factory.ParticipantClient();
        var second = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser), max: 5);
        await Api.Enrol(first, categoryId);
        await Api.Enrol(second, categoryId);

        var res = await organiser.PutAsJsonAsync($"/api/categories/{categoryId}", new
        {
            categoryName = "10km",
            distanceKM = 10,
            maxParticipants = 1,
            entryFee = 150
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_ByAnotherOrganiser_Returns403()
    {
        var owner = await factory.OrganiserClient();
        var other = await factory.OrganiserClient();
        var categoryId = await Api.CreateCategory(owner, await Api.CreateEvent(owner));

        var res = await other.PutAsJsonAsync($"/api/categories/{categoryId}", new
        {
            categoryName = "Hijacked",
            distanceKM = 10,
            maxParticipants = 10,
            entryFee = 10
        });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_ByAnotherOrganiser_Returns403()
    {
        var owner = await factory.OrganiserClient();
        var other = await factory.OrganiserClient();
        var categoryId = await Api.CreateCategory(owner, await Api.CreateEvent(owner));

        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/categories/{categoryId}")).StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateOrDeleteCategory_Returns403()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));

        var put = await participant.PutAsJsonAsync($"/api/categories/{categoryId}", new
        {
            categoryName = "Nope",
            distanceKM = 5,
            maxParticipants = 5,
            entryFee = 5
        });
        var del = await participant.DeleteAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, del.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_Missing_Returns404()
    {
        var organiser = await factory.OrganiserClient();
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.DeleteAsync("/api/categories/999999")).StatusCode);
    }
}
