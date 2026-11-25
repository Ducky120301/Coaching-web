namespace Coaching.Domain.Students;

public interface IStudentRepository
{
    /// <summary>Hồ sơ thuộc coach; hồ sơ của coach khác coi như không tồn tại.</summary>
    Task<Student?> GetAsync(Guid id, Guid coachId, CancellationToken cancellationToken = default);

    void Add(Student student);
}
