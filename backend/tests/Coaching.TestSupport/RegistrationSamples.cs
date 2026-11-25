using Coaching.Domain.Students;

namespace Coaching.TestSupport;

/// <summary>Form đăng ký hợp lệ, đủ mọi mục (ngày chuyển khoản 01/11/2026 ≤ "hôm nay" của mọi test).</summary>
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
        BodyFatPercent = 18m,
        BodyFatMethod = "InBody",
        TrainingYears = "1–2 năm",
        WeeklyTrainingTime = "5 buổi, mỗi buổi 75 phút",
        WeightBeforeLiftingKg = 68m,
        HeightThenCm = 171m,
        TrainingGoal = "Giảm mỡ, giữ cơ",
        CurrentTraining = "Push / Pull / Legs, mỗi bài 3 set",
        MedicalHistory = "Không",
        Availability = "T3–T7 buổi tối, CN buổi sáng",
        Equipment = "Phòng gym đầy đủ",
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
        PhotoFrontUrl = "https://drive.google.com/open?id=front",
        PhotoBackUrl = "https://drive.google.com/open?id=back",
        PhotoLeftUrl = "https://drive.google.com/open?id=left",
        PhotoRightUrl = "https://drive.google.com/open?id=right",
        Expectations = "Có lộ trình rõ ràng",
        PreviousCoaching = "Chưa từng theo coach",
        InvestmentReason = "Muốn khỏe hơn trước Tết",
    };
}
