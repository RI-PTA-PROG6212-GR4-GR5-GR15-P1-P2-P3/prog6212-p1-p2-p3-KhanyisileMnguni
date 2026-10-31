namespace RaceDay.API.Services;

/// <summary>Checks that an uploaded file really is a small image before it is sent to Blob Storage.</summary>
public static class ImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024;   // 5 MB

    private static readonly HashSet<string> Extensions = new() { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly HashSet<string> ContentTypes = new() { "image/jpeg", "image/png", "image/gif", "image/webp" };

    /// <returns>An error message for the user, or null when the file is acceptable.</returns>
    public static async Task<string?> ValidateAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0) return "Choose an image file to upload.";
        if (file.Length > MaxBytes) return "The image must be 5 MB or smaller.";

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var contentType = (file.ContentType ?? "").ToLowerInvariant();
        if (!Extensions.Contains(extension) || !ContentTypes.Contains(contentType))
            return "Only JPG, PNG, GIF or WEBP images are allowed.";

        // The extension and content type come from the client, so also check the file's first bytes.
        var header = new byte[12];
        int read;
        await using (var stream = file.OpenReadStream())
            read = await stream.ReadAsync(header.AsMemory(0, header.Length));

        return LooksLikeImage(header, read) ? null : "The file is not a valid image.";
    }

    private static bool LooksLikeImage(byte[] b, int length)
    {
        if (length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return true;   // PNG
        if (length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return true;                    // JPEG
        if (length >= 4 && b[0] == 'G' && b[1] == 'I' && b[2] == 'F' && b[3] == '8') return true;         // GIF
        if (length >= 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'
                         && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') return true;     // WEBP
        return false;
    }
}
