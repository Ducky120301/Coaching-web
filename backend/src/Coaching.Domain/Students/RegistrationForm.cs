using Coaching.Domain.Common;

namespace Coaching.Domain.Students;

/// <summary>
/// Câu trả lời form đăng ký đã kiểm.
/// @ADR-f02-ho-so-hoc-vien-002: owned value object lưu một cột JSON.
/// @ADR-f02-ho-so-hoc-vien-006: một nguồn luật kiểm cho tạo, sửa và import.
/// @ADR-f02-import-excel-001: mục theo câu hỏi Google Form (không Lịch rảnh/Dụng cụ; mỡ + cách đo, cân nặng + chiều cao
/// trước khi tập, 4 ảnh → 1 link là mục gộp; Số năm tập luyện là chữ tự do).
/// @ADR-f02-import-excel-003: <see cref="Validate"/> gom MỌI lỗi (import báo từng ô); <see cref="Create"/> ném lỗi đầu tiên như cũ.
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
    public string? BodyFatAndMethod { get; private set; }
    public string? TrainingYears { get; private set; }
    public string? WeeklyTrainingTime { get; private set; }
    public string? WeightHeightBeforeLifting { get; private set; }
    public string? TrainingGoal { get; private set; }
    public string? CurrentTraining { get; private set; }
    public string? MedicalHistory { get; private set; }
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
    public string? BodyPhotoUrl { get; private set; }
    public string? Expectations { get; private set; }
    public string? PreviousCoaching { get; private set; }
    public string? InvestmentReason { get; private set; }

    private RegistrationForm() { }

    /// <summary>Form trống (mỗi lần một instance mới — owned entity không dùng chung giữa các hồ sơ).</summary>
    public static RegistrationForm CreateEmpty() => new();

    /// <summary>
    /// @ADR-f02-import-excel-003: kiểm mọi mục, gom MỌI lỗi theo thứ tự form. <c>Form</c> = null khi có ít nhất 1 lỗi.
    /// </summary>
    public static (RegistrationForm? Form, IReadOnlyList<RegistrationFieldError> Errors) Validate(
        RegistrationInput input, DateOnly today)
    {
        var (form, failures) = Check(input, today);
        return (form, failures.Select(f => f.ToFieldError()).ToList());
    }

    /// <summary>@ADR-f02-ho-so-hoc-vien-006: lỗi đầu tiên → DomainException (mã + câu chữ không đổi) → 400.</summary>
    public static RegistrationForm Create(RegistrationInput input, DateOnly today)
    {
        var (form, failures) = Check(input, today);
        return failures.Count == 0 ? form! : throw failures[0].ToException();
    }

    private static (RegistrationForm? Form, List<Failure> Failures) Check(RegistrationInput input, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(input);
        var check = new Checker();
        // Object initializer chạy theo thứ tự viết = thứ tự form → lỗi đầu tiên giống bản trước
        var form = new RegistrationForm
        {
            Pack = check.Option("pack", input.Pack, RegistrationOptions.Packs),
            Price = check.PriceOf(input.Price),
            TransferDate = check.TransferDateOf(input.TransferDate, today),
            Address = check.Line("address", input.Address),
            Age = check.AgeOf(input.Age),
            HeightCm = check.Range("heightCm", input.HeightCm, 100, 250),
            WeightKg = check.Range("weightKg", input.WeightKg, 20, 300),
            ChestCm = check.Range("chestCm", input.ChestCm, 10, 250),
            BellyCm = check.Range("bellyCm", input.BellyCm, 10, 250),
            WaistCm = check.Range("waistCm", input.WaistCm, 10, 250),
            HipCm = check.Range("hipCm", input.HipCm, 10, 250),
            RightArmCm = check.Range("rightArmCm", input.RightArmCm, 10, 250),
            LeftArmCm = check.Range("leftArmCm", input.LeftArmCm, 10, 250),
            RightThighCm = check.Range("rightThighCm", input.RightThighCm, 10, 250),
            LeftThighCm = check.Range("leftThighCm", input.LeftThighCm, 10, 250),
            BodyFatAndMethod = check.Line("bodyFatAndMethod", input.BodyFatAndMethod), // @BR-f02-import-excel-004
            TrainingYears = check.Line("trainingYears", input.TrainingYears), // @BR-f02-import-excel-003
            WeeklyTrainingTime = check.Line("weeklyTrainingTime", input.WeeklyTrainingTime),
            WeightHeightBeforeLifting = check.Paragraph("weightHeightBeforeLifting", input.WeightHeightBeforeLifting), // @BR-f02-import-excel-005
            TrainingGoal = check.Paragraph("trainingGoal", input.TrainingGoal),
            CurrentTraining = check.Paragraph("currentTraining", input.CurrentTraining),
            MedicalHistory = check.Paragraph("medicalHistory", input.MedicalHistory),
            FoodBudgetReadiness = check.Option("foodBudgetReadiness", input.FoodBudgetReadiness, RegistrationOptions.FoodBudgetReadiness),
            Occupation = check.Line("occupation", input.Occupation),
            ActivityLevel = check.Option("activityLevel", input.ActivityLevel, RegistrationOptions.ActivityLevels),
            StressLevel = check.Option("stressLevel", input.StressLevel, RegistrationOptions.StressLevels),
            SleepDuration = check.Option("sleepDuration", input.SleepDuration, RegistrationOptions.SleepDurations),
            Caffeine = check.Line("caffeine", input.Caffeine),
            Supplements = check.Paragraph("supplements", input.Supplements),
            CurrentDiet = check.Paragraph("currentDiet", input.CurrentDiet),
            MealAndTrainingTimes = check.Paragraph("mealAndTrainingTimes", input.MealAndTrainingTimes),
            SessionsPerWeek = check.Option("sessionsPerWeek", input.SessionsPerWeek, RegistrationOptions.SessionsPerWeek),
            ProgramStyle = check.Option("programStyle", input.ProgramStyle, RegistrationOptions.ProgramStyles),
            BodyPhotoUrl = check.Url("bodyPhotoUrl", input.BodyPhotoUrl), // @BR-f02-import-excel-006
            Expectations = check.Paragraph("expectations", input.Expectations),
            PreviousCoaching = check.Paragraph("previousCoaching", input.PreviousCoaching),
            InvestmentReason = check.Paragraph("investmentReason", input.InvestmentReason),
        };
        return (check.Failures.Count == 0 ? form : null, check.Failures);
    }

    /// <summary>@BR-f02-ho-so-hoc-vien-004 @BR-f02-import-excel-007: mã các mục bắt buộc còn trống, theo thứ tự form.</summary>
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
        BodyFatAndMethod = BodyFatAndMethod, TrainingYears = TrainingYears, WeeklyTrainingTime = WeeklyTrainingTime,
        WeightHeightBeforeLifting = WeightHeightBeforeLifting, TrainingGoal = TrainingGoal, CurrentTraining = CurrentTraining,
        MedicalHistory = MedicalHistory, FoodBudgetReadiness = FoodBudgetReadiness, Occupation = Occupation,
        ActivityLevel = ActivityLevel, StressLevel = StressLevel, SleepDuration = SleepDuration, Caffeine = Caffeine,
        Supplements = Supplements, CurrentDiet = CurrentDiet, MealAndTrainingTimes = MealAndTrainingTimes,
        SessionsPerWeek = SessionsPerWeek, ProgramStyle = ProgramStyle, BodyPhotoUrl = BodyPhotoUrl,
        Expectations = Expectations, PreviousCoaching = PreviousCoaching, InvestmentReason = InvestmentReason,
    };

    private Dictionary<string, object?> ValuesByCode() => new()
    {
        ["pack"] = Pack, ["price"] = Price, ["transferDate"] = TransferDate, ["address"] = Address, ["age"] = Age,
        ["heightCm"] = HeightCm, ["weightKg"] = WeightKg, ["chestCm"] = ChestCm, ["bellyCm"] = BellyCm,
        ["waistCm"] = WaistCm, ["hipCm"] = HipCm, ["rightArmCm"] = RightArmCm, ["leftArmCm"] = LeftArmCm,
        ["rightThighCm"] = RightThighCm, ["leftThighCm"] = LeftThighCm, ["bodyFatAndMethod"] = BodyFatAndMethod,
        ["trainingYears"] = TrainingYears, ["weeklyTrainingTime"] = WeeklyTrainingTime,
        ["weightHeightBeforeLifting"] = WeightHeightBeforeLifting, ["trainingGoal"] = TrainingGoal,
        ["currentTraining"] = CurrentTraining, ["medicalHistory"] = MedicalHistory,
        ["foodBudgetReadiness"] = FoodBudgetReadiness, ["occupation"] = Occupation, ["activityLevel"] = ActivityLevel,
        ["stressLevel"] = StressLevel, ["sleepDuration"] = SleepDuration, ["caffeine"] = Caffeine,
        ["supplements"] = Supplements, ["currentDiet"] = CurrentDiet, ["mealAndTrainingTimes"] = MealAndTrainingTimes,
        ["sessionsPerWeek"] = SessionsPerWeek, ["programStyle"] = ProgramStyle, ["bodyPhotoUrl"] = BodyPhotoUrl,
        ["expectations"] = Expectations, ["previousCoaching"] = PreviousCoaching, ["investmentReason"] = InvestmentReason,
    };

    /// <summary>Một lỗi: <c>Reason</c> cho import (không kèm tên mục), <c>Message</c> cho DomainException (giữ câu cũ).</summary>
    private sealed record Failure(string Field, string Error, string Reason, string Message)
    {
        public RegistrationFieldError ToFieldError() => new(Field, Reason);

        public DomainException ToException() => new($"student.{Field}.{Error}", Message);
    }

    /// <summary>@ADR-f02-import-excel-003: các luật kiểm của F02, ghi lỗi thay vì ném để gom đủ.</summary>
    private sealed class Checker
    {
        public List<Failure> Failures { get; } = [];

        private void Fail(string field, string error, string reason, string? message = null) =>
            Failures.Add(new Failure(field, error, reason, message ?? $"{RegistrationFields.LabelOf(field)} {reason}."));

        // @ADR-f02-ho-so-hoc-vien-009
        private string? Text(string code, string? raw, int maxLength)
        {
            var value = raw?.Trim();
            if (string.IsNullOrEmpty(value))
                return null; // @BR-f02-ho-so-hoc-vien-004: chỉ khoảng trắng = trống
            if (value.Length <= maxLength)
                return value;
            Fail(code, "too_long", $"tối đa {maxLength} ký tự");
            return null;
        }

        public string? Line(string code, string? raw) => Text(code, raw, LineMaxLength);

        public string? Paragraph(string code, string? raw) => Text(code, raw, ParagraphMaxLength);

        // @BR-f02-ho-so-hoc-vien-009
        public string? Option(string code, string? raw, IReadOnlyList<string> options)
        {
            var value = raw?.Trim();
            if (string.IsNullOrEmpty(value))
                return null;
            if (options.Contains(value))
                return value;
            Fail(code, "invalid_option", "không thuộc danh sách",
                $"{RegistrationFields.LabelOf(code)} không có lựa chọn \"{value}\".");
            return null;
        }

        // @BR-f02-ho-so-hoc-vien-008
        public decimal? Range(string code, decimal? value, decimal min, decimal max)
        {
            if (value is not { } number || (number >= min && number <= max))
                return value;
            Fail(code, "out_of_range", $"phải từ {min} đến {max}");
            return null;
        }

        // @BR-f02-ho-so-hoc-vien-008: tuổi là số nguyên 10–100
        public int? AgeOf(int? value)
        {
            if (value is null or (>= 10 and <= 100))
                return value;
            Fail("age", "out_of_range", "phải từ 10 đến 100");
            return null;
        }

        // @BR-f02-ho-so-hoc-vien-008: giá là số nguyên từ 0 đồng
        public long? PriceOf(long? value)
        {
            if (value is null or >= 0)
                return value;
            Fail("price", "out_of_range", "phải từ 0 đồng");
            return null;
        }

        // @BR-f02-ho-so-hoc-vien-010: today = IClock.Today (giờ Việt Nam)
        public DateOnly? TransferDateOf(DateOnly? value, DateOnly today)
        {
            if (value is not { } date || date <= today)
                return value;
            Fail("transferDate", "in_future", "không được sau hôm nay", "Ngày chuyển khoản không được sau ngày hôm nay.");
            return null;
        }

        // @BR-f02-ho-so-hoc-vien-011 @BR-f02-import-excel-006
        public string? Url(string code, string? raw)
        {
            var value = Text(code, raw, UrlMaxLength);
            if (value is null)
                return null;
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                return value;
            Fail(code, "invalid_url", "link ảnh không hợp lệ",
                $"{RegistrationFields.LabelOf(code)} phải là địa chỉ web bắt đầu bằng http:// hoặc https://.");
            return null;
        }
    }
}
