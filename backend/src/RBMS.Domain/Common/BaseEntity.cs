namespace RBMS.Domain.Common;

/// <summary>Root of every persisted entity. UUID surrogate key generated client-side.</summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

/// <summary>Marks an entity as belonging to a tenant. Drives the global tenant query filter.</summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}

/// <summary>Marks an entity as soft-deletable. Drives the global "not deleted" query filter.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}

/// <summary>Marks an entity whose audit columns are stamped by the SaveChanges interceptor.</summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
}

/// <summary>
/// Base for all business entities: tenant-scoped, auditable, soft-deletable, with an
/// optimistic-concurrency token mapped to PostgreSQL's system column <c>xmin</c>.
/// </summary>
public abstract class AuditableEntity : BaseEntity, ITenantEntity, IAuditableEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    /// <summary>Maps to xmin; EF treats it as a concurrency token. Do not set manually.</summary>
    public uint Version { get; set; }
}
