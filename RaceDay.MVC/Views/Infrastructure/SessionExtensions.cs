using RaceDay.MVC.Models.Api;

namespace RaceDay.MVC.Infrastructure;

public static class SessionExtensions
{
    public static bool IsLoggedIn(this ISession session) => !string.IsNullOrEmpty(session.GetString(SessionKeys.Token));

    public static string? UserRole(this ISession session) => session.GetString(SessionKeys.Role);

    public static string UserName(this ISession session) => session.GetString(SessionKeys.Name) ?? "";

    public static int? UserId(this ISession session) => session.GetInt32(SessionKeys.UserId);

    /// <summary>Creates the session: stores the token, user ID, role and name returned by the API.</summary>
    public static void SignIn(this ISession session, LoginResponse login)
    {
        session.Clear();
        session.SetString(SessionKeys.Token, login.Token);
        session.SetString(SessionKeys.Role, login.Role);
        session.SetInt32(SessionKeys.UserId, login.UserId);
        session.SetString(SessionKeys.Name, login.Name);
    }
}
