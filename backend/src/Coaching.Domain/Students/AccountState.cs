namespace Coaching.Domain.Students;

/// <summary>Trạng thái tài khoản coach nhìn thấy trên hồ sơ (tính theo thời điểm hiện tại).</summary>
public enum AccountState
{
    NotSent,
    Sent,
    TempPasswordExpired,
    Activated,
}
