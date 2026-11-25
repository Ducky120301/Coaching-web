using Coaching.Domain.Coaches;
using Coaching.Domain.Students;

namespace Coaching.Application.Students;

/// <summary>Hồ sơ chi tiết cho coach (chỉ dùng ở /api/admin).</summary>
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
    string Version)
{
    public static StudentDetailDto From(Student student, Coach coach) => new(
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
        StudentVersion.Format(student.Version)); // @ADR-f02-ho-so-hoc-vien-004

    /// <summary>MVP chỉ có coach sở hữu hồ sơ thao tác trên hồ sơ.</summary>
    public static string ActorName(Guid userId, Coach coach) =>
        userId == coach.UserId ? coach.DisplayName : "Người dùng khác";
}
