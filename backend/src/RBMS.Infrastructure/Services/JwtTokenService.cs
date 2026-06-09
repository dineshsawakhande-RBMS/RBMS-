using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RBMS.Application.Common.Interfaces;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Services;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "rbms-api";
    public string Audience { get; set; } = "rbms-api";
    public string SigningKey { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 15;
}

/// <summary>Custom JWT claim types used across the API and authorization policies.</summary>
public static class RbmsClaims
{
    public const string TenantId = "tenant_id";
    public const string StoreId = "store_id";
    public const string Permission = "permission";
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly IDateTime _clock;
    private readonly SigningCredentials _credentials;

    public JwtTokenService(IOptions<JwtOptions> options, IDateTime clock)
    {
        _options = options.Value;
        _clock = clock;
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be configured and at least 32 chars.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public TokenPair CreateTokenPair(User user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var expires = _clock.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(RbmsClaims.TenantId, user.TenantId.ToString())
        };
        if (user.StoreId is { } storeId)
            claims.Add(new Claim(RbmsClaims.StoreId, storeId.ToString()));
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim(RbmsClaims.Permission, p)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: _clock.UtcNow.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: _credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenPair(accessToken, GenerateRefreshToken(), expires);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }
}
