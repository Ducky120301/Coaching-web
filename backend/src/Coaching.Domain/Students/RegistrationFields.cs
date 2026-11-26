namespace Coaching.Domain.Students;

public sealed record RegistrationField(string Code, string Label, bool Required);

/// <summary>
/// Mục của form theo thứ tự hiển thị; Required = bắt buộc trước khi gửi tài khoản (F01-P1 kiểm).
/// @ADR-f02-import-excel-001 @BR-f02-import-excel-001: không có Lịch rảnh, Dụng cụ.
/// @BR-f02-import-excel-007: Số năm tập luyện, Cân nặng và chiều cao trước khi tập tạ, Link ảnh toàn thân bắt buộc;
/// Mỡ cơ thể và cách đo không bắt buộc.
/// </summary>
public static class RegistrationFields
{
    public static readonly IReadOnlyList<RegistrationField> All =
    [
        new("pack", "Gói đã mua", true),
        new("price", "Giá", true),
        new("transferDate", "Ngày chuyển khoản", true),
        new("address", "Địa chỉ", true),
        new("age", "Tuổi", true),
        new("heightCm", "Chiều cao", true),
        new("weightKg", "Cân nặng", true),
        new("chestCm", "Vòng 1 (ngực)", false),
        new("bellyCm", "Vòng 2 (bụng)", false),
        new("waistCm", "Vòng eo (trên rốn 4 cm)", false),
        new("hipCm", "Vòng 3 (mông)", false),
        new("rightArmCm", "Tay phải (gồng biceps)", false),
        new("leftArmCm", "Tay trái (gồng biceps)", false),
        new("rightThighCm", "Đùi phải (chỗ to nhất)", false),
        new("leftThighCm", "Đùi trái (chỗ to nhất)", false),
        new("bodyFatAndMethod", "Mỡ cơ thể và cách đo", false), // @BR-f02-import-excel-004
        new("trainingYears", "Số năm tập luyện", true), // @BR-f02-import-excel-003
        new("weeklyTrainingTime", "Thời gian dành cho tập mỗi tuần", true),
        new("weightHeightBeforeLifting", "Cân nặng và chiều cao trước khi tập tạ", true), // @BR-f02-import-excel-005
        new("trainingGoal", "Mục tiêu tập luyện", true),
        new("currentTraining", "Chế độ tập hiện tại", false),
        new("medicalHistory", "Tiền sử bệnh lý và chấn thương", true),
        new("foodBudgetReadiness", "Sẵn sàng đầu tư cho thực phẩm chất lượng", true),
        new("occupation", "Nghề nghiệp", false),
        new("activityLevel", "Mức độ hoạt động", true),
        new("stressLevel", "Mức độ căng thẳng", true),
        new("sleepDuration", "Thời gian ngủ mỗi ngày", true),
        new("caffeine", "Caffeine mỗi ngày", false),
        new("supplements", "Thực phẩm bổ sung đang dùng", false),
        new("currentDiet", "Chế độ dinh dưỡng hiện tại", true),
        new("mealAndTrainingTimes", "Giờ có thể ăn và giờ đi tập", true),
        new("sessionsPerWeek", "Số buổi tập mỗi tuần", true),
        new("programStyle", "Phong cách giáo án muốn theo", true),
        new("bodyPhotoUrl", "Link ảnh toàn thân", true), // @BR-f02-import-excel-006
        new("expectations", "Mong đợi gì từ coach", true),
        new("previousCoaching", "Từng theo coach / online coaching chưa, trải nghiệm ra sao", true),
        new("investmentReason", "Điều gì khiến bạn quyết định đầu tư vào bản thân lúc này", true),
    ];

    public static string LabelOf(string code) => All.Single(f => f.Code == code).Label;
}
