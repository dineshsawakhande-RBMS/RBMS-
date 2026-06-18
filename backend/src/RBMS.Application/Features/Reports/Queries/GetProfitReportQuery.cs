using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Application.Features.Reports.Queries;

public record GetProfitReportQuery(DateOnly? From = null, DateOnly? To = null) : IRequest<ProfitReportDto>;

public class GetProfitReportQueryHandler : IRequestHandler<GetProfitReportQuery, ProfitReportDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;

    public GetProfitReportQueryHandler(IApplicationDbContext db, IDateTime clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<ProfitReportDto> Handle(GetProfitReportQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var from = request.From ?? new DateOnly(today.Year, today.Month, 1);
        var to = request.To ?? today;

        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // Group by a single scalar key (product id) so it translates cleanly on Npgsql;
        // resolve names in a follow-up query.
        var grouped = await _db.SaleItems
            .Where(i => i.Sale.InvoiceDate >= start && i.Sale.InvoiceDate < end)
            .GroupBy(i => i.Variant.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TaxableAmount),
                Cogs = g.Sum(x => x.UnitCost * x.Quantity)
            })
            .ToListAsync(cancellationToken);

        var ids = grouped.Select(g => g.ProductId).ToList();
        var names = await _db.Products
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var rows = grouped
            .Select(g => new ProfitReportRow(
                names.GetValueOrDefault(g.ProductId, ""), g.Quantity, g.Revenue, g.Cogs, g.Revenue - g.Cogs))
            .OrderByDescending(r => r.Profit)
            .ToList();

        return new ProfitReportDto(
            from, to, rows.Sum(r => r.Revenue), rows.Sum(r => r.Cogs), rows.Sum(r => r.Profit), rows);
    }
}
