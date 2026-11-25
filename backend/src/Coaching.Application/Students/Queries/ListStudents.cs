using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Queries;

/// <summary>@BR-f02-ho-so-hoc-vien-023: mỗi dòng có tên, email, trạng thái coaching, trạng thái tài khoản.</summary>
public sealed record StudentListItemDto(
    Guid Id,
    string FullName,
    string Email,
    string CoachingStatus,
    string AccountStatus,
    DateTimeOffset CreatedAt);

public sealed class ListStudentsHandler(CurrentCoach currentCoach, IStudentRepository students)
{
    public async Task<Result<IReadOnlyList<StudentListItemDto>>> HandleAsync(string? query,
        CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;

        // @BR-f02-ho-so-hoc-vien-024: từ khóa chuẩn hóa giống SearchKey (không dấu, lowercase)
        var rows = await students.ListAsync(coach.Id, SearchText.Normalize(query), cancellationToken);
        IReadOnlyList<StudentListItemDto> items = rows
            .Select(r => new StudentListItemDto(r.Id, r.FullName, r.Email,
                r.CoachingStatus.ToString(), r.AccountStatus.ToString(), r.CreatedAt))
            .ToList();
        return Result<IReadOnlyList<StudentListItemDto>>.Success(items);
    }
}
