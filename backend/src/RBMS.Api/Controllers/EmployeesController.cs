using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Application.Common.Models;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Employees;
using RBMS.Application.Features.Employees.Commands;
using RBMS.Application.Features.Employees.Queries;

namespace RBMS.Api.Controllers;

public class EmployeesController : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.EmployeeManage)]
    [ProducesResponseType(typeof(PagedResult<EmployeeListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EmployeeListItemDto>>> GetEmployees(
        [FromQuery] GetEmployeesQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.EmployeeManage)]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDto>> GetEmployee(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetEmployeeByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.EmployeeManage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateEmployee([FromBody] CreateEmployeeCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetEmployee), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.EmployeeManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id must match.");
        await Mediator.Send(command, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.EmployeeManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteEmployee(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteEmployeeCommand(id), ct);
        return NoContent();
    }
}
