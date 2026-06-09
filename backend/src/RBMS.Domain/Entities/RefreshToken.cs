using RBMS.Domain.Common;

namespace RBMS.Domain.Entities;

/// <summary>
/// A rotating refresh token. The raw token is never stored — only its SHA-256 hash.
/// Rotation links each token to its replacement, enabling reuse detection.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = null!;
    public Guid Jti { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public Guid? ReplacedById { get; set; }
    public string? ReasonRevoked { get; set; }

    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}
