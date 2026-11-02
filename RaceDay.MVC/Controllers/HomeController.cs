using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Models.Api;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class HomeController(ApiClient api) : AppController
{
    /// <summary>Home page: every upcoming event, with filters. Anyone can browse.</summary>
    public async Task<IActionResult> Index([FromQuery] EventFilter filter)
    {
        var result = await api.GetAsync<List<EventResponse>>("/api/events" + BuildQuery(filter));
        if (Expired(result) is { } expired) return expired;

        return View(new HomeViewModel
        {
            Filter = filter,
            Events = result.Data ?? new List<EventResponse>(),
            Error = result.Success ? null : result.Error
        });
    }

    public IActionResult Error() => View("Error", new ErrorViewModel());

    /// <summary>Friendly page for 404 and other status codes (the pipeline sends them here).</summary>
    public IActionResult HttpStatus(int code)
    {
        var model = code switch
        {
            404 => new ErrorViewModel { Title = "Page not found", Message = "We couldn't find that page. It may have moved or never existed." },
            403 => new ErrorViewModel { Title = "Not allowed", Message = "You don't have permission to see that page." },
            _ => new ErrorViewModel()
        };
        return View("Error", model);
    }

    private static string BuildQuery(EventFilter f)
    {
        var pairs = new List<KeyValuePair<string, string?>>();
        if (!string.IsNullOrWhiteSpace(f.Search)) pairs.Add(new("search", f.Search.Trim()));
        if (!string.IsNullOrWhiteSpace(f.EventType)) pairs.Add(new("eventType", f.EventType));
        if (!string.IsNullOrWhiteSpace(f.Location)) pairs.Add(new("location", f.Location.Trim()));
        if (f.FromDate is { } fromDate) pairs.Add(new("fromDate", fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        if (f.ToDate is { } toDate) pairs.Add(new("toDate", toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        return QueryString.Create(pairs).ToUriComponent();
    }
}
