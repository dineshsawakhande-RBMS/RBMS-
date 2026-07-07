namespace RBMS.Application.Features.Analytics;

// ---- Dead / slow-moving stock ----
public record DeadStockRow(
    Guid VariantId, string Sku, string ProductName, decimal QuantityOnHand, decimal AvgCost,
    decimal StockValue, decimal UnitsSold, DateTimeOffset? LastSaleDate, int? DaysSinceLastSale, bool IsDead);

public record DeadStockReportDto(
    int Days, int SlowThreshold, int DeadCount, int SlowCount,
    decimal DeadValue, decimal SlowValue, decimal TotalTiedValue,
    IReadOnlyList<DeadStockRow> Rows);

// ---- Customer retention ----
public record RetentionMonthPoint(
    int Year, int Month, string Label, int ActiveCustomers, int NewCustomers, int ReturningCustomers);

public record TopCustomerRow(
    Guid CustomerId, string Name, string Mobile, int Orders, decimal TotalSpend, DateTimeOffset LastPurchase);

public record CustomerRetentionDto(
    int Months, int TotalCustomers, int RepeatCustomers, decimal RepeatRatePct, int NewCustomersInPeriod,
    decimal AvgOrdersPerCustomer, decimal AvgSpendPerCustomer,
    IReadOnlyList<RetentionMonthPoint> Trend, IReadOnlyList<TopCustomerRow> TopCustomers);
