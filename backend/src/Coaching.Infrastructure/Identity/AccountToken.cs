using Coaching.Application.Accounts;

namespace Coaching.Infrastructure.Identity;

/// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-003: mã kích hoạt / link đặt lại — chỉ lưu hash SHA-256 (hex 64 ký tự).</summary>
public sealed class AccountToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UserId { get; set; }

    public AccountTokenPurpose Purpose { get; set; }

    public AccountTokenSource Source { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}
