using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;

namespace RBMS.Application.Features.Purchases.Queries;

public record GetPurchasesQuery(
    Guid? SupplierId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<PurchaseListItemDto>>;

public class GetPurchasesQueryHandler : IRequestHandler<GetPurchasesQuery, PagedResult<PurchaseListItemDto>>
{
    private readonly IApplicationDbContext _db;

    public GetPurchasesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<PurchaseListItemDto>> Handle(
        GetPurchasesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.Purchases.AsNoTracking();
        if (request.SupplierId is { } sid)
            query = query.Where(p => p.SupplierId == sid);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.InvoiceDate).ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => new PurchaseListItemDto(
                p.Id, p.Supplier.Name, p.InvoiceNumber, p.InvoiceDate,
                p.GrandTotal, p.AmountPaid, p.PaymentStatus))
            .ToListAsync(cancellationToken);

        return new PagedResult<PurchaseListItemDto>(items, total, page, size);
    }
}
