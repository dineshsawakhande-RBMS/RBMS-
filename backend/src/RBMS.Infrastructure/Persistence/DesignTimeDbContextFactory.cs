using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Infrastructure.Persistence;

/// <summary>
/// Lets EF Core tooling (`dotnet ef migrations add ...`) build the context without the
/// full DI graph. Connection string can be overridden via the RBMS_DB env var.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("RBMS_DB")
            ?? "Host=localhost;Port=5432;Database=rbms;Username=rbms_admin;Password=changeme_local";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(conn, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ApplicationDbContext(options, new DesignTimeCurrentUser());
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public Guid? TenantId => null;
        public Guid? StoreId => null;
        public string? Username => null;
        public string? IpAddress => null;
        public IReadOnlyCollection<string> Roles => Array.Empty<string>();
        public IReadOnlyCollection<string> Permissions => Array.Empty<string>();
        public bool IsAuthenticated => false;
        public bool HasPermission(string permission) => false;
    }
}
