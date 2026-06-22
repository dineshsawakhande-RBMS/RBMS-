using Microsoft.AspNetCore.Mvc;
using RBMS.Application.Common.Models;
using RBMS.Application.Features.Notifications;
using RBMS.Application.Features.Notifications.Commands;
using RBMS.Application.Features.Notifications.Queries;

namespace RBMS.Api.Controllers;

/// <summary>Each user sees and manages only their own notifications (no extra permission needed).</summary>
public class NotificationsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetNotifications(
        [FromQuery] GetNotificationsQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken ct)
        => Ok(await Mediator.Send(new GetUnreadNotificationCountQuery(), ct));

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshNotificationsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<RefreshNotificationsResult>> Refresh(CancellationToken ct)
        => Ok(await Mediator.Send(new RefreshNotificationsCommand(), ct));

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new MarkNotificationReadCommand(id), ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> MarkAllRead(CancellationToken ct)
        => Ok(await Mediator.Send(new MarkAllNotificationsReadCommand(), ct));
}
