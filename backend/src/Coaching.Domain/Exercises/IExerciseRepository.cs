namespace Coaching.Domain.Exercises;

public interface IExerciseRepository
{
    /// <summary>Bài thuộc coach (được theo dõi để sửa/xoá); bài của coach khác coi như không tồn tại.</summary>
    Task<Exercise?> GetAsync(Guid id, Guid coachId, CancellationToken ct = default);

    /// <summary>@BR-f03-thu-vien-bai-tap-020: bài để sửa; khi lưu so Version trong DB với <paramref name="expectedVersion"/>.</summary>
    Task<Exercise?> GetForUpdateAsync(Guid id, Guid coachId, byte[] expectedVersion, CancellationToken ct = default);

    /// <summary>@BR-f03-thu-vien-bai-tap-001: bài cùng khoá tên + dụng cụ (kể cả đang ẩn), không theo dõi.</summary>
    Task<Exercise?> FindByKeyAsync(Guid coachId, string nameKey, Guid equipmentId, CancellationToken ct = default);

    void Add(Exercise e);

    void Remove(Exercise e);
}
