using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Products;
using RBMS.Application.Features.Products.Commands;
using RBMS.Application.Features.Products.Queries;

namespace RBMS.Api.Controllers;

public class ProductsController : ApiControllerBase
{
    private static readonly string[] AllowedContentTypes =
        { "image/jpeg", "image/png", "image/webp", "image/gif", "video/mp4", "video/webm" };
    private const long MaxUploadBytes = 25 * 1024 * 1024; // 25 MB

    private readonly IFileStorage _files;
    public ProductsController(IFileStorage files) => _files = files;

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

    // ---- images / video ----
    [HttpGet("{id:guid}/images")]
    [HasPermission(Permissions.ProductView)]
    [ProducesResponseType(typeof(IReadOnlyList<ProductImageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductImageDto>>> GetImages(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetProductImagesQuery(id), ct));

    [HttpPost("{id:guid}/images")]
    [HasPermission(Permissions.ProductManage)]
    [RequestSizeLimit(MaxUploadBytes + 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile file, [FromForm] bool isPrimary, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file uploaded.");
        if (file.Length > MaxUploadBytes) return BadRequest("File exceeds the 25 MB limit.");
        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest("Only JPEG/PNG/WebP/GIF images or MP4/WebM video are allowed.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = file.ContentType.StartsWith("video/") ? ".mp4" : ".jpg";
        var key = $"products/{id}/{Guid.NewGuid():N}{ext.ToLowerInvariant()}";

        await using (var stream = file.OpenReadStream())
            await _files.UploadAsync(key, stream, file.ContentType, ct);

        var imageId = await Mediator.Send(new AddProductImageCommand(id, key, isPrimary), ct);
        return Ok(new { id = imageId, url = $"/uploads/{key}" });
    }

    [HttpDelete("images/{imageId:guid}")]
    [HasPermission(Permissions.ProductManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteImage(Guid imageId, CancellationToken ct)
    {
        await Mediator.Send(new DeleteProductImageCommand(imageId), ct);
        return NoContent();
    }
}
