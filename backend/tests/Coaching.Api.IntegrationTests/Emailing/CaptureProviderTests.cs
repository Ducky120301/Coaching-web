using System.Net;
using System.Text.Json;
using Coaching.Application.Abstractions;
using Coaching.Infrastructure.Emailing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Emailing;

[Collection(ApiCollection.Name)]
public sealed class CaptureProviderTests(ApiFactory factory)
{
    private static string NewAddress() => $"mail-{Guid.NewGuid():N}@test.local";

    private async Task SendAsync(string to, string subject)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IEmailSender>()
            .SendAsync(new EmailMessage(to, subject, $"<p>{subject}</p>", $"Thân thư {subject}"));
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-007: chốt 1 — Capture ở Production thì API không khởi động
    [Fact]
    public void Capture_provider_in_production_stops_startup()
    {
        var ex = Assert.ThrowsAny<Exception>(() => factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.UseSetting("Email:Provider", "Capture");
        }).CreateClient());

        Assert.Contains("Email:Provider=Capture", ex.ToString());
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-007: chốt 2 — provider khác Capture thì không có endpoint đọc thư
    [Theory]
    [InlineData("Log")]
    [InlineData("Resend")]
    public async Task Test_emails_endpoint_is_absent_for_other_providers(string provider)
    {
        await using var host = factory.WithWebHostBuilder(b => b.UseSetting("Email:Provider", provider));

        var response = await host.CreateClient().GetAsync("/api/test/emails?to=a@test.local");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(host.Services.GetService<EmailOutbox>());
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-007: e2e đọc thư theo người nhận, không cần đăng nhập, không trả HTML
    [Fact]
    public async Task Test_emails_endpoint_lists_captured_mail_of_recipient()
    {
        var to = NewAddress();
        await SendAsync(to, "Thư một");
        await SendAsync(NewAddress(), "Thư người khác");
        await SendAsync(to.ToUpperInvariant(), "Thư hai");

        var response = await factory.CreateClient().GetAsync($"/api/test/emails?to={Uri.EscapeDataString(to)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var mails = json.RootElement.EnumerateArray().ToList();
        Assert.Equal(new[] { "Thư một", "Thư hai" }, mails.Select(m => m.GetProperty("subject").GetString()).ToList());
        Assert.Equal("Thân thư Thư một", mails[0].GetProperty("textBody").GetString());
        Assert.Equal(to, mails[0].GetProperty("to").GetString());
        Assert.True(mails[0].TryGetProperty("sentAt", out _));
        Assert.False(mails[0].TryGetProperty("htmlBody", out _));
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-007: FailNext mô phỏng dịch vụ email lỗi đúng một lần
    [Fact]
    public async Task FailNext_fails_exactly_one_send_and_captures_nothing()
    {
        var to = NewAddress();
        factory.Outbox.FailNext = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => SendAsync(to, "Lỗi"));
        Assert.False(factory.Outbox.FailNext);
        Assert.DoesNotContain(factory.Outbox.All, m => m.To == to);

        await SendAsync(to, "Lần sau");
        var mail = await factory.Outbox.WaitForAsync(to);
        Assert.Equal("Lần sau", mail.Subject);
    }
}
