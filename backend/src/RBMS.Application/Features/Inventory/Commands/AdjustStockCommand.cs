using FluentValidation;
using MediatR;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Inventory.Commands;

/// <summary>
/// Records a manual stock adjustment (stock count correction, theft, found stock, etc.).
/// Each line's signed delta is applied through the stock ledger — stock is never touched
/// directly. A positive delta is an AdjustmentIn, a negative delta an AdjustmentOut.
/// </summary>
public record AdjustStockCommand(
    Guid StoreId,
    string Reason,
    IReadOnlyList<AdjustStockLineInput> Lines) : IRequest<Guid>, ITransactionalRequest;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one adjustment line is required.");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(i => i.VariantId).NotEmpty();
            l.RuleFor(i => i.QuantityDelta).NotEqual(0).WithMessage("Delta cannot be zero.");
        });
    }
}

public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IStockLedger _ledger;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public AdjustStockCommandHandler(
        IApplicationDbContext db, IStockLedger ledger, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _ledger = ledger;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Guid> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new ForbiddenAccessException("No tenant context.");
        var now = _clock.UtcNow;

        var adjustment = new StockAdjustment
        {
            TenantId = tenantId,
            StoreId = request.StoreId,
            AdjustmentNo = $"ADJ-{now:yyyyMMddHHmmssfff}",
            Reason = request.Reason.Trim(),
            AdjustmentDate = DateOnly.FromDateTime(now.UtcDateTime),
            Lines = request.Lines.Select(l => new StockAdjustmentLine
            {
                VariantId = l.VariantId,
                QuantityDelta = l.QuantityDelta,
                UnitCost = l.UnitCost
            }).ToList()
        };
        _db.StockAdjustments.Add(adjustment);

        foreach (var line in request.Lines)
        {
            var type = line.QuantityDelta > 0
                ? StockMovementType.AdjustmentIn
                : StockMovementType.AdjustmentOut;

            await _ledger.ApplyAsync(
                request.StoreId, line.VariantId, type, Math.Abs(line.QuantityDelta),
                unitCost: line.UnitCost, referenceType: "Adjustment", referenceId: adjustment.Id,
                notes: request.Reason, ct: cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return adjustment.Id;
    }
}
