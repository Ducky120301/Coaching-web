namespace Coaching.Infrastructure.Identity;

/// <summary>
/// @ADR-f00-codebase-003: phân biệt thu hồi do xoay vòng / đăng xuất / phát hiện lộ mã.
/// @ADR-f01-p1-tai-khoan-hoc-vien-009: PasswordReset — thu hồi vì học viên đặt lại mật khẩu (cột nvarchar(20) đủ chỗ, không đổi schema).
/// </summary>
public enum RefreshTokenRevokedReason
{
    Rotated,
    Logout,
    ReuseDetected,
    PasswordReset,
}
