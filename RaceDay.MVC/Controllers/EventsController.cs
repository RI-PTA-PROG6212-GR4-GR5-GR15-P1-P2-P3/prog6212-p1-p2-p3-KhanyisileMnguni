using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Infrastructure;
using RaceDay.MVC.Models;
using RaceDay.MVC.Models.Api;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class EventsController(ApiClient api) : AppController
{
    /// <summary>Full details of one event, with its categories and an "Enter Event" button for Participants.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var result = await api.GetAsync<EventDetailResponse>($"/api/events/{id}");
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.StatusCode == HttpStatusCode.NotFound
                ? "That event could not be found."
                : result.Error;
            return RedirectToAction("Index", "Home");
        }

        // For a logged-in Participant, find out which categories they have already entered.
        var enrolled = new HashSet<int>();
        if (Role == "Participant")
        {
            var mine = await api.GetAsync<List<EnrolmentResponse>>("/api/enrolments/customer");
            if (Expired(mine) is { } expired) return expired;
            if (mine.Success && mine.Data is not null)
                enrolled = mine.Data.Select(e => e.CategoryId).ToHashSet();
        }

        return View(new EventDetailsViewModel { Event = result.Data, EnrolledCategoryIds = enrolled });
    }

    /// <summary>"Enter Event": records the enrolment straight away.</summary>
    [HttpPost, ValidateAntiForgeryToken, SessionAuthorize("Participant")]
    public async Task<IActionResult> Enter(int eventId, int categoryId)
    {
        var result = await api.PostAsync<EnrolmentResponse>($"/api/categories/{categoryId}/enrolments");
        if (Expired(result) is { } expired) return expired;

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        TempData["Success"] = $"You're in! You have entered {result.Data?.EventName} ({result.Data?.CategoryName}).";
        return RedirectToAction("MyEvents", "Participant");
    }
}
