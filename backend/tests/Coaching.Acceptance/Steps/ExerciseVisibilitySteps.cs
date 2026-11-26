using System.Net;
using Coaching.Acceptance.Support;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class ExerciseVisibilitySteps(AcceptanceContext ctx, ExerciseApi exercises)
{
    private readonly Dictionary<string, Guid> _deleted = new();

    // @BR-f03-thu-vien-bai-tap-015
    [When(@"^coach ẩn bài ""([^""]*)""$")]
    public Task Hide(string name) => exercises.TryHideAsync(name);

    // @BR-f03-thu-vien-bai-tap-015
    [When(@"^coach bỏ ẩn bài ""([^""]*)""$")]
    public Task Unhide(string name) => exercises.TryUnhideAsync(name);

    // @BR-f03-thu-vien-bai-tap-015
    [Then(@"^bài ""([^""]*)"" ở trạng thái đã ẩn$")]
    public async Task IsHidden(string name)
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        Assert.True((await exercises.GetAsync(name)).IsHidden);
    }

    // @BR-f03-thu-vien-bai-tap-015
    [Then(@"^bài ""([^""]*)"" không còn ở trạng thái đã ẩn$")]
    public async Task IsVisible(string name)
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        Assert.False((await exercises.GetAsync(name)).IsHidden);
    }

    // @BR-f03-thu-vien-bai-tap-017 @ADR-f03-thu-vien-bai-tap-004: P1 chưa có giáo án — NoExerciseUsage
    [Given(@"^thư viện có bài ""([^""]*)"" dụng cụ ""([^""]*)"" chưa có giáo án nào dùng$")]
    public Task LibraryHasUnused(string name, string equipment) => exercises.CreateOkAsync(name, "Ngực", equipment);

    // @BR-f03-thu-vien-bai-tap-017
    [When(@"^coach xoá bài ""([^""]*)""$")]
    public async Task Delete(string name)
    {
        _deleted[name] = exercises.IdOf(name);
        await exercises.TryDeleteAsync(name);
    }

    // @BR-f03-thu-vien-bai-tap-017
    [Given(@"^coach đã xoá bài ""([^""]*)""$")]
    public async Task Deleted(string name)
    {
        await Delete(name);
        Assert.Equal(HttpStatusCode.NoContent, ctx.LastResponse!.StatusCode);
    }

    // @BR-f03-thu-vien-bai-tap-017: bài đã xoá (Id lúc xoá) không còn — dụng cụ chỉ để đọc cho rõ
    [Then(@"^thư viện không còn bài ""([^""]*)"" dụng cụ ""([^""]*)""$")]
    public async Task LibraryNoLongerHas(string name, string equipment)
    {
        Assert.Equal(HttpStatusCode.NoContent, ctx.LastResponse!.StatusCode);
        using var response = await exercises.TryGetAsync(_deleted[name]);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("exercise.not_found", await StudentApi.ErrorCodeAsync(response));
    }
}
