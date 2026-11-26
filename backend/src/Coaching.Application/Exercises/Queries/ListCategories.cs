using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Queries;

public sealed record CategoryDto(Guid Id, string Name);

public sealed record CategoriesDto(IReadOnlyList<CategoryDto> MuscleGroups, IReadOnlyList<CategoryDto> Equipment)
{
    public static CategoriesDto From(IEnumerable<ExerciseCategory> categories)
    {
        var ordered = categories.OrderBy(c => c.SortOrder).ThenBy(c => c.Name, StringComparer.Ordinal).ToList();
        return new CategoriesDto(Of(ordered, CategoryKind.MuscleGroup), Of(ordered, CategoryKind.Equipment));
    }

    private static List<CategoryDto> Of(IEnumerable<ExerciseCategory> categories, CategoryKind kind) =>
        categories.Where(c => c.Kind == kind).Select(c => new CategoryDto(c.Id, c.Name)).ToList();
}

/// <summary>@BR-f03-thu-vien-bai-tap-005: danh mục của coach đang đăng nhập, mỗi loại theo SortOrder.</summary>
public sealed class ListCategoriesHandler(CurrentCoach currentCoach, IExerciseCategoryRepository categories)
{
    public async Task<Result<CategoriesDto>> HandleAsync(CancellationToken ct = default)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return StudentErrors.CoachNotFound;

        return CategoriesDto.From(await categories.ListAsync(coach.Id, ct));
    }
}
