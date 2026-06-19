using Microsoft.Extensions.Options;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Infrastructure.Services;

public class StorageOptions
{
    public const string SectionName = "Storage";
    /// <summary>"Local" (default) or "S3".</summary>
    public string Provider { get; set; } = "Local";
    /// <summary>Filesystem root for local storage; set from the web content root at startup.</summary>
    public string? LocalRoot { get; set; }
}

/// <summary>
/// Saves uploaded files to the local filesystem (under the configured root) and serves them
/// as static files at <c>/uploads/{key}</c>. The drop-in alternative to S3 for single-server
/// / on-prem deployments — no AWS required.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly StorageOptions _options;

    public LocalFileStorage(IOptions<StorageOptions> options) => _options = options.Value;

    private string Root => _options.LocalRoot
        ?? throw new InvalidOperationException("Storage:LocalRoot is not configured.");

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var full = Path.Combine(Root, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var fs = File.Create(full);
        await content.CopyToAsync(fs, ct);
        return key;
    }

    /// <summary>Returns the public static path; the frontend prefixes the API origin.</summary>
    public Task<string> GetPresignedDownloadUrlAsync(string key, TimeSpan validFor, CancellationToken ct = default)
        => Task.FromResult($"/uploads/{key}");

    public Task<string> GetPresignedUploadUrlAsync(string key, string contentType, TimeSpan validFor, CancellationToken ct = default)
        => throw new NotSupportedException("Local storage uses direct upload, not presigned URLs.");

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var full = Path.Combine(Root, key.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }
}
