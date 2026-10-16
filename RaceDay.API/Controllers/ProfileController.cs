using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[Route("api/profile")]
[Authorize]
public class ProfileController(AppDbContext db) : ApiControllerBase
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
            return Ok(new ProfileResponse(o.OrganiserID, "Organiser", o.Name, o.Email, o.OrganisationName, null, null));
        }

        var p = await db.Participants.FindAsync(CurrentUserId);
        if (p is null) return Fail(404, "User no longer exists.");
        return Ok(new ProfileResponse(p.ParticipantID, "Participant", p.Name, p.Email, null, p.DateOfBirth, p.EmergencyContact));
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
            return Ok(new ProfileResponse(o.OrganiserID, "Organiser", o.Name, o.Email, o.OrganisationName, null, null));
        }

        var p = await db.Participants.FindAsync(CurrentUserId);
        if (p is null) return Fail(404, "User no longer exists.");
        if (await EmailTaken(email, participantId: p.ParticipantID))
            return Fail(409, "That email address is already registered.");

        p.Name = req.Name.Trim();
        p.Email = email;
        p.EmergencyContact = req.EmergencyContact?.Trim();
        await db.SaveChangesAsync();
        return Ok(new ProfileResponse(p.ParticipantID, "Participant", p.Name, p.Email, null, p.DateOfBirth, p.EmergencyContact));
    }

    private async Task<bool> EmailTaken(string email, int? organiserId = null, int? participantId = null) =>
        await db.Organisers.AnyAsync(x => x.Email == email && x.OrganiserID != organiserId) ||
        await db.Participants.AnyAsync(x => x.Email == email && x.ParticipantID != participantId);
}

