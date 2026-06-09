using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Domain.Entities;

namespace RBMS.Application.Features.Auth;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public RefreshTokenCommandHandler(
        IApplicationDbContext db, IJwtTokenService jwt, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _jwt = jwt;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = _jwt.HashRefreshToken(request.RefreshToken);

        var token = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .Include(rt => rt.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken)
            ?? throw new ForbiddenAccessException("Invalid refresh token.");

        // Reuse detection: a revoked token being presented again means it was stolen —
        // revoke the whole chain for that user.
        if (token.RevokedAt is not null)
        {
            await RevokeAllForUserAsync(token.UserId, "Reuse detected", cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            throw new ForbiddenAccessException("Refresh token has been revoked.");
        }

        if (!token.IsActive)
            throw new ForbiddenAccessException("Refresh token has expired.");

        var user = token.User;
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = await _db.RolePermissions
            .Where(rp => user.UserRoles.Select(ur => ur.RoleId).Contains(rp.RoleId))
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var tokens = _jwt.CreateTokenPair(user, roles, permissions);

        var replacement = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _jwt.HashRefreshToken(tokens.RefreshToken),
            Jti = Guid.NewGuid(),
            ExpiresAt = _clock.UtcNow.Add(RefreshTokenLifetime),
            CreatedAt = _clock.UtcNow,
            CreatedByIp = _currentUser.IpAddress
        };
        _db.RefreshTokens.Add(replacement);

        token.RevokedAt = _clock.UtcNow;
        token.RevokedByIp = _currentUser.IpAddress;
        token.ReplacedById = replacement.Id;
        token.ReasonRevoked = "Rotated";

        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            user.Id, user.Username, user.FullName, roles,
            tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAt);
    }

    private async Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken ct)
    {
        var active = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in active)
        {
            t.RevokedAt = _clock.UtcNow;
            t.ReasonRevoked = reason;
        }
    }
}
