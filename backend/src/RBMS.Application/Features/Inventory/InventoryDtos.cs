using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Inventory;

public record StockLevelDto(
    Guid VariantId,
    string Sku,
    string ProductName,
    string? Size,
    string? Color,
    decimal QuantityOnHand,
    decimal ReorderLevel,
    decimal AvgCost,
    decimal StockValue,
    bool IsLow);

public record StockMovementDto(
    Guid Id,
    StockMovementType MovementType,
    decimal Quantity,
    decimal BalanceAfter,
    decimal? UnitCost,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Notes,
    DateTimeOffset CreatedAt);

/// <summary>One line of a manual stock adjustment. Delta is signed (+ increases, − decreases).</summary>
public record AdjustStockLineInput(Guid VariantId, decimal QuantityDelta, decimal? UnitCost);
