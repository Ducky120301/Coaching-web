using System.Threading.Channels;
using Coaching.Application.Abstractions;

namespace Coaching.Infrastructure.Emailing;

/// <summary>Hàng đợi email trong process (singleton). Thư trong hàng đợi mất khi process khởi động lại.</summary>
internal sealed class QueuedEmailSender : IEmailQueue
{
    private readonly Channel<EmailMessage> _channel =
        Channel.CreateUnbounded<EmailMessage>(new UnboundedChannelOptions { SingleReader = true });

    internal ChannelReader<EmailMessage> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _channel.Writer.WriteAsync(message, ct);
    }
}
