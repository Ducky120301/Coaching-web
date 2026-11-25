using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Coaching.Domain.Students;

namespace Coaching.Acceptance.Support;

/// <summary>Tên mục trong living doc → mã mục JSON của `registration`.</summary>
public static class RegistrationLabels
{
    public static readonly IReadOnlyDictionary<string, string> Codes = new Dictionary<string, string>
    {
        ["Gói đã mua"] = "pack",
        ["Giá"] = "price",
        ["Tuổi"] = "age",
        ["Chiều cao"] = "heightCm",
        ["Cân nặng"] = "weightKg",
        ["Mỡ cơ thể"] = "bodyFatPercent",
        ["Mục tiêu tập luyện"] = "trainingGoal",
        ["Số buổi tập mỗi tuần"] = "sessionsPerWeek",
        ["Ảnh mặt trước"] = "photoFrontUrl",
        ["Ảnh mặt sau"] = "photoBackUrl",
    };

    private static readonly HashSet<string> NumericCodes = ["price", "age", "heightCm", "weightKg", "bodyFatPercent"];

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
