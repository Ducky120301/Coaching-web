namespace Coaching.Domain.Students;

/// <summary>Luật mật khẩu riêng — dùng cho cả kích hoạt (Task 5) và đặt lại qua link (Task 7).</summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    /// <summary>
    /// @BR-f01-p1-tai-khoan-hoc-vien-013: 8–128 ký tự (string.Length). @BR-f01-p1-tai-khoan-hoc-vien-015: không trim, giữ dấu.
    /// @BR-f01-p1-tai-khoan-hoc-vien-014: hai lần nhập khớp theo so sánh ordinal.
    /// </summary>
    public static PasswordCheck Check(string? password, string? confirm)
    {
        var length = password?.Length ?? 0;
        if (length < MinLength)
            return PasswordCheck.TooShort;
        if (length > MaxLength)
            return PasswordCheck.TooLong;
        return string.Equals(password, confirm, StringComparison.Ordinal) ? PasswordCheck.Ok : PasswordCheck.Mismatch;
    }
}
