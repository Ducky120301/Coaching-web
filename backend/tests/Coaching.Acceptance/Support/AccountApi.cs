using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Coaching.Application.Auth;
using Coaching.Application.Students;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Emailing;
using Coaching.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Acceptance.Support;

/// <summary>
/// Tài khoản học viên trong scenario F01-P1: hồ sơ "học viên đó", gửi thông tin đăng nhập, đọc thư capture
/// (Email:Provider=Capture → EmailOutbox singleton trong DI), đăng nhập bằng mật khẩu tạm.
/// </summary>
public sealed partial class AccountApi(AcceptanceContext ctx, StudentApi api)
{
    /// <summary>Họ tên của "học viên đó" trong mọi scenario tài khoản.</summary>
    public const string StudentName = "Học viên tài khoản";

    public const string DefaultOwnPassword = "Coach2025";

    public const string CredentialsSubject = "Thông tin đăng nhập Duobexiuuu Coaching";

    /// <summary>Email hiện tại theo họ tên hồ sơ (cập nhật khi gửi / sửa email thành công).</summary>
    public Dictionary<string, string> Emails { get; } = new();

    /// <summary>Mật khẩu tạm của các lần gửi THÀNH CÔNG, theo thứ tự (đọc từ thư capture).</summary>
    public List<string> TempPasswords { get; } = [];

    /// <summary>"mật khẩu tạm đó" của scenario.</summary>
    public string? FocusTemp { get; set; }

    /// <summary>Mã kích hoạt của lần đăng nhập bằng mật khẩu tạm gần nhất.</summary>
    public string? ActivationToken { get; private set; }

    /// <summary>Mật khẩu riêng hiện tại của học viên.</summary>
    public string? OwnPassword { get; set; }

    /// <summary>Số thư cùng loại tới học viên ngay trước thao tác gửi gần nhất.</summary>
    public int MailCountBefore { get; private set; }

    /// <summary>Số dòng lịch sử tài khoản trước thao tác đang kiểm.</summary>
    public int HistoryBefore { get; set; }

    public string SentBody { get; set; } = string.Empty;

    public string LastBody { get; private set; } = string.Empty;

    public StudentDetailDto? LastDetail { get; private set; }

    public EmailOutbox Outbox => ctx.Host.Services.GetRequiredService<EmailOutbox>();

    public string Email(string name = StudentName) => Emails[name];

    public Guid Id(string name = StudentName) => api.Ids[name];

    public IReadOnlyList<CapturedEmail> Mails(string to, string subject) =>
        Outbox.All.Where(m => string.Equals(m.To, to.Trim(), StringComparison.OrdinalIgnoreCase) && m.Subject == subject).ToList();

    public IReadOnlyList<CapturedEmail> CredentialMails(string? to = null) => Mails(to ?? Email(), CredentialsSubject);

    /// <summary>Hồ sơ đủ mục bắt buộc (mặc định), tài khoản Chờ kích hoạt, chưa gửi lần nào.</summary>
    public async Task<StudentDetailDto> CreateAsync(string name = StudentName, string? email = null,
        RegistrationInput? registration = null)
    {
        await api.LoginCoachAsync();
        var detail = await api.CreateOkAsync(name, email ?? StudentApi.NewEmail(), registration ?? RegistrationSamples.Complete());
        Emails[name] = detail.Email;
        return detail;
    }

    public async Task<HttpResponseMessage> TrySendCredentialsAsync(string name = StudentName)
    {
        await api.LoginCoachAsync(); // đồng hồ có thể đã tua quá hạn access token
        MailCountBefore = CredentialMails(Emails[name]).Count;
        var response = await ctx.SendJsonAsync(HttpMethod.Post, $"/api/admin/students/{api.Ids[name]}/credentials", new { },
            api.AdminToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var detail = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
            Emails[name] = detail.Email;
            TempPasswords.Add(TempPassword(CredentialMails(detail.Email)[^1]));
        }

        return response;
    }

    public async Task SendOkAsync(string name = StudentName)
    {
        Assert.Equal(HttpStatusCode.OK, (await TrySendCredentialsAsync(name)).StatusCode);
        FocusTemp = TempPasswords[^1];
    }

    /// <summary>Tạo hồ sơ, gửi thông tin đăng nhập rồi tua đồng hồ <paramref name="elapsed"/>.</summary>
    public async Task SendThenWaitAsync(TimeSpan elapsed)
    {
        await CreateAsync();
        await SendOkAsync();
        ctx.Clock.Advance(elapsed);
    }

    /// <summary>Coach xem hồ sơ: nhớ cả body thô (để kiểm không có mật khẩu) và DTO.</summary>
    public async Task<StudentDetailDto> ViewAsync(string name = StudentName)
    {
        await api.LoginCoachAsync();
        var response = await ctx.GetAsync($"/api/admin/students/{api.Ids[name]}", api.AdminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        LastBody = await response.Content.ReadAsStringAsync();
        LastDetail = JsonSerializer.Deserialize<StudentDetailDto>(LastBody, JsonSerializerOptions.Web)!;
        return LastDetail;
    }

    /// <summary>Đăng nhập bằng email của "học viên đó" (hoặc <paramref name="email"/>); nhớ mã kích hoạt hoặc phiên.</summary>
    public async Task<HttpResponseMessage> StudentLoginAsync(string password, string app = "client", string? email = null)
    {
        var response = await ctx.LoginAsync(email ?? Email(), password, app);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (json.RootElement.TryGetProperty("activationRequired", out var required) && required.GetBoolean())
                ActivationToken = json.RootElement.GetProperty("activationToken").GetString();
            else
                ctx.Tokens = json.RootElement.Deserialize<AuthTokens>(JsonSerializerOptions.Web);
        }

        return response;
    }

