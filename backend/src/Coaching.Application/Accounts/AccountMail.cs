using Coaching.Application.Abstractions;
using Coaching.Application.Common;

namespace Coaching.Application.Accounts;

internal static class AccountMail
{
    /// <summary>
    /// @ADR-f01-p1-tai-khoan-hoc-vien-004: gửi là bước cuối trước commit; lỗi soạn thư (thiếu App:ClientBaseUrl) hoặc lỗi dịch vụ
    /// email thành Result lỗi để use case trả về → ExecuteInTransactionAsync rollback (không thành 500).
    /// </summary>
    public static async Task<Result> SendAsync(IEmailSender sender, Result<EmailMessage> message, CancellationToken ct)
    {
        if (!message.IsSuccess)
            return Result.Failure(message.Error!);
        try
        {
            await sender.SendAsync(message.Value, ct);
            return Result.Success();
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return Result.Failure(AccountErrors.EmailSendFailed); // @BR-f01-p1-tai-khoan-hoc-vien-006
        }
    }
}
