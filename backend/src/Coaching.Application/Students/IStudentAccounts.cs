using Coaching.Application.Common;
using Coaching.Domain.Common;

namespace Coaching.Application.Students;

/// <summary>
/// @ADR-f02-ho-so-hoc-vien-001: tài khoản đăng nhập của học viên (Identity), tạo "Chờ kích hoạt": role Student,
/// không mật khẩu → chưa đăng nhập được. Email trùng với bất kỳ tài khoản nào (kể cả coach) → StudentErrors.EmailTaken.
/// </summary>
public interface IStudentAccounts
{
    Task<Result> CreatePendingAsync(Guid userId, Email email, string fullName, CancellationToken cancellationToken = default);
}
