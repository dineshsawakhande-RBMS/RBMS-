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
}
