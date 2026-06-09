using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Interfaces;
using RBMS.Domain.Common;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUser _currentUser;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUser currentUser)
        : base(options)
        => _currentUser = currentUser;

    /// <summary>Current tenant, used by the global query filters. Empty => unscoped (e.g. login).</summary>
    public Guid TenantId => _currentUser.TenantId ?? Guid.Empty;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<LoginHistory> LoginHistory => Set<LoginHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Apply all IEntityTypeConfiguration<> in this assembly.
        b.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Map AuditableEntity.Version to PostgreSQL's system column xmin (optimistic concurrency).
        foreach (var et in b.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(et.ClrType))
            {
                b.Entity(et.ClrType).Property("Version")
                    .HasColumnName("xmin").HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
            }
        }

        // Global query filters: tenant scoping + soft delete. Referencing the context's
        // TenantId property lets EF evaluate it per-query against the executing context.
        b.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Store>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
        b.Entity<User>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
        b.Entity<Category>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
        b.Entity<Brand>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
        b.Entity<Product>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
        b.Entity<ProductVariant>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
        b.Entity<ProductImage>().HasQueryFilter(e => e.TenantId == TenantId);
    }
}
