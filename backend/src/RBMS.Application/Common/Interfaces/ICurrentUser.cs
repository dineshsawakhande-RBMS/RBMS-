namespace RBMS.Application.Common.Interfaces;

/// <summary>Ambient information about the authenticated caller, resolved from the JWT.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    Guid? StoreId { get; }
    string? Username { get; }
    string? IpAddress { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
}

/// <summary>Abstracts the system clock so handlers and tests are deterministic.</summary>
public interface IDateTime
{
    DateTimeOffset UtcNow { get; }
}
