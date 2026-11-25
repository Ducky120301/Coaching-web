using Coaching.Application.Abstractions;
using Coaching.Application.Accounts;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Domain.Students;
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
    IAccountTokens accountTokens,
    IClock clock) : IAuthService
{
    // @BR-f00-codebase-007
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    // @BR-f00-codebase-011 @BR-f00-codebase-012
    public static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(30);

    public async Task<Result<LoginOutcome>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
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

        // @ADR-f01-p1-tai-khoan-hoc-vien-002 @ADR-f01-p1-tai-khoan-hoc-vien-001: học viên Chờ kích hoạt chỉ có mật khẩu tạm.
        // Kiểm TRƯỚC vai trò: mật khẩu tạm trên web quản trị nhận thông báo chung, không phải WrongApp.
        if (await FindPendingStudentAsync(user.Id, cancellationToken) is { } pending)
            return await ChallengeAsync(user, pending, app, now, cancellationToken);

        // @ADR-f00-codebase-001 @BR-f00-codebase-004 @BR-f00-codebase-005: sai web → không cấp phiên.
        // Kiểm SAU khi mật khẩu đúng: người chưa biết mật khẩu chỉ nhận thông báo chung (@BR-f00-codebase-006).
        var requiredRole = app == ClientApp.Admin ? Roles.Admin : Roles.Student;
        if (!await userManager.IsInRoleAsync(user, requiredRole))
            return AuthErrors.WrongApp(app);

        await ResetFailedAttemptsAsync(user);
        return new LoginOutcome(await IssueTokensAsync(user, cancellationToken), null);
    }

    public async Task<AuthTokens> IssueSessionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"Không tìm thấy tài khoản {userId}.");
        return await IssueTokensAsync(user, cancellationToken);
    }

    private Task<Student?> FindPendingStudentAsync(Guid userId, CancellationToken ct) =>
        db.Students.AsNoTracking()
            .SingleOrDefaultAsync(s => s.UserId == userId && s.AccountStatus == AccountStatus.PendingActivation, ct);

    private async Task<Result<LoginOutcome>> ChallengeAsync(AppUser user, Student pending, ClientApp app, DateTimeOffset now,
        CancellationToken ct)
    {
        // @BR-f01-p1-tai-khoan-hoc-vien-004 @BR-f01-p1-tai-khoan-hoc-vien-012: mật khẩu tạm hết hạn → thông báo chung, tính là một lần sai
        if (!pending.IsTempPasswordValid(now))
        {
            await RegisterFailedAttemptAsync(user, now, ct);
            return AuthErrors.InvalidCredentials;
        }

        // @ADR-f01-p1-tai-khoan-hoc-vien-002 @BR-f01-p1-tai-khoan-hoc-vien-011: mật khẩu tạm không mở web quản trị (thông báo chung)
        if (app == ClientApp.Admin)
            return AuthErrors.InvalidCredentials;

        await ResetFailedAttemptsAsync(user);
        // @ADR-f01-p1-tai-khoan-hoc-vien-002 @BR-f01-p1-tai-khoan-hoc-vien-011: không cấp JWT → chưa gọi được API dữ liệu nào;
        // mã kích hoạt 30 phút, không quá hạn mật khẩu tạm
        var cap = now + AccountTokenLifetimes.Activation;
        var tempExpiry = pending.TempPasswordExpiresAt!.Value;
        var expiresAt = tempExpiry < cap ? tempExpiry : cap;
        var token = await accountTokens.IssueAsync(user.Id, AccountTokenPurpose.Activation, AccountTokenSource.Login, expiresAt, ct);
        return new LoginOutcome(null, new ActivationChallenge(true, token, expiresAt));
    }

    private static bool IsLockedOut(AppUser user, DateTimeOffset now) =>
        user.LockoutEnd is { } end && end > now;

    // @BR-f00-codebase-007: chỉ xóa bộ đếm khi mật khẩu đúng VÀ đúng web
    private async Task ResetFailedAttemptsAsync(AppUser user)
    {
        if (user.AccessFailedCount != 0 || user.LockoutEnd is not null)
        {
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            await userManager.UpdateAsync(user);
        }
    }

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
                var graceUser = leaf is null ? null : await FindActiveUserAsync(stored.UserId);
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

        var user = await FindActiveUserAsync(stored.UserId);
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

    // Khóa đăng nhập (BR-007) chỉ áp cho đăng nhập; mã làm mới hợp lệ đã chứng minh quyền sở hữu phiên → vẫn gia hạn được.
    private async Task<AppUser?> FindActiveUserAsync(Guid userId) =>
        await userManager.FindByIdAsync(userId.ToString());

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
