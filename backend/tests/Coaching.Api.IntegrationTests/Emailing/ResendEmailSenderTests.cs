using System.Net;
using Coaching.Application.Abstractions;
using Coaching.Infrastructure.Emailing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Coaching.Api.IntegrationTests.Emailing;

public sealed class ResendEmailSenderTests
{
    // @NFR-f01-p1-tai-khoan-hoc-vien-001: lỗi nhà cung cấp phải để lại dấu vết chẩn đoán nhưng không lộ thân thư / mật khẩu tạm
    [Fact]
    public async Task Provider_failure_logs_warning_with_recipient_and_subject_but_never_body()
    {
        var logger = new SpyLogger();
        var sender = new ResendEmailSender(
            new HttpClient(new StubHandler(HttpStatusCode.ServiceUnavailable, "echo TEMP-SECRET-123")) { BaseAddress = new Uri("https://api.resend.test/") },
            Options.Create(new EmailOptions { ResendApiKey = "key", From = "a@b.c" }),
            logger);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync(new EmailMessage("hv@test.local", "Tài khoản của bạn", "<p>TEMP-SECRET-123</p>", "TEMP-SECRET-123")));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("hv@test.local", entry.Text);
        Assert.Contains("Tài khoản của bạn", entry.Text);
        Assert.Contains("503", entry.Text);
        Assert.DoesNotContain("TEMP-SECRET-123", entry.Text);
        Assert.DoesNotContain("TEMP-SECRET-123", ex.Message);
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }

    private sealed class SpyLogger : ILogger<ResendEmailSender>
    {
        public List<(LogLevel Level, string Text)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
