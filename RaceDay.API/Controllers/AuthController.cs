using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[Route("api/auth")]
public class AuthController(AppDbContext db, PasswordService passwords, JwtService jwt) : ApiControllerBase
{
    /// <summary>Registers a new user as an Organiser or a Participant.</summary>
    /// <remarks>
    /// Expects name, email, password (min 8 chars) and role ("Organiser" or "Participant").
    /// Organisers may add organisationName. Participants must add dateOfBirth and may add emergencyContact.
    /// The password is stored as a salted hash.
    /// </remarks>
    /// <response code="201">Returns the new user ID and role.</response>
    /// <response code="400">Invalid or missing fields.</response>
    /// <response code="409">Email already in use.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult> Register(RegisterRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var role = req.Role.Trim().ToLowerInvariant();

        if (role is not ("organiser" or "participant"))
            return Fail(400, "Role must be either 'Organiser' or 'Participant'.");

        if (await db.Organisers.AnyAsync(o => o.Email == email) ||
            await db.Participants.AnyAsync(p => p.Email == email))
            return Fail(409, "That email address is already registered.");

        if (role == "organiser")
        {
            var organiser = new Organiser
            {
                Name = req.Name.Trim(),
                Email = email,
                Password = passwords.Hash(req.Password),
                OrganisationName = req.OrganisationName?.Trim()
            };
            db.Organisers.Add(organiser);
            await db.SaveChangesAsync();
            return StatusCode(201, new AuthResponse(organiser.OrganiserID, "Organiser", organiser.Name));
        }

        if (req.DateOfBirth is null || req.DateOfBirth.Value >= DateOnly.FromDateTime(DateTime.Today))
            return Fail(400, "Participants must supply a valid dateOfBirth in the past.");

        var participant = new Participant
        {
            Name = req.Name.Trim(),
            Email = email,
            Password = passwords.Hash(req.Password),
            DateOfBirth = req.DateOfBirth.Value,
            EmergencyContact = req.EmergencyContact?.Trim()
        };
        db.Participants.Add(participant);
        await db.SaveChangesAsync();
        return StatusCode(201, new AuthResponse(participant.ParticipantID, "Participant", participant.Name));
    }

    /// <summary>Logs a user in and returns a JWT access token.</summary>
    /// <remarks>
    /// Copy the returned token, click the Authorize button in Swagger and paste it in.
    /// It is then sent as "Authorization: Bearer {token}" on every request. The token carries the user's ID and role.
    /// </remarks>
    /// <response code="200">Returns the JWT token, its expiry, and the user ID, role and name.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult> Login(LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        int id; string name, role, hash;
        var organiser = await db.Organisers.FirstOrDefaultAsync(o => o.Email == email);
        if (organiser is not null)
        {
            (id, name, role, hash) = (organiser.OrganiserID, organiser.Name, "Organiser", organiser.Password);
        }
        else
        {
            var participant = await db.Participants.FirstOrDefaultAsync(p => p.Email == email);
            if (participant is null) return Fail(401, "Invalid email or password.");
            (id, name, role, hash) = (participant.ParticipantID, participant.Name, "Participant", participant.Password);
        }

        if (!passwords.Verify(hash, req.Password))
            return Fail(401, "Invalid email or password.");

        var token = jwt.CreateToken(id, name, email, role);

        return Ok(new LoginResponse(id, role, name, token.Token, token.ExpiresAtUtc));
    }
}

