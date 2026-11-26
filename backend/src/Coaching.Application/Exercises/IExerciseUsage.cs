namespace Coaching.Application.Exercises;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-004: điểm cắm "bài đang được dùng trong giáo án". F05 thay đăng ký bằng truy vấn giáo án thật;
/// luồng xoá không phải sửa.
/// </summary>
public interface IExerciseUsage
{
    Task<bool> IsUsedAsync(Guid exerciseId, CancellationToken ct = default);
}

/// <summary>@ADR-f03-thu-vien-bai-tap-004: P1 chưa có giáo án → không bài nào đang được dùng.</summary>
public sealed class NoExerciseUsage : IExerciseUsage
{
    public Task<bool> IsUsedAsync(Guid exerciseId, CancellationToken ct = default) => Task.FromResult(false);
}
