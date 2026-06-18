using RBMS.Domain.Enums;

namespace RBMS.Application.Features.Sales;

public record SaleItemInput(Guid VariantId, decimal Quantity, decimal UnitPrice, decimal Discount, decimal GstRate);

public record SalePaymentInput(PaymentMethod Method, decimal Amount, string? Reference);

public record SaleItemDto(
    Guid VariantId, string Sku, string ProductName, decimal Quantity,
    decimal UnitPrice, decimal Discount, decimal GstRate, decimal TaxAmount, decimal LineTotal);

public record SalePaymentDto(PaymentMethod Method, decimal Amount, string? Reference);

public record SaleDto(
    Guid Id, string InvoiceNumber, DateTimeOffset InvoiceDate, Guid StoreId, Guid? CustomerId,
    SaleStatus Status, decimal Subtotal, decimal Discount, decimal Cgst, decimal Sgst,
    decimal GrandTotal, decimal AmountPaid, decimal ChangeDue, PaymentStatus PaymentStatus,
    IReadOnlyList<SaleItemDto> Items, IReadOnlyList<SalePaymentDto> Payments);

public record SaleListItemDto(
    Guid Id, string InvoiceNumber, DateTimeOffset InvoiceDate, decimal GrandTotal,
    SaleStatus Status, PaymentStatus PaymentStatus);

public record SaleReturnItemInput(Guid VariantId, decimal Quantity, decimal UnitPrice);
