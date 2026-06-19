using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using RBMS.Application.Features.Payroll.Queries;

namespace RBMS.Api.Reporting;

/// <summary>Renders a monthly salary slip PDF using QuestPDF.</summary>
public static class SalarySlipPdf
{
    public const string ContentType = "application/pdf";
    private const string Accent = "#6C5CE7";
    private static readonly string[] Months =
        { "", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

    public static byte[] Generate(SalarySlipDto s)
    {
        string Money(decimal v) => $"{s.Currency} {v.ToString("N2", CultureInfo.InvariantCulture)}";
        var earnings = s.Lines.Where(l => l.IsEarning).ToList();
        var deductions = s.Lines.Where(l => !l.IsEarning).ToList();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(36);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor("#1a1a1a"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Text(s.BusinessName).FontSize(18).Bold().FontColor(Accent);
                    row.ConstantItem(220).Column(col =>
                    {
                        col.Item().AlignRight().Text("SALARY SLIP").FontSize(16).Bold();
                        col.Item().AlignRight().Text($"{Months[s.PeriodMonth]} {s.PeriodYear}").FontSize(10).FontColor("#666");
                    });
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Item().PaddingBottom(8).Row(r =>
                    {
                        r.RelativeItem().Text($"Employee: {s.EmployeeName} ({s.EmployeeCode})");
                        r.RelativeItem().AlignRight().Text($"{s.Designation ?? ""}");
                    });
                    col.Item().PaddingBottom(8).Text($"Attendance: {s.PresentDays}/{s.WorkingDays} days").FontSize(9).FontColor("#666");

                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Padding(4).Column(e =>
                        {
                            e.Item().Background(Accent).Padding(5).Text("Earnings").FontColor("#fff").Bold();
                            foreach (var line in earnings)
                                e.Item().BorderBottom(0.5f).BorderColor("#eee").Padding(5).Row(x =>
                                {
                                    x.RelativeItem().Text(line.Name);
                                    x.ConstantItem(90).AlignRight().Text(Money(line.Amount));
                                });
                        });
                        r.RelativeItem().Padding(4).Column(d =>
                        {
                            d.Item().Background("#9aa0a6").Padding(5).Text("Deductions").FontColor("#fff").Bold();
                            if (deductions.Count == 0)
                                d.Item().Padding(5).Text("—").FontColor("#999");
                            foreach (var line in deductions)
                                d.Item().BorderBottom(0.5f).BorderColor("#eee").Padding(5).Row(x =>
                                {
                                    x.RelativeItem().Text(line.Name);
                                    x.ConstantItem(90).AlignRight().Text(Money(line.Amount));
                                });
                        });
                    });

                    col.Item().PaddingTop(16).AlignRight().Text($"Net Pay: {Money(s.NetPay)}").FontSize(14).Bold().FontColor(Accent);
                });

                page.Footer().AlignCenter().Text("This is a system-generated salary slip.").FontSize(9).FontColor("#999");
            });
        });

        return doc.GeneratePdf();
    }
}
