using Coaching.Application.Abstractions;
using Coaching.Infrastructure.Emailing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Coaching.Api.IntegrationTests.Emailing;

public sealed class LoggingEmailSenderTests
{
    private static readonly EmailMessage Mail =
        new("lan@gmail.com", "Thông tin đăng nhập Duobexiuuu Coaching", "<p>Mật khẩu tạm: K7m-PqR4</p>", "Mật khẩu tạm: K7m-PqR4");

    // @NFR-f01-p1-tai-khoan-hoc-vien-001: ở Production chỉ ghi người nhận + tiêu đề, không ghi thân thư
    [Fact]
    public async Task Production_logs_recipient_and_subject_only()
    {
        var logger = new ListLogger();

        await new LoggingEmailSender(logger, new Env(Environments.Production)).SendAsync(Mail);

        var line = Assert.Single(logger.Messages);
        Assert.Contains("lan@gmail.com", line);
        Assert.Contains("Thông tin đăng nhập Duobexiuuu Coaching", line);
        Assert.DoesNotContain("K7m-PqR4", line);
    }

    // @NFR-f01-p1-tai-khoan-hoc-vien-001: môi trường thử ghi cả thân thư để kiểm tra
    [Fact]
    public async Task Non_production_logs_body_for_testing()
    {
        var logger = new ListLogger();

        await new LoggingEmailSender(logger, new Env(Environments.Development)).SendAsync(Mail);

        Assert.Contains("K7m-PqR4", Assert.Single(logger.Messages));
    }

    private sealed class ListLogger : ILogger<LoggingEmailSender>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }

    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "Coaching.Api";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
