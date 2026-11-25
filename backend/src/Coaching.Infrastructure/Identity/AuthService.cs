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

    // @BR-f00-codebase-011 @BR-f00-codebase-012
    public static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(30);

    public async Task<Result<AuthTokens>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
            return AuthErrors.InvalidCredentials;

        if (request.App is not { } app)
            return AuthErrors.AppRequired;

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

        // @ADR-f00-codebase-001 @BR-f00-codebase-004 @BR-f00-codebase-005: sai web → không cấp phiên.
        // Kiểm SAU khi mật khẩu đúng: người chưa biết mật khẩu chỉ nhận thông báo chung (@BR-f00-codebase-006).
        var requiredRole = app == ClientApp.Admin ? Roles.Admin : Roles.Student;
        if (!await userManager.IsInRoleAsync(user, requiredRole))
            return AuthErrors.WrongApp(app);

        // @BR-f00-codebase-007: chỉ xóa bộ đếm khi đăng nhập thành công (đúng mật khẩu VÀ đúng web).
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

        if (stored.RevokedAt is { } revokedAt)
        {
            if (stored.RevokedReason != RefreshTokenRevokedReason.Rotated)
                return AuthErrors.InvalidRefreshToken; // @BR-f00-codebase-013: phiên đã đăng xuất / đã bị thu hồi

            if (now - revokedAt <= ReuseGracePeriod)
            {
                // @ADR-f00-codebase-003 @BR-f00-codebase-011: gửi trùng trong 30s → cấp cặp mới, không thu hồi
                // Chuỗi chỉ được có MỘT nhánh sống: xoay từ lá mới nhất của chuỗi.
                var leaf = await FindLiveLeafAsync(stored, now, cancellationToken);
                var graceUser = leaf is null ? null : await FindActiveUserAsync(stored.UserId, now);
                return graceUser is null
                    ? AuthErrors.InvalidRefreshToken
                    : await IssueTokensAsync(graceUser, cancellationToken, rotateFrom: leaf);
            }

            // @BR-f00-codebase-012: gửi lại sau 30s → coi là lộ mã, thu hồi mọi phiên
            await RevokeAllAsync(stored.UserId, now, cancellationToken);
            return AuthErrors.InvalidRefreshToken;
        }

        if (!stored.IsActive(now))
            return AuthErrors.InvalidRefreshToken;

        var user = await FindActiveUserAsync(stored.UserId, now);
        if (user is null)
            return AuthErrors.InvalidRefreshToken;

        return await IssueTokensAsync(user, cancellationToken, rotateFrom: stored);
    }

    // @ADR-f00-codebase-003: lần theo ReplacedByTokenHash (tối đa 5 bước) tới hậu duệ mới nhất còn hiệu lực.
    private async Task<RefreshToken?> FindLiveLeafAsync(RefreshToken start, DateTimeOffset now, CancellationToken ct)
    {
        var current = start;
        for (var hop = 0; hop < 5 && current.ReplacedByTokenHash is { } next; hop++)
        {
            var child = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == next, ct);
            if (child is null)
                return null;
            if (child.RevokedAt is null)
                return child.IsActive(now) ? child : null;
            current = child;
        }

        return null;
    }

    private async Task<AppUser?> FindActiveUserAsync(Guid userId, DateTimeOffset now)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null || IsLockedOut(user, now) ? null : user;
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
            stored.RevokedReason = RefreshTokenRevokedReason.Logout; // @BR-f00-codebase-013
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
            rotateFrom.RevokedReason = RefreshTokenRevokedReason.Rotated;
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
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokedReason, RefreshTokenRevokedReason.ReuseDetected), cancellationToken);
    }
}
