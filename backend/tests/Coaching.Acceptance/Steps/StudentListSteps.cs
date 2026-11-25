using Coaching.Acceptance.Support;
using Coaching.Application.Students.Queries;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class StudentListSteps(AcceptanceContext ctx, StudentApi api)
{
    private IReadOnlyList<StudentListItemDto>? _rows;

    // @BR-f02-ho-so-hoc-vien-023
    [Given(@"^coach đã tạo lần lượt hồ sơ (.+)$")]
    public async Task CreatedInOrder(string names)
    {
        foreach (var name in StepText.Quoted(names))
        {
            await api.CreateOkAsync(name);
            ctx.Clock.Advance(TimeSpan.FromSeconds(1)); // CreatedAt khác nhau → thứ tự xác định
        }
    }

    [When("coach xem danh sách học viên")]
    public async Task ViewList() => _rows = await api.ListAsync(null);

    [Then(@"^danh sách lần lượt là (.+)$")]
    public void ListInOrder(string names) =>
        Assert.Equal(StepText.Quoted(names), _rows!.Select(r => r.FullName).ToList());

    [Then("mỗi dòng cho biết tên, email, trạng thái coaching và trạng thái tài khoản")]
    public void RowsShowNameEmailAndStatuses() => Assert.All(_rows!, row =>
    {
        Assert.False(string.IsNullOrWhiteSpace(row.FullName));
        Assert.Contains("@", row.Email);
        Assert.Contains(row.CoachingStatus, StudentApi.CoachingStatuses.Values);
        Assert.Contains(row.AccountStatus, StudentApi.AccountStatuses.Values);
    });

    // @BR-f02-ho-so-hoc-vien-024
    [Given(@"^có hồ sơ ""([^""]*)"" với email ""([^""]*)""$")]
    public Task HasStudent(string fullName, string email) => api.CreateOkAsync(fullName, email);

    [When(@"^coach tìm học viên với từ khóa ""([^""]*)""$")]
    public async Task Search(string keyword) => _rows = await api.ListAsync(keyword);

    [Then(@"^kết quả có hồ sơ ""([^""]*)""$")]
    public void ResultContains(string fullName) => Assert.Contains(_rows!, r => r.FullName == fullName);

    [Then("kết quả tìm kiếm rỗng")]
    public void ResultEmpty() => Assert.Empty(_rows!);

    // @BR-f02-ho-so-hoc-vien-025
    [Then(@"^hồ sơ của ""([^""]*)"" vẫn có trong danh sách học viên$")]
    public async Task StillListed(string fullName) =>
        Assert.Contains(await api.ListAsync(null), r => r.Id == api.Ids[fullName]);
}
