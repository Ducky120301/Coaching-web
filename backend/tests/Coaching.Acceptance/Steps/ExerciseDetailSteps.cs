using System.Net;
using Coaching.Acceptance.Support;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class ExerciseDetailSteps(AcceptanceContext ctx, ExerciseApi exercises)
{
    // @BR-f03-thu-vien-bai-tap-007
    [When(@"^coach thêm bài ""([^""]*)"" dụng cụ ""([^""]*)"" nhóm cơ ""([^""]*)"" với video ""([^""]*)""$")]
    public async Task AddWithVideo(string name, string equipment, string muscleGroup, string link) =>
        await exercises.TryCreateAsync((await exercises.CommandAsync(name, muscleGroup, equipment)) with { VideoUrl = link });

    // @BR-f03-thu-vien-bai-tap-008
    [When(@"^coach thêm bài ""([^""]*)"" dụng cụ ""([^""]*)"" nhóm cơ ""([^""]*)"" không có video$")]
    public Task AddWithoutVideo(string name, string equipment, string muscleGroup) =>
        exercises.TryCreateAsync(name, muscleGroup, equipment);

    // @BR-f03-thu-vien-bai-tap-007 @ADR-f03-thu-vien-bai-tap-005: lưu nguyên văn
    [Then(@"^bài ""([^""]*)"" có video ""([^""]*)""$")]
    public async Task HasVideo(string name, string link)
    {
        Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);
        Assert.Equal(link, (await exercises.GetAsync(name)).VideoUrl);
    }

    // @BR-f03-thu-vien-bai-tap-007
    [Then(@"^bài không được thêm vì link video không phải YouTube$")]
    public Task VideoNotYouTube() => exercises.AssertErrorAsync(HttpStatusCode.BadRequest, "exercise.video_not_youtube");

    // @BR-f03-thu-vien-bai-tap-008
    [Then(@"^bài ""([^""]*)"" bị đánh dấu thiếu video$")]
    public async Task MarkedMissingVideo(string name) => Assert.False((await exercises.GetAsync(name)).HasVideo);

    // @BR-f03-thu-vien-bai-tap-008
    [Then(@"^bài ""([^""]*)"" không bị đánh dấu thiếu video$")]
    public async Task NotMarkedMissingVideo(string name) => Assert.True((await exercises.GetAsync(name)).HasVideo);

    // @BR-f03-thu-vien-bai-tap-009
    [When(@"^coach cập nhật bài ""([^""]*)"" với Sets mặc định (\d+)$")]
    public Task UpdateSets(string name, int sets) => exercises.TryUpdateAsync(name, c => c with { Sets = sets });

    // @BR-f03-thu-vien-bai-tap-009
    [When(@"^coach cập nhật bài ""([^""]*)"" không có Sets mặc định$")]
    public Task UpdateWithoutSets(string name) => exercises.TryUpdateAsync(name, c => c with { Sets = null });

    // @BR-f03-thu-vien-bai-tap-009
    [Then(@"^bài ""([^""]*)"" có Sets mặc định (\d+)$")]
    public async Task HasSets(string name, int sets) => Assert.Equal(sets, (await exercises.GetAsync(name)).Sets);

    // @BR-f03-thu-vien-bai-tap-009
    [Then(@"^bài ""([^""]*)"" được lưu, chưa có Sets mặc định$")]
    public async Task SavedWithoutSets(string name)
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        Assert.Null((await exercises.GetAsync(name)).Sets);
    }

    // @BR-f03-thu-vien-bai-tap-009
    [Then(@"^bài không được lưu vì Sets phải từ 1 đến 10$")]
    public Task SetsOutOfRange() => exercises.AssertErrorAsync(HttpStatusCode.BadRequest, "exercise.sets_out_of_range");

    // @BR-f03-thu-vien-bai-tap-010 @ADR-f03-thu-vien-bai-tap-006: "10" → 10/10; "10–12", "10-12" → 10/12
    [When(@"^coach cập nhật bài ""([^""]*)"" với Reps mặc định ""([^""]*)""$")]
    public Task UpdateReps(string name, string reps)
    {
        var (min, max) = DoseText.ParseRange(reps);
        return exercises.TryUpdateAsync(name, c => c with { RepsMin = min, RepsMax = max });
    }

    // @BR-f03-thu-vien-bai-tap-010
    [Then(@"^bài ""([^""]*)"" có Reps mặc định ""([^""]*)""$")]
    public async Task HasReps(string name, string reps)
    {
        var exercise = await exercises.GetAsync(name);
        Assert.Equal(reps, DoseText.FormatRange(exercise.RepsMin, exercise.RepsMax));
    }

    // @BR-f03-thu-vien-bai-tap-010
    [Then(@"^bài không được lưu vì Reps không hợp lệ$")]
    public Task RepsInvalid() => exercises.AssertErrorAsync(HttpStatusCode.BadRequest, "exercise.reps_invalid");

    // @BR-f03-thu-vien-bai-tap-011
    [When(@"^coach cập nhật bài ""([^""]*)"" với RIR mặc định ""([^""]*)""$")]
    public Task UpdateRir(string name, string rir)
    {
        var (min, max) = DoseText.ParseRange(rir);
        return exercises.TryUpdateAsync(name, c => c with { RirMin = min, RirMax = max });
    }

    // @BR-f03-thu-vien-bai-tap-011
    [Then(@"^bài ""([^""]*)"" có RIR mặc định ""([^""]*)""$")]
    public async Task HasRir(string name, string rir)
    {
        var exercise = await exercises.GetAsync(name);
        Assert.Equal(rir, DoseText.FormatRange(exercise.RirMin, exercise.RirMax));
    }

    // @BR-f03-thu-vien-bai-tap-011
    [Then(@"^bài không được lưu vì RIR không hợp lệ$")]
    public Task RirInvalid() => exercises.AssertErrorAsync(HttpStatusCode.BadRequest, "exercise.rir_invalid");

    // @BR-f03-thu-vien-bai-tap-012
    [When(@"^coach cập nhật bài ""([^""]*)"" với thời gian nghỉ mặc định (\d+) giây$")]
    public Task UpdateRest(string name, int seconds) => exercises.TryUpdateAsync(name, c => c with { RestSeconds = seconds });

    // @BR-f03-thu-vien-bai-tap-012: đọc là "90s"
    [Then(@"^bài ""([^""]*)"" có thời gian nghỉ mặc định ""([^""]*)""$")]
    public async Task HasRest(string name, string rest) =>
        Assert.Equal(rest, DoseText.FormatRest((await exercises.GetAsync(name)).RestSeconds));

    // @BR-f03-thu-vien-bai-tap-012
    [Then(@"^bài không được lưu vì thời gian nghỉ phải từ 15 đến 600 giây$")]
    public Task RestOutOfRange() => exercises.AssertErrorAsync(HttpStatusCode.BadRequest, "exercise.rest_out_of_range");

    // @BR-f03-thu-vien-bai-tap-013 @BR-f03-thu-vien-bai-tap-021
    [When(@"^coach cập nhật bài ""([^""]*)"" với hướng dẫn kỹ thuật ""([^""]*)""$")]
    public Task UpdateTechnique(string name, string text) => exercises.TryUpdateAsync(name, c => c with { TechniqueNotes = text });

    // @BR-f03-thu-vien-bai-tap-013 @BR-f03-thu-vien-bai-tap-021
    [Then(@"^bài ""([^""]*)"" có hướng dẫn kỹ thuật ""([^""]*)""$")]
    public async Task HasTechnique(string name, string text) =>
        Assert.Equal(text, (await exercises.GetAsync(name)).TechniqueNotes);

    // @BR-f03-thu-vien-bai-tap-014
    [When(@"^coach cập nhật bài ""([^""]*)"" với lỗi hay gặp ""([^""]*)""$")]
    public Task UpdateMistakes(string name, string text) => exercises.TryUpdateAsync(name, c => c with { CommonMistakes = text });

    // @BR-f03-thu-vien-bai-tap-014
    [Then(@"^bài ""([^""]*)"" có lỗi hay gặp ""([^""]*)""$")]
    public async Task HasMistakes(string name, string text) =>
        Assert.Equal(text, (await exercises.GetAsync(name)).CommonMistakes);

    // @BR-f03-thu-vien-bai-tap-021
    [Given(@"^thư viện có bài ""([^""]*)"" với hướng dẫn kỹ thuật ""([^""]*)""$")]
    public Task LibraryHasWithTechnique(string name, string text) =>
        exercises.CreateOkAsync(name, "Vai", "Cable", c => c with { TechniqueNotes = text });
}
