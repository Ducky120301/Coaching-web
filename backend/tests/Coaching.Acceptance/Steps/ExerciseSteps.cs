using System.Net;
using System.Text.Json;
using Coaching.Acceptance.Support;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class ExerciseSteps(AcceptanceContext ctx, ExerciseApi exercises, ExerciseCatalogApi catalog)
{
    private const string DefaultMuscleGroup = "Ngực";
    private string? _staleVersion;

    // @BR-f03-thu-vien-bai-tap-001
    [Given(@"^thư viện có bài ""([^""]*)"" dụng cụ ""([^""]*)""$")]
    public Task LibraryHas(string name, string equipment) => exercises.CreateOkAsync(name, DefaultMuscleGroup, equipment);

    // @BR-f03-thu-vien-bai-tap-001
    [Given(@"^thư viện có bài ""([^""]*)"" dụng cụ ""([^""]*)"" và bài ""([^""]*)"" dụng cụ ""([^""]*)""$")]
    public async Task LibraryHasTwo(string first, string firstEquipment, string second, string secondEquipment)
    {
        await exercises.CreateOkAsync(first, DefaultMuscleGroup, firstEquipment);
        await exercises.CreateOkAsync(second, DefaultMuscleGroup, secondEquipment);
    }

    // @BR-f03-thu-vien-bai-tap-001 (dùng lại ở BR-015)
    [Given(@"^thư viện có bài ""([^""]*)"" dụng cụ ""([^""]*)"" đang ẩn$")]
    public async Task LibraryHasHidden(string name, string equipment)
    {
        await exercises.CreateOkAsync(name, DefaultMuscleGroup, equipment);
        await exercises.HideAsync(name);
    }

    // @BR-f03-thu-vien-bai-tap-001 @BR-f03-thu-vien-bai-tap-002 @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004
    [When(@"^coach thêm bài ""([^""]*)"" dụng cụ ""([^""]*)"" nhóm cơ ""([^""]*)""$")]
    public Task Add(string name, string equipment, string muscleGroup) => exercises.TryCreateAsync(name, muscleGroup, equipment);

    // @BR-f03-thu-vien-bai-tap-002
    [When(@"^coach thêm một bài không có tên, dụng cụ ""([^""]*)"" nhóm cơ ""([^""]*)""$")]
    public Task AddWithoutName(string equipment, string muscleGroup) => exercises.TryCreateAsync(null, muscleGroup, equipment);

    // @BR-f03-thu-vien-bai-tap-003
    [When(@"^coach thêm bài ""([^""]*)"" dụng cụ ""([^""]*)"", không có nhóm cơ$")]
    public Task AddWithoutMuscleGroup(string name, string equipment) => exercises.TryCreateAsync(name, null, equipment);

    // @BR-f03-thu-vien-bai-tap-004
    [When(@"^coach thêm bài ""([^""]*)"" nhóm cơ ""([^""]*)"", không có dụng cụ$")]
    public Task AddWithoutEquipment(string name, string muscleGroup) => exercises.TryCreateAsync(name, muscleGroup, null);

    // @BR-f03-thu-vien-bai-tap-001: "bài đó" = bài vừa tạo ở Given
    [When(@"^coach đổi tên bài đó thành ""([^""]*)""$")]
    public Task RenameLast(string newName) => exercises.TryUpdateAsync(exercises.LastCreatedName!, c => c with { Name = newName });

    // @BR-f03-thu-vien-bai-tap-001
    [When(@"^coach đổi tên bài ""([^""]*)"" thành ""([^""]*)""$")]
    public Task Rename(string name, string newName) => exercises.TryUpdateAsync(name, c => c with { Name = newName });

    // @BR-f03-thu-vien-bai-tap-020: giữ version lúc mở sửa, rồi "nơi khác" lưu đổi nhóm cơ sang Vai
    [Given(@"^coach đang sửa bài ""([^""]*)"" và bài đó vừa được sửa ở nơi khác$")]
    public async Task EditingWhileChangedElsewhere(string name)
    {
        _staleVersion = (await exercises.CreateOkAsync(name, DefaultMuscleGroup, "Machine")).Version;
        var vai = await catalog.MuscleGroupIdAsync("Vai");
        Assert.Equal(HttpStatusCode.OK, (await exercises.TryUpdateAsync(name, c => c with { MuscleGroupId = vai })).StatusCode);
    }

    // @BR-f03-thu-vien-bai-tap-020
    [When(@"^coach cập nhật bài ""([^""]*)"" dựa trên phiên bản đang sửa$")]
    public async Task UpdateFromStaleVersion(string name)
    {
        var lung = await catalog.MuscleGroupIdAsync("Lưng");
        await exercises.TryUpdateAsync(name, c => c with { MuscleGroupId = lung }, _staleVersion);
    }

    // @BR-f03-thu-vien-bai-tap-001 @BR-f03-thu-vien-bai-tap-002 @BR-f03-thu-vien-bai-tap-006 @BR-f03-thu-vien-bai-tap-017
    [Then(@"^thư viện có bài ""([^""]*)"" dụng cụ ""([^""]*)""$")]
    public async Task LibraryContains(string name, string equipment)
    {
        var exercise = await exercises.GetAsync(name);
        Assert.Equal(name, exercise.Name);
        Assert.Equal(equipment, exercise.Equipment);
    }

    // @BR-f03-thu-vien-bai-tap-001
    [Then(@"^bài không được (?:thêm|lưu) vì đã có bài cùng tên và dụng cụ$")]
    public Task Duplicate() => exercises.AssertErrorAsync(HttpStatusCode.Conflict, "exercise.duplicate");

    // @BR-f03-thu-vien-bai-tap-001: lỗi trùng chỉ tới bài đang ẩn
    [Then(@"^coach được gợi ý bỏ ẩn bài ""([^""]*)"" dụng cụ ""([^""]*)""$")]
    public async Task SuggestedToUnhide(string name, string equipment)
    {
        using var json = JsonDocument.Parse(await ctx.LastResponse!.Content.ReadAsStringAsync());
        Assert.Equal(exercises.IdOf(name), json.RootElement.GetProperty("hiddenExerciseId").GetGuid());
        var hidden = await exercises.GetAsync(name);
        Assert.True(hidden.IsHidden);
        Assert.Equal(equipment, hidden.Equipment);
    }

    // @BR-f03-thu-vien-bai-tap-001
    [Then(@"^bài ""([^""]*)"" giữ nguyên$")]
    public async Task Unchanged(string name) => Assert.Equal(name, (await exercises.GetAsync(name)).Name);

    // @BR-f03-thu-vien-bai-tap-002 @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004
    [Then(@"^bài không được thêm vì thiếu (tên|nhóm cơ|dụng cụ)$")]
    public Task Missing(string what) => exercises.AssertErrorAsync(HttpStatusCode.BadRequest, what switch
    {
        "tên" => "exercise.name_required",
        "nhóm cơ" => "exercise.muscle_group_required",
        _ => "exercise.equipment_required",
    });

    // @BR-f03-thu-vien-bai-tap-003
    [Then(@"^bài ""([^""]*)"" thuộc nhóm cơ ""([^""]*)""$")]
    public async Task HasMuscleGroup(string name, string muscleGroup) =>
        Assert.Equal(muscleGroup, (await exercises.GetAsync(name)).MuscleGroup);

    // @BR-f03-thu-vien-bai-tap-004
    [Then(@"^bài ""([^""]*)"" dùng dụng cụ ""([^""]*)""$")]
    public async Task UsesEquipment(string name, string equipment) =>
        Assert.Equal(equipment, (await exercises.GetAsync(name)).Equipment);

    // @BR-f03-thu-vien-bai-tap-020
    [Then(@"^bài không được lưu vì vừa được sửa ở nơi khác$")]
    public Task VersionConflict() => exercises.AssertErrorAsync(HttpStatusCode.Conflict, "exercise.version_conflict");

    // @BR-f03-thu-vien-bai-tap-020: lần lưu ở nơi khác (nhóm cơ Vai) không bị ghi đè
    [Then(@"^bài ""([^""]*)"" giữ nội dung của lần lưu trước$")]
    public async Task KeepsPreviousSave(string name) => Assert.Equal("Vai", (await exercises.GetAsync(name)).MuscleGroup);
}
