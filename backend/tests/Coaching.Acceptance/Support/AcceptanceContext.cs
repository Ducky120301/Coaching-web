using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Abstractions;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Coaching.Acceptance.Support;

/// <summary>Trạng thái của 1 scenario (Reqnroll tạo mới cho mỗi scenario).</summary>
public sealed class AcceptanceContext : IDisposable
{
    public const string AdminEmail = "admin@acceptance.local";
    public const string AdminPassword = "Admin@12345";
    public const string AdminName = "Coach Duobexiuuu";

    /// <summary>App:ClientBaseUrl của scenario — địa chỉ trong email (NFR-f01-p1-tai-khoan-hoc-vien-003).</summary>
    public const string ClientBaseUrl = "http://client.acceptance.local";

    /// <summary>
    /// Mọi scenario bắt đầu lúc 01/11/2026 00:00 UTC. FakeTimeProvider chỉ tua tiến, nên step "hôm nay là …"
    /// (≥ 01/11/2026) luôn tua được. JwtBearer và mọi nghiệp vụ đã đọc giờ qua TimeProvider (F00 Task 2).
    /// </summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero));

    private ApiHost? _host;

    public Dictionary<string, string?> Settings { get; } = new()
    {
        ["Jwt:SigningKey"] = "acceptance-signing-key-0123456789abcdef",
        ["AdminSeed:Email"] = AdminEmail,
        ["AdminSeed:Password"] = AdminPassword,
        ["AdminSeed:FullName"] = AdminName,
        // Thư F01-P1 đọc qua EmailOutbox (singleton trong DI); EmailOutbox.FailNext mô phỏng dịch vụ email lỗi
        ["Email:Provider"] = "Capture",
        ["App:ClientBaseUrl"] = ClientBaseUrl,
        ["RateLimiting:AuthPermitPerMinute"] = "1000",
        // @ADR-f03-thu-vien-bai-tap-007: scenario BDD bắt đầu với thư viện bài tập trống
        ["Seed:ExerciseLibrary"] = "false",
    };

    public string ConnectionString { get; } = SqlServerHooks.NewDatabaseConnectionString();

    public Exception? StartupError { get; private set; }

    public HttpResponseMessage? LastResponse { get; set; }

    public AuthTokens? Tokens { get; set; }

    public ApiHost Host
    {
        get
        {
            if (_host is null)
                TryStart();
            return _host ?? throw new InvalidOperationException("API không khởi động được.", StartupError);
        }
    }

    /// <summary>Khởi động (lại) API trên cùng database; lỗi khởi động được ghi vào StartupError.</summary>
    public void TryStart()
    {
        _host?.Dispose();
        _host = null;
        StartupError = null;
        var host = new ApiHost(ConnectionString, Settings, Clock);
        try
        {
            _ = host.Services; // buộc host khởi động → seed
            _host = host;
        }
        catch (Exception ex)
        {
            StartupError = ex;
            host.Dispose();
        }
    }

    public HttpClient Client() => Host.CreateClient();

    public async Task<HttpResponseMessage> PostJsonAsync(string url, object body, string? ip = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        if (ip is not null)
            request.Headers.Add("X-Forwarded-For", ip);
        LastResponse = await Client().SendAsync(request);
        return LastResponse;
    }

    /// <summary>Gửi request có body JSON (tùy chọn) và Bearer token (tùy chọn).</summary>
    public async Task<HttpResponseMessage> SendJsonAsync(HttpMethod method, string url, object? body, string? accessToken)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        if (accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        LastResponse = await Client().SendAsync(request);
        return LastResponse;
    }

    public Task<HttpResponseMessage> LoginAsync(string email, string password, string app, string? ip = null) =>
        PostJsonAsync("/api/auth/login", new { email, password, app }, ip);

    public async Task<AuthTokens> LoginOkAsync(string email, string password, string app)
    {
        var response = await LoginAsync(email, password, app);
        response.EnsureSuccessStatusCode();
        Tokens = (await response.Content.ReadFromJsonAsync<AuthTokens>())!;
        return Tokens;
    }

    public async Task<HttpResponseMessage> GetAsync(string url, string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        LastResponse = await Client().SendAsync(request);
        return LastResponse;
    }

    /// <summary>
    /// Tạo học viên đăng nhập được (luồng mời làm ở F01): tài khoản Identity có mật khẩu + hồ sơ Student của coach admin.
    /// </summary>
    public async Task<(string Email, string Password)> CreateStudentAsync()
    {
        var email = $"hocvien-{Guid.NewGuid():N}@acceptance.local";
        const string password = "Student@123";
        await using var scope = Host.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var users = sp.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser { UserName = email, Email = email, FullName = "Học viên Acceptance", EmailConfirmed = true };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
            throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, Roles.Student);

        // @ADR-f02-ho-so-hoc-vien-001: học viên luôn có hồ sơ Student (/api/client/me đọc từ Student)
        var db = sp.GetRequiredService<AppDbContext>();
        var adminEmail = Email.Create(AdminEmail);
        var coach = await db.Coaches.SingleAsync(c => c.Email == adminEmail);
        var now = sp.GetRequiredService<IClock>().UtcNow;
        var student = Student.Create(coach.Id, user.Id, user.FullName, Email.Create(email), now);
        // F01-P1: học viên có mật khẩu riêng là học viên Đã kích hoạt — Chờ kích hoạt chỉ đăng nhập được bằng mật khẩu tạm
        student.Activate(now);
        db.Students.Add(student);
        await db.SaveChangesAsync();
        return (email, password);
    }

    public void Dispose() => _host?.Dispose();
}
