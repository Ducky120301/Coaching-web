using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Coaching.Acceptance.Support;

/// <summary>Trạng thái của 1 scenario (Reqnroll tạo mới cho mỗi scenario).</summary>
public sealed class AcceptanceContext : IDisposable
{
    public const string AdminEmail = "admin@acceptance.local";
    public const string AdminPassword = "Admin@12345";
    public const string AdminName = "Coach Duobexiuuu";

    private ApiHost? _host;

    // Bắt đầu từ giờ thật: trước Task 2 JwtBearer còn so hạn token với đồng hồ hệ thống.
    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

    public Dictionary<string, string?> Settings { get; } = new()
    {
        ["Jwt:SigningKey"] = "acceptance-signing-key-0123456789abcdef",
        ["AdminSeed:Email"] = AdminEmail,
        ["AdminSeed:Password"] = AdminPassword,
        ["AdminSeed:FullName"] = AdminName,
        ["Email:Provider"] = "Log",
        ["RateLimiting:AuthPermitPerMinute"] = "1000",
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
            _ = host.Services; // buộc host khởi động → migrate + seed
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

    /// <summary>Tạo tài khoản học viên trực tiếp qua Identity (luồng mời làm ở F01).</summary>
    public async Task<(string Email, string Password)> CreateStudentAsync()
    {
        var email = $"hocvien-{Guid.NewGuid():N}@acceptance.local";
        const string password = "Student@123";
        await using var scope = Host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser { UserName = email, Email = email, FullName = "Học viên Acceptance", EmailConfirmed = true };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
            throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, Roles.Student);
        return (email, password);
    }

    public void Dispose() => _host?.Dispose();
}
