using FluentValidation;
using MediatR;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Inventory.Commands;

/// <summary>Writes off damaged stock — an outbound Damaged movement through the ledger.</summary>
public record RecordDamagedStockCommand(
    Guid StoreId,
    Guid VariantId,
    decimal Quantity,
    string? Notes) : IRequest, ITransactionalRequest;

public class RecordDamagedStockCommandValidator : AbstractValidator<RecordDamagedStockCommand>
{
    public RecordDamagedStockCommandValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class RecordDamagedStockCommandHandler : IRequestHandler<RecordDamagedStockCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IStockLedger _ledger;

    public RecordDamagedStockCommandHandler(IApplicationDbContext db, IStockLedger ledger)
    {
        _db = db;
        _ledger = ledger;
    }

    public async Task Handle(RecordDamagedStockCommand request, CancellationToken cancellationToken)
    {
        await _ledger.ApplyAsync(
            request.StoreId, request.VariantId, StockMovementType.Damaged, request.Quantity,
            referenceType: "Damaged", notes: request.Notes, ct: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
