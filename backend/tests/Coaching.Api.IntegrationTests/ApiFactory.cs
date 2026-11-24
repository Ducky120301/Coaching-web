using System.Net.Http.Json;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Coaching.TestSupport;

namespace Coaching.Api.IntegrationTests;

/// <summary>Chạy API thật trên SQL Server thật (container Testcontainers), mỗi lần chạy test một DB mới.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin@12345";

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
        builder.UseSetting("Email:Provider", "Log");
        builder.UseSetting("RateLimiting:AuthPermitPerMinute", "1000");
    }

    public async Task<AuthTokens> LoginAsync(string email, string password)
    {
        var response = await CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokens>())!;
    }

    /// <summary>Tạo tài khoản học viên trực tiếp qua Identity (luồng mời/kích hoạt làm ở F01).</summary>
    public async Task<(string Email, string Password)> CreateStudentAsync()
    {
        var email = $"student-{Guid.NewGuid():N}@test.local";
        const string password = "Student@123";

        await using var scope = Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser { UserName = email, Email = email, FullName = "Học viên Test", EmailConfirmed = true };
        var created = await users.CreateAsync(user, password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        Assert.True((await users.AddToRoleAsync(user, Roles.Student)).Succeeded);
        return (email, password);
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
