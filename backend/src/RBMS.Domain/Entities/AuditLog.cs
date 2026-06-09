using RBMS.Domain.Enums;

namespace RBMS.Domain.Entities;

/// <summary>Written automatically by the audit SaveChanges interceptor. Append-only.</summary>
public class AuditLog
{
    public long Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public AuditAction Action { get; set; }
    public string EntityName { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? OldValues { get; set; }       // JSON
    public string? NewValues { get; set; }       // JSON
    public string[]? ChangedColumns { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
