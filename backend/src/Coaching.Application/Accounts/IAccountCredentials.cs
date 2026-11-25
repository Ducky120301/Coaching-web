namespace Coaching.Application.Accounts;

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-001: mật khẩu (tạm hoặc riêng) là mật khẩu Identity của tài khoản — chỉ lưu dạng hash.
/// Cài đặt bằng UserManager ở Infrastructure; các lệnh tự SaveChanges nên luôn gọi trong IUnitOfWork.ExecuteInTransactionAsync.
/// </summary>
public interface IAccountCredentials
{
    /// <summary>Sinh mật khẩu tạm mới, thay mật khẩu hiện tại, trả mật khẩu gốc để đưa vào email (không lưu ở đâu khác).</summary>
    Task<string> SetTemporaryPasswordAsync(Guid userId, CancellationToken ct = default);
}
