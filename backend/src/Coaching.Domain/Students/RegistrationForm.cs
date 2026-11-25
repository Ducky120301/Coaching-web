using Coaching.Domain.Common;

namespace Coaching.Domain.Students;

/// <summary>
/// Câu trả lời form đăng ký đã kiểm.
/// @ADR-f02-ho-so-hoc-vien-002: owned value object lưu một cột JSON.
/// @ADR-f02-ho-so-hoc-vien-006: một nguồn luật kiểm cho tạo, sửa và import sau này; lỗi đầu tiên → DomainException → 400.
/// Văn bản đã trim; chuỗi chỉ có khoảng trắng coi là trống (null).
/// </summary>
public sealed class RegistrationForm
{
    // @ADR-f02-ho-so-hoc-vien-009
    public const int LineMaxLength = 200;
    public const int ParagraphMaxLength = 2000;
    public const int UrlMaxLength = 2048;

    public string? Pack { get; private set; }
    public long? Price { get; private set; }
    public DateOnly? TransferDate { get; private set; }
    public string? Address { get; private set; }
    public int? Age { get; private set; }
    public decimal? HeightCm { get; private set; }
    public decimal? WeightKg { get; private set; }
    public decimal? ChestCm { get; private set; }
    public decimal? BellyCm { get; private set; }
    public decimal? WaistCm { get; private set; }
    public decimal? HipCm { get; private set; }
    public decimal? RightArmCm { get; private set; }
    public decimal? LeftArmCm { get; private set; }
    public decimal? RightThighCm { get; private set; }
    public decimal? LeftThighCm { get; private set; }
    public decimal? BodyFatPercent { get; private set; }
    public string? BodyFatMethod { get; private set; }
    public string? TrainingYears { get; private set; }
    public string? WeeklyTrainingTime { get; private set; }
    public decimal? WeightBeforeLiftingKg { get; private set; }
    public decimal? HeightThenCm { get; private set; }
    public string? TrainingGoal { get; private set; }
    public string? CurrentTraining { get; private set; }
    public string? MedicalHistory { get; private set; }
    public string? Availability { get; private set; }
    public string? Equipment { get; private set; }
    public string? FoodBudgetReadiness { get; private set; }
    public string? Occupation { get; private set; }
    public string? ActivityLevel { get; private set; }
    public string? StressLevel { get; private set; }
    public string? SleepDuration { get; private set; }
    public string? Caffeine { get; private set; }
    public string? Supplements { get; private set; }
    public string? CurrentDiet { get; private set; }
    public string? MealAndTrainingTimes { get; private set; }
    public string? SessionsPerWeek { get; private set; }
    public string? ProgramStyle { get; private set; }
    public string? PhotoFrontUrl { get; private set; }
    public string? PhotoBackUrl { get; private set; }
    public string? PhotoLeftUrl { get; private set; }
    public string? PhotoRightUrl { get; private set; }
    public string? Expectations { get; private set; }
    public string? PreviousCoaching { get; private set; }
    public string? InvestmentReason { get; private set; }

    private RegistrationForm() { }

    /// <summary>Form trống (mỗi lần một instance mới — owned entity không dùng chung giữa các hồ sơ).</summary>
    public static RegistrationForm CreateEmpty() => new();

