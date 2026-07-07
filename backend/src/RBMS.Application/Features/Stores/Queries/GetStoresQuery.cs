using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Application.Features.Stores.Queries;

public record GetStoresQuery(bool IncludeInactive = true) : IRequest<IReadOnlyList<StoreListItemDto>>;

public class GetStoresQueryHandler : IRequestHandler<GetStoresQuery, IReadOnlyList<StoreListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public GetStoresQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<StoreListItemDto>> Handle(GetStoresQuery request, CancellationToken ct)
    {
        var query = _db.Stores.AsNoTracking();
        if (!request.IncludeInactive) query = query.Where(s => s.IsActive);
        return await query
            .OrderBy(s => s.Name)
            .Select(s => new StoreListItemDto(s.Id, s.Code, s.Name, s.City, s.Phone, s.IsActive))
            .ToListAsync(ct);
    }
}

public record GetStoreByIdQuery(Guid Id) : IRequest<StoreDto>;

public class GetStoreByIdQueryHandler : IRequestHandler<GetStoreByIdQuery, StoreDto>
{
    private readonly IApplicationDbContext _db;
    public GetStoreByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<StoreDto> Handle(GetStoreByIdQuery request, CancellationToken ct)
    {
        var s = await _db.Stores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Store), request.Id);
        return new StoreDto(s.Id, s.Code, s.Name, s.Gstin, s.Phone, s.Email,
            s.AddressLine1, s.City, s.State, s.Pincode, s.IsActive);
    }
}
