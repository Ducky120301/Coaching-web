using System.Net;
using Coaching.Acceptance.Support;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class StudentEditSteps(AcceptanceContext ctx, StudentApi api)
{
    private string? _fullName;
    private string? _openedVersion;

    // @BR-f02-ho-so-hoc-vien-012
    [Given(@"^hồ sơ của ""([^""]*)"" có cân nặng (\d+) kg$")]
    public async Task HasWeight(string fullName, int kg)
    {
        await api.EnsureAsync(fullName);
        await api.UpdateOkAsync(fullName, r => r with { WeightKg = kg });
    }

    [When(@"^coach sửa cân nặng của ""([^""]*)"" thành (\d+) kg$")]
    public Task EditWeight(string fullName, int kg) => api.UpdateOkAsync(fullName, r => r with { WeightKg = kg });

    [Then(@"^hồ sơ của ""([^""]*)"" ghi nhận cân nặng (\d+) kg$")]
    public async Task WeightIs(string fullName, int kg) =>
        Assert.Equal(kg, (await api.GetAsync(fullName)).Registration.WeightKg);

    // @BR-f02-ho-so-hoc-vien-013 (dùng lại ở BR-014)
    [Given(@"^hồ sơ của ""([^""]*)"" đã có$")]
    public Task Exists(string fullName) => api.EnsureAsync(fullName);

    [Given(@"^bây giờ là (\d\d):(\d\d) ngày ""([^""]*)"" giờ Việt Nam$")]
    public Task NowIs(int hour, int minute, string date) => api.MoveClockToAsync(StepText.VietnamTime(date, hour, minute));

    [When(@"^coach sửa mục tiêu tập luyện của ""([^""]*)""$")]
    public Task EditGoal(string fullName) =>
        api.UpdateOkAsync(fullName, r => r with { TrainingGoal = $"Mục tiêu {Guid.NewGuid():N}" });

    [Then(@"^hồ sơ của ""([^""]*)"" cho biết sửa lần cuối lúc (\d\d):(\d\d) ngày ""([^""]*)"" bởi ""([^""]*)""$")]
    public async Task LastEdited(string fullName, int hour, int minute, string date, string editor)
    {
        var detail = await api.GetAsync(fullName);
        Assert.Equal(StepText.VietnamTime(date, hour, minute), detail.UpdatedAt);
        Assert.Equal(editor, detail.UpdatedBy);
    }

    // @BR-f02-ho-so-hoc-vien-015
    [Given(@"^coach đang xem phiên bản mới nhất của hồ sơ ""([^""]*)""$")]
    public async Task ViewingLatest(string fullName)
    {
        await api.EnsureAsync(fullName);
        _openedVersion = (await api.GetAsync(fullName)).Version;
    }

    [When(@"^coach lưu thay đổi mục tiêu tập luyện của ""([^""]*)""$")]
    public Task SaveGoal(string fullName) =>
        api.TryUpdateAsync(fullName, r => r with { TrainingGoal = "Tăng cơ, giảm mỡ" }, _openedVersion);

    [Then("thay đổi được lưu")]
    public void ChangeSaved() => Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);

    [Given(@"^coach mở hồ sơ ""([^""]*)"" ở hai nơi cùng một phiên bản$")]
    public async Task OpenedTwice(string fullName)
    {
        await api.EnsureAsync(fullName);
        _fullName = fullName;
        _openedVersion = (await api.GetAsync(fullName)).Version;
    }

    [Given("thay đổi ở nơi thứ nhất đã được lưu")]
    public async Task FirstPlaceSaved()
    {
        var response = await api.TryUpdateAsync(_fullName!, r => r with { TrainingGoal = "Nơi thứ nhất" }, _openedVersion);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [When("coach lưu thay đổi ở nơi thứ hai")]
    public Task SecondPlaceSaves() =>
        api.TryUpdateAsync(_fullName!, r => r with { TrainingGoal = "Nơi thứ hai" }, _openedVersion);

    [Then("thay đổi ở nơi thứ hai không được lưu vì hồ sơ đã được sửa ở nơi khác")]
    public Task SecondPlaceRejected() => api.AssertErrorAsync(HttpStatusCode.Conflict, "student.version_conflict");

    [Then(@"^hồ sơ của ""([^""]*)"" giữ thay đổi của nơi thứ nhất$")]
    public async Task KeepsFirstPlace(string fullName) =>
        Assert.Equal("Nơi thứ nhất", (await api.GetAsync(fullName)).Registration.TrainingGoal);
}
