using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Sales.Commands;

/// <summary>
/// POS billing. Each line moves stock OUT through the ledger (oversell-guarded) and snapshots
/// the moving-average cost as COGS for profit reporting. GST is split CGST/SGST (intra-state).
/// </summary>
public record CreateSaleCommand(
    Guid StoreId,
    Guid? CustomerId,
    decimal Discount,
    IReadOnlyList<SaleItemInput> Items,
    IReadOnlyList<SalePaymentInput> Payments,
    string? Notes) : IRequest<Guid>, ITransactionalRequest;

public class CreateSaleCommandValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleCommandValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Items).NotEmpty().WithMessage("A sale needs at least one line.");
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(l => l.VariantId).NotEmpty();
            i.RuleFor(l => l.Quantity).GreaterThan(0);
            i.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            i.RuleFor(l => l.Discount).GreaterThanOrEqualTo(0);
            i.RuleFor(l => l.GstRate).InclusiveBetween(0, 100);
        });
        RuleForEach(x => x.Payments).ChildRules(p => p.RuleFor(l => l.Amount).GreaterThan(0));
    }
}

public class CreateSaleCommandHandler : IRequestHandler<CreateSaleCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IStockLedger _ledger;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public CreateSaleCommandHandler(
        IApplicationDbContext db, IStockLedger ledger, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _ledger = ledger;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Guid> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");
        var now = _clock.UtcNow;

        // COGS = current moving-average cost per variant at this store (unchanged by the sale-out).
        var variantIds = request.Items.Select(i => i.VariantId).Distinct().ToList();
        var costs = await _db.Inventory
            .Where(i => i.StoreId == request.StoreId && variantIds.Contains(i.VariantId))
            .ToDictionaryAsync(i => i.VariantId, i => i.AvgCost, cancellationToken);

        decimal subtotal = 0, taxTotal = 0;
        var items = new List<SaleItem>();
        foreach (var line in request.Items)
        {
            var taxable = line.Quantity * line.UnitPrice - line.Discount;
            if (taxable < 0) taxable = 0;
            var tax = Math.Round(taxable * line.GstRate / 100m, 2);
            subtotal += taxable;
            taxTotal += tax;

            items.Add(new SaleItem
            {
                VariantId = line.VariantId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                UnitCost = costs.GetValueOrDefault(line.VariantId, 0m),
                Discount = line.Discount,
                GstRate = line.GstRate,
                TaxableAmount = taxable,
                TaxAmount = tax,
                LineTotal = taxable + tax
            });
        }

        var rawTotal = subtotal + taxTotal - request.Discount;
        if (rawTotal < 0) rawTotal = 0;
        var grandTotal = Math.Round(rawTotal, MidpointRounding.AwayFromZero); // whole-rupee
        var roundOff = grandTotal - rawTotal;
        var amountPaid = request.Payments.Sum(p => p.Amount);

        var sale = new Sale
        {
            TenantId = tenantId,
            StoreId = request.StoreId,
            CustomerId = request.CustomerId,
            CashierId = _currentUser.UserId,
            InvoiceNumber = $"INV-{now:yyyyMMddHHmmssfff}",
            InvoiceDate = now,
            Status = SaleStatus.Completed,
            Subtotal = subtotal,
            Discount = request.Discount,
            TaxableAmount = subtotal,
            Cgst = Math.Round(taxTotal / 2m, 2),
            Sgst = taxTotal - Math.Round(taxTotal / 2m, 2),
            Igst = 0,
            RoundOff = roundOff,
            GrandTotal = grandTotal,
            AmountPaid = amountPaid,
            ChangeDue = amountPaid > grandTotal ? amountPaid - grandTotal : 0,
            PaymentStatus = amountPaid <= 0 ? PaymentStatus.Pending
                : amountPaid >= grandTotal ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid,
            Notes = request.Notes,
            Items = items,
            Payments = request.Payments
                .Select(p => new SalePayment { Method = p.Method, Amount = p.Amount, Reference = p.Reference })
                .ToList()
        };
        _db.Sales.Add(sale);

        // Move every line OUT of stock (ledger refuses to oversell → 409).
        foreach (var line in request.Items)
        {
            await _ledger.ApplyAsync(
                request.StoreId, line.VariantId, StockMovementType.SaleOut, line.Quantity,
                referenceType: "Sale", referenceId: sale.Id, notes: sale.InvoiceNumber, ct: cancellationToken);
        }

        // Loyalty: 1 point per ₹100 of grand total, credited to the customer (if any).
        if (request.CustomerId is { } customerId)
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
            var points = (int)Math.Floor(grandTotal / 100m);
            if (customer is not null && points > 0)
            {
                customer.LoyaltyPoints += points;
                _db.LoyaltyTransactions.Add(new LoyaltyTransaction
                {
                    TenantId = tenantId,
                    CustomerId = customerId,
                    TxnType = LoyaltyTxnType.Earn,
                    Points = points,
                    ReferenceType = "Sale",
                    ReferenceId = sale.Id,
                    Notes = sale.InvoiceNumber,
                    CreatedAt = now
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return sale.Id;
    }
}
