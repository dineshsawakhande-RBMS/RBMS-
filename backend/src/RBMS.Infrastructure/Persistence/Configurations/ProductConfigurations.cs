using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> e)
    {
        e.ToTable("categories");
        e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        e.HasOne(x => x.Parent).WithMany(c => c.Children).HasForeignKey(x => x.ParentId);
        e.HasIndex(x => new { x.TenantId, x.ParentId, x.Name }).IsUnique();
    }
}

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> e)
    {
        e.ToTable("brands");
        e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        e.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> e)
    {
        e.ToTable("products");
        e.Property(x => x.Name).HasMaxLength(300).IsRequired();
        e.Property(x => x.HsnCode).HasMaxLength(10);
        e.Property(x => x.GstRate).HasPrecision(5, 2);
        e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId);
        e.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId);
        e.HasMany(x => x.Variants).WithOne(v => v.Product).HasForeignKey(v => v.ProductId);
        e.HasMany(x => x.Images).WithOne(i => i.Product).HasForeignKey(i => i.ProductId);
        e.HasIndex(x => new { x.TenantId, x.Name });
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> e)
    {
        e.ToTable("product_variants");
        e.Property(x => x.Sku).HasMaxLength(50).IsRequired();
        e.Property(x => x.Barcode).HasMaxLength(64);
        e.Property(x => x.Size).HasMaxLength(30);
        e.Property(x => x.Color).HasMaxLength(40);
        e.Property(x => x.PurchasePrice).HasPrecision(14, 2);
        e.Property(x => x.SellingPrice).HasPrecision(14, 2);
        e.Property(x => x.Mrp).HasPrecision(14, 2);
        e.Property(x => x.ReorderLevel).HasPrecision(14, 3);
        e.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique();
        e.HasIndex(x => new { x.TenantId, x.Barcode })
            .IsUnique()
            .HasFilter("barcode IS NOT NULL");
    }
}

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> e)
    {
        e.ToTable("product_images");
        e.Property(x => x.S3Key).IsRequired();
    }
}
