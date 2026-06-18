using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;

namespace RBMS.Application.Features.Inventory.Queries;

public record GetStockLevelsQuery(
    Guid StoreId,
    string? Search = null,
    bool LowStockOnly = false,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<StockLevelDto>>;

public class GetStockLevelsQueryHandler
    : IRequestHandler<GetStockLevelsQuery, PagedResult<StockLevelDto>>
{
    private readonly IApplicationDbContext _db;

    public GetStockLevelsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<StockLevelDto>> Handle(
        GetStockLevelsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.Inventory
            .AsNoTracking()
            .Where(i => i.StoreId == request.StoreId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(i =>
                i.Variant.Sku.ToLower().Contains(term) ||
                i.Variant.Product.Name.ToLower().Contains(term));
        }

        if (request.LowStockOnly)
            query = query.Where(i => i.QuantityOnHand <= i.Variant.ReorderLevel);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(i => i.Variant.Product.Name).ThenBy(i => i.Variant.Sku)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(i => new StockLevelDto(
                i.VariantId,
                i.Variant.Sku,
                i.Variant.Product.Name,
                i.Variant.Size,
                i.Variant.Color,
                i.QuantityOnHand,
                i.Variant.ReorderLevel,
                i.AvgCost,
                i.QuantityOnHand * i.AvgCost,
                i.QuantityOnHand <= i.Variant.ReorderLevel,
                i.Variant.SellingPrice))
            .ToListAsync(cancellationToken);

        return new PagedResult<StockLevelDto>(items, total, page, size);
    }
}
