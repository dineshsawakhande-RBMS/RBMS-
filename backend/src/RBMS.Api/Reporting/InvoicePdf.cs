using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RBMS.Application.Features.Sales.Queries;

namespace RBMS.Api.Reporting;

/// <summary>Renders a GST tax invoice PDF for a sale using QuestPDF.</summary>
public static class InvoicePdf
{
    public const string ContentType = "application/pdf";
    private static readonly string Accent = "#6C5CE7";

    public static byte[] Generate(InvoiceDto inv)
    {
        string Money(decimal v) => $"{inv.Currency} {v.ToString("N2", CultureInfo.InvariantCulture)}";

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(36);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor("#1a1a1a"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(inv.BusinessName).FontSize(18).Bold().FontColor(Accent);
                        if (!string.IsNullOrWhiteSpace(inv.BusinessGstin))
                            col.Item().Text($"GSTIN: {inv.BusinessGstin}").FontSize(9).FontColor("#666");
                    });
                    row.ConstantItem(180).Column(col =>
                    {
                        col.Item().AlignRight().Text("TAX INVOICE").FontSize(16).Bold();
                        col.Item().AlignRight().Text(inv.InvoiceNumber).FontSize(10);
                        col.Item().AlignRight().Text(inv.InvoiceDate.ToString("dd MMM yyyy, HH:mm")).FontSize(9).FontColor("#666");
                    });
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Item().PaddingBottom(8).Text($"Bill to: {inv.CustomerName ?? "Walk-in customer"}").FontSize(11);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);   // item
                            c.RelativeColumn(1);   // qty
                            c.RelativeColumn(2);   // price
                            c.RelativeColumn(1);   // gst
                            c.RelativeColumn(2);   // total
                        });

                        table.Header(header =>
                        {
                            void H(string t, bool right = false)
                            {
                                var cell = header.Cell().Background(Accent).Padding(5);
                                (right ? cell.AlignRight() : cell.AlignLeft())
                                    .Text(t).FontColor("#fff").Bold().FontSize(9);
                            }
                            H("Item"); H("Qty", true); H("Price", true); H("GST%", true); H("Amount", true);
                        });

                        foreach (var line in inv.Lines)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor("#eee").Padding(5)
                                .Text($"{line.Sku} — {line.ProductName}");
                            table.Cell().BorderBottom(0.5f).BorderColor("#eee").Padding(5).AlignRight()
                                .Text(line.Quantity.ToString("0.###", CultureInfo.InvariantCulture));
                            table.Cell().BorderBottom(0.5f).BorderColor("#eee").Padding(5).AlignRight()
                                .Text(Money(line.UnitPrice));
                            table.Cell().BorderBottom(0.5f).BorderColor("#eee").Padding(5).AlignRight()
                                .Text($"{line.GstRate:0.#}%");
                            table.Cell().BorderBottom(0.5f).BorderColor("#eee").Padding(5).AlignRight()
                                .Text(Money(line.LineTotal));
                        }
                    });

                    col.Item().PaddingTop(12).AlignRight().Column(totals =>
                    {
                        void Row(string label, string value, bool bold = false)
                        {
                            totals.Item().Row(r =>
                            {
                                var l = r.ConstantItem(120).Text(label).FontSize(bold ? 12 : 10);
                                if (bold) l.Bold();
                                var v = r.ConstantItem(120).AlignRight().Text(value).FontSize(bold ? 12 : 10);
                                if (bold) v.Bold();
                            });
                        }
                        Row("Taxable", Money(inv.Subtotal));
                        if (inv.Discount > 0) Row("Discount", $"- {Money(inv.Discount)}");
                        Row("CGST", Money(inv.Cgst));
                        Row("SGST", Money(inv.Sgst));
                        Row("Grand Total", Money(inv.GrandTotal), bold: true);
                        Row("Paid", Money(inv.AmountPaid));
                    });
                });

                page.Footer().AlignCenter().Text("Thank you for your business!").FontSize(9).FontColor("#999");
            });
        });

        return doc.GeneratePdf();
    }
}
