using Coaching.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Persistence.Repositories;

internal sealed class StudentRepository(AppDbContext db) : IStudentRepository
{
    public Task<Student?> GetAsync(Guid id, Guid coachId, CancellationToken cancellationToken = default) =>
        db.Students.SingleOrDefaultAsync(s => s.Id == id && s.CoachId == coachId, cancellationToken);

    public async Task<Student?> GetForUpdateAsync(Guid id, Guid coachId, byte[] expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var student = await GetAsync(id, coachId, cancellationToken);
        if (student is not null)
            db.Entry(student).Property(s => s.Version).OriginalValue = expectedVersion; // @ADR-f02-ho-so-hoc-vien-004
        return student;
    }

    public void Add(Student student) => db.Students.Add(student);
}
