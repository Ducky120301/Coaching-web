using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Students.Queries;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class StudentListTests(ApiFactory factory)
{
    private static async Task<List<StudentListItemDto>> ListAsync(HttpClient admin, string? q = null) =>
        (await admin.GetFromJsonAsync<List<StudentListItemDto>>(
            q is null ? StudentRequests.Url : $"{StudentRequests.Url}?q={Uri.EscapeDataString(q)}"))!;

    // @BR-f02-ho-so-hoc-vien-025 (seed loại khỏi living doc): không có API xóa hồ sơ
    [Fact]
    public async Task Delete_student_is_not_allowed()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var response = await admin.DeleteAsync($"{StudentRequests.Url}/{student.Id}");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Contains(await ListAsync(admin), r => r.Id == student.Id);
    }

    // @BR-f02-ho-so-hoc-vien-023: hồ sơ tạo sau đứng trước
    [Fact]
    public async Task Newest_student_comes_first()
    {
        var admin = await factory.AdminClientAsync();
        var older = await StudentRequests.CreateAsync(admin, "Học viên cũ");
        var newer = await StudentRequests.CreateAsync(admin, "Học viên mới");

        var ids = (await ListAsync(admin)).Select(r => r.Id).ToList();

        Assert.True(ids.IndexOf(newer.Id) < ids.IndexOf(older.Id));
    }

    // @BR-f02-ho-so-hoc-vien-024 @ADR-f02-ho-so-hoc-vien-003: "duc" tìm ra "Đức" (collation CI_AI không làm được)
    [Theory]
    [InlineData("duc")]
    [InlineData("ĐỨC")]
    [InlineData("minh d")]
    public async Task Search_ignores_case_and_vietnamese_marks_including_d(string keyword)
    {
        var admin = await factory.AdminClientAsync();
        var duc = await StudentRequests.CreateAsync(admin, "Hoàng Minh Đức");

        Assert.Contains(await ListAsync(admin, keyword), r => r.Id == duc.Id);
    }

    // @ADR-f02-ho-so-hoc-vien-003: hồ sơ tạo trước khi có SearchKey được tính lại khi khởi động
    [Fact]
    public async Task Startup_backfills_empty_search_keys()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin, "Trần Thị Bích Ngọc");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Students] SET [SearchKey] = N'' WHERE [Id] = {student.Id}");
        }
        Assert.DoesNotContain(await ListAsync(admin, "bich ngoc"), r => r.Id == student.Id);

        await using (var restarted = factory.WithWebHostBuilder(_ => { }))
            _ = restarted.Services; // khởi động lại → DatabaseInitializer

        Assert.Contains(await ListAsync(admin, "bich ngoc"), r => r.Id == student.Id);
    }
}
