using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Infrastructure;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

/// <summary>Shared behaviour for every controller.</summary>
public abstract class AppController : Controller
{
    protected bool IsLoggedIn => HttpContext.Session.IsLoggedIn();

    protected string? Role => HttpContext.Session.UserRole();

    /// <summary>
    /// If the API says the token is no longer valid (expired or logged out), end the session and send the user
    /// to the login page. Returns null when everything is fine, so callers can write: if (Expired(r) is { } go) return go;
    /// </summary>
    protected IActionResult? Expired(ApiResult result)
    {
        if (!result.IsUnauthorized) return null;

        HttpContext.Session.Clear();
        TempData["Error"] = "Your session has expired. Please log in again.";
        return RedirectToAction("Login", "Account");
    }

    /// <summary>Where each role lands after logging in.</summary>
    protected IActionResult RedirectToHome() =>
        Role == "Organiser" ? RedirectToAction("Dashboard", "Organiser") : RedirectToAction("Index", "Home");
}
