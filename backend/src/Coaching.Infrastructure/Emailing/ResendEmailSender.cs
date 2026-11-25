using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Coaching.Infrastructure.Emailing;

/// <summary>Gửi email qua Resend REST API (https://resend.com/docs/api-reference/emails/send-email).</summary>
internal sealed class ResendEmailSender(HttpClient httpClient, IOptions<EmailOptions> options, ILogger<ResendEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(new
            {
                from = opts.From,
                to = new[] { message.To },
                subject = message.Subject,
                html = message.HtmlBody,
                text = message.TextBody,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.ResendApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // @NFR-f01-p1-tai-khoan-hoc-vien-001: chỉ log người nhận + tiêu đề + mã trạng thái; không log thân thư/mật khẩu tạm
            // và không đọc body phản hồi (có thể lặp lại nội dung thư).
            logger.LogWarning("[Email:Resend] Gửi thất bại To={To} Subject={Subject} Status={Status}",
                message.To, message.Subject, (int)response.StatusCode);
            throw new InvalidOperationException($"Resend trả lỗi {(int)response.StatusCode}");
        }
    }
}
