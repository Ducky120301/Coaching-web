namespace Coaching.Application.Abstractions;

public sealed record EmailMessage(string To, string Subject, string HtmlBody, string? TextBody = null);

/// <summary>Gửi email giao dịch (mời tài khoản, đặt lại mật khẩu). Đổi nhà cung cấp chỉ ở Infrastructure.</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
