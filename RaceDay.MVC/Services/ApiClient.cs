using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RaceDay.MVC.Infrastructure;

namespace RaceDay.MVC.Services;

/// <summary>
/// The one class that talks to the Part 2 API. It adds the logged-in user's token (from the session) to every
/// request and turns every failure (wrong input, API down, expired session) into a friendly message.
/// Controllers never build HTTP requests themselves.
/// </summary>
public class ApiClient(IHttpClientFactory factory, IHttpContextAccessor accessor, ILogger<ApiClient> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private record Raw(HttpStatusCode Status, byte[] Body, string? ContentType, bool Reachable);

    // ---------- JSON calls ----------

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
        SendAsync<T>(HttpMethod.Post, url, JsonBody(body));

    public Task<ApiResult<T>> PutAsync<T>(string url, object body) =>
        SendAsync<T>(HttpMethod.Put, url, JsonBody(body));

    /// <summary>For calls that return no body (204 No Content), e.g. logout or cancel.</summary>
    public async Task<ApiResult> PostAsync(string url, object? body = null) =>
        ToResult(await SendRawAsync(HttpMethod.Post, url, JsonBody(body)));

    public async Task<ApiResult> DeleteAsync(string url) =>
        ToResult(await SendRawAsync(HttpMethod.Delete, url));

    // ---------- File calls ----------

    /// <summary>Sends an uploaded file to the API (which stores it in Azure Blob Storage).</summary>
    public async Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file)
    {
        using var form = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.TryParse(file.ContentType, out var type)
            ? type
            : new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "file", file.FileName);

        return ToResult<T>(await SendRawAsync(HttpMethod.Post, url, form));
    }

    /// <summary>Downloads an image from the API. Returns null if there is none.</summary>
    public async Task<(byte[] Bytes, string ContentType)?> GetFileAsync(string url)
    {
        var raw = await SendRawAsync(HttpMethod.Get, url);
        if (raw.Status != HttpStatusCode.OK || raw.Body.Length == 0) return null;
        return (raw.Body, raw.ContentType ?? "application/octet-stream");
    }

    // ---------- internals ----------

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content = null) =>
        ToResult<T>(await SendRawAsync(method, url, content));

    private static StringContent JsonBody(object? body) =>
        new(JsonSerializer.Serialize(body ?? new { }, Json), Encoding.UTF8, "application/json");

    private async Task<Raw> SendRawAsync(HttpMethod method, string url, HttpContent? content = null)
    {
        try
        {
            var client = factory.CreateClient("Api");
            using var request = new HttpRequestMessage(method, url) { Content = content };

            var token = accessor.HttpContext?.Session.GetString(SessionKeys.Token);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.SendAsync(request);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            return new Raw(response.StatusCode, bytes, response.Content.Headers.ContentType?.MediaType, true);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "The RaceDay API could not be reached: {Method} {Url}", method, url);
            return new Raw(HttpStatusCode.ServiceUnavailable, [], null, false);
        }
    }

    private static ApiResult ToResult(Raw raw)
    {
        var error = ErrorFor(raw);
        return new ApiResult { StatusCode = raw.Status, Success = error is null, Error = error };
    }

    private ApiResult<T> ToResult<T>(Raw raw)
    {
        var error = ErrorFor(raw);
        if (error is not null)
            return new ApiResult<T> { StatusCode = raw.Status, Success = false, Error = error };

        try
        {
            var data = raw.Body.Length > 0 ? JsonSerializer.Deserialize<T>(raw.Body, Json) : default;
            return new ApiResult<T> { StatusCode = raw.Status, Success = true, Data = data };
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "The API returned a response that could not be read.");
            return new ApiResult<T>
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Success = false,
                Error = "Something went wrong on our side. Please try again."
            };
        }
    }

    /// <summary>A message that is safe to show the user, or null when the call succeeded.</summary>
    private static string? ErrorFor(Raw raw)
    {
        if (!raw.Reachable)
            return "We can't reach the RaceDay service right now. Please try again in a moment.";

        var code = (int)raw.Status;
        if (code is >= 200 and < 300) return null;

        var detail = ExtractMessage(raw.Body);
        return raw.Status switch
        {
            HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
            HttpStatusCode.Forbidden => detail ?? "You are not allowed to do that.",
            HttpStatusCode.NotFound => detail ?? "We couldn't find what you were looking for.",
            HttpStatusCode.BadRequest or HttpStatusCode.Conflict =>
                detail ?? "That couldn't be completed. Please check your details and try again.",
            _ => "Something went wrong on our side. Please try again."
        };
    }

    /// <summary>Pulls the friendly message out of the API's error JSON ("detail", or the validation "errors").</summary>
    private static string? ExtractMessage(byte[] body)
    {
        if (body.Length == 0) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                return detail.GetString();

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array ? p.Value.EnumerateArray().ToList() : new List<JsonElement>())
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString())
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Distinct();
                var joined = string.Join(" ", messages);
                return joined.Length > 0 ? joined : null;
            }
        }
        catch (JsonException)
        {
            // not JSON: fall through to the generic message
        }
        return null;
    }
}
