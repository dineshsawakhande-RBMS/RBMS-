using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Analytics.Queries;

/// <summary>Customer retention over the trailing <paramref name="Months"/> months: repeat rate,
/// new-vs-returning trend, and the top customers by spend.</summary>
public record GetCustomerRetentionQuery(int Months = 6) : IRequest<CustomerRetentionDto>;

public class GetCustomerRetentionQueryHandler : IRequestHandler<GetCustomerRetentionQuery, CustomerRetentionDto>
{
    private static readonly string[] MonthNames =
        { "", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;

    public GetCustomerRetentionQueryHandler(IApplicationDbContext db, IDateTime clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<CustomerRetentionDto> Handle(GetCustomerRetentionQuery request, CancellationToken ct)
    {
        var months = Math.Clamp(request.Months, 1, 24);
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var firstOfThisMonth = new DateOnly(today.Year, today.Month, 1);
        var windowStartDate = firstOfThisMonth.AddMonths(-(months - 1));
        var windowStart = new DateTimeOffset(windowStartDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // Lifetime per-customer aggregates (single-scalar GroupBy → translates on Npgsql).
        var agg = await _db.Sales.AsNoTracking()
            .Where(s => s.CustomerId != null && s.Status == SaleStatus.Completed)
            .GroupBy(s => s.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Orders = g.Count(),
                Spend = g.Sum(x => x.GrandTotal),
                First = g.Min(x => x.InvoiceDate),
                Last = g.Max(x => x.InvoiceDate),
            })
            .ToListAsync(ct);

        var total = agg.Count;
        var repeat = agg.Count(a => a.Orders >= 2);
        var newInPeriod = agg.Count(a => a.First >= windowStart);
        var firstMonthOf = agg.ToDictionary(a => a.CustomerId!.Value, a => (a.First.Year, a.First.Month));

        // Sales within the window (customer + month) for the new-vs-returning trend.
        var windowSales = await _db.Sales.AsNoTracking()
            .Where(s => s.CustomerId != null && s.Status == SaleStatus.Completed && s.InvoiceDate >= windowStart)
            .Select(s => new { CustomerId = s.CustomerId!.Value, s.InvoiceDate })
            .ToListAsync(ct);

        var trend = new List<RetentionMonthPoint>();
        for (var k = 0; k < months; k++)
        {
            var m = windowStartDate.AddMonths(k);
            var active = windowSales
                .Where(s => s.InvoiceDate.Year == m.Year && s.InvoiceDate.Month == m.Month)
                .Select(s => s.CustomerId).Distinct().ToList();
            var newCount = active.Count(cid =>
                firstMonthOf.TryGetValue(cid, out var f) && f.Year == m.Year && f.Month == m.Month);
            trend.Add(new RetentionMonthPoint(
                m.Year, m.Month, $"{MonthNames[m.Month]} {m.Year}",
                active.Count, newCount, active.Count - newCount));
        }

        var topAgg = agg.OrderByDescending(a => a.Spend).Take(10).ToList();
        var topIds = topAgg.Select(a => a.CustomerId!.Value).ToList();
        var customers = await _db.Customers.AsNoTracking()
            .Where(c => topIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name, c.Mobile })
            .ToListAsync(ct);
        var custMap = customers.ToDictionary(c => c.Id);

        var topCustomers = topAgg.Select(a =>
        {
            var id = a.CustomerId!.Value;
            var c = custMap.GetValueOrDefault(id);
            return new TopCustomerRow(id, c?.Name ?? "(deleted)", c?.Mobile ?? "", a.Orders, a.Spend, a.Last);
        }).ToList();

        return new CustomerRetentionDto(
            months, total, repeat,
            total == 0 ? 0 : Math.Round(repeat * 100m / total, 1),
            newInPeriod,
            total == 0 ? 0 : Math.Round((decimal)agg.Average(a => a.Orders), 1),
            total == 0 ? 0 : Math.Round(agg.Average(a => a.Spend), 2),
            trend, topCustomers);
    }
}
