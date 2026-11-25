using Coaching.Domain.Coaches;
using Coaching.Domain.Students;

namespace Coaching.Application.Students;

/// <summary>Một dòng lịch sử trạng thái coaching (@BR-f02-ho-so-hoc-vien-020).</summary>
public sealed record StatusChangeDto(string From, string To, string Actor, DateTimeOffset At, string Reason);

/// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-006: một dòng lịch sử tài khoản — không có mật khẩu hay token.</summary>
public sealed record AccountEventDto(string Type, DateTimeOffset At, string? FromEmail, string? ToEmail);

/// <summary>Trạng thái + lịch sử tài khoản đăng nhập. History xếp mới nhất trước.</summary>
public sealed record AccountInfoDto(
    string State,
    DateTimeOffset? CredentialsSentAt,
    DateTimeOffset? TempPasswordExpiresAt,
    DateTimeOffset? ActivatedAt,
    IReadOnlyList<AccountEventDto> History)
{
    public static AccountInfoDto From(Student student, DateTimeOffset now) => new(
        student.GetAccountState(now).ToString(),
        student.CredentialsSentAt,
        student.TempPasswordExpiresAt,
        student.ActivatedAt,
        student.AccountEvents // @ADR-f01-p1-tai-khoan-hoc-vien-006: mới nhất trước
            .OrderByDescending(e => e.At)
            .ThenByDescending(e => e.Id)
            .Select(e => new AccountEventDto(e.Type.ToString(), e.At, e.FromEmail, e.ToEmail))
            .ToList());
}

/// <summary>Hồ sơ chi tiết cho coach — CHỈ dùng ở /api/admin (có ghi chú nội bộ).</summary>
public sealed record StudentDetailDto(
    Guid Id,
    Guid CoachId,
    string FullName,
    string Email,
    string AccountStatus,
    string CoachingStatus,
    DateTimeOffset CreatedAt,
    RegistrationInput Registration,
    IReadOnlyList<string> MissingRequired,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    string Version,
    string? InternalNote,
    DateTimeOffset? InternalNoteUpdatedAt,
    IReadOnlyList<StatusChangeDto> StatusHistory,
    AccountInfoDto Account)
{
    /// <param name="now">Thời điểm hiện tại (IClock.UtcNow) — để tính mật khẩu tạm đã hết hạn chưa.</param>
    public static StudentDetailDto From(Student student, Coach coach, DateTimeOffset now) => new(
        student.Id,
        student.CoachId,
        student.FullName,
        student.Email.Value,
        student.AccountStatus.ToString(),
        student.CoachingStatus.ToString(),
        student.CreatedAt,
        student.Registration.ToInput(),
        student.MissingRequiredFields, // @BR-f02-ho-so-hoc-vien-004
        student.UpdatedAt, // @BR-f02-ho-so-hoc-vien-013
        student.UpdatedByUserId is { } editor ? ActorName(editor, coach) : null,
        StudentVersion.Format(student.Version), // @ADR-f02-ho-so-hoc-vien-004
        student.InternalNote, // @BR-f02-ho-so-hoc-vien-016: coach thấy ghi chú
        student.InternalNoteUpdatedAt, // @BR-f02-ho-so-hoc-vien-014
        student.StatusHistory // @BR-f02-ho-so-hoc-vien-021: mới nhất trước
            .OrderByDescending(c => c.At)
            .ThenByDescending(c => c.Id)
            .Select(c => new StatusChangeDto(c.FromStatus.ToString(), c.ToStatus.ToString(),
                ActorName(c.ActorUserId, coach), c.At, c.Reason))
            .ToList(),
        AccountInfoDto.From(student, now)); // @ADR-f01-p1-tai-khoan-hoc-vien-006

    /// <summary>MVP chỉ có coach sở hữu hồ sơ thao tác trên hồ sơ.</summary>
    public static string ActorName(Guid userId, Coach coach) =>
        userId == coach.UserId ? coach.DisplayName : "Người dùng khác";
}
