using System.Net;

namespace RaceDay.MVC.Services;

/// <summary>The outcome of one call to the API. Error is always a message that is safe to show to the user.</summary>
public class ApiResult
{
    public HttpStatusCode StatusCode { get; init; }
    public bool Success { get; init; }
    public string? Error { get; init; }
    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }
}
