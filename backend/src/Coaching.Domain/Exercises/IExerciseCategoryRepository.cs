namespace Coaching.Domain.Exercises;

public interface IExerciseCategoryRepository
{
    /// <summary>Mọi mục của coach (cả hai loại), theo SortOrder.</summary>
    Task<IReadOnlyList<ExerciseCategory>> ListAsync(Guid coachId, CancellationToken ct = default);

    /// <summary>Mục thuộc coach; mục của coach khác coi như không tồn tại.</summary>
    Task<ExerciseCategory?> GetAsync(Guid id, Guid coachId, CancellationToken ct = default);

    /// <summary>@BR-f03-thu-vien-bai-tap-006: đã có mục cùng loại, cùng khoá tên.</summary>
    Task<bool> ExistsAsync(Guid coachId, CategoryKind kind, string nameKey, CancellationToken ct = default);

    void Add(ExerciseCategory c);
}