    public static RegistrationForm Create(RegistrationInput input, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new RegistrationForm
        {
            Pack = Option("pack", input.Pack, RegistrationOptions.Packs),
            Price = PriceOf(input.Price),
            TransferDate = TransferDateOf(input.TransferDate, today),
            Address = Line("address", input.Address),
            Age = AgeOf(input.Age),
            HeightCm = Range("heightCm", input.HeightCm, 100, 250),
            WeightKg = Range("weightKg", input.WeightKg, 20, 300),
            ChestCm = Range("chestCm", input.ChestCm, 10, 250),
            BellyCm = Range("bellyCm", input.BellyCm, 10, 250),
            WaistCm = Range("waistCm", input.WaistCm, 10, 250),
            HipCm = Range("hipCm", input.HipCm, 10, 250),
            RightArmCm = Range("rightArmCm", input.RightArmCm, 10, 250),
            LeftArmCm = Range("leftArmCm", input.LeftArmCm, 10, 250),
            RightThighCm = Range("rightThighCm", input.RightThighCm, 10, 250),
            LeftThighCm = Range("leftThighCm", input.LeftThighCm, 10, 250),
            BodyFatPercent = Range("bodyFatPercent", input.BodyFatPercent, 3, 70),
            BodyFatMethod = Option("bodyFatMethod", input.BodyFatMethod, RegistrationOptions.BodyFatMethods),
            TrainingYears = Option("trainingYears", input.TrainingYears, RegistrationOptions.TrainingYears),
            WeeklyTrainingTime = Line("weeklyTrainingTime", input.WeeklyTrainingTime),
            WeightBeforeLiftingKg = Range("weightBeforeLiftingKg", input.WeightBeforeLiftingKg, 20, 300),
            HeightThenCm = Range("heightThenCm", input.HeightThenCm, 100, 250),
            TrainingGoal = Paragraph("trainingGoal", input.TrainingGoal),
            CurrentTraining = Paragraph("currentTraining", input.CurrentTraining),
            MedicalHistory = Paragraph("medicalHistory", input.MedicalHistory),
            Availability = Paragraph("availability", input.Availability),
            Equipment = Paragraph("equipment", input.Equipment),
            FoodBudgetReadiness = Option("foodBudgetReadiness", input.FoodBudgetReadiness, RegistrationOptions.FoodBudgetReadiness),
            Occupation = Line("occupation", input.Occupation),
            ActivityLevel = Option("activityLevel", input.ActivityLevel, RegistrationOptions.ActivityLevels),
            StressLevel = Option("stressLevel", input.StressLevel, RegistrationOptions.StressLevels),
            SleepDuration = Option("sleepDuration", input.SleepDuration, RegistrationOptions.SleepDurations),
            Caffeine = Line("caffeine", input.Caffeine),
            Supplements = Paragraph("supplements", input.Supplements),
            CurrentDiet = Paragraph("currentDiet", input.CurrentDiet),
            MealAndTrainingTimes = Paragraph("mealAndTrainingTimes", input.MealAndTrainingTimes),
            SessionsPerWeek = Option("sessionsPerWeek", input.SessionsPerWeek, RegistrationOptions.SessionsPerWeek),
            ProgramStyle = Option("programStyle", input.ProgramStyle, RegistrationOptions.ProgramStyles),
            PhotoFrontUrl = Url("photoFrontUrl", input.PhotoFrontUrl),
            PhotoBackUrl = Url("photoBackUrl", input.PhotoBackUrl),
            PhotoLeftUrl = Url("photoLeftUrl", input.PhotoLeftUrl),
            PhotoRightUrl = Url("photoRightUrl", input.PhotoRightUrl),
            Expectations = Paragraph("expectations", input.Expectations),
            PreviousCoaching = Paragraph("previousCoaching", input.PreviousCoaching),
            InvestmentReason = Paragraph("investmentReason", input.InvestmentReason),
        };
    }

    /// <summary>@BR-f02-ho-so-hoc-vien-004: mã các mục bắt buộc còn trống, theo thứ tự form.</summary>
    public IReadOnlyList<string> MissingRequired()
    {
        var values = ValuesByCode();
        return RegistrationFields.All.Where(f => f.Required && values[f.Code] is null).Select(f => f.Code).ToList();
    }

    public RegistrationInput ToInput() => new()
    {
        Pack = Pack, Price = Price, TransferDate = TransferDate, Address = Address, Age = Age, HeightCm = HeightCm,
        WeightKg = WeightKg, ChestCm = ChestCm, BellyCm = BellyCm, WaistCm = WaistCm, HipCm = HipCm,
        RightArmCm = RightArmCm, LeftArmCm = LeftArmCm, RightThighCm = RightThighCm, LeftThighCm = LeftThighCm,
        BodyFatPercent = BodyFatPercent, BodyFatMethod = BodyFatMethod,
        TrainingYears = TrainingYears, WeeklyTrainingTime = WeeklyTrainingTime, WeightBeforeLiftingKg = WeightBeforeLiftingKg,
        HeightThenCm = HeightThenCm, TrainingGoal = TrainingGoal, CurrentTraining = CurrentTraining,
        MedicalHistory = MedicalHistory, Availability = Availability, Equipment = Equipment,
        FoodBudgetReadiness = FoodBudgetReadiness, Occupation = Occupation, ActivityLevel = ActivityLevel,
        StressLevel = StressLevel, SleepDuration = SleepDuration, Caffeine = Caffeine, Supplements = Supplements,
        CurrentDiet = CurrentDiet, MealAndTrainingTimes = MealAndTrainingTimes,
        SessionsPerWeek = SessionsPerWeek, ProgramStyle = ProgramStyle,
        PhotoFrontUrl = PhotoFrontUrl, PhotoBackUrl = PhotoBackUrl, PhotoLeftUrl = PhotoLeftUrl, PhotoRightUrl = PhotoRightUrl,
        Expectations = Expectations, PreviousCoaching = PreviousCoaching, InvestmentReason = InvestmentReason,
    };

