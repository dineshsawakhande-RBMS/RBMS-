using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Application.Features.Products.Queries;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IApplicationDbContext _db;

    public GetProductByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.HsnCode,
                p.GstRate,
                p.CategoryId,
                p.Category != null ? p.Category.Name : null,
                p.BrandId,
                p.Brand != null ? p.Brand.Name : null,
                p.IsActive,
                p.Variants
                    .OrderBy(v => v.Sku)
                    .Select(v => new ProductVariantDto(
                        v.Id, v.Sku, v.Barcode, v.Size, v.Color,
                        v.PurchasePrice, v.SellingPrice, v.Mrp, v.ReorderLevel, v.IsActive))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return product ?? throw new NotFoundException(nameof(Domain.Entities.Product), request.Id);
    }
}
