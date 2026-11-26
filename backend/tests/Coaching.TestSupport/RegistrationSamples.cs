using Coaching.Domain.Students;

namespace Coaching.TestSupport;

/// <summary>
/// Form đăng ký hợp lệ, đủ mọi mục (ngày chuyển khoản 01/11/2026 ≤ "hôm nay" của mọi test).
/// @ADR-f02-import-excel-001: mục theo câu hỏi Google Form.
/// </summary>
public static class RegistrationSamples
{
    public static RegistrationInput Complete() => new()
    {
        Pack = "Pack 2",
        Price = 3_500_000,
        TransferDate = new DateOnly(2026, 11, 1),
        Address = "TP. Hồ Chí Minh",
        Age = 28,
        HeightCm = 172m,
        WeightKg = 72.5m,
        ChestCm = 98m,
        BellyCm = 86m,
        WaistCm = 82m,
        HipCm = 96m,
        RightArmCm = 34m,
        LeftArmCm = 33.5m,
        RightThighCm = 56m,
        LeftThighCm = 55.5m,
        BodyFatAndMethod = "18 % – InBody",
        TrainingYears = "1–2 năm",
        WeeklyTrainingTime = "5 buổi, mỗi buổi 75 phút",
        WeightHeightBeforeLifting = "68 kg, 171 cm",
        TrainingGoal = "Giảm mỡ, giữ cơ",
        CurrentTraining = "Push / Pull / Legs, mỗi bài 3 set",
        MedicalHistory = "Không",
        FoodBudgetReadiness = "Sẵn sàng",
        Occupation = "Nhân viên văn phòng",
        ActivityLevel = "Nhẹ",
        StressLevel = "Vừa",
        SleepDuration = "6–7 tiếng",
        Caffeine = "2 ly cà phê sữa",
        Supplements = "Whey",
        CurrentDiet = "3 bữa chính, ăn ngoài buổi trưa",
        MealAndTrainingTimes = "Ăn 7h, 12h, 19h. Tập 17h30–19h.",
        SessionsPerWeek = "5 buổi",
        ProgramStyle = "Upper / Lower",
        BodyPhotoUrl = "https://drive.google.com/open?id=front",
        Expectations = "Có lộ trình rõ ràng",
        PreviousCoaching = "Chưa từng theo coach",
        InvestmentReason = "Muốn khỏe hơn trước Tết",
    };
}
