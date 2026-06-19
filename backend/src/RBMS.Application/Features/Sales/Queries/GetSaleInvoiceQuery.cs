using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Application.Features.Sales.Queries;

public record InvoiceLineDto(
    string Sku, string ProductName, decimal Quantity, decimal UnitPrice,
    decimal GstRate, decimal TaxAmount, decimal LineTotal);

public record InvoiceDto(
    string BusinessName, string? BusinessGstin, string Currency,
    string InvoiceNumber, DateTimeOffset InvoiceDate, string? CustomerName,
    decimal Subtotal, decimal Discount, decimal Cgst, decimal Sgst,
    decimal GrandTotal, decimal AmountPaid, IReadOnlyList<InvoiceLineDto> Lines);

public record GetSaleInvoiceQuery(Guid SaleId) : IRequest<InvoiceDto>;

public class GetSaleInvoiceQueryHandler : IRequestHandler<GetSaleInvoiceQuery, InvoiceDto>
{
    private readonly IApplicationDbContext _db;

    public GetSaleInvoiceQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<InvoiceDto> Handle(GetSaleInvoiceQuery request, CancellationToken ct)
    {
        var sale = await _db.Sales
            .AsNoTracking()
            .Where(s => s.Id == request.SaleId)
            .Select(s => new
            {
                s.TenantId, s.CustomerId, s.InvoiceNumber, s.InvoiceDate,
                s.Subtotal, s.Discount, s.Cgst, s.Sgst, s.GrandTotal, s.AmountPaid,
                Lines = s.Items.Select(i => new InvoiceLineDto(
                    i.Variant.Sku, i.Variant.Product.Name, i.Quantity, i.UnitPrice,
                    i.GstRate, i.TaxAmount, i.LineTotal)).ToList()
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Sale), request.SaleId);

        var tenant = await _db.Tenants.AsNoTracking()
            .Where(t => t.Id == sale.TenantId)
            .Select(t => new { t.Name, t.Gstin, t.Currency })
            .FirstOrDefaultAsync(ct);

        string? customerName = null;
        if (sale.CustomerId is { } cid)
            customerName = await _db.Customers.AsNoTracking()
                .Where(c => c.Id == cid).Select(c => c.Name).FirstOrDefaultAsync(ct);

        return new InvoiceDto(
            tenant?.Name ?? "RBMS", tenant?.Gstin, tenant?.Currency ?? "INR",
            sale.InvoiceNumber, sale.InvoiceDate, customerName,
            sale.Subtotal, sale.Discount, sale.Cgst, sale.Sgst, sale.GrandTotal, sale.AmountPaid, sale.Lines);
    }
}
