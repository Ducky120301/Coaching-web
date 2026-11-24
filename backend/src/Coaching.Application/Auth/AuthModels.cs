namespace Coaching.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    AuthUser User);

public sealed record AuthUser(Guid Id, string Email, string FullName, IReadOnlyList<string> Roles);
