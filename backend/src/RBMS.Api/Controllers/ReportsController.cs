using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Api.Reporting;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Reports.Queries;

namespace RBMS.Api.Controllers;

public class ReportsController : ApiControllerBase
{
    private const string Csv = "csv";

    [HttpGet("sales")]
    [HasPermission(Permissions.ReportView)]
    public async Task<IActionResult> Sales(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? format, CancellationToken ct)
    {
        var report = await Mediator.Send(new GetSalesReportQuery(from, to), ct);
        if (!IsCsv(format)) return Ok(report);

        var bytes = Reporting.Csv.Build(
            new[] { "Invoice", "Date", "Taxable", "Tax", "Grand Total", "Payment" },
            report.Rows.Select(r => new[]
            {
                r.InvoiceNumber, r.Date.ToString("yyyy-MM-dd HH:mm"),
                Reporting.Csv.Money(r.Taxable), Reporting.Csv.Money(r.Tax),
                Reporting.Csv.Money(r.GrandTotal), r.PaymentStatus
            }));
        return File(bytes, "text/csv", $"sales-{report.From:yyyyMMdd}-{report.To:yyyyMMdd}.csv");
    }

    [HttpGet("purchases")]
    [HasPermission(Permissions.ReportView)]
    public async Task<IActionResult> Purchases(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? format, CancellationToken ct)
    {
        var report = await Mediator.Send(new GetPurchaseReportQuery(from, to), ct);
        if (!IsCsv(format)) return Ok(report);

        var bytes = Reporting.Csv.Build(
            new[] { "Invoice", "Supplier", "Date", "Grand Total", "Paid", "Payment" },
            report.Rows.Select(r => new[]
            {
                r.InvoiceNumber ?? "", r.SupplierName, r.Date.ToString("yyyy-MM-dd"),
                Reporting.Csv.Money(r.GrandTotal), Reporting.Csv.Money(r.AmountPaid), r.PaymentStatus
            }));
        return File(bytes, "text/csv", $"purchases-{report.From:yyyyMMdd}-{report.To:yyyyMMdd}.csv");
    }

    [HttpGet("inventory")]
    [HasPermission(Permissions.ReportView)]
    public async Task<IActionResult> Inventory(
        [FromQuery] Guid storeId, [FromQuery] string? format, CancellationToken ct)
    {
        var report = await Mediator.Send(new GetInventoryReportQuery(storeId), ct);
        if (!IsCsv(format)) return Ok(report);

        var bytes = Reporting.Csv.Build(
            new[] { "SKU", "Product", "On Hand", "Avg Cost", "Stock Value" },
            report.Rows.Select(r => new[]
            {
                r.Sku, r.ProductName, Reporting.Csv.Num(r.QuantityOnHand),
                Reporting.Csv.Money(r.AvgCost), Reporting.Csv.Money(r.StockValue)
            }));
        return File(bytes, "text/csv", "inventory-valuation.csv");
    }

    [HttpGet("profit")]
    [HasPermission(Permissions.ReportView)]
    public async Task<IActionResult> Profit(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? format, CancellationToken ct)
    {
        var report = await Mediator.Send(new GetProfitReportQuery(from, to), ct);
        if (!IsCsv(format)) return Ok(report);

        var bytes = Reporting.Csv.Build(
            new[] { "Product", "Qty Sold", "Revenue", "COGS", "Profit" },
            report.Rows.Select(r => new[]
            {
                r.ProductName, Reporting.Csv.Num(r.QuantitySold),
                Reporting.Csv.Money(r.Revenue), Reporting.Csv.Money(r.Cogs), Reporting.Csv.Money(r.Profit)
            }));
        return File(bytes, "text/csv", $"profit-{report.From:yyyyMMdd}-{report.To:yyyyMMdd}.csv");
    }

    private static bool IsCsv(string? format) => string.Equals(format, Csv, StringComparison.OrdinalIgnoreCase);
}
