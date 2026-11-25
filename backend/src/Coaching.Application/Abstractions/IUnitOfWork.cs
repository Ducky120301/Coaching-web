using Coaching.Application.Common;

namespace Coaching.Application.Abstractions;

/// <summary>Lưu mọi thay đổi của một use case trong một transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// @ADR-f02-ho-so-hoc-vien-001: chạy <paramref name="operation"/> trong transaction tường minh (cho thao tác tự
    /// SaveChanges như UserManager). Commit khi kết quả thành công; rollback khi thất bại hoặc có exception.
    /// </summary>
    Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken = default);
}
