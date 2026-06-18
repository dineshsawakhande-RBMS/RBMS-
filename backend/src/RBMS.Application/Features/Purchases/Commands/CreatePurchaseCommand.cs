using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Purchases.Commands;

/// <summary>
/// Records a goods receipt (purchase entry). Each line flows through the stock ledger as a
/// PurchaseIn — auto-updating inventory and moving-average cost. The grand total is posted
/// to the supplier ledger (credit), and any amount paid is posted as a debit.
/// </summary>
public record CreatePurchaseCommand(
    Guid SupplierId,
    Guid StoreId,
    string? InvoiceNumber,
    DateOnly InvoiceDate,
    decimal Discount,
    decimal AmountPaid,
    string? Notes,
    IReadOnlyList<PurchaseItemInput> Items) : IRequest<Guid>, ITransactionalRequest;

public class CreatePurchaseCommandValidator : AbstractValidator<CreatePurchaseCommand>
{
    public CreatePurchaseCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AmountPaid).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Items).NotEmpty().WithMessage("A purchase needs at least one line.");
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(l => l.VariantId).NotEmpty();
            i.RuleFor(l => l.Quantity).GreaterThan(0);
            i.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
            i.RuleFor(l => l.GstRate).InclusiveBetween(0, 100);
        });
    }
}

public class CreatePurchaseCommandHandler : IRequestHandler<CreatePurchaseCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IStockLedger _ledger;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public CreatePurchaseCommandHandler(
        IApplicationDbContext db, IStockLedger ledger, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _ledger = ledger;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Guid> Handle(CreatePurchaseCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");

        decimal subtotal = 0, taxTotal = 0;
        var items = new List<PurchaseItem>();
        foreach (var line in request.Items)
        {
            var lineNet = line.Quantity * line.UnitCost;
            var lineTax = Math.Round(lineNet * line.GstRate / 100m, 2);
            subtotal += lineNet;
            taxTotal += lineTax;
            items.Add(new PurchaseItem
            {
                VariantId = line.VariantId,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                GstRate = line.GstRate,
                LineTotal = lineNet + lineTax
            });
        }

        var grandTotal = subtotal - request.Discount + taxTotal;
        if (request.AmountPaid > grandTotal)
            throw new ConflictException("Amount paid cannot exceed the grand total.");

        var purchase = new Purchase
        {
            TenantId = tenantId,
            StoreId = request.StoreId,
            SupplierId = request.SupplierId,
            InvoiceNumber = request.InvoiceNumber,
            InvoiceDate = request.InvoiceDate,
            Status = PurchaseStatus.Confirmed,
            Subtotal = subtotal,
            Discount = request.Discount,
            TaxTotal = taxTotal,
            GrandTotal = grandTotal,
            AmountPaid = request.AmountPaid,
            PaymentStatus = request.AmountPaid <= 0 ? PaymentStatus.Pending
                : request.AmountPaid >= grandTotal ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid,
            Notes = request.Notes,
            Items = items
        };
        _db.Purchases.Add(purchase);

        // Move every line INTO stock through the ledger (updates inventory + moving avg cost).
        foreach (var line in request.Items)
        {
            await _ledger.ApplyAsync(
                request.StoreId, line.VariantId, StockMovementType.PurchaseIn, line.Quantity,
                unitCost: line.UnitCost, referenceType: "Purchase", referenceId: purchase.Id,
                notes: request.InvoiceNumber, ct: cancellationToken);
        }

        // Supplier owes: credit the grand total; debit anything paid now.
        var entryDate = request.InvoiceDate;
        _db.SupplierLedger.Add(NewLedger(tenantId, request.SupplierId, entryDate, "Purchase",
            purchase.Id, credit: grandTotal, debit: 0, notes: request.InvoiceNumber));
        if (request.AmountPaid > 0)
            _db.SupplierLedger.Add(NewLedger(tenantId, request.SupplierId, entryDate, "Payment",
                purchase.Id, credit: 0, debit: request.AmountPaid, notes: "Paid on purchase"));

        await _db.SaveChangesAsync(cancellationToken);
        return purchase.Id;
    }

    private SupplierLedgerEntry NewLedger(
        Guid tenantId, Guid supplierId, DateOnly date, string refType, Guid refId,
        decimal credit, decimal debit, string? notes) => new()
        {
            TenantId = tenantId,
            SupplierId = supplierId,
            EntryDate = date,
            ReferenceType = refType,
            ReferenceId = refId,
            Credit = credit,
            Debit = debit,
            Notes = notes,
            CreatedAt = _clock.UtcNow,
            CreatedBy = _currentUser.UserId
        };
}
