using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class ModelBindingProblemTests(ApiFactory factory)
{
    // ProblemDetails luôn có `code`: lỗi model-binding của [ApiController] → 400 request.invalid, tiêu đề tiếng Việt, giữ `errors`
    [Fact]
    public async Task Malformed_body_returns_problem_with_code_and_vietnamese_title()
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PostAsJsonAsync(StudentRequests.Url,
            new { fullName = "Lan", email = StudentRequests.NewEmail(), registration = new { weightKg = "abc" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("request.invalid", json.RootElement.GetProperty("code").GetString());
        Assert.Equal("Dữ liệu gửi lên không hợp lệ.", json.RootElement.GetProperty("title").GetString());
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
        Assert.True(json.RootElement.TryGetProperty("errors", out var errors));
        Assert.NotEmpty(errors.EnumerateObject());
    }
}
