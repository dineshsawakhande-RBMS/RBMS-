using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;

namespace RBMS.Application.Features.Suppliers.Queries;

public record GetSuppliersQuery(
    string? Search = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<SupplierListItemDto>>;

public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, PagedResult<SupplierListItemDto>>
{
    private readonly IApplicationDbContext _db;

    public GetSuppliersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<SupplierListItemDto>> Handle(
        GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term) || s.Code.ToLower().Contains(term));
        }
        if (request.IsActive is { } active)
            query = query.Where(s => s.IsActive == active);

        var total = await query.CountAsync(cancellationToken);

        // Outstanding = opening balance + Σ(credit − debit) from the ledger.
        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(s => new SupplierListItemDto(
                s.Id, s.Code, s.Name, s.Phone, s.Gstin,
                s.OpeningBalance + _db.SupplierLedger
                    .Where(l => l.SupplierId == s.Id && l.ReferenceType != "Opening")
                    .Sum(l => l.Credit - l.Debit),
                s.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<SupplierListItemDto>(items, total, page, size);
    }
}
