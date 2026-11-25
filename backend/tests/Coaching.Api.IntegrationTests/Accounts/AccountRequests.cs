using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Coaching.Application.Students;
using Coaching.Infrastructure.Emailing;
using Coaching.TestSupport;

namespace Coaching.Api.IntegrationTests.Accounts;

internal static partial class AccountRequests
{
    public const string CredentialsSubject = "Thông tin đăng nhập Duobexiuuu Coaching";

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

    [GeneratedRegex(@"Mật khẩu tạm:\s*(\S+)")]
    private static partial Regex TempPasswordPattern();
}
