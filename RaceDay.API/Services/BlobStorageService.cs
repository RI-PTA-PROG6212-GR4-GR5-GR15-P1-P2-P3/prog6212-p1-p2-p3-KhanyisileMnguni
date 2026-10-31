using System.Collections.Concurrent;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services;

/// <summary>
/// Stores uploaded images in Azure Blob Storage. The connection string is read from configuration
/// ("AzureBlobStorage:ConnectionString") and must come from User Secrets or an environment variable,
/// never from a file in the repository. The client is created on first use, so the API still starts
/// (and the unit tests still run) when no storage account is configured.
/// The containers are private: the browser never receives the Azure URL, the API serves the image itself.
/// </summary>
public class BlobStorageService(IConfiguration config) : IBlobStorageService
{
    private readonly Lazy<BlobServiceClient> _client = new(() =>
    {
        var connectionString = config["AzureBlobStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "AzureBlobStorage:ConnectionString is not configured. Set it with User Secrets " +
                "(dotnet user-secrets set \"AzureBlobStorage:ConnectionString\" \"...\").");
        return new BlobServiceClient(connectionString);
    });

    private readonly ConcurrentDictionary<string, bool> _containersChecked = new();

    private async Task<BlobContainerClient> GetContainerAsync(string name, CancellationToken ct)
    {
        var container = _client.Value.GetBlobContainerClient(name);
        if (_containersChecked.TryAdd(name, true))
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);   // private container
        return container;
    }

    public async Task<string> UploadAsync(string container, IFormFile file, CancellationToken ct = default)
    {
        var containerClient = await GetContainerAsync(container, ct);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blobName = $"{Guid.NewGuid():N}{extension}";     // unique, so uploads never overwrite each other
        var blob = containerClient.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
        }, ct);

        return blob.Uri.ToString();   // the URL that gets saved in the database
    }

    public async Task<BlobDownload?> DownloadAsync(string container, string blobUrl, CancellationToken ct = default)
    {
        var containerClient = await GetContainerAsync(container, ct);
        try
        {
            var blob = containerClient.GetBlobClient(BlobNames.FromUrl(blobUrl));
            var response = await blob.DownloadStreamingAsync(cancellationToken: ct);
            return new BlobDownload(response.Value.Content, response.Value.Details.ContentType ?? "application/octet-stream");
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string container, string blobUrl, CancellationToken ct = default)
    {
        var containerClient = await GetContainerAsync(container, ct);
        await containerClient.DeleteBlobIfExistsAsync(BlobNames.FromUrl(blobUrl), cancellationToken: ct);
    }
}
