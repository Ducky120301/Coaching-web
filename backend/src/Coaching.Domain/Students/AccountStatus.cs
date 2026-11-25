namespace Coaching.Domain.Students;

/// <summary>Trạng thái tài khoản đăng nhập của học viên. Chuyển sang Activated do F01 (kích hoạt) xử lý.</summary>
public enum AccountStatus
{
    PendingActivation,
    Activated,
}
