using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Import;

/// <summary>
/// @ADR-f02-import-excel-007: import đồng bộ trong một request — kiểm file (≤ 5 MB, đọc được, đủ cột, 1…500 dòng)
/// rồi duyệt từng dòng; mỗi dòng hợp lệ tạo hồ sơ bằng StudentRegistrar trong transaction riêng. File chỉ nằm trong bộ nhớ.
/// </summary>
public sealed class ImportStudentsHandler(
    CurrentCoach currentCoach,
    IWorkbookReader reader,
    StudentRegistrar registrar,
    IClock clock)
{
    public const long MaxBytes = 5 * 1024 * 1024; // @BR-f02-import-excel-015
    public const int MaxRows = 500; // @BR-f02-import-excel-016

    private const string StatusCreated = "Created";
    private const string StatusSkipped = "Skipped";
    private const string Required = "bắt buộc"; // @BR-f02-import-excel-024
    private const string InvalidEmail = "email không hợp lệ"; // @BR-f02-import-excel-023
    private const string EmailTaken = "email đã có hồ sơ"; // @BR-f02-import-excel-025
    private const string NotInList = "không thuộc danh sách"; // @BR-f02-import-excel-030

    private static readonly WorkbookCell EmptyCell = new(null, null, null);

    /// <summary>Đọc một ô vào mục form; Error = lý do khi ô sai kiểu hoặc không thuộc danh sách (mục giữ trống).</summary>
    private delegate (RegistrationInput Input, string? Error) FieldReader(RegistrationInput input, WorkbookCell cell);

    /// <summary>Mã mục form → cách đọc ô. Khoảng giá trị, độ dài, link, ngày ≤ hôm nay: RegistrationForm.Validate.</summary>
    private static readonly IReadOnlyDictionary<string, FieldReader> Fields = new Dictionary<string, FieldReader>
    {
        ["pack"] = Option("pack", (r, v) => r with { Pack = v }),
        ["price"] = Price((r, v) => r with { Price = v }),
        ["transferDate"] = Date((r, v) => r with { TransferDate = v }),
        ["address"] = Text((r, v) => r with { Address = v }),
        ["age"] = Integer((r, v) => r with { Age = v }),
        ["heightCm"] = Decimal((r, v) => r with { HeightCm = v }),
        ["weightKg"] = Decimal((r, v) => r with { WeightKg = v }),
        ["chestCm"] = Decimal((r, v) => r with { ChestCm = v }),
        ["bellyCm"] = Decimal((r, v) => r with { BellyCm = v }),
        ["waistCm"] = Decimal((r, v) => r with { WaistCm = v }),
        ["hipCm"] = Decimal((r, v) => r with { HipCm = v }),
        ["rightArmCm"] = Decimal((r, v) => r with { RightArmCm = v }),
        ["leftArmCm"] = Decimal((r, v) => r with { LeftArmCm = v }),
        ["rightThighCm"] = Decimal((r, v) => r with { RightThighCm = v }),
        ["leftThighCm"] = Decimal((r, v) => r with { LeftThighCm = v }),
        ["bodyFatAndMethod"] = Text((r, v) => r with { BodyFatAndMethod = v }),
        ["trainingYears"] = Text((r, v) => r with { TrainingYears = v }),
        ["weeklyTrainingTime"] = Text((r, v) => r with { WeeklyTrainingTime = v }),
        ["weightHeightBeforeLifting"] = Text((r, v) => r with { WeightHeightBeforeLifting = v }),
        ["trainingGoal"] = Text((r, v) => r with { TrainingGoal = v }),
        ["currentTraining"] = Text((r, v) => r with { CurrentTraining = v }),
        ["medicalHistory"] = Text((r, v) => r with { MedicalHistory = v }),
        ["foodBudgetReadiness"] = Option("foodBudgetReadiness", (r, v) => r with { FoodBudgetReadiness = v }),
        ["occupation"] = Text((r, v) => r with { Occupation = v }),
        ["activityLevel"] = Option("activityLevel", (r, v) => r with { ActivityLevel = v }),
        ["stressLevel"] = Option("stressLevel", (r, v) => r with { StressLevel = v }),
        ["sleepDuration"] = Option("sleepDuration", (r, v) => r with { SleepDuration = v }),
        ["caffeine"] = Text((r, v) => r with { Caffeine = v }),
        ["supplements"] = Text((r, v) => r with { Supplements = v }),
        ["currentDiet"] = Text((r, v) => r with { CurrentDiet = v }),
        ["mealAndTrainingTimes"] = Text((r, v) => r with { MealAndTrainingTimes = v }),
        ["sessionsPerWeek"] = Option("sessionsPerWeek", (r, v) => r with { SessionsPerWeek = v }),
        ["programStyle"] = Option("programStyle", (r, v) => r with { ProgramStyle = v }),
        ["bodyPhotoUrl"] = Text((r, v) => r with { BodyPhotoUrl = v }),
        ["expectations"] = Text((r, v) => r with { Expectations = v }),
        ["previousCoaching"] = Text((r, v) => r with { PreviousCoaching = v }),
        ["investmentReason"] = Text((r, v) => r with { InvestmentReason = v }),
    };

    public async Task<Result<ImportResultDto>> HandleAsync(Stream file, long length, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        // @BR-f02-import-excel-013: hồ sơ thuộc coach đang đăng nhập
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return StudentErrors.CoachNotFound;

        // @BR-f02-import-excel-015: kiểm kích thước trước khi đọc
        if (length > MaxBytes)
            return ImportErrors.FileTooLarge;

        WorkbookSheet sheet;
        try
        {
            sheet = reader.Read(file); // @BR-f02-import-excel-017: chỉ sheet đầu
        }
        catch (InvalidWorkbookException)
        {
            return ImportErrors.FileInvalid; // @BR-f02-import-excel-014
        }

        // @BR-f02-import-excel-018 @BR-f02-import-excel-020 @ADR-f02-import-excel-005
        var columns = new ImportColumns(sheet.Headers);
        // @BR-f02-import-excel-019
        if (!columns.Has(StudentImportColumns.Email))
            return ImportErrors.MissingColumn(StudentImportColumns.EmailHeader);
        if (!columns.Has(StudentImportColumns.FullName))
            return ImportErrors.MissingColumn(StudentImportColumns.FullNameHeader);

        // @BR-f02-import-excel-022: dòng hoàn toàn trống bị bỏ qua, không tính dòng, không báo lỗi
        var rows = sheet.Rows.Where(r => !IsBlank(r)).ToList();
        if (rows.Count == 0)
            return ImportErrors.NoRows; // @BR-f02-import-excel-021
        if (rows.Count > MaxRows)
            return ImportErrors.TooManyRows; // @BR-f02-import-excel-016

        // @BR-f02-import-excel-033: từng dòng độc lập, theo thứ tự trong file
        var firstRowByEmail = new Dictionary<string, int>(StringComparer.Ordinal);
        var results = new List<ImportRowDto>(rows.Count);
        foreach (var row in rows)
            results.Add(await ImportRowAsync(coach.Id, row, columns, firstRowByEmail, ct));

        // @BR-f02-import-excel-034: số dòng đã tạo, bỏ qua + từng dòng kèm số dòng Excel và lý do
        var created = results.Count(r => r.Status == StatusCreated);
        return new ImportResultDto(created, results.Count - created, results);
    }

    private async Task<ImportRowDto> ImportRowAsync(Guid coachId, WorkbookRow row, ImportColumns columns,
        Dictionary<string, int> firstRowByEmail, CancellationToken ct)
    {
        var errors = new List<ImportCellErrorDto>();
        var emailHeader = columns.HeaderOf(StudentImportColumns.Email);
        var nameHeader = columns.HeaderOf(StudentImportColumns.FullName);
        var emailText = columns.Cell(row, StudentImportColumns.Email).Text;
        var fullName = columns.Cell(row, StudentImportColumns.FullName).Text;

        // @BR-f02-import-excel-024: trống email / họ tên → báo ô thiếu
        Email? email = null;
        if (emailText is null)
            errors.Add(new ImportCellErrorDto(emailHeader, Required));
        else if (TryEmail(emailText) is { } parsed)
            email = parsed; // Email đã trim + chữ thường (@BR-f02-import-excel-025, @BR-f02-import-excel-026)
        else
            errors.Add(new ImportCellErrorDto(emailHeader, InvalidEmail)); // @BR-f02-import-excel-023
        if (fullName is null)
            errors.Add(new ImportCellErrorDto(nameHeader, Required));
        else if (fullName.Length > Student.FullNameMaxLength)
            errors.Add(new ImportCellErrorDto(nameHeader, $"tối đa {Student.FullNameMaxLength} ký tự"));

        // @BR-f02-import-excel-026: chỉ dòng xuất hiện trước của một email được xét; dòng sau bị bỏ qua, báo trùng
        if (email is not null)
        {
            if (firstRowByEmail.TryGetValue(email.Value, out var firstRow))
                return Skip(row, email.Value, [new ImportCellErrorDto(emailHeader, $"trùng email với dòng {firstRow}")]);
            firstRowByEmail[email.Value] = row.RowNumber;
        }

        // @BR-f02-import-excel-023: đọc mọi ô có giá trị theo loại mục (@BR-f02-import-excel-030 danh sách,
        // @BR-f02-import-excel-031 ngày, @BR-f02-import-excel-032 giá) rồi kiểm bằng đúng luật của form — mỗi ô sai được báo
        var input = new RegistrationInput();
        foreach (var field in columns.Fields)
        {
            if (!Fields.TryGetValue(field, out var read))
                continue; // email, họ tên
            var (next, error) = read(input, columns.Cell(row, field));
            input = next;
            if (error is not null)
                errors.Add(new ImportCellErrorDto(columns.HeaderOf(field), error));
        }
        var (form, fieldErrors) = RegistrationForm.Validate(input, clock.Today);
        errors.AddRange(fieldErrors.Select(e => new ImportCellErrorDto(columns.HeaderOf(e.Code), e.Message)));

        // @BR-f02-import-excel-023 @BR-f02-import-excel-024: có ô sai hoặc thiếu → bỏ cả dòng
        if (errors.Count > 0 || email is null || fullName is null || form is null)
            return Skip(row, email?.Value ?? emailText, errors);

        // @BR-f02-import-excel-027: không đòi đủ mục bắt buộc — hồ sơ còn thiếu mục vẫn được tạo
        // @BR-f02-import-excel-028 @BR-f02-import-excel-029: StudentRegistrar tạo Chờ kích hoạt + Đang coaching, không gửi email
        // @BR-f02-import-excel-033: mỗi dòng một transaction — hồ sơ đã tạo được giữ dù dòng sau lỗi hay quá trình dừng
        Result<Student> registered;
        try
        {
            registered = await registrar.RegisterAsync(coachId, fullName, email, form, null, ct);
        }
        catch (DomainException ex)
        {
            return Skip(row, email.Value, [new ImportCellErrorDto(nameHeader, ex.Message)]);
        }
        if (registered.IsSuccess)
            return new ImportRowDto(row.RowNumber, email.Value, StatusCreated, registered.Value.Id, []);

        // @BR-f02-import-excel-025: email đã thuộc hồ sơ khác / tài khoản coach → bỏ qua, hồ sơ có sẵn không đổi
        var reason = registered.Error == StudentErrors.EmailTaken ? EmailTaken : registered.Error!.Message;
        return Skip(row, email.Value, [new ImportCellErrorDto(emailHeader, reason)]);
    }

    private static Email? TryEmail(string text)
    {
        try
        {
            return Email.Create(text);
        }
        catch (DomainException)
        {
            return null;
        }
    }

    private static ImportRowDto Skip(WorkbookRow row, string? email, IReadOnlyList<ImportCellErrorDto> errors) =>
        new(row.RowNumber, email, StatusSkipped, null, errors);

    private static bool IsBlank(WorkbookRow row) =>
        row.Cells.All(c => c.Text is null && c.Number is null && c.Date is null);

    private static FieldReader Text(Func<RegistrationInput, string?, RegistrationInput> set) =>
        (input, cell) => (set(input, cell.Text), null);

    // @BR-f02-import-excel-030 @ADR-f02-import-excel-006: đưa về giá trị chuẩn; không đưa về được → ô không hợp lệ
    private static FieldReader Option(string code, Func<RegistrationInput, string?, RegistrationInput> set) =>
        (input, cell) =>
        {
            if (cell.Text is null)
                return (input, null);
            if (RegistrationOptions.Match(code, cell.Text) is not { } value)
                return (input, NotInList);
            return (set(input, value), null);
        };

    private static FieldReader Integer(Func<RegistrationInput, int?, RegistrationInput> set) =>
        (input, cell) =>
        {
            if (!ImportCellParser.TryInteger(cell, out var value, out var error))
                return (input, error);
            return (set(input, value), null);
        };

    private static FieldReader Decimal(Func<RegistrationInput, decimal?, RegistrationInput> set) =>
        (input, cell) =>
        {
            if (!ImportCellParser.TryDecimal(cell, out var value, out var error))
                return (input, error);
            return (set(input, value), null);
        };

    // @BR-f02-import-excel-032
    private static FieldReader Price(Func<RegistrationInput, long?, RegistrationInput> set) =>
        (input, cell) =>
        {
            if (!ImportCellParser.TryPrice(cell, out var value, out var error))
                return (input, error);
            return (set(input, value), null);
        };

    // @BR-f02-import-excel-031
    private static FieldReader Date(Func<RegistrationInput, DateOnly?, RegistrationInput> set) =>
        (input, cell) =>
        {
            if (!ImportCellParser.TryDate(cell, out var value, out var error))
                return (input, error);
            return (set(input, value), null);
        };

    /// <summary>Cột đã nhận ra: mã mục → (vị trí, tiêu đề hiển thị). Hai cột cùng mục → cột đầu tiên thắng.</summary>
    private sealed class ImportColumns
    {
        private readonly Dictionary<string, (int Index, string Header)> _byField = new();

        public ImportColumns(IReadOnlyList<string> headers)
        {
            for (var i = 0; i < headers.Count; i++)
            {
                if (StudentImportColumns.FieldFor(headers[i]) is { } field)
                    _byField.TryAdd(field, (i, StudentImportColumns.DisplayHeader(headers[i])));
            }
        }

        public IEnumerable<string> Fields => _byField.Keys;

        public bool Has(string field) => _byField.ContainsKey(field);

        public string HeaderOf(string field) =>
            _byField.TryGetValue(field, out var column) ? column.Header : RegistrationFields.LabelOf(field);

        public WorkbookCell Cell(WorkbookRow row, string field) =>
            _byField.TryGetValue(field, out var column) && column.Index < row.Cells.Count ? row.Cells[column.Index] : EmptyCell;
    }
}
