using RBMS.Domain.Entities;

namespace RBMS.Application.Common.Interfaces;

public record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);

/// <summary>Issues JWT access tokens and opaque rotating refresh tokens.</summary>
public interface IJwtTokenService
{
    TokenPair CreateTokenPair(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
    string HashRefreshToken(string rawToken);
}

/// <summary>Hashes and verifies user passwords (ASP.NET Core PasswordHasher under the hood).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}

/// <summary>Object storage abstraction over S3 (presigned URLs, upload, delete).</summary>
public interface IFileStorage
{
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default);
    Task<string> GetPresignedDownloadUrlAsync(string key, TimeSpan validFor, CancellationToken ct = default);
    Task<string> GetPresignedUploadUrlAsync(string key, string contentType, TimeSpan validFor, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}

/// <summary>Transactional email via SES.</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
