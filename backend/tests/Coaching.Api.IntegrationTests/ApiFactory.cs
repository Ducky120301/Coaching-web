using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Abstractions;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Emailing;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Coaching.TestSupport;

namespace Coaching.Api.IntegrationTests;

/// <summary>Chạy API thật trên SQL Server thật (container Testcontainers), mỗi lần chạy test một DB mới.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin@12345";
    public const string ClientBaseUrl = "http://client.test";

    /// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-007: hộp thư trong bộ nhớ của host test.</summary>
    public EmailOutbox Outbox => Services.GetRequiredService<EmailOutbox>();

    private readonly DatabaseFixture _database = new();
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        _connectionString = await _database.NewDatabaseAsync();
        _ = Services; // khởi động host → seed admin
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-0123456789abcdef");
        builder.UseSetting("AdminSeed:Email", AdminEmail);
        builder.UseSetting("AdminSeed:Password", AdminPassword);
        // @ADR-f01-p1-tai-khoan-hoc-vien-007: test đọc mật khẩu tạm / link đặt lại từ hộp thư trong bộ nhớ
        builder.UseSetting("Email:Provider", "Capture");
        // @ADR-f01-p1-tai-khoan-hoc-vien-008
        builder.UseSetting("App:ClientBaseUrl", ClientBaseUrl);
        builder.UseSetting("RateLimiting:AuthPermitPerMinute", "1000");
    }

    public async Task<AuthTokens> LoginAsync(string email, string password, ClientApp? app = null)
    {
        var resolvedApp = app ?? (email == AdminEmail ? ClientApp.Admin : ClientApp.Client);
        var response = await CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password, resolvedApp));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokens>())!;
    }

    /// <summary>HttpClient đã gắn token của admin seed.</summary>
    public async Task<HttpClient> AdminClientAsync()
    {
        var tokens = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    /// <summary>Host test đã chạy Email:Provider=Capture (Task 2) — alias để test F01-P1 ghi rõ ý đọc thư.</summary>
    public WebApplicationFactory<Program> Capture => this;

    /// <summary>HttpClient admin seed trên host Capture.</summary>
    public Task<HttpClient> CaptureAdminClientAsync() => AdminClientAsync();

    /// <summary>Tạo học viên đăng nhập được (luồng mời/kích hoạt làm ở F01): tài khoản có mật khẩu + hồ sơ Student.</summary>
    public async Task<(string Email, string Password)> CreateStudentAsync()
    {
        var email = $"student-{Guid.NewGuid():N}@test.local";
        const string password = "Student@123";

        await using var scope = Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var users = sp.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser { UserName = email, Email = email, FullName = "Học viên Test", EmailConfirmed = true };
        var created = await users.CreateAsync(user, password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        Assert.True((await users.AddToRoleAsync(user, Roles.Student)).Succeeded);

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
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
