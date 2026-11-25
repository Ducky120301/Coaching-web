using Coaching.Application.Abstractions;
using Coaching.Application.Accounts;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Identity;

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-009: thu hồi mọi refresh token còn sống của tài khoản — dùng chung cho đặt lại mật khẩu
/// (ISessionRevoker) và phát hiện lộ mã của F00 (AuthService, @BR-f00-codebase-012). Chạy trong transaction hiện tại nếu có.
/// </summary>
internal sealed class SessionRevoker(AppDbContext db, IClock clock) : ISessionRevoker
{
    // @BR-f01-p1-tai-khoan-hoc-vien-021
    public Task RevokeAllAsync(Guid userId, CancellationToken ct = default) =>
        RevokeAllAsync(userId, RefreshTokenRevokedReason.PasswordReset, ct);

    public async Task RevokeAllAsync(Guid userId, RefreshTokenRevokedReason reason, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokedReason, reason), ct);
    }
}
