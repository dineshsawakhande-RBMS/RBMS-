namespace RBMS.Application.Features.Products;

public record ProductVariantDto(
    Guid Id, string Sku, string? Barcode, string? Size, string? Color,
    decimal PurchasePrice, decimal SellingPrice, decimal? Mrp, decimal ReorderLevel, bool IsActive);

public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    string? HsnCode,
    decimal GstRate,
    Guid? CategoryId,
    string? CategoryName,
    Guid? BrandId,
    string? BrandName,
    bool IsActive,
    IReadOnlyList<ProductVariantDto> Variants);

public record ProductListItemDto(
    Guid Id, string Name, string? BrandName, string? CategoryName,
    decimal GstRate, int VariantCount, bool IsActive);

// ---- write models ----
public record CreateVariantInput(
    string Sku, string? Barcode, string? Size, string? Color,
    decimal PurchasePrice, decimal SellingPrice, decimal? Mrp, decimal ReorderLevel);
