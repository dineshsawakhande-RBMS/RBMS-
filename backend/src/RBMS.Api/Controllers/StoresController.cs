using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Stores;
using RBMS.Application.Features.Stores.Commands;
using RBMS.Application.Features.Stores.Queries;

namespace RBMS.Api.Controllers;

public class StoresController : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.StoreView)]
    [ProducesResponseType(typeof(IReadOnlyList<StoreListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StoreListItemDto>>> GetStores(
        [FromQuery] GetStoresQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.StoreView)]
    [ProducesResponseType(typeof(StoreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreDto>> GetStore(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetStoreByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.StoreManage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateStore([FromBody] CreateStoreCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetStore), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.StoreManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateStore(Guid id, [FromBody] UpdateStoreCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id must match.");
        await Mediator.Send(command, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.StoreManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteStore(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteStoreCommand(id), ct);
        return NoContent();
    }
}
