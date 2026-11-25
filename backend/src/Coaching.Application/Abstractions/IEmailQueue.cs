namespace Coaching.Application.Abstractions;

/// <summary>Đẩy email vào hàng đợi trong process để gửi nền (người gọi không chờ, không biết kết quả gửi).</summary>
public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default);
}
