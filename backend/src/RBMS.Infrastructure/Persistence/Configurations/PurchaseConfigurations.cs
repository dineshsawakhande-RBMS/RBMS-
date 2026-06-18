using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> e)
    {
        e.ToTable("suppliers");
        e.Property(x => x.Code).HasMaxLength(20).IsRequired();
        e.Property(x => x.Name).HasMaxLength(300).IsRequired();
        e.Property(x => x.Gstin).HasMaxLength(15);
        e.Property(x => x.OpeningBalance).HasPrecision(14, 2);
        e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}

public class SupplierLedgerEntryConfiguration : IEntityTypeConfiguration<SupplierLedgerEntry>
{
    public void Configure(EntityTypeBuilder<SupplierLedgerEntry> e)
    {
        e.ToTable("supplier_ledger");
        e.Property(x => x.Debit).HasPrecision(14, 2);
        e.Property(x => x.Credit).HasPrecision(14, 2);
        e.Property(x => x.ReferenceType).HasMaxLength(30).IsRequired();
        e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId);
        e.HasIndex(x => new { x.SupplierId, x.EntryDate });
    }
}

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> e)
    {
        e.ToTable("purchases");
        e.Property(x => x.InvoiceNumber).HasMaxLength(50);
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(20);
        foreach (var p in new[] { nameof(Purchase.Subtotal), nameof(Purchase.Discount),
                     nameof(Purchase.TaxTotal), nameof(Purchase.GrandTotal), nameof(Purchase.AmountPaid) })
            e.Property(p).HasPrecision(14, 2);
        e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId);
        e.HasMany(x => x.Items).WithOne(i => i.Purchase).HasForeignKey(i => i.PurchaseId);
        e.HasIndex(x => new { x.SupplierId, x.InvoiceDate });
    }
}

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> e)
    {
        e.ToTable("purchase_items");
        e.Property(x => x.Quantity).HasPrecision(14, 3);
        e.Property(x => x.UnitCost).HasPrecision(14, 2);
        e.Property(x => x.GstRate).HasPrecision(5, 2);
        e.Property(x => x.LineTotal).HasPrecision(14, 2);
        e.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId);
    }
}

public class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturn>
{
    public void Configure(EntityTypeBuilder<PurchaseReturn> e)
    {
        e.ToTable("purchase_returns");
        e.Property(x => x.ReturnNumber).HasMaxLength(30).IsRequired();
        e.Property(x => x.TotalAmount).HasPrecision(14, 2);
        e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId);
        e.HasMany(x => x.Items).WithOne(i => i.PurchaseReturn).HasForeignKey(i => i.PurchaseReturnId);
        e.HasIndex(x => new { x.TenantId, x.ReturnNumber }).IsUnique();
    }
}

public class PurchaseReturnItemConfiguration : IEntityTypeConfiguration<PurchaseReturnItem>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnItem> e)
    {
        e.ToTable("purchase_return_items");
        e.Property(x => x.Quantity).HasPrecision(14, 3);
        e.Property(x => x.UnitCost).HasPrecision(14, 2);
        e.Property(x => x.LineTotal).HasPrecision(14, 2);
    }
}
