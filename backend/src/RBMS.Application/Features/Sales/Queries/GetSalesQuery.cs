using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;

namespace RBMS.Application.Features.Sales.Queries;

public record GetSalesQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<SaleListItemDto>>;

public class GetSalesQueryHandler : IRequestHandler<GetSalesQuery, PagedResult<SaleListItemDto>>
{
    private readonly IApplicationDbContext _db;

    public GetSalesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<SaleListItemDto>> Handle(GetSalesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.Sales.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.InvoiceDate)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(s => new SaleListItemDto(
                s.Id, s.InvoiceNumber, s.InvoiceDate, s.GrandTotal, s.Status, s.PaymentStatus))
            .ToListAsync(cancellationToken);

        return new PagedResult<SaleListItemDto>(items, total, page, size);
    }
}
