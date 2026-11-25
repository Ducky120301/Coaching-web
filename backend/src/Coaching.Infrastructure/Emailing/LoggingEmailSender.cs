using Coaching.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Coaching.Infrastructure.Emailing;

/// <summary>Không gửi email thật, chỉ ghi log.</summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger, IHostEnvironment environment) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        // @NFR-f01-p1-tai-khoan-hoc-vien-001: thân thư có mật khẩu tạm / link đặt lại → Production chỉ ghi người nhận + tiêu đề
        if (environment.IsProduction())
            logger.LogInformation("[Email:Log] To={To} Subject={Subject}", message.To, message.Subject);
        else
            logger.LogInformation("[Email:Log] To={To} Subject={Subject}\n{Body}",
                message.To, message.Subject, message.TextBody ?? message.HtmlBody);
        return Task.CompletedTask;
    }
}
