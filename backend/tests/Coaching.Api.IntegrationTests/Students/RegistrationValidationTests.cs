using System.Net;
using System.Net.Http.Json;
using System.Text;
using Coaching.Application.Students;
using Coaching.TestSupport;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class RegistrationValidationTests(ApiFactory factory)
{
    // @BR-f02-ho-so-hoc-vien-008 (seed loại khỏi living doc): cân nặng "abc"
    [Fact]
    public async Task Non_numeric_weight_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var body = $$$"""{"fullName":"Lan","email":"{{{StudentRequests.NewEmail()}}}","registration":{"weightKg":"abc"}}""";

        var response = await admin.PostAsync(StudentRequests.Url, new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // @BR-f02-ho-so-hoc-vien-009 (seed loại khỏi living doc): giá trị ngoài danh sách
    [Theory]
    [InlineData("sessionsPerWeek", "8 buổi")]
    [InlineData("stressLevel", "Stress-free")]
    public async Task Option_outside_list_is_rejected(string code, string value)
    {
        var admin = await factory.AdminClientAsync();
        var registration = new Dictionary<string, object?> { [code] = value };

        var response = await admin.PostAsJsonAsync(StudentRequests.Url,
            new { fullName = "Lan", email = StudentRequests.NewEmail(), registration });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal($"student.{code}.invalid_option", await StudentRequests.ErrorCodeAsync(response));
    }

    // @ADR-f02-ho-so-hoc-vien-002 @BR-f02-ho-so-hoc-vien-004: form đủ mục lưu vào cột JSON và đọc lại nguyên vẹn
    [Fact]
    public async Task Complete_registration_round_trips_through_json_column()
    {
        var admin = await factory.AdminClientAsync();
        var input = RegistrationSamples.Complete();

        var created = await admin.PostAsJsonAsync(StudentRequests.Url,
            new { fullName = "Lan", email = StudentRequests.NewEmail(), registration = input });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<StudentDetailDto>())!.Id;

        var detail = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{id}"))!;
        Assert.Equal(input, detail.Registration);
        Assert.Empty(detail.MissingRequired);
    }
}
