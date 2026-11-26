using Coaching.Application.Abstractions;
using Coaching.Application.Common;
using Coaching.Domain.Coaches;
using Coaching.Domain.Exercises;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Audit;
using Coaching.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    public DbSet<Coach> Coaches => Set<Coach>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AccountToken> AccountTokens => Set<AccountToken>();

    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    public DbSet<ExerciseCategory> ExerciseCategories => Set<ExerciseCategory>();
    public DbSet<Exercise> Exercises => Set<Exercise>();

    // @ADR-f02-ho-so-hoc-vien-001: execution strategy (retry) bọc cả transaction; commit chỉ khi kết quả thành công
    public Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        return Database.CreateExecutionStrategy().ExecuteAsync(async ct =>
        {
            // Retry: clear tracker để entity của lần thất bại trước (AppUser, Student) không bị track trùng key.
            if (++attempt > 1)
                ChangeTracker.Clear();
            await using var transaction = await Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation(ct);
                if (result.IsSuccess)
                {
                    await transaction.CommitAsync(ct);
                    return result;
                }
                await transaction.RollbackAsync(ct);
                // @BR-f02-import-excel-033: unit-of-work thất bại không để lại entity Added (vd AppUser đụng chỉ mục duy nhất)
                // trong context dùng chung — nếu không, mọi SaveChanges sau đó (dòng import kế tiếp) chèn lại và lỗi theo.
                ChangeTracker.Clear();
                return result;
            }
            catch
            {
                ChangeTracker.Clear();
                throw;
            }
        }, cancellationToken);
    }

    // @ADR-f02-ho-so-hoc-vien-004: use case gọi qua IUnitOfWork nhận lỗi của Application, không phụ thuộc EF.
    // Identity (UserManager) gọi SaveChangesAsync công khai nên vẫn tự xử lý DbUpdateConcurrencyException như cũ.
    async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // @BR-f03-thu-vien-bai-tap-006: hai request đua nhau qua bước kiểm trùng → use case trả 409 thay vì 500
                throw new UniqueConstraintException(ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
