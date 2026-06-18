using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBMS.Domain.Entities;
using RBMS.Infrastructure.Persistence;
using RBMS.Infrastructure.Persistence.Interceptors;
using RBMS.Infrastructure.Services;

namespace RBMS.IntegrationTests;

/// <summary>
/// Boots the real API pipeline (auth, authorization, MediatR, interceptors) but swaps the
/// PostgreSQL context for an in-memory store so tests run anywhere without Docker.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Unique DB name per factory instance so parallel test classes don't share state.
    private readonly string _dbName = $"rbms-tests-{Guid.NewGuid()}";

    public static class Seed
    {
        public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid StoreId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid VariantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public const string Username = "owner";
        public const string Password = "Password123!";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["Jwt:SigningKey"] = "integration_test_signing_key_at_least_32_chars_long",
                ["Jwt:Issuer"] = "rbms-api",
                ["Jwt:Audience"] = "rbms-api"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the Npgsql-backed context AND its EF9 options-configuration registrations
            // (these accumulate and would otherwise re-apply UseNpgsql alongside InMemory).
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType == typeof(ApplicationDbContext)
                         || d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration"))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                options.AddInterceptors(
                    sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
            });
        });
    }

    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == Seed.Username))
            return;

        var hasher = new PasswordHasherService();

        var tenant = new Tenant { Id = Seed.TenantId, Name = "Western Wear Co" };
        var store = new Store { Id = Seed.StoreId, TenantId = Seed.TenantId, Code = "MAIN", Name = "Main Store" };

        var role = new Role { TenantId = Seed.TenantId, Name = RoleNames.Owner };
        foreach (var code in new[]
                 {
                     "product.view", "inventory.view", "inventory.adjust",
                     "supplier.manage", "purchase.view", "purchase.manage"
                 })
        {
            var perm = new Permission { Code = code };
            role.RolePermissions.Add(new RolePermission { Role = role, Permission = perm });
            db.Permissions.Add(perm);
        }

        var product = new Product { TenantId = Seed.TenantId, Name = "Floral Maxi Dress", GstRate = 12m };
        var variant = new ProductVariant
        {
            Id = Seed.VariantId,
            TenantId = Seed.TenantId,
            Product = product,
            Sku = "FMD-M-RED",
            Size = "M",
            Color = "Red",
            PurchasePrice = 500,
            SellingPrice = 999,
            ReorderLevel = 3
        };

        var user = new User
        {
            TenantId = Seed.TenantId,
            StoreId = Seed.StoreId,
            Username = Seed.Username,
            Email = "owner@example.com",
            FullName = "Shop Owner",
            PasswordHash = hasher.Hash(Seed.Password),
            IsActive = true
        };
        user.UserRoles.Add(new UserRole { User = user, Role = role });

        db.Tenants.Add(tenant);
        db.Stores.Add(store);
        db.Roles.Add(role);
        db.Products.Add(product);
        db.ProductVariants.Add(variant);
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}