    private Dictionary<string, object?> ValuesByCode() => new()
    {
        ["pack"] = Pack, ["price"] = Price, ["transferDate"] = TransferDate, ["address"] = Address, ["age"] = Age,
        ["heightCm"] = HeightCm, ["weightKg"] = WeightKg, ["chestCm"] = ChestCm, ["bellyCm"] = BellyCm,
        ["waistCm"] = WaistCm, ["hipCm"] = HipCm, ["rightArmCm"] = RightArmCm, ["leftArmCm"] = LeftArmCm,
        ["rightThighCm"] = RightThighCm, ["leftThighCm"] = LeftThighCm, ["bodyFatPercent"] = BodyFatPercent,
        ["bodyFatMethod"] = BodyFatMethod, ["trainingYears"] = TrainingYears, ["weeklyTrainingTime"] = WeeklyTrainingTime,
        ["weightBeforeLiftingKg"] = WeightBeforeLiftingKg, ["heightThenCm"] = HeightThenCm,
        ["trainingGoal"] = TrainingGoal, ["currentTraining"] = CurrentTraining, ["medicalHistory"] = MedicalHistory,
        ["availability"] = Availability, ["equipment"] = Equipment, ["foodBudgetReadiness"] = FoodBudgetReadiness,
        ["occupation"] = Occupation, ["activityLevel"] = ActivityLevel, ["stressLevel"] = StressLevel,
        ["sleepDuration"] = SleepDuration, ["caffeine"] = Caffeine, ["supplements"] = Supplements,
        ["currentDiet"] = CurrentDiet, ["mealAndTrainingTimes"] = MealAndTrainingTimes,
        ["sessionsPerWeek"] = SessionsPerWeek, ["programStyle"] = ProgramStyle,
        ["photoFrontUrl"] = PhotoFrontUrl, ["photoBackUrl"] = PhotoBackUrl, ["photoLeftUrl"] = PhotoLeftUrl,
        ["photoRightUrl"] = PhotoRightUrl, ["expectations"] = Expectations, ["previousCoaching"] = PreviousCoaching,
        ["investmentReason"] = InvestmentReason,
    };

    // @ADR-f02-ho-so-hoc-vien-009
    private static string? Text(string code, string? raw, int maxLength)
    {
        var value = raw?.Trim();
        if (string.IsNullOrEmpty(value))
            return null; // @BR-f02-ho-so-hoc-vien-004: chỉ khoảng trắng = trống
        if (value.Length > maxLength)
            throw Invalid(code, "too_long", $"{RegistrationFields.LabelOf(code)} tối đa {maxLength} ký tự.");
        return value;
    }

    private static string? Line(string code, string? raw) => Text(code, raw, LineMaxLength);

    private static string? Paragraph(string code, string? raw) => Text(code, raw, ParagraphMaxLength);

    // @BR-f02-ho-so-hoc-vien-009
    private static string? Option(string code, string? raw, IReadOnlyList<string> options)
    {
        var value = raw?.Trim();
        if (string.IsNullOrEmpty(value))
            return null;
        if (!options.Contains(value))
            throw Invalid(code, "invalid_option", $"{RegistrationFields.LabelOf(code)} không có lựa chọn \"{value}\".");
        return value;
    }

    // @BR-f02-ho-so-hoc-vien-008
    private static decimal? Range(string code, decimal? value, decimal min, decimal max)
    {
        if (value is { } number && (number < min || number > max))
            throw Invalid(code, "out_of_range", $"{RegistrationFields.LabelOf(code)} phải từ {min} đến {max}.");
        return value;
    }

    // @BR-f02-ho-so-hoc-vien-008: tuổi là số nguyên 10–100
    private static int? AgeOf(int? value) =>
        value is null or (>= 10 and <= 100)
            ? value
            : throw Invalid("age", "out_of_range", "Tuổi phải từ 10 đến 100.");

    // @BR-f02-ho-so-hoc-vien-008: giá là số nguyên từ 0 đồng
    private static long? PriceOf(long? value) =>
        value is null or >= 0 ? value : throw Invalid("price", "out_of_range", "Giá phải từ 0 đồng.");

    // @BR-f02-ho-so-hoc-vien-010: today = IClock.Today (giờ Việt Nam)
    private static DateOnly? TransferDateOf(DateOnly? value, DateOnly today) =>
        value is { } date && date > today
            ? throw Invalid("transferDate", "in_future", "Ngày chuyển khoản không được sau ngày hôm nay.")
            : value;

    // @BR-f02-ho-so-hoc-vien-011
    private static string? Url(string code, string? raw)
    {
        var value = Text(code, raw, UrlMaxLength);
        if (value is null)
            return null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw Invalid(code, "invalid_url",
                $"{RegistrationFields.LabelOf(code)} phải là địa chỉ web bắt đầu bằng http:// hoặc https://.");
        return value;
    }

    private static DomainException Invalid(string code, string error, string message) =>
        new($"student.{code}.{error}", message);
}
