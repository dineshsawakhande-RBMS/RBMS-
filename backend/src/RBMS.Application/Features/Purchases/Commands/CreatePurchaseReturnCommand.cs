using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Purchases.Commands;

/// <summary>
/// Returns goods to a supplier. Each line flows through the stock ledger as a
/// PurchaseReturn (outbound), and the total is debited to the supplier ledger
/// (reduces what we owe).
/// </summary>
public record CreatePurchaseReturnCommand(
    Guid SupplierId,
    Guid StoreId,
    Guid? PurchaseId,
    string? Reason,
    IReadOnlyList<PurchaseReturnItemInput> Items) : IRequest<Guid>, ITransactionalRequest;

public class CreatePurchaseReturnCommandValidator : AbstractValidator<CreatePurchaseReturnCommand>
{
    public CreatePurchaseReturnCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(l => l.VariantId).NotEmpty();
            i.RuleFor(l => l.Quantity).GreaterThan(0);
            i.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}

public class CreatePurchaseReturnCommandHandler : IRequestHandler<CreatePurchaseReturnCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IStockLedger _ledger;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public CreatePurchaseReturnCommandHandler(
        IApplicationDbContext db, IStockLedger ledger, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _ledger = ledger;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Guid> Handle(CreatePurchaseReturnCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");
        var now = _clock.UtcNow;

        var total = request.Items.Sum(i => i.Quantity * i.UnitCost);

        var ret = new PurchaseReturn
        {
            TenantId = tenantId,
            StoreId = request.StoreId,
            SupplierId = request.SupplierId,
            PurchaseId = request.PurchaseId,
            ReturnNumber = $"PR-{now:yyyyMMddHHmmssfff}",
            ReturnDate = DateOnly.FromDateTime(now.UtcDateTime),
            Reason = request.Reason,
            TotalAmount = total,
            Items = request.Items.Select(i => new PurchaseReturnItem
            {
                VariantId = i.VariantId,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost,
                LineTotal = i.Quantity * i.UnitCost
            }).ToList()
        };
        _db.PurchaseReturns.Add(ret);

        // Move stock OUT (the ledger guards against returning more than on hand).
        foreach (var line in request.Items)
        {
            await _ledger.ApplyAsync(
                request.StoreId, line.VariantId, StockMovementType.PurchaseReturn, line.Quantity,
                unitCost: line.UnitCost, referenceType: "PurchaseReturn", referenceId: ret.Id,
                notes: request.Reason, ct: cancellationToken);
        }

        // Returning goods reduces what we owe the supplier → debit.
        _db.SupplierLedger.Add(new SupplierLedgerEntry
        {
            TenantId = tenantId,
            SupplierId = request.SupplierId,
            EntryDate = ret.ReturnDate,
            ReferenceType = "PurchaseReturn",
            ReferenceId = ret.Id,
            Debit = total,
            Notes = request.Reason,
            CreatedAt = now,
            CreatedBy = _currentUser.UserId
        });

        await _db.SaveChangesAsync(cancellationToken);
        return ret.Id;
    }
}
