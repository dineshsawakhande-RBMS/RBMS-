using RBMS.Domain.Common;
using RBMS.Domain.Enums;

namespace RBMS.Domain.Entities;

/// <summary>A POS sale / invoice. Each line moves stock OUT through the ledger.</summary>
public class Sale : AuditableEntity
{
    public Guid StoreId { get; set; }
    public Guid? CustomerId { get; set; }      // optional (walk-in); Customer module later
    public Guid? CashierId { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public DateTimeOffset InvoiceDate { get; set; }
    public SaleStatus Status { get; set; } = SaleStatus.Completed;

    public decimal Subtotal { get; set; }       // Σ line taxable amounts (after line discounts)
    public decimal Discount { get; set; }        // invoice-level discount amount
    public decimal TaxableAmount { get; set; }
    public decimal Cgst { get; set; }
    public decimal Sgst { get; set; }
    public decimal Igst { get; set; }
    public decimal RoundOff { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal ChangeDue { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Paid;
    public string? Notes { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<SalePayment> Payments { get; set; } = new List<SalePayment>();
}

public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }
    public Guid VariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }      // selling price at sale time
    public decimal UnitCost { get; set; }        // COGS snapshot (moving-avg cost) for profit
    public decimal Discount { get; set; }
    public decimal GstRate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

    public Sale Sale { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}

public class SalePayment : BaseEntity
{
    public Guid SaleId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }

    public Sale Sale { get; set; } = null!;
}

public class SaleReturn : AuditableEntity
{
    public Guid StoreId { get; set; }
    public Guid SaleId { get; set; }
    public string ReturnNumber { get; set; } = null!;
    public DateTimeOffset ReturnDate { get; set; }
    public string? Reason { get; set; }
    public PaymentMethod? RefundMethod { get; set; }
    public decimal TotalAmount { get; set; }

    public Sale Sale { get; set; } = null!;
    public ICollection<SaleReturnItem> Items { get; set; } = new List<SaleReturnItem>();
}

public class SaleReturnItem : BaseEntity
{
    public Guid SaleReturnId { get; set; }
    public Guid VariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public SaleReturn SaleReturn { get; set; } = null!;
}
