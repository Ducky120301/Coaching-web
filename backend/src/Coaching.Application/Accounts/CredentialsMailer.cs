using Coaching.Application.Abstractions;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Accounts;

/// <summary>
/// Đặt mật khẩu tạm mới + hủy mã kích hoạt đang có + gửi thư thông tin đăng nhập. Dùng chung cho gửi / gửi lại (Task 4)
/// và sửa email của hồ sơ đã gửi (Task 6). Gọi SAU Student.RecordCredentialsSent / ChangeEmail và SaveChanges, trong transaction.
/// </summary>
public sealed class CredentialsMailer(
    IAccountCredentials credentials,
    IAccountTokens tokens,
    IAccountEmails emails,
    IEmailSender sender)
{
    public async Task<Result> SendAsync(Student student, CancellationToken ct)
    {
        // @BR-f01-p1-tai-khoan-hoc-vien-003 @BR-f01-p1-tai-khoan-hoc-vien-005: mỗi lần gửi một mật khẩu tạm mới, mật khẩu cũ mất hiệu lực
        var temporaryPassword = await credentials.SetTemporaryPasswordAsync(student.UserId, ct);
        // @BR-f01-p1-tai-khoan-hoc-vien-005: đang đặt mật khẩu riêng bằng mật khẩu tạm cũ → mã kích hoạt cũ bị hủy
        await tokens.RevokeAllAsync(student.UserId, AccountTokenPurpose.Activation, ct);
        return await AccountMail.SendAsync(sender,
            emails.Credentials(student.Email.Value, student.FullName, temporaryPassword, student.TempPasswordExpiresAt!.Value), ct);
    }
}
