namespace Coaching.Domain.Students;

/// <summary>Câu trả lời form đăng ký thô (chưa kiểm) — cũng là hình dạng JSON `registration` của API.</summary>
public sealed record RegistrationInput
{
    // 1. Tài khoản và gói
    public string? Pack { get; init; }
    public long? Price { get; init; }
    public DateOnly? TransferDate { get; init; }
    public string? Address { get; init; }
    public int? Age { get; init; }
    public decimal? HeightCm { get; init; }

    // 2. Số đo cơ thể hiện tại
    public decimal? WeightKg { get; init; }
    public decimal? ChestCm { get; init; }
    public decimal? BellyCm { get; init; }
    public decimal? WaistCm { get; init; }
    public decimal? HipCm { get; init; }
    public decimal? RightArmCm { get; init; }
    public decimal? LeftArmCm { get; init; }
    public decimal? RightThighCm { get; init; }
    public decimal? LeftThighCm { get; init; }
    public decimal? BodyFatPercent { get; init; }
    public string? BodyFatMethod { get; init; }

    // 3. Tập luyện hiện tại
    public string? TrainingYears { get; init; }
    public string? WeeklyTrainingTime { get; init; }
    public decimal? WeightBeforeLiftingKg { get; init; }
    public decimal? HeightThenCm { get; init; }
    public string? TrainingGoal { get; init; }
    public string? CurrentTraining { get; init; }
    public string? MedicalHistory { get; init; }
    public string? Availability { get; init; }
    public string? Equipment { get; init; }

    // 4. Ăn uống, sinh hoạt
    public string? FoodBudgetReadiness { get; init; }
    public string? Occupation { get; init; }
    public string? ActivityLevel { get; init; }
    public string? StressLevel { get; init; }
    public string? SleepDuration { get; init; }
    public string? Caffeine { get; init; }
    public string? Supplements { get; init; }
    public string? CurrentDiet { get; init; }
    public string? MealAndTrainingTimes { get; init; }

    // 5. Chương trình tập sắp tới
    public string? SessionsPerWeek { get; init; }
    public string? ProgramStyle { get; init; }
    public string? PhotoFrontUrl { get; init; }
    public string? PhotoBackUrl { get; init; }
    public string? PhotoLeftUrl { get; init; }
    public string? PhotoRightUrl { get; init; }

    // 6. Kỳ vọng
    public string? Expectations { get; init; }
    public string? PreviousCoaching { get; init; }
    public string? InvestmentReason { get; init; }
}
