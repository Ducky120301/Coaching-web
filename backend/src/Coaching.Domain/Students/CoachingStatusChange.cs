using Coaching.Domain.Common;

namespace Coaching.Domain.Students;

/// <summary>
/// @ADR-f02-ho-so-hoc-vien-005 @BR-f02-ho-so-hoc-vien-020: một dòng lịch sử đổi trạng thái coaching.
/// @BR-f02-ho-so-hoc-vien-021: không có setter công khai, chỉ Student tạo được → không sửa/xóa.
/// </summary>
public sealed class CoachingStatusChange : Entity<Guid>
{
    public CoachingStatus FromStatus { get; private set; }

    public CoachingStatus ToStatus { get; private set; }

    public Guid ActorUserId { get; private set; }

    public DateTimeOffset At { get; private set; }

    public string Reason { get; private set; } = default!;

    private CoachingStatusChange() { }

    internal CoachingStatusChange(CoachingStatus from, CoachingStatus to, Guid actorUserId, DateTimeOffset at, string reason)
        : base(Guid.CreateVersion7())
    {
        FromStatus = from;
        ToStatus = to;
        ActorUserId = actorUserId;
        At = at;
        Reason = reason;
    }
}
