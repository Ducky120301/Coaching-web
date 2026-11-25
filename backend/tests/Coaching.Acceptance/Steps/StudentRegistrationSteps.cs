using System.Net;
using Coaching.Acceptance.Support;
using Coaching.Domain.Students;
using Coaching.TestSupport;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class StudentRegistrationSteps(AcceptanceContext ctx, StudentApi api)
{
    // @BR-f02-ho-so-hoc-vien-004
    [When(@"^coach lưu hồ sơ học viên đủ mọi mục bắt buộc trừ (.+)$")]
    public Task SaveCompleteExcept(string labels)
    {
        var input = RegistrationSamples.Complete();
        foreach (var label in StepText.Quoted(labels))
            input = RegistrationLabels.With(input, RegistrationLabels.Codes[label], null);
        return api.TryCreateAsync("Học viên đăng ký", StudentApi.NewEmail(), input);
    }

    [When("coach lưu hồ sơ học viên đủ mọi mục bắt buộc")]
    public Task SaveComplete() =>
        api.TryCreateAsync("Học viên đăng ký", StudentApi.NewEmail(), RegistrationSamples.Complete());

    [Then("hồ sơ được lưu")]
    public void Saved() => Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);

    [Then(@"^hồ sơ cho biết còn thiếu đúng (\d+) mục: (.+)$")]
    public void MissingExactly(int count, string labels)
    {
        var expected = StepText.Quoted(labels).Select(l => RegistrationLabels.Codes[l]).Order().ToList();
        Assert.Equal(count, expected.Count);
        Assert.Equal(expected, api.LastDetail!.MissingRequired.Order().ToList());
    }

    [Then("hồ sơ cho biết không còn mục bắt buộc nào thiếu")]
    public void NothingMissing()
    {
        Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);
        Assert.Empty(api.LastDetail!.MissingRequired);
    }

    // @BR-f02-ho-so-hoc-vien-008 @BR-f02-ho-so-hoc-vien-009
    [When(@"^coach lưu hồ sơ học viên có ""([^""]*)"" là ""([^""]*)""$")]
    public Task SaveWithValue(string label, string value) =>
        api.TryCreateAsync("Học viên số liệu", StudentApi.NewEmail(),
            RegistrationLabels.With(new RegistrationInput(), RegistrationLabels.Codes[label], value));

    [Then(@"^hồ sơ ghi nhận ""([^""]*)"" là ""([^""]*)""$")]
    public void Recorded(string label, string value)
    {
        Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);
        RegistrationLabels.AssertValue(api.LastDetail!.Registration, RegistrationLabels.Codes[label], value);
    }

    [Then(@"^hồ sơ không được lưu vì ""([^""]*)"" ngoài khoảng cho phép$")]
    public Task OutOfRange(string label) =>
        api.AssertErrorAsync(HttpStatusCode.BadRequest, $"student.{RegistrationLabels.Codes[label]}.out_of_range");

    // @BR-f02-ho-so-hoc-vien-010 (dùng lại ở BR-014): "hôm nay" = 09:00 giờ VN của ngày đó
    [Given(@"^hôm nay là ""([^""]*)"" theo giờ Việt Nam$")]
    public Task TodayIs(string date) => api.MoveClockToAsync(StepText.VietnamTime(date, 9, 0));

    [Given(@"^thời điểm hiện tại là (\d\d):(\d\d) ngày ""([^""]*)"" giờ UTC$")]
    public Task UtcNowIs(int hour, int minute, string date)
    {
        var day = StepText.ParseDate(date);
        return api.MoveClockToAsync(new DateTimeOffset(day.Year, day.Month, day.Day, hour, minute, 0, TimeSpan.Zero));
    }

    [When(@"^coach lưu hồ sơ học viên có ngày chuyển khoản ""([^""]*)""$")]
    public Task SaveTransferDate(string date) =>
        api.TryCreateAsync("Học viên chuyển khoản", StudentApi.NewEmail(),
            new RegistrationInput { TransferDate = StepText.ParseDate(date) });

    [Then(@"^hồ sơ ghi nhận ngày chuyển khoản ""([^""]*)""$")]
    public void TransferDateRecorded(string date)
    {
        Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);
        Assert.Equal(StepText.ParseDate(date), api.LastDetail!.Registration.TransferDate);
    }

    [Then("hồ sơ không được lưu vì ngày chuyển khoản sau hôm nay")]
    public Task TransferDateInFuture() =>
        api.AssertErrorAsync(HttpStatusCode.BadRequest, "student.transferDate.in_future");

    // @BR-f02-ho-so-hoc-vien-011
    [When(@"^coach lưu hồ sơ học viên có ảnh mặt trước ""([^""]*)""$")]
    public Task SavePhoto(string url) =>
        api.TryCreateAsync("Học viên ảnh", StudentApi.NewEmail(), new RegistrationInput { PhotoFrontUrl = url });

    [Then(@"^hồ sơ ghi nhận ảnh mặt trước ""([^""]*)""$")]
    public void PhotoRecorded(string url)
    {
        Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);
        Assert.Equal(url, api.LastDetail!.Registration.PhotoFrontUrl);
    }

    [Then("hồ sơ không được lưu vì link ảnh không hợp lệ")]
    public Task PhotoInvalid() => api.AssertErrorAsync(HttpStatusCode.BadRequest, "student.photoFrontUrl.invalid_url");
}
