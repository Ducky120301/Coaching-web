using Coaching.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Coaching.Infrastructure.Emailing;

/// <summary>Gửi nền từng thư trong hàng đợi qua IEmailSender đang cấu hình. Lỗi → log Warning (không ghi thân thư), gửi tiếp thư sau.</summary>
internal sealed class EmailQueueWorker(QueuedEmailSender queue, IServiceScopeFactory scopes, ILogger<EmailQueueWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IEmailSender>().SendAsync(message, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Gửi email trong hàng đợi lỗi: To={To} Subject={Subject}", message.To, message.Subject);
            }
        }
    }
}
