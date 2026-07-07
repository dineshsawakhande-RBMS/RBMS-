using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;
using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Inventory.Commands;

public record TransferStockLineInput(Guid VariantId, decimal Quantity);

/// <summary>
/// Moves stock between two stores of the same tenant. Each line is a TransferOut from the
/// source and a TransferIn to the destination (sharing one reference id), applied through the
/// stock ledger so the no-negative guard protects the source and moving-average cost carries over.
/// </summary>
public record TransferStockCommand(
    Guid FromStoreId, Guid ToStoreId, IReadOnlyList<TransferStockLineInput> Lines, string? Notes)
    : IRequest<Guid>, ITransactionalRequest;

public class TransferStockCommandValidator : AbstractValidator<TransferStockCommand>
{
    public TransferStockCommandValidator()
    {
        RuleFor(x => x.FromStoreId).NotEmpty();
        RuleFor(x => x.ToStoreId).NotEmpty();
        RuleFor(x => x.ToStoreId).NotEqual(x => x.FromStoreId)
            .WithMessage("Source and destination stores must differ.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line is required.");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(i => i.VariantId).NotEmpty();
            l.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}

public class TransferStockCommandHandler : IRequestHandler<TransferStockCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IStockLedger _ledger;

    public TransferStockCommandHandler(IApplicationDbContext db, IStockLedger ledger)
    {
        _db = db;
        _ledger = ledger;
    }

    public async Task<Guid> Handle(TransferStockCommand request, CancellationToken ct)
    {
        var storeIds = new[] { request.FromStoreId, request.ToStoreId };
        var found = await _db.Stores.CountAsync(s => storeIds.Contains(s.Id), ct);
        if (found < 2) throw new NotFoundException("Store", "source or destination");

        var transferId = Guid.NewGuid();
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? "Store transfer" : request.Notes.Trim();

        foreach (var line in request.Lines)
        {
            // Carry the source's moving-average cost to the destination.
            var sourceCost = await _db.Inventory.AsNoTracking()
                .Where(i => i.StoreId == request.FromStoreId && i.VariantId == line.VariantId)
                .Select(i => (decimal?)i.AvgCost)
                .FirstOrDefaultAsync(ct);

            await _ledger.ApplyAsync(
                request.FromStoreId, line.VariantId, StockMovementType.TransferOut, line.Quantity,
                referenceType: "Transfer", referenceId: transferId, notes: notes, ct: ct);

            await _ledger.ApplyAsync(
                request.ToStoreId, line.VariantId, StockMovementType.TransferIn, line.Quantity,
                unitCost: sourceCost, referenceType: "Transfer", referenceId: transferId, notes: notes, ct: ct);
        }

        await _db.SaveChangesAsync(ct);
        return transferId;
    }
}
