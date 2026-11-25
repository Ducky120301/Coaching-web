namespace Coaching.Application.Accounts;

public enum AccountTokenPurpose
{
    Activation,
    PasswordReset,
}

/// <summary>Ai khiến token được phát: đăng nhập bằng mật khẩu tạm, coach bấm gửi, học viên tự xin.</summary>
public enum AccountTokenSource
{
    Login,
    Coach,
    Self,
}

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-003: mã kích hoạt + link đặt lại. Chỉ lưu SHA-256; dùng một lần;
/// phát token mới thì token còn hiệu lực cùng user + mục đích bị hủy; hết hạn khi now &gt;= ExpiresAt.
/// Ghi ngay vào DB — gọi trong IUnitOfWork.ExecuteInTransactionAsync nếu cần đi cùng thay đổi khác.
/// </summary>
public interface IAccountTokens
{
    /// <summary>Hủy mọi token còn hiệu lực cùng user + mục đích, phát token mới; trả token gốc (32 byte ngẫu nhiên, base64url).</summary>
    Task<string> IssueAsync(Guid userId, AccountTokenPurpose purpose, AccountTokenSource source, DateTimeOffset expiresAt,
        CancellationToken ct = default);

    /// <summary>UserId nếu token đúng mục đích, chưa tiêu, chưa hủy và now &lt; ExpiresAt; ngược lại null. Không tiêu token.</summary>
    Task<Guid?> FindValidAsync(string? rawToken, AccountTokenPurpose purpose, CancellationToken ct = default);

    /// <summary>Đánh dấu đã dùng (lần đầu); gọi lại không đổi gì.</summary>
    Task ConsumeAsync(string rawToken, CancellationToken ct = default);

    Task RevokeAllAsync(Guid userId, AccountTokenPurpose purpose, CancellationToken ct = default);

    /// <summary>Thời điểm phát token gần nhất theo user + mục đích + nguồn; null nếu chưa phát lần nào.</summary>
    Task<DateTimeOffset?> LastIssuedAtAsync(Guid userId, AccountTokenPurpose purpose, AccountTokenSource source,
        CancellationToken ct = default);
}
