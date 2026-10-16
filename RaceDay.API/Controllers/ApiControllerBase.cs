using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>ID of the logged-in user, read from the JWT. Only call on [Authorize]d actions.</summary>
    protected int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected bool IsOrganiser => User.IsInRole("Organiser");
    protected bool IsParticipant => User.IsInRole("Participant");

    protected ObjectResult Fail(int statusCode, string detail) => Problem(detail: detail, statusCode: statusCode);
}
