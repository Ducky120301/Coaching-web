using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Exercises;
using Coaching.Application.Exercises.Commands;
using Coaching.Application.Exercises.Queries;

namespace Coaching.Acceptance.Support;

/// <summary>Gọi /api/admin/exercises bằng phiên coach; nhớ Id bài theo tên trong scenario. Đọc bài KHÔNG ghi đè ctx.LastResponse.</summary>
public sealed class ExerciseApi(AcceptanceContext ctx, StudentApi coach, ExerciseCatalogApi catalog)
{
    public const string Url = "/api/admin/exercises";

    public Dictionary<string, Guid> Ids { get; } = new();

    public string? LastCreatedName { get; private set; }

    public Guid IdOf(string name) =>
        Ids.TryGetValue(name, out var id) ? id : throw new InvalidOperationException($"Scenario chưa tạo bài \"{name}\".");

    public async Task<SaveExerciseCommand> CommandAsync(string? name, string? muscleGroup, string? equipment) => new(
        name,
        muscleGroup is null ? null : await catalog.MuscleGroupIdAsync(muscleGroup),
        equipment is null ? null : await catalog.EquipmentIdAsync(equipment),
        null, null, null, null, null, null, null, null, null, null);

    public async Task<HttpResponseMessage> TryCreateAsync(SaveExerciseCommand command)
    {
        var response = await ctx.SendJsonAsync(HttpMethod.Post, Url, command, coach.AdminToken);
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var created = (await response.Content.ReadFromJsonAsync<ExerciseDetailDto>())!;
            Ids[created.Name] = created.Id;
            LastCreatedName = created.Name;
        }
        return response;
    }

    public async Task<HttpResponseMessage> TryCreateAsync(string? name, string? muscleGroup, string? equipment) =>
        await TryCreateAsync(await CommandAsync(name, muscleGroup, equipment));

    public async Task<ExerciseDetailDto> CreateOkAsync(string name, string muscleGroup, string equipment,
        Func<SaveExerciseCommand, SaveExerciseCommand>? change = null)
    {
        var command = await CommandAsync(name, muscleGroup, equipment);
        var response = await TryCreateAsync(change is null ? command : change(command));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await GetAsync(name);
    }

    public async Task<HttpResponseMessage> TryGetAsync(Guid id)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{Url}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", coach.AdminToken);
        return await ctx.Client().SendAsync(request);
    }

    public async Task<ExerciseDetailDto> GetAsync(string name)
    {
        using var response = await TryGetAsync(IdOf(name));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ExerciseDetailDto>())!;
    }

    public static SaveExerciseCommand ToCommand(ExerciseDetailDto d) =>
        new(d.Name, d.MuscleGroupId, d.EquipmentId, d.VideoUrl, d.Sets, d.RepsMin, d.RepsMax, d.RirMin, d.RirMax,
            d.RestSeconds, d.TechniqueNotes, d.CommonMistakes, d.Version);

    /// <summary>PUT bài: đọc bản hiện tại, áp <paramref name="change"/>, gửi kèm version (mặc định: bản vừa đọc).</summary>
    public async Task<HttpResponseMessage> TryUpdateAsync(string name, Func<SaveExerciseCommand, SaveExerciseCommand> change,
        string? version = null)
    {
        var current = await GetAsync(name);
        var command = change(ToCommand(current)) with { Version = version ?? current.Version };
        var response = await ctx.SendJsonAsync(HttpMethod.Put, $"{Url}/{current.Id}", command, coach.AdminToken);
        if (response.IsSuccessStatusCode)
        {
            var saved = (await response.Content.ReadFromJsonAsync<ExerciseDetailDto>())!;
            Ids[saved.Name] = saved.Id;
        }
        return response;
    }

    // @BR-f03-thu-vien-bai-tap-015
    public Task<HttpResponseMessage> TryHideAsync(string name) =>
        ctx.SendJsonAsync(HttpMethod.Post, $"{Url}/{IdOf(name)}/hide", null, coach.AdminToken);

    // @BR-f03-thu-vien-bai-tap-015
    public Task<HttpResponseMessage> TryUnhideAsync(string name) =>
        ctx.SendJsonAsync(HttpMethod.Post, $"{Url}/{IdOf(name)}/unhide", null, coach.AdminToken);

    // @BR-f03-thu-vien-bai-tap-017
    public Task<HttpResponseMessage> TryDeleteAsync(string name) =>
        ctx.SendJsonAsync(HttpMethod.Delete, $"{Url}/{IdOf(name)}", null, coach.AdminToken);

    // @BR-f03-thu-vien-bai-tap-016 @BR-f03-thu-vien-bai-tap-018 @BR-f03-thu-vien-bai-tap-019
    public async Task<IReadOnlyList<ExerciseListItemDto>> ListAsync(string? q = null, string? muscleGroup = null,
        string? equipment = null, bool hidden = false)
    {
        var query = new List<string>();
        if (q is not null)
            query.Add($"q={Uri.EscapeDataString(q)}");
        if (muscleGroup is not null)
            query.Add($"muscleGroupId={await catalog.MuscleGroupIdAsync(muscleGroup)}");
        if (equipment is not null)
            query.Add($"equipmentId={await catalog.EquipmentIdAsync(equipment)}");
        if (hidden)
            query.Add("hidden=true");

        var response = await ctx.GetAsync(query.Count == 0 ? Url : $"{Url}?{string.Join('&', query)}", coach.AdminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<ExerciseListItemDto>>())!;
    }

    /// <summary>Đặt bài ở trạng thái đã ẩn qua API ẩn bài.</summary>
    public async Task HideAsync(string name) =>
        Assert.Equal(HttpStatusCode.OK, (await TryHideAsync(name)).StatusCode);

    public Task AssertErrorAsync(HttpStatusCode status, string code) => coach.AssertErrorAsync(status, code);
}
