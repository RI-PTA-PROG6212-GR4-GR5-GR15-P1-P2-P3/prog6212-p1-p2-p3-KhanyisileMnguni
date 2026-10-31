using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;

namespace RaceDay.API.Controllers;

[Route("api/participants")]
public class ParticipantsController(AppDbContext db) : ApiControllerBase
{
    /// <summary>Returns the logged-in Participant's full performance history across all past events.</summary>
    /// <remarks>Each item includes the finishing position and totalFinishers (everyone who finished in that category), e.g. "47th of 312".</remarks>
    /// <response code="200">Array of results with event and category context, newest first.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("me/history")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<HistoryItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> History()
    {
        var me = CurrentUserId;

        var rows = await db.Results
            .Where(r => r.Enrolment.ParticipantID == me)
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .Select(r => new
            {
                r.ResultID,
                r.EnrolmentID,
                r.Enrolment.CategoryID,
                EventId = r.Enrolment.Category.EventID,
                EventName = r.Enrolment.Category.Event.EventName,
                EventDate = r.Enrolment.Category.Event.EventDate,
                Location = r.Enrolment.Category.Event.Location,
                CategoryName = r.Enrolment.Category.CategoryName,
                Distance = r.Enrolment.Category.DistanceKM,
                r.FinishTime,
                r.Position,
                r.Status
            })
            .ToListAsync();

        // How many people finished in each of those categories (DNF / DNS / disqualified do not count).
        var categoryIds = rows.Select(x => x.CategoryID).Distinct().ToList();
        var finishers = await db.Results
            .Where(r => r.Status == "Finished" && categoryIds.Contains(r.Enrolment.CategoryID))
            .GroupBy(r => r.Enrolment.CategoryID)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        var list = rows.Select(x => new HistoryItemResponse(
                x.ResultID, x.EnrolmentID, x.EventId, x.EventName, x.EventDate, x.Location,
                x.CategoryName, x.Distance, x.FinishTime, x.Position, x.Status,
                x.CategoryID, finishers.GetValueOrDefault(x.CategoryID)))
            .ToList();

        return Ok(list);
    }
}
