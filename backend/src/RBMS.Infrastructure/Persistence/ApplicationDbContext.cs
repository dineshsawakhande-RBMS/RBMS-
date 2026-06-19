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
    public DbSet<Inventory> Inventory => Set<Inventory>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentLine> StockAdjustmentLines => Set<StockAdjustmentLine>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierLedgerEntry> SupplierLedger => Set<SupplierLedgerEntry>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<PayrollLine> PayrollLines => Set<PayrollLine>();
    public DbSet<SalaryAdvance> SalaryAdvances => Set<SalaryAdvance>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();
    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
    public DbSet<SaleReturnItem> SaleReturnItems => Set<SaleReturnItem>();
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
        b.Entity<Inventory>().HasQueryFilter(e => e.TenantId == TenantId);
        b.Entity<StockMovement>().HasQueryFilter(e => e.TenantId == TenantId);
        b.Entity<StockAdjustment>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
        b.Entity<Supplier>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
        b.Entity<SupplierLedgerEntry>().HasQueryFilter(e => e.TenantId == TenantId);
        b.Entity<Purchase>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
        b.Entity<PurchaseReturn>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
        b.Entity<Sale>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
        b.Entity<SaleReturn>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
        b.Entity<Customer>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
        b.Entity<LoyaltyTransaction>().HasQueryFilter(e => e.TenantId == TenantId);
        b.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
        b.Entity<Payroll>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
        b.Entity<SalaryAdvance>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == TenantId);
    }
}
