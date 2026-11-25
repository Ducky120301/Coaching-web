using Coaching.Domain.Common;

namespace Coaching.Domain.Students;

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-006: một dòng lịch sử tài khoản. Không có setter công khai, chỉ Student tạo được
/// → không sửa/xóa. Không bao giờ chứa mật khẩu hay token.
/// </summary>
public sealed class StudentAccountEvent : Entity<Guid>
{
    public AccountEventType Type { get; private set; }

    public DateTimeOffset At { get; private set; }

    /// <summary>Người thực hiện: coach (gửi, sửa email, gửi link) hoặc chính học viên (kích hoạt).</summary>
    public Guid? ActorUserId { get; private set; }

    /// <summary>Chỉ có ở EmailChanged.</summary>
    public string? FromEmail { get; private set; }

    /// <summary>Chỉ có ở EmailChanged.</summary>
    public string? ToEmail { get; private set; }

    private StudentAccountEvent() { }

    internal StudentAccountEvent(AccountEventType type, DateTimeOffset at, Guid? actorUserId,
        string? fromEmail = null, string? toEmail = null)
        : base(Guid.CreateVersion7())
    {
        Type = type;
        At = at;
        ActorUserId = actorUserId;
        FromEmail = fromEmail;
        ToEmail = toEmail;
    }
}
