using Coaching.Application.Abstractions;
using Coaching.Application.Common;

namespace Coaching.Application.Accounts;

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-008: soạn email tài khoản (text + HTML) với địa chỉ web học viên từ cấu hình.
/// Thiếu cấu hình → AccountErrors.ClientUrlMissing, không tạo thư.
/// </summary>
public interface IAccountEmails
{
    Result<EmailMessage> Credentials(string toEmail, string fullName, string temporaryPassword, DateTimeOffset expiresAt);

    Result<EmailMessage> PasswordReset(string toEmail, string fullName, string rawToken);
}
