using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Exercises;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Coaching.Api.IntegrationTests.Exercises;

[Collection(ApiCollection.Name)]
public sealed class ExerciseHideDeleteTests(ApiFactory factory)
{
    private static string Name(string label) => ExerciseRequests.UniqueName(label);

    private static string Url(Guid id, string? action = null) =>
        action is null ? $"{ExerciseRequests.Url}/{id}" : $"{ExerciseRequests.Url}/{id}/{action}";

    // @BR-f03-thu-vien-bai-tap-015: ẩn → đã ẩn; bỏ ẩn → hết ẩn; gọi lại không lỗi
    [Fact]
    public async Task BR015_Hide_and_unhide_through_the_api()
    {
        var admin = await factory.AdminClientAsync();
        var exercise = await ExerciseRequests.CreateAsync(admin, Name("Chest Dip"), "Ngực", "Bodyweight");

        var hidden = await admin.PostAsync(Url(exercise.Id, "hide"), null);
        Assert.Equal(HttpStatusCode.OK, hidden.StatusCode);
        Assert.True((await hidden.Content.ReadFromJsonAsync<ExerciseDetailDto>())!.IsHidden);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync(Url(exercise.Id, "hide"), null)).StatusCode);
        Assert.True((await ExerciseRequests.GetAsync(admin, exercise.Id)).IsHidden);

        var shown = await admin.PostAsync(Url(exercise.Id, "unhide"), null);
        Assert.Equal(HttpStatusCode.OK, shown.StatusCode);
        Assert.False((await shown.Content.ReadFromJsonAsync<ExerciseDetailDto>())!.IsHidden);
        Assert.False((await ExerciseRequests.GetAsync(admin, exercise.Id)).IsHidden);
    }

    // @BR-f03-thu-vien-bai-tap-017: bài chưa được dùng xoá được; cặp (tên, dụng cụ) dùng lại được
    [Fact]
    public async Task BR017_Unused_exercise_is_deleted_and_its_name_can_be_reused()
    {
        var admin = await factory.AdminClientAsync();
        var exercise = await ExerciseRequests.CreateAsync(admin, Name("Chest Dip"), "Ngực", "Bodyweight");

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync(Url(exercise.Id))).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(Url(exercise.Id))).StatusCode);
        var again = await ExerciseRequests.CreateAsync(admin, exercise.Name, "Ngực", "Bodyweight");
        Assert.NotEqual(exercise.Id, again.Id);
    }

    // @BR-f03-thu-vien-bai-tap-017 @ADR-f03-thu-vien-bai-tap-004: bài đang dùng (bản giả trả true) → 409, bài vẫn còn, ẩn vẫn được
    [Fact]
    public async Task BR017_Exercise_in_use_cannot_be_deleted_only_hidden()
    {
        await using var host = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            services.RemoveAll<IExerciseUsage>();
            services.AddScoped<IExerciseUsage, AlwaysUsed>();
        }));
        var admin = await ExerciseRequests.AdminClientAsync(factory, host);
        var exercise = await ExerciseRequests.CreateAsync(admin, Name("Chest Press"));

        var response = await admin.DeleteAsync(Url(exercise.Id));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using (var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
        {
            Assert.Equal("exercise.in_use", json.RootElement.GetProperty("code").GetString());
            Assert.Equal("Bài đang có trong giáo án, chỉ ẩn được.", json.RootElement.GetProperty("title").GetString());
        }
        Assert.Equal(exercise.Name, (await ExerciseRequests.GetAsync(admin, exercise.Id)).Name);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync(Url(exercise.Id, "hide"), null)).StatusCode);
    }

    // @ADR-f03-thu-vien-bai-tap-004: P1 chưa có giáo án — cài đặt mặc định luôn "chưa được dùng"
    [Fact]
    public async Task ADR004_Default_usage_is_never_used()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var usage = scope.ServiceProvider.GetRequiredService<IExerciseUsage>();

        Assert.IsType<NoExerciseUsage>(usage);
        Assert.False(await usage.IsUsedAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Unknown_exercise_cannot_be_hidden_or_deleted()
    {
        var admin = await factory.AdminClientAsync();
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync(Url(id, "hide"), null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync(Url(id, "unhide"), null)).StatusCode);
        var deleted = await admin.DeleteAsync(Url(id));
        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
        Assert.Equal("exercise.not_found", await StudentRequests.ErrorCodeAsync(deleted));
    }

    // NFR-f03-thu-vien-bai-tap-002: bài của coach khác → 404, không bị ẩn/xoá
    [Fact]
    public async Task Coach_cannot_hide_or_delete_another_coachs_exercise()
    {
        var (_, other) = await ExerciseRequests.NewCoachAsync(factory);
        var theirs = await ExerciseRequests.CreateAsync(other, Name("Riêng"));
        var admin = await factory.AdminClientAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync(Url(theirs.Id, "hide"), null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync(Url(theirs.Id, "unhide"), null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync(Url(theirs.Id))).StatusCode);

        var intact = await ExerciseRequests.GetAsync(other, theirs.Id);
        Assert.False(intact.IsHidden);
    }

    // NFR-f03-thu-vien-bai-tap-002
    [Fact]
    public async Task Student_cannot_hide_or_delete()
    {
        var exercise = await ExerciseRequests.CreateAsync(await factory.AdminClientAsync(), Name("Chest Press"));
        var student = await ExerciseRequests.StudentClientAsync(factory);

        Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsync(Url(exercise.Id, "hide"), null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.DeleteAsync(Url(exercise.Id))).StatusCode);
    }

    private sealed class AlwaysUsed : IExerciseUsage
    {
        public Task<bool> IsUsedAsync(Guid exerciseId, CancellationToken ct = default) => Task.FromResult(true);
    }
}
