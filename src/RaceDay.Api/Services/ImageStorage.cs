namespace RaceDay.Api.Services;

public record StoredImage(Stream Content, string ContentType);

public interface IImageStorage
{
    Task<string> UploadAsync(string folder, string prefix, Stream content, string contentType, string extension, CancellationToken cancellationToken);
    Task<StoredImage?> DownloadAsync(string blobName, CancellationToken cancellationToken);
    Task DeleteAsync(string blobName, CancellationToken cancellationToken);
}

public class LocalImageStorage : IImageStorage
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif"
    };

    private readonly string _root;

    public LocalImageStorage(IWebHostEnvironment environment)
    {
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        _root = Path.GetFullPath(Path.Combine(webRoot, "uploads"));
    }

    public async Task<string> UploadAsync(string folder, string prefix, Stream content, string contentType, string extension, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(_root, folder);
        Directory.CreateDirectory(directory);

        var fileName = $"{prefix}-{Guid.NewGuid():N}{extension}";
        await using var target = File.Create(Path.Combine(directory, fileName));
        await content.CopyToAsync(target, cancellationToken);

        return $"{folder}/{fileName}";
    }

    public Task<StoredImage?> DownloadAsync(string blobName, CancellationToken cancellationToken)
    {
        var fullPath = ResolvePath(blobName);
        if (fullPath is null || !File.Exists(fullPath))
        {
            return Task.FromResult<StoredImage?>(null);
        }

        var extension = Path.GetExtension(fullPath);
        var contentType = ContentTypes.TryGetValue(extension, out var found) ? found : "application/octet-stream";
        return Task.FromResult<StoredImage?>(new StoredImage(File.OpenRead(fullPath), contentType));
    }

    public Task DeleteAsync(string blobName, CancellationToken cancellationToken)
    {
        var fullPath = ResolvePath(blobName);
        if (fullPath is not null && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string? ResolvePath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        return fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? fullPath : null;
    }
}

public static class ImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif"
    };

    public static string? Validate(IFormFile? file, out string extension)
    {
        extension = string.Empty;

        if (file is null || file.Length == 0)
        {
            return "Please choose an image file to upload.";
        }

        if (file.Length > MaxBytes)
        {
            return "The image must be 5 MB or smaller.";
        }

        if (!Extensions.TryGetValue(file.ContentType, out var ext))
        {
            return "Only JPEG, PNG, WebP or GIF images are allowed.";
        }

        extension = ext;
        return null;
    }
}
