using System.Globalization;
using System.Net;
using Coaching.Acceptance.Support;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class CoachingStatusSteps(AcceptanceContext ctx, StudentApi api)
{
    private int _historyBefore;

    // @BR-f02-ho-so-hoc-vien-017 @BR-f02-ho-so-hoc-vien-018 @BR-f02-ho-so-hoc-vien-019 (dùng lại ở BR-023, 025)
    [Given(@"^trạng thái coaching của ""([^""]*)"" là ""([^""]*)""$")]
    public async Task StatusIs(string fullName, string label)
    {
        await api.EnsureAsync(fullName);
        var target = StudentApi.CoachingStatuses[label];
        if ((await api.GetAsync(fullName)).CoachingStatus != target)
            Assert.Equal(HttpStatusCode.OK, (await api.TryChangeStatusAsync(fullName, target, "Chuẩn bị kịch bản")).StatusCode);
    }

    [When(@"^coach đổi trạng thái coaching của ""([^""]*)"" sang ""([^""]*)"" với lý do ""([^""]*)""$")]
    public Task Change(string fullName, string label, string reason) => ChangeAsync(fullName, label, reason);

    [When(@"^coach đổi trạng thái coaching của ""([^""]*)"" sang ""([^""]*)"" với lý do chỉ gồm khoảng trắng$")]
    public Task ChangeWithBlankReason(string fullName, string label) => ChangeAsync(fullName, label, "   ");

    [Then("việc đổi trạng thái bị từ chối vì trạng thái không thay đổi")]
    public Task RejectedUnchanged() => api.AssertErrorAsync(HttpStatusCode.BadRequest, "student.status_unchanged");

    [Then("việc đổi trạng thái bị từ chối vì thiếu lý do")]
    public Task RejectedMissingReason() => api.AssertErrorAsync(HttpStatusCode.BadRequest, "student.reason_required");

    [Then(@"^trạng thái coaching của ""([^""]*)"" vẫn là ""([^""]*)""$")]
    public async Task StatusStillIs(string fullName, string label) =>
        Assert.Equal(StudentApi.CoachingStatuses[label], (await api.GetAsync(fullName)).CoachingStatus);

    // @BR-f02-ho-so-hoc-vien-019
    [Given(@"^hồ sơ của ""([^""]*)"" có mục tiêu tập luyện ""([^""]*)""$")]
    public async Task HasGoal(string fullName, string goal)
    {
        await api.EnsureAsync(fullName);
        await api.UpdateOkAsync(fullName, r => r with { TrainingGoal = goal });
    }

    // @BR-f02-ho-so-hoc-vien-019 @BR-f02-ho-so-hoc-vien-022
    [Then(@"^hồ sơ của ""([^""]*)"" (?:vẫn có|có) mục tiêu tập luyện ""([^""]*)""$")]
    public async Task GoalIs(string fullName, string goal) =>
        Assert.Equal(goal, (await api.GetAsync(fullName)).Registration.TrainingGoal);

    // @BR-f02-ho-so-hoc-vien-020
    [Then(@"^lịch sử trạng thái của ""([^""]*)"" có thêm đúng một dòng:$")]
    public async Task HistoryGainedOneRow(string fullName, DataTable table)
    {
        var detail = await api.GetAsync(fullName);
        Assert.Equal(_historyBefore + 1, detail.StatusHistory.Count);

        var expected = table.Rows.Single();
        var newest = detail.StatusHistory[0];
        Assert.Equal(StudentApi.CoachingStatuses[expected["tu"]], newest.From);
        Assert.Equal(StudentApi.CoachingStatuses[expected["sang"]], newest.To);
        Assert.Equal(expected["nguoi_thuc_hien"], newest.Actor);
        Assert.Equal(expected["thoi_diem"],
            newest.At.ToOffset(StepText.VietnamOffset).ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture));
        Assert.Equal(expected["ly_do"], newest.Reason);
    }

    // @BR-f02-ho-so-hoc-vien-021
    [Given(@"^""([^""]*)"" đã được chuyển ""([^""]*)"" sang ""([^""]*)"" rồi ""([^""]*)"" sang ""([^""]*)""$")]
    public async Task ChangedTwice(string fullName, string from1, string to1, string from2, string to2)
    {
        await api.EnsureAsync(fullName);
        Assert.Equal(StudentApi.CoachingStatuses[from1], (await api.GetAsync(fullName)).CoachingStatus);
        Assert.Equal(HttpStatusCode.OK, (await api.TryChangeStatusAsync(fullName, StudentApi.CoachingStatuses[to1], "Lần 1")).StatusCode);
        ctx.Clock.Advance(TimeSpan.FromMinutes(1)); // hai lần đổi khác thời điểm → thứ tự "mới nhất trước" xác định
        Assert.Equal(StudentApi.CoachingStatuses[from2], api.LastDetail!.CoachingStatus);
        Assert.Equal(HttpStatusCode.OK, (await api.TryChangeStatusAsync(fullName, StudentApi.CoachingStatuses[to2], "Lần 2")).StatusCode);
    }

    [When(@"^coach xem lịch sử trạng thái của ""([^""]*)""$")]
    public Task ViewHistory(string fullName) => api.GetAsync(fullName);

    [Then(@"^lịch sử có (\d+) dòng, dòng ""([^""]*)"" sang ""([^""]*)"" đứng đầu$")]
    public void HistoryNewestFirst(int count, string from, string to)
    {
        var history = api.LastDetail!.StatusHistory;
        Assert.Equal(count, history.Count);
        Assert.Equal(StudentApi.CoachingStatuses[from], history[0].From);
        Assert.Equal(StudentApi.CoachingStatuses[to], history[0].To);
    }

    // @BR-f02-ho-so-hoc-vien-022
    [Given(@"^tài khoản của ""([^""]*)"" ở trạng thái ""([^""]*)""$")]
    public async Task AccountStatusIs(string fullName, string label)
    {
        await api.EnsureAsync(fullName);
        Assert.Equal(StudentApi.AccountStatuses[label], (await api.GetAsync(fullName)).AccountStatus);
    }

    [When(@"^coach sửa mục tiêu tập luyện của ""([^""]*)"" thành ""([^""]*)""$")]
    public Task EditGoalTo(string fullName, string goal) => api.UpdateOkAsync(fullName, r => r with { TrainingGoal = goal });

    private async Task ChangeAsync(string fullName, string label, string reason)
    {
        await api.EnsureAsync(fullName);
        _historyBefore = (await api.GetAsync(fullName)).StatusHistory.Count;
        await api.TryChangeStatusAsync(fullName, StudentApi.CoachingStatuses[label], reason);
    }
}
