using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Coaching.Application.Students;
using Coaching.Infrastructure.Emailing;
using Coaching.TestSupport;

namespace Coaching.Api.IntegrationTests.Accounts;

internal static partial class AccountRequests
{
    public const string CredentialsSubject = "Thông tin đăng nhập Duobexiuuu Coaching";

    public const string ResetSubject = "Đặt lại mật khẩu Duobexiuuu Coaching";

    /// <summary>Hồ sơ đủ mục bắt buộc, tài khoản Chờ kích hoạt.</summary>
    public static async Task<StudentDetailDto> CreateCompleteAsync(HttpClient admin, string? email = null)
    {
        var response = await admin.PostAsJsonAsync("/api/admin/students", new
        {
            fullName = "Học viên tài khoản",
            email = email ?? $"s{Guid.NewGuid():N}@test.local",
            registration = RegistrationSamples.Complete(),
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
    }

    public static Task<HttpResponseMessage> SendCredentialsAsync(HttpClient admin, Guid id) =>
        admin.PostAsJsonAsync($"/api/admin/students/{id}/credentials", new { });

    /// <summary>Gửi thông tin đăng nhập thành công; trả mật khẩu tạm đọc từ thư capture mới nhất tới học viên.</summary>
    public static async Task<string> SendCredentialsOkAsync(ApiFactory factory, HttpClient admin, StudentDetailDto student)
    {
        var response = await SendCredentialsAsync(admin, student.Id);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return TempPassword(factory.Outbox.All.Last(m => m.To == student.Email && m.Subject == CredentialsSubject));
    }

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password, string app = "client") =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password, app });

    public static string TempPassword(CapturedEmail mail) =>
        TempPasswordPattern().Match(mail.TextBody) is { Success: true } match
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"Thư tới {mail.To} không có dòng mật khẩu tạm.");

    /// <summary>Hồ sơ mới → gửi thông tin → đăng nhập bằng mật khẩu tạm; trả mật khẩu tạm và mã kích hoạt.</summary>
    public static async Task<(StudentDetailDto Student, string Temp, string ActivationToken)> ReachActivationAsync(ApiFactory factory)
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await CreateCompleteAsync(admin);
        var temp = await SendCredentialsOkAsync(factory, admin, student);
        var login = await LoginAsync(factory.Capture.CreateClient(), student.Email, temp);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return (student, temp, json.RootElement.GetProperty("activationToken").GetString()!);
    }

    public static Task<HttpResponseMessage> ActivateAsync(HttpClient client, string token, string password, string confirmPassword) =>
        client.PostAsJsonAsync("/api/auth/activate", new { activationToken = token, password, confirmPassword });

    /// <summary>Học viên Đã kích hoạt qua luồng thật với mật khẩu riêng <paramref name="password"/>.</summary>
    public static async Task<StudentDetailDto> CreateActivatedAsync(ApiFactory factory, string password = "Coach2025")
    {
        var (student, _, token) = await ReachActivationAsync(factory);
        var response = await ActivateAsync(factory.Capture.CreateClient(), token, password, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return student;
    }

    public static Task<HttpResponseMessage> ChangeEmailAsync(HttpClient admin, Guid id, string email, string version) =>
        admin.PutAsJsonAsync($"/api/admin/students/{id}/email", new { email, version });

    public static Task<HttpResponseMessage> SendResetLinkAsync(HttpClient admin, Guid id) =>
        admin.PostAsJsonAsync($"/api/admin/students/{id}/password-reset-link", new { });

    public static Task<HttpResponseMessage> ForgotAsync(HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

    public static Task<HttpResponseMessage> ResetAsync(HttpClient client, string token, string password, string confirmPassword) =>
        client.PostAsJsonAsync("/api/auth/reset-password", new { token, password, confirmPassword });

    public static string ResetToken(CapturedEmail mail) =>
        ResetTokenPattern().Match(mail.TextBody) is { Success: true } match
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"Thư tới {mail.To} không có link đặt lại.");

    /// <summary>Chờ (tối đa 5 giây) tới khi có ít nhất <paramref name="count"/> thư cùng tiêu đề tới <paramref name="to"/>.</summary>
    public static async Task<IReadOnlyList<CapturedEmail>> WaitForMailsAsync(EmailOutbox outbox, string to, string subject, int count)
    {
        var watch = Stopwatch.StartNew();
        while (true)
        {
            var mails = outbox.All.Where(m => m.To == to && m.Subject == subject).ToList();
            if (mails.Count >= count)
                return mails;
            if (watch.Elapsed > TimeSpan.FromSeconds(5))
                throw new TimeoutException($"Sau 5 giây mới có {mails.Count}/{count} thư '{subject}' tới {to}.");
            await Task.Delay(50);
        }
    }

    [GeneratedRegex(@"/dat-lai-mat-khau#t=([A-Za-z0-9_-]+)")]
    private static partial Regex ResetTokenPattern();

    [GeneratedRegex(@"Mật khẩu tạm:\s*(\S+)")]
    private static partial Regex TempPasswordPattern();
}
