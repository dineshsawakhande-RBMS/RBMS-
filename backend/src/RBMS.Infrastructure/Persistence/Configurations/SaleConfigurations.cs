using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Persistence.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> e)
    {
        e.ToTable("sales");
        e.Property(x => x.InvoiceNumber).HasMaxLength(30).IsRequired();
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(20);
        foreach (var p in new[]
                 {
                     nameof(Sale.Subtotal), nameof(Sale.Discount), nameof(Sale.TaxableAmount),
                     nameof(Sale.Cgst), nameof(Sale.Sgst), nameof(Sale.Igst), nameof(Sale.RoundOff),
                     nameof(Sale.GrandTotal), nameof(Sale.AmountPaid), nameof(Sale.ChangeDue)
                 })
            e.Property(p).HasPrecision(14, 2);
        e.HasMany(x => x.Items).WithOne(i => i.Sale).HasForeignKey(i => i.SaleId);
        e.HasMany(x => x.Payments).WithOne(p => p.Sale).HasForeignKey(p => p.SaleId);
        e.HasIndex(x => new { x.TenantId, x.InvoiceNumber }).IsUnique();
        e.HasIndex(x => new { x.StoreId, x.InvoiceDate });
    }
}

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> e)
    {
        e.ToTable("sale_items");
        e.Property(x => x.Quantity).HasPrecision(14, 3);
        foreach (var p in new[]
                 {
                     nameof(SaleItem.UnitPrice), nameof(SaleItem.UnitCost), nameof(SaleItem.Discount),
                     nameof(SaleItem.TaxableAmount), nameof(SaleItem.TaxAmount), nameof(SaleItem.LineTotal)
                 })
            e.Property(p).HasPrecision(14, 2);
        e.Property(x => x.GstRate).HasPrecision(5, 2);
        e.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId);
    }
}

public class SalePaymentConfiguration : IEntityTypeConfiguration<SalePayment>
{
    public void Configure(EntityTypeBuilder<SalePayment> e)
    {
        e.ToTable("sale_payments");
        e.Property(x => x.Method).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.Amount).HasPrecision(14, 2);
    }
}

public class SaleReturnConfiguration : IEntityTypeConfiguration<SaleReturn>
{
    public void Configure(EntityTypeBuilder<SaleReturn> e)
    {
        e.ToTable("sale_returns");
        e.Property(x => x.ReturnNumber).HasMaxLength(30).IsRequired();
        e.Property(x => x.RefundMethod).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.TotalAmount).HasPrecision(14, 2);
        e.HasOne(x => x.Sale).WithMany().HasForeignKey(x => x.SaleId);
        e.HasMany(x => x.Items).WithOne(i => i.SaleReturn).HasForeignKey(i => i.SaleReturnId);
        e.HasIndex(x => new { x.TenantId, x.ReturnNumber }).IsUnique();
    }
}

public class SaleReturnItemConfiguration : IEntityTypeConfiguration<SaleReturnItem>
{
    public void Configure(EntityTypeBuilder<SaleReturnItem> e)
    {
        e.ToTable("sale_return_items");
        e.Property(x => x.Quantity).HasPrecision(14, 3);
        e.Property(x => x.UnitPrice).HasPrecision(14, 2);
        e.Property(x => x.LineTotal).HasPrecision(14, 2);
    }
}
