using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Students;
using Coaching.Domain.Students;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class UpdateStudentTests(ApiFactory factory)
{
    // @BR-f02-ho-so-hoc-vien-012 (seed loại khỏi living doc): sửa hồ sơ không tồn tại
    [Fact]
    public async Task Updating_unknown_student_returns_404()
    {
        var admin = await factory.AdminClientAsync();
        var existing = await StudentRequests.CreateAsync(admin);

        var response = await admin.PutAsJsonAsync($"{StudentRequests.Url}/{Guid.NewGuid()}",
            new { fullName = "Lan", registration = new RegistrationInput(), version = existing.Version });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("student.not_found", await StudentRequests.ErrorCodeAsync(response));
    }

    // @BR-f02-ho-so-hoc-vien-012: email gửi kèm trong lệnh sửa bị bỏ qua
    [Fact]
    public async Task Email_cannot_be_changed_by_update()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var response = await admin.PutAsJsonAsync($"{StudentRequests.Url}/{student.Id}", new
        {
            fullName = "Lan",
            email = "doi-email@test.local",
            registration = new RegistrationInput { WeightKg = 70m },
            version = student.Version,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        Assert.Equal(student.Email, updated.Email);
        Assert.Equal(70m, updated.Registration.WeightKg);
    }

    // @ADR-f02-ho-so-hoc-vien-004: thiếu version → 400; mỗi lần lưu ra version mới
    [Fact]
    public async Task Version_is_required_and_changes_after_each_save()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var missing = await admin.PutAsJsonAsync($"{StudentRequests.Url}/{student.Id}",
            new { fullName = "Lan", registration = new RegistrationInput() });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal("student.version_required", await StudentRequests.ErrorCodeAsync(missing));

        var saved = await admin.PutAsJsonAsync($"{StudentRequests.Url}/{student.Id}",
            new { fullName = "Lan", registration = new RegistrationInput(), version = student.Version });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.NotEqual(student.Version, (await saved.Content.ReadFromJsonAsync<StudentDetailDto>())!.Version);
    }

    // @BR-f02-ho-so-hoc-vien-015: lưu dựa trên phiên bản cũ → 409, không ghi đè
    [Fact]
    public async Task Stale_version_is_rejected_with_409()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);
        var url = $"{StudentRequests.Url}/{student.Id}";

        var first = await admin.PutAsJsonAsync(url,
            new { fullName = "Lan", registration = new RegistrationInput { TrainingGoal = "Một" }, version = student.Version });
        var second = await admin.PutAsJsonAsync(url,
            new { fullName = "Lan", registration = new RegistrationInput { TrainingGoal = "Hai" }, version = student.Version });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("student.version_conflict", await StudentRequests.ErrorCodeAsync(second));
        Assert.Equal("Một", (await admin.GetFromJsonAsync<StudentDetailDto>(url))!.Registration.TrainingGoal);
    }
}
