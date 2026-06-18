using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Application.Features.Dashboard;

public record DashboardSummaryDto(
    decimal TodaySales,
    decimal MonthlySales,
    decimal PurchaseCost,
    decimal Profit,
    decimal InventoryValue,
    int ProductCount,
    int LowStockCount,
    int EmployeeCount,
    decimal PendingSalaries,
    decimal MonthlyExpenses,
    decimal CashFlow,
    IReadOnlyList<TopProductDto> TopSellingProducts);

public record TopProductDto(Guid ProductId, string Name, decimal QuantitySold, decimal Revenue);

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

/// <summary>
/// Aggregates the headline KPIs in a single round-trip. Sales/profit come from the Sales
/// module, inventory valuation from the projected inventory, purchases from goods receipts.
/// Employee/salary/expense figures wire in as those modules are built (see docs/roadmap.md).
/// </summary>
public class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;

    public GetDashboardSummaryQueryHandler(IApplicationDbContext db, IDateTime clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var todayStart = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var todaySales = await _db.Sales
            .Where(s => s.InvoiceDate >= todayStart)
            .SumAsync(s => (decimal?)s.GrandTotal, cancellationToken) ?? 0m;

        var monthlySales = await _db.Sales
            .Where(s => s.InvoiceDate >= monthStart)
            .SumAsync(s => (decimal?)s.GrandTotal, cancellationToken) ?? 0m;

        // Profit (month) = Σ (line taxable − COGS) over sale items in this month.
        var monthlyProfit = await _db.SaleItems
            .Where(i => i.Sale.InvoiceDate >= monthStart)
            .SumAsync(i => (decimal?)(i.TaxableAmount - i.UnitCost * i.Quantity), cancellationToken) ?? 0m;

        var purchaseCost = await _db.Purchases
            .Where(p => p.InvoiceDate >= DateOnly.FromDateTime(monthStart.UtcDateTime))
            .SumAsync(p => (decimal?)p.GrandTotal, cancellationToken) ?? 0m;

        var inventoryValue = await _db.Inventory
            .SumAsync(i => (decimal?)(i.QuantityOnHand * i.AvgCost), cancellationToken) ?? 0m;

        var productCount = await _db.Products.CountAsync(p => p.IsActive, cancellationToken);

        var lowStockCount = await _db.Inventory
            .CountAsync(i => i.QuantityOnHand <= i.Variant.ReorderLevel, cancellationToken);

        // Group by the (joined) product id only — a single scalar key translates cleanly to
        // SQL; resolve product names in a small follow-up query. (Grouping by a multi-member
        // key across nested joins does not translate on Npgsql.)
        var topRaw = await _db.SaleItems
            .GroupBy(i => i.Variant.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topIds = topRaw.Select(t => t.ProductId).ToList();
        var names = await _db.Products
            .Where(p => topIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var topSelling = topRaw
            .Select(t => new TopProductDto(
                t.ProductId, names.GetValueOrDefault(t.ProductId, ""), t.Quantity, t.Revenue))
            .ToList();

        return new DashboardSummaryDto(
            TodaySales: todaySales,
            MonthlySales: monthlySales,
            PurchaseCost: purchaseCost,
            Profit: monthlyProfit,
            InventoryValue: inventoryValue,
            ProductCount: productCount,
            LowStockCount: lowStockCount,
            EmployeeCount: 0,
            PendingSalaries: 0m,
            MonthlyExpenses: 0m,
            CashFlow: monthlySales - purchaseCost,
            TopSellingProducts: topSelling);
    }
}
