using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;

namespace RaceDay.API.Controllers;

[Route("api/categories")]
public class CategoriesController(AppDbContext db) : ApiControllerBase
{
    /// <summary>Updates a category. Only the Organiser who owns its event may do this.</summary>
    /// <response code="200">The updated category.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult> Update(int id, CategoryRequest req)
    {
        var c = await db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.CategoryID == id);
        if (c is null) return Fail(404, "Category not found.");
        if (c.Event.OrganiserID != CurrentUserId) return Fail(403, "You can only manage categories of your own events.");

        var enrolled = await db.Enrolments.CountAsync(n => n.CategoryID == id);
        if (req.MaxParticipants < enrolled)
            return Fail(400, $"MaxParticipants cannot be lower than the {enrolled} participants already enrolled.");

        c.CategoryName = req.CategoryName.Trim();
        c.DistanceKM = req.DistanceKM;
        c.MaxParticipants = req.MaxParticipants;
        c.EntryFee = req.EntryFee;
        await db.SaveChangesAsync();

        return Ok(await db.Categories.Where(x => x.CategoryID == id).ToResponse().FirstAsync());
    }

    /// <summary>Removes a category from an event. Owning Organiser only.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">The category already has enrolments.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult> Delete(int id)
    {
        var c = await db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.CategoryID == id);
        if (c is null) return Fail(404, "Category not found.");
        if (c.Event.OrganiserID != CurrentUserId) return Fail(403, "You can only manage categories of your own events.");
        if (await db.Enrolments.AnyAsync(n => n.CategoryID == id))
            return Fail(409, "This category already has enrolments and cannot be removed.");

        db.Categories.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Enrols the logged-in Participant into a category.</summary>
    /// <remarks>The body is optional; the server records today's date as the enrolment date.</remarks>
    /// <response code="201">The new enrolment.</response>
    /// <response code="400">The event has already taken place.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Category is full, or you are already enrolled in it.</response>
    [HttpPost("{id:int}/enrolments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult> Enrol(int id, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] EnrolRequest? req = null)
    {
        var c = await db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.CategoryID == id);
        if (c is null) return Fail(404, "Category not found.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (c.Event.EventDate < today) return Fail(400, "This event has already taken place.");

        var me = CurrentUserId;
        if (await db.Enrolments.AnyAsync(n => n.CategoryID == id && n.ParticipantID == me))
            return Fail(409, "You are already enrolled in this category.");

        var taken = await db.Enrolments.CountAsync(n => n.CategoryID == id);
        if (taken >= c.MaxParticipants) return Fail(409, "This category is full.");

        var enrolment = new Models.Enrolment
        {
            ParticipantID = me,
            CategoryID = id,
            EnrolmentDate = today,
            Status = "Registered"
        };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();

        var created = await db.Enrolments.Where(n => n.EnrolmentID == enrolment.EnrolmentID).ToResponse().FirstAsync();
        return StatusCode(201, created);
    }
}

