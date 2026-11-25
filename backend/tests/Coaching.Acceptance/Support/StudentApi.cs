using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Application.Students;
using Coaching.Application.Students.Queries;
using Coaching.Domain.Students;

namespace Coaching.Acceptance.Support;

/// <summary>Gọi /api/admin/students bằng phiên coach; nhớ Id hồ sơ theo họ tên trong scenario.</summary>
public sealed class StudentApi(AcceptanceContext ctx)
{
    public static readonly IReadOnlyDictionary<string, string> CoachingStatuses = new Dictionary<string, string>
    {
        ["Đang coaching"] = "Active",
        ["Tạm dừng"] = "Paused",
        ["Đã kết thúc"] = "Ended",
    };

    public static readonly IReadOnlyDictionary<string, string> AccountStatuses = new Dictionary<string, string>
    {
        ["Chờ kích hoạt"] = "PendingActivation",
        ["Đã kích hoạt"] = "Activated",
    };

    public string? AdminToken { get; private set; }

    public Dictionary<string, Guid> Ids { get; } = new();

    public StudentDetailDto? LastDetail { get; private set; }

    public async Task LoginCoachAsync() =>
        AdminToken = (await ctx.LoginOkAsync(AcceptanceContext.AdminEmail, AcceptanceContext.AdminPassword, "admin")).AccessToken;

    /// <summary>Tua đồng hồ tới <paramref name="instant"/> (chỉ tiến) rồi đăng nhập lại để phiên coach còn hạn.</summary>
    public async Task MoveClockToAsync(DateTimeOffset instant)
    {
        if (instant < ctx.Clock.GetUtcNow())
            throw new InvalidOperationException($"Không lùi được đồng hồ từ {ctx.Clock.GetUtcNow():O} về {instant:O}.");
        ctx.Clock.SetUtcNow(instant);
        if (AdminToken is not null)
            await LoginCoachAsync();
    }

    public async Task<HttpResponseMessage> TryCreateAsync(string? fullName, string? email, RegistrationInput? registration = null)
    {
        var response = await ctx.SendJsonAsync(HttpMethod.Post, "/api/admin/students",
            new { fullName, email, registration }, AdminToken);
        await RememberCreatedAsync(response, fullName);
        return response;
    }

    public async Task<StudentDetailDto> CreateOkAsync(string fullName, string? email = null, RegistrationInput? registration = null)
    {
        var response = await TryCreateAsync(fullName, email ?? NewEmail(), registration);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return LastDetail!;
    }

    public async Task<Guid> EnsureAsync(string fullName) =>
        Ids.TryGetValue(fullName, out var id) ? id : (await CreateOkAsync(fullName)).Id;

    public async Task<StudentDetailDto> GetAsync(string fullName)
    {
        var response = await ctx.GetAsync($"/api/admin/students/{Ids[fullName]}", AdminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        LastDetail = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        return LastDetail;
    }

    /// <summary>PUT hồ sơ: lấy bản hiện tại, áp <paramref name="change"/> lên form, gửi kèm version (mặc định: bản vừa đọc).</summary>
    public async Task<HttpResponseMessage> TryUpdateAsync(string fullName, Func<RegistrationInput, RegistrationInput> change,
        string? version = null)
    {
        var current = await GetAsync(fullName);
        var body = new { fullName = current.FullName, registration = change(current.Registration), version = version ?? current.Version };
        var response = await ctx.SendJsonAsync(HttpMethod.Put, $"/api/admin/students/{Ids[fullName]}", body, AdminToken);
        if (response.IsSuccessStatusCode)
            LastDetail = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        return response;
    }

    public async Task<StudentDetailDto> UpdateOkAsync(string fullName, Func<RegistrationInput, RegistrationInput> change)
    {
        var response = await TryUpdateAsync(fullName, change);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return LastDetail!;
    }

    public async Task<StudentDetailDto> UpdateNoteOkAsync(string fullName, string? note)
    {
        var current = await GetAsync(fullName);
        var response = await ctx.SendJsonAsync(HttpMethod.Put, $"/api/admin/students/{Ids[fullName]}/internal-note",
            new { note, version = current.Version }, AdminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        LastDetail = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        return LastDetail;
    }

    public async Task<HttpResponseMessage> TryChangeStatusAsync(string fullName, string status, string reason, string? version = null)
    {
        var current = await GetAsync(fullName);
        var response = await ctx.SendJsonAsync(HttpMethod.Post, $"/api/admin/students/{Ids[fullName]}/coaching-status",
            new { status, reason, version = version ?? current.Version }, AdminToken);
        if (response.IsSuccessStatusCode)
            LastDetail = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        return response;
    }

    public async Task<IReadOnlyList<StudentListItemDto>> ListAsync(string? q)
    {
        var url = string.IsNullOrEmpty(q) ? "/api/admin/students" : $"/api/admin/students?q={Uri.EscapeDataString(q)}";
        var response = await ctx.GetAsync(url, AdminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<StudentListItemDto>>())!;
    }

    public async Task AssertErrorAsync(HttpStatusCode status, string code)
    {
        Assert.Equal(status, ctx.LastResponse!.StatusCode);
        Assert.Equal(code, await ErrorCodeAsync(ctx.LastResponse));
    }

    /// <summary>Email ngẫu nhiên chỉ gồm chữ số hex → không khớp từ khóa tìm kiếm trong living doc.</summary>
    public static string NewEmail() => $"s{Guid.NewGuid():N}@acceptance.local";

    public static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private async Task RememberCreatedAsync(HttpResponseMessage response, string? fullName)
    {
        if (response.StatusCode != HttpStatusCode.Created)
            return;
        LastDetail = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        if (!string.IsNullOrWhiteSpace(fullName))
            Ids[fullName.Trim()] = LastDetail.Id;
    }
}
