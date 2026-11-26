using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Exercises.Commands;
using Coaching.Domain.Exercises;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Exercises;

[Collection(ApiCollection.Name)]
public sealed class ExerciseCategoryTests(ApiFactory factory)
{
    private static string Tag() => Guid.NewGuid().ToString("N")[..10];

    private async Task<int> CountAsync(Guid coachId, CategoryKind kind)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ExerciseCategories.CountAsync(c => c.CoachId == coachId && c.Kind == kind);
    }

    // @BR-f03-thu-vien-bai-tap-005 @ADR-f03-thu-vien-bai-tap-003: coach seed có sẵn 11 nhóm cơ + 7 dụng cụ, đúng thứ tự
    [Fact]
    public async Task BR005_Seeded_coach_starts_with_default_catalog_in_order()
    {
        var catalog = await ExerciseRequests.CategoriesAsync(await factory.AdminClientAsync());

        Assert.Equal(DefaultCatalog.MuscleGroups.ToArray(), catalog.MuscleGroups.Take(11).Select(c => c.Name).ToArray());
        Assert.Equal(DefaultCatalog.Equipment.ToArray(), catalog.Equipment.Take(7).Select(c => c.Name).ToArray());
    }

    // @BR-f03-thu-vien-bai-tap-005 @ADR-f03-thu-vien-bai-tap-003: coach chưa có danh mục được tạo ở lần khởi động kế tiếp; khởi động lại không tạo thêm
    [Fact]
    public async Task BR005_Coach_without_catalog_gets_it_on_next_startup_once()
    {
        var (coachId, _) = await ExerciseRequests.NewCoachAsync(factory, withCatalog: false);
        Assert.Equal(0, await CountAsync(coachId, CategoryKind.MuscleGroup));

        await using (var first = factory.WithWebHostBuilder(_ => { }))
            _ = first.Services;
        await using (var second = factory.WithWebHostBuilder(_ => { }))
            _ = second.Services;

        Assert.Equal(11, await CountAsync(coachId, CategoryKind.MuscleGroup));
        Assert.Equal(7, await CountAsync(coachId, CategoryKind.Equipment));
    }

    // @BR-f03-thu-vien-bai-tap-006: mục mới (đã trim) đứng sau các mục đã có, dùng được ngay
    [Fact]
    public async Task BR006_New_equipment_is_trimmed_and_listed_last()
    {
        var admin = await factory.AdminClientAsync();
        var name = $"Resistance Band {Tag()}";

        var response = await admin.PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind = "Equipment", name = $"  {name} " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<CategoryCreatedDto>())!;
        Assert.Equal("Equipment", created.Kind);
        Assert.Equal(name, created.Name);
        var catalog = await ExerciseRequests.CategoriesAsync(admin);
        Assert.Equal(created.Id, catalog.Equipment[^1].Id);
        Assert.DoesNotContain(catalog.MuscleGroups, c => c.Id == created.Id);
    }

    // @BR-f03-thu-vien-bai-tap-006: trùng không phân biệt hoa/thường, dấu, khoảng trắng thừa — với mục CÙNG loại
    [Theory]
    [InlineData("MuscleGroup", "ngực")]
    [InlineData("MuscleGroup", "  NGUC  ")]
    [InlineData("musclegroup", "Vai   sau")]
    [InlineData("Equipment", "smith   machine")]
    [InlineData("Equipment", "CABLE")]
    public async Task BR006_Name_equal_to_an_item_of_the_same_kind_is_duplicate(string kind, string name)
    {
        var response = await (await factory.AdminClientAsync())
            .PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind, name });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("category.duplicate", json.RootElement.GetProperty("code").GetString());
        Assert.Equal("Đã có mục cùng tên.", json.RootElement.GetProperty("title").GetString());
    }

    // @BR-f03-thu-vien-bai-tap-006: cùng tên ở loại khác không phải trùng
    [Fact]
    public async Task BR006_Same_name_in_the_other_kind_is_allowed()
    {
        var admin = await factory.AdminClientAsync();
        var name = $"Landmine {Tag()}";

        Assert.Equal(HttpStatusCode.Created,
            (await admin.PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind = "Equipment", name })).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await admin.PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind = "MuscleGroup", name })).StatusCode);
    }

    // @BR-f03-thu-vien-bai-tap-006: tên danh mục trống → không thêm
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task BR006_Blank_category_name_is_rejected(string? name)
    {
        var response = await (await factory.AdminClientAsync())
            .PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind = "MuscleGroup", name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("category.name_required", await StudentRequests.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Category_name_longer_than_100_is_rejected()
    {
        var response = await (await factory.AdminClientAsync())
            .PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind = "MuscleGroup", name = new string('x', 101) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("category.name_too_long", await StudentRequests.ErrorCodeAsync(response));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Muscle")]
    [InlineData("5")]
    public async Task Unknown_category_kind_is_rejected(string? kind)
    {
        var response = await (await factory.AdminClientAsync())
            .PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind, name = $"Mục {Tag()}" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("category.kind_invalid", await StudentRequests.ErrorCodeAsync(response));
    }

    // @BR-f03-thu-vien-bai-tap-006: hai request thêm cùng tên đua nhau qua bước kiểm trùng → chỉ mục duy nhất chặn, trả 409 thay vì 500
    [Fact]
    public async Task BR006_Concurrent_duplicate_category_returns_409()
    {
        var race = new Race();
        await using var host = ExerciseRequests.WithRace<ExerciseCategory>(factory, race,
            c => c.Name.StartsWith("Race ", StringComparison.Ordinal),
            async (sp, c) =>
            {
                var db = sp.GetRequiredService<AppDbContext>();
                db.ExerciseCategories.Add(ExerciseCategory.Create(c.CoachId, c.Kind, c.Name, c.SortOrder + 1000));
                await db.SaveChangesAsync();
            });
        var admin = await ExerciseRequests.AdminClientAsync(factory, host);

        var response = await admin.PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind = "Equipment", name = $"Race {Tag()}" });

        Assert.True(race.Happened, "Giả lập đua không chạy");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("category.duplicate", await StudentRequests.ErrorCodeAsync(response));
    }

    // Route riêng không được đăng ký đôi với route kế thừa [controller] của AdminControllerBase
    [Fact]
    public async Task Categories_are_served_only_at_the_kebab_case_route()
    {
        var admin = await factory.AdminClientAsync();

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(ExerciseRequests.CategoriesUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/admin/ExerciseCategories")).StatusCode);
    }

    // NFR-f03-thu-vien-bai-tap-002: học viên không truy cập được danh mục
    [Fact]
    public async Task Student_cannot_read_or_add_categories()
    {
        var student = await ExerciseRequests.StudentClientAsync(factory);

        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync(ExerciseRequests.CategoriesUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync(ExerciseRequests.CategoriesUrl,
            new { kind = "Equipment", name = "Band" })).StatusCode);
    }

    // NFR-f03-thu-vien-bai-tap-002 @ADR-f03-thu-vien-bai-tap-003: danh mục thuộc coach — coach khác không thấy mục của nhau
    [Fact]
    public async Task Coach_sees_only_own_categories()
    {
        var (_, other) = await ExerciseRequests.NewCoachAsync(factory);
        var name = $"Riêng {Tag()}";
        Assert.Equal(HttpStatusCode.Created,
            (await other.PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind = "Equipment", name })).StatusCode);

        Assert.Contains((await ExerciseRequests.CategoriesAsync(other)).Equipment, c => c.Name == name);
        Assert.DoesNotContain((await ExerciseRequests.CategoriesAsync(await factory.AdminClientAsync())).Equipment, c => c.Name == name);
    }
}
