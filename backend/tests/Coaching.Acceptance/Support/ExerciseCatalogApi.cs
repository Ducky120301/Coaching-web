using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Exercises.Queries;

namespace Coaching.Acceptance.Support;

/// <summary>Gọi /api/admin/exercise-categories bằng phiên coach của StudentApi. Đọc danh mục KHÔNG ghi đè ctx.LastResponse.</summary>
public sealed class ExerciseCatalogApi(AcceptanceContext ctx, StudentApi coach)
{
    public const string Url = "/api/admin/exercise-categories";

    public async Task<CategoriesDto> GetAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", coach.AdminToken);
        using var response = await ctx.Client().SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CategoriesDto>())!;
    }

    public Task<HttpResponseMessage> TryAddAsync(string kind, string? name) =>
        ctx.SendJsonAsync(HttpMethod.Post, Url, new { kind, name }, coach.AdminToken);

    public async Task<Guid> MuscleGroupIdAsync(string name) => (await GetAsync()).MuscleGroups.Single(c => c.Name == name).Id;

    public async Task<Guid> EquipmentIdAsync(string name) => (await GetAsync()).Equipment.Single(c => c.Name == name).Id;
}
