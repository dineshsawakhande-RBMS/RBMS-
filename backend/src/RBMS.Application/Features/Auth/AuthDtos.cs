namespace RBMS.Application.Features.Auth;

public record AuthResultDto(
    Guid UserId,
    string Username,
    string FullName,
    IReadOnlyList<string> Roles,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt);
