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
    public const int InternalNoteMaxLength = 5000; // @ADR-f02-ho-so-hoc-vien-009
    public const int ReasonMaxLength = 500; // @ADR-f02-ho-so-hoc-vien-009

    private readonly List<CoachingStatusChange> _statusHistory = [];

    public Guid CoachId { get; private set; }

    /// <summary>Id tài khoản đăng nhập (Identity user) tạo cùng hồ sơ.</summary>
    public Guid UserId { get; private set; }

    public string FullName { get; private set; } = default!;

    public Email Email { get; private set; } = default!;

    public AccountStatus AccountStatus { get; private set; }

    public CoachingStatus CoachingStatus { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    // @ADR-f02-ho-so-hoc-vien-002
    public RegistrationForm Registration { get; private set; } = default!;

    /// <summary>@BR-f02-ho-so-hoc-vien-004: hồ sơ luôn cho biết mục bắt buộc còn thiếu.</summary>
    public IReadOnlyList<string> MissingRequiredFields => Registration.MissingRequired();

    // @BR-f02-ho-so-hoc-vien-013: chỉ ghi lần sửa gần nhất, không lưu giá trị cũ
    public DateTimeOffset? UpdatedAt { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    /// <summary>@ADR-f02-ho-so-hoc-vien-004: rowversion do SQL Server sinh, dùng chặn ghi đè.</summary>
    public byte[] Version { get; private set; } = [];

    /// <summary>Chỉ coach thấy — không bao giờ có trong dữ liệu trả cho học viên (@BR-f02-ho-so-hoc-vien-016).</summary>
    public string? InternalNote { get; private set; }

    /// <summary>@BR-f02-ho-so-hoc-vien-014: thời điểm sửa ghi chú, tách khỏi UpdatedAt của hồ sơ.</summary>
    public DateTimeOffset? InternalNoteUpdatedAt { get; private set; }

    /// <summary>@BR-f02-ho-so-hoc-vien-021: chỉ đọc; chỉ ChangeCoachingStatus thêm dòng.</summary>
    public IReadOnlyList<CoachingStatusChange> StatusHistory => _statusHistory.AsReadOnly();

    private Student() { }

    private Student(Guid id) : base(id) { }

    public static Student Create(Guid coachId, Guid userId, string? fullName, Email email, DateTimeOffset at,
        RegistrationForm? registration = null)
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
            Registration = registration ?? RegistrationForm.CreateEmpty(),
        };
    }

    /// <summary>
    /// @BR-f02-ho-so-hoc-vien-012: sửa họ tên và mọi mục form (kể cả số đo, ảnh ban đầu); email không sửa ở đây.
    /// @BR-f02-ho-so-hoc-vien-013: ghi người sửa và thời điểm sửa gần nhất.
    /// </summary>
    public void UpdateRegistration(string? fullName, RegistrationForm registration, Guid actorUserId, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(registration);
        var name = NormalizeFullName(fullName); // kiểm trước khi đổi gì
        FullName = name;
        Registration = registration;
        UpdatedAt = at;
        UpdatedByUserId = actorUserId;
    }

    /// <summary>@BR-f02-ho-so-hoc-vien-014: thời điểm sửa ghi chú chỉ đổi khi nội dung ghi chú đổi.</summary>
    public void UpdateInternalNote(string? note, DateTimeOffset at)
    {
        var value = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (value is not null && value.Length > InternalNoteMaxLength)
            throw new DomainException("student.internal_note_too_long",
                $"Ghi chú nội bộ tối đa {InternalNoteMaxLength} ký tự.");
        if (value == InternalNote)
            return;
        InternalNote = value;
        InternalNoteUpdatedAt = at;
    }

    /// <summary>
    /// @BR-f02-ho-so-hoc-vien-017: chỉ sang trạng thái khác trạng thái hiện tại.
    /// @BR-f02-ho-so-hoc-vien-018: cần lý do 1–500 ký tự (sau trim).
    /// @BR-f02-ho-so-hoc-vien-019: Active / Paused / Ended đổi qua lại theo mọi chiều; dữ liệu khác giữ nguyên.
    /// @BR-f02-ho-so-hoc-vien-020 @ADR-f02-ho-so-hoc-vien-005: thành công ⇒ thêm đúng một dòng lịch sử; thất bại ⇒ không thêm.
    /// @BR-f02-ho-so-hoc-vien-022: không phụ thuộc trạng thái tài khoản.
    /// </summary>
    public void ChangeCoachingStatus(CoachingStatus newStatus, string? reason, Guid actorUserId, DateTimeOffset at)
    {
        if (!Enum.IsDefined(newStatus))
            throw new DomainException("student.status_invalid", "Trạng thái coaching không hợp lệ.");
        if (newStatus == CoachingStatus)
            throw new DomainException("student.status_unchanged", "Trạng thái coaching không thay đổi.");

        var text = reason?.Trim();
        if (string.IsNullOrEmpty(text))
            throw new DomainException("student.reason_required", "Cần nhập lý do đổi trạng thái coaching.");
        if (text.Length > ReasonMaxLength)
            throw new DomainException("student.reason_too_long", $"Lý do tối đa {ReasonMaxLength} ký tự.");

        _statusHistory.Add(new CoachingStatusChange(CoachingStatus, newStatus, actorUserId, at, text));
        CoachingStatus = newStatus;
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
