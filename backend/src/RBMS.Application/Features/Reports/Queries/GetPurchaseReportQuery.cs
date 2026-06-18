using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Application.Features.Reports.Queries;

public record GetPurchaseReportQuery(DateOnly? From = null, DateOnly? To = null) : IRequest<PurchaseReportDto>;

public class GetPurchaseReportQueryHandler : IRequestHandler<GetPurchaseReportQuery, PurchaseReportDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;

    public GetPurchaseReportQueryHandler(IApplicationDbContext db, IDateTime clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PurchaseReportDto> Handle(GetPurchaseReportQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var from = request.From ?? new DateOnly(today.Year, today.Month, 1);
        var to = request.To ?? today;

        var rows = await _db.Purchases
            .AsNoTracking()
            .Where(p => p.InvoiceDate >= from && p.InvoiceDate <= to)
            .OrderBy(p => p.InvoiceDate)
            .Select(p => new PurchaseReportRow(
                p.InvoiceNumber, p.Supplier.Name, p.InvoiceDate,
                p.GrandTotal, p.AmountPaid, p.PaymentStatus.ToString()))
            .ToListAsync(cancellationToken);

        return new PurchaseReportDto(
            from, to, rows.Count, rows.Sum(r => r.GrandTotal), rows.Sum(r => r.AmountPaid), rows);
    }
}
