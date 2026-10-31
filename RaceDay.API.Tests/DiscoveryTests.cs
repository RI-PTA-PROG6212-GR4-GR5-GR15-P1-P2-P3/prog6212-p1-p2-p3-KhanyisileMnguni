using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;

namespace RaceDay.API.Tests;

/// <summary>Participant home-page filtering and the Organiser dashboard.</summary>
public class DiscoveryTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<List<EventResponse>> Browse(string query = "") =>
        (await factory.CreateClient().GetFromJsonAsync<List<EventResponse>>("/api/events" + query))!;

    [Fact]
    public async Task Filter_ByEventType_ReturnsOnlyThatType()
    {
        var organiser = await factory.OrganiserClient();
        var runId = await Api.CreateEventFull(organiser, "Filter Run " + Guid.NewGuid(), "Run", Api.FutureDate, "Pretoria");
        var cycleId = await Api.CreateEventFull(organiser, "Filter Cycle " + Guid.NewGuid(), "Cycle", Api.FutureDate, "Pretoria");

        var cycles = await Browse("?eventType=cycle");   // not case sensitive

        Assert.Contains(cycles, e => e.EventId == cycleId);
        Assert.DoesNotContain(cycles, e => e.EventId == runId);
        Assert.All(cycles, e => Assert.Equal("Cycle", e.EventType));
    }

    [Fact]
    public async Task Search_FindsAnEventByPartOfItsName_IgnoringCase()
    {
        var organiser = await factory.OrganiserClient();
        var token = "Zebra" + Guid.NewGuid().ToString("N");
        var id = await Api.CreateEventFull(organiser, $"The {token} Classic", "Run", Api.FutureDate, "Pretoria");

        var found = await Browse("?search=" + token.ToUpperInvariant());

        var match = Assert.Single(found);
        Assert.Equal(id, match.EventId);
    }

    [Fact]
    public async Task Search_WithNoMatch_ReturnsAnEmptyList()
    {
        var found = await Browse("?search=no-such-event-" + Guid.NewGuid().ToString("N"));
        Assert.Empty(found);
    }

    [Fact]
    public async Task Filter_ByLocation_ReturnsOnlyEventsInThatPlace()
    {
        var organiser = await factory.OrganiserClient();
        var place = "Polokwane" + Guid.NewGuid().ToString("N");
        var inPlace = await Api.CreateEventFull(organiser, "Place A", "Walk", Api.FutureDate, $"Near {place}, Limpopo");
        var elsewhere = await Api.CreateEventFull(organiser, "Place B", "Walk", Api.FutureDate, "Durban");

        var found = await Browse("?location=" + place);

        Assert.Contains(found, e => e.EventId == inPlace);
        Assert.DoesNotContain(found, e => e.EventId == elsewhere);
    }

    [Fact]
    public async Task Filter_ByDateRange_ReturnsOnlyEventsInsideIt()
    {
        var organiser = await factory.OrganiserClient();
        var early = await Api.CreateEventFull(organiser, "Early " + Guid.NewGuid(), "Run", "2098-03-01", "Pretoria");
        var late = await Api.CreateEventFull(organiser, "Late " + Guid.NewGuid(), "Run", "2098-09-01", "Pretoria");

        var fromJune = await Browse("?fromDate=2098-06-01");
        var untilJune = await Browse("?toDate=2098-06-01");

        Assert.Contains(fromJune, e => e.EventId == late);
        Assert.DoesNotContain(fromJune, e => e.EventId == early);
        Assert.Contains(untilJune, e => e.EventId == early);
        Assert.DoesNotContain(untilJune, e => e.EventId == late);
    }

    [Fact]
    public async Task Filters_CanBeCombined()
    {
        var organiser = await factory.OrganiserClient();
        var token = "Combo" + Guid.NewGuid().ToString("N");
        var wanted = await Api.CreateEventFull(organiser, $"{token} Ride", "Cycle", Api.FutureDate, "Cape Town");
        var wrongType = await Api.CreateEventFull(organiser, $"{token} Jog", "Run", Api.FutureDate, "Cape Town");

        var found = await Browse($"?search={token}&eventType=Cycle&location=cape");

        var match = Assert.Single(found);
        Assert.Equal(wanted, match.EventId);
        Assert.NotEqual(wrongType, match.EventId);
    }

    // ---------- Organiser dashboard ----------

    [Fact]
    public async Task Dashboard_ListsOnlyTheOrganisersOwnEvents_WithCounts()
    {
        var organiser = await factory.OrganiserClient();
        var someoneElse = await factory.OrganiserClient();
        var first = await factory.ParticipantClient();
        var second = await factory.ParticipantClient();

        var eventId = await Api.CreateEvent(organiser);
        var categoryId = await Api.CreateCategory(organiser, eventId);
        await Api.Enrol(first, categoryId);
        await Api.Enrol(second, categoryId);
        var othersEvent = await Api.CreateEvent(someoneElse);

        var res = await organiser.GetAsync("/api/events/mine");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var events = await res.Content.ReadFromJsonAsync<List<OrganiserEventSummaryResponse>>();
        var mine = Assert.Single(events!);
        Assert.Equal(eventId, mine.EventId);
        Assert.Equal(1, mine.CategoryCount);
        Assert.Equal(2, mine.EnrolmentCount);
        Assert.DoesNotContain(events!, e => e.EventId == othersEvent);
    }

    [Fact]
    public async Task Dashboard_IncludesPastEvents_ThatTheBrowsePageHides()
    {
        var organiser = await factory.OrganiserClient();
        var pastId = await Api.CreateEventOn(organiser, "2020-05-05", "Long Ago Run");

        var events = await organiser.GetFromJsonAsync<List<OrganiserEventSummaryResponse>>("/api/events/mine");

        Assert.Contains(events!, e => e.EventId == pastId);
    }

    [Fact]
    public async Task Dashboard_ForANewOrganiser_IsEmpty()
    {
        var organiser = await factory.OrganiserClient();
        var events = await organiser.GetFromJsonAsync<List<OrganiserEventSummaryResponse>>("/api/events/mine");
        Assert.Empty(events!);
    }

    [Fact]
    public async Task Dashboard_AsAParticipant_Returns403()
    {
        var participant = await factory.ParticipantClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.GetAsync("/api/events/mine")).StatusCode);
    }

    [Fact]
    public async Task Dashboard_WithoutLogin_Returns401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/events/mine")).StatusCode);
    }
}
