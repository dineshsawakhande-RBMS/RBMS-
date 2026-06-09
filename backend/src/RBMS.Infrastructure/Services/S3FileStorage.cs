using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Infrastructure.Services;

public class AwsStorageOptions
{
    public const string SectionName = "AwsStorage";
    public string DocumentsBucket { get; set; } = "";
    public string ImagesBucket { get; set; } = "";
}

/// <summary>S3-backed object storage. Documents are accessed via short-lived presigned URLs.</summary>
public class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3;
    private readonly AwsStorageOptions _options;

    public S3FileStorage(IAmazonS3 s3, IOptions<AwsStorageOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    private string BucketFor(string key) =>
        key.StartsWith("images/", StringComparison.OrdinalIgnoreCase)
            ? _options.ImagesBucket
            : _options.DocumentsBucket;

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = BucketFor(key),
            Key = key,
            InputStream = content,
            ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS
        }, ct);
        return key;
    }

    public Task<string> GetPresignedDownloadUrlAsync(string key, TimeSpan validFor, CancellationToken ct = default)
        => Task.FromResult(_s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = BucketFor(key),
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(validFor)
        }));

    public Task<string> GetPresignedUploadUrlAsync(string key, string contentType, TimeSpan validFor, CancellationToken ct = default)
        => Task.FromResult(_s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = BucketFor(key),
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = DateTime.UtcNow.Add(validFor)
        }));

    public Task DeleteAsync(string key, CancellationToken ct = default)
        => _s3.DeleteObjectAsync(BucketFor(key), key, ct);
}
