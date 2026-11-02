using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Infrastructure;
using RaceDay.MVC.Models;
using RaceDay.MVC.Models.Api;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

/// <summary>Everything an Organiser sees: dashboard, events, categories, enrolments and results entry.</summary>
[SessionAuthorize("Organiser")]
public class OrganiserController(ApiClient api) : AppController
{
    // ---------- Dashboard ----------

    public async Task<IActionResult> Dashboard()
    {
        var result = await api.GetAsync<List<OrganiserEventSummaryResponse>>("/api/events/mine");
        if (Expired(result) is { } expired) return expired;

        var events = result.Data ?? new List<OrganiserEventSummaryResponse>();
        var today = DateOnly.FromDateTime(DateTime.Today);
        return View(new OrganiserDashboardViewModel
        {
            Events = events,
            Upcoming = events.Where(e => e.EventDate >= today).OrderBy(e => e.EventDate).ToList(),
            TotalEnrolments = events.Sum(e => e.EnrolmentCount),
            Error = result.Success ? null : result.Error
        });
    }

    // ---------- Events: list ----------

    public async Task<IActionResult> Events()
    {
        var result = await api.GetAsync<List<OrganiserEventSummaryResponse>>("/api/events/mine");
        if (Expired(result) is { } expired) return expired;

        return View(new OrganiserEventsViewModel
        {
            Events = (result.Data ?? new List<OrganiserEventSummaryResponse>()).OrderByDescending(e => e.EventDate).ToList(),
            Error = result.Success ? null : result.Error
        });
    }

    // ---------- Events: create ----------

    [HttpGet]
    public IActionResult Create() => View(new EventFormViewModel { EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model, IFormFile? banner)
    {
        if (model.DistanceKm is null)
            ModelState.AddModelError(nameof(model.DistanceKm), "Please enter the main distance in km.");
        if (!ModelState.IsValid) return View(model);

        var created = await api.PostAsync<CreatedEventResponse>("/api/events", EventBody(model));
        if (Expired(created) is { } expired) return expired;
        if (!created.Success || created.Data is null)
        {
            ModelState.AddModelError("", created.Error ?? "We couldn't create the event. Please try again.");
            return View(model);
        }
        var eventId = created.Data.EventId;

        // The main distance becomes the event's first category (more can be added on the Categories page).
        var firstCategory = await api.PostAsync<CreatedCategoryResponse>($"/api/events/{eventId}/categories", new
        {
            categoryName = $"{model.DistanceKm.Value.ToString("0.##", CultureInfo.InvariantCulture)} km",
            distanceKM = model.DistanceKm,
            maxParticipants = model.MaxParticipants,
            entryFee = model.EntryFee
        });

        string? problem = firstCategory.Success ? null : "The event was created, but its first category could not be added: " + firstCategory.Error;

        if (banner is { Length: > 0 })
        {
            var upload = await api.UploadAsync<ImageUploadResponse>($"/api/events/{eventId}/banner", banner);
            if (!upload.Success) problem = (problem is null ? "" : problem + " ") + "The banner image was not saved: " + upload.Error;
        }

        TempData["Success"] = $"\"{model.EventName}\" has been created.";
        if (problem is not null) TempData["Error"] = problem;
        return RedirectToAction(nameof(Categories), new { id = eventId });
    }

    // ---------- Events: edit ----------

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (ev, redirect) = await LoadMyEvent(id);
        if (redirect is not null) return redirect;

        return View(new EventFormViewModel
        {
            EventId = ev!.EventId,
            EventName = ev.EventName,
            EventType = ev.EventType,
            EventDate = ev.EventDate,
            Location = ev.Location,
            Description = ev.Description,
            HasBanner = ev.HasBanner
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model, IFormFile? banner)
    {
        model.EventId = id;
        // Distance, fee and capacity belong to categories when editing, so they are not validated here.
        ModelState.Remove(nameof(model.DistanceKm));
        ModelState.Remove(nameof(model.MaxParticipants));
        ModelState.Remove(nameof(model.EntryFee));
        if (!ModelState.IsValid) return View(model);

        var result = await api.PutAsync<EventResponse>($"/api/events/{id}", EventBody(model));
        if (Expired(result) is { } expired) return expired;
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "We couldn't save your changes. Please try again.");
            return View(model);
        }

        if (banner is { Length: > 0 })
        {
            var upload = await api.UploadAsync<ImageUploadResponse>($"/api/events/{id}/banner", banner);
            if (!upload.Success)
            {
                TempData["Error"] = "Your changes were saved, but the banner image was not: " + upload.Error;
                return RedirectToAction(nameof(Events));
            }
        }

        TempData["Success"] = $"\"{model.EventName}\" has been updated.";
        return RedirectToAction(nameof(Events));
    }

    // ---------- Events: delete ----------

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await api.DeleteAsync($"/api/events/{id}");
        if (Expired(result) is { } expired) return expired;

