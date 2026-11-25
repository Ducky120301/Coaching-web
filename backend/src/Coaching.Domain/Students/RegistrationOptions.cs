namespace Coaching.Domain.Students;

/// <summary>
/// @BR-f02-ho-so-hoc-vien-009 @ADR-f02-ho-so-hoc-vien-006: danh sách giá trị cho trước, đúng theo form prototype.
/// FE có bản sao chỉ để hiển thị.
/// </summary>
public static class RegistrationOptions
{
    public static readonly IReadOnlyList<string> Packs = ["Pack 1", "Pack 2", "Pack 3", "Khác"];

    public static readonly IReadOnlyList<string> BodyFatMethods =
        ["Ước lượng bằng mắt", "Cân điện tử", "InBody", "Thước kẹp mỡ", "DEXA", "Khác"];

    public static readonly IReadOnlyList<string> TrainingYears =
        ["Chưa tập", "Dưới 6 tháng", "6–12 tháng", "1–2 năm", "2–3 năm", "3–5 năm", "Trên 5 năm"];

    public static readonly IReadOnlyList<string> FoodBudgetReadiness = ["Sẵn sàng", "Đang cân nhắc", "Chưa sẵn sàng"];

    public static readonly IReadOnlyList<string> ActivityLevels =
        ["Ít vận động (ngồi nhiều)", "Nhẹ", "Vừa", "Nhiều", "Rất nhiều (lao động chân tay)"];

    public static readonly IReadOnlyList<string> StressLevels = ["Thấp", "Vừa", "Cao", "Rất cao"];

    public static readonly IReadOnlyList<string> SleepDurations =
        ["Dưới 5 tiếng", "5–6 tiếng", "6–7 tiếng", "7–8 tiếng", "Trên 8 tiếng"];

    public static readonly IReadOnlyList<string> SessionsPerWeek = ["2 buổi", "3 buổi", "4 buổi", "5 buổi", "6 buổi", "7 buổi"];

    public static readonly IReadOnlyList<string> ProgramStyles =
        ["Push / Pull / Legs", "Upper / Lower", "Full body", "Mỗi buổi một nhóm cơ", "Để coach chọn", "Khác"];
}
