namespace Coaching.Domain.Students;

public sealed record RegistrationField(string Code, string Label, bool Required);

/// <summary>Mục của form theo thứ tự hiển thị; Required = bắt buộc trước khi gửi tài khoản (F01-P1 kiểm).</summary>
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
        new("bodyFatPercent", "Mỡ cơ thể", false),
        new("bodyFatMethod", "Cách đo mỡ", false),
        new("trainingYears", "Số năm tập luyện", true),
        new("weeklyTrainingTime", "Thời gian dành cho tập mỗi tuần", true),
        new("weightBeforeLiftingKg", "Cân nặng trước khi tập tạ", true),
        new("heightThenCm", "Chiều cao khi đó", true),
        new("trainingGoal", "Mục tiêu tập luyện", true),
        new("currentTraining", "Chế độ tập hiện tại", false),
        new("medicalHistory", "Tiền sử bệnh lý và chấn thương", true),
        new("availability", "Lịch rảnh", false),
        new("equipment", "Dụng cụ", false),
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
        new("photoFrontUrl", "Ảnh mặt trước", true),
        new("photoBackUrl", "Ảnh mặt sau", true),
        new("photoLeftUrl", "Ảnh bên trái", true),
        new("photoRightUrl", "Ảnh bên phải", true),
        new("expectations", "Mong đợi gì từ coach", true),
        new("previousCoaching", "Từng theo coach / online coaching chưa, trải nghiệm ra sao", true),
        new("investmentReason", "Điều gì khiến bạn quyết định đầu tư vào bản thân lúc này", true),
    ];

    public static string LabelOf(string code) => All.Single(f => f.Code == code).Label;
}
