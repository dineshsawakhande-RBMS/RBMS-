using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;

namespace RBMS.Application.Features.Products;

public record ProductImageDto(Guid Id, string Url, bool IsPrimary, bool IsVideo);

// ---- add (row is created after the controller saves the file via IFileStorage) ----
public record AddProductImageCommand(Guid ProductId, string Key, bool IsPrimary)
    : IRequest<Guid>, ITransactionalRequest;

public class AddProductImageCommandHandler : IRequestHandler<AddProductImageCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public AddProductImageCommandHandler(IUnitOfWork uow, IApplicationDbContext db, ICurrentUser currentUser, IDateTime clock)
    {
        _uow = uow;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Guid> Handle(AddProductImageCommand request, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");
        var exists = await _db.Products.AnyAsync(p => p.Id == request.ProductId, ct);
        if (!exists) throw new NotFoundException(nameof(Product), request.ProductId);

        var image = new ProductImage
        {
            TenantId = tenantId,
            ProductId = request.ProductId,
            S3Key = request.Key,
            IsPrimary = request.IsPrimary,
            SortOrder = 0,
            CreatedAt = _clock.UtcNow,
        };
        await _uow.Repository<ProductImage>().AddAsync(image, ct);
        await _uow.SaveChangesAsync(ct);
        return image.Id;
    }
}

// ---- list ----
public record GetProductImagesQuery(Guid ProductId) : IRequest<IReadOnlyList<ProductImageDto>>;

public class GetProductImagesQueryHandler : IRequestHandler<GetProductImagesQuery, IReadOnlyList<ProductImageDto>>
{
    private static readonly string[] VideoExt = { ".mp4", ".webm", ".mov", ".ogg" };
    private readonly IApplicationDbContext _db;

    public GetProductImagesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductImageDto>> Handle(GetProductImagesQuery request, CancellationToken ct)
    {
        var images = await _db.ProductImages.AsNoTracking()
            .Where(i => i.ProductId == request.ProductId)
            .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder)
            .Select(i => new { i.Id, i.S3Key, i.IsPrimary })
            .ToListAsync(ct);

        return images.Select(i => new ProductImageDto(
            i.Id, $"/uploads/{i.S3Key}", i.IsPrimary,
            VideoExt.Contains(Path.GetExtension(i.S3Key).ToLowerInvariant()))).ToList();
    }
}

// ---- delete (removes the file then the row) ----
public record DeleteProductImageCommand(Guid ImageId) : IRequest, ITransactionalRequest;

public class DeleteProductImageCommandHandler : IRequestHandler<DeleteProductImageCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _files;

    public DeleteProductImageCommandHandler(IApplicationDbContext db, IFileStorage files)
    {
        _db = db;
        _files = files;
    }

    public async Task Handle(DeleteProductImageCommand request, CancellationToken ct)
    {
        var image = await _db.ProductImages.FirstOrDefaultAsync(i => i.Id == request.ImageId, ct)
            ?? throw new NotFoundException(nameof(ProductImage), request.ImageId);

        await _files.DeleteAsync(image.S3Key, ct);
        _db.ProductImages.Remove(image);
        await _db.SaveChangesAsync(ct);
    }
}
