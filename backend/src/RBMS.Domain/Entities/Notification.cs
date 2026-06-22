using RBMS.Domain.Common;
using RBMS.Domain.Enums;

namespace RBMS.Domain.Entities;

/// <summary>
/// A per-user in-app notification. Rows are reconciled from live feeds (low-stock, expiring
/// documents, salary-due, pending leaves) on refresh; <see cref="DedupKey"/> keeps refreshes
/// idempotent and lets resolved alerts be pruned.
/// </summary>
public class Notification : AuditableEntity
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationSeverity Severity { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;

    /// <summary>In-app route to open when the notification is clicked (e.g. "/inventory").</summary>
    public string? LinkPath { get; set; }

    /// <summary>Stable identity of the underlying alert (e.g. "lowstock:{variantId}").</summary>
    public string DedupKey { get; set; } = null!;

    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }

    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
