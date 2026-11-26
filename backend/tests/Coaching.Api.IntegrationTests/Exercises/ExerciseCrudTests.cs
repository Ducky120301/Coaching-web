using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Exercises;
using Coaching.Domain.Exercises;
using Coaching.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Exercises;

[Collection(ApiCollection.Name)]
public sealed class ExerciseCrudTests(ApiFactory factory)
{
    private static string Name(string label) => ExerciseRequests.UniqueName(label);

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, await StudentRequests.ErrorCodeAsync(response));
    }

    // @ADR-f03-thu-vien-bai-tap-001 @BR-f03-thu-vien-bai-tap-002 @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004
    [Fact]
    public async Task ADR001_Created_exercise_round_trips_with_category_names_and_version()
    {
        var admin = await factory.AdminClientAsync();
        var name = Name("Chest Press");

        var response = await admin.PostAsJsonAsync(ExerciseRequests.Url,
            await ExerciseRequests.CommandAsync(admin, $"  {name} ", "Ngực", "Machine"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ExerciseDetailDto>())!;
        Assert.Equal($"{ExerciseRequests.Url}/{created.Id}", response.Headers.Location!.AbsolutePath, ignoreCase: true);
        Assert.Equal(name, created.Name);
        Assert.Equal("Ngực", created.MuscleGroup);
        Assert.Equal("Machine", created.Equipment);
        Assert.False(created.IsHidden);
        Assert.False(created.HasVideo);
        Assert.NotEmpty(created.Version);
        Assert.Equal(created, await ExerciseRequests.GetAsync(admin, created.Id));
    }

    // @BR-f03-thu-vien-bai-tap-002: 100 ký tự lưu được; 101 ký tự → không lưu (seed ❌)
    [Fact]
    public async Task BR002_Name_of_101_characters_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var name100 = $"{Guid.NewGuid():N}{new string('x', 68)}";

        Assert.Equal(100, (await ExerciseRequests.CreateAsync(admin, name100)).Name.Length);
        var response = await admin.PostAsJsonAsync(ExerciseRequests.Url,
            await ExerciseRequests.CommandAsync(admin, $"{Guid.NewGuid():N}{new string('x', 69)}"));
        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "exercise.name_too_long");
    }

    // @BR-f03-thu-vien-bai-tap-002: tên trống hoặc chỉ khoảng trắng → báo thiếu tên
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task BR002_Blank_name_is_rejected(string? name)
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PostAsJsonAsync(ExerciseRequests.Url, await ExerciseRequests.CommandAsync(admin, name));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "exercise.name_required");
    }

    // @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004: danh mục trống (seed ❌)
    [Fact]
    public async Task BR003_BR004_Missing_muscle_group_or_equipment_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var command = await ExerciseRequests.CommandAsync(admin, Name("Chest Press"));

        await AssertProblemAsync(await admin.PostAsJsonAsync(ExerciseRequests.Url, command with { MuscleGroupId = null }),
            HttpStatusCode.BadRequest, "exercise.muscle_group_required");
        await AssertProblemAsync(await admin.PostAsJsonAsync(ExerciseRequests.Url, command with { EquipmentId = null }),
            HttpStatusCode.BadRequest, "exercise.equipment_required");
        await AssertProblemAsync(await admin.PostAsJsonAsync(ExerciseRequests.Url, command with { MuscleGroupId = Guid.Empty }),
            HttpStatusCode.BadRequest, "exercise.muscle_group_required");
    }

    // @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004: nhóm cơ/dụng cụ không thuộc danh mục của coach (seed ❌) — gồm sai loại và mục của coach khác
    [Fact]
    public async Task BR003_BR004_Category_outside_the_coachs_catalog_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var (_, other) = await ExerciseRequests.NewCoachAsync(factory);
        var command = await ExerciseRequests.CommandAsync(admin, Name("Chest Press"));
        var theirs = await ExerciseRequests.CategoriesAsync(other);

        foreach (var wrong in new[]
                 {
                     command with { MuscleGroupId = Guid.NewGuid() },
                     command with { EquipmentId = Guid.NewGuid() },
                     command with { MuscleGroupId = command.EquipmentId },
                     command with { EquipmentId = command.MuscleGroupId },
                     command with { MuscleGroupId = theirs.MuscleGroups[0].Id },
                     command with { EquipmentId = theirs.Equipment[0].Id },
                 })
        {
            await AssertProblemAsync(await admin.PostAsJsonAsync(ExerciseRequests.Url, wrong),
                HttpStatusCode.BadRequest, "exercise.category_not_found");
        }
    }

    // @BR-f03-thu-vien-bai-tap-001: cùng tên chuẩn hoá + cùng dụng cụ → 409 (không gợi ý bỏ ẩn); khác dụng cụ → tạo được
    [Fact]
    public async Task BR001_Same_name_written_differently_with_same_equipment_is_duplicate()
    {
        var admin = await factory.AdminClientAsync();
        var tag = Guid.NewGuid().ToString("N");
        await ExerciseRequests.CreateAsync(admin, $"Ép ngực {tag}", "Ngực", "Cable");

        var duplicate = await admin.PostAsJsonAsync(ExerciseRequests.Url,
            await ExerciseRequests.CommandAsync(admin, $"  ep   NGUC {tag} ", "Ngực", "Cable"));

        await AssertProblemAsync(duplicate, HttpStatusCode.Conflict, "exercise.duplicate");
        using var json = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.TryGetProperty("hiddenExerciseId", out _));
        Assert.Equal("Dumbbell", (await ExerciseRequests.CreateAsync(admin, $"Ép ngực {tag}", "Ngực", "Dumbbell")).Equipment);
    }

    // @BR-f03-thu-vien-bai-tap-001: trùng một bài đang ẩn → 409 kèm Id bài đó để gợi ý bỏ ẩn
    [Fact]
    public async Task BR001_Duplicate_of_a_hidden_exercise_points_to_it()
    {
        var admin = await factory.AdminClientAsync();
        var hidden = await ExerciseRequests.CreateAsync(admin, Name("Face Pull"), "Vai sau", "Cable");
        await ExerciseRequests.HideInDatabaseAsync(factory, hidden.Id);

        var response = await admin.PostAsJsonAsync(ExerciseRequests.Url,
            await ExerciseRequests.CommandAsync(admin, hidden.Name, "Vai sau", "Cable"));

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "exercise.duplicate");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(hidden.Id, json.RootElement.GetProperty("hiddenExerciseId").GetGuid());
    }

    // @BR-f03-thu-vien-bai-tap-001: sửa A thành (tên, dụng cụ) của B → 409, A giữ nguyên; sửa hoa/thường tên của chính nó → lưu được
    [Fact]
    public async Task BR001_Renaming_onto_another_exercise_is_rejected_but_own_case_change_is_kept()
    {
        var admin = await factory.AdminClientAsync();
        var legPress = await ExerciseRequests.CreateAsync(admin, Name("leg press"), "Đùi trước", "Machine");
        var hackSquat = await ExerciseRequests.CreateAsync(admin, Name("Hack Squat"), "Đùi trước", "Machine");

        var clash = await ExerciseRequests.UpdateAsync(admin, hackSquat, c => c with { Name = legPress.Name.ToUpperInvariant() });
        await AssertProblemAsync(clash, HttpStatusCode.Conflict, "exercise.duplicate");
        Assert.Equal(hackSquat.Name, (await ExerciseRequests.GetAsync(admin, hackSquat.Id)).Name);

        var own = await ExerciseRequests.UpdateAsync(admin, legPress, c => c with { Name = legPress.Name.Replace("leg press", "Leg Press") });
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.StartsWith("Leg Press ", (await own.Content.ReadFromJsonAsync<ExerciseDetailDto>())!.Name);
    }

    // @BR-f03-thu-vien-bai-tap-001 @ADR-f03-thu-vien-bai-tap-001: hai request cùng (tên, dụng cụ) đua nhau qua bước kiểm trùng → chỉ mục duy nhất chặn, 409 thay vì 500
    [Fact]
    public async Task BR001_Create_losing_the_unique_race_returns_409()
    {
        var race = new Race();
        await using var host = ExerciseRequests.WithRace<Exercise>(factory, race,
            e => e.Name.StartsWith("Race ", StringComparison.Ordinal),
            async (sp, e) =>
            {
                var db = sp.GetRequiredService<AppDbContext>();
                db.Exercises.Add(Exercise.Create(e.CoachId, e.Name, e.MuscleGroupId, e.EquipmentId));
                await db.SaveChangesAsync();
            });
        var admin = await ExerciseRequests.AdminClientAsync(factory, host);

        var response = await admin.PostAsJsonAsync(ExerciseRequests.Url,
            await ExerciseRequests.CommandAsync(admin, Name("Race"), "Lưng", "Cable"));

        Assert.True(race.Happened, "Giả lập đua không chạy");
        await AssertProblemAsync(response, HttpStatusCode.Conflict, "exercise.duplicate");
    }

    // @BR-f03-thu-vien-bai-tap-001: 2 request song song → 1 tạo, 1 bị báo trùng
    [Fact]
    public async Task BR001_Two_parallel_creates_produce_one_exercise()
    {
        var admin = await factory.AdminClientAsync();
        var command = await ExerciseRequests.CommandAsync(admin, Name("Parallel Row"), "Lưng", "Cable");

        var responses = await Task.WhenAll(
            admin.PostAsJsonAsync(ExerciseRequests.Url, command),
            admin.PostAsJsonAsync(ExerciseRequests.Url, command));

        Assert.Equal(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict },
            responses.Select(r => r.StatusCode).OrderBy(s => (int)s).ToArray());
    }

    // @BR-f03-thu-vien-bai-tap-020: lưu dựa trên phiên bản cũ → 409, không ghi đè lần lưu trước
    [Fact]
    public async Task BR020_Stale_version_is_rejected_and_keeps_the_previous_save()
    {
        var admin = await factory.AdminClientAsync();
        var original = await ExerciseRequests.CreateAsync(admin, Name("Chest Press"));
        var catalog = await ExerciseRequests.CategoriesAsync(admin);
        var vai = catalog.MuscleGroups.Single(c => c.Name == "Vai").Id;
        var lung = catalog.MuscleGroups.Single(c => c.Name == "Lưng").Id;

        var first = await ExerciseRequests.UpdateAsync(admin, original, c => c with { MuscleGroupId = vai });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.NotEqual(original.Version, (await first.Content.ReadFromJsonAsync<ExerciseDetailDto>())!.Version);

        var stale = await ExerciseRequests.UpdateAsync(admin, original, c => c with { MuscleGroupId = lung });

        await AssertProblemAsync(stale, HttpStatusCode.Conflict, "exercise.version_conflict");
        Assert.Equal("Vai", (await ExerciseRequests.GetAsync(admin, original.Id)).MuscleGroup);
    }

    [Fact]
    public async Task BR020_Update_without_version_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var exercise = await ExerciseRequests.CreateAsync(admin, Name("Chest Press"));

        var response = await ExerciseRequests.UpdateAsync(admin, exercise, c => c with { Version = null });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "exercise.version_required");
    }

    [Fact]
    public async Task Unknown_exercise_is_404()
    {
        var response = await (await factory.AdminClientAsync()).GetAsync($"{ExerciseRequests.Url}/{Guid.NewGuid()}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "exercise.not_found");
    }

    // NFR-f03-thu-vien-bai-tap-002: học viên không gọi được API thư viện
    [Fact]
    public async Task Student_cannot_use_the_exercise_api()
    {
        var admin = await factory.AdminClientAsync();
        var exercise = await ExerciseRequests.CreateAsync(admin, Name("Chest Press"));
        var student = await ExerciseRequests.StudentClientAsync(factory);

        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync($"{ExerciseRequests.Url}/{exercise.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync(ExerciseRequests.Url, ExerciseRequests.ToCommand(exercise))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.PutAsJsonAsync($"{ExerciseRequests.Url}/{exercise.Id}", ExerciseRequests.ToCommand(exercise))).StatusCode);
    }

    // NFR-f03-thu-vien-bai-tap-002: bài của coach khác → 404, không bị đổi
    [Fact]
    public async Task Coach_cannot_read_or_change_another_coachs_exercise()
    {
        var (_, other) = await ExerciseRequests.NewCoachAsync(factory);
        var theirs = await ExerciseRequests.CreateAsync(other, Name("Riêng"));
        var admin = await factory.AdminClientAsync();
        var adminCommand = await ExerciseRequests.CommandAsync(admin, "Bị sửa");

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{ExerciseRequests.Url}/{theirs.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"{ExerciseRequests.Url}/{theirs.Id}",
            adminCommand with { Version = theirs.Version })).StatusCode);
        Assert.Equal(theirs.Name, (await ExerciseRequests.GetAsync(other, theirs.Id)).Name);
    }
}
