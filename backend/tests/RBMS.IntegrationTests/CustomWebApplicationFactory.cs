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
    public static class Seed
    {
        public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
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
                options.UseInMemoryDatabase("rbms-integration-tests");
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
        var perm = new Permission { Code = "product.view", Description = "View products" };
        var role = new Role { TenantId = Seed.TenantId, Name = RoleNames.Owner };
        role.RolePermissions.Add(new RolePermission { Role = role, Permission = perm });

        var user = new User
        {
            TenantId = Seed.TenantId,
            Username = Seed.Username,
            Email = "owner@example.com",
            FullName = "Shop Owner",
            PasswordHash = hasher.Hash(Seed.Password),
            IsActive = true
        };
        user.UserRoles.Add(new UserRole { User = user, Role = role });

        db.Tenants.Add(tenant);
        db.Permissions.Add(perm);
        db.Roles.Add(role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}
