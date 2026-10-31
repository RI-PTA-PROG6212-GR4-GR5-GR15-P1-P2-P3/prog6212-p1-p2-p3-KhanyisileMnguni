namespace RaceDay.API.Services;

/// <summary>Container names used in Azure Blob Storage (must be lowercase).</summary>
public static class BlobContainers
{
    public const string EventBanners = "event-banners";
    public const string ProfilePictures = "profile-pictures";
}

public static class BlobNames
{
    /// <summary>"https://account.blob.core.windows.net/event-banners/abc123.png" becomes "abc123.png".</summary>
    public static string FromUrl(string blobUrl) => Path.GetFileName(new Uri(blobUrl).AbsolutePath);
}

public record BlobDownload(Stream Content, string ContentType);

/// <summary>All file storage goes through this interface. Only the API talks to Azure; the MVC site never does.</summary>
public interface IBlobStorageService
{
    /// <summary>Uploads the file and returns the blob's full URL (this is what is saved in the database).</summary>
    Task<string> UploadAsync(string container, IFormFile file, CancellationToken ct = default);

    /// <summary>Returns the blob's bytes and content type, or null if it does not exist.</summary>
    Task<BlobDownload?> DownloadAsync(string container, string blobUrl, CancellationToken ct = default);

    Task DeleteAsync(string container, string blobUrl, CancellationToken ct = default);
}

public static class BlobStorageExtensions
{
    /// <summary>Deletes a blob but never throws: a failed clean-up must not fail the user's request.</summary>
    public static async Task TryDeleteAsync(this IBlobStorageService blobs, string container, string? blobUrl)
    {
        if (string.IsNullOrEmpty(blobUrl)) return;
        try { await blobs.DeleteAsync(container, blobUrl); }
        catch (Exception) { /* best effort only */ }
    }
}
