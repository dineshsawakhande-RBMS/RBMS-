using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Application.Common.Models;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Customers;
using RBMS.Application.Features.Customers.Commands;
using RBMS.Application.Features.Customers.Queries;

namespace RBMS.Api.Controllers;

public class CustomersController : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.CustomerManage)]
    [ProducesResponseType(typeof(PagedResult<CustomerListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CustomerListItemDto>>> GetCustomers(
        [FromQuery] GetCustomersQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.CustomerManage)]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetCustomer(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetCustomerByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.CustomerManage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateCustomer([FromBody] CreateCustomerCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetCustomer), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.CustomerManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id must match.");
        await Mediator.Send(command, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.CustomerManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCustomer(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteCustomerCommand(id), ct);
        return NoContent();
    }
}
