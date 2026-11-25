using System.Net;
using Coaching.Acceptance.Support;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class EmailChangeSteps(AcceptanceContext ctx, StudentApi api, AccountApi account)
{
    private static readonly IReadOnlyDictionary<string, string> HistoryTypes = new Dictionary<string, string>
    {
        ["gửi lần đầu"] = "CredentialsSent",
        ["gửi lại kèm ghi chú mật khẩu tạm cũ bị hủy"] = "CredentialsResent",
        ["học viên đặt mật khẩu riêng"] = "Activated",
    };

    // @BR-f01-p1-tai-khoan-hoc-vien-007 @BR-f01-p1-tai-khoan-hoc-vien-010
    [Given(@"^một hồ sơ học viên với email ""([^""]*)"", (?:tài khoản Chờ kích hoạt|chưa gửi thông tin đăng nhập lần nào)$")]
    public Task PendingWithEmail(string email) => account.CreateAsync(email: email);

    // @BR-f01-p1-tai-khoan-hoc-vien-007 @BR-f01-p1-tai-khoan-hoc-vien-008 @BR-f01-p1-tai-khoan-hoc-vien-009 @BR-f01-p1-tai-khoan-hoc-vien-027
    [When(@"^coach sửa email của học viên đó thành ""([^""]*)""$")]
    public Task ChangeEmail(string email) => account.TryChangeEmailAsync(email);

    // @BR-f01-p1-tai-khoan-hoc-vien-009 @BR-f01-p1-tai-khoan-hoc-vien-010
    [Given(@"^coach đã sửa email của (?:học viên|hồ sơ) đó thành ""([^""]*)""$")]
    public async Task ChangedEmail(string email) =>
        Assert.Equal(HttpStatusCode.OK, (await account.TryChangeEmailAsync(email)).StatusCode);

    // @BR-f01-p1-tai-khoan-hoc-vien-007 @BR-f01-p1-tai-khoan-hoc-vien-009 @BR-f01-p1-tai-khoan-hoc-vien-010
    [Then(@"^hồ sơ đó (?:vẫn )?có email ""([^""]*)""$")]
    public async Task HasEmail(string email) => Assert.Equal(email, (await account.ViewAsync()).Email);

    // @BR-f01-p1-tai-khoan-hoc-vien-007
    [When("coach sửa email của học viên đó sang một email chưa ai dùng")]
    public Task ChangeToUnusedEmail() => account.TryChangeEmailAsync(StudentApi.NewEmail());

    [Then("email của học viên đó không đổi vì tài khoản đã kích hoạt")]
    public async Task LockedEmailUnchanged()
    {
        await api.AssertErrorAsync(HttpStatusCode.Conflict, "account.email_locked");
        await account.AssertProblemTitleAsync("Tài khoản đã kích hoạt, không sửa được email.");
        Assert.Equal(account.PreviousEmail, (await account.ViewAsync()).Email);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-008 — ví dụ "tài khoản coach" dùng step có sẵn của StudentCreateSteps
    [Given(@"^email ""([^""]*)"" đã thuộc về một hồ sơ khác$")]
    public Task EmailOwnedByAnotherProfile(string email) => account.CreateAsync("Chủ email", email);

    [Then("email của học viên đó không đổi vì email đã được dùng")]
    public async Task TakenEmailUnchanged()
    {
        await api.AssertErrorAsync(HttpStatusCode.Conflict, "student.email_taken");
        Assert.Equal(account.PreviousEmail, (await account.ViewAsync()).Email);
    }

    [Then("mật khẩu tạm đã gửi cho học viên đó vẫn dùng được")]
    public async Task SentTempStillWorks()
    {
        await account.StudentLoginAsync(account.TempPasswords[^1]);
        await account.AssertActivationChallengeAsync();
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-008 (dùng lại ở BR-017)
    [Given("một hồ sơ học viên tài khoản Chờ kích hoạt")]
    public Task PendingProfile() => account.CreateAsync();

    [Then("email của học viên đó không đổi vì email không hợp lệ")]
    public async Task InvalidEmailUnchanged()
    {
        await api.AssertErrorAsync(HttpStatusCode.BadRequest, "email.invalid");
        Assert.Equal(account.PreviousEmail, (await account.ViewAsync()).Email);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-009 @BR-f01-p1-tai-khoan-hoc-vien-027
    [Given(@"^một học viên với email ""([^""]*)"" đã được gửi thông tin đăng nhập$")]
    public async Task SentWithEmail(string email)
    {
        await account.CreateAsync(email: email);
        await account.SendOkAsync();
        ctx.Clock.Advance(TimeSpan.FromMinutes(1)); // lần sửa sau có thời điểm khác → thứ tự "mới nhất trước" xác định
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-009 @BR-f01-p1-tai-khoan-hoc-vien-010
    [Then(@"^email ""([^""]*)"" nhận được email thông tin đăng nhập(?: mới)?$")]
    public void CredentialsMailTo(string email)
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        Assert.Single(account.CredentialMails(email));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-009
    [When(@"^học viên đăng nhập web học viên bằng email ""([^""]*)"" và mật khẩu tạm cũ$")]
    public Task LoginWithOldTemp(string email) => account.StudentLoginAsync(account.TempPasswords[^2], email: email);

    // @BR-f01-p1-tai-khoan-hoc-vien-010
    [Then("không email nào được gửi cho học viên đó")]
    public void NoMailSent() =>
        Assert.DoesNotContain(account.Outbox.All, m => m.To == account.PreviousEmail || m.To == account.Email());

    // @BR-f01-p1-tai-khoan-hoc-vien-027
    [Given("coach đã gửi thông tin đăng nhập lần đầu, rồi gửi lại cho một học viên")]
    public async Task SentThenResent()
    {
        await account.CreateAsync();
        await account.SendOkAsync();
        ctx.Clock.Advance(TimeSpan.FromMinutes(1));
        await account.SendOkAsync();
    }

    [When("học viên đó kích hoạt tài khoản bằng mật khẩu tạm mới nhất")]
    public async Task ActivateWithLatestTemp()
    {
        ctx.Clock.Advance(TimeSpan.FromMinutes(1));
        await account.StudentLoginAsync(account.TempPasswords[^1]);
        await account.AssertActivationChallengeAsync();
        Assert.Equal(HttpStatusCode.OK, (await account.TryActivateAsync("Coach2026", "Coach2026")).StatusCode);
    }

    [Then(@"^lịch sử tài khoản của học viên đó có ""(\d+)"" dòng theo thứ tự: (.+)$")]
    public async Task HistoryInOrder(int count, string order)
    {
        var history = (await account.ViewAsync()).Account.History; // mới nhất trước
        var expected = order.Split(", ").Select(label => HistoryTypes[label]).ToList();
        Assert.Equal(count, expected.Count);
        Assert.Equal(expected, history.AsEnumerable().Reverse().Select(e => e.Type).ToList());
    }

    [Then(@"^lịch sử tài khoản của học viên đó có thêm dòng sửa email từ ""([^""]*)"" sang ""([^""]*)""$")]
    public async Task EmailChangeRow(string from, string to)
    {
        var history = (await account.ViewAsync()).Account.History;
        Assert.Equal(account.HistoryBefore + 1, history.Count);
        Assert.Equal("EmailChanged", history[0].Type);
        Assert.Equal(from, history[0].FromEmail);
        Assert.Equal(to, history[0].ToEmail);
    }
}
