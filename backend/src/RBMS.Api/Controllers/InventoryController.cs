using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Application.Common.Models;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Inventory;
using RBMS.Application.Features.Inventory.Commands;
using RBMS.Application.Features.Inventory.Queries;

namespace RBMS.Api.Controllers;

public class InventoryController : ApiControllerBase
{
    [HttpGet("levels")]
    [HasPermission(Permissions.InventoryView)]
    [ProducesResponseType(typeof(PagedResult<StockLevelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StockLevelDto>>> GetStockLevels(
        [FromQuery] GetStockLevelsQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("low-stock")]
    [HasPermission(Permissions.InventoryView)]
    [ProducesResponseType(typeof(PagedResult<StockLevelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StockLevelDto>>> GetLowStock(
        [FromQuery] Guid storeId, CancellationToken ct)
        => Ok(await Mediator.Send(new GetStockLevelsQuery(storeId, LowStockOnly: true, PageSize: 100), ct));

    [HttpGet("variants/{variantId:guid}/movements")]
    [HasPermission(Permissions.InventoryView)]
    [ProducesResponseType(typeof(PagedResult<StockMovementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StockMovementDto>>> GetMovements(
        Guid variantId, [FromQuery] GetMovementHistoryQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query with { VariantId = variantId }, ct));

    [HttpPost("adjustments")]
    [HasPermission(Permissions.InventoryAdjust)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> AdjustStock([FromBody] AdjustStockCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));

    [HttpPost("damaged")]
    [HasPermission(Permissions.InventoryAdjust)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordDamaged([FromBody] RecordDamagedStockCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return NoContent();
    }
}
