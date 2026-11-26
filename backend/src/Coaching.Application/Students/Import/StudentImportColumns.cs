using System.Text.RegularExpressions;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Import;

/// <summary>
/// @ADR-f02-import-excel-005 @BR-f02-import-excel-018: nhận cột theo tiêu đề câu hỏi Google Form, không theo thứ tự cột.
/// Tiêu đề được chuẩn hoá (dòng đầu, trim, chữ thường, bỏ dấu tiếng Việt, gộp khoảng trắng) rồi so tiền tố theo thứ tự
/// bảng — khớp đầu tiên thắng (vì vậy "dia chi email" đứng trước "dia chi", "can nang cua ban" trước "can nang").
/// </summary>
public static partial class StudentImportColumns
{
    public const string Email = "email";
    public const string FullName = "fullName";

    /// <summary>Tên cột nêu trong lỗi thiếu cột (@BR-f02-import-excel-019).</summary>
    public const string EmailHeader = "Địa chỉ email";
    public const string FullNameHeader = "Họ Và Tên (Name)";

    // Field null = cột cố ý bỏ qua (@BR-f02-import-excel-020)
    private static readonly (string Prefix, string? Field)[] Prefixes =
    [
        ("dau thoi gian", null),
        ("dia chi email", Email),
        ("ho va ten", FullName),
        ("dia chi", "address"),
        ("tuoi", "age"),
        ("chieu cao", "heightCm"),
        ("can nang cua ban", "weightHeightBeforeLifting"),
        ("can nang", "weightKg"),
        ("so do vong 1", "chestCm"),
        ("so do vong 2", "bellyCm"),
        ("so do vong eo", "waistCm"),
        ("so do vong 3", "hipCm"),
        ("so do tay phai", "rightArmCm"),
        ("so do tay trai", "leftArmCm"),
        ("so do dui phai", "rightThighCm"),
        ("so do dui trai", "leftThighCm"),
        ("mo trong co the", "bodyFatAndMethod"),
        ("so nam tap luyen", "trainingYears"),
        ("muc tieu tap luyen", "trainingGoal"),
        ("neu gia su co mot chuong trinh tap", "weeklyTrainingTime"),
        ("trinh bay che do tap luyen", "currentTraining"),
        ("ban co tien su benh ly", "medicalHistory"),
        ("cau hoi quan trong", "foodBudgetReadiness"),
        ("nghe nghiep", "occupation"),
        ("muc do hoat dong", "activityLevel"),
        ("muc do cang thang", "stressLevel"),
        ("thoi gian ngu", "sleepDuration"),
        ("hang ngay ban su dung bao nhieu caffeine", "caffeine"),
        ("neu ten ra cac loai thuc pham ho tro", "supplements"),
        ("trinh bay che do dinh duong", "currentDiet"),
        ("thoi gian ban co the an", "mealAndTrainingTimes"),
        ("ban se tap duoc bao nhieu buoi", "sessionsPerWeek"),
        ("ban muon giao an tap", "programStyle"),
        ("phong cach giao an", "programStyle"),
        ("dinh kem 4 anh", "bodyPhotoUrl"),
        ("ban mong doi dieu gi tu coach", "expectations"),
        ("ban tung theo coach", "previousCoaching"),
        ("dieu gi khien ban quyet dinh dau tu", "investmentReason"),
        // Cột gộp "Bạn mua Pack nào, giá bao nhiêu và ngày chuyển khoản" của file mẫu không phải một mục form
        ("ban mua pack nao, gia", null),
        ("ban mua pack nao", "pack"),
        ("gia goi ban mua", "price"),
        ("ngay chuyen khoan", "transferDate"),
    ];

    /// <summary>Dòng đầu của tiêu đề, đã trim — tên cột hiển thị trong kết quả import.</summary>
    public static string DisplayHeader(string header)
    {
        ArgumentNullException.ThrowIfNull(header);
        return header.Trim().Split('\n')[0].TrimEnd('\r').Trim();
    }

    /// <summary>Dòng đầu, trim, chữ thường, bỏ dấu tiếng Việt (đ → d), gộp khoảng trắng.</summary>
    public static string NormalizeHeader(string header) =>
        Spaces().Replace(SearchText.Normalize(DisplayHeader(header)), " ");

    /// <summary>"email" | "fullName" | mã mục form; null = cột bị bỏ qua (vd "Dấu thời gian").</summary>
    public static string? FieldFor(string header)
    {
        var normalized = NormalizeHeader(header);
        if (normalized.Length == 0)
            return null;
        foreach (var (prefix, field) in Prefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.Ordinal))
                return field;
        }
        return null; // @BR-f02-import-excel-020
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
