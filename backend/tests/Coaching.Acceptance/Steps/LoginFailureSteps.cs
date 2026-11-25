using System.Net;
using System.Net.Http.Json;
using Coaching.Acceptance.Support;
using Microsoft.AspNetCore.Mvc;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class LoginFailureSteps(AcceptanceContext ctx)
{
    private string _email = AcceptanceContext.AdminEmail;
    private string _password = AcceptanceContext.AdminPassword;

    private async Task FailLoginTimes(int times)
    {
        for (var i = 0; i < times; i++)
            await ctx.LoginAsync(AcceptanceContext.AdminEmail, "sai-mat-khau", "admin");
    }

    // @BR-f00-codebase-006 — các dòng Examples của Outline
    [Given("email đã có tài khoản nhưng mật khẩu sai")]
    public void ExistingEmailWrongPassword() => (_email, _password) = (AcceptanceContext.AdminEmail, "sai-mat-khau");

    [Given("email chưa có tài khoản nào")]
    public void UnknownEmail() => (_email, _password) = ("khong-ton-tai@acceptance.local", "Bat-ky-123");

    [Given("tài khoản đang bị khóa đăng nhập, mật khẩu nhập đúng")]
    public async Task LockedAccountCorrectPassword()
    {
        await FailLoginTimes(5);
        (_email, _password) = (AcceptanceContext.AdminEmail, AcceptanceContext.AdminPassword);
    }

    [When("người dùng đăng nhập bằng thông tin đó")]
    public Task LoginWithThatInfo() => ctx.LoginAsync(_email, _password, "admin");

    [Then(@"hệ thống từ chối với thông báo ""(.*)""")]
    public async Task RejectedWithMessage(string message)
    {
        Assert.False(ctx.LastResponse!.IsSuccessStatusCode);
        var problem = await ctx.LastResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(message, problem!.Title);
    }

    // @BR-f00-codebase-007
    [Given(@"một tài khoản đã đăng nhập sai mật khẩu ""(\d+)"" lần liên tiếp")]
    public Task FailedTimes(int times) => FailLoginTimes(times);

    [Given(@"một tài khoản vừa đăng nhập sai mật khẩu lần thứ ""(\d+)"" liên tiếp cách đây ""(\d+)"" phút")]
    public async Task FailedTimesMinutesAgo(int times, int minutes)
    {
        await FailLoginTimes(times);
        ctx.Clock.Advance(TimeSpan.FromMinutes(minutes));
    }

    [Given(@"một tài khoản bị khóa đăng nhập cách đây ""(\d+)"" phút")]
    public async Task LockedMinutesAgo(int minutes)
    {
        await FailLoginTimes(5);
        ctx.Clock.Advance(TimeSpan.FromMinutes(minutes));
    }

    [When("người dùng đăng nhập vào tài khoản đó bằng mật khẩu đúng")]
    public Task LoginCorrect() => ctx.LoginAsync(AcceptanceContext.AdminEmail, AcceptanceContext.AdminPassword, "admin");

    [Then("người dùng đăng nhập thành công")]
    public void LoginSucceeded() => Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);

    [Then("đăng nhập bị từ chối")]
    public void LoginRejected() => Assert.Equal(HttpStatusCode.Unauthorized, ctx.LastResponse!.StatusCode);
}
