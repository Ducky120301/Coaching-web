namespace Coaching.Domain.Students;

/// <summary>
/// Câu trả lời form đăng ký thô (chưa kiểm) — cũng là hình dạng JSON `registration` của API.
/// @ADR-f02-import-excel-001: theo câu hỏi Google Form; khoá cũ (availability, equipment, bodyFatPercent, bodyFatMethod,
/// weightBeforeLiftingKg, heightThenCm, photo*Url) gửi lên bị bỏ qua.
/// </summary>
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
    public string? BodyFatAndMethod { get; init; } // @BR-f02-import-excel-004

    // 3. Tập luyện hiện tại
    public string? TrainingYears { get; init; } // @BR-f02-import-excel-003
    public string? WeeklyTrainingTime { get; init; }
    public string? WeightHeightBeforeLifting { get; init; } // @BR-f02-import-excel-005
    public string? TrainingGoal { get; init; }
    public string? CurrentTraining { get; init; }
    public string? MedicalHistory { get; init; }

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
    public string? BodyPhotoUrl { get; init; } // @BR-f02-import-excel-006

    // 6. Kỳ vọng
    public string? Expectations { get; init; }
    public string? PreviousCoaching { get; init; }
    public string? InvestmentReason { get; init; }
}
