using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Students;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class CreateStudentTests(ApiFactory factory)
{
    // @BR-f02-ho-so-hoc-vien-003: chỉ có họ tên + email vẫn lưu được (scenario @web; kiểm thêm ở API)
    [Fact]
    public async Task Name_and_email_only_is_saved()
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PostAsJsonAsync(StudentRequests.Url, new { fullName = "Lan", email = StudentRequests.NewEmail() });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        Assert.Equal("Lan", dto.FullName);
        Assert.Equal($"/api/admin/students/{dto.Id}", response.Headers.Location!.AbsolutePath, ignoreCase: true);

        var get = await admin.GetAsync($"{StudentRequests.Url}/{dto.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    // @BR-f02-ho-so-hoc-vien-003 (seed loại khỏi living doc): họ tên 101 ký tự
    [Fact]
    public async Task Full_name_longer_than_100_is_rejected()
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PostAsJsonAsync(StudentRequests.Url,
            new { fullName = new string('a', 101), email = StudentRequests.NewEmail() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("student.full_name_too_long", await StudentRequests.ErrorCodeAsync(response));
    }

    // @BR-f02-ho-so-hoc-vien-005 @BR-f02-ho-so-hoc-vien-006 (seed loại khỏi living doc): trạng thái gửi kèm bị bỏ qua
    [Fact]
    public async Task Statuses_sent_in_body_are_ignored()
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PostAsJsonAsync(StudentRequests.Url, new
        {
            fullName = "Lan",
            email = StudentRequests.NewEmail(),
            accountStatus = "Activated",
            coachingStatus = "Ended",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        Assert.Equal("PendingActivation", dto.AccountStatus);
        Assert.Equal("Active", dto.CoachingStatus);
    }

    // @BR-f02-ho-so-hoc-vien-012 tiền đề: hồ sơ không tồn tại → 404
    [Fact]
    public async Task Unknown_student_returns_404()
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.GetAsync($"{StudentRequests.Url}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("student.not_found", await StudentRequests.ErrorCodeAsync(response));
    }

    // @ADR-f02-ho-so-hoc-vien-001: lưu hồ sơ lỗi sau khi đã tạo tài khoản → transaction rollback, không còn tài khoản mồ côi
    [Fact]
    public async Task Account_is_rolled_back_when_saving_student_fails()
    {
        var admin = await factory.AdminClientAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER [TR_Students_Fail] ON [Students] INSTEAD OF INSERT AS THROW 50000, 'boom', 1;");
        try
        {
            var email = StudentRequests.NewEmail();

            var response = await admin.PostAsJsonAsync(StudentRequests.Url, new { fullName = "Lỗi giữa chừng", email });

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.False(await db.Users.AnyAsync(u => u.Email == email));
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER [TR_Students_Fail];");
        }
    }
}
