using System.Net;
using Coaching.Acceptance.Support;
using Coaching.TestSupport;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class CredentialsSteps(AcceptanceContext ctx, StudentApi api, AccountApi account)
{
    /// <summary>Mã mục bắt buộc dùng để làm trống khi scenario cần "còn N mục bắt buộc trống".</summary>
    private static readonly string[] RequiredCodes = ["pack", "price", "age", "heightCm", "weightKg"];

    // @BR-f01-p1-tai-khoan-hoc-vien-001
    [Given(@"^một hồ sơ học viên đủ mục bắt buộc, tài khoản Chờ kích hoạt, coaching ""([^""]*)""$")]
    public async Task CompletePendingWithStatus(string label)
    {
        await account.CreateAsync();
        var status = StudentApi.CoachingStatuses[label];
        if (status != "Active")
        {
            var changed = await api.TryChangeStatusAsync(AccountApi.StudentName, status, "Chuẩn bị kịch bản");
            Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        }
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-001 @BR-f01-p1-tai-khoan-hoc-vien-002 @BR-f01-p1-tai-khoan-hoc-vien-003 @BR-f01-p1-tai-khoan-hoc-vien-006
    [When("coach gửi thông tin đăng nhập cho học viên đó")]
    [When("coach gửi lại thông tin đăng nhập cho học viên đó")]
    public Task Send() => account.TrySendCredentialsAsync();

    // @BR-f01-p1-tai-khoan-hoc-vien-001 @BR-f01-p1-tai-khoan-hoc-vien-002
    [Then(@"^học viên đó nhận được email thông tin đăng nhập(?: mới)?$")]
    public void ReceivedCredentials()
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        Assert.Equal(account.MailCountBefore + 1, account.CredentialMails().Count);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-001 (dùng lại ở BR-007, BR-017)
    [Given("một học viên có tài khoản Đã kích hoạt")]
    public Task ActivatedStudent() => account.CreateActivatedAsync();

    [Then("thông tin đăng nhập không được gửi vì tài khoản đã kích hoạt")]
    public async Task RejectedActivated()
    {
        await api.AssertErrorAsync(HttpStatusCode.Conflict, "account.already_activated");
        Assert.Equal(account.MailCountBefore, account.CredentialMails().Count);
    }

    [Then("coach được hướng dẫn gửi link đặt lại mật khẩu thay thế")]
    public Task GuidedToResetLink() =>
        account.AssertProblemTitleAsync("Tài khoản đã kích hoạt. Chỉ gửi được link đặt lại mật khẩu.");

    // @BR-f01-p1-tai-khoan-hoc-vien-002 (dùng lại ở BR-006, BR-010)
    [Given("một hồ sơ học viên đủ mục bắt buộc, chưa gửi thông tin đăng nhập lần nào")]
    public Task CompleteNeverSent() => account.CreateAsync();

    [Given(@"^một hồ sơ học viên còn ""(\d+)"" mục bắt buộc trống, chưa gửi thông tin đăng nhập lần nào$")]
    public async Task MissingRequired(int count)
    {
        var input = RegistrationSamples.Complete();
        foreach (var code in RequiredCodes.Take(count))
            input = RegistrationLabels.With(input, code, null);
        var detail = await account.CreateAsync(registration: input);
        Assert.Equal(count, detail.MissingRequired.Count);
    }

    [Then(@"^thông tin đăng nhập không được gửi vì còn ""(\d+)"" mục bắt buộc chưa điền$")]
    public async Task RejectedMissing(int count)
    {
        await api.AssertErrorAsync(HttpStatusCode.BadRequest, "account.required_fields_missing");
        await account.AssertProblemTitleAsync($"Còn {count} mục bắt buộc chưa điền.");
        Assert.Equal(account.MailCountBefore, account.CredentialMails().Count);
    }

    [Then("hồ sơ đó vẫn được lưu")]
    public async Task ProfileStillSaved()
    {
        var detail = await account.ViewAsync();
        Assert.Equal("NotSent", detail.Account.State);
        Assert.NotEmpty(detail.MissingRequired);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-002 @BR-f01-p1-tai-khoan-hoc-vien-006 (dùng lại ở BR-008)
    [Given(@"^một (?:hồ sơ )?học viên đã được gửi thông tin đăng nhập(?:, tài khoản Chờ kích hoạt)?$")]
    public async Task CreatedAndSent()
    {
        await account.CreateAsync();
        await account.SendOkAsync();
    }

    [Given("sau đó coach để trống một mục bắt buộc của hồ sơ đó")]
    public async Task ClearOneRequired()
    {
        await api.LoginCoachAsync();
        var detail = await api.UpdateOkAsync(AccountApi.StudentName, r => RegistrationLabels.With(r, "trainingGoal", null));
        Assert.Contains("trainingGoal", detail.MissingRequired);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-003
    [Given(@"^một hồ sơ học viên đủ mục bắt buộc với email ""([^""]*)"", tài khoản Chờ kích hoạt$")]
    public Task CompleteWithEmail(string email) => account.CreateAsync(email: email);

    [Then(@"^email ""([^""]*)"" nhận được địa chỉ đăng nhập web học viên, email tài khoản ""([^""]*)"", một mật khẩu tạm và hạn dùng ""(\d+)"" giờ$")]
    public void CredentialsMailContent(string to, string accountEmail, int hours)
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        var mail = Assert.Single(account.CredentialMails(to));
        var temp = AccountApi.TempPassword(mail);
        Assert.Contains($"{AcceptanceContext.ClientBaseUrl}/login", mail.TextBody);
        Assert.Contains(accountEmail, mail.TextBody);
        Assert.Contains($"{hours} giờ", mail.TextBody);
        Assert.Contains(temp, mail.HtmlBody);
        Assert.Contains($"{AcceptanceContext.ClientBaseUrl}/login", mail.HtmlBody);
    }

    [Then("địa chỉ đăng nhập trong email không chứa mật khẩu tạm hay mã bí mật nào")]
    public void LoginUrlHasNoSecret()
    {
        var mail = account.CredentialMails()[^1];
        var temp = AccountApi.TempPassword(mail);
        var urls = AccountApi.Urls(mail.TextBody).Concat(AccountApi.Urls(mail.HtmlBody)).ToList();
        Assert.Contains($"{AcceptanceContext.ClientBaseUrl}/login", urls);
        Assert.All(urls, url =>
        {
            Assert.DoesNotContain(temp, url);
            Assert.DoesNotContain("?", url);
            Assert.DoesNotContain("#", url);
        });
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-004 @BR-f01-p1-tai-khoan-hoc-vien-012
    [Given(@"^học viên được gửi mật khẩu tạm cách đây ""(\d+)"" giờ ""(\d+)"" phút$")]
    public Task SentHoursMinutesAgo(int hours, int minutes) =>
        account.SendThenWaitAsync(TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes));

    [Given(@"^(?:một )?học viên được gửi mật khẩu tạm cách đây (?:đúng )?""(\d+)"" giờ$")]
    public Task SentHoursAgo(int hours) => account.SendThenWaitAsync(TimeSpan.FromHours(hours));

    // @BR-f01-p1-tai-khoan-hoc-vien-004 @BR-f01-p1-tai-khoan-hoc-vien-012 (dùng lại ở BR-016)
    [When("học viên đăng nhập web học viên bằng mật khẩu tạm đó")]
    public Task LoginWithFocusTemp() => account.StudentLoginAsync(account.FocusTemp!);

    // @BR-f01-p1-tai-khoan-hoc-vien-004 @BR-f01-p1-tai-khoan-hoc-vien-005
    [Then("học viên được đưa tới bước đặt mật khẩu riêng")]
    public Task AtActivationStep() => account.AssertActivationChallengeAsync();

    // @BR-f01-p1-tai-khoan-hoc-vien-005
    [Given("coach đã gửi thông tin đăng nhập cho học viên hai lần liên tiếp")]
    public async Task SentTwice()
    {
        await account.CreateAsync();
        await account.SendOkAsync();
        await account.SendOkAsync();
    }

    [When(@"^học viên đăng nhập web học viên bằng mật khẩu tạm của lần gửi (sau cùng|trước)$")]
    public Task LoginWithSend(string which) =>
        account.StudentLoginAsync(which == "sau cùng" ? account.TempPasswords[^1] : account.TempPasswords[^2]);

    // @BR-f01-p1-tai-khoan-hoc-vien-012
    [Given("một học viên có mật khẩu tạm của lần gửi trước, coach đã gửi lại")]
    public async Task HasPreviousTemp()
    {
        await account.CreateAsync();
        await account.SendOkAsync();
        await account.SendOkAsync();
        account.FocusTemp = account.TempPasswords[^2];
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-006 (dùng lại ở BR-009)
    [Given("dịch vụ email đang không gửi được thư")]
    public async Task EmailServiceDown()
    {
        account.HistoryBefore = (await account.ViewAsync()).Account.History.Count;
        account.Outbox.FailNext = true;
    }

    [Then("coach được báo thông tin đăng nhập chưa gửi được")]
    public async Task ToldNotSent()
    {
        await api.AssertErrorAsync(HttpStatusCode.ServiceUnavailable, "account.email_send_failed");
        await account.AssertProblemTitleAsync("Chưa gửi được email. Vui lòng thử lại sau.");
    }

    [Then("mật khẩu tạm đã gửi trước đó vẫn dùng được")]
    public async Task PreviousTempStillWorks()
    {
        await account.StudentLoginAsync(account.TempPasswords[^1]);
        await account.AssertActivationChallengeAsync();
    }

    [Then("lịch sử tài khoản của học viên đó không có thêm dòng nào")]
    public async Task HistoryUnchanged() =>
        Assert.Equal(account.HistoryBefore, (await account.ViewAsync()).Account.History.Count);

    [Then("coach thấy tài khoản vẫn chưa được gửi thông tin đăng nhập")]
    public async Task StillNotSent()
    {
        var info = (await account.ViewAsync()).Account;
        Assert.Equal("NotSent", info.State);
        Assert.Null(info.CredentialsSentAt);
        Assert.Empty(info.History);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-025
    [Given("coach vừa gửi thông tin đăng nhập cho một học viên")]
    public async Task JustSent()
    {
        await account.CreateAsync();
        await account.SendOkAsync();
        account.SentBody = await ctx.LastResponse!.Content.ReadAsStringAsync();
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-025 (dùng lại ở BR-026)
    [When("coach xem hồ sơ của học viên đó")]
    public Task ViewProfile() => account.ViewAsync();

    [Then("coach thấy trạng thái tài khoản của học viên đó")]
    public void SeesAccountState()
    {
        Assert.Equal("Sent", account.LastDetail!.Account.State);
        Assert.NotNull(account.LastDetail.Account.CredentialsSentAt);
        Assert.NotNull(account.LastDetail.Account.TempPasswordExpiresAt);
    }

    [Then("coach không thấy mật khẩu tạm nào của học viên đó")]
    public void NoTempPasswordVisible()
    {
        var temp = Assert.Single(account.TempPasswords);
        Assert.DoesNotContain(temp, account.SentBody);
        Assert.DoesNotContain(temp, account.LastBody);
    }
}
