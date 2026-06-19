using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Api.Reporting;
using RBMS.Application.Common.Models;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Sales;
using RBMS.Application.Features.Sales.Commands;
using RBMS.Application.Features.Sales.Queries;

namespace RBMS.Api.Controllers;

public class SalesController : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.SaleCreate)]
    [ProducesResponseType(typeof(PagedResult<SaleListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SaleListItemDto>>> GetSales(
        [FromQuery] GetSalesQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.SaleCreate)]
    [ProducesResponseType(typeof(SaleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleDto>> GetSale(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetSaleByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.SaleCreate)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateSale([FromBody] CreateSaleCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetSale), new { id }, id);
    }

    [HttpGet("{id:guid}/invoice")]
    [HasPermission(Permissions.SaleCreate)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken ct)
    {
        var invoice = await Mediator.Send(new GetSaleInvoiceQuery(id), ct);
        var pdf = InvoicePdf.Generate(invoice);
        return File(pdf, InvoicePdf.ContentType, $"invoice-{invoice.InvoiceNumber}.pdf");
    }

    [HttpPost("returns")]
    [HasPermission(Permissions.SaleRefund)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateSaleReturn([FromBody] CreateSaleReturnCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));
}
