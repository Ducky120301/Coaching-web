using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Acceptance.Support;
using Coaching.Application.Students;
using Coaching.Domain.Students;
using Coaching.TestSupport;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class StudentRegistrationSteps(AcceptanceContext ctx, StudentApi api)
{
    /// <summary>@BR-f02-import-excel-001: mục đã bỏ khỏi form → khoá JSON cũ.</summary>
    private static readonly IReadOnlyDictionary<string, string> RemovedFields = new Dictionary<string, string>
    {
        ["Lịch rảnh"] = "availability",
        ["Dụng cụ"] = "equipment",
    };

    private Guid _createdId;

    // @BR-f02-ho-so-hoc-vien-004 @BR-f02-import-excel-007
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

    // @BR-f02-import-excel-007: để trống một mục không bắt buộc
    [When(@"^coach lưu hồ sơ học viên đủ mọi mục bắt buộc và để trống ""([^""]*)""$")]
    public Task SaveCompleteWithEmpty(string label) =>
        api.TryCreateAsync("Học viên đăng ký", StudentApi.NewEmail(),
            RegistrationLabels.With(RegistrationSamples.Complete(), RegistrationLabels.Codes[label], null));

    // @BR-f02-import-excel-001: gửi kèm Lịch rảnh/Dụng cụ như client cũ (ví dụ ❌ của spec) — phải bị bỏ qua
    [When("coach tạo hồ sơ học viên đủ mọi mục bắt buộc")]
    public async Task CreateCompleteWithLegacyFields()
    {
        var registration = JsonSerializer.SerializeToNode(RegistrationSamples.Complete(), JsonSerializerOptions.Web)!.AsObject();
        registration["availability"] = "Tối 2-4-6";
        registration["equipment"] = "Tạ đơn";

        var response = await ctx.SendJsonAsync(HttpMethod.Post, "/api/admin/students",
            new { fullName = "Học viên form mới", email = StudentApi.NewEmail(), registration }, api.AdminToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _createdId = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!.Id;
    }

    // @BR-f02-import-excel-001: đọc lại hồ sơ (JSON thô) — không có khoá của 2 mục đã bỏ
    [Then(@"^hồ sơ không có mục ""([^""]*)"" và ""([^""]*)""$")]
    public async Task HasNoFields(string first, string second)
    {
        var response = await ctx.GetAsync($"/api/admin/students/{_createdId}", api.AdminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var registration = json.RootElement.GetProperty("registration");

        foreach (var label in new[] { first, second })
        {
            Assert.False(registration.TryGetProperty(RemovedFields[label], out _), $"Hồ sơ vẫn có mục \"{label}\".");
            Assert.DoesNotContain(RegistrationFields.All, f => f.Label == label);
        }
    }

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

    // @BR-f02-ho-so-hoc-vien-008 @BR-f02-ho-so-hoc-vien-009 @BR-f02-import-excel-003 @BR-f02-import-excel-004
    // @BR-f02-import-excel-005 @BR-f02-import-excel-006
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

    // @BR-f02-ho-so-hoc-vien-011 @BR-f02-import-excel-006: hồ sơ chỉ còn một "Link ảnh toàn thân"
    [When(@"^coach lưu hồ sơ học viên có link ảnh toàn thân ""([^""]*)""$")]
    public Task SaveBodyPhoto(string url) =>
        api.TryCreateAsync("Học viên ảnh", StudentApi.NewEmail(), new RegistrationInput { BodyPhotoUrl = url });

    [Then(@"^hồ sơ ghi nhận link ảnh toàn thân ""([^""]*)""$")]
    public void BodyPhotoRecorded(string url)
    {
        Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);
        Assert.Equal(url, api.LastDetail!.Registration.BodyPhotoUrl);
    }

    // @BR-f02-ho-so-hoc-vien-011 @BR-f02-import-excel-006
    [Then("hồ sơ không được lưu vì link ảnh không hợp lệ")]
    public Task PhotoInvalid() => api.AssertErrorAsync(HttpStatusCode.BadRequest, "student.bodyPhotoUrl.invalid_url");
}
