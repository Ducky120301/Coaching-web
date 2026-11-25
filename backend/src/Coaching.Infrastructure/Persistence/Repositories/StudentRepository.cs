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

    public Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Students.AsNoTracking().SingleOrDefaultAsync(s => s.UserId == userId, cancellationToken);

    public Task<Student?> GetTrackedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Students.SingleOrDefaultAsync(s => s.UserId == userId, cancellationToken);

    // @BR-f02-ho-so-hoc-vien-023 @BR-f02-ho-so-hoc-vien-024 @ADR-f02-ho-so-hoc-vien-003:
    // 1 truy vấn theo index (CoachId, CreatedAt) + LIKE '%q%' trên SearchKey (EF escape ký tự đặc biệt của LIKE)
    public async Task<IReadOnlyList<StudentSummary>> ListAsync(Guid coachId, string normalizedQuery,
        CancellationToken cancellationToken = default)
    {
        var query = db.Students.AsNoTracking().Where(s => s.CoachId == coachId);
        if (normalizedQuery.Length > 0)
            query = query.Where(s => s.SearchKey.Contains(normalizedQuery));

        var rows = await query
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.Id) // Guid v7: thứ tự ổn định khi cùng CreatedAt
            .Select(s => new { s.Id, s.FullName, s.Email, s.CoachingStatus, s.AccountStatus, s.CreatedAt })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new StudentSummary(r.Id, r.FullName, r.Email.Value, r.CoachingStatus, r.AccountStatus, r.CreatedAt))
            .ToList();
    }

    public void Add(Student student) => db.Students.Add(student);
}
