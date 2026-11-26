using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Exercises.Queries;

namespace Coaching.Api.IntegrationTests.Exercises;

[Collection(ApiCollection.Name)]
public sealed class ExerciseListTests(ApiFactory factory)
{
    private static string Tag() => Guid.NewGuid().ToString("N");

    private static async Task<List<ExerciseListItemDto>> ListAsync(HttpClient client, string query) =>
        (await client.GetFromJsonAsync<List<ExerciseListItemDto>>($"{ExerciseRequests.Url}?{query}"))!;

    private static string Q(string text) => $"q={Uri.EscapeDataString(text)}";

    // @BR-f03-thu-vien-bai-tap-016: mặc định không có bài đã ẩn; chọn xem bài đã ẩn mới thấy
    [Fact]
    public async Task BR016_Hidden_exercises_are_listed_only_when_asked()
    {
        var admin = await factory.AdminClientAsync();
        var tag = Tag();
        var visible = await ExerciseRequests.CreateAsync(admin, $"Chest Press {tag}");
        var hidden = await ExerciseRequests.CreateAsync(admin, $"Chest Dip {tag}", "Ngực", "Bodyweight");
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"{ExerciseRequests.Url}/{hidden.Id}/hide", null)).StatusCode);

        Assert.Equal(new[] { visible.Name }, (await ListAsync(admin, Q(tag))).Select(r => r.Name).ToArray());
        var onlyHidden = await ListAsync(admin, $"{Q(tag)}&hidden=true");
        Assert.Equal(new[] { hidden.Name }, onlyHidden.Select(r => r.Name).ToArray());
        Assert.True(onlyHidden[0].IsHidden);
    }

    // @BR-f03-thu-vien-bai-tap-018 @ADR-f03-thu-vien-bai-tap-002: khớp một phần, không phân biệt hoa/thường, dấu, kiểu dấu gạch
    [Fact]
    public async Task BR018_Search_matches_part_of_the_name_ignoring_case_marks_and_dashes()
    {
        var admin = await factory.AdminClientAsync();
        var tag = Tag();
        var fly = await ExerciseRequests.CreateAsync(admin, $"45° Incline Chest Fly {tag}", "Ngực", "Cable");
        var ep = await ExerciseRequests.CreateAsync(admin, $"Ép ngực {tag}", "Ngực", "Machine");
        var row = await ExerciseRequests.CreateAsync(admin, $"T–Bar Row {tag}", "Lưng", "Barbell");

        Assert.Equal(fly.Id, Assert.Single(await ListAsync(admin, Q($"INCLINE chest fly {tag}"))).Id);
        Assert.Equal(ep.Id, Assert.Single(await ListAsync(admin, Q($"ep nguc {tag}"))).Id);
        Assert.Equal(row.Id, Assert.Single(await ListAsync(admin, Q($"t-bar row {tag}"))).Id);
        Assert.Equal(3, (await ListAsync(admin, Q(tag))).Count);
        Assert.Empty(await ListAsync(admin, Q($"zzz {tag}")));
    }

    // @BR-f03-thu-vien-bai-tap-019: nhóm cơ, dụng cụ và từ khoá kết hợp — bài phải thoả mọi điều kiện đã chọn
    [Fact]
    public async Task BR019_Filters_and_keyword_combine()
    {
        var admin = await factory.AdminClientAsync();
        var tag = Tag();
        var catalog = await ExerciseRequests.CategoriesAsync(admin);
        Guid Muscle(string name) => catalog.MuscleGroups.Single(c => c.Name == name).Id;
        Guid Equipment(string name) => catalog.Equipment.Single(c => c.Name == name).Id;
        await ExerciseRequests.CreateAsync(admin, $"Dumbbell Fly {tag}", "Ngực", "Dumbbell");
        await ExerciseRequests.CreateAsync(admin, $"Cable Fly {tag}", "Ngực", "Cable");
        await ExerciseRequests.CreateAsync(admin, $"Rear Delt Fly {tag}", "Vai sau", "Dumbbell");
        await ExerciseRequests.CreateAsync(admin, $"Face Pull {tag}", "Vai sau", "Cable");

        var all = await ListAsync(admin, $"{Q($"fly {tag}")}&muscleGroupId={Muscle("Ngực")}&equipmentId={Equipment("Dumbbell")}");
        Assert.Equal(new[] { $"Dumbbell Fly {tag}" }, all.Select(r => r.Name).ToArray());

        var cable = await ListAsync(admin, $"{Q(tag)}&equipmentId={Equipment("Cable")}");
        Assert.Equal(new[] { $"Cable Fly {tag}", $"Face Pull {tag}" }, cable.Select(r => r.Name).ToArray());

        var rear = await ListAsync(admin, $"{Q(tag)}&muscleGroupId={Muscle("Vai sau")}");
        Assert.Equal(new[] { $"Face Pull {tag}", $"Rear Delt Fly {tag}" }, rear.Select(r => r.Name).ToArray());
    }

    // Danh sách sắp theo tên; mỗi dòng có tên nhóm cơ, dụng cụ, có/thiếu video
    [Fact]
    public async Task List_is_sorted_by_name_with_category_names_and_video_flag()
    {
        var admin = await factory.AdminClientAsync();
        var tag = Tag();
        await ExerciseRequests.CreateAsync(admin, $"{tag} C", "Vai", "Cable",
            c => c with { VideoUrl = "https://www.youtube.com/shorts/lMJUXEvcMkQ" });
        await ExerciseRequests.CreateAsync(admin, $"{tag} A");
        await ExerciseRequests.CreateAsync(admin, $"{tag} B");

        var rows = await ListAsync(admin, Q(tag));

        Assert.Equal(new[] { $"{tag} A", $"{tag} B", $"{tag} C" }, rows.Select(r => r.Name).ToArray());
        Assert.Equal(("Vai", "Cable", true, false), (rows[2].MuscleGroup, rows[2].Equipment, rows[2].HasVideo, rows[2].IsHidden));
        Assert.False(rows[0].HasVideo);
    }

    // NFR-f03-thu-vien-bai-tap-002: không thấy bài của coach khác; học viên không gọi được
    [Fact]
    public async Task List_contains_only_the_coachs_own_exercises()
    {
        var tag = Tag();
        var (_, other) = await ExerciseRequests.NewCoachAsync(factory);
        await ExerciseRequests.CreateAsync(other, $"Riêng {tag}");

        Assert.Empty(await ListAsync(await factory.AdminClientAsync(), Q(tag)));
        Assert.Single(await ListAsync(other, Q(tag)));
        var student = await ExerciseRequests.StudentClientAsync(factory);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync(ExerciseRequests.Url)).StatusCode);
    }
}
