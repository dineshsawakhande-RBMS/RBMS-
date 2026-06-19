using RBMS.Domain.Common;
using RBMS.Domain.Enums;

namespace RBMS.Domain.Entities;

public class Customer : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string Mobile { get; set; } = null!;
    public string? Email { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public DateOnly? Birthday { get; set; }
    public DateOnly? Anniversary { get; set; }
    public int LoyaltyPoints { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LoyaltyTransaction : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public LoyaltyTxnType TxnType { get; set; }
    public int Points { get; set; }                 // signed: +earn / -redeem
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
}
