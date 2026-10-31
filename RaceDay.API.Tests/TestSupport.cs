using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Services;

namespace RaceDay.API.Tests;

/// <summary>Stands in for Azure Blob Storage in the tests: files are kept in memory, nothing leaves the machine.</summary>
public class FakeBlobStorage : IBlobStorageService
{
    private readonly ConcurrentDictionary<string, (byte[] Bytes, string ContentType)> _blobs = new();

    public async Task<string> UploadAsync(string container, IFormFile file, CancellationToken ct = default)
    {
        using var memory = new MemoryStream();
        await file.CopyToAsync(memory, ct);
        var name = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        _blobs[$"{container}/{name}"] = (memory.ToArray(), file.ContentType);
        return $"https://fake-storage.test/{container}/{name}";   // like Azure, the upload returns a URL
    }

    public Task<BlobDownload?> DownloadAsync(string container, string blobUrl, CancellationToken ct = default)
    {
        BlobDownload? result = null;
        if (_blobs.TryGetValue($"{container}/{BlobNames.FromUrl(blobUrl)}", out var blob))
            result = new BlobDownload(new MemoryStream(blob.Bytes), blob.ContentType);
        return Task.FromResult(result);
    }

    public Task DeleteAsync(string container, string blobUrl, CancellationToken ct = default)
    {
        _blobs.TryRemove($"{container}/{BlobNames.FromUrl(blobUrl)}", out _);
        return Task.CompletedTask;
    }

    /// <summary>How many files are currently stored in a container.</summary>
    public int CountIn(string container) => _blobs.Keys.Count(k => k.StartsWith(container + "/"));
}

/// <summary>Boots the real API in memory, swapping SQL Server for an in-memory database and Azure for a fake.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "RaceDayTests_" + Guid.NewGuid();

    /// <summary>The fake storage this API instance is using, so tests can check what was stored or deleted.</summary>
    public FakeBlobStorage Blobs => Services.GetRequiredService<FakeBlobStorage>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // Remove every registration tied to AppDbContext's options (SQL Server provider),
            // then register the in-memory provider instead.
            var toRemove = services
                .Where(d => d.ServiceType.IsGenericType &&
                            d.ServiceType.GetGenericArguments().Contains(typeof(AppDbContext)) &&
                            d.ServiceType.Name.Contains("DbContextOptions"))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));

            // Never talk to real Azure in tests.
            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<FakeBlobStorage>();
            services.AddSingleton<IBlobStorageService>(sp => sp.GetRequiredService<FakeBlobStorage>());
        });
    }
}

/// <summary>Helpers that keep each test short and readable.</summary>
public static class Api
{
    public const string Password = "Passw0rd!123";
    public const string FutureDate = "2099-06-01";

    public static string UniqueEmail() => $"{Guid.NewGuid():N}@test.co.za";

    /// <summary>Logs in and attaches the returned JWT to every later request made by this client.</summary>
    public static async Task SignIn(HttpClient client, string email)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        res.EnsureSuccessStatusCode();
        var login = (await res.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
    }

    /// <summary>Each client carries its own token, so each one is a separate logged-in user.</summary>
    public static async Task<HttpClient> OrganiserClient(this ApiFactory f)
    {
        var client = f.CreateClient();
        var email = UniqueEmail();
        var reg = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Test Organiser",
            email,
            password = Password,
            role = "Organiser",
            organisationName = "Test Events"
        });
        reg.EnsureSuccessStatusCode();
        await SignIn(client, email);
        return client;
    }

    public static async Task<HttpClient> ParticipantClient(this ApiFactory f)
    {
        var client = f.CreateClient();
        var email = UniqueEmail();
        var reg = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Test Participant",
            email,
            password = Password,
            role = "Participant",
            dateOfBirth = "1995-05-24",
            emergencyContact = "082 555 0102"
        });
        reg.EnsureSuccessStatusCode();
        await SignIn(client, email);
        return client;
    }

    public static object EventBody(string name = "Test Run") => new
    {
        eventName = name,
        eventType = "Run",
        eventDate = FutureDate,
        location = "Centurion, Gauteng",
        description = "A test event."
    };

    public static async Task<int> CreateEvent(HttpClient organiser)
    {
        var res = await organiser.PostAsJsonAsync("/api/events", EventBody());
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<IdBody>())!.EventId;
    }

    /// <summary>Creates an event on a specific date (e.g. a past date to test "event already happened" rules).</summary>
    public static async Task<int> CreateEventOn(HttpClient organiser, string date, string name = "Dated Run")
        => await CreateEventFull(organiser, name, "Run", date, "Centurion, Gauteng");

    /// <summary>Creates an event with every field chosen by the test (used for the filter tests).</summary>
    public static async Task<int> CreateEventFull(HttpClient organiser, string name, string type, string date, string location)
    {
        var res = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = name,
            eventType = type,
            eventDate = date,
            location,
            description = "A test event."
        });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<IdBody>())!.EventId;
    }

    public static async Task<int> CreateCategory(HttpClient organiser, int eventId, int max = 100)
    {
        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "10km",
            distanceKM = 10.0,
            maxParticipants = max,
            entryFee = 150.0
        });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<IdBody>())!.CategoryId;
    }

    public static async Task<int> Enrol(HttpClient participant, int categoryId)
    {
        var res = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<EnrolmentResponse>())!.EnrolmentId;
    }

    /// <summary>The smallest bytes that still look like a PNG file (the API checks the file's first bytes).</summary>
    public static byte[] PngBytes(int extraBytes = 64) =>
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.Concat(new byte[extraBytes]).ToArray();

    /// <summary>Uploads a file as multipart form data with the field name "file", like the Swagger upload button does.</summary>
    public static async Task<HttpResponseMessage> UploadImage(HttpClient client, string url, byte[] bytes,
        string fileName = "picture.png", string contentType = "image/png")
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        return await client.PostAsync(url, form);
    }

    public record IdBody(int EventId, int CategoryId);
}