    /// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-002: 200 có mã kích hoạt, KHÔNG có phiên.</summary>
    public async Task AssertActivationChallengeAsync()
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        using var json = JsonDocument.Parse(await ctx.LastResponse.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("activationRequired").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("activationToken").GetString()));
        Assert.False(json.RootElement.TryGetProperty("accessToken", out _));
        Assert.False(json.RootElement.TryGetProperty("refreshToken", out _));
    }

    public async Task AssertProblemTitleAsync(string title)
    {
        var problem = await ctx.LastResponse!.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(title, problem!.Title);
    }

    /// <summary>Học viên Đã kích hoạt qua luồng thật: coach gửi → đăng nhập bằng mật khẩu tạm → đặt mật khẩu riêng.</summary>
    public async Task<StudentDetailDto> CreateActivatedAsync(string name = StudentName, string? email = null,
        string password = DefaultOwnPassword)
    {
        var detail = await CreateAsync(name, email);
        await SendOkAsync(name);
        await StudentLoginAsync(TempPasswords[^1], email: Emails[name]);
        await AssertActivationChallengeAsync();
        Assert.Equal(HttpStatusCode.OK, (await TryActivateAsync(password, password)).StatusCode);
        return detail;
    }

    /// <summary>Hồ sơ mới, coach gửi, học viên đăng nhập bằng mật khẩu tạm → đang ở bước đặt mật khẩu riêng.</summary>
    public async Task ReachActivationStepAsync()
    {
        await CreateAsync();
        await SendOkAsync();
        await StudentLoginAsync(TempPasswords[^1]);
        await AssertActivationChallengeAsync();
    }

    /// <summary>POST /api/auth/activate bằng mã kích hoạt của lần đăng nhập gần nhất.</summary>
    public async Task<HttpResponseMessage> TryActivateAsync(string password, string confirmPassword)
    {
        var response = await ctx.PostJsonAsync("/api/auth/activate",
            new { activationToken = ActivationToken, password, confirmPassword });
        if (response.StatusCode == HttpStatusCode.OK)
            OwnPassword = password;
        return response;
    }

    /// <summary>Email của hồ sơ ngay trước lần sửa gần nhất.</summary>
    public string? PreviousEmail { get; private set; }

    /// <summary>PUT email kèm version vừa đọc; ghi lại email cũ, số dòng lịch sử và mật khẩu tạm mới (nếu có thư mới).</summary>
    public async Task<HttpResponseMessage> TryChangeEmailAsync(string email, string name = StudentName)
    {
        var current = await ViewAsync(name);
        PreviousEmail = current.Email;
        HistoryBefore = current.Account.History.Count;
        var credentialMailsBefore = Outbox.All.Count(m => m.Subject == CredentialsSubject);
        var response = await ctx.SendJsonAsync(HttpMethod.Put, $"/api/admin/students/{api.Ids[name]}/email",
            new { email, version = current.Version }, api.AdminToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var detail = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
            Emails[name] = detail.Email;
            if (Outbox.All.Count(m => m.Subject == CredentialsSubject) > credentialMailsBefore)
                TempPasswords.Add(TempPassword(CredentialMails(detail.Email)[^1]));
        }

        return response;
    }

    /// <summary>Phản hồi gần nhất là phiên học viên (AuthTokens) gọi được /api/client.</summary>
    public async Task AssertStudentSessionAsync()
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        var tokens = (await ctx.LastResponse.Content.ReadFromJsonAsync<AuthTokens>())!;
        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));
        ctx.Tokens = tokens;
        var me = await ctx.GetAsync("/api/client/me", tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    /// <summary>@BR-f01-p1-tai-khoan-hoc-vien-026: câu trạng thái tài khoản coach thấy, theo giờ Việt Nam.</summary>
    public static string StateLabel(AccountInfoDto account) => account.State switch
    {
        "NotSent" => "Chưa gửi thông tin đăng nhập",
        "Sent" => $"Đã gửi lúc {Vietnam(account.CredentialsSentAt!.Value, "dd'/'MM HH':'mm")}, "
            + $"mật khẩu tạm còn hạn đến {Vietnam(account.TempPasswordExpiresAt!.Value, "dd'/'MM HH':'mm")}",
        "TempPasswordExpired" => "Mật khẩu tạm đã hết hạn",
        "Activated" => $"Đã kích hoạt ngày {Vietnam(account.ActivatedAt!.Value, "dd'/'MM'/'yyyy")}",
        _ => throw new InvalidOperationException($"Trạng thái tài khoản lạ: {account.State}"),
    };

    private static string Vietnam(DateTimeOffset instant, string format) =>
        instant.ToOffset(StepText.VietnamOffset).ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Mật khẩu tạm trong thư (dòng "Mật khẩu tạm: …" của AccountEmails).</summary>
    public static string TempPassword(CapturedEmail mail) =>
        TempPasswordPattern().Match(mail.TextBody) is { Success: true } match
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"Thư '{mail.Subject}' tới {mail.To} không có dòng mật khẩu tạm.");

    /// <summary>Mọi địa chỉ http(s) trong thân thư (bỏ dấu câu dính cuối).</summary>
    public static IReadOnlyList<string> Urls(string body) =>
        UrlPattern().Matches(body).Select(m => m.Value.TrimEnd('.', ',', ')')).ToList();

    [GeneratedRegex(@"Mật khẩu tạm:\s*(\S+)")]
    private static partial Regex TempPasswordPattern();

    [GeneratedRegex(@"https?://[^\s""'<>]+")]
    private static partial Regex UrlPattern();
}
