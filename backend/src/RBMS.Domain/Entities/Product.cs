using RBMS.Domain.Common;

namespace RBMS.Domain.Entities;

public class Category : AuditableEntity
{
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}

public class Brand : AuditableEntity
{
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

public class Product : AuditableEntity
{
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? HsnCode { get; set; }
    public decimal GstRate { get; set; }
    public bool IsActive { get; set; } = true;

    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}

public class ProductVariant : AuditableEntity
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = null!;
    public string? Barcode { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal? Mrp { get; set; }
    public decimal ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;

    public Product Product { get; set; } = null!;
}

public class ProductImage : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string S3Key { get; set; } = null!;
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Product Product { get; set; } = null!;
}
