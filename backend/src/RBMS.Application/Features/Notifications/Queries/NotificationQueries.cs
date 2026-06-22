using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;

namespace RBMS.Application.Features.Notifications.Queries;

public record GetNotificationsQuery(bool UnreadOnly = false, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<NotificationDto>>;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, PagedResult<NotificationDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetNotificationsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenAccessException("No user context.");
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (request.UnreadOnly) query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.Severity, n.Title, n.Message, n.LinkPath,
                n.RelatedEntityType, n.RelatedEntityId, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<NotificationDto>(items, total, page, size);
    }
}

public record GetUnreadNotificationCountQuery : IRequest<int>;

public class GetUnreadNotificationCountQueryHandler : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetUnreadNotificationCountQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenAccessException("No user context.");
        return await _db.Notifications.AsNoTracking().CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }
}
