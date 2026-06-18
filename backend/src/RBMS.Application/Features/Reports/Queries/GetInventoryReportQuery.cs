using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Application.Features.Reports.Queries;

public record GetInventoryReportQuery(Guid StoreId) : IRequest<InventoryReportDto>;

public class GetInventoryReportQueryHandler : IRequestHandler<GetInventoryReportQuery, InventoryReportDto>
{
    private readonly IApplicationDbContext _db;

    public GetInventoryReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<InventoryReportDto> Handle(GetInventoryReportQuery request, CancellationToken cancellationToken)
    {
        var rows = await _db.Inventory
            .AsNoTracking()
            .Where(i => i.StoreId == request.StoreId)
            .OrderBy(i => i.Variant.Product.Name).ThenBy(i => i.Variant.Sku)
            .Select(i => new InventoryReportRow(
                i.Variant.Sku, i.Variant.Product.Name, i.QuantityOnHand, i.AvgCost,
                i.QuantityOnHand * i.AvgCost))
            .ToListAsync(cancellationToken);

        return new InventoryReportDto(rows.Sum(r => r.StockValue), rows.Count, rows);
    }
}
