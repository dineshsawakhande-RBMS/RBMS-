using RBMS.Domain.Common;
using RBMS.Domain.Enums;

namespace RBMS.Domain.Entities;

/// <summary>Goods receipt / purchase entry — this is what moves stock IN (via the ledger).</summary>
public class Purchase : AuditableEntity
{
    public Guid StoreId { get; set; }
    public Guid SupplierId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public Guid? InvoiceDocumentId { get; set; }     // S3-backed doc (Document module)
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Confirmed;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? Notes { get; set; }

    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}

public class PurchaseItem : BaseEntity
{
    public Guid PurchaseId { get; set; }
    public Guid VariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal GstRate { get; set; }
    public decimal LineTotal { get; set; }

    public Purchase Purchase { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}

public class PurchaseReturn : AuditableEntity
{
    public Guid StoreId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid? PurchaseId { get; set; }
    public string ReturnNumber { get; set; } = null!;
    public DateOnly ReturnDate { get; set; }
    public string? Reason { get; set; }
    public decimal TotalAmount { get; set; }

    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseReturnItem> Items { get; set; } = new List<PurchaseReturnItem>();
}

public class PurchaseReturnItem : BaseEntity
{
    public Guid PurchaseReturnId { get; set; }
    public Guid VariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }

    public PurchaseReturn PurchaseReturn { get; set; } = null!;
}
