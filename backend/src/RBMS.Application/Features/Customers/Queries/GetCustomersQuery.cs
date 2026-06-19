using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;

namespace RBMS.Application.Features.Customers.Queries;

public record GetCustomersQuery(string? Search = null, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<CustomerListItemDto>>;

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PagedResult<CustomerListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public GetCustomersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<CustomerListItemDto>> Handle(GetCustomersQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term) || c.Mobile.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * size).Take(size)
            .Select(c => new CustomerListItemDto(c.Id, c.Name, c.Mobile, c.Email, c.LoyaltyPoints, c.IsActive))
            .ToListAsync(ct);

        return new PagedResult<CustomerListItemDto>(items, total, page, size);
    }
}

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto>;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    private readonly IApplicationDbContext _db;
    public GetCustomerByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken ct)
    {
        var c = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Customer), request.Id);

        return new CustomerDto(
            c.Id, c.Name, c.Mobile, c.Email, c.AddressLine1, c.City, c.State, c.Pincode,
            c.Birthday, c.Anniversary, c.LoyaltyPoints, c.IsActive);
    }
}
