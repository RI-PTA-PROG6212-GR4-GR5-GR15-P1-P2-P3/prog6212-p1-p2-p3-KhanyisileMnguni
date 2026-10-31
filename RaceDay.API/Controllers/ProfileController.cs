using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[Route("api/profile")]
[Authorize]
public class ProfileController(AppDbContext db, IBlobStorageService blobs) : ApiControllerBase
{
    /// <summary>Returns the profile of the logged-in user (fields depend on their role).</summary>
    /// <response code="200">The profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult> Get()
    {
        if (IsOrganiser)
        {
            var o = await db.Organisers.FindAsync(CurrentUserId);
            if (o is null) return Fail(404, "User no longer exists.");
            return Ok(new ProfileResponse(o.OrganiserID, "Organiser", o.Name, o.Email, o.OrganisationName, null, null, false));
        }

        var p = await db.Participants.FindAsync(CurrentUserId);
        if (p is null) return Fail(404, "User no longer exists.");
        return Ok(ParticipantProfile(p));
    }

    /// <summary>Updates the logged-in user's profile.</summary>
    /// <remarks>Body: name, email, emergencyContact (participants), organisationName (organisers).</remarks>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Invalid fields.</response>
    /// <response code="409">Email belongs to another user.</response>
    [HttpPut]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult> Update(UpdateProfileRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        if (IsOrganiser)
        {
            var o = await db.Organisers.FindAsync(CurrentUserId);
            if (o is null) return Fail(404, "User no longer exists.");
            if (await EmailTaken(email, organiserId: o.OrganiserID))
                return Fail(409, "That email address is already registered.");

            o.Name = req.Name.Trim();
            o.Email = email;
            o.OrganisationName = req.OrganisationName?.Trim();
            await db.SaveChangesAsync();
            return Ok(new ProfileResponse(o.OrganiserID, "Organiser", o.Name, o.Email, o.OrganisationName, null, null, false));
        }

        var p = await db.Participants.FindAsync(CurrentUserId);
        if (p is null) return Fail(404, "User no longer exists.");
        if (await EmailTaken(email, participantId: p.ParticipantID))
            return Fail(409, "That email address is already registered.");

        p.Name = req.Name.Trim();
        p.Email = email;
        p.EmergencyContact = req.EmergencyContact?.Trim();
        await db.SaveChangesAsync();
        return Ok(ParticipantProfile(p));
    }

    /// <summary>Uploads (or replaces) the logged-in Participant's profile picture. Stored in Azure Blob Storage.</summary>
    /// <remarks>Send a multipart form with one file field named "file". JPG, PNG, GIF or WEBP, up to 5 MB.</remarks>
    /// <response code="200">Saved. Url is the route that serves the picture.</response>
    /// <response code="400">Not a valid image, wrong type, or too large.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpPost("picture")]
    [Authorize(Roles = "Participant")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult> UploadPicture(IFormFile file)
    {
        var p = await db.Participants.FindAsync(CurrentUserId);
        if (p is null) return Fail(404, "User no longer exists.");

        var error = await ImageRules.ValidateAsync(file);
        if (error is not null) return Fail(400, error);

        var newName = await blobs.UploadAsync(BlobContainers.ProfilePictures, file);
        var oldName = p.ProfilePictureUrl;
        p.ProfilePictureUrl = newName;
        await db.SaveChangesAsync();

        await blobs.TryDeleteAsync(BlobContainers.ProfilePictures, oldName);   // remove the replaced picture
        return Ok(new ImageUploadResponse(true, "/api/profile/picture"));
    }

    /// <summary>Returns the logged-in Participant's profile picture (the image itself).</summary>
    /// <response code="200">The image.</response>
    /// <response code="404">No picture uploaded yet.</response>
    [HttpGet("picture")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK, "image/png", "image/jpeg", "image/gif", "image/webp")]
    public async Task<ActionResult> GetPicture()
    {
        var p = await db.Participants.FindAsync(CurrentUserId);
        if (p?.ProfilePictureUrl is null) return Fail(404, "No profile picture has been uploaded.");

        var blob = await blobs.DownloadAsync(BlobContainers.ProfilePictures, p.ProfilePictureUrl);
        if (blob is null) return Fail(404, "No profile picture has been uploaded.");
        return File(blob.Content, blob.ContentType);
    }

    private static ProfileResponse ParticipantProfile(Participant p) =>
        new(p.ParticipantID, "Participant", p.Name, p.Email, null, p.DateOfBirth, p.EmergencyContact,
            p.ProfilePictureUrl is not null);

    private async Task<bool> EmailTaken(string email, int? organiserId = null, int? participantId = null) =>
        await db.Organisers.AnyAsync(x => x.Email == email && x.OrganiserID != organiserId) ||
        await db.Participants.AnyAsync(x => x.Email == email && x.ParticipantID != participantId);
}
