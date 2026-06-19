using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Attendance;
using RBMS.Application.Features.Attendance.Commands;
using RBMS.Application.Features.Attendance.Queries;

namespace RBMS.Api.Controllers;

public class AttendanceController : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.AttendanceView)]
    [ProducesResponseType(typeof(IReadOnlyList<AttendanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AttendanceDto>>> GetMonthly(
        [FromQuery] Guid employeeId, [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
        => Ok(await Mediator.Send(new GetMonthlyAttendanceQuery(employeeId, year, month), ct));

    [HttpGet("summary")]
    [HasPermission(Permissions.AttendanceView)]
    [ProducesResponseType(typeof(AttendanceSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceSummaryDto>> GetSummary(
        [FromQuery] Guid employeeId, [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
        => Ok(await Mediator.Send(new GetAttendanceSummaryQuery(employeeId, year, month), ct));

    [HttpPost]
    [HasPermission(Permissions.AttendanceManage)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> Mark([FromBody] MarkAttendanceCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));
}
