using System.Net;
using Coaching.Acceptance.Support;
using Coaching.Application.Exercises.Queries;
using Coaching.Domain.Exercises;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class ExerciseCategorySteps(AcceptanceContext ctx, ExerciseCatalogApi catalog)
{
    private CategoriesDto? _seen;

    private static string[] Split(string names) => names.Split(", ", StringSplitOptions.TrimEntries);

    // @BR-f03-thu-vien-bai-tap-005
    [When(@"^coach xem danh mục của thư viện bài tập$")]
    public async Task ViewCatalog() => _seen = await catalog.GetAsync();

    // @BR-f03-thu-vien-bai-tap-005 @ADR-f03-thu-vien-bai-tap-003
    [Then(@"^danh mục nhóm cơ gồm ""([^""]*)""$")]
    public void MuscleGroupsAre(string names) =>
        Assert.Equal(Split(names), _seen!.MuscleGroups.Select(c => c.Name).ToArray());

    // @BR-f03-thu-vien-bai-tap-005 @ADR-f03-thu-vien-bai-tap-003
    [Then(@"^danh mục dụng cụ gồm ""([^""]*)""$")]
    public void EquipmentIs(string names) =>
        Assert.Equal(Split(names), _seen!.Equipment.Select(c => c.Name).ToArray());

    // @BR-f03-thu-vien-bai-tap-006
    [Given(@"^coach đã thêm dụng cụ ""([^""]*)""$")]
    public async Task EquipmentAdded(string name) =>
        Assert.Equal(HttpStatusCode.Created, (await catalog.TryAddAsync("Equipment", name)).StatusCode);

    // @BR-f03-thu-vien-bai-tap-006
    [When(@"^coach thêm nhóm cơ ""([^""]*)""$")]
    public Task AddMuscleGroup(string name) => catalog.TryAddAsync("MuscleGroup", name);

    // @BR-f03-thu-vien-bai-tap-006 @ADR-f03-thu-vien-bai-tap-002: báo trùng, danh mục vẫn chỉ có một mục cùng khoá
    [Then(@"^nhóm cơ không được thêm vì đã có ""([^""]*)""$")]
    public async Task MuscleGroupDuplicate(string existing)
    {
        Assert.Equal(HttpStatusCode.Conflict, ctx.LastResponse!.StatusCode);
        Assert.Equal("category.duplicate", await StudentApi.ErrorCodeAsync(ctx.LastResponse));
        var sameKey = (await catalog.GetAsync()).MuscleGroups
            .Where(c => ExerciseNameKey.For(c.Name) == ExerciseNameKey.For(existing))
            .Select(c => c.Name)
            .ToList();
        Assert.Equal(new[] { existing }, sameKey);
    }
}
