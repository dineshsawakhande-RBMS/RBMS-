using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Application.Common.Models;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Suppliers;
using RBMS.Application.Features.Suppliers.Commands;
using RBMS.Application.Features.Suppliers.Queries;

namespace RBMS.Api.Controllers;

public class SuppliersController : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.SupplierManage)]
    [ProducesResponseType(typeof(PagedResult<SupplierListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SupplierListItemDto>>> GetSuppliers(
        [FromQuery] GetSuppliersQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.SupplierManage)]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierDto>> GetSupplier(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetSupplierByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.SupplierManage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateSupplier([FromBody] CreateSupplierCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetSupplier), new { id }, id);
    }
}
