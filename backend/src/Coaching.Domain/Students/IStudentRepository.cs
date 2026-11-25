namespace Coaching.Domain.Students;

public interface IStudentRepository
{
    /// <summary>Hồ sơ thuộc coach; hồ sơ của coach khác coi như không tồn tại.</summary>
    Task<Student?> GetAsync(Guid id, Guid coachId, CancellationToken cancellationToken = default);

    /// <summary>
    /// @ADR-f02-ho-so-hoc-vien-004: hồ sơ để sửa; khi lưu, hệ thống so Version trong DB với
    /// <paramref name="expectedVersion"/> và báo xung đột nếu khác.
    /// </summary>
    Task<Student?> GetForUpdateAsync(Guid id, Guid coachId, byte[] expectedVersion, CancellationToken cancellationToken = default);

    /// <summary>Hồ sơ của tài khoản đăng nhập (dùng cho /api/client — lọc theo user trong token).</summary>
    Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hồ sơ của tài khoản đăng nhập, được theo dõi để sửa (kích hoạt). Không lọc theo coach: quyền đến từ mã kích hoạt đã băm.
    /// </summary>
    Task<Student?> GetTrackedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// @BR-f02-ho-so-hoc-vien-023: mọi hồ sơ của coach ở mọi trạng thái, tạo sau đứng trước.
    /// @BR-f02-ho-so-hoc-vien-024: <paramref name="normalizedQuery"/> (đã qua SearchText.Normalize) rỗng = không lọc.
    /// </summary>
    Task<IReadOnlyList<StudentSummary>> ListAsync(Guid coachId, string normalizedQuery, CancellationToken cancellationToken = default);

    void Add(Student student);
}
