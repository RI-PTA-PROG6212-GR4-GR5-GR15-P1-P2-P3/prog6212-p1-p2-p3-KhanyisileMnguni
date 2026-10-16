using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;

namespace RaceDay.API.Controllers;

[Route("api/participants")]
public class ParticipantsController(AppDbContext db) : ApiControllerBase
{
    /// <summary>Returns the logged-in Participant's full performance history across all past events.</summary>
    /// <response code="200">Array of results with event and category context, newest first.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("me/history")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<HistoryItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> History()
    {
        var me = CurrentUserId;
        var list = await db.Results
            .Where(r => r.Enrolment.ParticipantID == me)
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .Select(r => new HistoryItemResponse(r.ResultID, r.EnrolmentID,
                r.Enrolment.Category.EventID, r.Enrolment.Category.Event.EventName,
                r.Enrolment.Category.Event.EventDate, r.Enrolment.Category.Event.Location,
                r.Enrolment.Category.CategoryName, r.Enrolment.Category.DistanceKM,
                r.FinishTime, r.Position, r.Status))
            .ToListAsync();
        return Ok(list);
    }
}

