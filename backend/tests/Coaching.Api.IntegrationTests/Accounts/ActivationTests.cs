using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Auth;
using Coaching.Application.Students;

namespace Coaching.Api.IntegrationTests.Accounts;

[Collection(ApiCollection.Name)]
public sealed class ActivationTests(ApiFactory factory)
{
    // @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-014: lỗi mật khẩu không tiêu mã kích hoạt
    [Fact]
    public async Task Invalid_password_keeps_the_activation_token()
    {
        var (_, _, token) = await AccountRequests.ReachActivationAsync(factory);
        var client = factory.Capture.CreateClient();

        var tooShort = await AccountRequests.ActivateAsync(client, token, "abc123", "abc123");
        var mismatch = await AccountRequests.ActivateAsync(client, token, "Coach2026", "Coach2027");
        var saved = await AccountRequests.ActivateAsync(client, token, "Coach2026", "Coach2026");

        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        Assert.Equal("auth.password_too_short", await StudentRequests.ErrorCodeAsync(tooShort));
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
        Assert.Equal("auth.password_mismatch", await StudentRequests.ErrorCodeAsync(mismatch));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-016: vào thẳng, Đã kích hoạt kèm thời điểm; mật khẩu tạm và mã kích hoạt hết hiệu lực
    [Fact]
    public async Task Activation_signs_in_and_retires_temp_password_and_token()
    {
        var (student, temp, token) = await AccountRequests.ReachActivationAsync(factory);
        var client = factory.Capture.CreateClient();

        var response = await AccountRequests.ActivateAsync(client, token, "Coach2026", "Coach2026");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = (await response.Content.ReadFromJsonAsync<AuthTokens>())!;
        using var me = new HttpRequestMessage(HttpMethod.Get, "/api/client/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(me)).StatusCode);

        var admin = await factory.CaptureAdminClientAsync();
        var detail = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{student.Id}"))!;
        Assert.Equal("Activated", detail.AccountStatus);
        Assert.Equal("Activated", detail.Account.State);
        Assert.NotNull(detail.Account.ActivatedAt);
        Assert.Equal("Activated", detail.Account.History[0].Type);

        var tempLogin = await AccountRequests.LoginAsync(client, student.Email, temp);
        Assert.Equal(HttpStatusCode.Unauthorized, tempLogin.StatusCode);
        var reuse = await AccountRequests.ActivateAsync(client, token, "Coach2027", "Coach2027");
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Equal("auth.activation_invalid", await StudentRequests.ErrorCodeAsync(reuse));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-015: giữ đúng từng ký tự, kể cả dấu và khoảng trắng đầu/cuối
    [Fact]
    public async Task Own_password_is_kept_exactly_as_typed()
    {
        const string password = " Mậtkhẩu 1 ";
        var student = await AccountRequests.CreateActivatedAsync(factory, password);
        var client = factory.Capture.CreateClient();

        var exact = await AccountRequests.LoginAsync(client, student.Email, password);
        var trimmed = await AccountRequests.LoginAsync(client, student.Email, password.Trim());

        Assert.Equal(HttpStatusCode.OK, exact.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, trimmed.StatusCode);
    }
}
