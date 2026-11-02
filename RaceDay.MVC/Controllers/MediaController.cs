using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Infrastructure;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

/// <summary>
/// Serves images to the browser. The pictures live in Azure Blob Storage, but only the API may read them,
/// so this controller asks the API for the image and passes it on. The browser never sees an Azure address.
/// </summary>
public class MediaController(ApiClient api) : AppController
{
    public async Task<IActionResult> EventBanner(int id)
    {
        var file = await api.GetFileAsync($"/api/events/{id}/banner");
        if (file is null) return LocalRedirect("~/images/event-placeholder.svg");

        Response.Headers.CacheControl = "public, max-age=60";
        return File(file.Value.Bytes, file.Value.ContentType);
    }

    [SessionAuthorize("Participant")]
    public async Task<IActionResult> ProfilePicture()
    {
        var file = await api.GetFileAsync("/api/profile/picture");
        if (file is null) return LocalRedirect("~/images/avatar-placeholder.svg");

        Response.Headers.CacheControl = "private, no-cache";
        return File(file.Value.Bytes, file.Value.ContentType);
    }
}