        if (result.Success) TempData["Success"] = "The event has been deleted.";
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Events));
    }

    // ---------- Categories ----------

    [HttpGet]
    public async Task<IActionResult> Categories(int id)
    {
        var (ev, redirect) = await LoadMyEvent(id);
        if (redirect is not null) return redirect;

        return View(new CategoriesPageViewModel { Event = ev!, NewCategory = new CategoryFormViewModel { EventId = id } });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory([Bind(Prefix = "NewCategory")] CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var (ev, redirect) = await LoadMyEvent(model.EventId);
            if (redirect is not null) return redirect;
            return View(nameof(Categories), new CategoriesPageViewModel { Event = ev!, NewCategory = model });
        }

        var result = await api.PostAsync<CreatedCategoryResponse>($"/api/events/{model.EventId}/categories", CategoryBody(model));
        if (Expired(result) is { } expired) return expired;

        if (result.Success) TempData["Success"] = $"Category \"{model.CategoryName}\" has been added.";
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Categories), new { id = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id, int categoryId)
    {
        var (ev, redirect) = await LoadMyEvent(id);
        if (redirect is not null) return redirect;

        var category = ev!.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
        if (category is null)
        {
            TempData["Error"] = "That category could not be found.";
            return RedirectToAction(nameof(Categories), new { id });
        }

        ViewData["EventName"] = ev.EventName;
        return View(new CategoryFormViewModel
        {
            EventId = id,
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            DistanceKm = category.DistanceKm,
            MaxParticipants = category.MaxParticipants,
            EntryFee = category.EntryFee
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await api.PutAsync<CategoryResponse>($"/api/categories/{model.CategoryId}", CategoryBody(model));
        if (Expired(result) is { } expired) return expired;
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "We couldn't save the category. Please try again.");
            return View(model);
        }

        TempData["Success"] = $"Category \"{model.CategoryName}\" has been updated.";
        return RedirectToAction(nameof(Categories), new { id = model.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
    {
        var result = await api.DeleteAsync($"/api/categories/{categoryId}");
        if (Expired(result) is { } expired) return expired;

        if (result.Success) TempData["Success"] = "The category has been removed.";
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Categories), new { id = eventId });
    }

    // ---------- Enrolments ----------

    public async Task<IActionResult> Enrolments(int id)
    {
        var (ev, redirect) = await LoadMyEvent(id);
        if (redirect is not null) return redirect;

        var result = await api.GetAsync<List<OrganiserEnrolmentResponse>>($"/api/events/{id}/enrolments");
        if (Expired(result) is { } expired) return expired;
        if (!result.Success) TempData["Error"] = result.Error;

        return View(new EventEnrolmentsViewModel { Event = ev!, Enrolments = result.Data ?? new List<OrganiserEnrolmentResponse>() });
    }

    // ---------- Results entry ----------

    public async Task<IActionResult> Results(int id)
    {
        var (ev, redirect) = await LoadMyEvent(id);
        if (redirect is not null) return redirect;

        var result = await api.GetAsync<List<OrganiserEnrolmentResponse>>($"/api/events/{id}/enrolments");
        if (Expired(result) is { } expired) return expired;
        if (!result.Success) TempData["Error"] = result.Error;

        return View(new EventEnrolmentsViewModel { Event = ev!, Enrolments = result.Data ?? new List<OrganiserEnrolmentResponse>() });
    }

    /// <summary>Saves one participant's result: captures a new one, or corrects the one already there.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResult(ResultInputModel input)
    {
        TimeSpan? finishTime = null;
        if (!string.IsNullOrWhiteSpace(input.FinishTime))
        {
            if (!TimeSpan.TryParseExact(input.FinishTime.Trim(), new[] { @"hh\:mm\:ss", @"h\:mm\:ss" },
                    CultureInfo.InvariantCulture, out var parsed))
            {
                TempData["Error"] = "Please enter the finish time as hours:minutes:seconds, for example 00:52:30.";
                return RedirectToAction(nameof(Results), new { id = input.EventId });
            }
            finishTime = parsed;
        }

        if (input.Status == "Finished" && finishTime is null)
        {
            TempData["Error"] = "A finish time is needed for a participant who finished.";
            return RedirectToAction(nameof(Results), new { id = input.EventId });
        }

        var body = new { finishTime, position = input.Position, status = input.Status };
        var url = $"/api/enrolments/{input.EnrolmentId}/result";
        var result = input.HasResult
            ? await api.PutAsync<ResultResponse>(url, body)
            : await api.PostAsync<ResultResponse>(url, body);
        if (Expired(result) is { } expired) return expired;

        if (result.Success) TempData["Success"] = "The result has been saved.";
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Results), new { id = input.EventId });
    }

    // ---------- helpers ----------

    /// <summary>Loads an event, but only if it belongs to the logged-in Organiser.</summary>
    private async Task<(EventDetailResponse? Event, IActionResult? Redirect)> LoadMyEvent(int id)
    {
        var mine = await api.GetAsync<List<OrganiserEventSummaryResponse>>("/api/events/mine");
        if (Expired(mine) is { } expired) return (null, expired);

        if (!mine.Success || mine.Data is null || mine.Data.All(e => e.EventId != id))
        {
            TempData["Error"] = mine.Success ? "That event isn't one of yours." : mine.Error;
            return (null, RedirectToAction(nameof(Events)));
        }

        var detail = await api.GetAsync<EventDetailResponse>($"/api/events/{id}");
        if (!detail.Success || detail.Data is null)
        {
            TempData["Error"] = detail.Error;
            return (null, RedirectToAction(nameof(Events)));
        }
        return (detail.Data, null);
    }

    private static object EventBody(EventFormViewModel m) => new
    {
        eventName = m.EventName,
        eventType = m.EventType,
        eventDate = m.EventDate,
        location = m.Location,
        description = m.Description
    };

    private static object CategoryBody(CategoryFormViewModel m) => new
    {
        categoryName = m.CategoryName,
        distanceKM = m.DistanceKm,
        maxParticipants = m.MaxParticipants,
        entryFee = m.EntryFee
    };
}
