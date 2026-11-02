using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Infrastructure;
using RaceDay.MVC.Models;
using RaceDay.MVC.Models.Api;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class AccountController(ApiClient api) : AppController
{
    // ---------- Login ----------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (IsLoggedIn) return RedirectToHome();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await api.PostAsync<LoginResponse>("/api/auth/login",
            new { email = model.Email, password = model.Password });

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError("", result.StatusCode == HttpStatusCode.Unauthorized
                ? "Your email or password is incorrect."
                : result.Error ?? "We couldn't log you in. Please try again.");
            return View(model);
        }

        // This is where the session is created: token, user ID, role and name.
        HttpContext.Session.SignIn(result.Data);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);
        return RedirectToHome();
    }

    // ---------- Register ----------

    [HttpGet]
    public IActionResult Register()
    {
        if (IsLoggedIn) return RedirectToHome();
        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role == "Participant")
        {
            if (model.DateOfBirth is null)
                ModelState.AddModelError(nameof(model.DateOfBirth), "Please enter your date of birth.");
            else if (model.DateOfBirth.Value >= DateOnly.FromDateTime(DateTime.Today))
                ModelState.AddModelError(nameof(model.DateOfBirth), "Your date of birth must be in the past.");
        }
        if (!ModelState.IsValid) return View(model);

        var isOrganiser = model.Role == "Organiser";
        var result = await api.PostAsync<AuthResponse>("/api/auth/register", new
        {
            name = model.Name,
            email = model.Email,
            password = model.Password,
            role = model.Role,
            organisationName = isOrganiser ? model.OrganisationName : null,
            dateOfBirth = isOrganiser ? null : model.DateOfBirth,
            emergencyContact = isOrganiser ? null : model.EmergencyContact
        });

        if (!result.Success)
        {
            ModelState.AddModelError("", result.StatusCode == HttpStatusCode.Conflict
                ? "An account with that email address already exists. Try logging in instead."
                : result.Error ?? "We couldn't create your account. Please try again.");
            return View(model);
        }

        TempData["Success"] = $"Welcome to RaceDay, {model.Name}! Your account is ready, so please log in.";
        return RedirectToAction(nameof(Login));
    }

    // ---------- Logout ----------

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Tell the API first so it stops accepting this token, then clear the session on this site.
        if (IsLoggedIn) await api.PostAsync("/api/auth/logout");

        HttpContext.Session.Clear();
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();
}
