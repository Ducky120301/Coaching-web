using System.Net;
using Coaching.Acceptance.Support;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

// @BR-f00-codebase-003
[Binding]
public sealed class AccessTokenLifetimeSteps(AcceptanceContext ctx)
{
    [Given(@"admin được cấp một lượt truy cập cách đây ""(\d+)"" phút")]
    [Given(@"admin được cấp một lượt truy cập cách đây ""(\d+)"" phút và chưa gia hạn")]
    public async Task AccessTokenIssuedMinutesAgo(int minutes)
    {
        await ctx.LoginOkAsync(AcceptanceContext.AdminEmail, AcceptanceContext.AdminPassword, "admin");
        ctx.Clock.Advance(TimeSpan.FromMinutes(minutes));
    }

    [When("admin dùng lượt truy cập đó để xem hồ sơ coach")]
    public Task UseAccessToken() => ctx.GetAsync("/api/admin/profile", ctx.Tokens!.AccessToken);

    [Then("admin thấy hồ sơ coach của mình")]
    public void SeesProfile() => Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
}
