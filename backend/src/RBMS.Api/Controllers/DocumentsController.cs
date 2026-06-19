using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Documents;
using RBMS.Application.Features.Documents.Commands;
using RBMS.Application.Features.Documents.Queries;
using RBMS.Domain.Enums;

namespace RBMS.Api.Controllers;

public class DocumentsController : ApiControllerBase
{
    private static readonly string[] AllowedContentTypes =
    {
        "application/pdf",
        "image/jpeg", "image/png", "image/webp", "image/gif",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain", "text/csv"
    };
    private const long MaxUploadBytes = 25 * 1024 * 1024; // 25 MB

    private readonly IFileStorage _files;
    public DocumentsController(IFileStorage files) => _files = files;

    [HttpGet]
    [HasPermission(Permissions.DocumentView)]
    [ProducesResponseType(typeof(PagedResult<DocumentListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DocumentListItemDto>>> GetDocuments(
        [FromQuery] GetDocumentsQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("expiring")]
    [HasPermission(Permissions.DocumentView)]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentListItemDto>>> GetExpiring(
        [FromQuery] int withinDays, CancellationToken ct)
        => Ok(await Mediator.Send(new GetExpiringDocumentsQuery(withinDays <= 0 ? 30 : withinDays), ct));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.DocumentView)]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> GetDocument(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetDocumentByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.DocumentManage)]
    [RequestSizeLimit(MaxUploadBytes + 1024)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadDocument(
        IFormFile file,
        [FromForm] string title,
        [FromForm] DocumentType documentType,
        [FromForm] string? description,
        [FromForm] string? tags,
        [FromForm] DateOnly? issueDate,
        [FromForm] DateOnly? expiryDate,
        [FromForm] string? relatedEntityType,
        [FromForm] Guid? relatedEntityId,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file uploaded.");
        if (file.Length > MaxUploadBytes) return BadRequest("File exceeds the 25 MB limit.");
        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest("Allowed types: PDF, image, Word, Excel, text/CSV.");
        if (string.IsNullOrWhiteSpace(title)) return BadRequest("Title is required.");

        var ext = Path.GetExtension(file.FileName);
        var key = $"documents/{Guid.NewGuid():N}/{Guid.NewGuid():N}{ext.ToLowerInvariant()}";

        await using (var stream = file.OpenReadStream())
            await _files.UploadAsync(key, stream, file.ContentType, ct);

        var id = await Mediator.Send(new CreateDocumentCommand(
            Title: title,
            DocumentType: documentType,
            Description: description,
            Tags: tags,
            FileKey: key,
            FileName: file.FileName,
            ContentType: file.ContentType,
            FileSizeBytes: file.Length,
            IssueDate: issueDate,
            ExpiryDate: expiryDate,
            RelatedEntityType: relatedEntityType,
            RelatedEntityId: relatedEntityId), ct);

        return CreatedAtAction(nameof(GetDocument), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.DocumentManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] UpdateDocumentCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id must match.");
        await Mediator.Send(command, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.DocumentManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteDocumentCommand(id), ct);
        return NoContent();
    }
}
