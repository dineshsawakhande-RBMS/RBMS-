using RBMS.Domain.Common;
using RBMS.Domain.Enums;

namespace RBMS.Domain.Entities;

/// <summary>
/// Projected current stock for a variant at a store. Kept in sync transactionally whenever
/// a <see cref="StockMovement"/> is written — NEVER set directly by business code.
/// </summary>
public class Inventory : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid StoreId { get; set; }
    public Guid VariantId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal AvgCost { get; set; }          // moving-average cost
    public DateTimeOffset UpdatedAt { get; set; }

    public ProductVariant Variant { get; set; } = null!;
}

/// <summary>
/// The append-only source of truth for every stock change. Rows are never updated or
/// deleted. <see cref="BalanceAfter"/> snapshots on-hand quantity right after the movement.
/// </summary>
public class StockMovement : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid StoreId { get; set; }
    public Guid VariantId { get; set; }
    public StockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }          // signed: +in / -out
    public decimal? UnitCost { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? ReferenceType { get; set; }     // 'Purchase','Sale','Adjustment','Return','Transfer'
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }

    public ProductVariant Variant { get; set; } = null!;
}

/// <summary>Header for a manual stock adjustment / damaged-stock entry. Lines emit movements.</summary>
public class StockAdjustment : AuditableEntity
{
    public Guid StoreId { get; set; }
    public string AdjustmentNo { get; set; } = null!;
    public string Reason { get; set; } = null!;     // 'Damaged','Count correction','Theft', etc.
    public DateOnly AdjustmentDate { get; set; }

    public ICollection<StockAdjustmentLine> Lines { get; set; } = new List<StockAdjustmentLine>();
}

public class StockAdjustmentLine : BaseEntity
{
    public Guid AdjustmentId { get; set; }
    public Guid VariantId { get; set; }
    public decimal QuantityDelta { get; set; }      // signed
    public decimal? UnitCost { get; set; }

    public StockAdjustment Adjustment { get; set; } = null!;
}
