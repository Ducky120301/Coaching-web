using System.Net;
using Coaching.Acceptance.Support;
using Coaching.Application.Auth;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class PasswordResetSteps(AcceptanceContext ctx, StudentApi api, AccountApi account)
{
    private AuthTokens? _phone;

    // @BR-f01-p1-tai-khoan-hoc-vien-017
    [Given(@"^một học viên có tài khoản Đã kích hoạt, coaching ""([^""]*)""$")]
    public async Task ActivatedWithCoachingStatus(string label)
    {
        await account.CreateActivatedAsync();
        await api.LoginCoachAsync();
        var status = StudentApi.CoachingStatuses[label];
        if (status != "Active")
        {
            var changed = await api.TryChangeStatusAsync(AccountApi.StudentName, status, "Chuẩn bị kịch bản");
            Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        }
    }

    [When("coach gửi link đặt lại mật khẩu cho học viên đó")]
    public Task SendResetLink() => account.TrySendResetLinkAsync();

    [Then("học viên đó nhận được email có link đặt lại mật khẩu")]
    public void ReceivedResetLink()
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        var mails = account.ResetMails();
        Assert.Equal(account.MailCountBefore + 1, mails.Count);
        Assert.Contains($"{AcceptanceContext.ClientBaseUrl}/dat-lai-mat-khau#t=", mails[^1].TextBody);
    }

    [Then("link đặt lại không được gửi vì tài khoản chưa kích hoạt")]
    public async Task RejectedNotActivated()
    {
        await api.AssertErrorAsync(HttpStatusCode.Conflict, "account.not_activated");
        Assert.Equal(account.MailCountBefore, account.ResetMails().Count);
    }

    [Then("coach được hướng dẫn gửi thông tin đăng nhập thay thế")]
    public Task GuidedToCredentials() =>
        account.AssertProblemTitleAsync("Tài khoản chưa kích hoạt. Hãy gửi thông tin đăng nhập.");

    // @BR-f01-p1-tai-khoan-hoc-vien-018
    [Given(@"^học viên nhận link đặt lại mật khẩu cách đây (?:đúng )?""(\d+)"" phút$")]
    public async Task GotLinkMinutesAgo(int minutes)
    {
        await account.CreateActivatedAsync();
        await account.ForgotOkAsync();
        ctx.Clock.Advance(TimeSpan.FromMinutes(minutes));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-018 @BR-f01-p1-tai-khoan-hoc-vien-019
    [When(@"^học viên đặt lại mật khẩu ""([^""]*)"" bằng link đó$")]
    public Task ResetWithThatLink(string password) => account.TryResetAsync(account.ResetTokens[^1], password, password);

    // @BR-f01-p1-tai-khoan-hoc-vien-018 @BR-f01-p1-tai-khoan-hoc-vien-019 @BR-f01-p1-tai-khoan-hoc-vien-020
    [Then(@"^học viên đăng nhập được bằng mật khẩu ""([^""]*)""$")]
    public async Task CanLogInWith(string password)
    {
        Assert.Equal(HttpStatusCode.NoContent, ctx.LastResponse!.StatusCode);
        await account.StudentLoginAsync(password);
        await account.AssertStudentSessionAsync();
    }

    [Then("mật khẩu không được đặt lại vì link đã hết hiệu lực")]
    public async Task LinkNoLongerValid()
    {
        await api.AssertErrorAsync(HttpStatusCode.BadRequest, "auth.reset_link_invalid");
        await account.AssertProblemTitleAsync("Link đặt lại đã hết hạn hoặc đã được dùng. Hãy xin link mới.");
        await account.StudentLoginAsync(account.OwnPassword!); // mật khẩu hiện tại vẫn là mật khẩu đăng nhập được
        await account.AssertStudentSessionAsync();
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-019
    [Given("học viên đã đặt lại mật khẩu thành công bằng một link còn hạn")]
    public async Task ResetOnce()
    {
        await account.CreateActivatedAsync();
        await account.ForgotOkAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await account.TryResetAsync(account.ResetTokens[^1], "Coach2026", "Coach2026")).StatusCode);
    }

    [Given(@"^học viên đã dùng một link còn hạn để đặt mật khẩu ""([^""]*)"" và bị từ chối$")]
    public async Task ResetRejected(string password)
    {
        await account.CreateActivatedAsync();
        await account.ForgotOkAsync();
        var response = await account.TryResetAsync(account.ResetTokens[^1], password, password);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("auth.password_too_short", await StudentApi.ErrorCodeAsync(response));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-020
    [Given("học viên đã nhận link đặt lại mật khẩu hai lần")]
    public async Task GotTwoLinks()
    {
        await account.CreateActivatedAsync();
        await account.ForgotOkAsync();
        ctx.Clock.Advance(TimeSpan.FromSeconds(61)); // qua giới hạn 60 giây của BR-024
        await account.ForgotOkAsync();
        Assert.Equal(2, account.ResetTokens.Count);
    }

    [When(@"^học viên đặt lại mật khẩu ""([^""]*)"" bằng link lần (một|hai)$")]
    public Task ResetWithLink(string password, string which) =>
        account.TryResetAsync(account.ResetTokens[which == "một" ? 0 : 1], password, password);

    // @BR-f01-p1-tai-khoan-hoc-vien-021
    [Given("học viên đang đăng nhập web học viên trên điện thoại")]
    public async Task LoggedInOnPhone()
    {
        await account.CreateActivatedAsync();
        Assert.Equal(HttpStatusCode.OK, (await account.StudentLoginAsync(account.OwnPassword!)).StatusCode);
        _phone = ctx.Tokens;
    }

    [When("học viên đặt lại mật khẩu bằng link trên máy tính")]
    public async Task ResetOnComputer()
    {
        await account.ForgotOkAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await account.TryResetAsync(account.ResetTokens[^1], "Coach2026", "Coach2026")).StatusCode);
    }

    [Then("phiên đăng nhập trên điện thoại kết thúc, học viên phải đăng nhập lại")]
    public async Task PhoneSessionEnded()
    {
        var refresh = await ctx.PostJsonAsync("/api/auth/refresh", new { refreshToken = _phone!.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Given(@"^học viên đã đặt lại mật khẩu từ ""([^""]*)"" sang ""([^""]*)""$")]
    public async Task ResetFromTo(string from, string to)
    {
        await account.CreateActivatedAsync(password: from);
        await account.ForgotOkAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await account.TryResetAsync(account.ResetTokens[^1], to, to)).StatusCode);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-022 @BR-f01-p1-tai-khoan-hoc-vien-023
    [Given(@"^email ""([^""]*)"" là (email chưa có tài khoản nào|email của học viên còn Chờ kích hoạt|email của tài khoản coach|email của học viên Đã kích hoạt)$")]
    public async Task EmailIs(string email, string kind)
    {
        switch (kind)
        {
            case "email của học viên còn Chờ kích hoạt":
                await account.CreateAsync(email: email);
                break;
            case "email của tài khoản coach":
                await account.CreateCoachAccountAsync(email);
                break;
            case "email của học viên Đã kích hoạt":
                await account.CreateActivatedAsync(email: email);
                break;
            default: // email chưa có tài khoản nào
                break;
        }
    }

    [When(@"^một người xin đặt lại mật khẩu cho email ""([^""]*)""$")]
    public Task SomeoneAsksForReset(string email) => account.ForgotAsync(email);

    // @BR-f02-import-excel-029 (dùng lại): import không gửi email nào cho học viên
    [Then(@"^không email nào được gửi tới ""([^""]*)""$")]
    public async Task NoMailTo(string email)
    {
        // 202 = Quên mật khẩu (F01-P1); 200 = import Excel (import-excel.feature) — request đã được xử lý
        Assert.Contains(ctx.LastResponse!.StatusCode, new[] { HttpStatusCode.Accepted, HttpStatusCode.OK });
        await Task.Delay(AccountApi.QueueSettle);
        Assert.DoesNotContain(account.Outbox.All, m => string.Equals(m.To, email.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-023 @BR-f01-p1-tai-khoan-hoc-vien-024
    [Then("người đó nhận thông báo chung của Quên mật khẩu, không phân biệt được email có tài khoản hay không")]
    [Then("lần xin đó vẫn nhận thông báo chung của Quên mật khẩu")]
    public Task GenericForgotMessage() => account.AssertForgotGenericAsync();

    // @BR-f01-p1-tai-khoan-hoc-vien-024
    [Given(@"^học viên Đã kích hoạt đã xin đặt lại mật khẩu cách đây ""(\d+)"" phút$")]
    public async Task AskedMinutesAgo(int minutes)
    {
        await account.CreateActivatedAsync();
        await account.ForgotOkAsync();
        ctx.Clock.Advance(TimeSpan.FromMinutes(minutes));
    }

    [When("học viên xin đặt lại mật khẩu lần nữa")]
    public Task AskAgain() => account.ForgotAsync(account.Email());

    [Then("học viên nhận được email đặt lại thứ hai")]
    public async Task ReceivedSecondResetMail() =>
        Assert.Equal(2, (await account.WaitForMailsAsync(account.Email(), AccountApi.ResetSubject, 2)).Count);

    [Given(@"^học viên Đã kích hoạt đã xin đặt lại mật khẩu ""(\d+)"" lần trong ""(\d+)"" giây vừa qua$")]
    public async Task AskedTimesWithinSeconds(int times, int seconds)
    {
        await account.CreateActivatedAsync();
        for (var i = 0; i < times; i++)
        {
            if (i > 0)
                ctx.Clock.Advance(TimeSpan.FromSeconds((double)seconds / (times - 1)));
            Assert.Equal(HttpStatusCode.Accepted, (await account.ForgotAsync(account.Email())).StatusCode);
        }
    }

    [When(@"^học viên xin đặt lại mật khẩu lần thứ ""(\d+)""$")]
    public Task AskNthTime(int nth)
    {
        Assert.True(nth > 1);
        return account.ForgotAsync(account.Email());
    }

    [Then(@"^học viên chỉ nhận được ""(\d+)"" email đặt lại$")]
    public async Task OnlyResetMails(int count)
    {
        await account.WaitForMailsAsync(account.Email(), AccountApi.ResetSubject, count);
        await Task.Delay(AccountApi.QueueSettle);
        Assert.Equal(count, account.ResetMails().Count);
    }
}
