using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Queries;

/// <summary>@BR-f03-thu-vien-bai-tap-016: <paramref name="Hidden"/> mặc định false — danh sách không gồm bài đã ẩn.</summary>
public sealed record ListExercisesQuery(string? Q, Guid? MuscleGroupId, Guid? EquipmentId, bool Hidden);

public sealed record ExerciseListItemDto(Guid Id, string Name, string MuscleGroup, string Equipment, bool HasVideo, bool IsHidden);

public sealed class ListExercisesHandler(
    CurrentCoach currentCoach,
    IExerciseRepository exercises,
    IExerciseCategoryRepository categories)
{
    public async Task<Result<IReadOnlyList<ExerciseListItemDto>>> HandleAsync(ListExercisesQuery query, CancellationToken ct = default)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return StudentErrors.CoachNotFound;

        // @BR-f03-thu-vien-bai-tap-018 @ADR-f03-thu-vien-bai-tap-002: từ khoá chuẩn hoá giống SearchKey
        // @BR-f03-thu-vien-bai-tap-019: nhóm cơ + dụng cụ + từ khoá kết hợp (AND)
        var filter = new ExerciseFilter(ExerciseNameKey.For(query.Q), query.MuscleGroupId, query.EquipmentId, query.Hidden);
        var rows = await exercises.ListAsync(coach.Id, filter, ct);
        var names = (await categories.ListAsync(coach.Id, ct)).ToDictionary(c => c.Id, c => c.Name);

        IReadOnlyList<ExerciseListItemDto> items = rows
            .Select(r => new ExerciseListItemDto(r.Id, r.Name, names[r.MuscleGroupId], names[r.EquipmentId], r.HasVideo, r.IsHidden))
            .ToList();
        return Result<IReadOnlyList<ExerciseListItemDto>>.Success(items);
    }
}
