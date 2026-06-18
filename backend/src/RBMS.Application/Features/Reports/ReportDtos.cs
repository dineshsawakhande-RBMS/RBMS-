namespace RBMS.Application.Features.Reports;

// ---- Sales report ----
public record SalesReportRow(
    string InvoiceNumber, DateTimeOffset Date, decimal Taxable, decimal Tax,
    decimal GrandTotal, string PaymentStatus);

public record SalesReportDto(
    DateOnly From, DateOnly To, int Count, decimal TotalTaxable, decimal TotalTax,
    decimal TotalSales, IReadOnlyList<SalesReportRow> Rows);

// ---- Purchase report ----
public record PurchaseReportRow(
    string? InvoiceNumber, string SupplierName, DateOnly Date,
    decimal GrandTotal, decimal AmountPaid, string PaymentStatus);

public record PurchaseReportDto(
    DateOnly From, DateOnly To, int Count, decimal TotalPurchases, decimal TotalPaid,
    IReadOnlyList<PurchaseReportRow> Rows);

// ---- Inventory valuation report ----
public record InventoryReportRow(
    string Sku, string ProductName, decimal QuantityOnHand, decimal AvgCost, decimal StockValue);

public record InventoryReportDto(
    decimal TotalValue, int LineCount, IReadOnlyList<InventoryReportRow> Rows);

// ---- Profit report (by product) ----
public record ProfitReportRow(
    string ProductName, decimal QuantitySold, decimal Revenue, decimal Cogs, decimal Profit);

public record ProfitReportDto(
    DateOnly From, DateOnly To, decimal TotalRevenue, decimal TotalCogs, decimal TotalProfit,
    IReadOnlyList<ProfitReportRow> Rows);
