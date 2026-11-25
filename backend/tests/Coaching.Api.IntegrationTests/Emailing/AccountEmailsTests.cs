using Coaching.Application.Accounts;
using Coaching.Infrastructure.Emailing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Coaching.Api.IntegrationTests.Emailing;

[Collection(ApiCollection.Name)]
public sealed class AccountEmailsTests(ApiFactory factory)
{
    private static AccountEmails Emails(string? baseUrl) =>
        new(Options.Create(new AppOptions { ClientBaseUrl = baseUrl }));

    // @ADR-f01-p1-tai-khoan-hoc-vien-008: địa chỉ đăng nhập lấy từ App:ClientBaseUrl; hạn 72 giờ ghi theo giờ VN
    [Fact]
    public void Credentials_email_has_login_url_account_and_temp_password()
    {
        var expiresAt = new DateTimeOffset(2026, 11, 28, 2, 0, 0, TimeSpan.Zero); // 09:00 28/11 giờ VN

        var result = Emails("https://hv.duobexiuuu.vn/").Credentials("lan@gmail.com", "Lan", "K7m-PqR4-tW9x", expiresAt);

        Assert.True(result.IsSuccess);
        var mail = result.Value;
        Assert.Equal("lan@gmail.com", mail.To);
        Assert.Equal("Thông tin đăng nhập Duobexiuuu Coaching", mail.Subject);
        Assert.Contains("Chào Lan,", mail.TextBody);
        Assert.Contains("https://hv.duobexiuuu.vn/login", mail.TextBody);
        Assert.DoesNotContain("vn//login", mail.TextBody);
        Assert.Contains("Email: lan@gmail.com", mail.TextBody);
        Assert.Contains("Mật khẩu tạm: K7m-PqR4-tW9x", mail.TextBody);
        Assert.Contains("Mật khẩu tạm có hiệu lực 72 giờ (đến 09:00 28/11/2026). Lần đầu đăng nhập, bạn sẽ đặt mật khẩu riêng.", mail.TextBody);
        Assert.Contains("Coach Duobexiuuu", mail.TextBody);
        Assert.Contains("https://hv.duobexiuuu.vn/login", mail.HtmlBody);
        Assert.Contains("K7m-PqR4-tW9x", mail.HtmlBody);
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-008: link đặt lại dạng <ClientBaseUrl>/dat-lai-mat-khau#t=<token>
    [Fact]
    public void Password_reset_email_has_fragment_link_and_30_minute_notice()
    {
        var result = Emails("https://hv.duobexiuuu.vn").PasswordReset("lan@gmail.com", "Lan", "abc_DEF-123");

        Assert.True(result.IsSuccess);
        var mail = result.Value;
        Assert.Equal("Đặt lại mật khẩu Duobexiuuu Coaching", mail.Subject);
        Assert.Contains("https://hv.duobexiuuu.vn/dat-lai-mat-khau#t=abc_DEF-123", mail.TextBody);
        Assert.Contains("Link có hiệu lực 30 phút và chỉ dùng được một lần. Nếu bạn không yêu cầu, hãy bỏ qua email này.", mail.TextBody);
        Assert.Contains("https://hv.duobexiuuu.vn/dat-lai-mat-khau#t=abc_DEF-123", mail.HtmlBody);
    }

    // @NFR-f01-p1-tai-khoan-hoc-vien-003 @ADR-f01-p1-tai-khoan-hoc-vien-008: thiếu/hỏng cấu hình → không tạo thư, báo lỗi rõ
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("hv.duobexiuuu.vn")]
    [InlineData("ftp://hv.duobexiuuu.vn")]
    public void Missing_or_invalid_client_base_url_returns_client_url_missing(string? baseUrl)
    {
        var emails = Emails(baseUrl);

        Assert.Equal(AccountErrors.ClientUrlMissing,
            emails.Credentials("lan@gmail.com", "Lan", "K7m-PqR4-tW9x", DateTimeOffset.UnixEpoch).Error);
        Assert.Equal(AccountErrors.ClientUrlMissing, emails.PasswordReset("lan@gmail.com", "Lan", "abc").Error);
    }

    // @NFR-f01-p1-tai-khoan-hoc-vien-003: API vẫn khởi động khi thiếu App:ClientBaseUrl; IAccountEmails trả 503 client_url_missing
    [Fact]
    public async Task Host_without_client_base_url_starts_and_reports_client_url_missing()
    {
        await using var host = factory.WithWebHostBuilder(b => b.UseSetting("App:ClientBaseUrl", ""));
        await using var scope = host.Services.CreateAsyncScope();

        var result = scope.ServiceProvider.GetRequiredService<IAccountEmails>()
            .PasswordReset("lan@gmail.com", "Lan", "abc");

        Assert.Equal("account.client_url_missing", result.Error!.Code);
        Assert.Equal("Chưa cấu hình địa chỉ web học viên nên chưa gửi được email.", result.Error.Message);
        Assert.Equal(Coaching.Application.Common.ErrorKind.Unavailable, result.Error.Kind);
    }
}
