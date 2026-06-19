using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Documents.Queries;

public record GetDocumentsQuery(
    string? Search = null,
    DocumentType? DocumentType = null,
    string? RelatedEntityType = null,
    Guid? RelatedEntityId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<DocumentListItemDto>>;

public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQuery, PagedResult<DocumentListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _files;

    public GetDocumentsQueryHandler(IApplicationDbContext db, IFileStorage files)
    {
        _db = db;
        _files = files;
    }

    public async Task<PagedResult<DocumentListItemDto>> Handle(GetDocumentsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.Documents.AsNoTracking();

        if (request.DocumentType.HasValue)
            query = query.Where(d => d.DocumentType == request.DocumentType.Value);

        if (!string.IsNullOrWhiteSpace(request.RelatedEntityType))
            query = query.Where(d => d.RelatedEntityType == request.RelatedEntityType);
        if (request.RelatedEntityId.HasValue)
            query = query.Where(d => d.RelatedEntityId == request.RelatedEntityId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(d => d.Title.ToLower().Contains(term)
                || d.FileName.ToLower().Contains(term)
                || (d.Tags != null && d.Tags.Contains(term))
                || (d.Description != null && d.Description.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .ToListAsync(ct);

        var items = await DocumentMapper.ToListItemsAsync(rows, _files, ct);
        return new PagedResult<DocumentListItemDto>(items, total, page, size);
    }
}

public record GetDocumentByIdQuery(Guid Id) : IRequest<DocumentDto>;

public class GetDocumentByIdQueryHandler : IRequestHandler<GetDocumentByIdQuery, DocumentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _files;

    public GetDocumentByIdQueryHandler(IApplicationDbContext db, IFileStorage files)
    {
        _db = db;
        _files = files;
    }

    public async Task<DocumentDto> Handle(GetDocumentByIdQuery request, CancellationToken ct)
    {
        var d = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Document), request.Id);

        var url = await _files.GetPresignedDownloadUrlAsync(d.FileKey, TimeSpan.FromMinutes(15), ct);
        return new DocumentDto(
            d.Id, d.Title, d.DocumentType, d.Description, DocumentTags.Split(d.Tags),
            d.FileName, d.ContentType, d.FileSizeBytes, d.IssueDate, d.ExpiryDate,
            d.RelatedEntityType, d.RelatedEntityId, url, d.CreatedAt);
    }
}

public record GetExpiringDocumentsQuery(int WithinDays = 30) : IRequest<IReadOnlyList<DocumentListItemDto>>;

public class GetExpiringDocumentsQueryHandler
    : IRequestHandler<GetExpiringDocumentsQuery, IReadOnlyList<DocumentListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _files;
    private readonly IDateTime _clock;

    public GetExpiringDocumentsQueryHandler(IApplicationDbContext db, IFileStorage files, IDateTime clock)
    {
        _db = db;
        _files = files;
        _clock = clock;
    }

    public async Task<IReadOnlyList<DocumentListItemDto>> Handle(GetExpiringDocumentsQuery request, CancellationToken ct)
    {
        var days = Math.Clamp(request.WithinDays, 1, 365);
        var cutoff = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime).AddDays(days);

        var rows = await _db.Documents.AsNoTracking()
            .Where(d => d.ExpiryDate != null && d.ExpiryDate <= cutoff)
            .OrderBy(d => d.ExpiryDate)
            .ToListAsync(ct);

        return await DocumentMapper.ToListItemsAsync(rows, _files, ct);
    }
}

internal static class DocumentMapper
{
    public static async Task<IReadOnlyList<DocumentListItemDto>> ToListItemsAsync(
        IReadOnlyList<Document> rows, IFileStorage files, CancellationToken ct)
    {
        var items = new List<DocumentListItemDto>(rows.Count);
        foreach (var d in rows)
        {
            var url = await files.GetPresignedDownloadUrlAsync(d.FileKey, TimeSpan.FromMinutes(15), ct);
            items.Add(new DocumentListItemDto(
                d.Id, d.Title, d.DocumentType, d.FileName, d.ContentType, d.FileSizeBytes,
                DocumentTags.Split(d.Tags), d.IssueDate, d.ExpiryDate,
                d.RelatedEntityType, d.RelatedEntityId, url, d.CreatedAt));
        }
        return items;
    }
}
