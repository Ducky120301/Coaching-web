using System.Net;
using Coaching.Acceptance.Support;
using Coaching.Application.Common;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class SeedingSteps(AcceptanceContext ctx)
{
    private const string DemoEmail = "hocvien.demo@acceptance.local";
    private const string DemoPassword = "Demo@12345";
    private string _originalPassword = AcceptanceContext.AdminPassword;

    // @BR-f00-codebase-016
    [Given("hệ thống chưa có dữ liệu và cấu hình khởi tạo admin hợp lệ")]
    public void FreshSystemValidSeed() { }

    [When("hệ thống khởi động")]
    [When("hệ thống khởi động lại")]
    public void SystemStarts() => ctx.TryStart();

    [Then("admin khởi tạo đăng nhập được và thấy hồ sơ coach của mình")]
    public async Task AdminCanLogInAndSeeProfile()
    {
        Assert.Null(ctx.StartupError);
        var tokens = await ctx.LoginOkAsync(AcceptanceContext.AdminEmail, AcceptanceContext.AdminPassword, "admin");
        var profile = await ctx.GetAsync("/api/admin/profile", tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    [Given("hệ thống đã khởi động một lần với cấu hình khởi tạo admin")]
    public void StartedOnce()
    {
        ctx.TryStart();
        Assert.Null(ctx.StartupError);
    }

    [Then("hệ thống vẫn chỉ có một tài khoản admin và một hồ sơ coach")]
    public async Task ExactlyOneAdminAndCoach()
    {
        Assert.Null(ctx.StartupError);
        await using var scope = ctx.Host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Single(await users.GetUsersInRoleAsync(Roles.Admin));
        Assert.Equal(1, await db.Coaches.CountAsync());
    }

    // @BR-f00-codebase-017
    [Given("cấu hình khởi tạo admin không có mật khẩu")]
    public void SeedWithoutPassword() => ctx.Settings["AdminSeed:Password"] = "";

    [Then("hệ thống không khởi động")]
    public void SystemDoesNotStart() => Assert.NotNull(ctx.StartupError);

    // @BR-f00-codebase-018
    [Given(@"cấu hình khởi tạo admin có mật khẩu dài ""(\d+)"" ký tự")]
    public void SeedPasswordLength(int length) => ctx.Settings["AdminSeed:Password"] = new string('a', length);

    // @BR-f00-codebase-019
    [Given("admin khởi tạo đã tồn tại với mật khẩu ban đầu")]
    public void AdminExistsWithOriginalPassword()
    {
        _originalPassword = ctx.Settings["AdminSeed:Password"]!;
        ctx.TryStart();
        Assert.Null(ctx.StartupError);
    }

    [Given("mật khẩu trong cấu hình khởi tạo đã được đổi sang giá trị khác")]
    public void SeedPasswordChanged() => ctx.Settings["AdminSeed:Password"] = "Changed@98765";

    [Then("admin vẫn đăng nhập được bằng mật khẩu ban đầu")]
    public async Task AdminStillUsesOriginalPassword()
    {
        Assert.Null(ctx.StartupError);
        var response = await ctx.LoginAsync(AcceptanceContext.AdminEmail, _originalPassword, "admin");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // @BR-f00-codebase-020
    [Given("môi trường bật cấu hình học viên demo")]
    public void DemoEnabled()
    {
        ctx.Settings["DemoSeed:Enabled"] = "true";
        ctx.Settings["DemoSeed:Email"] = DemoEmail;
        ctx.Settings["DemoSeed:Password"] = DemoPassword;
    }

    [Given("môi trường không bật cấu hình học viên demo")]
    public void DemoDisabled() => ctx.Settings["DemoSeed:Enabled"] = "false";

    [Then("có đúng một học viên demo và học viên đó đăng nhập được")]
    public async Task ExactlyOneDemoStudentCanLogIn()
    {
        Assert.Null(ctx.StartupError);
        await using (var scope = ctx.Host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var students = await users.GetUsersInRoleAsync(Roles.Student);
            Assert.Single(students, u => u.Email == DemoEmail);
        }
        var response = await ctx.LoginAsync(DemoEmail, DemoPassword, "client");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Then("hệ thống không có học viên demo nào")]
    public async Task NoDemoStudent()
    {
        Assert.Null(ctx.StartupError);
        await using var scope = ctx.Host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        Assert.Empty(await users.GetUsersInRoleAsync(Roles.Student));
    }
}
