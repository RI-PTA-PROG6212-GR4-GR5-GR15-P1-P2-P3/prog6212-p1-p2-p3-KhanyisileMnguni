using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.MVC.Infrastructure;

/// <summary>
/// Protects a page. Not logged in: redirected to the login page. Logged in with the wrong role: redirected
/// to the "access denied" page. Put it on a controller or an action, e.g. [SessionAuthorize("Organiser")].
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute(string? role = null) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var http = context.HttpContext;

        if (!http.Session.IsLoggedIn())
        {
            var returnUrl = http.Request.Path + http.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (role is not null && !string.Equals(http.Session.UserRole(), role, StringComparison.OrdinalIgnoreCase))
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
