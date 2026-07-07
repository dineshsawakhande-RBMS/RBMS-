using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Security;
using RBMS.Domain.Entities;
using RBMS.Domain.Enums;

namespace RBMS.Infrastructure.Persistence;

/// <summary>
/// Idempotent development seed data: a tenant, a store, the full permission catalogue,
/// roles, two login users, and a handful of products with opening stock — so the app is
/// usable by hand immediately. Runs only in Development (wired in Program.cs).
/// </summary>
public static class DbSeeder
{
    public static readonly Guid TenantId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid StoreId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    public const string OwnerUsername = "owner";
    public const string CashierUsername = "cashier";
    public const string DefaultPassword = "Password123!";

    public static async Task SeedAsync(
        ApplicationDbContext db, IPasswordHasher hasher, ILogger logger, CancellationToken ct = default)
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == TenantId, ct))
        {
            logger.LogInformation("Seed data already present — skipping.");
            return;
        }

        logger.LogInformation("Seeding development data...");
        var now = DateTimeOffset.UtcNow;

        // --- permissions (catalogue must match Permissions constants) ---
        var allCodes = new[]
        {
            Permissions.DashboardView, Permissions.StoreView, Permissions.StoreManage,
            Permissions.ProductView, Permissions.ProductManage,
            Permissions.InventoryView, Permissions.InventoryAdjust, Permissions.PurchaseView,
            Permissions.PurchaseManage, Permissions.SaleCreate, Permissions.SaleRefund,
            Permissions.CustomerManage, Permissions.SupplierManage, Permissions.EmployeeManage,
            Permissions.PayrollManage, Permissions.ExpenseManage, Permissions.DocumentView,
            Permissions.DocumentManage, Permissions.AttendanceView, Permissions.AttendanceManage,
            Permissions.LeaveApprove, Permissions.ReportView, Permissions.UserManage,
            Permissions.AuditView
        };
        var permissions = allCodes.ToDictionary(c => c, c => new Permission { Code = c });
        db.Permissions.AddRange(permissions.Values);

        // --- tenant + store ---
        db.Tenants.Add(new Tenant
        {
            Id = TenantId, Name = "Western Wear Co", LegalName = "Western Wear Pvt Ltd",
            Currency = "INR", Country = "India", CreatedAt = now
        });
        db.Stores.Add(new Store
        {
            Id = StoreId, TenantId = TenantId, Code = "MAIN", Name = "Main Store",
            City = "Pune", State = "Maharashtra", CreatedAt = now
        });

        // --- roles ---
        var owner = new Role { TenantId = TenantId, Name = RoleNames.Owner, Description = "Full access", CreatedAt = now };
        foreach (var p in permissions.Values)
            owner.RolePermissions.Add(new RolePermission { Role = owner, Permission = p });

        var cashier = new Role { TenantId = TenantId, Name = RoleNames.Cashier, Description = "POS & sales", CreatedAt = now };
        foreach (var code in new[]
                 {
                     Permissions.DashboardView, Permissions.ProductView, Permissions.InventoryView,
                     Permissions.SaleCreate, Permissions.SaleRefund, Permissions.CustomerManage
                 })
            cashier.RolePermissions.Add(new RolePermission { Role = cashier, Permission = permissions[code] });

        db.Roles.AddRange(owner, cashier);

        // --- users ---
        var ownerUser = NewUser(OwnerUsername, "owner@westernwear.test", "Shop Owner", hasher, now);
        ownerUser.UserRoles.Add(new UserRole { User = ownerUser, Role = owner });
        var cashierUser = NewUser(CashierUsername, "cashier@westernwear.test", "Front Desk Cashier", hasher, now);
        cashierUser.UserRoles.Add(new UserRole { User = cashierUser, Role = cashier });
        db.Users.AddRange(ownerUser, cashierUser);

        // --- catalogue ---
        var dresses = new Category { TenantId = TenantId, Name = "Dresses", CreatedAt = now };
        var tops = new Category { TenantId = TenantId, Name = "Tops", CreatedAt = now };
        var brandA = new Brand { TenantId = TenantId, Name = "Aurelia", CreatedAt = now };
        var brandB = new Brand { TenantId = TenantId, Name = "Biba", CreatedAt = now };
        db.Categories.AddRange(dresses, tops);
        db.Brands.AddRange(brandA, brandB);

        var p1 = NewProduct("Floral Maxi Dress", dresses, brandA, 12m, now,
            ("FMD-S-RED", "S", "Red", 600, 1199), ("FMD-M-RED", "M", "Red", 600, 1199), ("FMD-L-BLU", "L", "Blue", 620, 1249));
        var p2 = NewProduct("A-Line Midi Dress", dresses, brandB, 12m, now,
            ("ALM-M-BLK", "M", "Black", 750, 1499), ("ALM-L-GRN", "L", "Green", 770, 1549));
        var p3 = NewProduct("Cotton Crop Top", tops, brandA, 5m, now,
            ("CCT-S-WHT", "S", "White", 250, 599), ("CCT-M-PNK", "M", "Pink", 250, 599));
        db.Products.AddRange(p1, p2, p3);

        // --- opening stock (legitimate direct opening balance: movement + matching projection) ---
        foreach (var v in p1.Variants.Concat(p2.Variants).Concat(p3.Variants))
            AddOpeningStock(db, v, quantity: 25, now);

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Seed complete. Login: '{Owner}' / '{Cashier}' (password '{Pwd}'). Tenant {TenantId}, Store {StoreId}.",
            OwnerUsername, CashierUsername, DefaultPassword, TenantId, StoreId);
    }

    private static User NewUser(string username, string email, string fullName, IPasswordHasher hasher, DateTimeOffset now)
        => new()
        {
            TenantId = TenantId, StoreId = StoreId, Username = username, Email = email,
            FullName = fullName, PasswordHash = hasher.Hash(DefaultPassword), IsActive = true,
            EmailConfirmed = true, CreatedAt = now
        };

    private static Product NewProduct(
        string name, Category category, Brand brand, decimal gst, DateTimeOffset now,
        params (string Sku, string Size, string Color, decimal Cost, decimal Sell)[] variants)
    {
        var product = new Product
        {
            TenantId = TenantId, Name = name, Category = category, Brand = brand,
            GstRate = gst, IsActive = true, CreatedAt = now
        };
        foreach (var v in variants)
            product.Variants.Add(new ProductVariant
            {
                TenantId = TenantId, Sku = v.Sku, Barcode = v.Sku, Size = v.Size, Color = v.Color,
                PurchasePrice = v.Cost, SellingPrice = v.Sell, Mrp = v.Sell, ReorderLevel = 5,
                IsActive = true, CreatedAt = now
            });
        return product;
    }

    private static void AddOpeningStock(ApplicationDbContext db, ProductVariant variant, decimal quantity, DateTimeOffset now)
    {
        db.Inventory.Add(new Inventory
        {
            TenantId = TenantId, StoreId = StoreId, Variant = variant,
            QuantityOnHand = quantity, AvgCost = variant.PurchasePrice, UpdatedAt = now
        });
        db.StockMovements.Add(new StockMovement
        {
            TenantId = TenantId, StoreId = StoreId, Variant = variant,
            MovementType = StockMovementType.OpeningStock, Quantity = quantity,
            UnitCost = variant.PurchasePrice, BalanceAfter = quantity,
            ReferenceType = "OpeningStock", Notes = "Seeded opening balance", CreatedAt = now
        });
    }
}
