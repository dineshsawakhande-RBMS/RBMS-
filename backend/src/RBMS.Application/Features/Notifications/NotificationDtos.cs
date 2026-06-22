using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Notifications;

public record NotificationDto(
    Guid Id, NotificationType Type, NotificationSeverity Severity, string Title, string Message,
    string? LinkPath, string? RelatedEntityType, Guid? RelatedEntityId, bool IsRead, DateTimeOffset CreatedAt);

/// <summary>Outcome of a refresh: how many alerts were added/cleared and the resulting unread total.</summary>
public record RefreshNotificationsResult(int Created, int Cleared, int UnreadCount);
