using RBMS.Domain.Common;

namespace RBMS.Domain.Entities;

/// <summary>Well-known role names. Roles are persisted per-tenant (plus system roles).</summary>
public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Owner = "Owner";
    public const string Manager = "Manager";
    public const string Cashier = "Cashier";
    public const string InventoryStaff = "InventoryStaff";
    public const string Accountant = "Accountant";

    public static readonly string[] All =
        { SuperAdmin, Owner, Manager, Cashier, InventoryStaff, Accountant };
}

public class Role : BaseEntity
{
    public Guid? TenantId { get; set; }   // null => system role
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class Permission : BaseEntity
{
    public string Code { get; set; } = null!;     // e.g. "product.manage"
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}

public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
