using System.Collections.Concurrent;
using RBMS.Application.Common.Interfaces;

namespace RBMS.IntegrationTests;

/// <summary>Hermetic <see cref="IFileStorage"/> for tests — keeps bytes in memory, no disk I/O.</summary>
public class InMemoryFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new();

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        _store[key] = ms.ToArray();
        return key;
    }

    public Task<string> GetPresignedDownloadUrlAsync(string key, TimeSpan validFor, CancellationToken ct = default)
        => Task.FromResult($"/uploads/{key}");

    public Task<string> GetPresignedUploadUrlAsync(string key, string contentType, TimeSpan validFor, CancellationToken ct = default)
        => Task.FromResult($"/uploads/{key}");

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
