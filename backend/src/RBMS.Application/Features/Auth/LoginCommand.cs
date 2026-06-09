using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Domain.Entities;

namespace RBMS.Application.Features.Auth;

public record LoginCommand(string Username, string Password) : IRequest<AuthResultDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public LoginCommandHandler(
        IApplicationDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwt,
        ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Login is pre-authentication: no tenant context yet, so bypass the tenant filter.
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            await RecordLoginAsync(null, request.Username, false, "User not found", cancellationToken);
            throw new ForbiddenAccessException("Invalid username or password.");
        }

        if (user.LockoutEnd is { } lockout && lockout > _clock.UtcNow)
        {
            await RecordLoginAsync(user, request.Username, false, "Locked out", cancellationToken);
            throw new ForbiddenAccessException("Account is temporarily locked. Try again later.");
        }

        if (!user.IsActive)
        {
            await RecordLoginAsync(user, request.Username, false, "Inactive", cancellationToken);
            throw new ForbiddenAccessException("Account is disabled.");
        }

        if (!_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedAttempts)
                user.LockoutEnd = _clock.UtcNow.Add(LockoutDuration);
            await RecordLoginAsync(user, request.Username, false, "Bad password", cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            throw new ForbiddenAccessException("Invalid username or password.");
        }

        // Success: reset counters, issue tokens.
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = _clock.UtcNow;

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = await _db.RolePermissions
            .Where(rp => user.UserRoles.Select(ur => ur.RoleId).Contains(rp.RoleId))
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var tokens = _jwt.CreateTokenPair(user, roles, permissions);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _jwt.HashRefreshToken(tokens.RefreshToken),
            Jti = Guid.NewGuid(),
            ExpiresAt = _clock.UtcNow.Add(RefreshTokenLifetime),
            CreatedAt = _clock.UtcNow,
            CreatedByIp = _currentUser.IpAddress
        });

        await RecordLoginAsync(user, request.Username, true, null, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            user.Id, user.Username, user.FullName, roles,
            tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAt);
    }

    private Task RecordLoginAsync(User? user, string username, bool ok, string? reason, CancellationToken ct)
    {
        _db.LoginHistory.Add(new LoginHistory
        {
            TenantId = user?.TenantId ?? Guid.Empty,
            UserId = user?.Id,
            UsernameTried = username,
            Succeeded = ok,
            FailureReason = reason,
            IpAddress = _currentUser.IpAddress,
            CreatedAt = _clock.UtcNow
        });
        return Task.CompletedTask;
    }
}
