using RBMS.Domain.Enums;

namespace RBMS.Application.Features.WhatsApp;

public record WhatsAppMessageDto(
    Guid Id, string ToPhone, string? RecipientName, WhatsAppMessageKind Kind, string Body,
    WhatsAppMessageStatus Status, string Provider, string? ProviderMessageId, string? Error,
    DateTimeOffset? SentAt, string? RelatedEntityType, Guid? RelatedEntityId, DateTimeOffset CreatedAt);
