using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;

namespace RBMS.Application.Features.Products.Queries;

public record GetProductsQuery(
    string? Search = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ProductListItemDto>>;

public class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, PagedResult<ProductListItemDto>>
{
    private readonly IApplicationDbContext _db;

    public GetProductsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ProductListItemDto>> Handle(
        GetProductsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);

        // Tenant + soft-delete scoping is applied automatically by global query filters.
        var query = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }
        if (request.CategoryId is { } cat)
            query = query.Where(p => p.CategoryId == cat);
        if (request.BrandId is { } brand)
            query = query.Where(p => p.BrandId == brand);
        if (request.IsActive is { } active)
            query = query.Where(p => p.IsActive == active);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => new ProductListItemDto(
                p.Id,
                p.Name,
                p.Brand != null ? p.Brand.Name : null,
                p.Category != null ? p.Category.Name : null,
                p.GstRate,
                p.Variants.Count,
                p.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductListItemDto>(items, total, page, size);
    }
}
