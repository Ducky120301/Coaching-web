using System.Net;
using System.Net.Http.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Exercises;
using Coaching.Application.Exercises.Commands;

namespace Coaching.Api.IntegrationTests.Exercises;

[Collection(ApiCollection.Name)]
public sealed class ExerciseDetailApiTests(ApiFactory factory)
{
    private static string Name(string label) => ExerciseRequests.UniqueName(label);

    private async Task<HttpResponseMessage> PostAsync(Func<SaveExerciseCommand, SaveExerciseCommand> change)
    {
        var admin = await factory.AdminClientAsync();
        return await admin.PostAsJsonAsync(ExerciseRequests.Url,
            change(await ExerciseRequests.CommandAsync(admin, Name("Chest Dip"), "Ngực", "Bodyweight")));
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, string code)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(code, await StudentRequests.ErrorCodeAsync(response));
    }

    // @BR-f03-thu-vien-bai-tap-007 @BR-f03-thu-vien-bai-tap-008 @ADR-f03-thu-vien-bai-tap-006 @BR-f03-thu-vien-bai-tap-013 @BR-f03-thu-vien-bai-tap-014
    [Fact]
    public async Task Create_and_update_store_video_dose_and_notes()
    {
        var admin = await factory.AdminClientAsync();
        var created = await ExerciseRequests.CreateAsync(admin, Name("Lateral Raise"), "Vai", "Cable", c => c with
        {
            VideoUrl = " https://www.youtube.com/shorts/lMJUXEvcMkQ ",
            Sets = 3, RepsMin = 12, RepsMax = 15, RirMin = 0, RirMax = 1, RestSeconds = 60,
            TechniqueNotes = "Giữ khuỷu hơi cong, kéo bằng vai",
            CommonMistakes = "Nhún vai khi kéo",
        });

        Assert.Equal("https://www.youtube.com/shorts/lMJUXEvcMkQ", created.VideoUrl);
        Assert.True(created.HasVideo);
        Assert.Equal(new int?[] { 3, 12, 15, 0, 1, 60 },
            new[] { created.Sets, created.RepsMin, created.RepsMax, created.RirMin, created.RirMax, created.RestSeconds });
        Assert.Equal("Giữ khuỷu hơi cong, kéo bằng vai", created.TechniqueNotes);
        Assert.Equal("Nhún vai khi kéo", created.CommonMistakes);

        var response = await ExerciseRequests.UpdateAsync(admin, created, c => c with
        {
            VideoUrl = null, Sets = null, RepsMin = 10, RepsMax = null, RirMin = null, RirMax = null, RestSeconds = null,
            TechniqueNotes = "Mới", CommonMistakes = "  ",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await ExerciseRequests.GetAsync(admin, created.Id);
        Assert.False(updated.HasVideo);
        Assert.Null(updated.VideoUrl);
        Assert.Equal(new int?[] { null, 10, 10, null, null, null },
            new[] { updated.Sets, updated.RepsMin, updated.RepsMax, updated.RirMin, updated.RirMax, updated.RestSeconds });
        Assert.Equal("Mới", updated.TechniqueNotes);
        Assert.Null(updated.CommonMistakes);
    }

    // @BR-f03-thu-vien-bai-tap-007: link không phải YouTube → 400, bài không được thêm
    [Theory]
    [InlineData("https://vimeo.com/123")]
    [InlineData("https://youtube.com.evil.com/watch?v=x")]
    [InlineData("javascript:alert(1)")]
    [InlineData("abc")]
    public async Task BR007_Non_YouTube_link_is_rejected(string link) =>
        await AssertProblemAsync(await PostAsync(c => c with { VideoUrl = link }), "exercise.video_not_youtube");

    // @BR-f03-thu-vien-bai-tap-009: Sets "abc" (seed ❌) — không phải số → lỗi đọc body 400 request.invalid
    [Fact]
    public async Task BR009_Sets_that_is_not_a_number_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var command = await ExerciseRequests.CommandAsync(admin, Name("Chest Press"));

        var response = await admin.PostAsJsonAsync(ExerciseRequests.Url, new
        {
            name = command.Name,
            muscleGroupId = command.MuscleGroupId,
            equipmentId = command.EquipmentId,
            sets = "abc",
        });

        await AssertProblemAsync(response, "request.invalid");
    }

    // @BR-f03-thu-vien-bai-tap-009 @BR-f03-thu-vien-bai-tap-010 @BR-f03-thu-vien-bai-tap-011 @BR-f03-thu-vien-bai-tap-012: ngoài khoảng → 400 đúng mã
    [Fact]
    public async Task BR009_BR012_Dose_out_of_range_is_rejected()
    {
        await AssertProblemAsync(await PostAsync(c => c with { Sets = 11 }), "exercise.sets_out_of_range");
        await AssertProblemAsync(await PostAsync(c => c with { RepsMin = 12, RepsMax = 10 }), "exercise.reps_invalid");
        await AssertProblemAsync(await PostAsync(c => c with { RepsMin = 101 }), "exercise.reps_invalid");
        await AssertProblemAsync(await PostAsync(c => c with { RirMin = 2, RirMax = 1 }), "exercise.rir_invalid");
        await AssertProblemAsync(await PostAsync(c => c with { RirMin = 6 }), "exercise.rir_invalid");
        await AssertProblemAsync(await PostAsync(c => c with { RestSeconds = 10 }), "exercise.rest_out_of_range");
        await AssertProblemAsync(await PostAsync(c => c with { RestSeconds = 601 }), "exercise.rest_out_of_range");
    }

    // @BR-f03-thu-vien-bai-tap-013 @BR-f03-thu-vien-bai-tap-014: ghi chú 2001 ký tự (seed ❌) → 400; 2000 lưu được
    [Fact]
    public async Task BR013_BR014_Notes_of_2001_characters_are_rejected()
    {
        await AssertProblemAsync(await PostAsync(c => c with { TechniqueNotes = new string('a', 2001) }), "exercise.notes_too_long");
        await AssertProblemAsync(await PostAsync(c => c with { CommonMistakes = new string('a', 2001) }), "exercise.notes_too_long");

        var ok = await PostAsync(c => c with { TechniqueNotes = new string('a', 2000), CommonMistakes = new string('b', 2000) });
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        Assert.Equal(2000, (await ok.Content.ReadFromJsonAsync<ExerciseDetailDto>())!.CommonMistakes!.Length);
    }
}
