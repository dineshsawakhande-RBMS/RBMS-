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
/// Aggregates the headline KPIs. Values sourced from modules already built (Products) are
/// computed live; sales/purchase/expense/payroll figures wire in as those modules land
/// (see docs/roadmap.md). Kept as one query so the dashboard is a single round-trip.
/// </summary>
public class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IApplicationDbContext _db;

    public GetDashboardSummaryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var productCount = await _db.Products.CountAsync(p => p.IsActive, cancellationToken);

        // Inventory valuation = sum over variants of purchase_price (qty joins in once the
        // Inventory module is built). Placeholder figures are explicitly zero, never faked.
        var inventoryValue = await _db.ProductVariants
            .Where(v => v.IsActive)
            .SumAsync(v => (decimal?)v.PurchasePrice, cancellationToken) ?? 0m;

        return new DashboardSummaryDto(
            TodaySales: 0m,
            MonthlySales: 0m,
            PurchaseCost: 0m,
            Profit: 0m,
            InventoryValue: inventoryValue,
            ProductCount: productCount,
            LowStockCount: 0,
            EmployeeCount: 0,
            PendingSalaries: 0m,
            MonthlyExpenses: 0m,
            CashFlow: 0m,
            TopSellingProducts: Array.Empty<TopProductDto>());
    }
}
