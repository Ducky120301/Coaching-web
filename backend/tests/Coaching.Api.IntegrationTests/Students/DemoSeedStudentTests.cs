using System.Net;
using System.Net.Http.Json;
using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class DemoSeedStudentTests(ApiFactory factory)
{
    private const string DemoEmail = "demo-backfill@test.local";

    private WebApplicationFactory<Program> WithDemoSeed() => factory.WithWebHostBuilder(b =>
    {
        b.UseSetting("DemoSeed:Enabled", "true");
        b.UseSetting("DemoSeed:Email", DemoEmail);
        b.UseSetting("DemoSeed:Password", "Demo@12345");
    });

    // @ADR-f02-ho-so-hoc-vien-001: user demo đã seed ở F00 (chưa có Student) được bổ sung Student; khởi động lại không tạo thêm
    [Fact]
    public async Task Demo_student_gets_a_profile_and_backfill_is_idempotent()
    {
        await using (var first = WithDemoSeed())
            _ = first.Services;

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = await db.Users.Where(u => u.Email == DemoEmail).Select(u => u.Id).SingleAsync();
        Assert.Equal(1, await db.Students.CountAsync(s => s.UserId == userId));

        // Giả lập dữ liệu F00: user demo chưa có Student
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM [StudentAccountEvents] WHERE [StudentId] IN (SELECT [Id] FROM [Students] WHERE [UserId] = {userId})");
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Students] WHERE [UserId] = {userId}");
        await using (var second = WithDemoSeed())
            _ = second.Services;
        await using (var third = WithDemoSeed())
            _ = third.Services;

        Assert.Equal(1, await db.Students.CountAsync(s => s.UserId == userId));
    }

    // @BR-f00-codebase-020: học viên demo (mật khẩu riêng từ seed) được kích hoạt → vẫn đăng nhập được dưới luật mật khẩu tạm của F01-P1
    [Fact]
    public async Task Demo_student_is_activated_and_can_log_in()
    {
        await using var host = WithDemoSeed();

        var response = await host.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = DemoEmail, password = "Demo@12345", app = "client" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("accessToken", await response.Content.ReadAsStringAsync());
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var email = Email.Create(DemoEmail);
        Assert.Equal(AccountStatus.Activated,
            await db.Students.Where(s => s.Email == email).Select(s => s.AccountStatus).SingleAsync());
    }
}
