using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Queries;

public sealed class GetStudentHandler(CurrentCoach currentCoach, IStudentRepository students)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;

        var student = await students.GetAsync(id, coach.Id, cancellationToken);
        return student is null
            ? StudentErrors.NotFound
            : Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach));
    }
}
