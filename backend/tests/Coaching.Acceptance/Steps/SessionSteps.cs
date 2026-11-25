using System.Net;
using System.Net.Http.Json;
using Coaching.Acceptance.Support;
using Coaching.Application.Auth;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class SessionSteps(AcceptanceContext ctx)
{
    private AuthTokens? _device1;
    private AuthTokens? _device2;
    private string? _replayToken;

    private async Task<AuthTokens> Refresh(string refreshToken)
    {
        var response = await ctx.PostJsonAsync("/api/auth/refresh", new { refreshToken });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokens>())!;
    }

    private async Task<(string Email, string Password)> StudentAccount()
    {
        var account = await ctx.CreateStudentAsync();
        return account;
    }

    // @BR-f00-codebase-011
    [Given(@"một phiên vừa được gia hạn cách đây ""(\d+)"" giây")]
    public async Task SessionRefreshedSecondsAgo(int seconds)
    {
        var (email, password) = await StudentAccount();
        var first = await ctx.LoginOkAsync(email, password, "client");
        _replayToken = first.RefreshToken;
        _device1 = await Refresh(first.RefreshToken);
        ctx.Clock.Advance(TimeSpan.FromSeconds(seconds));
    }

    [When("mã gia hạn trước lần đổi đó được gửi lại")]
    [When("mã gia hạn của phiên đó được gửi lại")]
    public Task ReplayOldToken() => ctx.PostJsonAsync("/api/auth/refresh", new { refreshToken = _replayToken });

    [Then("người dùng vẫn giữ được phiên mà không phải đăng nhập lại")]
    public async Task StillHasSession()
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        var tokens = (await ctx.LastResponse.Content.ReadFromJsonAsync<AuthTokens>())!;
        var me = await ctx.GetAsync("/api/client/me", tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    // @BR-f00-codebase-012
    [Given("một tài khoản đang đăng nhập trên hai thiết bị")]
    public async Task LoggedInOnTwoDevices()
    {
        var (email, password) = await StudentAccount();
        _device1 = await ctx.LoginOkAsync(email, password, "client");
        _device2 = await ctx.LoginOkAsync(email, password, "client");
    }

    [Given(@"một phiên của tài khoản đó đã được gia hạn cách đây ""(\d+)"" giây")]
    public async Task OneSessionRefreshedSecondsAgo(int seconds)
    {
        _replayToken = _device1!.RefreshToken;
        _device1 = await Refresh(_device1.RefreshToken);
        ctx.Clock.Advance(TimeSpan.FromSeconds(seconds));
    }

    [Then("hệ thống từ chối")]
    public void Rejected() => Assert.Equal(HttpStatusCode.Unauthorized, ctx.LastResponse!.StatusCode);

    [Then("cả hai thiết bị của tài khoản đó phải đăng nhập lại")]
    public async Task BothDevicesMustLogIn()
    {
        var d1 = await ctx.PostJsonAsync("/api/auth/refresh", new { refreshToken = _device1!.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, d1.StatusCode);
        var d2 = await ctx.PostJsonAsync("/api/auth/refresh", new { refreshToken = _device2!.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, d2.StatusCode);
    }

    // @BR-f00-codebase-013
    [Given("một phiên đã đăng xuất")]
    public async Task LoggedOutSession()
    {
        var (email, password) = await StudentAccount();
        var tokens = await ctx.LoginOkAsync(email, password, "client");
        _replayToken = tokens.RefreshToken;
        var logout = await ctx.PostJsonAsync("/api/auth/logout", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
    }

    [Then("hệ thống từ chối và người dùng phải đăng nhập lại")]
    public void RejectedMustLogIn() => Assert.Equal(HttpStatusCode.Unauthorized, ctx.LastResponse!.StatusCode);
}
