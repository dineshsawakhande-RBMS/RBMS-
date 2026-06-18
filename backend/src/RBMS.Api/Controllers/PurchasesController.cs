using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Application.Common.Models;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Purchases;
using RBMS.Application.Features.Purchases.Commands;
using RBMS.Application.Features.Purchases.Queries;

namespace RBMS.Api.Controllers;

public class PurchasesController : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.PurchaseView)]
    [ProducesResponseType(typeof(PagedResult<PurchaseListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PurchaseListItemDto>>> GetPurchases(
        [FromQuery] GetPurchasesQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.PurchaseView)]
    [ProducesResponseType(typeof(PurchaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseDto>> GetPurchase(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetPurchaseByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.PurchaseManage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreatePurchase([FromBody] CreatePurchaseCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetPurchase), new { id }, id);
    }

    [HttpPost("returns")]
    [HasPermission(Permissions.PurchaseManage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreatePurchaseReturn([FromBody] CreatePurchaseReturnCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));
}
