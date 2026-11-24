using System.Net;
using System.Net.Http.Json;
using Coaching.Acceptance.Support;
using Coaching.Application.Coaches;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class AccessSteps(AcceptanceContext ctx)
{
    private string? _studentEmail;

    // @BR-f00-codebase-001
    [Given("admin đã đăng nhập")]
    public async Task AdminLoggedIn() =>
        await ctx.LoginOkAsync(AcceptanceContext.AdminEmail, AcceptanceContext.AdminPassword, "admin");

    // @BR-f00-codebase-002
    [Given("học viên đã đăng nhập")]
    public async Task StudentLoggedIn()
    {
        var (email, password) = await ctx.CreateStudentAsync();
        _studentEmail = email;
        await ctx.LoginOkAsync(email, password, "client");
    }

    [Given("người dùng chưa đăng nhập")]
    public void NotLoggedIn() => ctx.Tokens = null;

    // @BR-f00-codebase-001 @BR-f00-codebase-002
    [When("admin xem hồ sơ coach của mình")]
    [When("người dùng yêu cầu xem hồ sơ coach")]
    [When("học viên yêu cầu xem hồ sơ coach")]
    public Task RequestCoachProfile() => ctx.GetAsync("/api/admin/profile", ctx.Tokens?.AccessToken);

    // @BR-f00-codebase-001 @BR-f00-codebase-002
    [When("học viên xem thông tin tài khoản của mình")]
    [When("người dùng yêu cầu xem thông tin tài khoản học viên")]
    [When("admin yêu cầu xem thông tin tài khoản học viên")]
    public Task RequestStudentMe() => ctx.GetAsync("/api/client/me", ctx.Tokens?.AccessToken);

    // @BR-f00-codebase-001
    [Then("admin thấy đúng email và tên coach của mình")]
    public async Task AdminSeesOwnProfile()
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        var profile = await ctx.LastResponse.Content.ReadFromJsonAsync<CoachProfileDto>();
        Assert.Equal(AcceptanceContext.AdminEmail, profile!.Email);
        Assert.Equal(AcceptanceContext.AdminName, profile.DisplayName);
    }

    // @BR-f00-codebase-002
    [Then("học viên thấy đúng email và họ tên của mình")]
    public async Task StudentSeesOwnAccount()
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        var me = await ctx.LastResponse.Content.ReadFromJsonAsync<StudentMe>();
        Assert.Equal(_studentEmail, me!.Email);
        Assert.Equal("Học viên Acceptance", me.FullName);
    }

    // @BR-f00-codebase-001 @BR-f00-codebase-002
    [Then("hệ thống từ chối vì người dùng chưa xác thực")]
    public void RejectedUnauthenticated() => Assert.Equal(HttpStatusCode.Unauthorized, ctx.LastResponse!.StatusCode);

    // @BR-f00-codebase-001 @BR-f00-codebase-002
    [Then("hệ thống từ chối vì admin không đủ quyền")]
    [Then("hệ thống từ chối vì học viên không đủ quyền")]
    public void RejectedForbidden() => Assert.Equal(HttpStatusCode.Forbidden, ctx.LastResponse!.StatusCode);

    private sealed record StudentMe(Guid UserId, string Email, string FullName);
}
