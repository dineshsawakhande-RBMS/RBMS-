using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> e)
    {
        e.ToTable("tenants");
        e.Property(x => x.Name).HasMaxLength(300).IsRequired();
        e.Property(x => x.Gstin).HasMaxLength(15);
        e.Property(x => x.Pan).HasMaxLength(10);
        e.Property(x => x.Currency).HasMaxLength(3);
    }
}

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> e)
    {
        e.ToTable("stores");
        e.Property(x => x.Code).HasMaxLength(20).IsRequired();
        e.Property(x => x.Name).HasMaxLength(300).IsRequired();
        e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        e.HasOne(x => x.Tenant).WithMany(t => t.Stores).HasForeignKey(x => x.TenantId);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.ToTable("users");
        e.Property(x => x.Username).HasMaxLength(100).IsRequired();
        e.Property(x => x.Email).HasMaxLength(256).IsRequired();
        e.Property(x => x.FullName).HasMaxLength(300).IsRequired();
        e.Property(x => x.PasswordHash).IsRequired();
        e.HasIndex(x => new { x.TenantId, x.Username }).IsUnique();
        e.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> e)
    {
        e.ToTable("roles");
        e.Property(x => x.Name).HasMaxLength(50).IsRequired();
        e.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> e)
    {
        e.ToTable("permissions");
        e.Property(x => x.Code).HasMaxLength(100).IsRequired();
        e.HasIndex(x => x.Code).IsUnique();
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> e)
    {
        e.ToTable("role_permissions");
        e.HasKey(x => new { x.RoleId, x.PermissionId });
        e.HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId);
        e.HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionId);
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> e)
    {
        e.ToTable("user_roles");
        e.HasKey(x => new { x.UserId, x.RoleId });
        e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId);
        e.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> e)
    {
        e.ToTable("refresh_tokens");
        e.Property(x => x.TokenHash).IsRequired();
        e.HasIndex(x => x.TokenHash);
        e.HasIndex(x => x.UserId);
        e.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId);
    }
}

public class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
{
    public void Configure(EntityTypeBuilder<LoginHistory> e)
    {
        e.ToTable("login_history");
        e.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> e)
    {
        e.ToTable("audit_logs");
        e.Property(x => x.OldValues).HasColumnType("jsonb");
        e.Property(x => x.NewValues).HasColumnType("jsonb");
        e.Property(x => x.Action).HasConversion<string>();
        e.HasIndex(x => new { x.EntityName, x.EntityId });
        e.HasIndex(x => new { x.TenantId, x.CreatedAt });
    }
}
