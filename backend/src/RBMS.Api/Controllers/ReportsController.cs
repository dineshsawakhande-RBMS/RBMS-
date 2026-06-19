using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Api.Reporting;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Reports.Queries;

namespace RBMS.Api.Controllers;

public class ReportsController : ApiControllerBase
{
    [HttpGet("sales")]
    [HasPermission(Permissions.ReportView)]
    public async Task<IActionResult> Sales(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? format, CancellationToken ct)
    {
        var r = await Mediator.Send(new GetSalesReportQuery(from, to), ct);
        return Export(format, $"sales-{r.From:yyyyMMdd}-{r.To:yyyyMMdd}", "Sales", r,
            new[] { "Invoice", "Date", "Taxable", "Tax", "Grand Total", "Payment" },
            r.Rows.Select(x => new[]
            {
                x.InvoiceNumber, x.Date.ToString("yyyy-MM-dd HH:mm"),
                Csv.Money(x.Taxable), Csv.Money(x.Tax), Csv.Money(x.GrandTotal), x.PaymentStatus
            }));
    }

    [HttpGet("purchases")]
    [HasPermission(Permissions.ReportView)]
    public async Task<IActionResult> Purchases(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? format, CancellationToken ct)
    {
        var r = await Mediator.Send(new GetPurchaseReportQuery(from, to), ct);
        return Export(format, $"purchases-{r.From:yyyyMMdd}-{r.To:yyyyMMdd}", "Purchases", r,
            new[] { "Invoice", "Supplier", "Date", "Grand Total", "Paid", "Payment" },
            r.Rows.Select(x => new[]
            {
                x.InvoiceNumber ?? "", x.SupplierName, x.Date.ToString("yyyy-MM-dd"),
                Csv.Money(x.GrandTotal), Csv.Money(x.AmountPaid), x.PaymentStatus
            }));
    }

    [HttpGet("inventory")]
    [HasPermission(Permissions.ReportView)]
    public async Task<IActionResult> Inventory(
        [FromQuery] Guid storeId, [FromQuery] string? format, CancellationToken ct)
    {
        var r = await Mediator.Send(new GetInventoryReportQuery(storeId), ct);
        return Export(format, "inventory-valuation", "Inventory", r,
            new[] { "SKU", "Product", "On Hand", "Avg Cost", "Stock Value" },
            r.Rows.Select(x => new[]
            {
                x.Sku, x.ProductName, Csv.Num(x.QuantityOnHand), Csv.Money(x.AvgCost), Csv.Money(x.StockValue)
            }));
    }

    [HttpGet("profit")]
    [HasPermission(Permissions.ReportView)]
    public async Task<IActionResult> Profit(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? format, CancellationToken ct)
    {
        var r = await Mediator.Send(new GetProfitReportQuery(from, to), ct);
        return Export(format, $"profit-{r.From:yyyyMMdd}-{r.To:yyyyMMdd}", "Profit", r,
            new[] { "Product", "Qty Sold", "Revenue", "COGS", "Profit" },
            r.Rows.Select(x => new[]
            {
                x.ProductName, Csv.Num(x.QuantitySold), Csv.Money(x.Revenue), Csv.Money(x.Cogs), Csv.Money(x.Profit)
            }));
    }

    /// <summary>Returns the report as JSON (default), CSV (?format=csv), or Excel (?format=xlsx).</summary>
    private IActionResult Export(
        string? format, string fileName, string sheet, object json,
        string[] headers, IEnumerable<string[]> rows)
    {
        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            return File(Csv.Build(headers, rows), "text/csv", $"{fileName}.csv");
        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase))
            return File(Xlsx.Build(sheet, headers, rows), Xlsx.ContentType, $"{fileName}.xlsx");
        return Ok(json);
    }
}
