namespace Coaching.Application.Accounts;

public static class AccountTokenLifetimes
{
    /// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-002: mã kích hoạt sống 30 phút nhưng không quá hạn mật khẩu tạm.</summary>
    public static readonly TimeSpan Activation = TimeSpan.FromMinutes(30);

    /// <summary>@BR-f01-p1-tai-khoan-hoc-vien-018: link đặt lại hết hiệu lực khi đủ 30 phút (now >= ExpiresAt).</summary>
    public static readonly TimeSpan PasswordReset = TimeSpan.FromMinutes(30);

    /// <summary>@BR-f01-p1-tai-khoan-hoc-vien-024 @ADR-f01-p1-tai-khoan-hoc-vien-005: mỗi tài khoản tối đa một thư Quên mật khẩu mỗi 60 giây.</summary>
    public static readonly TimeSpan SelfResetInterval = TimeSpan.FromSeconds(60);
}
