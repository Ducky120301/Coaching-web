using System.Net;
using Coaching.Acceptance.Support;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class ActivationSteps(AcceptanceContext ctx, StudentApi api, AccountApi account)
{
    // @BR-f01-p1-tai-khoan-hoc-vien-011 @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-014 (dùng cho BR-005 sc.3)
    [Given("học viên đã đăng nhập bằng mật khẩu tạm và đang ở bước đặt mật khẩu riêng")]
    [Given("học viên đã đăng nhập bằng mật khẩu tạm và chưa đặt mật khẩu riêng")]
    [Given("học viên đang đặt mật khẩu riêng bằng mật khẩu tạm còn hiệu lực")]
    public Task AtActivationStep() => account.ReachActivationStepAsync();

    // BR-005 sc.3 (rule của Task 4): coach gửi lại khi học viên đang đặt mật khẩu bằng mật khẩu tạm cũ
    [Given("sau đó coach gửi lại thông tin đăng nhập cho học viên đó")]
    public Task CoachResends() => account.SendOkAsync();

    [When("học viên lưu một mật khẩu riêng hợp lệ")]
    public Task SaveValidPassword() => account.TryActivateAsync("Coach2026", "Coach2026");

    [Then("mật khẩu riêng không được lưu")]
    public Task NotSaved() => api.AssertErrorAsync(HttpStatusCode.Unauthorized, "auth.activation_invalid");

    [Then("học viên phải đăng nhập lại bằng mật khẩu tạm mới")]
    public async Task MustLogInWithNewTemp()
    {
        await account.AssertProblemTitleAsync("Phiên kích hoạt đã hết hạn. Vui lòng đăng nhập lại bằng mật khẩu tạm mới nhất.");
        Assert.Equal("PendingActivation", (await account.ViewAsync()).AccountStatus);
        await account.StudentLoginAsync(account.TempPasswords[^1]);
        await account.AssertActivationChallengeAsync();
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-011: thứ duy nhất học viên có là mã kích hoạt — không phải JWT
    [When("học viên xem hồ sơ của mình")]
    public Task ViewOwnProfile() => ctx.GetAsync("/api/client/me", account.ActivationToken);

    [Then("học viên bị từ chối vì cần đặt mật khẩu riêng trước")]
    public async Task RejectedUntilActivated()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, ctx.LastResponse!.StatusCode);
        Assert.Equal("PendingActivation", (await account.ViewAsync()).AccountStatus);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-011
    [Given("một học viên vừa được gửi mật khẩu tạm")]
    public async Task JustSentTemp()
    {
        await account.CreateAsync();
        await account.SendOkAsync();
    }

    [When("học viên đăng nhập web quản trị bằng mật khẩu tạm đó")]
    public Task LoginAdminWithTemp() => account.StudentLoginAsync(account.FocusTemp!, "admin");

    // @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-014
    [When(@"^học viên lưu mật khẩu riêng ""([^""]*)""$")]
    public Task SavePassword(string password) => account.TryActivateAsync(password, password);

    [When(@"^học viên lưu một mật khẩu riêng dài ""(\d+)"" ký tự$")]
    public Task SavePasswordOfLength(int length)
    {
        var password = new string('a', length);
        return account.TryActivateAsync(password, password);
    }

    [When(@"^học viên lưu mật khẩu riêng ""([^""]*)"", nhập lại ""([^""]*)""$")]
    public Task SavePasswordWithConfirmation(string password, string confirm) => account.TryActivateAsync(password, confirm);

    [Then("mật khẩu riêng được lưu")]
    public Task Saved() => account.AssertStudentSessionAsync();

    [Then(@"^mật khẩu riêng không được lưu vì cần ít nhất ""(\d+)"" ký tự$")]
    public async Task TooShort(int min)
    {
        await api.AssertErrorAsync(HttpStatusCode.BadRequest, "auth.password_too_short");
        await account.AssertProblemTitleAsync($"Mật khẩu cần ít nhất {min} ký tự.");
    }

    [Then(@"^mật khẩu riêng không được lưu vì dài quá ""(\d+)"" ký tự$")]
    public async Task TooLong(int max)
    {
        await api.AssertErrorAsync(HttpStatusCode.BadRequest, "auth.password_too_long");
        await account.AssertProblemTitleAsync($"Mật khẩu tối đa {max} ký tự.");
    }

    [Then("mật khẩu riêng không được lưu vì hai lần nhập không khớp")]
    public async Task Mismatch()
    {
        await api.AssertErrorAsync(HttpStatusCode.BadRequest, "auth.password_mismatch");
        await account.AssertProblemTitleAsync("Hai lần nhập mật khẩu không khớp.");
    }

    [Then("tài khoản của học viên vẫn Chờ kích hoạt")]
    public async Task StillPending() => Assert.Equal("PendingActivation", (await account.ViewAsync()).AccountStatus);

    // @BR-f01-p1-tai-khoan-hoc-vien-015
    [Given(@"^học viên đã kích hoạt tài khoản với mật khẩu riêng ""([^""]*)""$")]
    public Task ActivatedWithPassword(string password) => account.CreateActivatedAsync(password: password);

    // @BR-f01-p1-tai-khoan-hoc-vien-015 (dùng lại ở BR-021)
    [When(@"^học viên đăng nhập web học viên bằng mật khẩu ""([^""]*)""$")]
    public Task LoginWithPassword(string password) => account.StudentLoginAsync(password);

    // @BR-f01-p1-tai-khoan-hoc-vien-015 @BR-f01-p1-tai-khoan-hoc-vien-016
    [Then("học viên vào được khu vực học viên")]
    public Task InStudentArea() => account.AssertStudentSessionAsync();

    // @BR-f01-p1-tai-khoan-hoc-vien-016
    [Given("học viên đã kích hoạt tài khoản bằng mật khẩu tạm còn hiệu lực")]
    public async Task ActivatedWithValidTemp()
    {
        await account.ReachActivationStepAsync();
        account.FocusTemp = account.TempPasswords[^1];
        Assert.Equal(HttpStatusCode.OK, (await account.TryActivateAsync("Coach2026", "Coach2026")).StatusCode);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-026
    [Given("một học viên chưa được gửi thông tin đăng nhập lần nào")]
    public Task NeverSent() => account.CreateAsync();

    [Given(@"^một học viên được gửi thông tin đăng nhập lúc (\d\d)/(\d\d) (\d\d):(\d\d), bây giờ là (\d\d)/(\d\d) (\d\d):(\d\d)$")]
    public async Task SentAtThenNow(int sentDay, int sentMonth, int sentHour, int sentMinute,
        int nowDay, int nowMonth, int nowHour, int nowMinute)
    {
        var year = ctx.Clock.GetUtcNow().Year;
        await account.CreateAsync();
        await api.MoveClockToAsync(new DateTimeOffset(year, sentMonth, sentDay, sentHour, sentMinute, 0, StepText.VietnamOffset));
        await account.SendOkAsync();
        await api.MoveClockToAsync(new DateTimeOffset(year, nowMonth, nowDay, nowHour, nowMinute, 0, StepText.VietnamOffset));
    }

    [Given(@"^một học viên kích hoạt tài khoản ngày (\d\d/\d\d/\d{4})$")]
    public async Task ActivatedOn(string date)
    {
        await account.CreateActivatedAsync();
        // FakeTimeProvider chỉ tua tiến (mọi scenario bắt đầu 01/11/2026) → ghi thẳng thời điểm kích hoạt trong quá khứ
        var activatedAt = StepText.VietnamTime(date, 9, 0);
        await using var scope = ctx.Host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Students] SET [ActivatedAt] = {activatedAt} WHERE [Id] = {account.Id()}");
    }

    [Then(@"^coach thấy trạng thái tài khoản ""([^""]*)""$")]
    public void SeesAccountState(string label) => Assert.Equal(label, AccountApi.StateLabel(account.LastDetail!.Account));
}
