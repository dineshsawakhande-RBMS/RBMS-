using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Application.Common.Models;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Attendance;
using RBMS.Application.Features.Attendance.Commands;
using RBMS.Application.Features.Attendance.Queries;

namespace RBMS.Api.Controllers;

public class LeavesController : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.AttendanceView)]
    [ProducesResponseType(typeof(PagedResult<LeaveRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LeaveRequestDto>>> GetLeaves(
        [FromQuery] GetLeaveRequestsQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpPost]
    [HasPermission(Permissions.AttendanceManage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateLeave([FromBody] CreateLeaveRequestCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetLeaves), new { id }, id);
    }

    [HttpPost("{id:guid}/decide")]
    [HasPermission(Permissions.LeaveApprove)]   // only a manager / responsible person may decide
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Decide(Guid id, [FromBody] DecideLeaveRequestBody body, CancellationToken ct)
    {
        await Mediator.Send(new DecideLeaveRequestCommand(id, body.Approve, body.DecisionNotes), ct);
        return NoContent();
    }
}

public record DecideLeaveRequestBody(bool Approve, string? DecisionNotes);
