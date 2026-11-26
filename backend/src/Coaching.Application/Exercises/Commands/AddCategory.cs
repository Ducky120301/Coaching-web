using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Commands;

/// <summary><paramref name="Kind"/>: "MuscleGroup" | "Equipment" (không phân biệt hoa/thường).</summary>
public sealed record AddCategoryCommand(string? Kind, string? Name);

public sealed record CategoryCreatedDto(Guid Id, string Kind, string Name);

public sealed class AddCategoryHandler(
    CurrentCoach currentCoach,
    IExerciseCategoryRepository categories,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<CategoryCreatedDto>> HandleAsync(AddCategoryCommand command, CancellationToken ct = default)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return StudentErrors.CoachNotFound;
        if (!Enum.TryParse<CategoryKind>(command.Kind, ignoreCase: true, out var kind) || !Enum.IsDefined(kind))
            return ExerciseErrors.CategoryKindInvalid;

        // @BR-f03-thu-vien-bai-tap-006: mục mới đứng sau các mục cùng loại đã có
        var existing = await categories.ListAsync(coach.Id, ct);
        var sortOrder = existing.Where(c => c.Kind == kind).Select(c => c.SortOrder).DefaultIfEmpty(0).Max() + 1;
        var category = ExerciseCategory.Create(coach.Id, kind, command.Name, sortOrder); // tên trống → 400 category.name_required

        // @BR-f03-thu-vien-bai-tap-006 @ADR-f03-thu-vien-bai-tap-002: trùng khoá tên với mục cùng loại → 409
        if (await categories.ExistsAsync(coach.Id, kind, category.NameKey, ct))
            return ExerciseErrors.CategoryDuplicate;

        categories.Add(category);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintException)
        {
            return ExerciseErrors.CategoryDuplicate; // @BR-f03-thu-vien-bai-tap-006: hai request đua nhau
        }

        return new CategoryCreatedDto(category.Id, category.Kind.ToString(), category.Name);
    }
}
