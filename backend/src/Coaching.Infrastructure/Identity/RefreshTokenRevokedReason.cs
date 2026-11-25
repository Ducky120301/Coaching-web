namespace Coaching.Infrastructure.Identity;

/// <summary>@ADR-f00-codebase-003: phân biệt thu hồi do xoay vòng / đăng xuất / phát hiện lộ mã.</summary>
public enum RefreshTokenRevokedReason
{
    Rotated,
    Logout,
    ReuseDetected,
}
