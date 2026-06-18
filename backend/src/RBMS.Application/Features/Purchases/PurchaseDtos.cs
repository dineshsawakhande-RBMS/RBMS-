using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Purchases;

public record PurchaseItemInput(Guid VariantId, decimal Quantity, decimal UnitCost, decimal GstRate);

public record PurchaseItemDto(
    Guid VariantId, string Sku, string ProductName, decimal Quantity,
    decimal UnitCost, decimal GstRate, decimal LineTotal);

public record PurchaseDto(
    Guid Id, Guid SupplierId, string SupplierName, Guid StoreId, string? InvoiceNumber,
    DateOnly InvoiceDate, PurchaseStatus Status, decimal Subtotal, decimal Discount,
    decimal TaxTotal, decimal GrandTotal, decimal AmountPaid, PaymentStatus PaymentStatus,
    IReadOnlyList<PurchaseItemDto> Items);

public record PurchaseListItemDto(
    Guid Id, string SupplierName, string? InvoiceNumber, DateOnly InvoiceDate,
    decimal GrandTotal, decimal AmountPaid, PaymentStatus PaymentStatus);

public record PurchaseReturnItemInput(Guid VariantId, decimal Quantity, decimal UnitCost);
