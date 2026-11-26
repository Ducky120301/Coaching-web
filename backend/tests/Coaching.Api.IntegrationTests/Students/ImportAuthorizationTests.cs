using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Coaches;
using Coaching.Application.Students;
using Coaching.Application.Students.Import;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class ImportAuthorizationTests(ApiFactory factory)
{
    // @BR-f02-import-excel-012: người chưa đăng nhập import → 401, không hồ sơ nào được tạo
    [Fact]
    public async Task Anonymous_import_is_rejected_with_401_and_creates_nothing()
    {
        var email = ImportRequests.NewEmail();

        var response = await ImportRequests.PostAsync(factory.CreateClient(), ImportRequests.OneRow(email));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(await ImportRequests.StudentExistsAsync(factory, email));
    }

    // @BR-f02-import-excel-012: học viên import → 403, không hồ sơ nào được tạo
    [Fact]
    public async Task Student_import_is_forbidden_with_403_and_creates_nothing()
    {
        var (studentEmail, password) = await factory.CreateStudentAsync();
        var tokens = await factory.LoginAsync(studentEmail, password);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var email = ImportRequests.NewEmail();

        var response = await ImportRequests.PostAsync(client, ImportRequests.OneRow(email));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(await ImportRequests.StudentExistsAsync(factory, email));
    }

    // @BR-f02-import-excel-012 @BR-f02-import-excel-013: coach import → hồ sơ được tạo và thuộc coach đó
    [Fact]
    public async Task Coach_import_creates_profile_owned_by_that_coach()
    {
        var admin = await factory.AdminClientAsync();
        var email = ImportRequests.NewEmail();

        var response = await ImportRequests.PostAsync(admin, ImportRequests.OneRow(email));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<ImportResultDto>())!;
        var row = Assert.Single(result.Rows);
        Assert.Equal("Created", row.Status);
        var detail = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{row.StudentId}"))!;
        var coach = (await admin.GetFromJsonAsync<CoachProfileDto>("/api/admin/profile"))!;
        Assert.Equal(coach.Id, detail.CoachId);
        Assert.Equal(email, detail.Email);
    }

    // @BR-f02-import-excel-014: request không có phần "file" → 400 import.file_invalid
    [Fact]
    public async Task Request_without_file_part_is_rejected_as_invalid_file()
    {
        var admin = await factory.AdminClientAsync();
        using var content = new MultipartFormDataContent { { new StringContent("x"), "note" } };

        var response = await admin.PostAsync(ImportRequests.Url, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("import.file_invalid", await StudentRequests.ErrorCodeAsync(response));
    }
}
