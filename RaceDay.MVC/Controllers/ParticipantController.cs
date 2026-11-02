using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Infrastructure;
using RaceDay.MVC.Models;
using RaceDay.MVC.Models.Api;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

/// <summary>Everything a logged-in Participant sees: their events, their results and their profile.</summary>
[SessionAuthorize("Participant")]
public class ParticipantController(ApiClient api) : AppController
{
    // ---------- My Events ----------

    public async Task<IActionResult> MyEvents()
    {
        var result = await api.GetAsync<List<EnrolmentResponse>>("/api/enrolments/customer");
        if (Expired(result) is { } expired) return expired;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var all = result.Data ?? new List<EnrolmentResponse>();
        return View(new MyEventsViewModel
        {
            Upcoming = all.Where(e => e.EventDate >= today).OrderBy(e => e.EventDate).ToList(),
            Past = all.Where(e => e.EventDate < today).OrderByDescending(e => e.EventDate).ToList(),
            Error = result.Success ? null : result.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await api.DeleteAsync($"/api/enrolments/{id}");
        if (Expired(result) is { } expired) return expired;

        if (result.Success) TempData["Success"] = "Your entry has been cancelled.";
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(MyEvents));
    }

    // ---------- My Results ----------

    public async Task<IActionResult> MyResults()
    {
        var result = await api.GetAsync<List<HistoryItemResponse>>("/api/participants/me/history");
        if (Expired(result) is { } expired) return expired;

        return View(new MyResultsViewModel
        {
            Results = result.Data ?? new List<HistoryItemResponse>(),
            Error = result.Success ? null : result.Error
        });
    }

    // ---------- Profile ----------

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var result = await api.GetAsync<ProfileResponse>("/api/profile");
        if (Expired(result) is { } expired) return expired;

        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction("Index", "Home");
        }
        return View(ToViewModel(result.Data));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(await WithReadOnlyFields(model));

        var result = await api.PutAsync<ProfileResponse>("/api/profile", new
        {
            name = model.Name,
            email = model.Email,
            emergencyContact = model.EmergencyContact
        });
        if (Expired(result) is { } expired) return expired;

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError("", result.StatusCode == System.Net.HttpStatusCode.Conflict
                ? "That email address is already used by another account."
                : result.Error ?? "We couldn't save your changes. Please try again.");
            return View(await WithReadOnlyFields(model));
        }

        HttpContext.Session.SetString(SessionKeys.Name, result.Data.Name);   // keep the name in the menu up to date
        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Please choose an image to upload first.";
            return RedirectToAction(nameof(Profile));
        }

        // The file goes to the API, which stores it in Azure Blob Storage. This site never talks to Azure.
        var result = await api.UploadAsync<ImageUploadResponse>("/api/profile/picture", file);
        if (Expired(result) is { } expired) return expired;

        if (result.Success) TempData["Success"] = "Your profile picture has been updated.";
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Profile));
    }

    private static ProfileViewModel ToViewModel(ProfileResponse p) => new()
    {
        Name = p.Name,
        Email = p.Email,
        EmergencyContact = p.EmergencyContact,
        DateOfBirth = p.DateOfBirth,
        HasProfilePicture = p.HasProfilePicture
    };

    /// <summary>When a form is sent back with errors, refill the parts the user cannot edit.</summary>
    private async Task<ProfileViewModel> WithReadOnlyFields(ProfileViewModel model)
    {
        var current = await api.GetAsync<ProfileResponse>("/api/profile");
        if (current.Success && current.Data is not null)
        {
            model.DateOfBirth = current.Data.DateOfBirth;
            model.HasProfilePicture = current.Data.HasProfilePicture;
        }
        return model;
    }
}
