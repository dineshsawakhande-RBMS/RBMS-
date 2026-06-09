using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Application.Common.Models;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Products;
using RBMS.Application.Features.Products.Commands;
using RBMS.Application.Features.Products.Queries;

namespace RBMS.Api.Controllers;

public class ProductsController : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.ProductView)]
    [ProducesResponseType(typeof(PagedResult<ProductListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> GetProducts(
        [FromQuery] GetProductsQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.ProductView)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetProductByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.ProductManage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateProduct([FromBody] CreateProductCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetProduct), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.ProductManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id must match.");
        await Mediator.Send(command, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.ProductManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteProductCommand(id), ct);
        return NoContent();
    }
}
