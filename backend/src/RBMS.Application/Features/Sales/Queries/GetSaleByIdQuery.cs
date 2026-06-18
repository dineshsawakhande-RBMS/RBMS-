using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Application.Features.Sales.Queries;

public record GetSaleByIdQuery(Guid Id) : IRequest<SaleDto>;

public class GetSaleByIdQueryHandler : IRequestHandler<GetSaleByIdQuery, SaleDto>
{
    private readonly IApplicationDbContext _db;

    public GetSaleByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<SaleDto> Handle(GetSaleByIdQuery request, CancellationToken cancellationToken)
    {
        var sale = await _db.Sales
            .AsNoTracking()
            .Where(s => s.Id == request.Id)
            .Select(s => new SaleDto(
                s.Id, s.InvoiceNumber, s.InvoiceDate, s.StoreId, s.CustomerId, s.Status,
                s.Subtotal, s.Discount, s.Cgst, s.Sgst, s.GrandTotal, s.AmountPaid, s.ChangeDue, s.PaymentStatus,
                s.Items.Select(i => new SaleItemDto(
                    i.VariantId, i.Variant.Sku, i.Variant.Product.Name, i.Quantity,
                    i.UnitPrice, i.Discount, i.GstRate, i.TaxAmount, i.LineTotal)).ToList(),
                s.Payments.Select(p => new SalePaymentDto(p.Method, p.Amount, p.Reference)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return sale ?? throw new NotFoundException(nameof(Domain.Entities.Sale), request.Id);
    }
}
