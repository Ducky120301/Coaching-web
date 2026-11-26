using System.Text.RegularExpressions;
using Coaching.Acceptance.Support;
using Coaching.Application.Exercises.Queries;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class ExerciseListSteps(ExerciseApi exercises)
{
    private IReadOnlyList<ExerciseListItemDto> _results = [];

    private string[] Names => _results.Select(r => r.Name).ToArray();

    // @BR-f03-thu-vien-bai-tap-018
    [Given(@"^thư viện có bài ""([^""]*)""$")]
    public Task Has(string name) => exercises.CreateOkAsync(name, "Ngực", "Machine");

    // @BR-f03-thu-vien-bai-tap-016
    [Given(@"^thư viện có bài ""([^""]*)"" đang ẩn$")]
    public async Task HasHidden(string name)
    {
        await exercises.CreateOkAsync(name, "Ngực", "Bodyweight");
        await exercises.HideAsync(name);
    }

    // @BR-f03-thu-vien-bai-tap-016
    [Given(@"^thư viện có bài ""([^""]*)"" đang ẩn và bài ""([^""]*)"" đang hiện$")]
    public async Task HasHiddenAndVisible(string hidden, string visible)
    {
        await HasHidden(hidden);
        await exercises.CreateOkAsync(visible, "Ngực", "Machine");
    }

    // @BR-f03-thu-vien-bai-tap-019: "Tên" (Nhóm cơ, Dụng cụ), … và "Tên" (…)
    [Given(@"^thư viện có ((?:""[^""]+"" \([^)]+\)(?:, | và )?)+)$")]
    public async Task HasMany(string list)
    {
        foreach (Match m in Regex.Matches(list, "\"([^\"]+)\" \\(([^,]+), ([^)]+)\\)"))
            await exercises.CreateOkAsync(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value);
    }

    // @BR-f03-thu-vien-bai-tap-016
    [When(@"^coach xem danh sách bài tập$")]
    public async Task ViewList() => _results = await exercises.ListAsync();

    // @BR-f03-thu-vien-bai-tap-016
    [When(@"^coach xem các bài đã ẩn$")]
    public async Task ViewHidden() => _results = await exercises.ListAsync(hidden: true);

    // @BR-f03-thu-vien-bai-tap-018
    [When(@"^coach tìm bài với từ khoá ""([^""]*)""$")]
    public async Task Search(string keyword) => _results = await exercises.ListAsync(q: keyword);

    // @BR-f03-thu-vien-bai-tap-019
    [When(@"^coach lọc nhóm cơ ""([^""]*)"", dụng cụ ""([^""]*)"" và tìm ""([^""]*)""$")]
    public async Task FilterAll(string muscleGroup, string equipment, string keyword) =>
        _results = await exercises.ListAsync(keyword, muscleGroup, equipment);

    // @BR-f03-thu-vien-bai-tap-019
    [When(@"^coach lọc dụng cụ ""([^""]*)""$")]
    public async Task FilterEquipment(string equipment) => _results = await exercises.ListAsync(equipment: equipment);

    // @BR-f03-thu-vien-bai-tap-016
    [Then(@"^danh sách có ""([^""]*)"" và không có ""([^""]*)""$")]
    public void ListHasNot(string present, string absent)
    {
        Assert.Contains(present, Names);
        Assert.DoesNotContain(absent, Names);
    }

    // @BR-f03-thu-vien-bai-tap-016
    [Then(@"^danh sách có ""([^""]*)""$")]
    public void ListHas(string name) => Assert.Contains(_results, r => r.Name == name && r.IsHidden);

    // @BR-f03-thu-vien-bai-tap-018
    [Then(@"^kết quả tìm rỗng$")]
    public void ResultsEmpty() => Assert.Empty(_results);

    // @BR-f03-thu-vien-bai-tap-019
    [Then(@"^kết quả chỉ có ""([^""]*)""$")]
    public void ResultsOnly(string name) => Assert.Equal(new[] { name }, Names);

    // @BR-f03-thu-vien-bai-tap-019
    [Then(@"^kết quả có ""([^""]*)"" và ""([^""]*)"", không có ""([^""]*)""$")]
    public void ResultsHaveNot(string first, string second, string absent)
    {
        Assert.Contains(first, Names);
        Assert.Contains(second, Names);
        Assert.DoesNotContain(absent, Names);
    }
}
