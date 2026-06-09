using Microsoft.AspNetCore.Identity;
using RBMS.Application.Common.Interfaces;
using RBMS.Domain.Entities;

namespace RBMS.Infrastructure.Services;

/// <summary>Wraps ASP.NET Core's PBKDF2 PasswordHasher behind the application abstraction.</summary>
public class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();
    private static readonly User Dummy = new();

    public string Hash(string password) => _inner.HashPassword(Dummy, password);

    public bool Verify(string hash, string password)
    {
        var result = _inner.VerifyHashedPassword(Dummy, hash, password);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
