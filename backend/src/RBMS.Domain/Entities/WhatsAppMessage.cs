using RBMS.Domain.Common;
using RBMS.Domain.Enums;

namespace RBMS.Domain.Entities;

/// <summary>
/// A WhatsApp message the shop sent (or attempted). Recorded as an outbox row so there's a
/// visible history regardless of provider; the actual delivery goes through
/// <see cref="Common.Interfaces.IWhatsAppSender"/> (local stub today, real provider later).
/// </summary>
public class WhatsAppMessage : AuditableEntity
{
    public string ToPhone { get; set; } = null!;
    public string? RecipientName { get; set; }
    public WhatsAppMessageKind Kind { get; set; }
    public string Body { get; set; } = null!;
    public WhatsAppMessageStatus Status { get; set; } = WhatsAppMessageStatus.Pending;
    public string Provider { get; set; } = null!;
    public string? ProviderMessageId { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset? SentAt { get; set; }

    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
}
