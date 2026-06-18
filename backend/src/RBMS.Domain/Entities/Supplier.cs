using RBMS.Domain.Common;

namespace RBMS.Domain.Entities;

public class Supplier : AuditableEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Gstin { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public int PaymentTermsDays { get; set; }
    public decimal OpeningBalance { get; set; }   // positive => we owe the supplier
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Supplier account ledger. Outstanding balance = opening balance + Σ(credit − debit).
/// A purchase credits (we owe more); a payment or return debits (we owe less).
/// </summary>
public class SupplierLedgerEntry : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid SupplierId { get; set; }
    public DateOnly EntryDate { get; set; }
    public string ReferenceType { get; set; } = null!;   // 'Purchase','PurchaseReturn','Payment','Opening'
    public Guid? ReferenceId { get; set; }
    public decimal Debit { get; set; }                    // reduces what we owe
    public decimal Credit { get; set; }                   // increases what we owe
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }

    public Supplier Supplier { get; set; } = null!;
}
