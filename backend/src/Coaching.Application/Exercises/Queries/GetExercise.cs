using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Queries;

public sealed class GetExerciseHandler(
    CurrentCoach currentCoach,
    IExerciseRepository exercises,
    IExerciseCategoryRepository categories)
{
    public async Task<Result<ExerciseDetailDto>> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return StudentErrors.CoachNotFound;

        // NFR-f03-thu-vien-bai-tap-002: bài của coach khác coi như không tồn tại
        if (await exercises.GetAsync(id, coach.Id, ct) is not { } exercise)
            return ExerciseErrors.NotFound;

        return await ExerciseCategoryCheck.DetailAsync(categories, exercise, ct);
    }
}
