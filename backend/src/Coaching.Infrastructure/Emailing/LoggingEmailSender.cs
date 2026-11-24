using Coaching.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Coaching.Infrastructure.Emailing;

/// <summary>Dùng cho dev/test: không gửi email thật, chỉ ghi log.</summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[Email:Log] To={To} Subject={Subject}\n{Body}",
            message.To, message.Subject, message.TextBody ?? message.HtmlBody);
        return Task.CompletedTask;
    }
}
