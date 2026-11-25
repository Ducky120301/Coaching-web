using Coaching.Application.Abstractions;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Queries;

/// <summary>
/// @ADR-f02-ho-so-hoc-vien-007 @BR-f02-ho-so-hoc-vien-016: DTO riêng cho phía học viên — KHÔNG có ghi chú nội bộ.
/// Giữ FullName, Email cho web học viên hiện tại (F00).
/// </summary>
public sealed record StudentSelfDto(string FullName, string Email, string CoachingStatus, string AccountStatus);

public sealed class GetStudentSelfHandler(IStudentRepository students, ICurrentUser currentUser)
{
    public async Task<Result<StudentSelfDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return new Error("auth.unauthenticated", "Chưa đăng nhập.", ErrorKind.Unauthorized);

        var student = await students.GetByUserIdAsync(userId, cancellationToken); // lọc theo user trong token
        return student is null
            ? StudentErrors.NotFound
            : Result<StudentSelfDto>.Success(new StudentSelfDto(
                student.FullName,
                student.Email.Value,
                student.CoachingStatus.ToString(),
                student.AccountStatus.ToString()));
    }
}
