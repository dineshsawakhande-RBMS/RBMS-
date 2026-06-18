using RBMS.Domain.Enums;

namespace RBMS.Application.Common.Interfaces;

/// <summary>
/// The single choke-point for all stock changes. Every module (Purchase, Sales, Adjustments,
/// Transfers) goes through here — business code NEVER mutates <c>Inventory.QuantityOnHand</c>
/// directly. Each call appends a <c>StockMovement</c> and re-projects the inventory row.
/// </summary>
public interface IStockLedger
{
    /// <param name="quantity">Positive magnitude; the ledger applies the sign from the type.</param>
    /// <remarks>Does NOT call SaveChanges — the caller's unit of work commits the transaction.</remarks>
    Task ApplyAsync(
        Guid storeId,
        Guid variantId,
        StockMovementType movementType,
        decimal quantity,
        decimal? unitCost = null,
        string? referenceType = null,
        Guid? referenceId = null,
        string? notes = null,
        CancellationToken ct = default);
}
