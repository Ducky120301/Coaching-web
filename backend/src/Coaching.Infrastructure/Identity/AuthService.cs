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
    // @BR-f00-codebase-007
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    public async Task<Result<AuthTokens>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
            return AuthErrors.InvalidCredentials;

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            return AuthErrors.InvalidCredentials;

        // @ADR-f00-codebase-005: tự kiểm khóa theo IClock thay vì IsLockedOutAsync (đồng hồ thật)
        var now = clock.UtcNow;
        if (IsLockedOut(user, now))
            return AuthErrors.InvalidCredentials; // @BR-f00-codebase-006: không lộ trạng thái khóa

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await RegisterFailedAttemptAsync(user, now, cancellationToken);
            return AuthErrors.InvalidCredentials;
        }

        if (user.AccessFailedCount != 0 || user.LockoutEnd is not null)
        {
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            await userManager.UpdateAsync(user);
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    private static bool IsLockedOut(AppUser user, DateTimeOffset now) =>
        user.LockoutEnd is { } end && end > now;

    // @BR-f00-codebase-007: sai 5 lần liên tiếp → khóa 5 phút.
    // Một câu UPDATE nguyên tử: cả hai vế SET tính từ giá trị cũ của dòng → không mất lần đếm khi đăng nhập song song.
    private async Task RegisterFailedAttemptAsync(AppUser user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var lockUntil = now.Add(LockoutDuration);
        await db.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(s => s
            .SetProperty(u => u.LockoutEnd, u => u.AccessFailedCount + 1 >= MaxFailedAttempts ? lockUntil : u.LockoutEnd)
            .SetProperty(u => u.AccessFailedCount, u => u.AccessFailedCount + 1 >= MaxFailedAttempts ? 0 : u.AccessFailedCount + 1),
            cancellationToken);
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
        if (user is null || IsLockedOut(user, now))
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
