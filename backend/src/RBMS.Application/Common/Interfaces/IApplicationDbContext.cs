using Microsoft.EntityFrameworkCore;
using RBMS.Domain.Entities;

namespace RBMS.Application.Common.Interfaces;

/// <summary>
/// Read/write access to the persistence model for the query side and handlers that need
/// composable LINQ. The command side prefers <see cref="IUnitOfWork"/> + repositories.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Store> Stores { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Category> Categories { get; }
    DbSet<Brand> Brands { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<Inventory> Inventory { get; }
    DbSet<StockMovement> StockMovements { get; }
    DbSet<StockAdjustment> StockAdjustments { get; }
    DbSet<StockAdjustmentLine> StockAdjustmentLines { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<SupplierLedgerEntry> SupplierLedger { get; }
    DbSet<Purchase> Purchases { get; }
    DbSet<PurchaseItem> PurchaseItems { get; }
    DbSet<PurchaseReturn> PurchaseReturns { get; }
    DbSet<PurchaseReturnItem> PurchaseReturnItems { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleItem> SaleItems { get; }
    DbSet<SalePayment> SalePayments { get; }
    DbSet<SaleReturn> SaleReturns { get; }
    DbSet<SaleReturnItem> SaleReturnItems { get; }
    DbSet<LoginHistory> LoginHistory { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
