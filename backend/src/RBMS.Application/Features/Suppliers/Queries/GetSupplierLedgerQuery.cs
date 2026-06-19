using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Application.Features.Suppliers.Queries;

public record GetSupplierLedgerQuery(Guid SupplierId) : IRequest<SupplierLedgerDto>;

public class GetSupplierLedgerQueryHandler : IRequestHandler<GetSupplierLedgerQuery, SupplierLedgerDto>
{
    private readonly IApplicationDbContext _db;

    public GetSupplierLedgerQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<SupplierLedgerDto> Handle(GetSupplierLedgerQuery request, CancellationToken cancellationToken)
    {
        var supplier = await _db.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Supplier), request.SupplierId);

        var raw = await _db.SupplierLedger.AsNoTracking()
            .Where(l => l.SupplierId == request.SupplierId)
            .OrderBy(l => l.EntryDate).ThenBy(l => l.CreatedAt)
            .Select(l => new { l.EntryDate, l.ReferenceType, l.Debit, l.Credit, l.Notes })
            .ToListAsync(cancellationToken);

        // Running balance from zero over all entries (the seeded "Opening" entry is included).
        decimal running = 0;
        var entries = raw.Select(e =>
        {
            running += e.Credit - e.Debit;
            return new SupplierLedgerEntryDto(e.EntryDate, e.ReferenceType, e.Debit, e.Credit, running, e.Notes);
        }).ToList();

        return new SupplierLedgerDto(supplier.Id, supplier.Name, running, entries);
    }
}
