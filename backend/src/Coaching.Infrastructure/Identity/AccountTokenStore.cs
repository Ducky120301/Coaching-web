using Coaching.Application.Abstractions;
using Coaching.Application.Accounts;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Identity;

/// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-003: kho token tài khoản dùng chung cho kích hoạt và đặt lại mật khẩu.</summary>
internal sealed class AccountTokenStore(AppDbContext db, IClock clock) : IAccountTokens
{
    public const int TokenBytes = 32;

    public async Task<string> IssueAsync(Guid userId, AccountTokenPurpose purpose, AccountTokenSource source,
        DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        // @ADR-f01-p1-tai-khoan-hoc-vien-003: có token mới thì token cũ cùng mục đích hết hiệu lực
        await LiveTokens(userId, purpose, now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, (DateTimeOffset?)now), ct);

        var raw = TokenHasher.NewToken(TokenBytes);
        db.AccountTokens.Add(new AccountToken
        {
            UserId = userId,
            Purpose = purpose,
            Source = source,
            TokenHash = TokenHasher.Hash(raw), // @NFR-f01-p1-tai-khoan-hoc-vien-001: chỉ lưu hash
            CreatedAt = now,
            ExpiresAt = expiresAt,
        });
        await db.SaveChangesAsync(ct);
        return raw;
    }

    public async Task<Guid?> FindValidAsync(string? rawToken, AccountTokenPurpose purpose, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return null;
        var hash = TokenHasher.Hash(rawToken);
        var now = clock.UtcNow;
        // @ADR-f01-p1-tai-khoan-hoc-vien-003: hết hạn khi now >= ExpiresAt
        return await db.AccountTokens.AsNoTracking()
            .Where(t => t.TokenHash == hash && t.Purpose == purpose
                && t.ConsumedAt == null && t.RevokedAt == null && t.ExpiresAt > now)
            .Select(t => (Guid?)t.UserId)
            .SingleOrDefaultAsync(ct);
    }

    public async Task<bool> ConsumeAsync(string rawToken, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        var hash = TokenHasher.Hash(rawToken);
        var now = clock.UtcNow;
        // @ADR-f01-p1-tai-khoan-hoc-vien-003: tiêu có điều kiện, nguyên tử — chỉ token còn hiệu lực (chưa tiêu, chưa hủy,
        // now < ExpiresAt); đúng một dòng bị cập nhật mới là tiêu thành công
        var affected = await db.AccountTokens
            .Where(t => t.TokenHash == hash && t.ConsumedAt == null && t.RevokedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ConsumedAt, (DateTimeOffset?)now), ct);
        return affected == 1;
    }

    public async Task RevokeAllAsync(Guid userId, AccountTokenPurpose purpose, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        await LiveTokens(userId, purpose, now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, (DateTimeOffset?)now), ct);
    }

    public Task<DateTimeOffset?> LastIssuedAtAsync(Guid userId, AccountTokenPurpose purpose, AccountTokenSource source,
        CancellationToken ct = default) =>
        db.AccountTokens.AsNoTracking()
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.Source == source)
            .MaxAsync(t => (DateTimeOffset?)t.CreatedAt, ct);

    private IQueryable<AccountToken> LiveTokens(Guid userId, AccountTokenPurpose purpose, DateTimeOffset now) =>
        db.AccountTokens.Where(t => t.UserId == userId && t.Purpose == purpose
            && t.ConsumedAt == null && t.RevokedAt == null && t.ExpiresAt > now);
}
