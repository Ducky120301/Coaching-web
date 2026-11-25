using Coaching.Application.Common;
using Coaching.Domain.Common;

namespace Coaching.Application.Accounts;

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-001: mật khẩu (tạm hoặc riêng) là mật khẩu Identity của tài khoản — chỉ lưu dạng hash.
/// Cài đặt bằng UserManager ở Infrastructure; các lệnh tự SaveChanges nên luôn gọi trong IUnitOfWork.ExecuteInTransactionAsync.
/// </summary>
public interface IAccountCredentials
{
    /// <summary>Sinh mật khẩu tạm mới, thay mật khẩu hiện tại, trả mật khẩu gốc để đưa vào email (không lưu ở đâu khác).</summary>
    Task<string> SetTemporaryPasswordAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Đặt mật khẩu riêng đúng từng ký tự (đã qua PasswordPolicy); mật khẩu cũ (kể cả mật khẩu tạm) mất hiệu lực.</summary>
    Task SetOwnPasswordAsync(Guid userId, string password, CancellationToken ct = default);

    /// <summary>
    /// Đổi email đăng nhập (Email + UserName) của tài khoản. Trùng với bất kỳ tài khoản nào (kể cả coach, không phân biệt
    /// hoa/thường) → StudentErrors.EmailTaken.
    /// </summary>
    Task<Result> ChangeEmailAsync(Guid userId, Email email, CancellationToken ct = default);

    /// <summary>Id tài khoản có email này (so khớp đã chuẩn hóa, không phân biệt hoa/thường); null nếu không có.</summary>
    Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken ct = default);
}
