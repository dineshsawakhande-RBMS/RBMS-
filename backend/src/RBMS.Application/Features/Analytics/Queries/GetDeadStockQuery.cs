using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Analytics.Queries;

/// <summary>
/// Slow / dead stock: in-stock variants that sold at or below <paramref name="SlowThreshold"/>
/// units in the trailing <paramref name="Days"/> window. "Dead" = zero sales in the window.
/// </summary>
public record GetDeadStockQuery(Guid? StoreId = null, int Days = 90, int SlowThreshold = 5)
    : IRequest<DeadStockReportDto>;

public class GetDeadStockQueryHandler : IRequestHandler<GetDeadStockQuery, DeadStockReportDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public GetDeadStockQueryHandler(IApplicationDbContext db, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<DeadStockReportDto> Handle(GetDeadStockQuery request, CancellationToken ct)
    {
        var storeId = request.StoreId ?? _currentUser.StoreId
            ?? throw new ForbiddenAccessException("No store context.");
        var days = Math.Clamp(request.Days, 1, 730);
        var slowThreshold = Math.Max(0, request.SlowThreshold);
        var now = _clock.UtcNow;
        var cutoff = now.AddDays(-days);

        // Units sold at this store within the window (single-scalar GroupBy → translates on Npgsql).
        var soldWindow = await _db.SaleItems.AsNoTracking()
            .Where(i => i.Sale.StoreId == storeId && i.Sale.Status == SaleStatus.Completed && i.Sale.InvoiceDate >= cutoff)
            .GroupBy(i => i.VariantId)
            .Select(g => new { VariantId = g.Key, Units = g.Sum(x => x.Quantity) })
            .ToListAsync(ct);
        var soldMap = soldWindow.ToDictionary(x => x.VariantId, x => x.Units);

        // Most recent sale at this store per variant, all time (for "days since last sale").
        var lastSale = await _db.SaleItems.AsNoTracking()
            .Where(i => i.Sale.StoreId == storeId && i.Sale.Status == SaleStatus.Completed)
            .GroupBy(i => i.VariantId)
            .Select(g => new { VariantId = g.Key, Last = g.Max(x => x.Sale.InvoiceDate) })
            .ToListAsync(ct);
        var lastMap = lastSale.ToDictionary(x => x.VariantId, x => x.Last);

        var inventory = await _db.Inventory.AsNoTracking()
            .Where(i => i.StoreId == storeId && i.QuantityOnHand > 0)
            .Select(i => new
            {
                i.VariantId,
                i.Variant.Sku,
                Name = i.Variant.Product.Name,
                i.QuantityOnHand,
                i.AvgCost,
            })
            .ToListAsync(ct);

        var rows = new List<DeadStockRow>();
        foreach (var i in inventory)
        {
            var units = soldMap.GetValueOrDefault(i.VariantId, 0m);
            if (units > slowThreshold) continue;   // healthy mover — not slow/dead

            DateTimeOffset? last = lastMap.TryGetValue(i.VariantId, out var l) ? l : null;
            int? daysSince = last.HasValue ? (int)(now - last.Value).TotalDays : null;
            var stockValue = Math.Round(i.QuantityOnHand * i.AvgCost, 2);

            rows.Add(new DeadStockRow(
                i.VariantId, i.Sku, i.Name, i.QuantityOnHand, i.AvgCost, stockValue,
                units, last, daysSince, units == 0m));
        }

        rows = rows.OrderByDescending(r => r.IsDead).ThenByDescending(r => r.StockValue).ToList();

        var deadValue = rows.Where(r => r.IsDead).Sum(r => r.StockValue);
        var slowValue = rows.Where(r => !r.IsDead).Sum(r => r.StockValue);

        return new DeadStockReportDto(
            days, slowThreshold, rows.Count(r => r.IsDead), rows.Count(r => !r.IsDead),
            deadValue, slowValue, deadValue + slowValue, rows);
    }
}
