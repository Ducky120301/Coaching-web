using Coaching.Application.Abstractions;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Coaching.Infrastructure.Identity;

internal sealed class AuthService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    JwtTokenGenerator tokenGenerator,
    IOptions<JwtOptions> jwtOptions,
    IClock clock) : IAuthService
{
    public async Task<Result<AuthTokens>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
            return AuthErrors.InvalidCredentials;

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            return AuthErrors.InvalidCredentials;

        if (await userManager.IsLockedOutAsync(user))
            return AuthErrors.LockedOut;

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return await userManager.IsLockedOutAsync(user) ? AuthErrors.LockedOut : AuthErrors.InvalidCredentials;
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<Result<AuthTokens>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return AuthErrors.InvalidRefreshToken;

        var now = clock.UtcNow;
        var hash = TokenHasher.Hash(request.RefreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (stored is null)
            return AuthErrors.InvalidRefreshToken;

        if (stored.RevokedAt is not null)
        {
            // Token đã bị thu hồi mà vẫn được dùng lại → nghi bị lộ: thu hồi toàn bộ phiên của user.
            await RevokeAllAsync(stored.UserId, now, cancellationToken);
            return AuthErrors.InvalidRefreshToken;
        }

        if (!stored.IsActive(now))
            return AuthErrors.InvalidRefreshToken;

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || await userManager.IsLockedOutAsync(user))
            return AuthErrors.InvalidRefreshToken;

        var tokens = await IssueTokensAsync(user, cancellationToken, rotateFrom: stored);
        return tokens;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var hash = TokenHasher.Hash(refreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = clock.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<AuthTokens> IssueTokensAsync(AppUser user, CancellationToken cancellationToken,
        RefreshToken? rotateFrom = null)
    {
        var now = clock.UtcNow;
        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var (accessToken, accessExpiresAt) = tokenGenerator.CreateAccessToken(user, roles);

        var refreshToken = TokenHasher.NewToken();
        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenHasher.Hash(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenDays),
        };
        db.RefreshTokens.Add(refreshEntity);

        if (rotateFrom is not null)
        {
            rotateFrom.RevokedAt = now;
            rotateFrom.ReplacedByTokenHash = refreshEntity.TokenHash;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new AuthTokens(
            accessToken,
            accessExpiresAt,
            refreshToken,
            refreshEntity.ExpiresAt,
            new AuthUser(user.Id, user.Email ?? string.Empty, user.FullName, roles));
    }

    private async Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
    }
}
