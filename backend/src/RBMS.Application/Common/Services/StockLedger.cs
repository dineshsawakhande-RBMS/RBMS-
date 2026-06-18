using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Application.Common.Services;

public class StockLedger : IStockLedger
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public StockLedger(IApplicationDbContext db, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    private static readonly HashSet<StockMovementType> Inbound = new()
    {
        StockMovementType.PurchaseIn, StockMovementType.SaleReturn, StockMovementType.TransferIn,
        StockMovementType.AdjustmentIn, StockMovementType.OpeningStock
    };

    public async Task ApplyAsync(
        Guid storeId, Guid variantId, StockMovementType movementType, decimal quantity,
        decimal? unitCost = null, string? referenceType = null, Guid? referenceId = null,
        string? notes = null, CancellationToken ct = default)
    {
        if (quantity <= 0)
            throw new ConflictException("Movement quantity must be greater than zero.");

        var isInbound = Inbound.Contains(movementType);
        var signedQty = isInbound ? quantity : -quantity;

        // Load or create the projection row for this (store, variant). Check the change
        // tracker first: within one unit of work several movements may touch the same
        // variant (e.g. multi-line adjustment), and those pending rows aren't yet queryable
        // from the store. Tracked so the caller's SaveChanges persists it in one transaction.
        var inventory =
            _db.Inventory.Local.FirstOrDefault(i => i.StoreId == storeId && i.VariantId == variantId)
            ?? await _db.Inventory.FirstOrDefaultAsync(
                   i => i.StoreId == storeId && i.VariantId == variantId, ct);

        if (inventory is null)
        {
            inventory = new Inventory
            {
                TenantId = _currentUser.TenantId ?? Guid.Empty,
                StoreId = storeId,
                VariantId = variantId,
                QuantityOnHand = 0,
                AvgCost = 0
            };
            _db.Inventory.Add(inventory);
        }

        var newQuantity = inventory.QuantityOnHand + signedQty;
        if (newQuantity < 0)
            throw new ConflictException(
                $"Insufficient stock: on hand {inventory.QuantityOnHand}, requested {quantity}.");

        // Moving-average cost is only affected by inbound movements with a known cost.
        if (isInbound && unitCost is { } cost && quantity > 0)
        {
            var totalValue = inventory.QuantityOnHand * inventory.AvgCost + quantity * cost;
            inventory.AvgCost = newQuantity == 0 ? 0 : totalValue / newQuantity;
        }

        inventory.QuantityOnHand = newQuantity;
        inventory.UpdatedAt = _clock.UtcNow;

        _db.StockMovements.Add(new StockMovement
        {
            TenantId = _currentUser.TenantId ?? Guid.Empty,
            StoreId = storeId,
            VariantId = variantId,
            MovementType = movementType,
            Quantity = signedQty,
            UnitCost = unitCost ?? (isInbound ? null : inventory.AvgCost),
            BalanceAfter = newQuantity,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Notes = notes,
            CreatedAt = _clock.UtcNow,
            CreatedBy = _currentUser.UserId
        });
    }
}
