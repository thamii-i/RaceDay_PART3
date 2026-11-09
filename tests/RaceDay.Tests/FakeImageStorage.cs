using System.Collections.Concurrent;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

public class FakeImageStorage : IImageStorage
{
    private readonly ConcurrentDictionary<string, (byte[] Bytes, string ContentType)> _files = new();

    public async Task<string> UploadAsync(string folder, string prefix, Stream content, string contentType, string extension, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var name = $"{folder}/{prefix}-{Guid.NewGuid():N}{extension}";
        _files[name] = (buffer.ToArray(), contentType);
        return name;
    }

    public Task<StoredImage?> DownloadAsync(string blobName, CancellationToken cancellationToken)
    {
        if (_files.TryGetValue(blobName, out var file))
        {
            return Task.FromResult<StoredImage?>(new StoredImage(new MemoryStream(file.Bytes), file.ContentType));
        }

        return Task.FromResult<StoredImage?>(null);
    }

    public Task DeleteAsync(string blobName, CancellationToken cancellationToken)
    {
        _files.TryRemove(blobName, out _);
        return Task.CompletedTask;
    }
}
