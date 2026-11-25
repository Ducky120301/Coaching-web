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
    IReadOnlyList<string> MissingRequired)
{
    /// <param name="coach">Coach sở hữu hồ sơ — dùng để hiển thị tên người sửa (Task 3, 5).</param>
    public static StudentDetailDto From(Student student, Coach coach) => new(
        student.Id,
        student.CoachId,
        student.FullName,
        student.Email.Value,
        student.AccountStatus.ToString(),
        student.CoachingStatus.ToString(),
        student.CreatedAt,
        student.Registration.ToInput(),
        student.MissingRequiredFields); // @BR-f02-ho-so-hoc-vien-004
}
