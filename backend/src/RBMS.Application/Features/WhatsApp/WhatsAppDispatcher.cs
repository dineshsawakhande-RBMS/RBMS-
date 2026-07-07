using RBMS.Application.Common.Interfaces;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.WhatsApp;

/// <summary>Records an outbox row, sends it through <see cref="IWhatsAppSender"/>, and stamps the
/// result. Does not call SaveChanges — the caller's unit of work commits.</summary>
internal static class WhatsAppDispatcher
{
    public static async Task<WhatsAppMessage> DispatchAsync(
        IApplicationDbContext db, IWhatsAppSender sender, IDateTime clock, Guid tenantId, Guid? storeId,
        string toPhone, string? recipientName, WhatsAppMessageKind kind, string body,
        string? relatedType, Guid? relatedId, CancellationToken ct)
    {
        var msg = new WhatsAppMessage
        {
            TenantId = tenantId,
            ToPhone = toPhone.Trim(),
            RecipientName = recipientName,
            Kind = kind,
            Body = body,
            Provider = sender.Provider,
            Status = WhatsAppMessageStatus.Pending,
            RelatedEntityType = relatedType,
            RelatedEntityId = relatedId,
        };

        var result = await sender.SendAsync(msg.ToPhone, body, ct);
        if (result.Success)
        {
            msg.Status = WhatsAppMessageStatus.Sent;
            msg.ProviderMessageId = result.ProviderMessageId;
            msg.SentAt = clock.UtcNow;
        }
        else
        {
            msg.Status = WhatsAppMessageStatus.Failed;
            msg.Error = result.Error;
        }

        db.WhatsAppMessages.Add(msg);
        return msg;
    }
}
