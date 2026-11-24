using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Coaching.Infrastructure.Emailing;

/// <summary>Gửi email qua Resend REST API (https://resend.com/docs/api-reference/emails/send-email).</summary>
internal sealed class ResendEmailSender(HttpClient httpClient, IOptions<EmailOptions> options) : IEmailSender
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
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Resend trả lỗi {(int)response.StatusCode}: {body}");
        }
    }
}
