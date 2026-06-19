using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Api.Reporting;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Payroll;
using RBMS.Application.Features.Payroll.Commands;
using RBMS.Application.Features.Payroll.Queries;

namespace RBMS.Api.Controllers;

public class PayrollController : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.PayrollManage)]
    [ProducesResponseType(typeof(IReadOnlyList<PayrollListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PayrollListItemDto>>> GetPayrolls(
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
        => Ok(await Mediator.Send(new GetPayrollsQuery(year, month), ct));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.PayrollManage)]
    [ProducesResponseType(typeof(PayrollDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PayrollDto>> GetPayroll(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetPayrollByIdQuery(id), ct));

    [HttpPost("generate")]
    [HasPermission(Permissions.PayrollManage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Generate([FromBody] GeneratePayrollCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));

    [HttpPost("{id:guid}/pay")]
    [HasPermission(Permissions.PayrollManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkPaid(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new MarkPayrollPaidCommand(id), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/slip")]
    [HasPermission(Permissions.PayrollManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Slip(Guid id, CancellationToken ct)
    {
        var slip = await Mediator.Send(new GetPayrollSlipQuery(id), ct);
        var pdf = SalarySlipPdf.Generate(slip);
        return File(pdf, SalarySlipPdf.ContentType, $"salary-slip-{slip.EmployeeCode}-{slip.PeriodMonth:00}{slip.PeriodYear}.pdf");
    }

    [HttpGet("advances")]
    [HasPermission(Permissions.PayrollManage)]
    [ProducesResponseType(typeof(IReadOnlyList<SalaryAdvanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SalaryAdvanceDto>>> GetAdvances(
        [FromQuery] Guid? employeeId, CancellationToken ct)
        => Ok(await Mediator.Send(new GetSalaryAdvancesQuery(employeeId), ct));

    [HttpPost("advances")]
    [HasPermission(Permissions.PayrollManage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> CreateAdvance([FromBody] CreateSalaryAdvanceCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));
}
