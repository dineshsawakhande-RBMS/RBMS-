using RBMS.Domain.Common;

namespace RBMS.Domain.Entities;

public class Tenant : BaseEntity, IAuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = null!;
    public string? LegalName { get; set; }
    public string? Gstin { get; set; }
    public string? Pan { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Country { get; set; } = "India";
    public string Currency { get; set; } = "INR";
    public string Timezone { get; set; } = "Asia/Kolkata";
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public ICollection<Store> Stores { get; set; } = new List<Store>();
}
