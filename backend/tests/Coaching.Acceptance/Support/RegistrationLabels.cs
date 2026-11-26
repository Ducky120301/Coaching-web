using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Coaching.Domain.Students;

namespace Coaching.Acceptance.Support;

/// <summary>Tên mục trong living doc → mã mục JSON của `registration` (@ADR-f02-import-excel-001: mục theo Google Form).</summary>
public static class RegistrationLabels
{
    public static readonly IReadOnlyDictionary<string, string> Codes = new Dictionary<string, string>
    {
        ["Gói đã mua"] = "pack",
        ["Giá"] = "price",
        ["Tuổi"] = "age",
        ["Chiều cao"] = "heightCm",
        ["Cân nặng"] = "weightKg",
        ["Mục tiêu tập luyện"] = "trainingGoal",
        ["Số buổi tập mỗi tuần"] = "sessionsPerWeek",
        ["Số năm tập luyện"] = "trainingYears", // @BR-f02-import-excel-003
        ["Mỡ cơ thể và cách đo"] = "bodyFatAndMethod", // @BR-f02-import-excel-004
        ["Cân nặng và chiều cao trước khi tập tạ"] = "weightHeightBeforeLifting", // @BR-f02-import-excel-005
        ["Link ảnh toàn thân"] = "bodyPhotoUrl", // @BR-f02-import-excel-006
    };

    private static readonly HashSet<string> NumericCodes = ["price", "age", "heightCm", "weightKg"];

    /// <summary>Đặt mục <paramref name="code"/> = <paramref name="text"/> (null = để trống). Số nhận dấu phẩy thập phân.</summary>
    public static RegistrationInput With(RegistrationInput input, string code, string? text)
    {
        var json = JsonSerializer.SerializeToNode(input, JsonSerializerOptions.Web)!.AsObject();
        json[code] = text is null ? null
            : NumericCodes.Contains(code) ? JsonValue.Create(ParseNumber(text))
            : JsonValue.Create(text);
        return json.Deserialize<RegistrationInput>(JsonSerializerOptions.Web)!;
    }

    public static void AssertValue(RegistrationInput registration, string code, string expected)
    {
        var node = JsonSerializer.SerializeToNode(registration, JsonSerializerOptions.Web)![code];
        Assert.NotNull(node);
        if (NumericCodes.Contains(code))
            Assert.Equal(ParseNumber(expected), node!.GetValue<decimal>());
        else
            Assert.Equal(expected, node!.GetValue<string>());
    }

    public static decimal ParseNumber(string text) =>
        decimal.Parse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture);
}
