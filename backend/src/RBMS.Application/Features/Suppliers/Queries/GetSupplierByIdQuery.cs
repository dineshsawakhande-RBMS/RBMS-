using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Application.Features.Suppliers.Queries;

public record GetSupplierByIdQuery(Guid Id) : IRequest<SupplierDto>;

public class GetSupplierByIdQueryHandler : IRequestHandler<GetSupplierByIdQuery, SupplierDto>
{
    private readonly IApplicationDbContext _db;

    public GetSupplierByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<SupplierDto> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        var supplier = await _db.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Supplier), request.Id);

        var ledgerNet = await _db.SupplierLedger
            .Where(l => l.SupplierId == supplier.Id && l.ReferenceType != "Opening")
            .SumAsync(l => l.Credit - l.Debit, cancellationToken);

        return new SupplierDto(
            supplier.Id, supplier.Code, supplier.Name, supplier.Gstin, supplier.ContactPerson,
            supplier.Phone, supplier.Email, supplier.AddressLine1, supplier.City, supplier.State,
            supplier.Pincode, supplier.PaymentTermsDays, supplier.OpeningBalance + ledgerNet,
            supplier.IsActive);
    }
}
