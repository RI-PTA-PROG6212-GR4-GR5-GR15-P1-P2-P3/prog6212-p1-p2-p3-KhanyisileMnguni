using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[Route("api/events")]
public class EventsController(AppDbContext db) : ApiControllerBase
{
    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <remarks>Body: eventName, eventType (Run / Walk / Cycle), eventDate, location, description. Distances are set per category.</remarks>
    /// <response code="201">Returns the new event ID.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult> Create(EventRequest req)
    {
        var ev = new Event
        {
            OrganiserID = CurrentUserId,
            EventName = req.EventName.Trim(),
            EventType = Mapping.Normalise(req.EventType),
            EventDate = req.EventDate!.Value,
            Location = req.Location.Trim(),
            Description = req.Description?.Trim()
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.EventID }, new { eventId = ev.EventID });
    }

    /// <summary>Lists upcoming events (public). Add ?includePast=true to include events that have already happened.</summary>
    /// <response code="200">Array of events, soonest first.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetAll([FromQuery] bool includePast = false)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var query = db.Events.AsQueryable();
        if (!includePast) query = query.Where(e => e.EventDate >= today);

        return Ok(await query.OrderBy(e => e.EventDate).ToResponse().ToListAsync());
    }

    /// <summary>Returns full details of one event, including its categories (public).</summary>
    /// <response code="200">The event and its categories.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetById(int id)
    {
        var e = await db.Events.Where(x => x.EventID == id).ToResponse().FirstOrDefaultAsync();
        if (e is null) return Fail(404, "Event not found.");

        var categories = await db.Categories.Where(c => c.EventID == id).OrderBy(c => c.DistanceKM).ToResponse().ToListAsync();
        return Ok(new EventDetailResponse(e.EventId, e.EventName, e.EventType, e.EventDate, e.Location,
            e.Description, e.OrganiserName, e.OrganisationName, categories));
    }

    /// <summary>Updates an event. Only the Organiser who created it may do this.</summary>
    /// <response code="200">The updated event.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult> Update(int id, EventRequest req)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return Fail(404, "Event not found.");
        if (ev.OrganiserID != CurrentUserId) return Fail(403, "You can only manage events you created.");

        ev.EventName = req.EventName.Trim();
        ev.EventType = Mapping.Normalise(req.EventType);
        ev.EventDate = req.EventDate!.Value;
        ev.Location = req.Location.Trim();
        ev.Description = req.Description?.Trim();
        await db.SaveChangesAsync();

        return Ok(await db.Events.Where(x => x.EventID == id).ToResponse().FirstAsync());
    }

    /// <summary>Deletes an event and its categories. Only the owning Organiser may do this.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Participants are already enrolled, so the event cannot be deleted.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult> Delete(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return Fail(404, "Event not found.");
        if (ev.OrganiserID != CurrentUserId) return Fail(403, "You can only manage events you created.");

        if (await db.Enrolments.AnyAsync(n => n.Category.EventID == id))
            return Fail(409, "This event already has enrolments and cannot be deleted.");

        db.Events.Remove(ev);   // categories are removed by cascade
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Adds a category (e.g. 10km, 21km, Under 20) to an event. Owning Organiser only.</summary>
    /// <remarks>Body: categoryName, distanceKM, maxParticipants, entryFee.</remarks>
    /// <response code="201">Returns the new category ID.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost("{id:int}/categories")]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult> AddCategory(int id, CategoryRequest req)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return Fail(404, "Event not found.");
        if (ev.OrganiserID != CurrentUserId) return Fail(403, "You can only manage events you created.");

        var category = new Category
        {
            EventID = id,
            CategoryName = req.CategoryName.Trim(),
            DistanceKM = req.DistanceKM,
            MaxParticipants = req.MaxParticipants,
            EntryFee = req.EntryFee
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return StatusCode(201, new { categoryId = category.CategoryID });
    }

    /// <summary>Lists all categories for an event (public).</summary>
    /// <response code="200">Array of categories.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetCategories(int id)
    {
        if (!await db.Events.AnyAsync(e => e.EventID == id)) return Fail(404, "Event not found.");
        return Ok(await db.Categories.Where(c => c.EventID == id).OrderBy(c => c.DistanceKM).ToResponse().ToListAsync());
    }

    /// <summary>Lists everyone enrolled in an event. Owning Organiser only.</summary>
    /// <response code="200">Array of enrolments with participant and category details.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<OrganiserEnrolmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetEnrolments(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return Fail(404, "Event not found.");
        if (ev.OrganiserID != CurrentUserId) return Fail(403, "You can only view enrolments for events you created.");

        var list = await db.Enrolments
            .Where(n => n.Category.EventID == id)
            .OrderBy(n => n.Category.DistanceKM).ThenBy(n => n.EnrolmentDate)
            .Select(n => new OrganiserEnrolmentResponse(n.EnrolmentID, n.ParticipantID, n.Participant.Name,
                n.Participant.Email, n.Participant.EmergencyContact, n.CategoryID, n.Category.CategoryName,
                n.EnrolmentDate, n.Status))
            .ToListAsync();
        return Ok(list);
    }
}

