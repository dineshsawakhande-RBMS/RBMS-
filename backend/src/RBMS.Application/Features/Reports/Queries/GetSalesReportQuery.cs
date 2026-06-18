using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Application.Features.Reports.Queries;

public record GetSalesReportQuery(DateOnly? From = null, DateOnly? To = null) : IRequest<SalesReportDto>;

public class GetSalesReportQueryHandler : IRequestHandler<GetSalesReportQuery, SalesReportDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;

    public GetSalesReportQueryHandler(IApplicationDbContext db, IDateTime clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<SalesReportDto> Handle(GetSalesReportQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var from = request.From ?? new DateOnly(today.Year, today.Month, 1);
        var to = request.To ?? today;

        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var rows = await _db.Sales
            .AsNoTracking()
            .Where(s => s.InvoiceDate >= start && s.InvoiceDate < end)
            .OrderBy(s => s.InvoiceDate)
            .Select(s => new SalesReportRow(
                s.InvoiceNumber, s.InvoiceDate, s.TaxableAmount, s.Cgst + s.Sgst + s.Igst,
                s.GrandTotal, s.PaymentStatus.ToString()))
            .ToListAsync(cancellationToken);

        return new SalesReportDto(
            from, to, rows.Count,
            rows.Sum(r => r.Taxable), rows.Sum(r => r.Tax), rows.Sum(r => r.GrandTotal), rows);
    }
}
