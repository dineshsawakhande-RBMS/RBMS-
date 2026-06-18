using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;

namespace RBMS.Application.Features.Inventory.Queries;

/// <summary>The append-only ledger for a variant — most recent movements first.</summary>
public record GetMovementHistoryQuery(
    Guid VariantId,
    Guid? StoreId = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<StockMovementDto>>;

public class GetMovementHistoryQueryHandler
    : IRequestHandler<GetMovementHistoryQuery, PagedResult<StockMovementDto>>
{
    private readonly IApplicationDbContext _db;

    public GetMovementHistoryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<StockMovementDto>> Handle(
        GetMovementHistoryQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 200);

        var query = _db.StockMovements
            .AsNoTracking()
            .Where(m => m.VariantId == request.VariantId);

        if (request.StoreId is { } storeId)
            query = query.Where(m => m.StoreId == storeId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(m => new StockMovementDto(
                m.Id, m.MovementType, m.Quantity, m.BalanceAfter, m.UnitCost,
                m.ReferenceType, m.ReferenceId, m.Notes, m.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<StockMovementDto>(items, total, page, size);
    }
}
