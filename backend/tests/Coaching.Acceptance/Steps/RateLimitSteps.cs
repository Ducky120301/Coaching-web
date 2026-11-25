using System.Net;
using System.Net.Http.Json;
using Coaching.Acceptance.Support;
using Microsoft.AspNetCore.Mvc;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

// @BR-f00-codebase-008
// @ADR-f00-codebase-004: kịch bản người dùng B kiểm việc chia giới hạn theo IP thật từ X-Forwarded-For
[Binding]
public sealed class RateLimitSteps(AcceptanceContext ctx)
{
    private const string IpA = "203.0.113.10";
    private const string IpB = "198.51.100.20";
    private string _currentIp = IpA;

    private async Task SendLogins(string ip, int count)
    {
        ctx.Settings["RateLimiting:AuthPermitPerMinute"] = "10";
        for (var i = 0; i < count; i++)
            await ctx.LoginAsync(AcceptanceContext.AdminEmail, "sai-mat-khau", "admin", ip);
    }

    [Given(@"một người dùng đã gửi ""(\d+)"" yêu cầu đăng nhập trong phút hiện tại")]
    public Task AlreadySent(int count) => SendLogins(_currentIp, count);

    [When("người dùng đó gửi thêm một yêu cầu đăng nhập")]
    public Task SendOneMore() => ctx.LoginAsync(AcceptanceContext.AdminEmail, "sai-mat-khau", "admin", _currentIp);

    [Then("yêu cầu được xử lý bình thường")]
    [Then("yêu cầu của người dùng B được xử lý bình thường")]
    public void ProcessedNormally() => Assert.NotEqual(HttpStatusCode.TooManyRequests, ctx.LastResponse!.StatusCode);

    [Then("hệ thống từ chối bằng thông báo tiếng Việt")]
    public async Task RejectedInVietnamese()
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, ctx.LastResponse!.StatusCode);
        var problem = await ctx.LastResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.StartsWith("Bạn thao tác quá nhanh. Vui lòng thử lại sau", problem!.Title);
    }

    [Then("người dùng được cho biết thời gian phải chờ")]
    public void TellsWaitTime()
    {
        var retryAfter = ctx.LastResponse!.Headers.RetryAfter?.Delta;
        Assert.NotNull(retryAfter);
        Assert.True(retryAfter > TimeSpan.Zero);
    }

    [Given("người dùng A đã chạm giới hạn yêu cầu đăng nhập trong phút hiện tại")]
    public Task UserAHitLimit() => SendLogins(IpA, 10);

    [Given("người dùng B ở một địa chỉ khác, cùng đi qua proxy của nhà cung cấp hosting")]
    public void UserBBehindSameProxy() => _currentIp = IpB;

    [When("người dùng B gửi một yêu cầu đăng nhập")]
    public Task UserBSends() => ctx.LoginAsync(AcceptanceContext.AdminEmail, "sai-mat-khau", "admin", IpB);
}
