namespace Coaching.Application.Accounts;

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-009: kết thúc mọi phiên (thu hồi mọi refresh token còn sống) của tài khoản sau khi đặt lại mật khẩu.
/// Access token còn sống tối đa 15 phút (F00); lần làm mới kế tiếp bị từ chối.
/// </summary>
public interface ISessionRevoker
{
    Task RevokeAllAsync(Guid userId, CancellationToken ct = default);
}
