using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Application.Features.Purchases.Queries;

public record GetPurchaseByIdQuery(Guid Id) : IRequest<PurchaseDto>;

public class GetPurchaseByIdQueryHandler : IRequestHandler<GetPurchaseByIdQuery, PurchaseDto>
{
    private readonly IApplicationDbContext _db;

    public GetPurchaseByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PurchaseDto> Handle(GetPurchaseByIdQuery request, CancellationToken cancellationToken)
    {
        var purchase = await _db.Purchases
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(p => new PurchaseDto(
                p.Id, p.SupplierId, p.Supplier.Name, p.StoreId, p.InvoiceNumber, p.InvoiceDate,
                p.Status, p.Subtotal, p.Discount, p.TaxTotal, p.GrandTotal, p.AmountPaid, p.PaymentStatus,
                p.Items.Select(i => new PurchaseItemDto(
                    i.VariantId, i.Variant.Sku, i.Variant.Product.Name, i.Quantity,
                    i.UnitCost, i.GstRate, i.LineTotal)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return purchase ?? throw new NotFoundException(nameof(Domain.Entities.Purchase), request.Id);
    }
}
