using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[Route("api/enrolments")]
public class EnrolmentsController(AppDbContext db) : ApiControllerBase
{
    /// <summary>Lists the logged-in Participant's enrolments, with event and category details.</summary>
    /// <response code="200">Array of enrolments.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("customer")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetMine()
    {
        var me = CurrentUserId;
        var list = await db.Enrolments
            .Where(n => n.ParticipantID == me)
            .OrderByDescending(n => n.Category.Event.EventDate)
            .ToResponse()
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Cancels one of the logged-in Participant's own enrolments.</summary>
    /// <response code="204">Cancelled.</response>
    /// <response code="403">Not your enrolment.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result has already been captured, so it cannot be cancelled.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Participant")]
    public async Task<ActionResult> Cancel(int id)
    {
        var n = await db.Enrolments.Include(x => x.Result).FirstOrDefaultAsync(x => x.EnrolmentID == id);
        if (n is null) return Fail(404, "Enrolment not found.");
        if (n.ParticipantID != CurrentUserId) return Fail(403, "You can only cancel your own enrolments.");
        if (n.Result is not null) return Fail(409, "A result has already been captured for this enrolment.");

        db.Enrolments.Remove(n);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Captures a result (finish time, position, status) for an enrolment. Owning Organiser only.</summary>
    /// <remarks>Body: finishTime (hh:mm:ss), position, status (Finished / DNF / DNS / Disqualified). A finishTime is required when status is Finished.</remarks>
    /// <response code="201">The new result.</response>
    /// <response code="400">Invalid fields.</response>
    /// <response code="403">The enrolment is not in one of your events.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("{id:int}/result")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult> CaptureResult(int id, ResultRequest req)
    {
        var n = await db.Enrolments
            .Include(x => x.Category).ThenInclude(c => c.Event)
            .Include(x => x.Result)
            .FirstOrDefaultAsync(x => x.EnrolmentID == id);

        if (n is null) return Fail(404, "Enrolment does not exist.");
        if (n.Category.Event.OrganiserID != CurrentUserId)
            return Fail(403, "You can only capture results for events you created.");
        if (n.Result is not null) return Fail(409, "A result has already been captured for this enrolment.");

        var status = Mapping.NormaliseResultStatus(req.Status);
        if (status == "Finished" && req.FinishTime is null)
            return Fail(400, "A finishTime is required when the status is Finished.");
        if (req.FinishTime is { } t && t <= TimeSpan.Zero)
            return Fail(400, "finishTime must be greater than zero.");

        var result = new Result
        {
            EnrolmentID = id,
            FinishTime = req.FinishTime,
            Position = req.Position,
            Status = status
        };
        db.Results.Add(result);
        n.Status = status == "Finished" ? "Completed" : status;
        await db.SaveChangesAsync();

        return StatusCode(201, result.ToResponse());
    }

    /// <summary>Returns the result for an enrolment: the Participant who owns it, or the Organiser of its event.</summary>
    /// <response code="200">The result.</response>
    /// <response code="403">You are neither the enrolled Participant nor the event's Organiser.</response>
    /// <response code="404">Enrolment not found, or no result captured yet.</response>
    [HttpGet("{id:int}/result")]
    [Authorize]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetResult(int id)
    {
        var n = await db.Enrolments
            .Include(x => x.Category).ThenInclude(c => c.Event)
            .Include(x => x.Result)
            .FirstOrDefaultAsync(x => x.EnrolmentID == id);
        if (n is null) return Fail(404, "Enrolment not found.");

        var me = CurrentUserId;
        var allowed = IsParticipant ? n.ParticipantID == me : n.Category.Event.OrganiserID == me;
        if (!allowed) return Fail(403, "You can only view results for your own enrolments or events.");

        if (n.Result is null) return Fail(404, "No result has been captured yet.");
        return Ok(n.Result.ToResponse());
    }
}
