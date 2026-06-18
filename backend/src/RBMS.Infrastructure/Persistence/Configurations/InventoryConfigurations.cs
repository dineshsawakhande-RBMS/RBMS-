using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Persistence.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> e)
    {
        e.ToTable("inventory", t =>
            t.HasCheckConstraint("ck_inventory_nonneg", "quantity_on_hand >= 0"));
        e.Property(x => x.QuantityOnHand).HasPrecision(14, 3);
        e.Property(x => x.AvgCost).HasPrecision(14, 2);
        e.HasIndex(x => new { x.StoreId, x.VariantId }).IsUnique();
        e.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> e)
    {
        e.ToTable("stock_movements");
        e.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.Quantity).HasPrecision(14, 3);
        e.Property(x => x.UnitCost).HasPrecision(14, 2);
        e.Property(x => x.BalanceAfter).HasPrecision(14, 3);
        e.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId);
        e.HasIndex(x => new { x.VariantId, x.CreatedAt });
        e.HasIndex(x => new { x.ReferenceType, x.ReferenceId });
    }
}

public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> e)
    {
        e.ToTable("stock_adjustments");
        e.Property(x => x.AdjustmentNo).HasMaxLength(30).IsRequired();
        e.Property(x => x.Reason).IsRequired();
        e.HasIndex(x => new { x.TenantId, x.AdjustmentNo }).IsUnique();
        e.HasMany(x => x.Lines).WithOne(l => l.Adjustment).HasForeignKey(l => l.AdjustmentId);
    }
}

public class StockAdjustmentLineConfiguration : IEntityTypeConfiguration<StockAdjustmentLine>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentLine> e)
    {
        e.ToTable("stock_adjustment_lines");
        e.Property(x => x.QuantityDelta).HasPrecision(14, 3);
        e.Property(x => x.UnitCost).HasPrecision(14, 2);
    }
}
