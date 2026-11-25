namespace Coaching.Application.Accounts;

public static class AccountTokenLifetimes
{
    /// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-002: mã kích hoạt sống 30 phút nhưng không quá hạn mật khẩu tạm.</summary>
    public static readonly TimeSpan Activation = TimeSpan.FromMinutes(30);
}
