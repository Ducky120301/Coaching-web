using Coaching.Application.Abstractions;

namespace Coaching.Infrastructure.Emailing;

/// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-007: Email:Provider=Capture — không gửi ra ngoài, lưu vào EmailOutbox.</summary>
internal sealed class CapturingEmailSender(EmailOutbox outbox, IClock clock) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (outbox.TryConsumeFailure())
            throw new InvalidOperationException("Email:Provider=Capture mô phỏng dịch vụ email lỗi (FailNext).");

        outbox.Add(new CapturedEmail(message.To, message.Subject, message.TextBody ?? string.Empty, message.HtmlBody, clock.UtcNow));
        return Task.CompletedTask;
    }
}
