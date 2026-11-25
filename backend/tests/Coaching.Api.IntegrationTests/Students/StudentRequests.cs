using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Application.Students;

namespace Coaching.Api.IntegrationTests.Students;

internal static class StudentRequests
{
    public const string Url = "/api/admin/students";

    public static string NewEmail() => $"s{Guid.NewGuid():N}@test.local";

    public static async Task<StudentDetailDto> CreateAsync(HttpClient admin, string fullName = "Học viên Test")
    {
        var response = await admin.PostAsJsonAsync(Url, new { fullName, email = NewEmail() });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
    }

    public static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
