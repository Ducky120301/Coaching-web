using System.Text;

namespace Coaching.Domain.Students;

/// <summary>
/// @BR-f02-ho-so-hoc-vien-009 @ADR-f02-ho-so-hoc-vien-006: danh sách giá trị cho trước, đúng theo form prototype.
/// FE có bản sao chỉ để hiển thị. "Số năm tập luyện" và cách đo mỡ không còn danh sách (@ADR-f02-import-excel-001).
/// </summary>
public static class RegistrationOptions
{
    public static readonly IReadOnlyList<string> Packs = ["Pack 1", "Pack 2", "Pack 3", "Khác"];

    public static readonly IReadOnlyList<string> FoodBudgetReadiness = ["Sẵn sàng", "Đang cân nhắc", "Chưa sẵn sàng"];

    public static readonly IReadOnlyList<string> ActivityLevels =
        ["Ít vận động (ngồi nhiều)", "Nhẹ", "Vừa", "Nhiều", "Rất nhiều (lao động chân tay)"];

    public static readonly IReadOnlyList<string> StressLevels = ["Thấp", "Vừa", "Cao", "Rất cao"];

    public static readonly IReadOnlyList<string> SleepDurations =
        ["Dưới 5 tiếng", "5–6 tiếng", "6–7 tiếng", "7–8 tiếng", "Trên 8 tiếng"];

    public static readonly IReadOnlyList<string> SessionsPerWeek = ["2 buổi", "3 buổi", "4 buổi", "5 buổi", "6 buổi", "7 buổi"];

    public static readonly IReadOnlyList<string> ProgramStyles =
        ["Push / Pull / Legs", "Upper / Lower", "Full body", "Mỗi buổi một nhóm cơ", "Để coach chọn", "Khác"];

    // @ADR-f02-import-excel-006: mã mục có danh sách → danh sách (khai báo SAU các danh sách — khởi tạo static theo thứ tự)
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> OptionsByField =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["pack"] = Packs,
            ["foodBudgetReadiness"] = FoodBudgetReadiness,
            ["activityLevel"] = ActivityLevels,
            ["stressLevel"] = StressLevels,
            ["sleepDuration"] = SleepDurations,
            ["sessionsPerWeek"] = SessionsPerWeek,
            ["programStyle"] = ProgramStyles,
        };

    /// <summary>
    /// @BR-f02-import-excel-030 @ADR-f02-import-excel-006: cách viết đã biết (tech-design §3 "Bảng cách viết đã biết",
    /// PO duyệt) — mã mục → (cách viết trong file → giá trị chuẩn). So khớp sau chuẩn hoá như <see cref="Match"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> KnownSpellings { get; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["programStyle"] = new Dictionary<string, string>
            {
                ["PUSH PULL LEG"] = "Push / Pull / Legs",
                ["PUSH PULL LEGS"] = "Push / Pull / Legs",
                ["PPL"] = "Push / Pull / Legs",
                ["UPPER LOWER"] = "Upper / Lower",
                ["FULLBODY"] = "Full body",
            },
            ["sleepDuration"] = new Dictionary<string, string>
            {
                ["< 5 tiếng"] = "Dưới 5 tiếng",
                ["> 8 tiếng"] = "Trên 8 tiếng",
            },
        };

    /// <summary>
    /// @BR-f02-import-excel-030 @ADR-f02-import-excel-006: đưa giá trị trong file import về giá trị chuẩn của mục
    /// <paramref name="fieldCode"/> — so sau khi chuẩn hoá (chữ thường, bỏ khoảng trắng, đồng nhất - – —, bỏ "/"),
    /// rồi so với bảng cách viết đã biết. Null = không đưa về được (ô không hợp lệ), giá trị trống hoặc mục không có danh sách.
    /// Chỉ import dùng; form nhập tay vẫn kiểm đúng nguyên văn.
    /// </summary>
    public static string? Match(string fieldCode, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !OptionsByField.TryGetValue(fieldCode, out var options))
            return null;

        var key = Key(raw);
        foreach (var option in options)
        {
            if (Key(option) == key)
                return option;
        }

        if (KnownSpellings.TryGetValue(fieldCode, out var spellings))
        {
            foreach (var (spelling, canonical) in spellings)
            {
                if (Key(spelling) == key)
                    return canonical;
            }
        }
        return null;
    }

    private static string Key(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormC).ToLowerInvariant())
        {
            if (char.IsWhiteSpace(c) || c == '/')
                continue;
            builder.Append(c is '–' or '—' ? '-' : c);
        }
        return builder.ToString();
    }
}
