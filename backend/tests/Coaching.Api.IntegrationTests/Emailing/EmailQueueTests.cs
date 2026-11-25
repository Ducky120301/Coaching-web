using Coaching.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Emailing;

[Collection(ApiCollection.Name)]
public sealed class EmailQueueTests(ApiFactory factory)
{
    private static string NewAddress() => $"queue-{Guid.NewGuid():N}@test.local";

    private ValueTask EnqueueAsync(string to, string subject) =>
        factory.Services.GetRequiredService<IEmailQueue>().EnqueueAsync(new EmailMessage(to, subject, "<p>x</p>", "x"));

    // Hàng đợi trong process: thư được gửi nền qua IEmailSender đang cấu hình
    [Fact]
    public async Task Enqueued_mail_is_sent_in_background()
    {
        var to = NewAddress();

        await EnqueueAsync(to, "Thư hàng đợi");

        var mail = await factory.Outbox.WaitForAsync(to);
        Assert.Equal("Thư hàng đợi", mail.Subject);
    }

    // Gửi lỗi chỉ mất thư đó (log Warning), worker vẫn chạy tiếp
    [Fact]
    public async Task Failed_mail_is_dropped_and_worker_keeps_sending()
    {
        var failed = NewAddress();
        var next = NewAddress();
        factory.Outbox.FailNext = true;

        await EnqueueAsync(failed, "Sẽ lỗi");
        await EnqueueAsync(next, "Vẫn gửi");

        Assert.Equal("Vẫn gửi", (await factory.Outbox.WaitForAsync(next)).Subject);
        Assert.DoesNotContain(factory.Outbox.All, m => m.To == failed);
    }
}
