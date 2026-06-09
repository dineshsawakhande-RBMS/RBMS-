using System.Security.Claims;
using RBMS.Application.Common.Interfaces;
using RBMS.Infrastructure.Services;

namespace RBMS.Api.Services;

/// <summary>Resolves the authenticated caller from the JWT claims on the current request.</summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public Guid? UserId => GetGuid(ClaimTypes.NameIdentifier) ?? GetGuid("sub");
    public Guid? TenantId => GetGuid(RbmsClaims.TenantId);
    public Guid? StoreId => GetGuid(RbmsClaims.StoreId);
    public string? Username => Principal?.FindFirstValue("unique_name") ?? Principal?.FindFirstValue(ClaimTypes.Name);

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>();

    public IReadOnlyCollection<string> Permissions =>
        Principal?.FindAll(RbmsClaims.Permission).Select(c => c.Value).ToArray() ?? Array.Empty<string>();

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool HasPermission(string permission) =>
        Principal?.HasClaim(RbmsClaims.Permission, permission) ?? false;

    private Guid? GetGuid(string claimType)
        => Guid.TryParse(Principal?.FindFirstValue(claimType), out var g) ? g : null;
}
