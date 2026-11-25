namespace Coaching.Domain.Students;

/// <summary>Một dòng danh sách học viên (@BR-f02-ho-so-hoc-vien-023): tên, email, trạng thái coaching, trạng thái tài khoản.</summary>
public sealed record StudentSummary(
    Guid Id,
    string FullName,
    string Email,
    CoachingStatus CoachingStatus,
    AccountStatus AccountStatus,
    DateTimeOffset CreatedAt);
