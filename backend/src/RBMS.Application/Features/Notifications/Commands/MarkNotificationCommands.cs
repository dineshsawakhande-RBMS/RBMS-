using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;

namespace RBMS.Application.Features.Notifications.Commands;

public record MarkNotificationReadCommand(Guid Id) : IRequest, ITransactionalRequest;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public MarkNotificationReadCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenAccessException("No user context.");
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == userId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Notification), request.Id);

        if (!n.IsRead)
        {
            n.IsRead = true;
            n.ReadAt = _clock.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }
}

public record MarkAllNotificationsReadCommand : IRequest<int>, ITransactionalRequest;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public MarkAllNotificationsReadCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenAccessException("No user context.");
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = _clock.UtcNow;
        }
        if (unread.Count > 0) await _db.SaveChangesAsync(ct);
        return unread.Count;
    }
}
