using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Import;

/// <summary>
/// @ADR-f02-import-excel-007: import đồng bộ trong một request — kiểm file (≤ 5 MB, đọc được, đủ cột, 1…500 dòng)
/// rồi duyệt từng dòng; mỗi dòng hợp lệ tạo hồ sơ bằng StudentRegistrar trong transaction riêng. File chỉ nằm trong bộ nhớ.
/// Bản Task 4: đọc email, họ tên và các mục chữ/link; ô số, ngày, giá và mục có danh sách cho trước đọc ở Task 5.
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

    private static readonly WorkbookCell EmptyCell = new(null, null, null);

    /// <summary>Mục chữ / link của form: ghi nguyên văn chữ của ô, luật độ dài + link kiểm ở RegistrationForm.Validate.</summary>
    private static readonly IReadOnlyDictionary<string, Func<RegistrationInput, string?, RegistrationInput>> TextFields =
        new Dictionary<string, Func<RegistrationInput, string?, RegistrationInput>>
        {
            ["address"] = (r, v) => r with { Address = v },
            ["bodyFatAndMethod"] = (r, v) => r with { BodyFatAndMethod = v },
            ["trainingYears"] = (r, v) => r with { TrainingYears = v },
            ["weeklyTrainingTime"] = (r, v) => r with { WeeklyTrainingTime = v },
            ["weightHeightBeforeLifting"] = (r, v) => r with { WeightHeightBeforeLifting = v },
            ["trainingGoal"] = (r, v) => r with { TrainingGoal = v },
            ["currentTraining"] = (r, v) => r with { CurrentTraining = v },
            ["medicalHistory"] = (r, v) => r with { MedicalHistory = v },
            ["occupation"] = (r, v) => r with { Occupation = v },
            ["caffeine"] = (r, v) => r with { Caffeine = v },
            ["supplements"] = (r, v) => r with { Supplements = v },
            ["currentDiet"] = (r, v) => r with { CurrentDiet = v },
            ["mealAndTrainingTimes"] = (r, v) => r with { MealAndTrainingTimes = v },
            ["bodyPhotoUrl"] = (r, v) => r with { BodyPhotoUrl = v },
            ["expectations"] = (r, v) => r with { Expectations = v },
            ["previousCoaching"] = (r, v) => r with { PreviousCoaching = v },
            ["investmentReason"] = (r, v) => r with { InvestmentReason = v },
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

        var results = new List<ImportRowDto>(rows.Count);
        foreach (var row in rows)
            results.Add(await ImportRowAsync(coach.Id, row, columns, ct));

        var created = results.Count(r => r.Status == StatusCreated);
        return new ImportResultDto(created, results.Count - created, results);
    }

    private async Task<ImportRowDto> ImportRowAsync(Guid coachId, WorkbookRow row, ImportColumns columns, CancellationToken ct)
    {
        var errors = new List<ImportCellErrorDto>();
        var emailText = columns.Cell(row, StudentImportColumns.Email).Text;
        var fullName = columns.Cell(row, StudentImportColumns.FullName).Text;

        Email? email = null;
        try
        {
            email = Email.Create(emailText);
        }
        catch (DomainException ex)
        {
            errors.Add(new ImportCellErrorDto(columns.HeaderOf(StudentImportColumns.Email), ex.Message));
        }

        var input = new RegistrationInput();
        foreach (var field in columns.Fields)
        {
            if (TextFields.TryGetValue(field, out var set))
                input = set(input, columns.Cell(row, field).Text);
        }

        var (form, fieldErrors) = RegistrationForm.Validate(input, clock.Today);
        errors.AddRange(fieldErrors.Select(e => new ImportCellErrorDto(columns.HeaderOf(e.Code), e.Message)));
        if (errors.Count > 0 || email is null || form is null)
            return Skip(row, email?.Value ?? emailText, errors);

        Result<Student> registered;
        try
        {
            registered = await registrar.RegisterAsync(coachId, fullName, email, form, null, ct);
        }
        catch (DomainException ex) // vd họ tên trống / quá dài — Student.Create kiểm
        {
            return Skip(row, email.Value, [new ImportCellErrorDto(columns.HeaderOf(StudentImportColumns.FullName), ex.Message)]);
        }

        return registered.IsSuccess
            ? new ImportRowDto(row.RowNumber, email.Value, StatusCreated, registered.Value.Id, [])
            : Skip(row, email.Value, [new ImportCellErrorDto(columns.HeaderOf(StudentImportColumns.Email), registered.Error!.Message)]);
    }

    private static ImportRowDto Skip(WorkbookRow row, string? email, IReadOnlyList<ImportCellErrorDto> errors) =>
        new(row.RowNumber, email, StatusSkipped, null, errors);

    private static bool IsBlank(WorkbookRow row) =>
        row.Cells.All(c => c.Text is null && c.Number is null && c.Date is null);

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
