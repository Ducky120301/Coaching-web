using Coaching.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Persistence.Repositories;

internal sealed class StudentRepository(AppDbContext db) : IStudentRepository
{
    public Task<Student?> GetAsync(Guid id, Guid coachId, CancellationToken cancellationToken = default) =>
        db.Students.SingleOrDefaultAsync(s => s.Id == id && s.CoachId == coachId, cancellationToken);

    public void Add(Student student) => db.Students.Add(student);
}
