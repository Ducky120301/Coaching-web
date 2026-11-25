using Coaching.Domain.Common;

namespace Coaching.Domain.Students;

/// <summary>
/// Hồ sơ học viên: form đăng ký, ghi chú nội bộ, trạng thái tài khoản và trạng thái coaching.
/// Không có thao tác xóa — ngừng coaching bằng trạng thái Ended.
/// </summary>
public sealed class Student : AggregateRoot<Guid>
{
    // @ADR-f02-ho-so-hoc-vien-009
    public const int FullNameMaxLength = 100;

    public Guid CoachId { get; private set; }

    /// <summary>Id tài khoản đăng nhập (Identity user) tạo cùng hồ sơ.</summary>
    public Guid UserId { get; private set; }

    public string FullName { get; private set; } = default!;

    public Email Email { get; private set; } = default!;

    public AccountStatus AccountStatus { get; private set; }

    public CoachingStatus CoachingStatus { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private Student() { }

    private Student(Guid id) : base(id) { }

    public static Student Create(Guid coachId, Guid userId, string? fullName, Email email, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(email);
        if (coachId == Guid.Empty)
            throw new DomainException("student.coach_required", "Hồ sơ phải thuộc một coach.");
        if (userId == Guid.Empty)
            throw new DomainException("student.user_required", "Hồ sơ phải gắn với một tài khoản đăng nhập.");

        return new Student(Guid.CreateVersion7())
        {
            CoachId = coachId, // @BR-f02-ho-so-hoc-vien-007
            UserId = userId,
            FullName = NormalizeFullName(fullName), // @BR-f02-ho-so-hoc-vien-003
            Email = email, // @BR-f02-ho-so-hoc-vien-001 @BR-f02-ho-so-hoc-vien-002: Email đã chuẩn hóa (trim + lowercase)
            AccountStatus = AccountStatus.PendingActivation, // @BR-f02-ho-so-hoc-vien-005
            CoachingStatus = CoachingStatus.Active, // @BR-f02-ho-so-hoc-vien-006
            CreatedAt = at,
        };
    }

    // @BR-f02-ho-so-hoc-vien-003
    private static string NormalizeFullName(string? fullName)
    {
        var name = fullName?.Trim();
        if (string.IsNullOrEmpty(name))
            throw new DomainException("student.full_name_required", "Họ tên là bắt buộc.");
        if (name.Length > FullNameMaxLength)
            throw new DomainException("student.full_name_too_long", $"Họ tên tối đa {FullNameMaxLength} ký tự.");
        return name;
    }
}
