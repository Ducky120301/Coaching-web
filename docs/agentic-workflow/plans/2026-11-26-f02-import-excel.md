---
doc-id: 2026-11-26-f02-import-excel
status: ready
---

# Import hồ sơ học viên từ Excel + form theo Google Form: Implementation Plan

> Frontmatter `status: draft | ready` là gate (exec-plan yêu cầu spec + tech-design + plan đều `ready`). User mark Ready sau khi review.

> **For agentic workers:** REQUIRED SUB-SKILL: Use `exec-plan` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Form hồ sơ khớp câu hỏi Google Form (kể cả chuyển dữ liệu hồ sơ cũ) và coach import file Excel để tạo nhiều hồ sơ một lần, có báo cáo từng dòng (BR-f02-import-excel-001→034).

**Architecture:** `RegistrationForm` đổi theo Google Form và có `Validate` gom mọi lỗi; dữ liệu cũ chuyển bằng Liquibase v0.4.0 trên cột JSON. Import đồng bộ: ClosedXML đọc sheet đầu → nhận cột theo tiêu đề → từng dòng kiểm + chuẩn hoá → `StudentRegistrar` tạo hồ sơ trong transaction riêng. Web admin: form mới, khối thông tin mới, card "Import từ Excel" ở trang Tạo hồ sơ; prototype sửa theo.

**Tech Stack:** .NET 10, EF Core 10, ClosedXML (MIT), Liquibase 4.33.0, Reqnroll.xUnit, Testcontainers.MsSql · Angular 22 (zoneless), Vitest · @cucumber/cucumber + Playwright.

## Global Constraints

- Làm trên nhánh `main`, không worktree (quyết định của user). Không push.
- Dependency rule: `Api → Application → Domain`; `Infrastructure → Application, Domain`; Domain không tham chiếu EF/ASP.NET/ClosedXML.
- Không gọi `DateTime.Now`/`DateTime.UtcNow` trong Domain/Application; "hôm nay" theo giờ Việt Nam qua `IClock.Today`.
- Schema/dữ liệu chỉ đổi qua Liquibase `database/changelog/v0.4.0/` (xml + `scripts/*.sql` + `rollback/*.sql`); KHÔNG dùng `dotnet ef migrations`. Chỉ thêm package ClosedXML (MIT), không thêm package nào khác ở backend.
- Giới hạn (nguyên văn spec): "Số năm tập luyện" ≤ 200 ký tự; "Mỡ cơ thể và cách đo" ≤ 200; "Cân nặng và chiều cao trước khi tập tạ" ≤ 2000; "Link ảnh toàn thân" http/https ≤ 2048; file .xlsx ≤ 5 MB và ≤ 500 dòng dữ liệu; chỉ sheet đầu.
- Mục bắt buộc mới: Số năm tập luyện, Cân nặng và chiều cao trước khi tập tạ, Link ảnh toàn thân; Mỡ cơ thể và cách đo không bắt buộc. Bỏ Lịch rảnh, Dụng cụ, 4 link ảnh, mỡ %, cách đo, cân nặng/chiều cao trước khi tập dạng số.
- Import: dòng có ô sai → bỏ cả dòng, báo từng ô (số dòng Excel, tên cột trong file, lý do); trống email/họ tên → bỏ dòng; trùng email hệ thống (kể cả coach, không phân biệt hoa/thường, bỏ khoảng trắng) hoặc trùng dòng trước trong file → bỏ dòng; ô bắt buộc khác để trống vẫn tạo; hồ sơ Chờ kích hoạt + Đang coaching; không gửi email; mỗi dòng transaction riêng; file không lưu xuống đĩa.
- Ngày chuyển khoản: ô ngày Excel hoặc chữ dd/MM/yyyy. Giá: ô số nguyên ≥ 0 hoặc chữ có dấu chấm nghìn, đuôi "đ"/"VNĐ".
- Mã lỗi file (400): `import.file_invalid` "File phải là Excel .xlsx đọc được.", `import.file_too_large` "File vượt giới hạn 5 MB.", `import.too_many_rows` "File vượt giới hạn 500 dòng.", `import.missing_column` "Thiếu cột \"{tên}\".", `import.no_rows` "File không có dòng dữ liệu.".
- FE bám **chính xác** prototype `docs/duobexiuuu-coaching-design.html` (đã sửa theo form mới ở Task 6); breakpoint 880px; không hard-code màu; kiểm ở 390px và 1280px. Câu chữ UI theo mục "Thiết kế giao diện (UI)" của tech-design.
- Living docs `features/hoc-vien/ho-so/{form-dang-ky,import-excel,ho-so-hoc-vien}.feature` đã chốt — KHÔNG sửa nội dung `.feature`. `@api` chạy Reqnroll, `@web` chạy CucumberJS.
- `reqnroll.json` `missingOrPendingStepsOutcome: Error`: scenario của task sau fail `MissingStepDefinition` cho tới khi được bind; mọi lỗi khác phải sửa.
- Annotation truy vết: `// @BR-f02-import-excel-<số>` / `// @ADR-f02-import-excel-<số>` tại code thực thi và trên test (SQL `-- @…`, TS `// @…`).
- Lệnh verify tổng: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"` và `cd frontend && npm run test:ci`.
- Commit message kết thúc bằng đúng hai dòng:
  ```
  Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
  ```

---

## Task map

| Task | Nội dung | Owns |
|---|---|---|
| 1 | Form theo Google Form ở backend + `Validate` gom lỗi | BR-001, 003–007; ADR-001, 003 |
| 2 | Chuyển dữ liệu hồ sơ cũ (Liquibase v0.4.0) | BR-002, 008–011; ADR-002 |
| 3 | Prefactor `StudentRegistrar` | — |
| 4 | Import mức file (ClosedXML, giới hạn, cột theo tiêu đề, API) | BR-012–022; ADR-004, 005, 007 |
| 5 | Import từng dòng (lỗi ô, trùng, chuẩn hoá, ngày/giá, kết quả) | BR-023–034; ADR-006 |
| 6 | [UI] Form Tạo/Sửa hồ sơ, khối Thông tin học viên, sửa prototype | — |
| 7 | [UI] Card "Import từ Excel" | — |
| 8 | e2e `@web` + e2e F02 bị ảnh hưởng | — |

---

### Task 1: Form theo Google Form ở backend — `RegistrationForm`/`RegistrationInput`/`RegistrationFields` mới, `Validate` gom lỗi; sửa fixtures `RegistrationSamples`, steps F02 + bind `form-dang-ky.feature`

**Files:**
- Create: `backend/src/Coaching.Domain/Students/RegistrationFieldError.cs`
- Modify: `backend/src/Coaching.Domain/Students/RegistrationForm.cs`, `RegistrationInput.cs`, `RegistrationFields.cs`
- Modify: `backend/tests/Coaching.TestSupport/RegistrationSamples.cs`
- Modify: `backend/tests/Coaching.Acceptance/Support/RegistrationLabels.cs`, `backend/tests/Coaching.Acceptance/Steps/StudentRegistrationSteps.cs`
- Test: `backend/tests/Coaching.Domain.Tests/Students/RegistrationGoogleFormTests.cs` (mới), `backend/tests/Coaching.Domain.Tests/Students/RegistrationFormTests.cs` (sửa)
- Test: `backend/tests/Coaching.Api.IntegrationTests/Students/RegistrationValidationTests.cs` (thêm 1 test)
- Living docs bind: `features/hoc-vien/ho-so/form-dang-ky.feature` (mọi step `@api`), ví dụ đã đổi trong `features/hoc-vien/ho-so/ho-so-hoc-vien.feature` (BR-f02-ho-so-hoc-vien-004/008/011). KHÔNG sửa `.feature`.

**Interfaces:**
- Consumes (F02/F01-P1): `RegistrationForm.Create(RegistrationInput, DateOnly)` (ADR-f02-ho-so-hoc-vien-006), `RegistrationOptions.*` (giữ nguyên ở task này — `TrainingYears`/`BodyFatMethods` xoá ở Task 5), `DomainException(code, message)`, `StudentDetailDto.Registration` (`RegistrationInput`), `StudentApi.TryCreateAsync/NewEmail/AssertErrorAsync`, `AcceptanceContext.SendJsonAsync/GetAsync`, `StepText.Quoted`, `ApiFactory.AdminClientAsync`, `StudentRequests.Url/NewEmail`.
- Produces:
  - Domain `Coaching.Domain.Students`:
    - `public sealed record RegistrationFieldError(string Code, string Message);` — `Code` = mã mục (`"age"`), `Message` = lý do tiếng Việt không kèm tên mục (`"phải từ 10 đến 100"`, `"tối đa 200 ký tự"`, `"không thuộc danh sách"`, `"không được sau hôm nay"`, `"link ảnh không hợp lệ"`, `"phải từ 0 đồng"`).
    - `RegistrationForm.Validate(RegistrationInput input, DateOnly today)` → `(RegistrationForm? Form, IReadOnlyList<RegistrationFieldError> Errors)`: gom MỌI lỗi theo thứ tự form; `Form` null khi có lỗi.
    - `RegistrationForm.Create(RegistrationInput input, DateOnly today)`: không đổi hành vi — ném `DomainException` của lỗi đầu tiên, mã `student.{code}.{too_long|invalid_option|out_of_range|in_future|invalid_url}` và câu chữ như trước.
    - `RegistrationForm` bỏ `Availability`, `Equipment`, `BodyFatPercent`, `BodyFatMethod`, `WeightBeforeLiftingKg`, `HeightThenCm`, `PhotoFrontUrl/BackUrl/LeftUrl/RightUrl`; thêm `BodyFatAndMethod` (dòng ≤ 200), `WeightHeightBeforeLifting` (đoạn ≤ 2000), `BodyPhotoUrl` (URL http/https ≤ 2048); `TrainingYears` thành dòng ≤ 200 (không danh sách).
    - `RegistrationInput` cùng hình dạng mới → JSON API `registration`: `bodyFatAndMethod`, `weightHeightBeforeLifting`, `bodyPhotoUrl` (khoá cũ gửi lên bị bỏ qua).
    - `RegistrationFields.All` mới: 37 mục, 24 mục bắt buộc; mã mới `bodyFatAndMethod` ("Mỡ cơ thể và cách đo", không bắt buộc), `weightHeightBeforeLifting` ("Cân nặng và chiều cao trước khi tập tạ", bắt buộc), `bodyPhotoUrl` ("Link ảnh toàn thân", bắt buộc).
  - Test support: `RegistrationSamples.Complete()` theo khoá mới (`BodyFatAndMethod = "18 % – InBody"`, `WeightHeightBeforeLifting = "68 kg, 171 cm"`, `BodyPhotoUrl = "https://drive.google.com/open?id=front"`).
  - Acceptance: `RegistrationLabels.Codes` thêm "Số năm tập luyện", "Mỡ cơ thể và cách đo", "Cân nặng và chiều cao trước khi tập tạ", "Link ảnh toàn thân" (bỏ "Mỡ cơ thể", "Ảnh mặt trước", "Ảnh mặt sau"); step mới `coach tạo hồ sơ học viên đủ mọi mục bắt buộc`, `hồ sơ không có mục "…" và "…"`, `coach lưu hồ sơ học viên đủ mọi mục bắt buộc và để trống "…"`, `coach lưu hồ sơ học viên có link ảnh toàn thân "…"`, `hồ sơ ghi nhận link ảnh toàn thân "…"` (thay 2 step "ảnh mặt trước").

**Owns:** `@BR-f02-import-excel-001`, `@BR-f02-import-excel-003`, `@BR-f02-import-excel-004`, `@BR-f02-import-excel-005`, `@BR-f02-import-excel-006`, `@BR-f02-import-excel-007` | `@ADR-f02-import-excel-001`, `@ADR-f02-import-excel-003`

Ghi chú bind step (đã soát `Steps/*.cs`, không bind trùng):
| Step trong `form-dang-ky.feature` | Binding |
|---|---|
| `coach "Coach Duobexiuuu" đã đăng nhập` | có sẵn `StudentCreateSteps.CoachLoggedIn` |
| `coach tạo hồ sơ học viên đủ mọi mục bắt buộc` | **mới** `StudentRegistrationSteps.CreateCompleteWithLegacyFields` |
| `hồ sơ không có mục "Lịch rảnh" và "Dụng cụ"` | **mới** `StudentRegistrationSteps.HasNoFields` |
| `coach lưu hồ sơ học viên có "<mục>" là "<giá trị>"` / `hồ sơ ghi nhận "<mục>" là "<giá trị>"` | có sẵn `SaveWithValue`/`Recorded` — chỉ thêm nhãn vào `RegistrationLabels.Codes` |
| `hồ sơ không được lưu vì link ảnh không hợp lệ` | có sẵn `PhotoInvalid` — đổi mã thành `student.bodyPhotoUrl.invalid_url` (dùng chung với F02 BR-011) |
| `coach lưu hồ sơ học viên đủ mọi mục bắt buộc trừ "Link ảnh toàn thân"` / `hồ sơ cho biết còn thiếu đúng 1 mục: …` | có sẵn `SaveCompleteExcept`/`MissingExactly` |
| `coach lưu hồ sơ học viên đủ mọi mục bắt buộc và để trống "Mỡ cơ thể và cách đo"` | **mới** `SaveCompleteWithEmpty` (không khớp regex `… trừ (.+)$` vì không có chữ "trừ") |
| `hồ sơ cho biết không còn mục bắt buộc nào thiếu` | có sẵn `NothingMissing` |
| F02: `coach lưu hồ sơ học viên có link ảnh toàn thân "…"` / `hồ sơ ghi nhận link ảnh toàn thân "…"` | **thay** 2 step "ảnh mặt trước" (text cũ không còn trong living docs) |

- [x] **Step 1: Viết Domain test cho form theo Google Form + `Validate` gom lỗi**

`backend/tests/Coaching.Domain.Tests/Students/RegistrationGoogleFormTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.TestSupport;

namespace Coaching.Domain.Tests.Students;

public sealed class RegistrationGoogleFormTests
{
    private static readonly DateOnly Today = new(2026, 11, 26);

    private static RegistrationForm Create(RegistrationInput input) => RegistrationForm.Create(input, Today);

    private static string ErrorOf(RegistrationInput input) =>
        Assert.Throws<DomainException>(() => Create(input)).Code;

    // @BR-f02-import-excel-001 @ADR-f02-import-excel-001: form không còn Lịch rảnh, Dụng cụ
    [Fact]
    public void Form_has_no_availability_or_equipment()
    {
        foreach (var name in new[] { "Availability", "Equipment" })
        {
            Assert.Null(typeof(RegistrationInput).GetProperty(name));
            Assert.Null(typeof(RegistrationForm).GetProperty(name));
        }
        Assert.DoesNotContain(RegistrationFields.All, f => f.Code is "availability" or "equipment");
    }

    // @ADR-f02-import-excel-001: 4 ảnh + 2 mục số + mỡ/cách đo cũ đã gộp
    [Fact]
    public void Merged_google_form_questions_replace_old_fields()
    {
        string[] removed =
        [
            "BodyFatPercent", "BodyFatMethod", "WeightBeforeLiftingKg", "HeightThenCm",
            "PhotoFrontUrl", "PhotoBackUrl", "PhotoLeftUrl", "PhotoRightUrl",
        ];
        foreach (var name in removed)
            Assert.Null(typeof(RegistrationInput).GetProperty(name));

        Assert.Equal("Mỡ cơ thể và cách đo", RegistrationFields.LabelOf("bodyFatAndMethod"));
        Assert.Equal("Cân nặng và chiều cao trước khi tập tạ", RegistrationFields.LabelOf("weightHeightBeforeLifting"));
        Assert.Equal("Link ảnh toàn thân", RegistrationFields.LabelOf("bodyPhotoUrl"));
    }

    // @BR-f02-import-excel-003
    [Theory]
    [InlineData("1")]
    [InlineData("Khoảng 2 năm rưỡi")]
    [InlineData("1–2 năm")] // giá trị của hồ sơ cũ vẫn giữ nguyên
    public void Training_years_is_free_text_kept_verbatim(string text) =>
        Assert.Equal(text, Create(new RegistrationInput { TrainingYears = text }).TrainingYears);

    // @BR-f02-import-excel-003 (seed ❌ 201 ký tự)
    [Fact]
    public void Training_years_is_at_most_200_characters()
    {
        Assert.Equal(200, Create(new RegistrationInput { TrainingYears = new string('a', 200) }).TrainingYears!.Length);
        Assert.Equal("student.trainingYears.too_long",
            ErrorOf(new RegistrationInput { TrainingYears = new string('a', 201) }));
    }

    // @BR-f02-import-excel-004
    [Theory]
    [InlineData("34")]
    [InlineData("18% đo InBody")]
    public void Body_fat_and_method_is_free_text_kept_verbatim(string text) =>
        Assert.Equal(text, Create(new RegistrationInput { BodyFatAndMethod = text }).BodyFatAndMethod);

    // @BR-f02-import-excel-004 (seed ❌ 201 ký tự)
    [Fact]
    public void Body_fat_and_method_is_at_most_200_characters()
    {
        Assert.Equal(200, Create(new RegistrationInput { BodyFatAndMethod = new string('a', 200) }).BodyFatAndMethod!.Length);
        Assert.Equal("student.bodyFatAndMethod.too_long",
            ErrorOf(new RegistrationInput { BodyFatAndMethod = new string('a', 201) }));
    }

    // @BR-f02-import-excel-005
    [Fact]
    public void Weight_and_height_before_lifting_is_a_paragraph_of_at_most_2000_characters()
    {
        Assert.Equal("60kg, 1m70",
            Create(new RegistrationInput { WeightHeightBeforeLifting = "60kg, 1m70" }).WeightHeightBeforeLifting);
        Assert.Equal(2000,
            Create(new RegistrationInput { WeightHeightBeforeLifting = new string('a', 2000) }).WeightHeightBeforeLifting!.Length);
        Assert.Equal("student.weightHeightBeforeLifting.too_long",
            ErrorOf(new RegistrationInput { WeightHeightBeforeLifting = new string('a', 2001) }));
    }

    // @BR-f02-import-excel-006
    [Theory]
    [InlineData("https://drive.google.com/open?id=abc")]
    [InlineData("http://example.com/anh.jpg")]
    public void Body_photo_url_accepts_web_links(string url) =>
        Assert.Equal(url, Create(new RegistrationInput { BodyPhotoUrl = url }).BodyPhotoUrl);

    // @BR-f02-import-excel-006
    [Theory]
    [InlineData("drive.google.com/abc")]
    [InlineData("ftp://x.vn/a.jpg")]
    [InlineData("javascript:alert(1)")]
    public void Body_photo_url_rejects_non_web_links(string url) =>
        Assert.Equal("student.bodyPhotoUrl.invalid_url", ErrorOf(new RegistrationInput { BodyPhotoUrl = url }));

    // @BR-f02-import-excel-006: tối đa 2048 ký tự
    [Fact]
    public void Body_photo_url_is_at_most_2048_characters()
    {
        var max = "https://x.vn/" + new string('a', 2048 - "https://x.vn/".Length);
        Assert.Equal(max, Create(new RegistrationInput { BodyPhotoUrl = max }).BodyPhotoUrl);
        Assert.Equal("student.bodyPhotoUrl.too_long", ErrorOf(new RegistrationInput { BodyPhotoUrl = max + "a" }));
    }

    // @BR-f02-import-excel-007
    [Fact]
    public void Required_fields_follow_google_form()
    {
        var required = RegistrationFields.All.Where(f => f.Required).Select(f => f.Code).ToHashSet();

        Assert.Contains("trainingYears", required);
        Assert.Contains("weightHeightBeforeLifting", required);
        Assert.Contains("bodyPhotoUrl", required);
        Assert.DoesNotContain("bodyFatAndMethod", required);
        Assert.Contains(RegistrationFields.All, f => f.Code == "bodyFatAndMethod");
        Assert.Equal(24, required.Count);
    }

    // @BR-f02-import-excel-007
    [Fact]
    public void Empty_body_photo_url_is_reported_missing() =>
        Assert.Equal(new[] { "bodyPhotoUrl" },
            Create(RegistrationSamples.Complete() with { BodyPhotoUrl = null }).MissingRequired());

    // @BR-f02-import-excel-007
    [Fact]
    public void Empty_body_fat_and_method_is_not_missing() =>
        Assert.Empty(Create(RegistrationSamples.Complete() with { BodyFatAndMethod = "  " }).MissingRequired());

    // @ADR-f02-import-excel-003: Validate gom mọi lỗi theo thứ tự form, lý do không kèm tên mục
    [Fact]
    public void Validate_collects_every_error_in_form_order()
    {
        var input = new RegistrationInput
        {
            TransferDate = Today.AddDays(1),
            Address = new string('a', 201),
            Age = 5,
            HeightCm = 300m,
            StressLevel = "Cao quá",
            BodyPhotoUrl = "drive.google.com/abc",
        };

        var (form, errors) = RegistrationForm.Validate(input, Today);

        Assert.Null(form);
        Assert.Equal(
            new[]
            {
                new RegistrationFieldError("transferDate", "không được sau hôm nay"),
                new RegistrationFieldError("address", "tối đa 200 ký tự"),
                new RegistrationFieldError("age", "phải từ 10 đến 100"),
                new RegistrationFieldError("heightCm", "phải từ 100 đến 250"),
                new RegistrationFieldError("stressLevel", "không thuộc danh sách"),
                new RegistrationFieldError("bodyPhotoUrl", "link ảnh không hợp lệ"),
            },
            errors);
    }

    // @ADR-f02-import-excel-003
    [Fact]
    public void Validate_returns_the_form_when_everything_is_valid()
    {
        var (form, errors) = RegistrationForm.Validate(RegistrationSamples.Complete(), Today);

        Assert.Empty(errors);
        Assert.NotNull(form);
        Assert.Equal(RegistrationSamples.Complete(), form.ToInput());
    }

    // @ADR-f02-import-excel-003: giá dưới 0 có lý do riêng
    [Fact]
    public void Validate_reports_negative_price()
    {
        var (_, errors) = RegistrationForm.Validate(new RegistrationInput { Price = -1 }, Today);

        Assert.Equal(new[] { new RegistrationFieldError("price", "phải từ 0 đồng") }, errors);
    }

    // @ADR-f02-import-excel-003 @ADR-f02-ho-so-hoc-vien-006: Create vẫn ném lỗi đầu tiên, mã + câu chữ như trước
    [Fact]
    public void Create_still_throws_the_first_error_with_unchanged_code_and_message()
    {
        var ex = Assert.Throws<DomainException>(() => Create(new RegistrationInput { Age = 5, HeightCm = 300m }));
        Assert.Equal("student.age.out_of_range", ex.Code);
        Assert.Equal("Tuổi phải từ 10 đến 100.", ex.Message);

        var option = Assert.Throws<DomainException>(() => Create(new RegistrationInput { StressLevel = "Cao quá" }));
        Assert.Equal("student.stressLevel.invalid_option", option.Code);
        Assert.Equal("Mức độ căng thẳng không có lựa chọn \"Cao quá\".", option.Message);

        var date = Assert.Throws<DomainException>(() => Create(new RegistrationInput { TransferDate = Today.AddDays(1) }));
        Assert.Equal("student.transferDate.in_future", date.Code);
        Assert.Equal("Ngày chuyển khoản không được sau ngày hôm nay.", date.Message);

        var url = Assert.Throws<DomainException>(() => Create(new RegistrationInput { BodyPhotoUrl = "ftp://x.vn/a.jpg" }));
        Assert.Equal("Link ảnh toàn thân phải là địa chỉ web bắt đầu bằng http:// hoặc https://.", url.Message);
    }
}
```

- [x] **Step 2: Sửa Domain test F02 hiện có theo mục mới (bỏ mỡ %, 2 mục số trước khi tập, 4 ảnh; 28 → 24 mục bắt buộc)**

Thay toàn bộ `backend/tests/Coaching.Domain.Tests/Students/RegistrationFormTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.TestSupport;

namespace Coaching.Domain.Tests.Students;

public sealed class RegistrationFormTests
{
    private static readonly DateOnly Today = new(2026, 11, 25);

    private static RegistrationForm Create(RegistrationInput input) => RegistrationForm.Create(input, Today);

    private static string ErrorOf(RegistrationInput input) =>
        Assert.Throws<DomainException>(() => Create(input)).Code;

    // @BR-f02-ho-so-hoc-vien-004
    [Fact]
    public void Complete_form_has_no_missing_required() =>
        Assert.Empty(Create(RegistrationSamples.Complete()).MissingRequired());

    // @BR-f02-ho-so-hoc-vien-004 @BR-f02-import-excel-007
    [Fact]
    public void Missing_required_lists_exactly_the_empty_required_fields_in_form_order()
    {
        var input = RegistrationSamples.Complete() with
        {
            Pack = null, TrainingGoal = "", BodyPhotoUrl = null, BodyFatAndMethod = null, // Mỡ cơ thể và cách đo không bắt buộc
        };

        Assert.Equal(new[] { "pack", "trainingGoal", "bodyPhotoUrl" }, Create(input).MissingRequired());
    }

    // @BR-f02-ho-so-hoc-vien-004 (seed loại khỏi living doc): mục chỉ chứa khoảng trắng tính là thiếu
    [Fact]
    public void Whitespace_only_required_text_counts_as_missing()
    {
        var form = Create(RegistrationSamples.Complete() with { TrainingGoal = "   " });

        Assert.Null(form.TrainingGoal);
        Assert.Equal(new[] { "trainingGoal" }, form.MissingRequired());
    }

    // @BR-f02-ho-so-hoc-vien-004 @BR-f02-import-excel-007
    [Fact]
    public void Empty_form_misses_all_24_required_fields() =>
        Assert.Equal(24, RegistrationForm.CreateEmpty().MissingRequired().Count);

    // @BR-f02-ho-so-hoc-vien-008
    [Theory]
    [InlineData(20)]
    [InlineData(72.5)]
    [InlineData(300)]
    public void Weight_inside_range_is_kept(double kg) =>
        Assert.Equal((decimal)kg, Create(new RegistrationInput { WeightKg = (decimal)kg }).WeightKg);

    // @BR-f02-ho-so-hoc-vien-008
    [Theory]
    [InlineData(19.9)]
    [InlineData(300.1)]
    [InlineData(720)]
    public void Weight_outside_range_is_rejected(double kg) =>
        Assert.Equal("student.weightKg.out_of_range", ErrorOf(new RegistrationInput { WeightKg = (decimal)kg }));

    // @BR-f02-ho-so-hoc-vien-008
    [Theory]
    [InlineData(10, true)]
    [InlineData(100, true)]
    [InlineData(9, false)]
    [InlineData(101, false)]
    public void Age_must_be_between_10_and_100(int age, bool valid)
    {
        var input = new RegistrationInput { Age = age };
        if (valid)
            Assert.Equal(age, Create(input).Age);
        else
            Assert.Equal("student.age.out_of_range", ErrorOf(input));
    }

    // @BR-f02-ho-so-hoc-vien-008
    [Theory]
    [InlineData(100, true)]
    [InlineData(250, true)]
    [InlineData(99.9, false)]
    [InlineData(250.1, false)]
    public void Height_must_be_between_100_and_250(double cm, bool valid)
    {
        var input = new RegistrationInput { HeightCm = (decimal)cm };
        if (valid)
            Assert.Equal((decimal)cm, Create(input).HeightCm);
        else
            Assert.Equal("student.heightCm.out_of_range", ErrorOf(input));
    }

    // @BR-f02-ho-so-hoc-vien-008
    [Fact]
    public void Girths_use_their_range()
    {
        var form = Create(new RegistrationInput { ChestCm = 10m, LeftThighCm = 250m });
        Assert.Equal(10m, form.ChestCm);
        Assert.Equal(250m, form.LeftThighCm);

        Assert.Equal("student.waistCm.out_of_range", ErrorOf(new RegistrationInput { WaistCm = 9.9m }));
        Assert.Equal("student.hipCm.out_of_range", ErrorOf(new RegistrationInput { HipCm = 250.1m }));
    }

    // @BR-f02-ho-so-hoc-vien-008
    [Fact]
    public void Price_starts_at_zero()
    {
        Assert.Equal(0, Create(new RegistrationInput { Price = 0 }).Price);
        Assert.Equal("student.price.out_of_range", ErrorOf(new RegistrationInput { Price = -1 }));
    }

    // @BR-f02-ho-so-hoc-vien-009
    [Fact]
    public void Option_in_list_is_kept_trimmed() =>
        Assert.Equal("4 buổi", Create(new RegistrationInput { SessionsPerWeek = " 4 buổi " }).SessionsPerWeek);

    // @BR-f02-ho-so-hoc-vien-009
    [Fact]
    public void Option_outside_list_is_rejected()
    {
        Assert.Equal("student.sessionsPerWeek.invalid_option", ErrorOf(new RegistrationInput { SessionsPerWeek = "8 buổi" }));
        Assert.Equal("student.stressLevel.invalid_option", ErrorOf(new RegistrationInput { StressLevel = "Stress-free" }));
    }

    // @BR-f02-ho-so-hoc-vien-010
    [Fact]
    public void Transfer_date_cannot_be_after_today()
    {
        Assert.Equal(Today, Create(new RegistrationInput { TransferDate = Today }).TransferDate);
        Assert.Equal("student.transferDate.in_future", ErrorOf(new RegistrationInput { TransferDate = Today.AddDays(1) }));
    }

    // @BR-f02-ho-so-hoc-vien-011
    [Theory]
    [InlineData("https://drive.google.com/open?id=abc")]
    [InlineData("http://example.com/anh.jpg")]
    public void Http_and_https_photo_links_are_kept(string url) =>
        Assert.Equal(url, Create(new RegistrationInput { BodyPhotoUrl = url }).BodyPhotoUrl);

    // @BR-f02-ho-so-hoc-vien-011 (seed "javascript:" loại khỏi living doc)
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("drive.google.com/abc")]
    [InlineData("ftp://example.com/anh.jpg")]
    public void Non_web_photo_links_are_rejected(string url) =>
        Assert.Equal("student.bodyPhotoUrl.invalid_url", ErrorOf(new RegistrationInput { BodyPhotoUrl = url }));

    // @ADR-f02-ho-so-hoc-vien-009
    [Fact]
    public void Text_length_limits_apply()
    {
        Assert.Equal(200, Create(new RegistrationInput { Address = new string('a', 200) }).Address!.Length);
        Assert.Equal("student.address.too_long", ErrorOf(new RegistrationInput { Address = new string('a', 201) }));
        Assert.Equal(2000, Create(new RegistrationInput { TrainingGoal = new string('a', 2000) }).TrainingGoal!.Length);
        Assert.Equal("student.trainingGoal.too_long", ErrorOf(new RegistrationInput { TrainingGoal = new string('a', 2001) }));
        Assert.Equal("student.bodyPhotoUrl.too_long",
            ErrorOf(new RegistrationInput { BodyPhotoUrl = "https://x.vn/" + new string('a', 2048) }));
    }

    // @ADR-f02-ho-so-hoc-vien-002
    [Fact]
    public void ToInput_round_trips_every_field() =>
        Assert.Equal(RegistrationSamples.Complete(), Create(RegistrationSamples.Complete()).ToInput());

    // @BR-f02-ho-so-hoc-vien-004
    [Fact]
    public void Student_exposes_missing_required_of_its_form()
    {
        var at = new DateTimeOffset(2026, 11, 25, 2, 0, 0, TimeSpan.Zero);
        var email = Email.Create("lan@gmail.com");

        Assert.Equal(24, Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Lan", email, at).MissingRequiredFields.Count);
        Assert.Empty(Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Lan", email, at,
            Create(RegistrationSamples.Complete())).MissingRequiredFields);
    }

    // @BR-f02-ho-so-hoc-vien-004: thêm mục vào RegistrationFields mà quên ánh xạ giá trị (hoặc ngược lại) sẽ gây KeyNotFoundException
    [Fact]
    public void Every_registration_field_code_has_a_value_mapping_and_vice_versa()
    {
        var method = typeof(RegistrationForm).GetMethod("ValuesByCode",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var mapped = ((IDictionary<string, object?>)method.Invoke(RegistrationForm.CreateEmpty(), null)!).Keys.ToHashSet();

        Assert.Equal(RegistrationFields.All.Select(f => f.Code).OrderBy(c => c), mapped.OrderBy(c => c));
        Assert.Equal(
            RegistrationFields.All.Where(f => f.Required).Select(f => f.Code),
            RegistrationForm.CreateEmpty().MissingRequired());
    }
}
```

- [x] **Step 3: Sửa fixture `RegistrationSamples` theo khoá mới**

Thay toàn bộ `backend/tests/Coaching.TestSupport/RegistrationSamples.cs`:
```csharp
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
```

- [x] **Step 4: Viết integration test — Lịch rảnh/Dụng cụ gửi lên không được ghi nhận (seed ❌ của BR-001)**

Trong `backend/tests/Coaching.Api.IntegrationTests/Students/RegistrationValidationTests.cs`, thêm using đầu file:
```csharp
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
```
và thêm test cuối class:
```csharp
    // @BR-f02-import-excel-001 (seed ❌ loại khỏi living doc): client cũ gửi Lịch rảnh/Dụng cụ → bỏ qua, không lưu vào cột JSON
    [Fact]
    public async Task Availability_and_equipment_sent_by_old_clients_are_not_stored()
    {
        var admin = await factory.AdminClientAsync();
        var registration = new Dictionary<string, object?>
        {
            ["availability"] = "Tối 2-4-6",
            ["equipment"] = "Tạ đơn",
            ["trainingYears"] = "1",
        };

        var response = await admin.PostAsJsonAsync(StudentRequests.Url,
            new { fullName = "Lan", email = StudentRequests.NewEmail(), registration });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!.Id;
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var raw = await db.Database
            .SqlQuery<string>($"SELECT [Registration] AS [Value] FROM [Students] WHERE [Id] = {id}")
            .SingleAsync();
        Assert.DoesNotContain("Availability", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Equipment", raw, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TrainingYears", raw, StringComparison.OrdinalIgnoreCase);
    }
```

- [x] **Step 5: Chạy — kỳ vọng FAIL (đỏ)**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students.Registration"`
Expected: FAIL — build error, ví dụ `'RegistrationInput' does not contain a definition for 'BodyFatAndMethod'`, `'RegistrationInput' does not contain a definition for 'BodyPhotoUrl'`, `The type or namespace name 'RegistrationFieldError' could not be found`, `'RegistrationForm' does not contain a definition for 'Validate'`.

- [x] **Step 6: Domain — `RegistrationFieldError`**

`backend/src/Coaching.Domain/Students/RegistrationFieldError.cs`:
```csharp
namespace Coaching.Domain.Students;

/// <summary>
/// @ADR-f02-import-excel-003: một mục sai của form đăng ký.
/// <paramref name="Code"/> = mã mục (vd "age"); <paramref name="Message"/> = lý do tiếng Việt KHÔNG kèm tên mục
/// (vd "phải từ 10 đến 100") — import ghép với tên cột trong file.
/// </summary>
public sealed record RegistrationFieldError(string Code, string Message);
```

- [x] **Step 7: Domain — `RegistrationInput` theo câu hỏi Google Form**

Thay toàn bộ `backend/src/Coaching.Domain/Students/RegistrationInput.cs`:
```csharp
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
```

- [x] **Step 8: Domain — `RegistrationFields` mới (37 mục, 24 bắt buộc)**

Thay toàn bộ `backend/src/Coaching.Domain/Students/RegistrationFields.cs`:
```csharp
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
```

- [x] **Step 9: Domain — `RegistrationForm` mới: mục Google Form + `Validate` gom lỗi, `Create` ném lỗi đầu**

Thay toàn bộ `backend/src/Coaching.Domain/Students/RegistrationForm.cs`:
```csharp
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
```

- [x] **Step 10: Chạy Domain test — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students"`
Expected: PASS — toàn bộ test trong `Coaching.Domain.Tests.Students` (gồm `RegistrationGoogleFormTests`, `RegistrationFormTests`, `ActivationTests`, `SendCredentialsTests`…) `Passed`, `Failed: 0`.

- [x] **Step 11: Acceptance — nhãn mục mới trong `RegistrationLabels`**

Thay toàn bộ `backend/tests/Coaching.Acceptance/Support/RegistrationLabels.cs`:
```csharp
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Coaching.Domain.Students;

namespace Coaching.Acceptance.Support;

/// <summary>Tên mục trong living doc → mã mục JSON của `registration` (@ADR-f02-import-excel-001: mục theo Google Form).</summary>
public static class RegistrationLabels
{
    public static readonly IReadOnlyDictionary<string, string> Codes = new Dictionary<string, string>
    {
        ["Gói đã mua"] = "pack",
        ["Giá"] = "price",
        ["Tuổi"] = "age",
        ["Chiều cao"] = "heightCm",
        ["Cân nặng"] = "weightKg",
        ["Mục tiêu tập luyện"] = "trainingGoal",
        ["Số buổi tập mỗi tuần"] = "sessionsPerWeek",
        ["Số năm tập luyện"] = "trainingYears", // @BR-f02-import-excel-003
        ["Mỡ cơ thể và cách đo"] = "bodyFatAndMethod", // @BR-f02-import-excel-004
        ["Cân nặng và chiều cao trước khi tập tạ"] = "weightHeightBeforeLifting", // @BR-f02-import-excel-005
        ["Link ảnh toàn thân"] = "bodyPhotoUrl", // @BR-f02-import-excel-006
    };

    private static readonly HashSet<string> NumericCodes = ["price", "age", "heightCm", "weightKg"];

    /// <summary>Đặt mục <paramref name="code"/> = <paramref name="text"/> (null = để trống). Số nhận dấu phẩy thập phân.</summary>
    public static RegistrationInput With(RegistrationInput input, string code, string? text)
    {
        var json = JsonSerializer.SerializeToNode(input, JsonSerializerOptions.Web)!.AsObject();
        json[code] = text is null ? null
            : NumericCodes.Contains(code) ? JsonValue.Create(ParseNumber(text))
            : JsonValue.Create(text);
        return json.Deserialize<RegistrationInput>(JsonSerializerOptions.Web)!;
    }

    public static void AssertValue(RegistrationInput registration, string code, string expected)
    {
        var node = JsonSerializer.SerializeToNode(registration, JsonSerializerOptions.Web)![code];
        Assert.NotNull(node);
        if (NumericCodes.Contains(code))
            Assert.Equal(ParseNumber(expected), node!.GetValue<decimal>());
        else
            Assert.Equal(expected, node!.GetValue<string>());
    }

    public static decimal ParseNumber(string text) =>
        decimal.Parse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture);
}
```

- [x] **Step 12: Acceptance — bind `form-dang-ky.feature` + sửa step F02 bị đổi**

Thay toàn bộ `backend/tests/Coaching.Acceptance/Steps/StudentRegistrationSteps.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Acceptance.Support;
using Coaching.Application.Students;
using Coaching.Domain.Students;
using Coaching.TestSupport;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class StudentRegistrationSteps(AcceptanceContext ctx, StudentApi api)
{
    /// <summary>@BR-f02-import-excel-001: mục đã bỏ khỏi form → khoá JSON cũ.</summary>
    private static readonly IReadOnlyDictionary<string, string> RemovedFields = new Dictionary<string, string>
    {
        ["Lịch rảnh"] = "availability",
        ["Dụng cụ"] = "equipment",
    };

    private Guid _createdId;

    // @BR-f02-ho-so-hoc-vien-004 @BR-f02-import-excel-007
    [When(@"^coach lưu hồ sơ học viên đủ mọi mục bắt buộc trừ (.+)$")]
    public Task SaveCompleteExcept(string labels)
    {
        var input = RegistrationSamples.Complete();
        foreach (var label in StepText.Quoted(labels))
            input = RegistrationLabels.With(input, RegistrationLabels.Codes[label], null);
        return api.TryCreateAsync("Học viên đăng ký", StudentApi.NewEmail(), input);
    }

    [When("coach lưu hồ sơ học viên đủ mọi mục bắt buộc")]
    public Task SaveComplete() =>
        api.TryCreateAsync("Học viên đăng ký", StudentApi.NewEmail(), RegistrationSamples.Complete());

    // @BR-f02-import-excel-007: để trống một mục không bắt buộc
    [When(@"^coach lưu hồ sơ học viên đủ mọi mục bắt buộc và để trống ""([^""]*)""$")]
    public Task SaveCompleteWithEmpty(string label) =>
        api.TryCreateAsync("Học viên đăng ký", StudentApi.NewEmail(),
            RegistrationLabels.With(RegistrationSamples.Complete(), RegistrationLabels.Codes[label], null));

    // @BR-f02-import-excel-001: gửi kèm Lịch rảnh/Dụng cụ như client cũ (ví dụ ❌ của spec) — phải bị bỏ qua
    [When("coach tạo hồ sơ học viên đủ mọi mục bắt buộc")]
    public async Task CreateCompleteWithLegacyFields()
    {
        var registration = JsonSerializer.SerializeToNode(RegistrationSamples.Complete(), JsonSerializerOptions.Web)!.AsObject();
        registration["availability"] = "Tối 2-4-6";
        registration["equipment"] = "Tạ đơn";

        var response = await ctx.SendJsonAsync(HttpMethod.Post, "/api/admin/students",
            new { fullName = "Học viên form mới", email = StudentApi.NewEmail(), registration }, api.AdminToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _createdId = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!.Id;
    }

    // @BR-f02-import-excel-001: đọc lại hồ sơ (JSON thô) — không có khoá của 2 mục đã bỏ
    [Then(@"^hồ sơ không có mục ""([^""]*)"" và ""([^""]*)""$")]
    public async Task HasNoFields(string first, string second)
    {
        var response = await ctx.GetAsync($"/api/admin/students/{_createdId}", api.AdminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var registration = json.RootElement.GetProperty("registration");

        foreach (var label in new[] { first, second })
        {
            Assert.False(registration.TryGetProperty(RemovedFields[label], out _), $"Hồ sơ vẫn có mục \"{label}\".");
            Assert.DoesNotContain(RegistrationFields.All, f => f.Label == label);
        }
    }

    [Then("hồ sơ được lưu")]
    public void Saved() => Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);

    [Then(@"^hồ sơ cho biết còn thiếu đúng (\d+) mục: (.+)$")]
    public void MissingExactly(int count, string labels)
    {
        var expected = StepText.Quoted(labels).Select(l => RegistrationLabels.Codes[l]).Order().ToList();
        Assert.Equal(count, expected.Count);
        Assert.Equal(expected, api.LastDetail!.MissingRequired.Order().ToList());
    }

    [Then("hồ sơ cho biết không còn mục bắt buộc nào thiếu")]
    public void NothingMissing()
    {
        Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);
        Assert.Empty(api.LastDetail!.MissingRequired);
    }

    // @BR-f02-ho-so-hoc-vien-008 @BR-f02-ho-so-hoc-vien-009 @BR-f02-import-excel-003 @BR-f02-import-excel-004
    // @BR-f02-import-excel-005 @BR-f02-import-excel-006
    [When(@"^coach lưu hồ sơ học viên có ""([^""]*)"" là ""([^""]*)""$")]
    public Task SaveWithValue(string label, string value) =>
        api.TryCreateAsync("Học viên số liệu", StudentApi.NewEmail(),
            RegistrationLabels.With(new RegistrationInput(), RegistrationLabels.Codes[label], value));

    [Then(@"^hồ sơ ghi nhận ""([^""]*)"" là ""([^""]*)""$")]
    public void Recorded(string label, string value)
    {
        Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);
        RegistrationLabels.AssertValue(api.LastDetail!.Registration, RegistrationLabels.Codes[label], value);
    }

    [Then(@"^hồ sơ không được lưu vì ""([^""]*)"" ngoài khoảng cho phép$")]
    public Task OutOfRange(string label) =>
        api.AssertErrorAsync(HttpStatusCode.BadRequest, $"student.{RegistrationLabels.Codes[label]}.out_of_range");

    // @BR-f02-ho-so-hoc-vien-010 (dùng lại ở BR-014): "hôm nay" = 09:00 giờ VN của ngày đó
    [Given(@"^hôm nay là ""([^""]*)"" theo giờ Việt Nam$")]
    public Task TodayIs(string date) => api.MoveClockToAsync(StepText.VietnamTime(date, 9, 0));

    [Given(@"^thời điểm hiện tại là (\d\d):(\d\d) ngày ""([^""]*)"" giờ UTC$")]
    public Task UtcNowIs(int hour, int minute, string date)
    {
        var day = StepText.ParseDate(date);
        return api.MoveClockToAsync(new DateTimeOffset(day.Year, day.Month, day.Day, hour, minute, 0, TimeSpan.Zero));
    }

    [When(@"^coach lưu hồ sơ học viên có ngày chuyển khoản ""([^""]*)""$")]
    public Task SaveTransferDate(string date) =>
        api.TryCreateAsync("Học viên chuyển khoản", StudentApi.NewEmail(),
            new RegistrationInput { TransferDate = StepText.ParseDate(date) });

    [Then(@"^hồ sơ ghi nhận ngày chuyển khoản ""([^""]*)""$")]
    public void TransferDateRecorded(string date)
    {
        Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);
        Assert.Equal(StepText.ParseDate(date), api.LastDetail!.Registration.TransferDate);
    }

    [Then("hồ sơ không được lưu vì ngày chuyển khoản sau hôm nay")]
    public Task TransferDateInFuture() =>
        api.AssertErrorAsync(HttpStatusCode.BadRequest, "student.transferDate.in_future");

    // @BR-f02-ho-so-hoc-vien-011 @BR-f02-import-excel-006: hồ sơ chỉ còn một "Link ảnh toàn thân"
    [When(@"^coach lưu hồ sơ học viên có link ảnh toàn thân ""([^""]*)""$")]
    public Task SaveBodyPhoto(string url) =>
        api.TryCreateAsync("Học viên ảnh", StudentApi.NewEmail(), new RegistrationInput { BodyPhotoUrl = url });

    [Then(@"^hồ sơ ghi nhận link ảnh toàn thân ""([^""]*)""$")]
    public void BodyPhotoRecorded(string url)
    {
        Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);
        Assert.Equal(url, api.LastDetail!.Registration.BodyPhotoUrl);
    }

    // @BR-f02-ho-so-hoc-vien-011 @BR-f02-import-excel-006
    [Then("hồ sơ không được lưu vì link ảnh không hợp lệ")]
    public Task PhotoInvalid() => api.AssertErrorAsync(HttpStatusCode.BadRequest, "student.bodyPhotoUrl.invalid_url");
}
```

- [x] **Step 13: Chạy acceptance `form-dang-ky.feature` + F02 bị ảnh hưởng — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "Category=BR-f02-import-excel-001|Category=BR-f02-import-excel-003|Category=BR-f02-import-excel-004|Category=BR-f02-import-excel-005|Category=BR-f02-import-excel-006|Category=BR-f02-import-excel-007|Category=BR-f02-ho-so-hoc-vien-004|Category=BR-f02-ho-so-hoc-vien-008|Category=BR-f02-ho-so-hoc-vien-009|Category=BR-f02-ho-so-hoc-vien-011"`
Expected: PASS — 11 scenario/example của `form-dang-ky.feature` (BR-001 ×1, BR-003 ×2, BR-004 ×2, BR-005 ×1, BR-006 ×3, BR-007 ×2) và các scenario BR-f02-ho-so-hoc-vien-004/008/009/011 đều `Passed`, `Failed: 0`. (Không có `No matching step definition found`.)

- [x] **Step 14: Chạy integration test hồ sơ — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Students|FullyQualifiedName~Coaching.Api.IntegrationTests.Accounts"`
Expected: PASS — gồm `RegistrationValidationTests.Availability_and_equipment_sent_by_old_clients_are_not_stored` và `Complete_registration_round_trips_through_json_column` (RegistrationSamples mới), `Failed: 0`.

- [x] **Step 15: Verify tổng backend**

Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: Build thành công (0 warning, TreatWarningsAsErrors). Mọi test Domain/Application/Integration và acceptance F00/F01-P1/F02 + `form-dang-ky.feature` `Passed`. Chỉ được phép fail các scenario `@api` của `features/hoc-vien/ho-so/import-excel.feature` (BR-f02-import-excel-012…034) với lỗi `No matching step definition found` (interim rule — bind ở Task 4/5). Không có lỗi nào khác.

- [x] **Step 16: Commit**

```bash
git add backend/src/Coaching.Domain/Students/RegistrationFieldError.cs backend/src/Coaching.Domain/Students/RegistrationForm.cs backend/src/Coaching.Domain/Students/RegistrationInput.cs backend/src/Coaching.Domain/Students/RegistrationFields.cs backend/tests/Coaching.TestSupport/RegistrationSamples.cs backend/tests/Coaching.Domain.Tests/Students/RegistrationGoogleFormTests.cs backend/tests/Coaching.Domain.Tests/Students/RegistrationFormTests.cs backend/tests/Coaching.Api.IntegrationTests/Students/RegistrationValidationTests.cs backend/tests/Coaching.Acceptance/Support/RegistrationLabels.cs backend/tests/Coaching.Acceptance/Steps/StudentRegistrationSteps.cs
git commit -m "$(cat <<'EOF'
feat(students): registration form follows Google Form questions, Validate collects all errors (BR-f02-import-excel-001, 003..007)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
)"
```

---

### Task 2: Chuyển dữ liệu hồ sơ cũ — Liquibase v0.4.0 + integration test

**Files:**
- Create: `database/changelog/v0.4.0/changelog.xml`, `database/changelog/v0.4.0/0001-registration-google-form.xml`, `database/changelog/v0.4.0/scripts/0001-registration-google-form.sql`, `database/changelog/v0.4.0/rollback/0001-registration-google-form.rollback.sql`
- Modify: `database/changelog/db.changelog-master.xml`
- Modify: `backend/tests/Coaching.TestSupport/DatabaseFixture.cs` (mở `FindChangelogDirectory` thành `public`)
- Test: `backend/tests/Coaching.Api.IntegrationTests/Students/RegistrationMigrationTests.cs`

**Interfaces:**
- Consumes: Task 1 (`RegistrationInput.BodyFatAndMethod/WeightHeightBeforeLifting/BodyPhotoUrl/TrainingYears` trong `StudentDetailDto.Registration`); cột `Students.Registration` (nvarchar(max) JSON, khoá = tên property C# vì `OwnsOne(...).ToJson("Registration")` không đặt `HasJsonPropertyName`), `Students.InternalNote`, `Students.InternalNoteUpdatedAt`, `Students.Version` (rowversion); `DatabaseFixture` (template DB đã chạy Liquibase rồi backup → restore cho mỗi lần chạy: khi Liquibase chạy, bảng `Students` còn trống nên changeset không chạm dòng nào — test phải seed JSON cũ rồi tự chạy lại script); `ApiFactory`, `StudentRequests.CreateAsync/Url`.
- Produces:
  - DB: changeset `v0.4.0-0001-registration-google-form` + tag `v0.4.0`. Với mỗi `Students` còn ít nhất 1 khoá cũ (`Availability`, `Equipment`, `BodyFatPercent`, `BodyFatMethod`, `WeightBeforeLiftingKg`, `HeightThenCm`, `PhotoFrontUrl`, `PhotoBackUrl`, `PhotoLeftUrl`, `PhotoRightUrl`): ghi `BodyFatAndMethod` / `WeightHeightBeforeLifting` / `BodyPhotoUrl`, nối link ảnh thừa vào `InternalNote` (+ `InternalNoteUpdatedAt = SYSDATETIMEOFFSET()` chỉ khi ghi chú đổi), xoá mọi khoá cũ. Chạy lại không đổi gì. Rollback: không làm gì (chỉ `PRINT`).
  - Test support: `public static string DatabaseFixture.FindChangelogDirectory()`.

**Owns:** `@BR-f02-import-excel-002`, `@BR-f02-import-excel-008`, `@BR-f02-import-excel-009`, `@BR-f02-import-excel-010`, `@BR-f02-import-excel-011` | `@ADR-f02-import-excel-002`

- [x] **Step 1: Mở `DatabaseFixture.FindChangelogDirectory` cho test dùng lại**

Trong `backend/tests/Coaching.TestSupport/DatabaseFixture.cs`, đổi:
```csharp
    private static string FindChangelogDirectory()
```
thành:
```csharp
    /// <summary>Thư mục `database/changelog` của repo (tìm ngược từ thư mục chạy test).</summary>
    public static string FindChangelogDirectory()
```

- [x] **Step 2: Viết integration test — seed JSON cũ, chạy script v0.4.0, so kết quả + chạy lại không đổi**

`backend/tests/Coaching.Api.IntegrationTests/Students/RegistrationMigrationTests.cs`:
```csharp
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Coaching.Application.Students;
using Coaching.Infrastructure.Persistence;
using Coaching.TestSupport;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

/// <summary>
/// @ADR-f02-import-excel-002: changeset Liquibase v0.4.0 chuyển `Students.Registration` cũ sang mục Google Form.
/// Template DB chạy Liquibase khi bảng Students còn trống → test seed JSON cũ (khoá = tên property C#, như EF ghi)
/// rồi chạy lại đúng file script của changeset.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class RegistrationMigrationTests(ApiFactory factory)
{
    private const string ChangeSetId = "v0.4.0-0001-registration-google-form";

    private static readonly DateTimeOffset SeededNoteAt = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private const string OldKeys =
        "N'Availability', N'Equipment', N'BodyFatPercent', N'BodyFatMethod', N'WeightBeforeLiftingKg', " +
        "N'HeightThenCm', N'PhotoFrontUrl', N'PhotoBackUrl', N'PhotoLeftUrl', N'PhotoRightUrl'";

    // @ADR-f02-import-excel-002: Liquibase đã chạy changeset v0.4.0 khi dựng template DB
    [Fact]
    public async Task Changeset_v0_4_0_is_applied_by_liquibase() =>
        Assert.Equal(1, await ScalarAsync<int>("SELECT COUNT(*) FROM [DATABASECHANGELOG] WHERE [ID] = @id",
            new SqlParameter("@id", ChangeSetId)));

    // @ADR-f02-import-excel-002: tiền đề của script — EF ghi khoá JSON bằng tên property C# (PascalCase)
    [Fact]
    public async Task Ef_writes_registration_json_keys_as_property_names()
    {
        var admin = await factory.AdminClientAsync();
        var response = await admin.PostAsJsonAsync(StudentRequests.Url, new
        {
            fullName = "Lan",
            email = StudentRequests.NewEmail(),
            registration = new { bodyPhotoUrl = "https://x.vn/new" },
        });
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!.Id;

        var raw = await ScalarAsync<string>("SELECT [Registration] FROM [Students] WHERE [Id] = @id", new SqlParameter("@id", id));

        Assert.Contains("\"BodyPhotoUrl\":", raw);
    }

    // @BR-f02-import-excel-002 @BR-f02-import-excel-008 @BR-f02-import-excel-009 @BR-f02-import-excel-010 @BR-f02-import-excel-011
    [Fact]
    public async Task Old_registration_is_moved_to_google_form_fields()
    {
        var admin = await factory.AdminClientAsync();
        var full = await SeedOldAsync(admin, """
            {"Pack":"Pack 2","TrainingYears":"1–2 năm","BodyFatPercent":18,"BodyFatMethod":"InBody",
             "WeightBeforeLiftingKg":60,"HeightThenCm":170,"Availability":"Tối 2-4-6","Equipment":"Tạ đơn",
             "PhotoFrontUrl":"https://x.vn/front","PhotoBackUrl":"https://x.vn/back",
             "PhotoLeftUrl":"https://x.vn/left","PhotoRightUrl":"https://x.vn/right"}
            """, note: "Hay đau gối", noteAt: SeededNoteAt);
        var partial = await SeedOldAsync(admin, """
            {"BodyFatPercent":18.5,"WeightBeforeLiftingKg":72.5,"PhotoLeftUrl":"https://x.vn/left"}
            """);
        var methodOnly = await SeedOldAsync(admin, """
            {"BodyFatMethod":"InBody","HeightThenCm":170,"PhotoFrontUrl":"https://x.vn/front","Availability":null}
            """, note: "Ghi chú cũ", noteAt: SeededNoteAt);
        var emptyOld = await SeedOldAsync(admin, """
            {"Pack":null,"BodyFatPercent":null,"BodyFatMethod":null,"WeightBeforeLiftingKg":null,"HeightThenCm":null,
             "Availability":null,"Equipment":null,"PhotoFrontUrl":null,"PhotoBackUrl":null,"PhotoLeftUrl":null,"PhotoRightUrl":null}
            """);

        await RunScriptAsync();

        // Có đủ dữ liệu cũ
        var a = await DetailAsync(admin, full);
        Assert.Equal("18 % – InBody", a.Registration.BodyFatAndMethod); // @BR-f02-import-excel-008
        Assert.Equal("60 kg, 170 cm", a.Registration.WeightHeightBeforeLifting); // @BR-f02-import-excel-009
        Assert.Equal("https://x.vn/front", a.Registration.BodyPhotoUrl); // @BR-f02-import-excel-010: lấy link ảnh trước
        Assert.Equal("1–2 năm", a.Registration.TrainingYears); // @BR-f02-import-excel-003: hồ sơ cũ giữ nguyên
        Assert.Equal("Pack 2", a.Registration.Pack);
        Assert.Equal( // @BR-f02-import-excel-011: link thừa nối sau ghi chú đang có, mỗi link một dòng
            "Hay đau gối\nẢnh mặt sau: https://x.vn/back\nẢnh bên trái: https://x.vn/left\nẢnh bên phải: https://x.vn/right",
            a.InternalNote);
        Assert.NotNull(a.InternalNoteUpdatedAt);
        Assert.NotEqual(SeededNoteAt, a.InternalNoteUpdatedAt);
        Assert.Equal(0, await OldKeysLeftAsync(full)); // @BR-f02-import-excel-002: Lịch rảnh, Dụng cụ (và khoá đã gộp) bị xoá

        // Chỉ có mỡ, chỉ có cân nặng, chỉ có link ảnh trái; số thập phân kiểu VN
        var b = await DetailAsync(admin, partial);
        Assert.Equal("18,5 %", b.Registration.BodyFatAndMethod); // @BR-f02-import-excel-008
        Assert.Equal("72,5 kg", b.Registration.WeightHeightBeforeLifting); // @BR-f02-import-excel-009
        Assert.Equal("https://x.vn/left", b.Registration.BodyPhotoUrl); // @BR-f02-import-excel-010
        Assert.Null(b.InternalNote); // @BR-f02-import-excel-011: 1 link → ghi chú không đổi
        Assert.Null(b.InternalNoteUpdatedAt);

        // Chỉ có cách đo, chỉ có chiều cao, 1 link
        var c = await DetailAsync(admin, methodOnly);
        Assert.Equal("InBody", c.Registration.BodyFatAndMethod); // @BR-f02-import-excel-008
        Assert.Equal("170 cm", c.Registration.WeightHeightBeforeLifting); // @BR-f02-import-excel-009
        Assert.Equal("https://x.vn/front", c.Registration.BodyPhotoUrl);
        Assert.Equal("Ghi chú cũ", c.InternalNote); // @BR-f02-import-excel-011
        Assert.Equal(SeededNoteAt, c.InternalNoteUpdatedAt);
        Assert.Equal(0, await OldKeysLeftAsync(methodOnly));

        // Khoá cũ có nhưng đều null (EF ghi null) → mục gộp để trống, khoá cũ bị xoá
        var d = await DetailAsync(admin, emptyOld);
        Assert.Null(d.Registration.BodyFatAndMethod); // @BR-f02-import-excel-008
        Assert.Null(d.Registration.WeightHeightBeforeLifting); // @BR-f02-import-excel-009
        Assert.Null(d.Registration.BodyPhotoUrl); // @BR-f02-import-excel-010
        Assert.Null(d.InternalNote);
        Assert.Equal(0, await OldKeysLeftAsync(emptyOld));
    }

    // @NFR-f02-import-excel-004 @ADR-f02-import-excel-002: chạy lại không đổi dữ liệu đã chuyển; hồ sơ dạng mới không bị chạm
    [Fact]
    public async Task Running_the_script_again_changes_nothing()
    {
        var admin = await factory.AdminClientAsync();
        var old = await SeedOldAsync(admin, """
            {"BodyFatPercent":18,"BodyFatMethod":"InBody","WeightBeforeLiftingKg":60,"HeightThenCm":170,
             "Availability":"Tối 2-4-6","PhotoFrontUrl":"https://x.vn/front","PhotoBackUrl":"https://x.vn/back"}
            """, note: "Hay đau gối", noteAt: SeededNoteAt);
        var created = await admin.PostAsJsonAsync(StudentRequests.Url, new
        {
            fullName = "Học viên dạng mới",
            email = StudentRequests.NewEmail(),
            registration = new { bodyPhotoUrl = "https://x.vn/new", bodyFatAndMethod = "20" },
            internalNote = "Ghi chú mới",
        });
        var fresh = (await created.Content.ReadFromJsonAsync<StudentDetailDto>())!.Id;
        var freshBefore = await SnapshotAsync(fresh);

        await RunScriptAsync();
        var oldAfterFirst = await SnapshotAsync(old);
        await RunScriptAsync();
        await RunScriptAsync();

        Assert.Equal(oldAfterFirst, await SnapshotAsync(old));
        Assert.Equal(freshBefore, await SnapshotAsync(fresh));
        var detail = await DetailAsync(admin, old);
        Assert.Equal("Hay đau gối\nẢnh mặt sau: https://x.vn/back", detail.InternalNote);
        Assert.Equal("18 % – InBody", detail.Registration.BodyFatAndMethod);
    }

    private async Task<Guid> SeedOldAsync(HttpClient admin, string json, string? note = null, DateTimeOffset? noteAt = null)
    {
        var created = await StudentRequests.CreateAsync(admin);
        await ExecAsync(
            "UPDATE [Students] SET [Registration] = @json, [InternalNote] = @note, [InternalNoteUpdatedAt] = @noteAt WHERE [Id] = @id",
            new SqlParameter("@json", json),
            new SqlParameter("@note", (object?)note ?? DBNull.Value),
            new SqlParameter("@noteAt", (object?)noteAt ?? DBNull.Value),
            new SqlParameter("@id", created.Id));
        return created.Id;
    }

    private static async Task<StudentDetailDto> DetailAsync(HttpClient admin, Guid id) =>
        (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{id}"))!;

    private Task<int> OldKeysLeftAsync(Guid id) => ScalarAsync<int>(
        "SELECT COUNT(*) FROM [Students] AS s CROSS APPLY OPENJSON(s.[Registration]) AS j " +
        $"WHERE s.[Id] = @id AND j.[key] IN ({OldKeys})",
        new SqlParameter("@id", id));

    private Task<string> SnapshotAsync(Guid id) => ScalarAsync<string>(
        "SELECT CONCAT([Registration], N'|', [InternalNote], N'|', CONVERT(nvarchar(40), [InternalNoteUpdatedAt], 127), " +
        "N'|', CONVERT(nvarchar(20), CAST([Version] AS bigint))) FROM [Students] WHERE [Id] = @id",
        new SqlParameter("@id", id));

    /// <summary>Chạy đúng file script của changeset, tách batch theo dòng `GO` như Liquibase (endDelimiter "\nGO").</summary>
    private async Task RunScriptAsync()
    {
        var path = Path.Combine(DatabaseFixture.FindChangelogDirectory(),
            "v0.4.0", "scripts", "0001-registration-google-form.sql");
        var sql = await File.ReadAllTextAsync(path);
        foreach (var batch in Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline).Where(b => !string.IsNullOrWhiteSpace(b)))
            await ExecAsync(batch);
    }

    private string ConnectionString()
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.GetConnectionString()!;
    }

    private async Task ExecAsync(string sql, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(ConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<T> ScalarAsync<T>(string sql, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(ConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
        command.Parameters.AddRange(parameters);
        return (T)(await command.ExecuteScalarAsync())!;
    }
}
```

- [x] **Step 3: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~RegistrationMigrationTests"`
Expected: FAIL — `Changeset_v0_4_0_is_applied_by_liquibase` (`Assert.Equal() Failure: Expected: 1 Actual: 0`); `Old_registration_is_moved_to_google_form_fields` và `Running_the_script_again_changes_nothing` lỗi `System.IO.DirectoryNotFoundException`/`FileNotFoundException` cho `v0.4.0/scripts/0001-registration-google-form.sql`. `Ef_writes_registration_json_keys_as_property_names` PASS (xác nhận khoá PascalCase).

- [x] **Step 4: Liquibase v0.4.0 — script chuyển dữ liệu, rollback, changeset, changelog, master**

`database/changelog/v0.4.0/scripts/0001-registration-google-form.sql`:
```sql
-- v0.4.0-0001: form hồ sơ theo câu hỏi Google Form — chuyển Students.Registration cũ sang mục mới (F02 import Excel)
-- @ADR-f02-import-excel-002: chỉ chạm hồ sơ còn khoá cũ → chạy lại không đổi gì (@NFR-f02-import-excel-004).
-- Khoá JSON = tên property C# (EF Core ToJson). Số thập phân ghi kiểu Việt Nam (72,5). NULL trong JSON_MODIFY = không ghi khoá.
UPDATE s
SET [Registration] = JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(c.[Cleaned],
        '$.BodyFatAndMethod', v.[BodyFatAndMethod]),
        '$.WeightHeightBeforeLifting', v.[WeightHeightBeforeLifting]),
        '$.BodyPhotoUrl', v.[BodyPhotoUrl]),
    -- @BR-f02-import-excel-011: link ảnh thừa nối vào cuối ghi chú nội bộ, mỗi link một dòng
    [InternalNote] = CASE
        WHEN v.[PhotoNote] IS NULL THEN s.[InternalNote]
        WHEN NULLIF(s.[InternalNote], N'') IS NULL THEN v.[PhotoNote]
        ELSE s.[InternalNote] + NCHAR(10) + v.[PhotoNote]
    END,
    [InternalNoteUpdatedAt] = CASE WHEN v.[PhotoNote] IS NULL THEN s.[InternalNoteUpdatedAt] ELSE SYSDATETIMEOFFSET() END
FROM [Students] AS s
CROSS APPLY (SELECT
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.BodyFatPercent'))), N'') AS [Fat],
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.BodyFatMethod'))), N'') AS [FatMethod],
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.WeightBeforeLiftingKg'))), N'') AS [WeightThen],
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.HeightThenCm'))), N'') AS [HeightThen],
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.PhotoFrontUrl'))), N'') AS [Front],
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.PhotoBackUrl'))), N'') AS [Back],
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.PhotoLeftUrl'))), N'') AS [Left],
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.PhotoRightUrl'))), N'') AS [Right]
) AS o
CROSS APPLY (SELECT
    -- 18 → "18", 72.5 → "72,5"
    REPLACE(CONVERT(nvarchar(40), CAST(TRY_CAST(o.[Fat] AS decimal(18, 4)) AS float)), N'.', N',') + N' %' AS [FatText],
    REPLACE(CONVERT(nvarchar(40), CAST(TRY_CAST(o.[WeightThen] AS decimal(18, 4)) AS float)), N'.', N',') + N' kg' AS [WeightText],
    REPLACE(CONVERT(nvarchar(40), CAST(TRY_CAST(o.[HeightThen] AS decimal(18, 4)) AS float)), N'.', N',') + N' cm' AS [HeightText]
) AS n
CROSS APPLY (SELECT
    -- @BR-f02-import-excel-008: "<mỡ> % – <cách đo>", bỏ phần trống
    CASE WHEN n.[FatText] IS NOT NULL AND o.[FatMethod] IS NOT NULL THEN n.[FatText] + N' – ' + o.[FatMethod]
         ELSE COALESCE(n.[FatText], o.[FatMethod]) END AS [BodyFatAndMethod],
    -- @BR-f02-import-excel-009: "<cân nặng> kg, <chiều cao> cm", bỏ phần trống
    CASE WHEN n.[WeightText] IS NOT NULL AND n.[HeightText] IS NOT NULL THEN n.[WeightText] + N', ' + n.[HeightText]
         ELSE COALESCE(n.[WeightText], n.[HeightText]) END AS [WeightHeightBeforeLifting],
    -- @BR-f02-import-excel-010: link đầu tiên có giá trị theo thứ tự trước, sau, trái, phải
    COALESCE(o.[Front], o.[Back], o.[Left], o.[Right]) AS [BodyPhotoUrl],
    -- @BR-f02-import-excel-011: các link không được chọn, mỗi link một dòng ghi rõ tên ảnh
    NULLIF(CONCAT_WS(NCHAR(10),
        CASE WHEN o.[Back] IS NOT NULL AND o.[Front] IS NOT NULL
             THEN N'Ảnh mặt sau: ' + o.[Back] END,
        CASE WHEN o.[Left] IS NOT NULL AND COALESCE(o.[Front], o.[Back]) IS NOT NULL
             THEN N'Ảnh bên trái: ' + o.[Left] END,
        CASE WHEN o.[Right] IS NOT NULL AND COALESCE(o.[Front], o.[Back], o.[Left]) IS NOT NULL
             THEN N'Ảnh bên phải: ' + o.[Right] END), N'') AS [PhotoNote]
) AS v
CROSS APPLY (SELECT
    -- @BR-f02-import-excel-002: xoá Lịch rảnh, Dụng cụ; xoá các khoá đã gộp
    JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(
    JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(s.[Registration],
        '$.Availability', NULL), '$.Equipment', NULL),
        '$.BodyFatPercent', NULL), '$.BodyFatMethod', NULL),
        '$.WeightBeforeLiftingKg', NULL), '$.HeightThenCm', NULL),
        '$.PhotoFrontUrl', NULL), '$.PhotoBackUrl', NULL),
        '$.PhotoLeftUrl', NULL), '$.PhotoRightUrl', NULL) AS [Cleaned]
) AS c
WHERE EXISTS (
    SELECT 1 FROM OPENJSON(s.[Registration]) AS j
    WHERE j.[key] IN (N'Availability', N'Equipment', N'BodyFatPercent', N'BodyFatMethod', N'WeightBeforeLiftingKg',
                      N'HeightThenCm', N'PhotoFrontUrl', N'PhotoBackUrl', N'PhotoLeftUrl', N'PhotoRightUrl'));
GO
```

`database/changelog/v0.4.0/rollback/0001-registration-google-form.rollback.sql`:
```sql
-- Rollback v0.4.0-0001: dữ liệu đã gộp/xoá không khôi phục được (@ADR-f02-import-excel-002) — khôi phục từ backup nếu cần.
PRINT N'v0.4.0-0001: rollback không đổi dữ liệu';
GO
```

`database/changelog/v0.4.0/0001-registration-google-form.xml`:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog http://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-4.33.xsd">
  <!-- @ADR-f02-import-excel-002: chuyển dữ liệu Registration cũ sang mục Google Form; chỉ chạm dòng còn khoá cũ -->
  <changeSet id="v0.4.0-0001-registration-google-form" author="duc.hm4@kiotviet.com">
    <comment>F02 import Excel: Registration theo Google Form (BodyFatAndMethod, WeightHeightBeforeLifting, BodyPhotoUrl; xoá Availability, Equipment, khoá cũ; link ảnh thừa vào InternalNote)</comment>
    <sqlFile path="scripts/0001-registration-google-form.sql" relativeToChangelogFile="true" splitStatements="true" endDelimiter="\nGO" stripComments="false"/>
    <rollback>
      <sqlFile path="rollback/0001-registration-google-form.rollback.sql" relativeToChangelogFile="true" splitStatements="true" endDelimiter="\nGO"/>
    </rollback>
  </changeSet>
</databaseChangeLog>
```

`database/changelog/v0.4.0/changelog.xml`:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!-- v0.4.0 = F02 import Excel (form hồ sơ theo Google Form). Thêm changeset mới TRƯỚC changeset tag. -->
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog http://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-4.33.xsd">
  <include file="0001-registration-google-form.xml" relativeToChangelogFile="true"/>

  <changeSet id="v0.4.0-tag" author="duc.hm4@kiotviet.com">
    <tagDatabase tag="v0.4.0"/>
  </changeSet>
</databaseChangeLog>
```

`database/changelog/db.changelog-master.xml` — thêm sau dòng include `v0.3.0`:
```xml
  <include file="v0.4.0/changelog.xml" relativeToChangelogFile="true"/>
```

- [x] **Step 5: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~RegistrationMigrationTests"`
Expected: PASS — 4 test `Passed` (`Changeset_v0_4_0_is_applied_by_liquibase`, `Ef_writes_registration_json_keys_as_property_names`, `Old_registration_is_moved_to_google_form_fields`, `Running_the_script_again_changes_nothing`), `Failed: 0`. (Nếu Liquibase lỗi khi dựng template, lỗi hiện ở `InitializeAsync` dạng `Liquibase update lỗi (exit …)` kèm log — sửa SQL rồi chạy lại.)

- [x] **Step 6: Verify tổng backend**

Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: Mọi test `Passed` trừ scenario `@api` của `features/hoc-vien/ho-so/import-excel.feature` (BR-f02-import-excel-012…034) fail `No matching step definition found` (interim rule). Không lỗi khác.

- [x] **Step 7: Commit**

```bash
git add database/changelog/v0.4.0 database/changelog/db.changelog-master.xml backend/tests/Coaching.TestSupport/DatabaseFixture.cs backend/tests/Coaching.Api.IntegrationTests/Students/RegistrationMigrationTests.cs
git commit -m "$(cat <<'EOF'
feat(db): v0.4.0 migrates old registration data to Google Form fields (BR-f02-import-excel-002, 008..011)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
)"
```

---

### Task 3: Prefactor — `StudentRegistrar` dùng chung (`CreateStudentHandler` không đổi hành vi)

**Files:**
- Create: `backend/src/Coaching.Application/Students/StudentRegistrar.cs`
- Modify: `backend/src/Coaching.Application/Students/Commands/CreateStudent.cs`, `backend/src/Coaching.Application/DependencyInjection.cs`
- Test: `backend/tests/Coaching.Application.Tests/Students/StudentRegistrarTests.cs`

**Interfaces:**
- Consumes: `Student.Create(Guid coachId, Guid userId, string? fullName, Email email, DateTimeOffset at, RegistrationForm? registration)`, `Student.UpdateInternalNote(string?, DateTimeOffset)`, `IStudentRepository.Add`, `IStudentAccounts.CreatePendingAsync(Guid userId, Email email, string fullName, CancellationToken)`, `IUnitOfWork.ExecuteInTransactionAsync<T>/SaveChangesAsync`, `IClock.UtcNow/Today`, `Result<T>`/`Error`, `StudentErrors.EmailTaken`, `CurrentCoach.GetAsync`, `StudentDetailDto.From(Student, Coach, DateTimeOffset)`.
- Produces:
  - `public sealed class StudentRegistrar(IStudentRepository students, IStudentAccounts accounts, IUnitOfWork unitOfWork, IClock clock)` với `public Task<Result<Student>> RegisterAsync(Guid coachId, string? fullName, Email email, RegistrationForm registration, string? internalNote, CancellationToken ct = default)`: tạo `Student` (Chờ kích hoạt, Đang coaching, `CreatedAt = clock.UtcNow`, ghi chú nội bộ nếu có) + tài khoản Chờ kích hoạt trong MỘT transaction; không gửi email. Họ tên/ghi chú sai → `DomainException` ném TRƯỚC khi tạo tài khoản; email trùng → `Result.Failure(StudentErrors.EmailTaken)`, không thêm hồ sơ. Đăng ký DI Scoped.
  - `CreateStudentHandler(CurrentCoach currentCoach, StudentRegistrar registrar, IClock clock)` — hành vi `POST /api/admin/students` không đổi (201 + `StudentDetailDto`, 400/409 như cũ).

**Owns:** —

- [x] **Step 1: Viết Application test cho `StudentRegistrar`**

`backend/tests/Coaching.Application.Tests/Students/StudentRegistrarTests.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Tests.Students;

public sealed class StudentRegistrarTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 2, 0, 0, TimeSpan.Zero);

    private readonly FakeStudents _students = new();
    private readonly FakeAccounts _accounts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private StudentRegistrar Registrar() => new(_students, _accounts, _unitOfWork, new FixedClock(Now));

    // @BR-f02-ho-so-hoc-vien-005 @BR-f02-ho-so-hoc-vien-006 @BR-f02-ho-so-hoc-vien-007 @BR-f02-ho-so-hoc-vien-014
    // @ADR-f02-ho-so-hoc-vien-001: hồ sơ + tài khoản Chờ kích hoạt trong một transaction
    [Fact]
    public async Task Registers_pending_active_student_with_pending_account_in_one_transaction()
    {
        var coachId = Guid.NewGuid();
        var email = Email.Create("lan@gmail.com");

        var result = await Registrar().RegisterAsync(coachId, " Lan ", email, RegistrationForm.CreateEmpty(), "Hay đau gối");

        Assert.True(result.IsSuccess);
        var student = result.Value;
        Assert.Equal(coachId, student.CoachId);
        Assert.Equal("Lan", student.FullName);
        Assert.Equal(email, student.Email);
        Assert.Equal(AccountStatus.PendingActivation, student.AccountStatus);
        Assert.Equal(CoachingStatus.Active, student.CoachingStatus);
        Assert.Equal(Now, student.CreatedAt);
        Assert.Equal("Hay đau gối", student.InternalNote);
        Assert.Equal(Now, student.InternalNoteUpdatedAt);
        Assert.Equal(new[] { (student.UserId, email, "Lan") }, _accounts.Created);
        Assert.Same(student, Assert.Single(_students.Added));
        Assert.Equal(1, _unitOfWork.Transactions);
        Assert.Equal(1, _unitOfWork.Saves);
    }

    // @BR-f02-ho-so-hoc-vien-001: email đã có tài khoản → lỗi, không thêm hồ sơ
    [Fact]
    public async Task Email_taken_returns_error_and_adds_nothing()
    {
        _accounts.Outcome = Result.Failure(StudentErrors.EmailTaken);

        var result = await Registrar().RegisterAsync(Guid.NewGuid(), "Lan", Email.Create("lan@gmail.com"),
            RegistrationForm.CreateEmpty(), null);

        Assert.Equal(StudentErrors.EmailTaken, result.Error);
        Assert.Empty(_students.Added);
        Assert.Equal(0, _unitOfWork.Saves);
    }

    // @BR-f02-ho-so-hoc-vien-003: họ tên trống → DomainException trước khi tạo tài khoản
    [Fact]
    public async Task Invalid_full_name_throws_before_creating_account()
    {
        await Assert.ThrowsAsync<DomainException>(() => Registrar().RegisterAsync(Guid.NewGuid(), "   ",
            Email.Create("lan@gmail.com"), RegistrationForm.CreateEmpty(), null));

        Assert.Empty(_accounts.Created);
        Assert.Equal(0, _unitOfWork.Transactions);
    }

    // @BR-f02-ho-so-hoc-vien-014: không có ghi chú → không có thời điểm sửa ghi chú
    [Fact]
    public async Task No_note_leaves_note_timestamp_empty()
    {
        var result = await Registrar().RegisterAsync(Guid.NewGuid(), "Lan", Email.Create("lan@gmail.com"),
            RegistrationForm.CreateEmpty(), "  ");

        Assert.Null(result.Value.InternalNote);
        Assert.Null(result.Value.InternalNoteUpdatedAt);
    }

    private sealed class FakeStudents : IStudentRepository
    {
        public List<Student> Added { get; } = [];

        public void Add(Student student) => Added.Add(student);

        public Task<Student?> GetAsync(Guid id, Guid coachId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Student?> GetForUpdateAsync(Guid id, Guid coachId, byte[] expectedVersion,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Student?> GetTrackedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StudentSummary>> ListAsync(Guid coachId, string normalizedQuery,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeAccounts : IStudentAccounts
    {
        public Result Outcome { get; set; } = Result.Success();

        public List<(Guid UserId, Email Email, string FullName)> Created { get; } = [];

        public Task<Result> CreatePendingAsync(Guid userId, Email email, string fullName,
            CancellationToken cancellationToken = default)
        {
            Created.Add((userId, email, fullName));
            return Task.FromResult(Outcome);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int Transactions { get; private set; }

        public int Saves { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Saves++;
            return Task.FromResult(1);
        }

        public Task<Result<T>> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<Result<T>>> operation,
            CancellationToken cancellationToken = default)
        {
            Transactions++;
            return operation(cancellationToken);
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        private static readonly TimeSpan Vietnam = TimeSpan.FromHours(7);

        public DateTimeOffset UtcNow => now;

        public DateTimeOffset VietnamNow => now.ToOffset(Vietnam);

        public DateOnly Today => DateOnly.FromDateTime(VietnamNow.DateTime);

        public DateTimeOffset ToVietnamTime(DateTimeOffset instant) => instant.ToOffset(Vietnam);
    }
}
```

- [x] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Application.Tests --filter "FullyQualifiedName~StudentRegistrarTests"`
Expected: FAIL — build error `The type or namespace name 'StudentRegistrar' could not be found`.

- [x] **Step 3: Tạo `StudentRegistrar`**

`backend/src/Coaching.Application/Students/StudentRegistrar.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Common;
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students;

/// <summary>
/// Tạo hồ sơ học viên + tài khoản đăng nhập Chờ kích hoạt — dùng chung cho tạo hồ sơ (F02) và import Excel.
/// Không gửi email. Form đã kiểm trước (RegistrationForm); họ tên/ghi chú sai → DomainException TRƯỚC khi tạo tài khoản.
/// </summary>
public sealed class StudentRegistrar(
    IStudentRepository students,
    IStudentAccounts accounts,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<Student>> RegisterAsync(Guid coachId, string? fullName, Email email,
        RegistrationForm registration, string? internalNote, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        // @BR-f02-ho-so-hoc-vien-005 @BR-f02-ho-so-hoc-vien-006 @BR-f02-ho-so-hoc-vien-007: Chờ kích hoạt, Đang coaching, thuộc coach.
        // Kiểm hồ sơ TRƯỚC khi tạo tài khoản; Id tài khoản sinh sẵn để Student giữ UserId.
        var student = Student.Create(coachId, Guid.CreateVersion7(), fullName, email, now, registration);
        // @BR-f02-ho-so-hoc-vien-014: ghi chú lúc tạo có thời điểm sửa ghi chú = thời điểm tạo; quá 5000 ký tự → 400 trước khi tạo tài khoản
        student.UpdateInternalNote(internalNote, now);

        // @ADR-f02-ho-so-hoc-vien-001: tài khoản + hồ sơ (kèm ghi chú) cùng một transaction
        return await unitOfWork.ExecuteInTransactionAsync<Student>(async token =>
        {
            var account = await accounts.CreatePendingAsync(student.UserId, email, student.FullName, token);
            if (!account.IsSuccess)
                return Result<Student>.Failure(account.Error!); // @BR-f02-ho-so-hoc-vien-001 → 409

            students.Add(student);
            await unitOfWork.SaveChangesAsync(token);
            return Result<Student>.Success(student);
        }, ct);
    }
}
```

- [x] **Step 4: `CreateStudentHandler` dùng `StudentRegistrar` + đăng ký DI**

Thay toàn bộ `backend/src/Coaching.Application/Students/Commands/CreateStudent.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

/// <summary>
/// Không có trường trạng thái: field lạ trong body bị bỏ qua (@BR-f02-ho-so-hoc-vien-005, @BR-f02-ho-so-hoc-vien-006).
/// <paramref name="InternalNote"/> tùy chọn — lưu cùng transaction tạo hồ sơ (@BR-f02-ho-so-hoc-vien-014).
/// </summary>
public sealed record CreateStudentCommand(
    string? FullName, string? Email, RegistrationInput? Registration, string? InternalNote = null);

public sealed class CreateStudentHandler(
    CurrentCoach currentCoach,
    StudentRegistrar registrar,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(CreateStudentCommand command, CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;

        var email = Email.Create(command.Email); // @BR-f02-ho-so-hoc-vien-002 → 400 email.required / email.invalid
        // @ADR-f02-ho-so-hoc-vien-006: luật form ở Domain; @BR-f02-ho-so-hoc-vien-010: "hôm nay" theo giờ VN
        var registration = RegistrationForm.Create(command.Registration ?? new RegistrationInput(), clock.Today);

        // @ADR-f02-ho-so-hoc-vien-001: tài khoản + hồ sơ (kèm ghi chú) cùng một transaction
        var registered = await registrar.RegisterAsync(coach.Id, command.FullName, email, registration,
            command.InternalNote, cancellationToken);
        if (!registered.IsSuccess)
            return registered.Error!; // @BR-f02-ho-so-hoc-vien-001 → 409

        var student = registered.Value;
        return StudentDetailDto.From(student, coach, student.CreatedAt);
    }
}
```

Trong `backend/src/Coaching.Application/DependencyInjection.cs`, thêm using:
```csharp
using Coaching.Application.Students;
```
và thêm ngay trước `services.AddScoped<CreateStudentHandler>();`:
```csharp
        services.AddScoped<StudentRegistrar>();
```

- [x] **Step 5: Chạy Application test — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Application.Tests --filter "FullyQualifiedName~StudentRegistrarTests"`
Expected: PASS — 4 test `Passed`, `Failed: 0`.

- [x] **Step 6: Chạy test hành vi tạo hồ sơ hiện có — kỳ vọng PASS (không đổi hành vi)**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~CreateStudentTests|FullyQualifiedName~InternalNoteTests|FullyQualifiedName~CoachIsolationTests|FullyQualifiedName~RegistrationValidationTests"`
Expected: PASS — gồm `Account_is_rolled_back_when_saving_student_fails`, `Full_name_longer_than_100_is_rejected`, `Statuses_sent_in_body_are_ignored`; `Failed: 0`.

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f02-ho-so-hoc-vien-001|Category=BR-f02-ho-so-hoc-vien-002|Category=BR-f02-ho-so-hoc-vien-003|Category=BR-f02-ho-so-hoc-vien-005|Category=BR-f02-ho-so-hoc-vien-006|Category=BR-f02-ho-so-hoc-vien-007|Category=BR-f02-ho-so-hoc-vien-014)&Category!=web"`
Expected: PASS — mọi scenario `@api` của các BR trên (tạo hồ sơ, email trùng → 409, họ tên trống → 400, Chờ kích hoạt/Đang coaching, thuộc coach, ghi chú lúc tạo) `Passed`, `Failed: 0`.

- [x] **Step 7: Verify tổng backend**

Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: Mọi test `Passed` trừ scenario `@api` của `features/hoc-vien/ho-so/import-excel.feature` (BR-f02-import-excel-012…034) fail `No matching step definition found` (interim rule). Số test pass/fail giống hệt sau Task 2 cộng 4 test `StudentRegistrarTests`.

- [x] **Step 8: Commit**

```bash
git add backend/src/Coaching.Application/Students/StudentRegistrar.cs backend/src/Coaching.Application/Students/Commands/CreateStudent.cs backend/src/Coaching.Application/DependencyInjection.cs backend/tests/Coaching.Application.Tests/Students/StudentRegistrarTests.cs
git commit -m "$(cat <<'EOF'
refactor(students): extract StudentRegistrar from CreateStudentHandler (no behaviour change)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
)"
```

---

### Task 4: Import mức file — ClosedXML reader, giới hạn 5 MB / 500 dòng, cột theo tiêu đề, `POST /api/admin/students/import`

**Files:**
- Modify: `backend/Directory.Packages.props`, `backend/src/Coaching.Infrastructure/Coaching.Infrastructure.csproj`, `backend/tests/Coaching.TestSupport/Coaching.TestSupport.csproj`
- Create: `backend/tests/Coaching.TestSupport/ImportWorkbook.cs`, `backend/tests/Coaching.TestSupport/Fixtures/make-encrypted-xlsx.py`, `backend/tests/Coaching.TestSupport/Fixtures/encrypted.xlsx` (sinh bằng script, nhị phân)
- Create: `backend/src/Coaching.Application/Students/Import/Workbook.cs`, `StudentImportColumns.cs`, `ImportDtos.cs`, `ImportErrors.cs`, `ImportStudentsHandler.cs`
- Modify: `backend/src/Coaching.Application/DependencyInjection.cs`
- Create: `backend/src/Coaching.Infrastructure/Import/ClosedXmlWorkbookReader.cs`
- Modify: `backend/src/Coaching.Infrastructure/DependencyInjection.cs`
- Modify: `backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs`
- Test: `backend/tests/Coaching.Application.Tests/Students/Import/StudentImportColumnsTests.cs` (C)
- Test: `backend/tests/Coaching.Api.IntegrationTests/Infrastructure/ClosedXmlWorkbookReaderTests.cs` (C), `backend/tests/Coaching.Api.IntegrationTests/Students/ImportRequests.cs` (C), `backend/tests/Coaching.Api.IntegrationTests/Students/ImportAuthorizationTests.cs` (C)
- Test: `backend/tests/Coaching.Acceptance/Support/ImportApi.cs` (C), `backend/tests/Coaching.Acceptance/Steps/ImportSteps.cs` (C), `backend/tests/Coaching.Acceptance/Steps/PasswordResetSteps.cs` (M — step `không email nào được gửi tới "…"` nhận cả 200 của import)

**Interfaces:**
- Consumes:
  - Task 1 — `RegistrationInput` khoá mới (`BodyFatAndMethod`, `WeightHeightBeforeLifting`, `BodyPhotoUrl`; không còn `Availability`, `Equipment`, `BodyFatPercent`, `BodyFatMethod`, `WeightBeforeLiftingKg`, `HeightThenCm`, `Photo*Url`), `RegistrationForm.Validate(RegistrationInput, DateOnly) → (RegistrationForm? Form, IReadOnlyList<RegistrationFieldError> Errors)`, `RegistrationFieldError(string Code, string Message)`, `RegistrationFields.LabelOf(code)`.
  - Task 3 — `StudentRegistrar.RegisterAsync(Guid coachId, string? fullName, Email email, RegistrationForm registration, string? internalNote, CancellationToken) → Task<Result<Student>>` (đã đăng ký Scoped trong `AddApplication` vì `CreateStudentHandler` dùng nó; tạo `Student` Chờ kích hoạt + Đang coaching + tài khoản trong một transaction, không gửi email; trùng email → `StudentErrors.EmailTaken`).
  - F02/F00 — `CurrentCoach.GetAsync`, `StudentErrors.CoachNotFound`, `SearchText.Normalize` (bỏ dấu, `đ→d`, trim, chữ thường), `Email.Create`, `IClock.Today`, `ResultExtensions.ToActionResult/ToProblem`, `AdminControllerBase` (`AdminOnly`), `GlobalExceptionHandler` (lỗi khác → 500). Test: `ApiFactory` (`AdminClientAsync`, `CreateStudentAsync`, `LoginAsync`), `StudentRequests.Url/ErrorCodeAsync`, `AcceptanceContext` (`Host`, `Client()`, `GetAsync`, `LastResponse`), `StudentApi` (`AdminToken`, `ListAsync`), step `Given coach "…" đã đăng nhập`.
- Produces:
  - Package: `ClosedXML` **0.105.1** (MIT, phát hành 25/07/2026 — implementer chạy `dotnet restore` để chắc chắn tải được) cho `Coaching.Infrastructure` và `Coaching.TestSupport` (test acceptance + integration nhận qua `Coaching.TestSupport`).
  - Application `Coaching.Application.Students.Import` (đúng skeleton): `WorkbookCell(string? Text, double? Number, DateTime? Date)`, `WorkbookRow(int RowNumber, IReadOnlyList<WorkbookCell> Cells)`, `WorkbookSheet(IReadOnlyList<string> Headers, IReadOnlyList<WorkbookRow> Rows)`, `IWorkbookReader.Read(Stream)`, `InvalidWorkbookException(string)`, `StudentImportColumns` (`Email`, `FullName`, `NormalizeHeader`, `FieldFor`; thêm `EmailHeader = "Địa chỉ email"`, `FullNameHeader = "Họ Và Tên (Name)"`, `DisplayHeader(header)` = dòng đầu đã trim — tên cột trong lỗi), `ImportCellErrorDto`, `ImportRowDto`, `ImportResultDto`, `ImportErrors` (5 lỗi, đúng code + câu chữ), `ImportStudentsHandler(CurrentCoach, IWorkbookReader, StudentRegistrar, IClock)` với `MaxBytes`, `MaxRows`, `HandleAsync(Stream, long, CancellationToken)`. Application không có abstraction logging → bỏ logger khỏi handler; số dòng tạo/bỏ qua log ở controller.
  - Bản Task 4 của handler chỉ đọc email, họ tên và các mục **chữ/link** (Line/Paragraph/Url); ô số/ngày/giá/mục có danh sách để trống — Task 5 thay toàn bộ file handler.
  - Infrastructure: `internal sealed ClosedXmlWorkbookReader : IWorkbookReader` (Singleton).
  - API: `POST /api/admin/students/import` (multipart, field `file`) → `200 ImportResultDto` | `400 ProblemDetails` (`import.file_invalid|file_too_large|too_many_rows|missing_column|no_rows`) | 401/403.
  - Test: `ImportWorkbook` (TestSupport: `EmailHeader`, `FullNameHeader`, `AddressHeader`, `OccupationHeader`, `AgeHeader`, `TransferDateHeader`, `PriceHeader`, `XlsxContentType`, `Standard()`, `AddRow`, `AddValidRow`, `AddValidRows`, `AddBlankRow`, `Set`, `Get`, `WithoutColumn`, `MoveColumnBefore`, `AddSecondSheetRow`, `ToBytes`, `Csv()`, `LegacyXls()`, `BrokenXlsx()`, `EncryptedXlsx()`, `OfSize(mb)`); `ImportApi` (acceptance: `Workbook`, `RawFile`, `FileName`, `LastFile`, `StudentsBefore`, `Result`, `ImportAsync`, `ImportOkAsync`, `OkResult`, `Row`, `ProfileOfRowAsync`, `CountStudentsAsync`, `StudentExistsAsync`, `AssertRejectedAsync`); `ImportRequests` (integration: `Url`, `NewEmail`, `OneRow`, `PostAsync`, `StudentExistsAsync`); step `When coach import file đó`, `Then không hồ sơ nào được tạo`, `Then (\d+) hồ sơ học viên mới được tạo`, `Given một file Excel có (\d+) dòng học viên hợp lệ`, `Given một file Excel có 1 dòng học viên hợp lệ với email "…"` (Task 5 dùng lại).

**Owns:** `@BR-f02-import-excel-012`, `@BR-f02-import-excel-013`, `@BR-f02-import-excel-014`, `@BR-f02-import-excel-015`, `@BR-f02-import-excel-016`, `@BR-f02-import-excel-017`, `@BR-f02-import-excel-018`, `@BR-f02-import-excel-019`, `@BR-f02-import-excel-020`, `@BR-f02-import-excel-021`, `@BR-f02-import-excel-022` | `@ADR-f02-import-excel-004`, `@ADR-f02-import-excel-005`, `@ADR-f02-import-excel-007`

> **File có mật khẩu (BR-f02-import-excel-014):** ClosedXML không tạo được file .xlsx mã hoá. Test dùng một file .xlsx đặt mật khẩu **thật** (`Fixtures/encrypted.xlsx`, container OLE theo MS-OFFCRYPTO) sinh một lần bằng `msoffcrypto-tool` qua script commit kèm (`Fixtures/make-encrypted-xlsx.py`), nhúng làm `EmbeddedResource` của `Coaching.TestSupport`. Đã thử: file sinh ra bắt đầu bằng `d0 cf 11 e0 a1 b1 1a e1` và `new XLWorkbook(stream)` ném `System.IO.FileFormatException`. Scenario `.xlsx có đặt mật khẩu` vì vậy chạy tự động, không cần `@manual`.
>
> **@BR-f02-import-excel-012:** scenario duy nhất của rule là `@web` (Task 8). Phía API được chốt bằng integration test: coach import tạo hồ sơ; học viên → 403; chưa đăng nhập → 401 (seed LOẠI), cả hai không tạo hồ sơ nào.
>
> **Bảng tiêu đề (ADR-f02-import-excel-005):** lấy từ tiêu đề thật của template chuẩn hoá của PO `C:SERSADMINDOWNLOADSCOPY OF G���I Đ���C (CâU TR��� L���I)-2.XLSX` (40 C���T A�AN, GóI/GIá/NGàY CK Là 3 C���T RIêNG `B���N MUA PACK NàO?`, `GIá GóI B���N MUA`, `NGàY CHUY���N KHO���N`) + TIêU đ��� DùNG TRONG `IMPORT-EXCEL.FEATURE`. C���T G���P KI���U Cũ `B���N MUA PACK NàO, GIá BAO NHIêU Và NGàY CHUY���N KHO���N` (FILE Cũ `DOCS/FORM H��� Sơ EXCEL đ��� IMPORT.XLSX`) B��� B��� QUA.

- [x] **Step 1: Thêm package ClosedXML (ghim phiên bản) cho Infrastructure và TestSupport**

`backend/Directory.Packages.props` — thêm ngay sau dòng `<PackageVersion Include="System.IdentityModel.Tokens.Jwt" Version="8.23.0" />`:
```xml
    <!-- @ADR-f02-import-excel-004: đọc .xlsx — ClosedXML (MIT, NFR-f02-import-excel-003), phát hành 25/07/2026 -->
    <PackageVersion Include="ClosedXML" Version="0.105.1" />
```

`backend/src/Coaching.Infrastructure/Coaching.Infrastructure.csproj` — thêm vào `ItemGroup` chứa các `PackageReference`, sau dòng `<PackageReference Include="System.IdentityModel.Tokens.Jwt" />`:
```xml
    <PackageReference Include="ClosedXML" />
```

`backend/tests/Coaching.TestSupport/Coaching.TestSupport.csproj` — thay toàn bộ nội dung:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>false</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Testcontainers.MsSql" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
    <!-- Test acceptance + integration dựng file .xlsx trong bộ nhớ (ImportWorkbook) -->
    <PackageReference Include="ClosedXML" />
  </ItemGroup>
  <ItemGroup>
    <!-- @BR-f02-import-excel-014: .xlsx đặt mật khẩu thật — ClosedXML không tạo được, sinh bằng Fixtures/make-encrypted-xlsx.py -->
    <EmbeddedResource Include="Fixtures\encrypted.xlsx" LogicalName="Coaching.TestSupport.Fixtures.encrypted.xlsx" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Coaching.Domain\Coaching.Domain.csproj" />
  </ItemGroup>
</Project>
```

Run: `cd backend && dotnet restore Coaching.slnx`
Expected: `Restored …Coaching.Infrastructure.csproj`, `…Coaching.TestSupport.csproj` — không có `NU1101`/`NU1102` (gói ClosedXML 0.105.1 tải được). Nếu restore báo không tìm thấy phiên bản: DỪNG, báo lại (không tự đổi version).

- [x] **Step 2: File .xlsx đặt mật khẩu (fixture) + builder `ImportWorkbook`**

`backend/tests/Coaching.TestSupport/Fixtures/make-encrypted-xlsx.py`:
```python
"""Sinh Fixtures/encrypted.xlsx: workbook .xlsx tối thiểu (tiêu đề + 1 dòng) rồi đặt mật khẩu bằng msoffcrypto-tool.

@BR-f02-import-excel-014: ClosedXML không tạo được file mã hoá nên test dùng file này (container OLE, MS-OFFCRYPTO).
Chạy một lần trong venv NGOÀI repo:  pip install msoffcrypto-tool==5.4.2
"""
import io
import sys
import zipfile

import msoffcrypto.format.ooxml as ooxml

PARTS = {
    "[Content_Types].xml": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">'
        '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>'
        '<Default Extension="xml" ContentType="application/xml"/>'
        '<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>'
        '<Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>'
        '</Types>',
    "_rels/.rels": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
        '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>'
        '</Relationships>',
    "xl/workbook.xml": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">'
        '<sheets><sheet name="Sheet1" sheetId="1" r:id="rId1"/></sheets></workbook>',
    "xl/_rels/workbook.xml.rels": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
        '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>'
        '</Relationships>',
    "xl/worksheets/sheet1.xml": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>'
        '<row r="1"><c r="A1" t="inlineStr"><is><t>Địa chỉ email</t></is></c><c r="B1" t="inlineStr"><is><t>Họ Và Tên (Name)</t></is></c></row>'
        '<row r="2"><c r="A2" t="inlineStr"><is><t>khoa@gmail.com</t></is></c><c r="B2" t="inlineStr"><is><t>Khoa</t></is></c></row>'
        '</sheetData></worksheet>',
}

plain = io.BytesIO()
with zipfile.ZipFile(plain, "w", zipfile.ZIP_DEFLATED) as package:
    for name, xml in PARTS.items():
        package.writestr(name, xml.encode("utf-8"))
plain.seek(0)
with open(sys.argv[1], "wb") as out:
    ooxml.OOXMLFile(plain).encrypt("Coach2026", out)
```

Sinh fixture (Git Bash trên Windows; Linux/macOS thay `Scripts` bằng `bin`), venv đặt NGOÀI repo:
```bash
python -m venv "$TEMP/xlsx-crypto"
"$TEMP/xlsx-crypto/Scripts/python" -m pip install msoffcrypto-tool==5.4.2
"$TEMP/xlsx-crypto/Scripts/python" -I backend/tests/Coaching.TestSupport/Fixtures/make-encrypted-xlsx.py backend/tests/Coaching.TestSupport/Fixtures/encrypted.xlsx
od -A x -t x1 -N 8 backend/tests/Coaching.TestSupport/Fixtures/encrypted.xlsx
```
Expected: dòng cuối `000000 d0 cf 11 e0 a1 b1 1a e1` (file khoảng 7 KB).

`backend/tests/Coaching.TestSupport/ImportWorkbook.cs`:
```csharp
using System.Text;
using ClosedXML.Excel;

namespace Coaching.TestSupport;

/// <summary>
/// File .xlsx "câu trả lời Google Form" dựng trong bộ nhớ cho test import: dòng 1 = tiêu đề, dòng dữ liệu bắt đầu từ
/// dòng Excel 2. Đặt ô theo tiêu đề; tiêu đề chưa có thì thêm cột ở cuối.
/// </summary>
public sealed class ImportWorkbook
{
    public const string EmailHeader = "Địa chỉ email";
    public const string FullNameHeader = "Họ Và Tên (Name)";
    public const string AddressHeader = "Địa chỉ (Address)";
    public const string OccupationHeader = "Nghề nghiệp của bạn là gì?";
    public const string AgeHeader = "Tuổi (Age)";
    public const string TransferDateHeader = "Ngày chuyển khoản";
    public const string PriceHeader = "Giá gói bạn mua";
    public const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const string EncryptedResource = "Coaching.TestSupport.Fixtures.encrypted.xlsx";

    /// <summary>Chữ ký OLE compound file — dùng chung cho .xls (BIFF) và .xlsx đặt mật khẩu.</summary>
    private static readonly byte[] CompoundFileSignature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    private readonly List<string> _headers;
    private readonly List<Dictionary<string, object?>> _rows = [];
    private readonly List<(string Email, string FullName)> _secondSheet = [];

    public ImportWorkbook(params string[] headers) => _headers = [.. headers];

    /// <summary>Email, họ tên + 2 cột chữ không bắt buộc (địa chỉ, nghề nghiệp).</summary>
    public static ImportWorkbook Standard() => new(EmailHeader, FullNameHeader, AddressHeader, OccupationHeader);

    public IReadOnlyList<string> Headers => _headers;

    /// <summary>Số dòng Excel của dòng dữ liệu cuối (tiêu đề = dòng 1).</summary>
    public int LastRow => _rows.Count + 1;

    /// <summary>Thêm dòng chỉ có email + họ tên (null = ô trống); trả số dòng Excel.</summary>
    public int AddRow(string? email, string? fullName)
    {
        _rows.Add(new Dictionary<string, object?> { [EmailHeader] = email, [FullNameHeader] = fullName });
        return LastRow;
    }

    /// <summary>Dòng hợp lệ: email, họ tên, và địa chỉ + nghề nghiệp nếu file có hai cột đó.</summary>
    public int AddValidRow(string email, string fullName)
    {
        var row = AddRow(email, fullName);
        Set(row, AddressHeader, "12 Lý Thường Kiệt, Hà Nội", addColumn: false);
        Set(row, OccupationHeader, "Kế toán", addColumn: false);
        return row;
    }

    /// <summary><paramref name="count"/> dòng hợp lệ, email <c>{prefix}{i}@gmail.com</c>, họ tên <c>Học viên {i}</c>.</summary>
    public ImportWorkbook AddValidRows(int count, string prefix = "hocvien")
    {
        for (var i = 1; i <= count; i++)
            AddValidRow($"{prefix}{i}@gmail.com", $"Học viên {i}");
        return this;
    }

    public int AddBlankRow()
    {
        _rows.Add(new Dictionary<string, object?>());
        return LastRow;
    }

    /// <summary>Đặt ô (dòng Excel, tiêu đề). Giá trị: string → ô chữ, DateTime/DateOnly → ô ngày, số → ô số, null → trống.</summary>
    public ImportWorkbook Set(int excelRow, string header, object? value, bool addColumn = true)
    {
        if (!_headers.Contains(header))
        {
            if (!addColumn)
                return this;
            _headers.Add(header);
        }
        _rows[excelRow - 2][header] = value;
        return this;
    }

    public object? Get(int excelRow, string header) => _rows[excelRow - 2].GetValueOrDefault(header);

    /// <summary>Bỏ mọi cột có tiêu đề bắt đầu bằng <paramref name="headerPrefix"/>.</summary>
    public ImportWorkbook WithoutColumn(string headerPrefix)
    {
        _headers.RemoveAll(h => h.StartsWith(headerPrefix, StringComparison.Ordinal));
        return this;
    }

    public ImportWorkbook MoveColumnBefore(string header, string before)
    {
        if (!_headers.Remove(header))
            throw new ArgumentException($"Không có cột \"{header}\".", nameof(header));
        var index = _headers.IndexOf(before);
        if (index < 0)
            throw new ArgumentException($"Không có cột \"{before}\".", nameof(before));
        _headers.Insert(index, header);
        return this;
    }

    /// <summary>Thêm một dòng (email, họ tên) ở sheet thứ hai (cùng tiêu đề email + họ tên).</summary>
    public ImportWorkbook AddSecondSheetRow(string email, string fullName)
    {
        _secondSheet.Add((email, fullName));
        return this;
    }

    public byte[] ToBytes()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Câu trả lời biểu mẫu 1");
        for (var c = 0; c < _headers.Count; c++)
            sheet.Cell(1, c + 1).Value = _headers[c];
        for (var r = 0; r < _rows.Count; r++)
        {
            for (var c = 0; c < _headers.Count; c++)
            {
                if (_rows[r].TryGetValue(_headers[c], out var value))
                    sheet.Cell(r + 2, c + 1).Value = ToCellValue(value);
            }
        }

        if (_secondSheet.Count > 0)
        {
            var second = workbook.AddWorksheet("Sheet2");
            second.Cell(1, 1).Value = EmailHeader;
            second.Cell(1, 2).Value = FullNameHeader;
            for (var i = 0; i < _secondSheet.Count; i++)
            {
                second.Cell(i + 2, 1).Value = _secondSheet[i].Email;
                second.Cell(i + 2, 2).Value = _secondSheet[i].FullName;
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // ---- File không phải .xlsx đọc được (@BR-f02-import-excel-014) ----

    public static byte[] Csv() => Encoding.UTF8.GetBytes("Địa chỉ email,Họ Và Tên (Name)\nlan@gmail.com,Nguyễn Thị Lan\n");

    /// <summary>Excel cũ .xls là OLE compound file (BIFF) — chữ ký D0 CF 11 E0 A1 B1 1A E1, không phải gói zip .xlsx.</summary>
    public static byte[] LegacyXls()
    {
        var bytes = new byte[4096];
        CompoundFileSignature.CopyTo(bytes, 0);
        return bytes;
    }

    /// <summary>.xlsx bị cắt nửa (zip hỏng).</summary>
    public static byte[] BrokenXlsx()
    {
        var bytes = Standard().AddValidRows(1).ToBytes();
        return bytes[..(bytes.Length / 2)];
    }

    /// <summary>.xlsx đặt mật khẩu thật (Fixtures/encrypted.xlsx, sinh bằng Fixtures/make-encrypted-xlsx.py).</summary>
    public static byte[] EncryptedXlsx()
    {
        using var resource = typeof(ImportWorkbook).Assembly.GetManifestResourceStream(EncryptedResource)
            ?? throw new InvalidOperationException("Thiếu Fixtures/encrypted.xlsx — chạy Fixtures/make-encrypted-xlsx.py.");
        using var copy = new MemoryStream();
        resource.CopyTo(copy);
        return copy.ToArray();
    }

    /// <summary>File nặng đúng <paramref name="megabytes"/> MB: workbook thật ở đầu + phần đệm (bị từ chối theo kích thước trước khi đọc).</summary>
    public static byte[] OfSize(int megabytes)
    {
        var bytes = new byte[megabytes * 1024 * 1024];
        Standard().AddValidRows(1).ToBytes().CopyTo(bytes, 0);
        return bytes;
    }

    private static XLCellValue ToCellValue(object? value) => value switch
    {
        null => Blank.Value,
        string text => text,
        DateTime date => date,
        DateOnly date => date.ToDateTime(TimeOnly.MinValue),
        int number => number,
        long number => number,
        double number => number,
        decimal number => (double)number,
        _ => throw new ArgumentException($"Kiểu ô không hỗ trợ: {value.GetType().Name}", nameof(value)),
    };
}
```

Run: `cd backend && dotnet build tests/Coaching.TestSupport`
Expected: `Build succeeded` — `0 Warning(s)`, `0 Error(s)`.

- [x] **Step 3: Viết unit test nhận cột theo tiêu đề — kỳ vọng FAIL**

`backend/tests/Coaching.Application.Tests/Students/Import/StudentImportColumnsTests.cs`:
```csharp
using Coaching.Application.Students.Import;

namespace Coaching.Application.Tests.Students.Import;

public sealed class StudentImportColumnsTests
{
    // @ADR-f02-import-excel-005 @BR-f02-import-excel-018: tiêu đề thật của file mẫu Google Form (và tiêu đề trong
    // living doc) → mã mục; so tiền tố của dòng đầu đã chuẩn hoá, không phụ thuộc thứ tự cột
    [Theory]
    [InlineData("Địa chỉ email", "email")]
    [InlineData("Họ Và Tên (Name)", "fullName")]
    [InlineData("Địa chỉ (Address)", "address")]
    [InlineData("Tuổi (Age)", "age")]
    [InlineData("Chiều cao (Height)", "heightCm")]
    [InlineData("Cân nặng (Weight)", "weightKg")]
    [InlineData("Số đo Vòng 1\n\nChest measurement", "chestCm")]
    [InlineData("Số đo Vòng 2\n\nWaist measurement", "bellyCm")]
    [InlineData("Số đo vòng Eo (trên rốn 4cm)\n\nWaist circumference (4 cm above the navel)", "waistCm")]
    [InlineData("Số đo vòng 3\n\nHip measurement", "hipCm")]
    [InlineData("Số đo tay phải (khi đang gồng Biceps)\n\nRight Arm Circumference", "rightArmCm")]
    [InlineData("Số đo tay trái (khi đang gồng Biceps)\n\nLeft Arm Circumference", "leftArmCm")]
    [InlineData("Số đo đùi phải (đo ở vị trí to nhất)\n\nRight Thigh Circumference", "rightThighCm")]
    [InlineData("Số đo đùi trái (đo ở vị trí to nhất)\n\nLeft Thigh Circumference", "leftThighCm")]
    [InlineData("Mỡ trong cơ thể (%) + cách thức đo \n\n% bodyfat and measurement method", "bodyFatAndMethod")]
    [InlineData("Số năm tập luyện\n\nTraining time", "trainingYears")]
    [InlineData("Mục tiêu Tập luyện của bạn là gì? \n\nTraining target", "trainingGoal")]
    [InlineData("Nếu giả sử có một chương trình tập hiệu quả, bạn có thể dành ra bao nhiêu thời gian trong tuần cho việc luyện tập? \n\nIf provided with an effective training program", "weeklyTrainingTime")]
    [InlineData("Cân nặng của bạn trước khi tập tạ là bao nhiêu và chiều cao của bạn khi đó là bao nhiêu? \n\nWhat was your weight", "weightHeightBeforeLifting")]
    [InlineData("Trình bày chế độ tập luyện của bạn hiện tại (khuyến khích chi tiết):\n\nPlease describe", "currentTraining")]
    [InlineData("Bạn có tiền sử bệnh lý và chấn thương nào không? \n\nHave you experienced", "medicalHistory")]
    [InlineData("CÂU HỎI QUAN TRỌNG: Bạn có sẵn sàng dành ra một khoản tiền kha khá để đầu tư vào những đồ ăn chất lượng như cá\n\nMPORTANT QUESTION", "foodBudgetReadiness")]
    [InlineData("Nghề nghiệp của bạn là gì? (Mục tiêu của câu hỏi này là để tìm hiểu về đồng hồ sinh học):\n\nWhat do you do for work?", "occupation")]
    [InlineData("Mức độ hoạt động\n\nActivity Level", "activityLevel")]
    [InlineData("Mức độ căng thẳng\n\nStress level", "stressLevel")]
    [InlineData("Thời gian ngủ trong 1 ngày:\n\nHow many hours of sleep do you get per day?:", "sleepDuration")]
    [InlineData("Hàng ngày bạn sử dụng bao nhiêu caffeine hoặc trong những ngày học/làm việc bình thường? \n\nHow much caffeine", "caffeine")]
    [InlineData("Nêu tên ra các loại thực phẩm hỗ trợ mà bạn đang sử dụng hiện tại? \n\nPlease list any supplements", "supplements")]
    [InlineData("Trình bày chế độ dinh dưỡng của bạn hiện tại? (khuyến khích chi tiết) \n\nPlease describe", "currentDiet")]
    [InlineData("Bạn sẽ tập được bao nhiêu buổi/ tuần? \n\nHow many days per week can you train?", "sessionsPerWeek")]
    [InlineData("Bạn muốn giáo án tập của bạn được thiết thế theo phong cách nào? \n\nWhat type of training style", "programStyle")]
    [InlineData("Đính kèm 4 ảnh toàn thân mặt truớc, mặt sau và 2 mặt bên với ánh sáng tốt. (Chụp toàn thân, cam để ngang hông) \n\nAttach 4 full-body photos", "bodyPhotoUrl")]
    [InlineData("Bạn mong đợi điều gì từ coach? (What are your expectations from your coach?)", "expectations")]
    [InlineData("Bạn từng theo coach/online coaching trước chưa? Trải nghiệm ra sao? (Have you ever had a coach)", "previousCoaching")]
    [InlineData("Điều gì khiến bạn quyết định đầu tư vào bản thân lúc này? (What inspired you)", "investmentReason")]
    [InlineData("Thời gian bạn có thể ăn các bữa và thời gian đi tập để mình lên mealplan cho chuẩn nha. (khuyến khích chi tiết) \n\nYour available meal times", "mealAndTrainingTimes")]
    [InlineData("Bạn mua Pack nào?", "pack")]
    [InlineData("Giá gói bạn mua", "price")]
    [InlineData("Ngày chuyển khoản", "transferDate")]
    [InlineData("Phong cách giáo án", "programStyle")]
    [InlineData("Đính kèm 4 ảnh", "bodyPhotoUrl")]
    [InlineData("Thời gian ngủ trong 1 ngày", "sleepDuration")]
    [InlineData("Nghề nghiệp", "occupation")]
    public void Form_question_header_maps_to_field(string header, string field) =>
        Assert.Equal(field, StudentImportColumns.FieldFor(header));

    // @BR-f02-import-excel-020: cột không thuộc form hồ sơ bị bỏ qua (kể cả cột gộp Gói + giá + ngày của file mẫu)
    [Theory]
    [InlineData("Dấu thời gian")]
    [InlineData("Cột 36")]
    [InlineData("Bạn mua Pack nào, giá bao nhiêu và ngày chuyển khoản")]
    [InlineData("")]
    public void Header_outside_registration_form_is_ignored(string header) =>
        Assert.Null(StudentImportColumns.FieldFor(header));

    // @BR-f02-import-excel-018: tiêu đề song ngữ, khác hoa/thường, thừa khoảng trắng vẫn khớp
    [Theory]
    [InlineData("Số đo Vòng 1 / Chest measurement", "chestCm")]
    [InlineData("  HỌ VÀ   TÊN  ", "fullName")]
    [InlineData("ĐỊA CHỈ EMAIL\nEmail address", "email")]
    public void Bilingual_or_differently_spaced_header_still_matches(string header, string field) =>
        Assert.Equal(field, StudentImportColumns.FieldFor(header));

    // @ADR-f02-import-excel-005
    [Fact]
    public void Normalize_keeps_first_line_strips_diacritics_and_collapses_spaces()
    {
        Assert.Equal("so do vong eo (tren ron 4cm)",
            StudentImportColumns.NormalizeHeader("Số đo vòng  Eo (trên rốn 4cm)\r\n\r\nWaist circumference"));
        Assert.Equal("dia chi email", StudentImportColumns.NormalizeHeader("  Địa chỉ email "));
        Assert.Equal("Số đo vòng 3", StudentImportColumns.DisplayHeader(" Số đo vòng 3\n\nHip measurement"));
    }
}
```

Run: `cd backend && dotnet test tests/Coaching.Application.Tests --filter "FullyQualifiedName~StudentImportColumnsTests"`
Expected: FAIL — build error `CS0234: The type or namespace name 'Import' does not exist in the namespace 'Coaching.Application.Students'`.

- [x] **Step 4: Application — kiểu workbook, bảng cột, DTO, lỗi cả file**

`backend/src/Coaching.Application/Students/Import/Workbook.cs`:
```csharp
namespace Coaching.Application.Students.Import;

/// <summary>
/// @ADR-f02-import-excel-004: một ô đã đọc. Text = giá trị hiển thị đã trim (null nếu trống; ô số → số dạng invariant,
/// ô ngày → dd/MM/yyyy); Number chỉ có ở ô số; Date chỉ có ở ô ngày của Excel.
/// </summary>
public sealed record WorkbookCell(string? Text, double? Number, DateTime? Date);

/// <summary>RowNumber = số dòng Excel (tiêu đề = 1).</summary>
public sealed record WorkbookRow(int RowNumber, IReadOnlyList<WorkbookCell> Cells);

/// <summary>Sheet đầu tiên: tiêu đề dòng 1 + các dòng dữ liệu; dòng trống ở cuối đã bị bỏ.</summary>
public sealed record WorkbookSheet(IReadOnlyList<string> Headers, IReadOnlyList<WorkbookRow> Rows);

/// <summary>@ADR-f02-import-excel-004: port đọc .xlsx — Application không phụ thuộc thư viện Excel.</summary>
public interface IWorkbookReader
{
    /// <exception cref="InvalidWorkbookException">Không phải .xlsx, file hỏng hoặc có mật khẩu.</exception>
    WorkbookSheet Read(Stream xlsx);
}

public sealed class InvalidWorkbookException(string message) : Exception(message);
```

`backend/src/Coaching.Application/Students/Import/StudentImportColumns.cs`:
```csharp
using System.Text.RegularExpressions;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Import;

/// <summary>
/// @ADR-f02-import-excel-005 @BR-f02-import-excel-018: nhận cột theo tiêu đề câu hỏi Google Form, không theo thứ tự cột.
/// Tiêu đề được chuẩn hoá (dòng đầu, trim, chữ thường, bỏ dấu tiếng Việt, gộp khoảng trắng) rồi so tiền tố theo thứ tự
/// bảng — khớp đầu tiên thắng (vì vậy "dia chi email" đứng trước "dia chi", "can nang cua ban" trước "can nang").
/// </summary>
public static partial class StudentImportColumns
{
    public const string Email = "email";
    public const string FullName = "fullName";

    /// <summary>Tên cột nêu trong lỗi thiếu cột (@BR-f02-import-excel-019).</summary>
    public const string EmailHeader = "Địa chỉ email";
    public const string FullNameHeader = "Họ Và Tên (Name)";

    // Field null = cột cố ý bỏ qua (@BR-f02-import-excel-020)
    private static readonly (string Prefix, string? Field)[] Prefixes =
    [
        ("dau thoi gian", null),
        ("dia chi email", Email),
        ("ho va ten", FullName),
        ("dia chi", "address"),
        ("tuoi", "age"),
        ("chieu cao", "heightCm"),
        ("can nang cua ban", "weightHeightBeforeLifting"),
        ("can nang", "weightKg"),
        ("so do vong 1", "chestCm"),
        ("so do vong 2", "bellyCm"),
        ("so do vong eo", "waistCm"),
        ("so do vong 3", "hipCm"),
        ("so do tay phai", "rightArmCm"),
        ("so do tay trai", "leftArmCm"),
        ("so do dui phai", "rightThighCm"),
        ("so do dui trai", "leftThighCm"),
        ("mo trong co the", "bodyFatAndMethod"),
        ("so nam tap luyen", "trainingYears"),
        ("muc tieu tap luyen", "trainingGoal"),
        ("neu gia su co mot chuong trinh tap", "weeklyTrainingTime"),
        ("trinh bay che do tap luyen", "currentTraining"),
        ("ban co tien su benh ly", "medicalHistory"),
        ("cau hoi quan trong", "foodBudgetReadiness"),
        ("nghe nghiep", "occupation"),
        ("muc do hoat dong", "activityLevel"),
        ("muc do cang thang", "stressLevel"),
        ("thoi gian ngu", "sleepDuration"),
        ("hang ngay ban su dung bao nhieu caffeine", "caffeine"),
        ("neu ten ra cac loai thuc pham ho tro", "supplements"),
        ("trinh bay che do dinh duong", "currentDiet"),
        ("thoi gian ban co the an", "mealAndTrainingTimes"),
        ("ban se tap duoc bao nhieu buoi", "sessionsPerWeek"),
        ("ban muon giao an tap", "programStyle"),
        ("phong cach giao an", "programStyle"),
        ("dinh kem 4 anh", "bodyPhotoUrl"),
        ("ban mong doi dieu gi tu coach", "expectations"),
        ("ban tung theo coach", "previousCoaching"),
        ("dieu gi khien ban quyet dinh dau tu", "investmentReason"),
        // Cột gộp "Bạn mua Pack nào, giá bao nhiêu và ngày chuyển khoản" của file mẫu không phải một mục form
        ("ban mua pack nao, gia", null),
        ("ban mua pack nao", "pack"),
        ("gia goi ban mua", "price"),
        ("ngay chuyen khoan", "transferDate"),
    ];

    /// <summary>Dòng đầu của tiêu đề, đã trim — tên cột hiển thị trong kết quả import.</summary>
    public static string DisplayHeader(string header)
    {
        ArgumentNullException.ThrowIfNull(header);
        return header.Trim().Split('\n')[0].TrimEnd('\r').Trim();
    }

    /// <summary>Dòng đầu, trim, chữ thường, bỏ dấu tiếng Việt (đ → d), gộp khoảng trắng.</summary>
    public static string NormalizeHeader(string header) =>
        Spaces().Replace(SearchText.Normalize(DisplayHeader(header)), " ");

    /// <summary>"email" | "fullName" | mã mục form; null = cột bị bỏ qua (vd "Dấu thời gian").</summary>
    public static string? FieldFor(string header)
    {
        var normalized = NormalizeHeader(header);
        if (normalized.Length == 0)
            return null;
        foreach (var (prefix, field) in Prefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.Ordinal))
                return field;
        }
        return null; // @BR-f02-import-excel-020
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
```

`backend/src/Coaching.Application/Students/Import/ImportDtos.cs`:
```csharp
namespace Coaching.Application.Students.Import;

/// <summary>Column = tiêu đề cột như trong file (dòng đầu); Reason = lý do tiếng Việt.</summary>
public sealed record ImportCellErrorDto(string Column, string Reason);

/// <summary>Một dòng có dữ liệu. Status "Created" | "Skipped"; StudentId chỉ có khi Created.</summary>
public sealed record ImportRowDto(int Row, string? Email, string Status, Guid? StudentId, IReadOnlyList<ImportCellErrorDto> Errors);

public sealed record ImportResultDto(int Created, int Skipped, IReadOnlyList<ImportRowDto> Rows);
```

`backend/src/Coaching.Application/Students/Import/ImportErrors.cs`:
```csharp
using Coaching.Application.Common;

namespace Coaching.Application.Students.Import;

/// <summary>Lỗi cả file — từ chối cả file, không tạo hồ sơ nào (400).</summary>
public static class ImportErrors
{
    // @BR-f02-import-excel-014
    public static readonly Error FileInvalid = new("import.file_invalid", "File phải là Excel .xlsx đọc được.");

    // @BR-f02-import-excel-015
    public static readonly Error FileTooLarge = new("import.file_too_large", "File vượt giới hạn 5 MB.");

    // @BR-f02-import-excel-016
    public static readonly Error TooManyRows = new("import.too_many_rows", "File vượt giới hạn 500 dòng.");

    // @BR-f02-import-excel-021
    public static readonly Error NoRows = new("import.no_rows", "File không có dòng dữ liệu.");

    // @BR-f02-import-excel-019
    public static Error MissingColumn(string header) => new("import.missing_column", $"Thiếu cột \"{header}\".");
}
```

Run: `cd backend && dotnet test tests/Coaching.Application.Tests --filter "FullyQualifiedName~StudentImportColumnsTests"`
Expected: `Passed: 51`, `Failed: 0`.

- [x] **Step 5: Viết test reader ClosedXML — kỳ vọng FAIL**

`backend/tests/Coaching.Api.IntegrationTests/Infrastructure/ClosedXmlWorkbookReaderTests.cs`:
```csharp
using Coaching.Application.Students.Import;
using Coaching.Infrastructure.Import;
using Coaching.TestSupport;

namespace Coaching.Api.IntegrationTests.Infrastructure;

public sealed class ClosedXmlWorkbookReaderTests
{
    private readonly ClosedXmlWorkbookReader _reader = new();

    // @ADR-f02-import-excel-004 @BR-f02-import-excel-017: chỉ sheet đầu; ô giữ kiểu chữ / số / ngày
    [Fact]
    public void Reads_first_sheet_only_with_typed_cells()
    {
        var workbook = new ImportWorkbook(ImportWorkbook.EmailHeader, ImportWorkbook.FullNameHeader,
            ImportWorkbook.AgeHeader, ImportWorkbook.TransferDateHeader);
        var row = workbook.AddRow("  lan@gmail.com ", "Nguyễn Thị Lan");
        workbook.Set(row, ImportWorkbook.AgeHeader, 28).Set(row, ImportWorkbook.TransferDateHeader, new DateTime(2026, 11, 12));
        workbook.AddSecondSheetRow("khac@gmail.com", "Học viên sheet hai");

        var sheet = _reader.Read(new MemoryStream(workbook.ToBytes()));

        Assert.Equal(new[] { "Địa chỉ email", "Họ Và Tên (Name)", "Tuổi (Age)", "Ngày chuyển khoản" }, sheet.Headers);
        var only = Assert.Single(sheet.Rows);
        Assert.Equal(2, only.RowNumber);
        Assert.Equal("lan@gmail.com", only.Cells[0].Text);
        Assert.Null(only.Cells[0].Number);
        Assert.Equal(28d, only.Cells[2].Number);
        Assert.Equal("28", only.Cells[2].Text);
        Assert.Equal(new DateTime(2026, 11, 12), only.Cells[3].Date);
        Assert.Equal("12/11/2026", only.Cells[3].Text);
    }

    // @BR-f02-import-excel-022: dòng trống ở giữa vẫn trả về (handler bỏ qua), dòng trống ở cuối bị bỏ
    [Fact]
    public void Keeps_blank_middle_row_and_drops_trailing_blank_rows()
    {
        var workbook = ImportWorkbook.Standard();
        workbook.AddValidRow("a@gmail.com", "A");
        workbook.AddBlankRow();
        workbook.AddValidRow("b@gmail.com", "B");
        workbook.AddBlankRow();

        var sheet = _reader.Read(new MemoryStream(workbook.ToBytes()));

        Assert.Equal(new[] { 2, 3, 4 }, sheet.Rows.Select(r => r.RowNumber));
        Assert.All(sheet.Rows[1].Cells, c => Assert.True(c.Text is null && c.Number is null && c.Date is null));
    }

    public static TheoryData<string> UnreadableKinds => new() { "csv", "xls", "broken", "encrypted" };

    // @BR-f02-import-excel-014 @ADR-f02-import-excel-004: .csv, .xls, .xlsx hỏng, .xlsx có mật khẩu → InvalidWorkbookException
    [Theory]
    [MemberData(nameof(UnreadableKinds))]
    public void Unreadable_file_throws_InvalidWorkbookException(string kind)
    {
        var bytes = kind switch
        {
            "csv" => ImportWorkbook.Csv(),
            "xls" => ImportWorkbook.LegacyXls(),
            "broken" => ImportWorkbook.BrokenXlsx(),
            _ => ImportWorkbook.EncryptedXlsx(),
        };

        var error = Record.Exception(() => _reader.Read(new MemoryStream(bytes)));

        Assert.True(error is InvalidWorkbookException, $"{kind}: {error?.GetType().Name ?? "không ném lỗi"}");
    }
}
```

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~ClosedXmlWorkbookReaderTests"`
Expected: FAIL — build error `CS0234: The type or namespace name 'Import' does not exist in the namespace 'Coaching.Infrastructure'`.

- [x] **Step 6: Infrastructure — `ClosedXmlWorkbookReader` + DI**

`backend/src/Coaching.Infrastructure/Import/ClosedXmlWorkbookReader.cs`:
```csharp
using System.Globalization;
using ClosedXML.Excel;
using Coaching.Application.Students.Import;

namespace Coaching.Infrastructure.Import;

/// <summary>
/// @ADR-f02-import-excel-004: đọc .xlsx bằng ClosedXML (MIT). File chép vào bộ nhớ (không ghi đĩa), chỉ đọc sheet đầu
/// (@BR-f02-import-excel-017), ô giữ kiểu chữ / số / ngày cho Task 5 (ngày, giá). File mở không được — .csv, .xls,
/// .xlsx hỏng hoặc có mật khẩu — đều là InvalidWorkbookException (@BR-f02-import-excel-014).
/// </summary>
internal sealed class ClosedXmlWorkbookReader : IWorkbookReader
{
    private static readonly WorkbookCell BlankCell = new(null, null, null);

    public WorkbookSheet Read(Stream xlsx)
    {
        ArgumentNullException.ThrowIfNull(xlsx);
        try
        {
            using var buffer = new MemoryStream();
            xlsx.CopyTo(buffer);
            buffer.Position = 0;
            using var workbook = new XLWorkbook(buffer);
            return ReadFirstSheet(workbook.Worksheet(1));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new InvalidWorkbookException($"Không đọc được file .xlsx ({ex.GetType().Name}).");
        }
    }

    private static WorkbookSheet ReadFirstSheet(IXLWorksheet sheet)
    {
        var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0; // dòng trống ở cuối không tính

        var headers = new List<string>(lastColumn);
        for (var c = 1; c <= lastColumn; c++)
            headers.Add(ToCell(sheet.Cell(1, c)).Text ?? string.Empty);

        var rows = new List<WorkbookRow>(Math.Max(0, lastRow - 1));
        for (var r = 2; r <= lastRow; r++)
        {
            var cells = new WorkbookCell[lastColumn];
            for (var c = 1; c <= lastColumn; c++)
                cells[c - 1] = ToCell(sheet.Cell(r, c));
            rows.Add(new WorkbookRow(r, cells));
        }
        return new WorkbookSheet(headers, rows);
    }

    private static WorkbookCell ToCell(IXLCell cell) => cell.DataType switch
    {
        XLDataType.Blank => BlankCell,
        XLDataType.Number => FromNumber(cell.GetDouble()),
        XLDataType.DateTime => FromDate(cell.GetDateTime()),
        XLDataType.Text => new WorkbookCell(TextOrNull(cell.GetText()), null, null),
        _ => new WorkbookCell(TextOrNull(cell.GetFormattedString()), null, null),
    };

    private static WorkbookCell FromNumber(double number) =>
        new(number.ToString(CultureInfo.InvariantCulture), number, null);

    private static WorkbookCell FromDate(DateTime date) =>
        new(date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), null, date);

    private static string? TextOrNull(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
```

`backend/src/Coaching.Infrastructure/DependencyInjection.cs`:
- thêm `using Coaching.Application.Students.Import;` và `using Coaching.Infrastructure.Import;` vào khối `using`;
- thêm ngay sau dòng `services.AddScoped<IAccountTokens, AccountTokenStore>(); // @ADR-f01-p1-tai-khoan-hoc-vien-003`:
```csharp
        services.AddSingleton<IWorkbookReader, ClosedXmlWorkbookReader>(); // @ADR-f02-import-excel-004
```

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~ClosedXmlWorkbookReaderTests"`
Expected: `Passed: 6`, `Failed: 0`.

- [x] **Step 7: Viết test API — integration (phân quyền) + acceptance (bind scenario BR-013…022)**

`backend/tests/Coaching.Api.IntegrationTests/Students/ImportRequests.cs`:
```csharp
using System.Net.Http.Headers;
using Coaching.Domain.Common;
using Coaching.Infrastructure.Persistence;
using Coaching.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

internal static class ImportRequests
{
    public const string Url = "/api/admin/students/import";

    public static string NewEmail() => $"import-{Guid.NewGuid():N}@test.local";

    public static byte[] OneRow(string email)
    {
        var workbook = ImportWorkbook.Standard();
        workbook.AddValidRow(email, "Học viên Import");
        return workbook.ToBytes();
    }

    public static async Task<HttpResponseMessage> PostAsync(HttpClient client, byte[] file, string fileName = "cau-tra-loi.xlsx")
    {
        using var content = new MultipartFormDataContent();
        var part = new ByteArrayContent(file);
        part.Headers.ContentType = new MediaTypeHeaderValue(ImportWorkbook.XlsxContentType);
        content.Add(part, "file", fileName);
        return await client.PostAsync(Url, content);
    }

    public static async Task<bool> StudentExistsAsync(ApiFactory factory, string email)
    {
        var value = Email.Create(email);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Students.AnyAsync(s => s.Email == value);
    }
}
```

`backend/tests/Coaching.Api.IntegrationTests/Students/ImportAuthorizationTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Coaches;
using Coaching.Application.Students;
using Coaching.Application.Students.Import;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class ImportAuthorizationTests(ApiFactory factory)
{
    // @BR-f02-import-excel-012: người chưa đăng nhập import → 401, không hồ sơ nào được tạo
    [Fact]
    public async Task Anonymous_import_is_rejected_with_401_and_creates_nothing()
    {
        var email = ImportRequests.NewEmail();

        var response = await ImportRequests.PostAsync(factory.CreateClient(), ImportRequests.OneRow(email));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(await ImportRequests.StudentExistsAsync(factory, email));
    }

    // @BR-f02-import-excel-012: học viên import → 403, không hồ sơ nào được tạo
    [Fact]
    public async Task Student_import_is_forbidden_with_403_and_creates_nothing()
    {
        var (studentEmail, password) = await factory.CreateStudentAsync();
        var tokens = await factory.LoginAsync(studentEmail, password);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var email = ImportRequests.NewEmail();

        var response = await ImportRequests.PostAsync(client, ImportRequests.OneRow(email));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(await ImportRequests.StudentExistsAsync(factory, email));
    }

    // @BR-f02-import-excel-012 @BR-f02-import-excel-013: coach import → hồ sơ được tạo và thuộc coach đó
    [Fact]
    public async Task Coach_import_creates_profile_owned_by_that_coach()
    {
        var admin = await factory.AdminClientAsync();
        var email = ImportRequests.NewEmail();

        var response = await ImportRequests.PostAsync(admin, ImportRequests.OneRow(email));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<ImportResultDto>())!;
        var row = Assert.Single(result.Rows);
        Assert.Equal("Created", row.Status);
        var detail = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{row.StudentId}"))!;
        var coach = (await admin.GetFromJsonAsync<CoachProfileDto>("/api/admin/profile"))!;
        Assert.Equal(coach.Id, detail.CoachId);
        Assert.Equal(email, detail.Email);
    }

    // @BR-f02-import-excel-014: request không có phần "file" → 400 import.file_invalid
    [Fact]
    public async Task Request_without_file_part_is_rejected_as_invalid_file()
    {
        var admin = await factory.AdminClientAsync();
        using var content = new MultipartFormDataContent { { new StringContent("x"), "note" } };

        var response = await admin.PostAsync(ImportRequests.Url, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("import.file_invalid", await StudentRequests.ErrorCodeAsync(response));
    }
}
```

`backend/tests/Coaching.Acceptance/Support/ImportApi.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Application.Students;
using Coaching.Application.Students.Import;
using Coaching.Domain.Common;
using Coaching.Infrastructure.Persistence;
using Coaching.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Acceptance.Support;

/// <summary>Import Excel trong một scenario: file đang soạn, lần import gần nhất và số hồ sơ ngay trước lần đó.</summary>
public sealed class ImportApi(AcceptanceContext ctx, StudentApi api)
{
    public const string Url = "/api/admin/students/import";

    public ImportWorkbook Workbook { get; set; } = ImportWorkbook.Standard();

    /// <summary>File không dựng từ <see cref="Workbook"/> (.csv, .xls, hỏng, có mật khẩu, 6 MB); null = dùng Workbook.</summary>
    public byte[]? RawFile { get; set; }

    public string FileName { get; set; } = "cau-tra-loi.xlsx";

    /// <summary>Nội dung file của lần import gần nhất (để "import lại đúng file đó").</summary>
    public byte[]? LastFile { get; private set; }

    public HttpStatusCode? LastStatus { get; private set; }

    public int StudentsBefore { get; private set; }

    public ImportResultDto? Result { get; private set; }

    public async Task<HttpResponseMessage> ImportAsync(byte[]? file = null)
    {
        file ??= RawFile ?? Workbook.ToBytes();
        StudentsBefore = await CountStudentsAsync();

        using var content = new MultipartFormDataContent();
        var part = new ByteArrayContent(file);
        part.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeOf(FileName));
        content.Add(part, "file", FileName);
        using var request = new HttpRequestMessage(HttpMethod.Post, Url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api.AdminToken);
        var response = await ctx.Client().SendAsync(request);

        ctx.LastResponse = response;
        LastFile = file;
        LastStatus = response.StatusCode;
        Result = response.StatusCode == HttpStatusCode.OK
            ? await response.Content.ReadFromJsonAsync<ImportResultDto>()
            : null;
        return response;
    }

    public async Task<ImportResultDto> ImportOkAsync(byte[]? file = null)
    {
        await ImportAsync(file);
        return OkResult();
    }

    /// <summary>Kết quả của lần import gần nhất — lần đó phải trả 200.</summary>
    public ImportResultDto OkResult()
    {
        Assert.Equal(HttpStatusCode.OK, LastStatus);
        return Result!;
    }

    public ImportRowDto Row(int excelRow) => Assert.Single(OkResult().Rows, r => r.Row == excelRow);

    /// <summary>Hồ sơ đã tạo từ dòng Excel <paramref name="excelRow"/> (đọc qua GET hồ sơ của coach).</summary>
    public async Task<StudentDetailDto> ProfileOfRowAsync(int excelRow)
    {
        var row = Row(excelRow);
        Assert.Equal("Created", row.Status);
        var response = await ctx.GetAsync($"/api/admin/students/{row.StudentId}", api.AdminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
    }

    public async Task<int> CountStudentsAsync()
    {
        await using var scope = ctx.Host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Students.CountAsync();
    }

    public async Task<bool> StudentExistsAsync(string email)
    {
        var value = Email.Create(email);
        await using var scope = ctx.Host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Students.AnyAsync(s => s.Email == value);
    }

    /// <summary>Lần import gần nhất bị từ chối cả file: 400, đúng mã lỗi, tiêu đề chứa <paramref name="titleContains"/>.</summary>
    public async Task AssertRejectedAsync(string code, string? titleContains = null)
    {
        Assert.Equal(HttpStatusCode.BadRequest, LastStatus);
        using var json = JsonDocument.Parse(await ctx.LastResponse!.Content.ReadAsStringAsync());
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
        if (titleContains is not null)
            Assert.Contains(titleContains, json.RootElement.GetProperty("title").GetString(), StringComparison.Ordinal);
    }

    private static string ContentTypeOf(string fileName) => Path.GetExtension(fileName) switch
    {
        ".csv" => "text/csv",
        ".xls" => "application/vnd.ms-excel",
        _ => ImportWorkbook.XlsxContentType,
    };
}
```

`backend/tests/Coaching.Acceptance/Steps/ImportSteps.cs`:
```csharp
using Coaching.Acceptance.Support;
using Coaching.TestSupport;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

/// <summary>import-excel.feature — mức file (BR-f02-import-excel-013…022) + step dùng chung cho Task 5.</summary>
[Binding]
public sealed class ImportSteps(ImportApi import, StudentApi api)
{
    // @BR-f02-import-excel-013 (dùng lại ở BR-f02-import-excel-029)
    [Given(@"^một file Excel có 1 dòng học viên hợp lệ với email ""([^""]*)""$")]
    public void OneValidRowWithEmail(string email) => import.Workbook.AddValidRow(email, "Nguyễn Thị Lan");

    [When("coach import file đó")]
    public Task CoachImports() => import.ImportAsync();

    // @BR-f02-import-excel-013: hồ sơ import nằm trong danh sách học viên của coach đã import
    [Then(@"^danh sách học viên của coach có hồ sơ ""([^""]*)""$")]
    public async Task CoachListHas(string email) =>
        Assert.Contains(await api.ListAsync(null), s => s.Email == email);

    // @BR-f02-import-excel-014
    [Given(@"^một file (\.csv|Excel cũ \.xls|\.xlsx bị hỏng|\.xlsx có đặt mật khẩu)$")]
    public void FileOfKind(string kind)
    {
        (import.RawFile, import.FileName) = kind switch
        {
            ".csv" => (ImportWorkbook.Csv(), "cau-tra-loi.csv"),
            "Excel cũ .xls" => (ImportWorkbook.LegacyXls(), "cau-tra-loi.xls"),
            ".xlsx bị hỏng" => (ImportWorkbook.BrokenXlsx(), "cau-tra-loi.xlsx"),
            _ => (ImportWorkbook.EncryptedXlsx(), "cau-tra-loi.xlsx"),
        };
    }

    // @BR-f02-import-excel-014
    [Then("file bị từ chối vì không phải file Excel .xlsx đọc được")]
    public Task RejectedNotXlsx() => import.AssertRejectedAsync("import.file_invalid", ".xlsx");

    // @BR-f02-import-excel-014 @BR-f02-import-excel-015 @BR-f02-import-excel-016 @BR-f02-import-excel-019
    // @BR-f02-import-excel-021 (Task 5 dùng lại cho dòng bị bỏ qua)
    [Then("không hồ sơ nào được tạo")]
    public async Task NoProfileCreated()
    {
        Assert.Equal(import.StudentsBefore, await import.CountStudentsAsync());
        if (import.Result is { } result)
            Assert.Equal(0, result.Created);
    }

    // @BR-f02-import-excel-015
    [Given("một file Excel nặng 6 MB")]
    public void SixMegabyteFile() => import.RawFile = ImportWorkbook.OfSize(6);

    [Then("file bị từ chối vì vượt giới hạn 5 MB")]
    public Task RejectedTooLarge() => import.AssertRejectedAsync("import.file_too_large", "5 MB");

    // @BR-f02-import-excel-016 (dùng lại ở BR-f02-import-excel-028)
    [Given(@"^một file Excel có (\d+) dòng học viên hợp lệ$")]
    public void ValidRows(int count) => import.Workbook.AddValidRows(count);

    // @BR-f02-import-excel-016 @BR-f02-import-excel-019 @BR-f02-import-excel-020 @BR-f02-import-excel-022
    [Then(@"^(\d+) hồ sơ học viên mới được tạo$")]
    public async Task ProfilesCreated(int count)
    {
        Assert.Equal(count, import.OkResult().Created);
        Assert.Equal(import.StudentsBefore + count, await import.CountStudentsAsync());
    }

    // @BR-f02-import-excel-016
    [Given(@"^một file Excel có (\d+) dòng học viên$")]
    public void Rows(int count) => import.Workbook.AddValidRows(count);

    [Then("file bị từ chối vì vượt giới hạn 500 dòng")]
    public Task RejectedTooManyRows() => import.AssertRejectedAsync("import.too_many_rows", "500 dòng");

    // @BR-f02-import-excel-017
    [Given("một file Excel có 1 dòng học viên ở sheet đầu và 1 dòng học viên khác ở sheet thứ hai")]
    public void RowsOnTwoSheets()
    {
        import.Workbook.AddValidRow("sheetdau@gmail.com", "Học viên sheet đầu");
        import.Workbook.AddSecondSheetRow("sheethai@gmail.com", "Học viên sheet hai");
    }

    [Then("chỉ học viên ở sheet đầu được tạo hồ sơ")]
    public async Task OnlyFirstSheetCreated()
    {
        Assert.Equal(1, import.OkResult().Created);
        Assert.True(await import.StudentExistsAsync("sheetdau@gmail.com"));
        Assert.False(await import.StudentExistsAsync("sheethai@gmail.com"));
    }

    // @BR-f02-import-excel-018
    [Given(@"^một file Excel có cột ""([^""]*)"" đứng trước cột ""([^""]*)"" và 1 dòng học viên hợp lệ$")]
    public void ColumnBeforeColumn(string first, string second)
    {
        import.Workbook.MoveColumnBefore(first, second);
        Assert.True(import.Workbook.Headers.ToList().IndexOf(first) < import.Workbook.Headers.ToList().IndexOf(second));
        import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
    }

    [Then("hồ sơ được tạo với đúng họ tên và email của dòng đó")]
    public async Task CreatedWithNameAndEmailOfRow()
    {
        var profile = await import.ProfileOfRowAsync(2);
        Assert.Equal(import.Workbook.Get(2, ImportWorkbook.FullNameHeader), profile.FullName);
        Assert.Equal(import.Workbook.Get(2, ImportWorkbook.EmailHeader), profile.Email);
    }

    // @BR-f02-import-excel-019: thiếu cột không bắt buộc vẫn xử lý
    [Given(@"^một file Excel không có cột ""([^""]*)"" và 1 dòng học viên hợp lệ$")]
    public void WithoutColumnOneValidRow(string header)
    {
        import.Workbook.WithoutColumn(header);
        Assert.DoesNotContain(import.Workbook.Headers, h => h.StartsWith(header, StringComparison.Ordinal));
        import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
    }

    // @BR-f02-import-excel-019: thiếu cột email / họ tên → từ chối cả file
    [Given(@"^một file Excel không có cột ""([^""]*)""$")]
    public void WithoutColumn(string header)
    {
        import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.WithoutColumn(header);
    }

    [Then(@"^file bị từ chối vì thiếu cột ""([^""]*)""$")]
    public Task RejectedMissingColumn(string header) => import.AssertRejectedAsync("import.missing_column", header);

    // @BR-f02-import-excel-020
    [Given(@"^một file Excel có cột ""([^""]*)"" và 1 dòng học viên hợp lệ$")]
    public void ExtraColumnOneValidRow(string header)
    {
        var row = import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.Set(row, header, new DateTime(2026, 10, 30, 21, 54, 2)); // giá trị kiểu "Dấu thời gian" của Google Form
    }

    // @BR-f02-import-excel-020 @BR-f02-import-excel-022
    [Then("kết quả import không có lỗi nào")]
    public void NoErrors()
    {
        var result = import.OkResult();
        Assert.Equal(0, result.Skipped);
        Assert.All(result.Rows, r =>
        {
            Assert.Equal("Created", r.Status);
            Assert.Empty(r.Errors);
        });
    }

    // @BR-f02-import-excel-021
    [Given("một file Excel chỉ có dòng tiêu đề")]
    public void HeaderOnly() => Assert.Equal(1, import.Workbook.LastRow);

    [Then("coach được báo file không có dòng dữ liệu")]
    public Task RejectedNoRows() => import.AssertRejectedAsync("import.no_rows", "không có dòng dữ liệu");

    // @BR-f02-import-excel-022
    [Given("một file Excel có 2 dòng học viên hợp lệ và một dòng trống ở giữa")]
    public void TwoValidRowsBlankBetween()
    {
        import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.AddBlankRow();
        import.Workbook.AddValidRow("minh@gmail.com", "Trần Văn Minh");
    }
}
```

`backend/tests/Coaching.Acceptance/Steps/PasswordResetSteps.cs` — trong `NoMailTo`, thay dòng
```csharp
        Assert.Equal(HttpStatusCode.Accepted, ctx.LastResponse!.StatusCode);
```
bằng
```csharp
        // 202 = Quên mật khẩu (F01-P1); 200 = import Excel (import-excel.feature) — request đã được xử lý
        Assert.Contains(ctx.LastResponse!.StatusCode, new[] { HttpStatusCode.Accepted, HttpStatusCode.OK });
```

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~ImportAuthorizationTests"`
Expected: FAIL 4 test — route chưa có nên mọi request trả 404, vd `Assert.Equal() Failure: Expected: Unauthorized, Actual: NotFound`.
Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f02-import-excel-013|Category=BR-f02-import-excel-014|Category=BR-f02-import-excel-015|Category=BR-f02-import-excel-016|Category=BR-f02-import-excel-017|Category=BR-f02-import-excel-018|Category=BR-f02-import-excel-019|Category=BR-f02-import-excel-020|Category=BR-f02-import-excel-021|Category=BR-f02-import-excel-022)&Category!=web"`
Expected: FAIL 15 scenario — `POST /api/admin/students/import` trả 404 (vd `Expected: OK, Actual: NotFound` / `Expected: BadRequest, Actual: NotFound`); không scenario nào báo `MissingStepDefinition` hay `Ambiguous`.

- [x] **Step 8: Handler mức file (bản Task 4) + DI + endpoint**

`backend/src/Coaching.Application/Students/Import/ImportStudentsHandler.cs`:
```csharp
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
```

`backend/src/Coaching.Application/DependencyInjection.cs`:
- thêm `using Coaching.Application.Students.Import;`;
- thêm ngay sau dòng `services.AddScoped<ResetPasswordHandler>();`:
```csharp
        services.AddScoped<ImportStudentsHandler>(); // @ADR-f02-import-excel-007
```

`backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs`:
- thêm `using Coaching.Application.Students.Import;` vào khối `using`;
- thêm ngay sau dòng `public sealed class StudentsController : AdminControllerBase` + `{`:
```csharp
    /// <summary>Phần multipart ngoài nội dung file (ranh giới, tiêu đề phần).</summary>
    private const long ImportOverheadBytes = 64 * 1024;

```
- thêm action ở cuối class (sau `SendPasswordResetLink`):
```csharp

    // @BR-f02-import-excel-012 @BR-f02-import-excel-013: AdminOnly như cả controller; hồ sơ gắn coach đang đăng nhập.
    // @ADR-f02-import-excel-007: xử lý đồng bộ; file giữ trong bộ nhớ (MemoryBufferThreshold ≥ giới hạn) — không ghi đĩa.
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImportStudentsHandler.MaxBytes + ImportOverheadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImportStudentsHandler.MaxBytes + ImportOverheadBytes,
        MemoryBufferThreshold = (int)(ImportStudentsHandler.MaxBytes + ImportOverheadBytes))]
    [ProducesResponseType<ImportResultDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Import([FromServices] ImportStudentsHandler handler,
        [FromServices] ILogger<StudentsController> logger, CancellationToken cancellationToken)
    {
        // @BR-f02-import-excel-015: từ chối theo Content-Length trước khi đọc body (không để 413 thô của Kestrel)
        if (Request.ContentLength > ImportStudentsHandler.MaxBytes + ImportOverheadBytes)
            return ImportErrors.FileTooLarge.ToProblem(this);

        IFormFile? file;
        try
        {
            file = (await Request.ReadFormAsync(cancellationToken)).Files.GetFile("file");
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            return ImportErrors.FileTooLarge.ToProblem(this);
        }
        catch (InvalidDataException)
        {
            return ImportErrors.FileInvalid.ToProblem(this);
        }

        if (file is null)
            return ImportErrors.FileInvalid.ToProblem(this); // @BR-f02-import-excel-014

        await using var stream = file.OpenReadStream();
        var result = await handler.HandleAsync(stream, file.Length, cancellationToken);
        if (result.IsSuccess) // Monitoring: chỉ số dòng, không log nội dung ô
            logger.LogInformation("Import hồ sơ học viên: tạo {Created}, bỏ qua {Skipped}", result.Value.Created, result.Value.Skipped);
        return result.ToActionResult(this);
    }
```

- [x] **Step 9: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~ImportAuthorizationTests|FullyQualifiedName~ClosedXmlWorkbookReaderTests"`
Expected: `Passed: 10`, `Failed: 0`.
Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f02-import-excel-013|Category=BR-f02-import-excel-014|Category=BR-f02-import-excel-015|Category=BR-f02-import-excel-016|Category=BR-f02-import-excel-017|Category=BR-f02-import-excel-018|Category=BR-f02-import-excel-019|Category=BR-f02-import-excel-020|Category=BR-f02-import-excel-021|Category=BR-f02-import-excel-022)&Category!=web"`
Expected: `Passed: 15`, `Failed: 0` (scenario 500 dòng mất vài giây).
Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "Category=BR-f01-p1-tai-khoan-hoc-vien-022&Category!=web"`
Expected: `Passed: 3`, `Failed: 0` (step `không email nào được gửi tới` vẫn xanh với Quên mật khẩu).
Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed ngoài scenario `@BR-f02-import-excel-023…033` của `import-excel.feature` báo `MissingStepDefinition` (interim rule; scenario BR-f02-import-excel-029 có thể đã xanh vì mọi step của nó đã có).

Kiểm không ghi file ra đĩa (NFR-f02-import-excel-002, review):
Run: `grep -rnE "File\.|Path\.|FileStream|CopyToFile" backend/src/Coaching.Application/Students/Import backend/src/Coaching.Infrastructure/Import`
Expected: không có dòng nào.

- [x] **Step 10: Commit**
```bash
git add backend/Directory.Packages.props backend/src backend/tests/Coaching.TestSupport backend/tests/Coaching.Application.Tests/Students/Import backend/tests/Coaching.Api.IntegrationTests/Infrastructure/ClosedXmlWorkbookReaderTests.cs backend/tests/Coaching.Api.IntegrationTests/Students/ImportRequests.cs backend/tests/Coaching.Api.IntegrationTests/Students/ImportAuthorizationTests.cs backend/tests/Coaching.Acceptance/Support/ImportApi.cs backend/tests/Coaching.Acceptance/Steps/ImportSteps.cs backend/tests/Coaching.Acceptance/Steps/PasswordResetSteps.cs
git commit -F - <<'EOF'
feat(students): import Excel file-level checks, ClosedXML reader, header mapping, import API (BR-f02-import-excel-012..022)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 5: Import từng dòng — lỗi từng ô, trùng email, chuẩn hoá giá trị chọn, ngày/giá, kết quả theo dòng

**Files:**
- Modify: `backend/src/Coaching.Domain/Students/RegistrationOptions.cs` (thay toàn bộ)
- Create: `backend/src/Coaching.Application/Students/Import/ImportCellParser.cs`
- Modify: `backend/src/Coaching.Application/Students/Import/ImportStudentsHandler.cs` (thay toàn bộ bản Task 4)
- Test: `backend/tests/Coaching.Domain.Tests/Students/RegistrationOptionsMatchTests.cs` (C)
- Test: `backend/tests/Coaching.Application.Tests/Students/Import/ImportCellParserTests.cs` (C)
- Test: `backend/tests/Coaching.Api.IntegrationTests/Students/ImportResultTests.cs` (C), `backend/tests/Coaching.Api.IntegrationTests/Students/ImportMidFileFailureTests.cs` (C)
- Test: `backend/tests/Coaching.Acceptance/Support/ImportApi.cs` (M), `backend/tests/Coaching.Acceptance/Steps/ImportRowSteps.cs` (C), `backend/tests/Coaching.Acceptance/Steps/StudentCreateSteps.cs` (M), `backend/tests/Coaching.Acceptance/Steps/PasswordResetSteps.cs` (M — chỉ annotation)

**Interfaces:**
- Consumes:
  - Task 4 — `WorkbookCell/WorkbookRow/WorkbookSheet`, `IWorkbookReader`, `InvalidWorkbookException`, `StudentImportColumns` (`Email`, `FullName`, `EmailHeader`, `FullNameHeader`, `FieldFor`, `DisplayHeader`), `ImportCellErrorDto/ImportRowDto/ImportResultDto`, `ImportErrors`, `ImportStudentsHandler.MaxBytes/MaxRows`, endpoint `POST /api/admin/students/import`; test: `ImportWorkbook` (`Standard`, `AddRow`, `AddValidRow`, `AddValidRows`, `Set`, `EmailHeader`, `FullNameHeader`, `AgeHeader`, `TransferDateHeader`, `PriceHeader`), `ImportApi` (`Workbook`, `LastFile`, `StudentsBefore`, `ImportAsync`, `ImportOkAsync`, `OkResult`, `Row`, `ProfileOfRowAsync`, `CountStudentsAsync`), `ImportRequests` (`NewEmail`, `PostAsync`, `StudentExistsAsync`), step `When coach import file đó`, `Then không hồ sơ nào được tạo`, `Then (\d+) hồ sơ học viên mới được tạo`, `Given một file Excel có (\d+) dòng học viên hợp lệ`, `Given một file Excel có 1 dòng học viên hợp lệ với email "…"`; `PasswordResetSteps.NoMailTo` đã nhận 200.
  - Task 1 — `RegistrationForm.Validate` gom mọi lỗi; `RegistrationFieldError.Message` là lý do tiếng Việt không kèm tên mục, ít nhất: tuổi ngoài khoảng `phải từ 10 đến 100`, link ảnh sai chứa `link ảnh không hợp lệ`, ngày chuyển khoản sau hôm nay chứa `không được sau hôm nay`; chiều cao ngoài 100–250 là lỗi.
  - Task 3 — `StudentRegistrar.RegisterAsync` (trùng email với hồ sơ hoặc tài khoản coach → `StudentErrors.EmailTaken`; mỗi lần gọi một transaction).
  - F02/F01-P1 — `Student.FullNameMaxLength`, `RegistrationFields.All` (Label, Code, Required), `StudentErrors.EmailTaken`, `IStudentAccounts` + `StudentAccounts` (internal, Infrastructure), `StudentApi` (`Ids`, `GetAsync`, `ListAsync`, `MoveClockToAsync`, `AccountStatuses`, `CoachingStatuses`), `StepText` (`ParseDate`, `VietnamTime`, `VietnamDate`, `Quoted`), step `Given email "…" đã thuộc về …` (StudentCreateSteps — mở rộng), `Then không email nào được gửi tới "…"` (PasswordResetSteps — dùng lại).
- Produces:
  - Domain: `RegistrationOptions.Match(string fieldCode, string? raw) → string?`, `RegistrationOptions.KnownSpellings` (mã mục → cách viết → giá trị chuẩn, đúng bảng tech-design §3); bỏ `RegistrationOptions.TrainingYears`, `RegistrationOptions.BodyFatMethods`.
  - Application: `ImportCellParser.TryInteger/TryDecimal/TryPrice/TryDate` (đúng chữ ký skeleton); `ImportStudentsHandler` bản cuối — lý do theo dòng: `bắt buộc`, `email không hợp lệ`, `email đã có hồ sơ`, `trùng email với dòng {n}`, `không thuộc danh sách`, `tối đa 100 ký tự` (họ tên), lý do của parser và của `RegistrationFieldError.Message`.
  - Test: `ImportApi.UsersBefore/ProfilesBefore/UsersSnapshotAsync`; step `Given hôm nay là "…"` (không có hậu tố "theo giờ Việt Nam").

**Owns:** `@BR-f02-import-excel-023`, `@BR-f02-import-excel-024`, `@BR-f02-import-excel-025`, `@BR-f02-import-excel-026`, `@BR-f02-import-excel-027`, `@BR-f02-import-excel-028`, `@BR-f02-import-excel-029`, `@BR-f02-import-excel-030`, `@BR-f02-import-excel-031`, `@BR-f02-import-excel-032`, `@BR-f02-import-excel-033`, `@BR-f02-import-excel-034` | `@ADR-f02-import-excel-006`

> **@BR-f02-import-excel-034:** scenario của rule là `@web` (Task 8). Phía API được chốt bằng integration test `ImportResultTests` (5 dòng: 3 hợp lệ, 1 trùng email đã có, 1 tuổi sai → `created 3`, `skipped 2`, lý do kèm số dòng Excel; mỗi dòng kết quả chỉ có `row/email/status/studentId/errors` — NFR-f02-import-excel-002).
>
> **@BR-f02-import-excel-033 — dừng giữa chừng (seed LOẠI):** `ImportMidFileFailureTests` thay `IStudentAccounts` bằng bản ném lỗi ở dòng thứ 3 → request 500, hai hồ sơ đầu còn, dòng lỗi và dòng sau không có. Test này xanh ngay từ Task 4 (mỗi dòng đã một transaction qua `StudentRegistrar`) — nó chốt hành vi, không đổi code.
>
> **"Hôm nay" trong scenario ngày chuyển khoản hợp lệ (BR-f02-import-excel-031):** đồng hồ acceptance bắt đầu 01/11/2026, còn ví dụ dùng 12/11 và 25/11/2026. Step dựng file tự tua "hôm nay" tới đúng ngày đó (09:00 giờ VN) nếu đồng hồ còn sớm hơn — để ngày không bị coi là sau hôm nay (BR-f02-ho-so-hoc-vien-010).

- [x] **Step 1: Viết Domain test `RegistrationOptions.Match` — kỳ vọng FAIL**

`backend/tests/Coaching.Domain.Tests/Students/RegistrationOptionsMatchTests.cs`:
```csharp
using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class RegistrationOptionsMatchTests
{
    // @BR-f02-import-excel-030 @ADR-f02-import-excel-006: chỉ khác hoa/thường, khoảng trắng, kiểu dấu gạch, dấu "/" → giá trị chuẩn
    [Theory]
    [InlineData("activityLevel", "Ít vận động (Ngồi nhiều)", "Ít vận động (ngồi nhiều)")]
    [InlineData("sleepDuration", "5-6 tiếng", "5–6 tiếng")]
    [InlineData("sleepDuration", "5 — 6 tiếng", "5–6 tiếng")]
    [InlineData("pack", "pack 1", "Pack 1")]
    [InlineData("pack", "  PACK   2 ", "Pack 2")]
    [InlineData("programStyle", "Push/Pull/Legs", "Push / Pull / Legs")]
    [InlineData("programStyle", "upper lower", "Upper / Lower")]
    [InlineData("sessionsPerWeek", "3buổi", "3 buổi")]
    public void Spelling_variant_matches_canonical_option(string field, string raw, string canonical) =>
        Assert.Equal(canonical, RegistrationOptions.Match(field, raw));

    // @BR-f02-import-excel-030: bảng cách viết đã biết (tech-design §3)
    [Theory]
    [InlineData("programStyle", "PUSH PULL LEG", "Push / Pull / Legs")]
    [InlineData("programStyle", "PUSH PULL LEGS", "Push / Pull / Legs")]
    [InlineData("programStyle", "PPL", "Push / Pull / Legs")]
    [InlineData("programStyle", "UPPER LOWER", "Upper / Lower")]
    [InlineData("programStyle", "FULLBODY", "Full body")]
    [InlineData("sleepDuration", "< 5 tiếng", "Dưới 5 tiếng")]
    [InlineData("sleepDuration", "> 8 tiếng", "Trên 8 tiếng")]
    public void Known_spelling_matches_canonical_option(string field, string raw, string canonical) =>
        Assert.Equal(canonical, RegistrationOptions.Match(field, raw));

    // @ADR-f02-import-excel-006: mọi giá trị đích trong bảng cách viết đã biết là một lựa chọn có thật
    [Fact]
    public void Known_spellings_point_to_existing_options()
    {
        foreach (var (field, spellings) in RegistrationOptions.KnownSpellings)
        {
            foreach (var canonical in spellings.Values)
                Assert.Equal(canonical, RegistrationOptions.Match(field, canonical));
        }
    }

    // @BR-f02-import-excel-030: không đưa về được → null (ô không hợp lệ)
    [Theory]
    [InlineData("stressLevel", "Cao quá")]
    [InlineData("activityLevel", "Cực kì năng động (Ví dụ: Làm những việc hoạt động chân tay nhiều)")]
    [InlineData("pack", "Pack 20 Tuần")]
    [InlineData("unknownField", "Pack 1")]
    [InlineData("pack", "   ")]
    public void Unmatched_value_returns_null(string field, string raw) =>
        Assert.Null(RegistrationOptions.Match(field, raw));

    // @ADR-f02-import-excel-006: giá trị chuẩn khớp chính nó (form nhập tay không đổi hành vi)
    [Fact]
    public void Every_option_matches_itself()
    {
        var lists = new (string Field, IReadOnlyList<string> Options)[]
        {
            ("pack", RegistrationOptions.Packs),
            ("foodBudgetReadiness", RegistrationOptions.FoodBudgetReadiness),
            ("activityLevel", RegistrationOptions.ActivityLevels),
            ("stressLevel", RegistrationOptions.StressLevels),
            ("sleepDuration", RegistrationOptions.SleepDurations),
            ("sessionsPerWeek", RegistrationOptions.SessionsPerWeek),
            ("programStyle", RegistrationOptions.ProgramStyles),
        };
        foreach (var (field, options) in lists)
        {
            foreach (var option in options)
                Assert.Equal(option, RegistrationOptions.Match(field, option));
        }
    }
}
```

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~RegistrationOptionsMatchTests"`
Expected: FAIL — build error `CS0117: 'RegistrationOptions' does not contain a definition for 'Match'` (và `'KnownSpellings'`).

- [x] **Step 2: Domain — `Match` + `KnownSpellings`, bỏ hai danh sách không còn dùng**

`backend/src/Coaching.Domain/Students/RegistrationOptions.cs` — thay toàn bộ nội dung:
```csharp
using System.Text;

namespace Coaching.Domain.Students;

/// <summary>
/// @BR-f02-ho-so-hoc-vien-009 @ADR-f02-ho-so-hoc-vien-006: danh sách giá trị cho trước, đúng theo form prototype.
/// FE có bản sao chỉ để hiển thị. "Số năm tập luyện" và cách đo mỡ không còn danh sách (@ADR-f02-import-excel-001).
/// </summary>
public static class RegistrationOptions
{
    public static readonly IReadOnlyList<string> Packs = ["Pack 1", "Pack 2", "Pack 3", "Khác"];

    public static readonly IReadOnlyList<string> FoodBudgetReadiness = ["Sẵn sàng", "Đang cân nhắc", "Chưa sẵn sàng"];

    public static readonly IReadOnlyList<string> ActivityLevels =
        ["Ít vận động (ngồi nhiều)", "Nhẹ", "Vừa", "Nhiều", "Rất nhiều (lao động chân tay)"];

    public static readonly IReadOnlyList<string> StressLevels = ["Thấp", "Vừa", "Cao", "Rất cao"];

    public static readonly IReadOnlyList<string> SleepDurations =
        ["Dưới 5 tiếng", "5–6 tiếng", "6–7 tiếng", "7–8 tiếng", "Trên 8 tiếng"];

    public static readonly IReadOnlyList<string> SessionsPerWeek = ["2 buổi", "3 buổi", "4 buổi", "5 buổi", "6 buổi", "7 buổi"];

    public static readonly IReadOnlyList<string> ProgramStyles =
        ["Push / Pull / Legs", "Upper / Lower", "Full body", "Mỗi buổi một nhóm cơ", "Để coach chọn", "Khác"];

    // @ADR-f02-import-excel-006: mã mục có danh sách → danh sách (khai báo SAU các danh sách — khởi tạo static theo thứ tự)
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> OptionsByField =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["pack"] = Packs,
            ["foodBudgetReadiness"] = FoodBudgetReadiness,
            ["activityLevel"] = ActivityLevels,
            ["stressLevel"] = StressLevels,
            ["sleepDuration"] = SleepDurations,
            ["sessionsPerWeek"] = SessionsPerWeek,
            ["programStyle"] = ProgramStyles,
        };

    /// <summary>
    /// @BR-f02-import-excel-030 @ADR-f02-import-excel-006: cách viết đã biết (tech-design §3 "Bảng cách viết đã biết",
    /// PO duyệt) — mã mục → (cách viết trong file → giá trị chuẩn). So khớp sau chuẩn hoá như <see cref="Match"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> KnownSpellings { get; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["programStyle"] = new Dictionary<string, string>
            {
                ["PUSH PULL LEG"] = "Push / Pull / Legs",
                ["PUSH PULL LEGS"] = "Push / Pull / Legs",
                ["PPL"] = "Push / Pull / Legs",
                ["UPPER LOWER"] = "Upper / Lower",
                ["FULLBODY"] = "Full body",
            },
            ["sleepDuration"] = new Dictionary<string, string>
            {
                ["< 5 tiếng"] = "Dưới 5 tiếng",
                ["> 8 tiếng"] = "Trên 8 tiếng",
            },
        };

    /// <summary>
    /// @BR-f02-import-excel-030 @ADR-f02-import-excel-006: đưa giá trị trong file import về giá trị chuẩn của mục
    /// <paramref name="fieldCode"/> — so sau khi chuẩn hoá (chữ thường, bỏ khoảng trắng, đồng nhất - – —, bỏ "/"),
    /// rồi so với bảng cách viết đã biết. Null = không đưa về được (ô không hợp lệ), giá trị trống hoặc mục không có danh sách.
    /// Chỉ import dùng; form nhập tay vẫn kiểm đúng nguyên văn.
    /// </summary>
    public static string? Match(string fieldCode, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !OptionsByField.TryGetValue(fieldCode, out var options))
            return null;

        var key = Key(raw);
        foreach (var option in options)
        {
            if (Key(option) == key)
                return option;
        }

        if (KnownSpellings.TryGetValue(fieldCode, out var spellings))
        {
            foreach (var (spelling, canonical) in spellings)
            {
                if (Key(spelling) == key)
                    return canonical;
            }
        }
        return null;
    }

    private static string Key(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormC).ToLowerInvariant())
        {
            if (char.IsWhiteSpace(c) || c == '/')
                continue;
            builder.Append(c is '–' or '—' ? '-' : c);
        }
        return builder.ToString();
    }
}
```

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~RegistrationOptionsMatchTests"`
Expected: `Passed: 22`, `Failed: 0`.
Run: `grep -rn "RegistrationOptions.TrainingYears\|RegistrationOptions.BodyFatMethods" backend/src backend/tests`
Expected: không có dòng nào (Task 1 đã đổi Số năm tập sang ô chữ và bỏ cách đo mỡ).
Run: `cd backend && dotnet test tests/Coaching.Domain.Tests`
Expected: `Failed: 0`.

- [x] **Step 3: Viết unit test đọc ô số / ngày / giá — kỳ vọng FAIL**

`backend/tests/Coaching.Application.Tests/Students/Import/ImportCellParserTests.cs`:
```csharp
using System.Globalization;
using Coaching.Application.Students.Import;

namespace Coaching.Application.Tests.Students.Import;

public sealed class ImportCellParserTests
{
    private static readonly WorkbookCell Blank = new(null, null, null);

    private static WorkbookCell Text(string text) => new(text, null, null);

    private static WorkbookCell Number(double number) => new(number.ToString(CultureInfo.InvariantCulture), number, null);

    private static WorkbookCell Date(DateTime date) =>
        new(date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), null, date);

    [Fact]
    public void Blank_cell_is_accepted_as_empty_value()
    {
        Assert.True(ImportCellParser.TryInteger(Blank, out var integer, out _));
        Assert.Null(integer);
        Assert.True(ImportCellParser.TryDecimal(Blank, out var number, out _));
        Assert.Null(number);
        Assert.True(ImportCellParser.TryPrice(Blank, out var price, out _));
        Assert.Null(price);
        Assert.True(ImportCellParser.TryDate(Blank, out var date, out _));
        Assert.Null(date);
    }

    // @BR-f02-import-excel-031: ô ngày của Excel
    [Fact]
    public void Excel_date_cell_is_accepted()
    {
        Assert.True(ImportCellParser.TryDate(Date(new DateTime(2026, 11, 12, 9, 30, 0)), out var value, out var error));
        Assert.Equal(new DateOnly(2026, 11, 12), value);
        Assert.Null(error);
    }

    // @BR-f02-import-excel-031: chữ dd/MM/yyyy
    [Fact]
    public void Day_month_year_text_is_accepted()
    {
        Assert.True(ImportCellParser.TryDate(Text("25/11/2026"), out var value, out _));
        Assert.Equal(new DateOnly(2026, 11, 25), value);
    }

    // @BR-f02-import-excel-031
    [Theory]
    [InlineData("11/25/2026")]
    [InlineData("hôm qua")]
    [InlineData("2026-11-25")]
    [InlineData("25/11/26")]
    public void Other_date_text_is_invalid(string text)
    {
        Assert.False(ImportCellParser.TryDate(Text(text), out var value, out var error));
        Assert.Null(value);
        Assert.False(string.IsNullOrEmpty(error));
    }

    // @BR-f02-import-excel-031: ô số (không phải ô ngày) không phải ngày
    [Fact]
    public void Number_cell_is_not_a_date()
    {
        Assert.False(ImportCellParser.TryDate(Number(46345.91), out _, out var error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    // @BR-f02-import-excel-032: số với dấu chấm phân cách nghìn, có thể kèm "đ" / "VNĐ"
    [Theory]
    [InlineData("10.000.000đ")]
    [InlineData("10.000.000 VNĐ")]
    [InlineData("10.000.000")]
    [InlineData("10000000")]
    [InlineData("10000000 đ")]
    public void Price_text_is_accepted(string text)
    {
        Assert.True(ImportCellParser.TryPrice(Text(text), out var value, out var error));
        Assert.Equal(10_000_000L, value);
        Assert.Null(error);
    }

    // @BR-f02-import-excel-032: ô số nguyên
    [Fact]
    public void Whole_number_price_cell_is_accepted()
    {
        Assert.True(ImportCellParser.TryPrice(Number(10_000_000), out var value, out _));
        Assert.Equal(10_000_000L, value);
    }

    // @BR-f02-import-excel-032
    [Theory]
    [InlineData("10tr")]
    [InlineData("10000000,5")]
    [InlineData("-100")]
    [InlineData("10.00.000")]
    [InlineData("1.0000")]
    public void Other_price_text_is_invalid(string text)
    {
        Assert.False(ImportCellParser.TryPrice(Text(text), out var value, out var error));
        Assert.Null(value);
        Assert.False(string.IsNullOrEmpty(error));
    }

    // @BR-f02-import-excel-032
    [Fact]
    public void Fractional_price_cell_is_invalid()
    {
        Assert.False(ImportCellParser.TryPrice(Number(10_000_000.5), out _, out var error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    // @BR-f02-import-excel-023: ô số sai kiểu là ô không hợp lệ (tuổi là số nguyên)
    [Fact]
    public void Integer_reads_whole_number_cell_or_digits_and_rejects_others()
    {
        Assert.True(ImportCellParser.TryInteger(Number(21), out var fromCell, out _));
        Assert.Equal(21, fromCell);
        Assert.True(ImportCellParser.TryInteger(Text("21"), out var fromText, out _));
        Assert.Equal(21, fromText);
        Assert.False(ImportCellParser.TryInteger(Number(21.5), out _, out var fraction));
        Assert.False(string.IsNullOrEmpty(fraction));
        Assert.False(ImportCellParser.TryInteger(Text("hai mươi"), out _, out var words));
        Assert.False(string.IsNullOrEmpty(words));
    }

    // @BR-f02-import-excel-023: số đo nhận ô số hoặc chữ có dấu phẩy / chấm thập phân
    [Theory]
    [InlineData("72,5", "72.5")]
    [InlineData("72.5", "72.5")]
    [InlineData("170", "170")]
    public void Decimal_accepts_comma_or_dot(string text, string expected)
    {
        Assert.True(ImportCellParser.TryDecimal(Text(text), out var value, out _));
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), value);
    }

    // @BR-f02-import-excel-023
    [Theory]
    [InlineData("170cm")]
    [InlineData("1,234.5")]
    public void Decimal_rejects_other_text(string text)
    {
        Assert.False(ImportCellParser.TryDecimal(Text(text), out var value, out var error));
        Assert.Null(value);
        Assert.False(string.IsNullOrEmpty(error));
    }
}
```

Run: `cd backend && dotnet test tests/Coaching.Application.Tests --filter "FullyQualifiedName~ImportCellParserTests"`
Expected: FAIL — build error `CS0103: The name 'ImportCellParser' does not exist in the current context`.

- [x] **Step 4: Application — `ImportCellParser`**

`backend/src/Coaching.Application/Students/Import/ImportCellParser.cs`:
```csharp
using System.Globalization;
using System.Text.RegularExpressions;

namespace Coaching.Application.Students.Import;

/// <summary>
/// Đọc ô số / ngày / giá của file import. Ô trống → true, value null (mục để trống). Sai dạng → false + lý do tiếng Việt
/// (@BR-f02-import-excel-023: ô sai làm bỏ cả dòng). Khoảng giá trị (tuổi, số đo, giá ≥ 0, ngày ≤ hôm nay) do
/// RegistrationForm.Validate kiểm.
/// </summary>
public static partial class ImportCellParser
{
    private const string NotInteger = "phải là số nguyên";
    private const string NotNumber = "phải là số";
    private const string NotPrice = "phải là số nguyên, có thể có dấu chấm phân cách nghìn và đuôi \"đ\" hoặc \"VNĐ\"";
    private const string NotDate = "phải là ngày dạng ngày/tháng/năm (dd/MM/yyyy)";

    /// <summary>Ngoài khoảng này double không còn chính xác tới hàng đơn vị.</summary>
    private const double MaxWholeNumber = 1e15;

    public static bool TryInteger(WorkbookCell c, out int? value, out string? error)
    {
        ArgumentNullException.ThrowIfNull(c);
        value = null;
        error = null;
        if (c.Number is { } number)
        {
            if (number != Math.Floor(number) || number < int.MinValue || number > int.MaxValue)
                return Fail(NotInteger, out error);
            value = (int)number;
            return true;
        }
        if (c.Date is not null)
            return Fail(NotInteger, out error);
        if (c.Text is null)
            return true;
        if (!int.TryParse(c.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed))
            return Fail(NotInteger, out error);
        value = parsed;
        return true;
    }

    /// <summary>Ô số, hoặc chữ số thập phân với dấu phẩy hoặc dấu chấm ("72,5" = "72.5").</summary>
    public static bool TryDecimal(WorkbookCell c, out decimal? value, out string? error)
    {
        ArgumentNullException.ThrowIfNull(c);
        value = null;
        error = null;
        if (c.Number is { } number)
        {
            if (Math.Abs(number) > MaxWholeNumber)
                return Fail(NotNumber, out error);
            value = (decimal)number;
            return true;
        }
        if (c.Date is not null)
            return Fail(NotNumber, out error);
        if (c.Text is null)
            return true;
        if (!decimal.TryParse(c.Text.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var parsed))
            return Fail(NotNumber, out error);
        value = parsed;
        return true;
    }

    /// <summary>@BR-f02-import-excel-032: ô số nguyên, hoặc chữ gồm số với dấu chấm phân cách nghìn, có thể kèm "đ" / "VNĐ".</summary>
    public static bool TryPrice(WorkbookCell c, out long? value, out string? error)
    {
        ArgumentNullException.ThrowIfNull(c);
        value = null;
        error = null;
        if (c.Number is { } number)
        {
            if (number != Math.Floor(number) || Math.Abs(number) > MaxWholeNumber)
                return Fail(NotPrice, out error);
            value = (long)number;
            return true;
        }
        if (c.Date is not null)
            return Fail(NotPrice, out error);
        if (c.Text is null)
            return true;
        var match = PriceText().Match(c.Text);
        if (!match.Success
            || !long.TryParse(match.Groups["digits"].Value.Replace(".", string.Empty, StringComparison.Ordinal),
                NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
            return Fail(NotPrice, out error);
        value = parsed;
        return true;
    }

    /// <summary>@BR-f02-import-excel-031: ô ngày của Excel, hoặc chữ dd/MM/yyyy.</summary>
    public static bool TryDate(WorkbookCell c, out DateOnly? value, out string? error)
    {
        ArgumentNullException.ThrowIfNull(c);
        value = null;
        error = null;
        if (c.Date is { } date)
        {
            value = DateOnly.FromDateTime(date);
            return true;
        }
        if (c.Number is not null)
            return Fail(NotDate, out error);
        if (c.Text is null)
            return true;
        if (!DateOnly.TryParseExact(c.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return Fail(NotDate, out error);
        value = parsed;
        return true;
    }

    private static bool Fail(string reason, out string? error)
    {
        error = reason;
        return false;
    }

    [GeneratedRegex(@"^(?<digits>[0-9]{1,3}(?:\.[0-9]{3})+|[0-9]+)\s*(?:đ|vnđ)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PriceText();
}
```

Run: `cd backend && dotnet test tests/Coaching.Application.Tests --filter "FullyQualifiedName~ImportCellParserTests"`
Expected: `Passed: 27`, `Failed: 0`.

- [x] **Step 5: Viết test mức dòng — acceptance (bind scenario BR-023…033) + integration (BR-033, BR-034) — kỳ vọng FAIL**

`backend/tests/Coaching.Acceptance/Support/ImportApi.cs` — sửa 3 chỗ:

(a) thêm ngay sau property `public ImportResultDto? Result { get; private set; }`:
```csharp

    /// <summary>@BR-f02-import-excel-025: mọi tài khoản (kèm role) ngay trước lần import gần nhất.</summary>
    public IReadOnlyList<string> UsersBefore { get; private set; } = [];

    /// <summary>@BR-f02-import-excel-025: hồ sơ đã biết tên trong scenario (StudentApi.Ids), JSON ngay trước lần import gần nhất.</summary>
    public Dictionary<string, string> ProfilesBefore { get; } = new();
```

(b) trong `ImportAsync`, thêm ngay sau dòng `StudentsBefore = await CountStudentsAsync();`:
```csharp
        UsersBefore = await UsersSnapshotAsync();
        ProfilesBefore.Clear();
        foreach (var name in api.Ids.Keys.ToList())
            ProfilesBefore[name] = JsonSerializer.Serialize(await api.GetAsync(name));
```

(c) thêm ngay trước method `AssertRejectedAsync`:
```csharp
    /// <summary>Mỗi tài khoản một dòng: Id, email, họ tên, băm mật khẩu, security stamp, các role.</summary>
    public async Task<IReadOnlyList<string>> UsersSnapshotAsync()
    {
        await using var scope = ctx.Host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = await db.Users.AsNoTracking().ToListAsync();
        var roles = await db.UserRoles.AsNoTracking().ToListAsync();
        return users
            .OrderBy(u => u.Id)
            .Select(u => $"{u.Id}|{u.Email}|{u.FullName}|{u.PasswordHash}|{u.SecurityStamp}|" +
                string.Join(",", roles.Where(r => r.UserId == u.Id).Select(r => r.RoleId).Order()))
            .ToList();
    }

```

`backend/tests/Coaching.Acceptance/Steps/StudentCreateSteps.cs` — trong `EmailBelongsTo`:
- thay hai dòng
```csharp
    // @BR-f02-ho-so-hoc-vien-001
    [Given(@"^email ""([^""]*)"" đã thuộc về (một hồ sơ đang coaching|tài khoản coach|một hồ sơ đã kết thúc coaching)$")]
```
bằng
```csharp
    // @BR-f02-ho-so-hoc-vien-001 @BR-f02-import-excel-025 ("một hồ sơ học viên" = hồ sơ đang coaching)
    [Given(@"^email ""([^""]*)"" đã thuộc về (một hồ sơ đang coaching|một hồ sơ học viên|tài khoản coach|một hồ sơ đã kết thúc coaching)$")]
```
- thay dòng `            case "một hồ sơ đang coaching":` bằng:
```csharp
            case "một hồ sơ đang coaching":
            case "một hồ sơ học viên":
```

`backend/tests/Coaching.Acceptance/Steps/PasswordResetSteps.cs` — thêm ngay trên dòng `[Then(@"^không email nào được gửi tới ""([^""]*)""$")]`:
```csharp
    // @BR-f02-import-excel-029 (dùng lại): import không gửi email nào cho học viên
```

`backend/tests/Coaching.Acceptance/Steps/ImportRowSteps.cs`:
```csharp
using System.Globalization;
using System.Text.Json;
using Coaching.Acceptance.Support;
using Coaching.Application.Students.Import;
using Coaching.Domain.Students;
using Coaching.TestSupport;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

/// <summary>import-excel.feature — mức dòng (BR-f02-import-excel-023…033).</summary>
[Binding]
public sealed class ImportRowSteps(ImportApi import, StudentApi api, AcceptanceContext ctx)
{
    /// <summary>"dòng đó" = dòng dữ liệu duy nhất của file (dòng Excel 2).</summary>
    private const int ThatRow = 2;

    /// <summary>Họ tên hồ sơ do step "email … đã thuộc về một hồ sơ học viên" tạo.</summary>
    private const string OwnerName = "Chủ email";

    // @BR-f02-import-excel-023
    [Given(@"^một file Excel có dòng 2 hợp lệ và dòng 3 có ""([^""]*)"" là ""([^""]*)""$")]
    public void Row2ValidRow3With(string header, string value)
    {
        import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        var row = import.Workbook.AddValidRow("minh@gmail.com", "Trần Văn Minh");
        import.Workbook.Set(row, header, value);
    }

    // @BR-f02-import-excel-023
    [Then(@"^chỉ học viên ở dòng (\d+) được tạo hồ sơ$")]
    public async Task OnlyRowCreated(int excelRow)
    {
        var created = Assert.Single(import.OkResult().Rows, r => r.Status == "Created");
        Assert.Equal(excelRow, created.Row);
        Assert.Equal(import.StudentsBefore + 1, await import.CountStudentsAsync());
    }

    // @BR-f02-import-excel-023: số dòng Excel + tên cột + lý do
    [Then(@"^kết quả import báo dòng (\d+), cột ""([^""]*)"" (.+)$")]
    public void RowColumnReason(int excelRow, string column, string reason) =>
        AssertCellError(import.Row(excelRow), column, reason);

    // @BR-f02-import-excel-023 @BR-f02-import-excel-030 @BR-f02-import-excel-031 @BR-f02-import-excel-032
    [Given(@"^một file Excel có 1 dòng học viên với ""([^""]*)"" là ""([^""]*)""$")]
    public void OneRowWith(string header, string value)
    {
        var row = import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.Set(row, header, value); // "Địa chỉ email" → thay email của dòng
    }

    // @BR-f02-import-excel-023
    [Then(@"^kết quả import báo dòng đó, cột ""([^""]*)"", lý do ""([^""]*)""$")]
    public void ThatRowColumnReason(string column, string reason) => AssertCellError(import.Row(ThatRow), column, reason);

    // @BR-f02-import-excel-023: "hôm nay" = 09:00 giờ Việt Nam của ngày đó
    [Given(@"^hôm nay là ""([^""]*)""$")]
    public Task TodayIs(string date) => api.MoveClockToAsync(StepText.VietnamTime(date, 9, 0));

    // @BR-f02-import-excel-023: một dòng có hai ô sai
    [Given(@"^một file Excel có 1 dòng học viên với ""([^""]*)"" là ""([^""]*)"" và ""([^""]*)"" là ""([^""]*)""$")]
    public void OneRowWithTwo(string firstHeader, string firstValue, string secondHeader, string secondValue)
    {
        var row = import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.Set(row, firstHeader, firstValue).Set(row, secondHeader, secondValue);
    }

    // @BR-f02-import-excel-023: báo cả hai ô
    [Then(@"^kết quả import báo dòng đó có (\d+) ô sai: (.+)$")]
    public void ThatRowHasErrors(int count, string columns)
    {
        var row = import.Row(ThatRow);
        Assert.Equal("Skipped", row.Status);
        Assert.Equal(count, row.Errors.Count);
        Assert.Equal(StepText.Quoted(columns).Order(), row.Errors.Select(e => e.Column).Order());
    }

    // @BR-f02-import-excel-024
    [Given("một file Excel có 1 dòng học viên có email nhưng để trống họ tên")]
    public void RowWithoutName() => import.Workbook.AddRow("lan@gmail.com", null);

    // @BR-f02-import-excel-024
    [Then(@"^kết quả import báo dòng đó thiếu ""([^""]*)""$")]
    public void ThatRowMissing(string column)
    {
        var row = import.Row(ThatRow);
        Assert.Equal("Skipped", row.Status);
        Assert.Contains(row.Errors, e => e.Column.StartsWith(column, StringComparison.Ordinal) && e.Reason == "bắt buộc");
    }

    // @BR-f02-import-excel-025: email có thể nằm trong ngoặc kép để giữ khoảng trắng đầu/cuối: "" quan@gmail.com ""
    [Given(@"^một file Excel có 1 dòng học viên với email ""(.*)""$")]
    public void OneRowWithEmail(string email) => import.Workbook.AddValidRow(Unquote(email), "Phạm Hoàng Quân");

    // @BR-f02-import-excel-025
    [Then("không hồ sơ mới nào được tạo")]
    public async Task NoNewProfile()
    {
        Assert.Equal(import.StudentsBefore, await import.CountStudentsAsync());
        Assert.Equal(0, import.OkResult().Created);
    }

    // @BR-f02-import-excel-025
    [Then("kết quả import báo dòng đó trùng email đã có")]
    public void ThatRowDuplicatesExisting() =>
        AssertCellError(import.Row(ThatRow), ImportWorkbook.EmailHeader, "email đã có hồ sơ");

    // @BR-f02-import-excel-025: hồ sơ / tài khoản có sẵn không bị thay đổi
    [Then(@"^thông tin của (một hồ sơ học viên|tài khoản coach) không thay đổi$")]
    public async Task OwnerUnchanged(string owner)
    {
        var usersNow = await import.UsersSnapshotAsync();
        Assert.All(import.UsersBefore, user => Assert.Contains(user, usersNow));
        if (owner == "một hồ sơ học viên")
            Assert.Equal(import.ProfilesBefore[OwnerName], JsonSerializer.Serialize(await api.GetAsync(OwnerName)));
    }

    // @BR-f02-import-excel-025
    [Given(@"^coach đã import thành công một file Excel có (\d+) dòng học viên$")]
    public async Task AlreadyImported(int count)
    {
        import.Workbook.AddValidRows(count);
        Assert.Equal(count, (await import.ImportOkAsync()).Created);
    }

    // @BR-f02-import-excel-025
    [When("coach import lại đúng file đó")]
    public Task ImportSameFileAgain() => import.ImportAsync(import.LastFile);

    // @BR-f02-import-excel-025
    [Then(@"^kết quả import báo cả (\d+) dòng trùng email đã có$")]
    public void AllRowsDuplicateExisting(int count)
    {
        var result = import.OkResult();
        Assert.Equal(count, result.Skipped);
        Assert.Equal(count, result.Rows.Count);
        Assert.All(result.Rows, r => AssertCellError(r, ImportWorkbook.EmailHeader, "email đã có hồ sơ"));
    }

    // @BR-f02-import-excel-026
    [Given(@"^một file Excel có dòng (\d+) và dòng (\d+) cùng email ""([^""]*)"", khác hoa/thường$")]
    public void SameEmailOnTwoRows(int first, int second, string email)
    {
        for (var row = 2; row <= second; row++)
        {
            var value = row == first ? email : row == second ? email.ToUpperInvariant() : $"hocvien{row}@gmail.com";
            import.Workbook.AddValidRow(value, $"Học viên dòng {row}");
        }
    }

    // @BR-f02-import-excel-026
    [Then(@"^chỉ một hồ sơ ""([^""]*)"" được tạo, theo dòng (\d+)$")]
    public async Task OnlyOneProfileFromRow(string email, int excelRow)
    {
        var row = import.Row(excelRow);
        Assert.Equal("Created", row.Status);
        var profile = Assert.Single(await api.ListAsync(null), s => s.Email == email);
        Assert.Equal(row.StudentId, profile.Id);
        Assert.Equal($"Học viên dòng {excelRow}", profile.FullName);
    }

    // @BR-f02-import-excel-026
    [Then(@"^kết quả import báo dòng (\d+) trùng email với dòng (\d+)$")]
    public void RowDuplicatesRow(int excelRow, int first) =>
        AssertCellError(import.Row(excelRow), ImportWorkbook.EmailHeader, $"trùng email với dòng {first}");

    // @BR-f02-import-excel-027
    [Given("một file Excel có 1 dòng học viên chỉ có email và họ tên")]
    public void OnlyEmailAndName()
    {
        import.Workbook = new ImportWorkbook(ImportWorkbook.EmailHeader, ImportWorkbook.FullNameHeader);
        import.Workbook.AddRow("lan@gmail.com", "Nguyễn Thị Lan");
    }

    // @BR-f02-import-excel-027
    [Then("hồ sơ đó cho biết còn thiếu các mục bắt buộc")]
    public async Task ProfileReportsMissingRequired()
    {
        var profile = await import.ProfileOfRowAsync(ThatRow);
        Assert.Equal(RegistrationFields.All.Where(f => f.Required).Select(f => f.Code), profile.MissingRequired);
    }

    // @BR-f02-import-excel-028
    [Then("hồ sơ vừa tạo có tài khoản Chờ kích hoạt và coaching Đang coaching")]
    public async Task PendingActivationAndActive()
    {
        var profile = await import.ProfileOfRowAsync(ThatRow);
        Assert.Equal(StudentApi.AccountStatuses["Chờ kích hoạt"], profile.AccountStatus);
        Assert.Equal(StudentApi.CoachingStatuses["Đang coaching"], profile.CoachingStatus);
    }

    // @BR-f02-import-excel-030: mục ghi nhận đúng giá trị chuẩn trong danh sách
    [Then(@"^hồ sơ vừa tạo ghi nhận ""([^""]*)"" là ""([^""]*)""$")]
    public async Task CreatedProfileRecords(string label, string expected)
    {
        var profile = await import.ProfileOfRowAsync(ThatRow);
        var code = RegistrationFields.All.Single(f => f.Label == label).Code;
        var node = JsonSerializer.SerializeToNode(profile.Registration, JsonSerializerOptions.Web)![code];
        Assert.Equal(expected, node?.GetValue<string>());
    }

    // @BR-f02-import-excel-030 @BR-f02-import-excel-031 @BR-f02-import-excel-032
    [Then(@"^kết quả import báo dòng đó, cột ""([^""]*)"" không hợp lệ$")]
    public void ThatRowColumnInvalid(string column)
    {
        var row = import.Row(ThatRow);
        Assert.Equal("Skipped", row.Status);
        Assert.Contains(row.Errors, e => e.Column == column && e.Reason.Length > 0);
    }

    // @BR-f02-import-excel-031: ô ngày của Excel
    [Given(@"^một file Excel có 1 dòng học viên với ngày chuyển khoản là ô ngày Excel (\d\d/\d\d/\d{4})$")]
    public async Task TransferDateCell(string date)
    {
        await EnsureTodayIsOnOrAfterAsync(date);
        var row = import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.Set(row, ImportWorkbook.TransferDateHeader, StepText.ParseDate(date).ToDateTime(TimeOnly.MinValue));
    }

    // @BR-f02-import-excel-031: chữ dd/MM/yyyy
    [Given(@"^một file Excel có 1 dòng học viên với ngày chuyển khoản là chữ ""([^""]*)""$")]
    public async Task TransferDateText(string date)
    {
        await EnsureTodayIsOnOrAfterAsync(date);
        var row = import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.Set(row, ImportWorkbook.TransferDateHeader, date);
    }

    // @BR-f02-import-excel-031
    [Then(@"^hồ sơ vừa tạo có ngày chuyển khoản ""([^""]*)""$")]
    public async Task CreatedProfileTransferDate(string date) =>
        Assert.Equal(StepText.ParseDate(date), (await import.ProfileOfRowAsync(ThatRow)).Registration.TransferDate);

    // @BR-f02-import-excel-032: ô số
    [Given(@"^một file Excel có 1 dòng học viên với giá là ô số (\d+)$")]
    public void PriceNumberCell(long price)
    {
        var row = import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.Set(row, ImportWorkbook.PriceHeader, price);
    }

    // @BR-f02-import-excel-032: chữ
    [Given(@"^một file Excel có 1 dòng học viên với giá là chữ ""([^""]*)""$")]
    public void PriceText(string price)
    {
        var row = import.Workbook.AddValidRow("lan@gmail.com", "Nguyễn Thị Lan");
        import.Workbook.Set(row, ImportWorkbook.PriceHeader, price);
    }

    // @BR-f02-import-excel-032
    [Then(@"^hồ sơ vừa tạo có giá ([\d.]+) đồng$")]
    public async Task CreatedProfilePrice(string price) =>
        Assert.Equal(long.Parse(price.Replace(".", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture),
            (await import.ProfileOfRowAsync(ThatRow)).Registration.Price);

    // @BR-f02-import-excel-033: dòng lỗi (tuổi ngoài 10–100) rải giữa các dòng hợp lệ
    [Given(@"^một file Excel có (\d+) dòng học viên, trong đó (\d+) dòng hợp lệ$")]
    public void RowsSomeInvalid(int total, int valid)
    {
        var invalidCount = total - valid;
        var step = Math.Max(1, total / Math.Max(1, invalidCount));
        var invalid = 0;
        for (var i = 1; i <= total; i++)
        {
            var row = import.Workbook.AddValidRow($"hocvien{i}@gmail.com", $"Học viên {i}");
            if (invalid < invalidCount && i % step == 0)
            {
                import.Workbook.Set(row, ImportWorkbook.AgeHeader, "5");
                invalid++;
            }
        }
        Assert.Equal(invalidCount, invalid);
    }

    private static void AssertCellError(ImportRowDto row, string column, string reason)
    {
        Assert.Equal("Skipped", row.Status);
        Assert.Null(row.StudentId);
        Assert.Contains(row.Errors, e => e.Column == column && e.Reason.Contains(reason, StringComparison.Ordinal));
    }

    private static string Unquote(string text) =>
        text.Length >= 2 && text[0] == '"' && text[^1] == '"' ? text[1..^1] : text;

    /// <summary>Đồng hồ scenario bắt đầu 01/11/2026 — tua tới ngày chuyển khoản nếu còn sớm hơn (ngày không được sau hôm nay).</summary>
    private async Task EnsureTodayIsOnOrAfterAsync(string date)
    {
        if (StepText.VietnamDate(ctx.Clock.GetUtcNow()) < StepText.ParseDate(date))
            await api.MoveClockToAsync(StepText.VietnamTime(date, 9, 0));
    }
}
```

`backend/tests/Coaching.Api.IntegrationTests/Students/ImportResultTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Application.Students.Import;
using Coaching.TestSupport;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class ImportResultTests(ApiFactory factory)
{
    // @BR-f02-import-excel-034: 5 dòng — 3 hợp lệ, 1 trùng email đã có, 1 tuổi sai → "Đã tạo 3, bỏ qua 2" kèm lý do theo số dòng Excel
    // @BR-f02-import-excel-023 @BR-f02-import-excel-025
    [Fact]
    public async Task Result_reports_counts_and_reason_per_excel_row()
    {
        var admin = await factory.AdminClientAsync();
        var existing = ImportRequests.NewEmail();
        Assert.Equal(HttpStatusCode.Created,
            (await admin.PostAsJsonAsync(StudentRequests.Url, new { fullName = "Đã có hồ sơ", email = existing })).StatusCode);
        var workbook = ImportWorkbook.Standard();
        workbook.AddValidRow(ImportRequests.NewEmail(), "Học viên 1");                 // dòng 2
        workbook.AddValidRow(existing.ToUpperInvariant(), "Trùng email đã có");        // dòng 3
        workbook.AddValidRow(ImportRequests.NewEmail(), "Học viên 2");                 // dòng 4
        var wrongAge = workbook.AddValidRow(ImportRequests.NewEmail(), "Tuổi sai");    // dòng 5
        workbook.Set(wrongAge, ImportWorkbook.AgeHeader, 5);
        workbook.AddValidRow(ImportRequests.NewEmail(), "Học viên 3");                 // dòng 6

        var response = await ImportRequests.PostAsync(admin, workbook.ToBytes());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ImportResultDto>(json, JsonSerializerOptions.Web)!;
        Assert.Equal(3, result.Created);
        Assert.Equal(2, result.Skipped);
        Assert.Equal(new[] { 2, 3, 4, 5, 6 }, result.Rows.Select(r => r.Row));

        var duplicate = result.Rows.Single(r => r.Row == 3);
        Assert.Equal("Skipped", duplicate.Status);
        Assert.Equal(existing, duplicate.Email);
        Assert.Equal(new ImportCellErrorDto("Địa chỉ email", "email đã có hồ sơ"), Assert.Single(duplicate.Errors));

        var age = result.Rows.Single(r => r.Row == 5);
        Assert.Equal("Skipped", age.Status);
        var ageError = Assert.Single(age.Errors);
        Assert.Equal("Tuổi (Age)", ageError.Column);
        Assert.Contains("phải từ 10 đến 100", ageError.Reason, StringComparison.Ordinal);

        Assert.All(result.Rows.Where(r => r.Status == "Created"), r =>
        {
            Assert.NotNull(r.StudentId);
            Assert.Empty(r.Errors);
        });

        // NFR-f02-import-excel-002: mỗi dòng kết quả chỉ có số dòng, email, trạng thái, Id hồ sơ và lỗi (cột + lý do)
        string[] allowed = ["row", "email", "status", "studentId", "errors"];
        using var document = JsonDocument.Parse(json);
        foreach (var row in document.RootElement.GetProperty("rows").EnumerateArray())
            Assert.All(row.EnumerateObject().Select(p => p.Name), name => Assert.Contains(name, allowed));
    }
}
```

`backend/tests/Coaching.Api.IntegrationTests/Students/ImportMidFileFailureTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Common;
using Coaching.Infrastructure.Identity;
using Coaching.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class ImportMidFileFailureTests(ApiFactory factory)
{
    // @BR-f02-import-excel-033: quá trình dừng ở dòng 4 (dòng dữ liệu thứ 3) → hồ sơ dòng 2, 3 đã tạo vẫn còn;
    // dòng lỗi và các dòng sau không có hồ sơ (mỗi dòng một transaction)
    [Fact]
    public async Task Profiles_created_before_a_crash_are_kept()
    {
        using var crashing = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            services.RemoveAll<IStudentAccounts>();
            services.AddScoped<StudentAccounts>();
            services.AddScoped<IStudentAccounts>(sp => new CrashOnEmail(sp.GetRequiredService<StudentAccounts>()));
        }));
        var admin = crashing.CreateClient();
        var login = await admin.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(ApiFactory.AdminEmail, ApiFactory.AdminPassword, ClientApp.Admin));
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokens>())!;
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var first = ImportRequests.NewEmail();
        var second = ImportRequests.NewEmail();
        var crash = $"{CrashOnEmail.Prefix}{Guid.NewGuid():N}@test.local";
        var after = ImportRequests.NewEmail();
        var workbook = ImportWorkbook.Standard();
        workbook.AddValidRow(first, "Dòng 2");
        workbook.AddValidRow(second, "Dòng 3");
        workbook.AddValidRow(crash, "Dòng 4 — dừng ở đây");
        workbook.AddValidRow(after, "Dòng 5");

        var response = await ImportRequests.PostAsync(admin, workbook.ToBytes());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(await ImportRequests.StudentExistsAsync(factory, first));
        Assert.True(await ImportRequests.StudentExistsAsync(factory, second));
        Assert.False(await ImportRequests.StudentExistsAsync(factory, crash));
        Assert.False(await ImportRequests.StudentExistsAsync(factory, after));
    }

    /// <summary>Giả lập mất kết nối giữa chừng: tạo tài khoản ném lỗi với email bắt đầu bằng <see cref="Prefix"/>.</summary>
    private sealed class CrashOnEmail(IStudentAccounts inner) : IStudentAccounts
    {
        public const string Prefix = "crash-";

        public Task<Result> CreatePendingAsync(Guid userId, Email email, string fullName,
            CancellationToken cancellationToken = default) =>
            email.Value.StartsWith(Prefix, StringComparison.Ordinal)
                ? throw new InvalidOperationException("Mất kết nối cơ sở dữ liệu giữa chừng (giả lập).")
                : inner.CreatePendingAsync(userId, email, fullName, cancellationToken);
    }
}
```

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f02-import-excel-023|Category=BR-f02-import-excel-024|Category=BR-f02-import-excel-025|Category=BR-f02-import-excel-026|Category=BR-f02-import-excel-027|Category=BR-f02-import-excel-028|Category=BR-f02-import-excel-029|Category=BR-f02-import-excel-030|Category=BR-f02-import-excel-031|Category=BR-f02-import-excel-032|Category=BR-f02-import-excel-033)&Category!=web"`
Expected: FAIL với lỗi assertion (không còn `MissingStepDefinition`/`Ambiguous`): BR-023 (cột "Tuổi (Age)" chưa được đọc → dòng 3 được tạo; lý do email là `Email không hợp lệ.`), BR-024 (lý do `Họ tên là bắt buộc.` thay vì `bắt buộc`), BR-025 (lý do `Email này đã được dùng cho một tài khoản khác.`), BR-026 (thiếu `trùng email với dòng 3`), BR-030/031/032 (cột chọn, ngày, giá chưa được đọc → mục trống / dòng sai vẫn được tạo), BR-033 (10 hồ sơ thay vì 8). Scenario BR-027/028/029 có thể đã PASS.
Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~ImportResultTests|FullyQualifiedName~ImportMidFileFailureTests"`
Expected: `ImportResultTests` FAIL (`Expected: 3, Actual: 4` — dòng tuổi sai được tạo); `ImportMidFileFailureTests` PASS (hành vi có từ Task 4, test chốt BR-f02-import-excel-033).

- [x] **Step 6: Handler bản cuối — lỗi từng ô, trùng email, giá trị chọn, ngày/giá, kết quả**

`backend/src/Coaching.Application/Students/Import/ImportStudentsHandler.cs` — thay toàn bộ nội dung:
```csharp
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
```

- [x] **Step 7: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f02-import-excel-023|Category=BR-f02-import-excel-024|Category=BR-f02-import-excel-025|Category=BR-f02-import-excel-026|Category=BR-f02-import-excel-027|Category=BR-f02-import-excel-028|Category=BR-f02-import-excel-029|Category=BR-f02-import-excel-030|Category=BR-f02-import-excel-031|Category=BR-f02-import-excel-032|Category=BR-f02-import-excel-033)&Category!=web"`
Expected: `Passed: 29`, `Failed: 0`.
Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Import"`
Expected: `Passed: 12` (ImportAuthorizationTests 4, ClosedXmlWorkbookReaderTests 6, ImportResultTests 1, ImportMidFileFailureTests 1), `Failed: 0`.
Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f02-ho-so-hoc-vien-001|Category=BR-f01-p1-tai-khoan-hoc-vien-008|Category=BR-f01-p1-tai-khoan-hoc-vien-022)&Category!=web"`
Expected: `Failed: 0` (step `email … đã thuộc về …` mở rộng và `không email nào được gửi tới` dùng chung vẫn xanh).
Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed — mọi scenario `@api` của `import-excel.feature` (44) và toàn bộ test F00/F01/F02 xanh.

- [x] **Step 8: Commit**
```bash
git add backend/src/Coaching.Domain/Students/RegistrationOptions.cs backend/src/Coaching.Application/Students/Import backend/tests/Coaching.Domain.Tests/Students/RegistrationOptionsMatchTests.cs backend/tests/Coaching.Application.Tests/Students/Import/ImportCellParserTests.cs backend/tests/Coaching.Api.IntegrationTests/Students/ImportResultTests.cs backend/tests/Coaching.Api.IntegrationTests/Students/ImportMidFileFailureTests.cs backend/tests/Coaching.Acceptance/Support/ImportApi.cs backend/tests/Coaching.Acceptance/Steps/ImportRowSteps.cs backend/tests/Coaching.Acceptance/Steps/StudentCreateSteps.cs backend/tests/Coaching.Acceptance/Steps/PasswordResetSteps.cs
git commit -F - <<'EOF'
feat(students): import Excel row validation, duplicate emails, option/date/price parsing, per-row result (BR-f02-import-excel-023..034)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 6: [UI] Form Tạo/Sửa hồ sơ theo Google Form + khối "Thông tin học viên" của Hồ sơ + sửa prototype

Màn hình (tech-design §3 "Thiết kế giao diện (UI)"): **Form Tạo/Sửa hồ sơ** (`/hoc-vien/moi`, `/hoc-vien/:id/sua`, page `AdTaoHoSo`) — "Số năm tập luyện" ô chữ 1 dòng; "Mỡ cơ thể và cách đo" ô chữ 1 dòng, không bắt buộc; "Cân nặng và chiều cao trước khi tập tạ" ô đoạn văn; "Link ảnh toàn thân" 1 ô link (thay 4 ô); bỏ "Lịch rảnh", "Dụng cụ". **Hồ sơ** (`/hoc-vien/:id`, page `AdHoSo`) — khối "Thông tin học viên": Mục tiêu tập luyện, Số buổi tập mỗi tuần, Giờ có thể ăn và giờ đi tập. PO yêu cầu sửa cả prototype (`SECS` của `AdTaoHoSo`, `<dl>` "Thông tin học viên" của `AdHoSo`) để prototype vẫn là nguồn chuẩn.

**Files:**
- Modify: `docs/duobexiuuu-coaching-design.html` (7 chỗ: 4 mục trong `SECS` của `AdTaoHoSo`, 3 dòng `<dt>/<dd>` của `AdHoSo`)
- Re-extract (git-ignored, chỉ để tham chiếu khi dựng màn): `.kv/sdd/proto/AdTaoHoSo.md`, `.kv/sdd/proto/AdHoSo.md`
- Modify: `frontend/projects/admin/src/app/students/registration-fields.ts`, `registration-fields.spec.ts`
- Modify: `frontend/projects/admin/src/app/students/student-form.component.html`, `student-form.component.spec.ts`
- Modify: `frontend/projects/admin/src/app/students/student-profile.component.html`, `student-profile.component.ts`, `student-profile.component.spec.ts`
- Không đổi: `student.models.ts` (`Registration = Record<string, RegistrationValue>` đã là object phẳng theo mã mục — khoá mới/bỏ khoá không đổi kiểu), `student-form.component.ts`, `*.scss` (mục mới dùng lại `.ctl`, `.field.wide`, `dl` có sẵn).

**Interfaces:**
- Consumes: Task 1 — `registration` của `POST/PUT /api/admin/students` và `StudentDetailDto` dùng khoá mới `trainingYears` (chuỗi ≤ 200), `bodyFatAndMethod` (chuỗi ≤ 200, không bắt buộc), `weightHeightBeforeLifting` (chuỗi ≤ 2000, bắt buộc), `bodyPhotoUrl` (URL http/https ≤ 2048, bắt buộc); không còn `availability`, `equipment`, `bodyFatPercent`, `bodyFatMethod`, `weightBeforeLiftingKg`, `heightThenCm`, `photoFrontUrl`, `photoBackUrl`, `photoLeftUrl`, `photoRightUrl`; `missingRequired` trả mã mục mới. F02/F01-P1 — `StudentFormComponent`, `StudentProfileComponent`, `FORM_SECTIONS`, `FIELD_LABEL`, `toPayload`, `toFormValues`.
- Produces: `FieldType` thêm `'url'`; `FORM_SECTIONS` mới (37 mục `registration`, 26 mục bắt buộc kể cả họ tên + email); `FIELD_LABEL['bodyPhotoUrl'] = 'Link ảnh toàn thân'`; `<dl class="info">` trên trang hồ sơ. Task 7 dùng lại trang Tạo hồ sơ (chèn card import); Task 8 dùng nhãn "Họ và tên", "Email đăng nhập" (không đổi).

**Owns:** —

- [x] **Step 1: Sửa prototype theo form Google Form và trích lại page**

Chạy từ gốc repo (script chỉ thay đúng 7 chuỗi, mỗi chuỗi phải xuất hiện đúng 1 lần — sai là dừng, không ghi file). `SECS` nằm trong factory dạng JSON thường; template `AdHoSo` nằm trong chuỗi JSON nên `"` và `/` đã escape (`\"`, `<\/`):

```bash
node --input-type=module - <<'EOF'
import { readFileSync, writeFileSync } from 'node:fs';
const file = 'docs/duobexiuuu-coaching-design.html';
let html = readFileSync(file, 'utf8');
// Một mục SECS đúng định dạng prototype (thứ tự khoá k, label, t, req, unit, opts, ph, note, w, rows)
const f = (k, label, t, req, unit, opts, ph, w, rows) =>
  `{"k": "${k}", "label": "${label}", "t": "${t}", "req": ${req}, "unit": "${unit}", "opts": [${opts.map((o) => `"${o}"`).join(', ')}], "ph": "${ph}", "note": "", "w": ${w}, "rows": ${rows}}`;
// Một dòng <dt>/<dd> của AdHoSo trong chuỗi JSON đã escape
const row = (dt, value) => String.raw`<dt style=\"color: var(--muted); font-size: 12px\">` + dt
  + String.raw`<\/dt><dd style=\"margin: 2px 0 0\">` + value + String.raw`<\/dd>`;
const edits = [
  // @ADR-f02-import-excel-001 — AdTaoHoSo: mỡ + cách đo gộp thành 1 ô chữ, không bắt buộc (@BR-f02-import-excel-004)
  [f('bf', 'Mỡ cơ thể', 'num', false, '%', [], '', 1, 3) + ', '
     + f('bfcach', 'Cách đo mỡ', 'sel', false, '', ['Ước lượng bằng mắt', 'Cân điện tử', 'InBody', 'Thước kẹp mỡ', 'DEXA', 'Khác'], '', 1, 3),
   f('bf', 'Mỡ cơ thể và cách đo', 'text', false, '', [], 'VD: 18% đo InBody', 1, 3)],
  // Số năm tập luyện: chữ tự do (@BR-f02-import-excel-003)
  [f('sonam', 'Số năm tập luyện', 'sel', true, '', ['Chưa tập', 'Dưới 6 tháng', '6–12 tháng', '1–2 năm', '2–3 năm', '3–5 năm', 'Trên 5 năm'], '', 1, 3),
   f('sonam', 'Số năm tập luyện', 'text', true, '', [], 'VD: 2 năm', 1, 3)],
  // Cân nặng + chiều cao trước khi tập tạ: 1 ô đoạn văn (@BR-f02-import-excel-005)
  [f('cantruoc', 'Cân nặng trước khi tập tạ', 'num', true, 'kg', [], '', 1, 3) + ', '
     + f('caotruoc', 'Chiều cao khi đó', 'num', true, 'cm', [], '', 1, 3),
   f('cantruoc', 'Cân nặng và chiều cao trước khi tập tạ', 'area', true, '', [], 'VD: 60 kg, 1m70', 2, 2)],
  // 4 link ảnh → 1 "Link ảnh toàn thân" (@BR-f02-import-excel-006)
  [[['anhtruoc', 'Ảnh mặt trước (link)'], ['anhsau', 'Ảnh mặt sau (link)'], ['anhtrai', 'Ảnh bên trái (link)'], ['anhphai', 'Ảnh bên phải (link)']]
     .map(([k, l]) => f(k, l, 'text', true, '', [], 'Link Google Drive', 1, 3)).join(', '),
   f('anh', 'Link ảnh toàn thân', 'text', true, '', [], 'Link Google Drive', 2, 3)],
  // AdHoSo "Thông tin học viên" (PO chốt 2026-11-26): bỏ Lịch rảnh/Dụng cụ (@BR-f02-import-excel-001)
  [row('Mục tiêu', 'Recomposition: giảm mỡ, giữ cơ. Về 70 kg trước Tết.'),
   row('Mục tiêu tập luyện', 'Recomposition: giảm mỡ, giữ cơ. Về 70 kg trước Tết.')],
  [row('Lịch rảnh', 'T3–T7 buổi tối, CN buổi sáng'), row('Số buổi tập mỗi tuần', '4 buổi')],
  [row('Dụng cụ', 'Phòng gym đầy đủ'), row('Giờ có thể ăn và giờ đi tập', 'Ăn 7h, 12h, 19h. Tập 17h30–19h.')],
];
for (const [from, to] of edits) {
  const n = html.split(from).length - 1;
  if (n !== 1) throw new Error(`Cần đúng 1 chỗ, thấy ${n}: ${from.slice(0, 90)}…`);
  html = html.replace(from, () => to);
}
writeFileSync(file, html);
console.log('Đã sửa 7 chỗ');
EOF
node .kv/sdd/proto/extract.cjs docs/duobexiuuu-coaching-design.html AdTaoHoSo AdHoSo
```

Expected: `Đã sửa 7 chỗ`, rồi `AdTaoHoSo <số ký tự>` và `AdHoSo <số ký tự>`.

Kiểm nhanh kết quả trích (đếm mục và mục bắt buộc của `SECS` — 41 mục gồm cả "Gán giáo án", 26 bắt buộc):
```bash
grep "^const SECS" .kv/sdd/proto/AdTaoHoSo.md | sed 's/^const SECS = //; s/;$//' \
  | node -e "const s=JSON.parse(require('fs').readFileSync(0,'utf8'));const a=s.flatMap(x=>x.fields);console.log(a.length,a.filter(f=>f.req).length)"
grep -n "Thông tin học viên" -A6 .kv/sdd/proto/AdHoSo.md
```
Expected: `41 26`; khối `<dl>` có đúng 4 dòng `Mục tiêu tập luyện`, `Số buổi tập mỗi tuần` (`4 buổi`), `Giờ có thể ăn và giờ đi tập` (`Ăn 7h, 12h, 19h. Tập 17h30–19h.`), `Hạn review`. `grep -c "Lịch rảnh\|Ảnh mặt trước" docs/duobexiuuu-coaching-design.html` → `0`.

Mở `docs/duobexiuuu-coaching-design.html#AdTaoHoSo` trong trình duyệt: nhóm "Số đo cơ thể hiện tại" kết thúc bằng ô "Mỡ cơ thể và cách đo"; "Tập luyện hiện tại" có ô chữ "Số năm tập luyện" và textarea rộng 2 cột "Cân nặng và chiều cao trước khi tập tạ"; "Chương trình tập sắp tới" có 1 ô rộng "Link ảnh toàn thân"; aside "Hồ sơ đã điền" ghi `0/26 mục bắt buộc, 0/41 mục tổng`.

- [x] **Step 2: Viết spec cho danh sách mục mới, payload, form và khối "Thông tin học viên"**

`frontend/projects/admin/src/app/students/registration-fields.spec.ts` — thay toàn bộ file:
```ts
import { describe, expect, it } from 'vitest';
import {
  FIELD_LABEL, FORM_FIELDS, FORM_SECTIONS, REGISTRATION_FIELDS, parseDecimal, parseInteger, toFormValues, toPayload,
} from './registration-fields';
import { StudentDetail } from './student.models';

const REMOVED = ['availability', 'equipment', 'bodyFatPercent', 'bodyFatMethod', 'weightBeforeLiftingKg', 'heightThenCm',
  'photoFrontUrl', 'photoBackUrl', 'photoLeftUrl', 'photoRightUrl'];

describe('registration fields', () => {
  // @BR-f02-import-excel-001 @BR-f02-import-excel-003 @BR-f02-import-excel-004 @BR-f02-import-excel-005 @BR-f02-import-excel-006 @BR-f02-import-excel-007
  it('follows the Google Form: no Lịch rảnh / Dụng cụ, merged items, one full-body photo link, without Gán giáo án', () => {
    expect(FORM_SECTIONS.map((s) => s.title)).toEqual([
      'Tài khoản và gói', 'Số đo cơ thể hiện tại', 'Tập luyện hiện tại', 'Ăn uống, sinh hoạt và môi trường sống',
      'Chương trình tập sắp tới', 'Kỳ vọng và cảm nhận', 'Chỉ coach thấy',
    ]);
    const labels = FORM_FIELDS.map((f) => f.label);
    for (const gone of ['Lịch rảnh', 'Dụng cụ', 'Gán giáo án', 'Cách đo mỡ', 'Chiều cao khi đó', 'Ảnh mặt trước (link)']) {
      expect(labels).not.toContain(gone);
    }
    for (const key of REMOVED) expect(FORM_FIELDS.some((f) => f.key === key)).toBe(false);

    const byKey = Object.fromEntries(FORM_FIELDS.map((f) => [f.key, f]));
    expect(byKey['trainingYears']).toMatchObject({ label: 'Số năm tập luyện', type: 'text', required: true });
    expect(byKey['trainingYears'].options).toBeUndefined();
    expect(byKey['bodyFatAndMethod']).toMatchObject({ label: 'Mỡ cơ thể và cách đo', type: 'text', required: false });
    expect(byKey['weightHeightBeforeLifting'])
      .toMatchObject({ label: 'Cân nặng và chiều cao trước khi tập tạ', type: 'area', required: true, wide: true });
    expect(byKey['bodyPhotoUrl']).toMatchObject({ label: 'Link ảnh toàn thân', type: 'url', required: true, wide: true });
    expect(FORM_SECTIONS[1].fields.at(-1)?.key).toBe('bodyFatAndMethod');
    expect(FORM_SECTIONS[2].fields.map((f) => f.key)).toEqual([
      'trainingYears', 'weeklyTrainingTime', 'weightHeightBeforeLifting', 'trainingGoal', 'currentTraining', 'medicalHistory',
    ]);
    expect(FORM_SECTIONS[4].fields.map((f) => f.key)).toEqual(['sessionsPerWeek', 'programStyle', 'bodyPhotoUrl']);

    expect(REGISTRATION_FIELDS).toHaveLength(37);
    expect(FORM_FIELDS.filter((f) => f.required)).toHaveLength(26);
    expect(FIELD_LABEL['bodyPhotoUrl']).toBe('Link ảnh toàn thân');
  });

  // @BR-f02-ho-so-hoc-vien-008: dấu phẩy thập phân
  it('parses decimal comma and grouped integers', () => {
    expect(parseDecimal('72,5')).toBe(72.5);
    expect(parseDecimal('250.1')).toBe(250.1);
    expect(parseDecimal('abc')).toBeNull();
    expect(parseInteger('3.500.000')).toBe(3500000);
    expect(parseInteger('28')).toBe(28);
    expect(parseInteger('2,5')).toBeNull();
    expect(parseInteger('3500000')).toBe(3500000);
    expect(parseInteger('3.5')).toBeNull();
    expect(parseInteger('35.00')).toBeNull();
  });

  it('builds the API payload and reports invalid numbers', () => {
    const { payload, errors } = toPayload({ fullName: ' Lan ', email: 'lan@gmail.com', weightKg: '72,5', age: 'abc', pack: 'Pack 2', internalNote: ' Gối phải ' });
    expect(payload.fullName).toBe('Lan');
    expect(payload.registration['weightKg']).toBe(72.5);
    expect(payload.registration['pack']).toBe('Pack 2');
    expect(payload.registration['trainingGoal']).toBeNull();
    expect(payload.internalNote).toBe('Gối phải');
    expect(errors).toEqual({ age: 'Nhập số hợp lệ' });
  });

  // @BR-f02-import-excel-003 @BR-f02-import-excel-004 @BR-f02-import-excel-005 @BR-f02-import-excel-006: chữ giữ nguyên văn (chỉ trim), không đổi sang số
  it('sends the merged text items verbatim and never sends removed keys', () => {
    const { payload, errors } = toPayload({
      trainingYears: '1', bodyFatAndMethod: '18% đo InBody', weightHeightBeforeLifting: '60kg, 1m70',
      bodyPhotoUrl: ' https://drive.google.com/open?id=abc ', availability: 'T3–T7 buổi tối',
    });
    expect(errors).toEqual({});
    expect(payload.registration['trainingYears']).toBe('1');
    expect(payload.registration['bodyFatAndMethod']).toBe('18% đo InBody');
    expect(payload.registration['weightHeightBeforeLifting']).toBe('60kg, 1m70');
    expect(payload.registration['bodyPhotoUrl']).toBe('https://drive.google.com/open?id=abc');
    for (const key of REMOVED) expect(key in payload.registration).toBe(false);
    expect(Object.keys(payload.registration)).toHaveLength(37);
  });

  it('shows stored numbers with a decimal comma', () => {
    const values = toFormValues({ fullName: 'Lan', email: 'lan@gmail.com', internalNote: null,
      registration: { weightKg: 72.5, age: 28, transferDate: '2026-11-25', trainingYears: '2 năm' } } as unknown as StudentDetail);
    expect(values['weightKg']).toBe('72,5');
    expect(values['age']).toBe('28');
    expect(values['transferDate']).toBe('2026-11-25');
    expect(values['trainingYears']).toBe('2 năm');
    expect(values['internalNote']).toBe('');
  });
});
```

`frontend/projects/admin/src/app/students/student-form.component.spec.ts`:
- thay chú thích và nhánh ảnh trong `fillAllRequired`:
```ts
  /** Điền đủ 26 mục bắt buộc bằng giá trị hợp lệ. */
  function fillAllRequired(el: HTMLElement) {
    for (const f of FORM_FIELDS.filter((x) => x.required)) {
      const value = f.key === 'email' ? 'lan@gmail.com'
        : f.type === 'select' ? f.options![0]
        : f.type === 'date' ? '2026-11-01'
        : f.type === 'integer' ? '28'
        : f.type === 'decimal' ? '170'
        : f.type === 'url' ? 'https://drive.google.com/x' : 'Lan';
      fill(el, f.key, value);
    }
  }
```
- trong test `restores the draft kept on this browser` thay `'2/30 mục bắt buộc'` bằng `'2/26 mục bắt buộc'`.
- trong test `"Lưu và gửi" with missing required fields saves the profile but does not send` thay `'Đã lưu hồ sơ. Còn 28 mục bắt buộc chưa điền nên chưa gửi thông tin đăng nhập.'` bằng `'Đã lưu hồ sơ. Còn 24 mục bắt buộc chưa điền nên chưa gửi thông tin đăng nhập.'`.
- thêm test sau test `offers only the allowed options for sessions per week`:
```ts
  // @BR-f02-import-excel-001 @BR-f02-import-excel-003 @BR-f02-import-excel-004 @BR-f02-import-excel-005 @BR-f02-import-excel-006
  it('asks the Google Form questions: free-text training years, merged items, one photo link, no Lịch rảnh / Dụng cụ', () => {
    const { el } = setup('create');
    const labels = [...el.querySelectorAll('label')].map((l) => l.textContent?.replace(/\s*\*$/, '').trim());

    expect(el.querySelector('#hs-trainingYears')?.tagName).toBe('INPUT');
    expect(el.querySelector('#hs-bodyFatAndMethod')?.tagName).toBe('INPUT');
    expect(el.querySelector('#hs-weightHeightBeforeLifting')?.tagName).toBe('TEXTAREA');
    expect(el.querySelector('#hs-bodyPhotoUrl')?.getAttribute('placeholder')).toBe('Link Google Drive');
    expect(el.querySelector('#hs-bodyPhotoUrl')?.closest('.field')?.classList.contains('wide')).toBe(true);
    expect(labels).toContain('Link ảnh toàn thân');
    expect(labels).toContain('Mỡ cơ thể và cách đo');
    expect(labels).not.toContain('Lịch rảnh');
    expect(labels).not.toContain('Dụng cụ');
    expect(el.querySelector('#hs-photoFrontUrl')).toBeNull();
    expect(el.querySelector('#hs-availability')).toBeNull();
    expect(el.textContent).toContain('0/26 mục bắt buộc, 0/40 mục tổng');
  });

  // @BR-f02-import-excel-003: số năm tập là chữ, gửi nguyên văn
  it('sends training years and the full-body photo link as typed', () => {
    const { http, el } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    fill(el, 'trainingYears', 'Khoảng 2 năm rưỡi');
    fill(el, 'bodyPhotoUrl', 'https://drive.google.com/open?id=abc');

    saveOnly(el);

    const body = http.expectOne('/api/admin/students').request.body;
    expect(body.registration['trainingYears']).toBe('Khoảng 2 năm rưỡi');
    expect(body.registration['bodyPhotoUrl']).toBe('https://drive.google.com/open?id=abc');
    expect('photoFrontUrl' in body.registration).toBe(false);
  });
```

`frontend/projects/admin/src/app/students/student-profile.component.spec.ts`:
- trong `function detail(...)` thay 2 dòng `registration: …` và `missingRequired: …` bằng:
```ts
    registration: { trainingGoal: 'Giảm mỡ, giữ cơ', sessionsPerWeek: '4 buổi', mealAndTrainingTimes: null, weightKg: 72.5 },
    missingRequired: ['pack', 'bodyPhotoUrl'], updatedAt: null, updatedBy: null, version: 'AAAAAAAAB9E=',
```
- trong test `shows the header, statuses, actions of an active student and missing required fields` thay `toContain('Ảnh mặt sau')` bằng `toContain('Link ảnh toàn thân')`.
- thêm test sau test đó:
```ts
  // PO chốt 2026-11-26 (tech-design UI) + @BR-f02-import-excel-001 @BR-f02-import-excel-006
  it('shows goal, sessions per week and meal/training times in the info block, and opens the full-body photo link', async () => {
    const { el } = await setup(detail({
      registration: {
        trainingGoal: 'Giảm mỡ, giữ cơ', sessionsPerWeek: '4 buổi', mealAndTrainingTimes: null,
        weightHeightBeforeLifting: '60kg, 1m70', bodyPhotoUrl: 'https://drive.google.com/open?id=abc',
      },
    }));

    const rows = [...el.querySelectorAll('dl.info > div')]
      .map((r) => [r.querySelector('dt')?.textContent?.trim(), r.querySelector('dd')?.textContent?.trim()]);
    expect(rows).toEqual([
      ['Mục tiêu tập luyện', 'Giảm mỡ, giữ cơ'],
      ['Số buổi tập mỗi tuần', '4 buổi'],
      ['Giờ có thể ăn và giờ đi tập', 'Chưa có'],
    ]);
    expect(el.textContent).not.toContain('Lịch rảnh');
    expect(el.textContent).not.toContain('Dụng cụ');
    const photo = el.querySelector('details a[href="https://drive.google.com/open?id=abc"]');
    expect(photo?.textContent?.trim()).toBe('Mở ảnh');
    expect(photo?.getAttribute('target')).toBe('_blank');
    expect(el.querySelector('details')?.textContent).toContain('60kg, 1m70');
    expect(el.querySelector('details')?.textContent).toContain('Cân nặng và chiều cao trước khi tập tạ');
  });
```

- [x] **Step 3: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: FAIL — `registration fields` (`expected [ …'Lịch rảnh'… ] not to contain 'Lịch rảnh'`, `toHaveLength(37)` nhận 44), `StudentFormComponent` (`#hs-trainingYears` là `SELECT`, `2/26 mục bắt buộc` không có, `Còn 24 mục`), `StudentProfileComponent` (`dl.info` không có, `Link ảnh toàn thân` không có). Các test khác PASS.

- [x] **Step 4: Danh sách mục theo Google Form**

`frontend/projects/admin/src/app/students/registration-fields.ts` — thay phần đầu file từ dòng `import { Registration, … }` tới hết khai báo `FIELD_LABEL` (các hàm `parseDecimal`, `parseInteger`, `toInputText`, `toPayload`, `toFormValues` giữ nguyên — chuỗi/URL đi nhánh `registration[field.key] = raw`):
```ts
import { Registration, RegistrationValue, StudentDetail } from './student.models';

/** `url`: ô 1 dòng nhận link (server kiểm http/https ≤ 2048, @BR-f02-import-excel-006). */
export type FieldType = 'text' | 'decimal' | 'integer' | 'date' | 'area' | 'select' | 'url';

export interface FieldDef {
  key: string;
  label: string;
  type: FieldType;
  required: boolean;
  unit?: string;
  placeholder?: string;
  note?: string;
  /** Chiếm cả hàng lưới (prototype w: 2). */
  wide?: boolean;
  rows?: number;
  options?: readonly string[];
}

export interface SectionDef {
  id: string;
  title: string;
  hint?: string;
  fields: readonly FieldDef[];
}

/**
 * Form đăng ký theo page AdTaoHoSo (thứ tự, nhãn, placeholder, đơn vị nguyên văn prototype — `SECS` đã sửa theo Google Form,
 * @ADR-f02-import-excel-001); bỏ "Gán giáo án" (F05). Danh sách lựa chọn là bản sao để hiển thị — server kiểm lại (RegistrationOptions).
 */
export const FORM_SECTIONS: readonly SectionDef[] = [
  {
    id: 'tk', title: 'Tài khoản và gói', hint: 'Thông tin để tạo tài khoản và ghi nhận thanh toán.',
    fields: [
      { key: 'fullName', label: 'Họ và tên', type: 'text', required: true, placeholder: 'VD: Phạm Hoàng Quân' },
      { key: 'email', label: 'Email đăng nhập', type: 'text', required: true, placeholder: 'ten@gmail.com', note: 'Hệ thống gửi mật khẩu tạm về email này.' },
      { key: 'pack', label: 'Gói đã mua', type: 'select', required: true, options: ['Pack 1', 'Pack 2', 'Pack 3', 'Khác'] },
      { key: 'price', label: 'Giá', type: 'integer', required: true, unit: 'đ', placeholder: 'VD: 3.500.000' },
      { key: 'transferDate', label: 'Ngày chuyển khoản', type: 'date', required: true },
      { key: 'address', label: 'Địa chỉ', type: 'text', required: true, placeholder: 'Tỉnh / thành phố' },
      { key: 'age', label: 'Tuổi', type: 'integer', required: true, unit: 'tuổi' },
      { key: 'heightCm', label: 'Chiều cao', type: 'decimal', required: true, unit: 'cm' },
    ],
  },
  {
    id: 'so', title: 'Số đo cơ thể hiện tại', hint: 'Số đo ban đầu, dùng để so sánh khi tổng kết phase.',
    fields: [
      { key: 'weightKg', label: 'Cân nặng', type: 'decimal', required: true, unit: 'kg' },
      { key: 'chestCm', label: 'Vòng 1 (ngực)', type: 'decimal', required: false, unit: 'cm' },
      { key: 'bellyCm', label: 'Vòng 2 (bụng)', type: 'decimal', required: false, unit: 'cm' },
      { key: 'waistCm', label: 'Vòng eo (trên rốn 4 cm)', type: 'decimal', required: false, unit: 'cm' },
      { key: 'hipCm', label: 'Vòng 3 (mông)', type: 'decimal', required: false, unit: 'cm' },
      { key: 'rightArmCm', label: 'Tay phải (gồng biceps)', type: 'decimal', required: false, unit: 'cm' },
      { key: 'leftArmCm', label: 'Tay trái (gồng biceps)', type: 'decimal', required: false, unit: 'cm' },
      { key: 'rightThighCm', label: 'Đùi phải (chỗ to nhất)', type: 'decimal', required: false, unit: 'cm' },
      { key: 'leftThighCm', label: 'Đùi trái (chỗ to nhất)', type: 'decimal', required: false, unit: 'cm' },
      // @BR-f02-import-excel-004 @BR-f02-import-excel-007: chữ tự do ≤ 200, không bắt buộc
      { key: 'bodyFatAndMethod', label: 'Mỡ cơ thể và cách đo', type: 'text', required: false, placeholder: 'VD: 18% đo InBody' },
    ],
  },
  {
    id: 'tap', title: 'Tập luyện hiện tại',
    fields: [
      // @BR-f02-import-excel-003: chữ tự do ≤ 200, giữ nguyên văn
      { key: 'trainingYears', label: 'Số năm tập luyện', type: 'text', required: true, placeholder: 'VD: 2 năm' },
      { key: 'weeklyTrainingTime', label: 'Thời gian dành cho tập mỗi tuần', type: 'text', required: true, placeholder: 'VD: 5 buổi, mỗi buổi 75 phút' },
      // @BR-f02-import-excel-005: đoạn văn ≤ 2000
      { key: 'weightHeightBeforeLifting', label: 'Cân nặng và chiều cao trước khi tập tạ', type: 'area', required: true, wide: true, rows: 2, placeholder: 'VD: 60 kg, 1m70' },
      { key: 'trainingGoal', label: 'Mục tiêu tập luyện', type: 'area', required: true, wide: true, rows: 2 },
      { key: 'currentTraining', label: 'Chế độ tập hiện tại', type: 'area', required: false, wide: true, rows: 4, placeholder: 'Lịch chia nhóm cơ, bài, số set, rep…' },
      { key: 'medicalHistory', label: 'Tiền sử bệnh lý và chấn thương', type: 'area', required: true, wide: true, rows: 2, placeholder: 'Ghi "Không" nếu không có' },
    ],
  },
  {
    id: 'an', title: 'Ăn uống, sinh hoạt và môi trường sống',
    fields: [
      { key: 'foodBudgetReadiness', label: 'Sẵn sàng đầu tư cho thực phẩm chất lượng', type: 'select', required: true, wide: true,
        options: ['Sẵn sàng', 'Đang cân nhắc', 'Chưa sẵn sàng'] },
      { key: 'occupation', label: 'Nghề nghiệp', type: 'text', required: false, wide: true, placeholder: 'Giờ làm, ca kíp, mức stress, đi lại nhiều không…' },
      { key: 'activityLevel', label: 'Mức độ hoạt động', type: 'select', required: true,
        options: ['Ít vận động (ngồi nhiều)', 'Nhẹ', 'Vừa', 'Nhiều', 'Rất nhiều (lao động chân tay)'] },
      { key: 'stressLevel', label: 'Mức độ căng thẳng', type: 'select', required: true, options: ['Thấp', 'Vừa', 'Cao', 'Rất cao'] },
      { key: 'sleepDuration', label: 'Thời gian ngủ mỗi ngày', type: 'select', required: true,
        options: ['Dưới 5 tiếng', '5–6 tiếng', '6–7 tiếng', '7–8 tiếng', 'Trên 8 tiếng'] },
      { key: 'caffeine', label: 'Caffeine mỗi ngày', type: 'text', required: false, placeholder: 'VD: 2 ly cà phê sữa' },
      { key: 'supplements', label: 'Thực phẩm bổ sung đang dùng', type: 'area', required: false, wide: true, rows: 2 },
      { key: 'currentDiet', label: 'Chế độ dinh dưỡng hiện tại', type: 'area', required: true, wide: true, rows: 4, placeholder: 'Các bữa trong ngày, ăn gì, lượng bao nhiêu…' },
      { key: 'mealAndTrainingTimes', label: 'Giờ có thể ăn và giờ đi tập', type: 'area', required: true, wide: true, rows: 3, placeholder: 'VD: Ăn 7h, 12h, 19h. Tập 17h30–19h.' },
    ],
  },
  {
    id: 'ct', title: 'Chương trình tập sắp tới',
    fields: [
      { key: 'sessionsPerWeek', label: 'Số buổi tập mỗi tuần', type: 'select', required: true,
        options: ['2 buổi', '3 buổi', '4 buổi', '5 buổi', '6 buổi', '7 buổi'] },
      { key: 'programStyle', label: 'Phong cách giáo án muốn theo', type: 'select', required: true,
        options: ['Push / Pull / Legs', 'Upper / Lower', 'Full body', 'Mỗi buổi một nhóm cơ', 'Để coach chọn', 'Khác'] },
      // @BR-f02-import-excel-006: 1 link http/https thay 4 link ảnh
      { key: 'bodyPhotoUrl', label: 'Link ảnh toàn thân', type: 'url', required: true, wide: true, placeholder: 'Link Google Drive' },
    ],
  },
  {
    id: 'kv', title: 'Kỳ vọng và cảm nhận',
    fields: [
      { key: 'expectations', label: 'Mong đợi gì từ coach', type: 'area', required: true, wide: true, rows: 2 },
      { key: 'previousCoaching', label: 'Từng theo coach / online coaching chưa, trải nghiệm ra sao', type: 'area', required: true, wide: true, rows: 2 },
      { key: 'investmentReason', label: 'Điều gì khiến bạn quyết định đầu tư vào bản thân lúc này', type: 'area', required: true, wide: true, rows: 2 },
    ],
  },
  {
    id: 'nb', title: 'Chỉ coach thấy', hint: 'Học viên không thấy phần này.',
    fields: [
      { key: 'internalNote', label: 'Ghi chú nội bộ', type: 'area', required: false, wide: true, rows: 2, placeholder: 'VD: Tránh lunge sâu vì gối phải' },
    ],
  },
];

export const FORM_FIELDS: readonly FieldDef[] = FORM_SECTIONS.flatMap((s) => s.fields);

const NOT_REGISTRATION = new Set(['fullName', 'email', 'internalNote']);

/** 37 mục của `registration` (bỏ họ tên, email, ghi chú nội bộ). */
export const REGISTRATION_FIELDS: readonly FieldDef[] = FORM_FIELDS.filter((f) => !NOT_REGISTRATION.has(f.key));

/** Nhãn hiển thị theo mã mục (dùng cho "Mục bắt buộc còn thiếu"). */
export const FIELD_LABEL: Readonly<Record<string, string>> = Object.fromEntries(FORM_FIELDS.map((f) => [f.key, f.label]));
```

`frontend/projects/admin/src/app/students/student-form.component.html` — trong nhánh `@default`, thay dòng:
```html
                             [attr.inputmode]="f.type === 'decimal' || f.type === 'integer' ? 'decimal' : null"
```
bằng:
```html
                             [attr.inputmode]="f.type === 'decimal' || f.type === 'integer' ? 'decimal' : f.type === 'url' ? 'url' : null"
```
(ô link vẫn là `input.ctl` 46px, padding `0 14px`, radius 14 như mọi ô chữ của prototype — prototype `"t": "text"`.)

- [x] **Step 5: Khối "Thông tin học viên" và link ảnh trên trang Hồ sơ**

`frontend/projects/admin/src/app/students/student-profile.component.html` — thay:
```html
        <dl>
          <div><dt>Mục tiêu</dt><dd>{{ text(d, 'trainingGoal') }}</dd></div>
          <div><dt>Lịch rảnh</dt><dd>{{ text(d, 'availability') }}</dd></div>
          <div><dt>Dụng cụ</dt><dd>{{ text(d, 'equipment') }}</dd></div>
        </dl>
```
bằng (nhãn nguyên văn `AdHoSo` đã sửa ở Step 1; dòng "Hạn review" của prototype thuộc F05 — chưa hiển thị, như trước):
```html
        <dl class="info">
          <div><dt>Mục tiêu tập luyện</dt><dd>{{ text(d, 'trainingGoal') }}</dd></div>
          <div><dt>Số buổi tập mỗi tuần</dt><dd>{{ text(d, 'sessionsPerWeek') }}</dd></div>
          <div><dt>Giờ có thể ăn và giờ đi tập</dt><dd>{{ text(d, 'mealAndTrainingTimes') }}</dd></div>
        </dl>
```
(`dl` / `dt` / `dd` dùng style có sẵn: `gap: 10px; font-size: 14px`, `dt` 12px `--muted`, `dd` `margin: 2px 0 0` — đúng prototype.)

`frontend/projects/admin/src/app/students/student-profile.component.ts` — thay hàm `isPhoto`:
```ts
  /** "Link ảnh toàn thân" hiện thành link "Mở ảnh" (@BR-f02-import-excel-006). */
  protected isPhoto(field: FieldDef): boolean {
    return field.type === 'url';
  }
```

- [x] **Step 6: Chạy — kỳ vọng PASS**

Run: `cd frontend && npm run test:ci`
Expected: PASS — không test nào failed (`registration fields` 5 test, `StudentFormComponent` thêm 2 test, `StudentProfileComponent` thêm 1 test; shared/client không đổi).
Run: `cd frontend && npx ng build admin`
Expected: build thành công, không warning budget mới.

Kiểm tay (compose có API Task 1: `docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build --wait`, rồi `cd frontend && npm run start:admin`, đăng nhập `admin@coaching.local`), mở song song `docs/duobexiuuu-coaching-design.html#AdTaoHoSo` / `#AdHoSo` ở **390px** và **1280px**:
- `/hoc-vien/moi`: thứ tự mục từng nhóm giống `SECS` (trừ "Gán giáo án"); "Mỡ cơ thể và cách đo" ô 46px không dấu `*`, không đơn vị; "Số năm tập luyện" ô chữ 46px có `*`; "Cân nặng và chiều cao trước khi tập tạ" textarea 2 dòng chiếm cả hàng, padding `12px 14px`; "Link ảnh toàn thân" 1 ô chiếm cả hàng, placeholder "Link Google Drive" màu `--faint`; badge nhóm "Tập luyện hiện tại" `0/5 bắt buộc`, "Chương trình tập sắp tới" `0/3 bắt buộc`; aside `0/26 mục bắt buộc, 0/40 mục tổng` (prototype ghi `/41` vì còn "Gán giáo án" — khác biệt có sẵn từ F02).
- Điền đủ → "Lưu và gửi thông tin đăng nhập" gửi được (đủ 26 mục); "Chỉ lưu hồ sơ" với "Số năm tập luyện" = `Khoảng 2 năm rưỡi` → trang hồ sơ, "Xem đủ form đăng ký" hiện nguyên văn; "Link ảnh toàn thân" hiện "Mở ảnh" mở tab mới.
- `/hoc-vien/:id`: card "Thông tin học viên" 3 dòng `dt` 12px `--muted` / `dd` 14px, mục trống ghi "Chưa có"; card rộng ≤ 400px ở 1280px, 1 cột ở 390px không tràn ngang.
- `/hoc-vien/:id/sua`: ô hiện đúng giá trị đã lưu của các mục mới.

- [x] **Step 7: Commit**

`.kv/sdd/proto/*.md` bị `.gitignore` (`.kv/*`) — không commit, chỉ commit prototype:
```bash
git add docs/duobexiuuu-coaching-design.html frontend/projects/admin/src/app/students
git commit -F - <<'EOF'
feat(admin): registration form follows the Google Form; profile info block; prototype updated

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 7: [UI] Tạo hồ sơ — card "Import từ Excel"

Màn hình (tech-design §3 "Thiết kế giao diện (UI)", PO chốt vị trí 2026-11-26): **Tạo hồ sơ** `/hoc-vien/moi` (page `AdTaoHoSo`) — card "Import từ Excel" ở **đầu trang, trên form**, chỉ ở chế độ tạo (không ở `/hoc-vien/:id/sua`). Card trắng `--surface`, viền `--line`, radius 12, padding `16px 18px`, h2 Barlow 700 22px (như card aside "Hồ sơ đã điền"); mô tả nguyên văn; nút phụ chọn file 44px viền `--field`; nút chính "Import" 44px `--primary`. Sau khi import: dòng "Đã tạo {x}, bỏ qua {y}" trong hộp `--okBg` (y = 0) / `--warnBg` (y > 0) + bảng lỗi kiểu bảng Dashboard (cột Dòng, Email, Cột, Lý do; dòng `--rowLine`). Lỗi cả file: `title` của API trong hộp `--warnBg`. Prototype không có card này (tech-design ghi "Suy ra") — giá trị style lấy từ tech-design và từ các khối có sẵn của `AdTaoHoSo` (card aside, `msgStyle`) và `Admin-Dashboard` (bảng).

Câu chữ không có trong tech-design (đặt theo giọng prototype, ghi lại để PO duyệt khi xem màn): nhãn nút chọn file "Chọn file .xlsx", trạng thái "Chưa chọn file", lỗi mạng "Không import được file. Vui lòng thử lại.".

**Files:**
- Create: `frontend/projects/admin/src/app/students/import-card.component.ts`, `import-card.component.spec.ts`
- Modify: `frontend/projects/admin/src/app/students/student.models.ts`, `students.api.ts`
- Modify: `frontend/projects/admin/src/app/students/student-form.component.ts`, `student-form.component.html`, `student-form.component.spec.ts`
- Modify: `frontend/nginx/coaching-common.inc` (nginx mặc định `client_max_body_size 1m` → file 1–5 MB qua compose/VPS bị nginx trả 413 trước khi tới API)

**Interfaces:**
- Consumes: Task 4/5 — `POST /api/admin/students/import` `multipart/form-data` field `file` → `200 ImportResultDto { created, skipped, rows: [{ row, email, status: "Created" | "Skipped", studentId, errors: [{ column, reason }] }] }` (chỉ dòng có dữ liệu); `400 ProblemDetails { code, title }` với `import.file_invalid` "File phải là Excel .xlsx đọc được.", `import.file_too_large` "File vượt giới hạn 5 MB.", `import.too_many_rows` "File vượt giới hạn 500 dòng.", `import.missing_column` "Thiếu cột \"{tên}\".", `import.no_rows` "File không có dòng dữ liệu."; 413 (Kestrel/nginx) → cùng nghĩa "File vượt giới hạn 5 MB.". Task 6 — `StudentFormComponent` (chế độ `create`/`edit`).
- Produces: `ImportRowStatus`, `ImportCellError`, `ImportRow`, `ImportResult` (student.models); `StudentsApi.importStudents(file: File): Observable<ImportResult>`; `<app-import-card />` (`section` `aria-labelledby` → region "Import từ Excel"; `input#import-file` có nhãn "Chọn file .xlsx"; nút "Import"; `p.box[role=status]` "Đã tạo {x}, bỏ qua {y}" hoặc lỗi cả file; `table` với `thead` Dòng/Email/Cột/Lý do, mỗi ô sai một `tbody tr`). Hằng `IMPORT_HINT`, `IMPORT_FAILED`, `IMPORT_TOO_LARGE`. Task 8 dùng các selector/nhãn này.

**Owns:** —

- [x] **Step 1: Viết spec cho card import và vị trí card trên trang Tạo hồ sơ**

`frontend/projects/admin/src/app/students/import-card.component.spec.ts`:
```ts
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { describe, expect, it } from 'vitest';
import { IMPORT_FAILED, IMPORT_HINT, IMPORT_TOO_LARGE, ImportCardComponent } from './import-card.component';
import { ImportResult } from './student.models';

const settle = () => new Promise((resolve) => setTimeout(resolve, 0));
const XLSX = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';

describe('ImportCardComponent', () => {
  function setup() {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    const fixture = TestBed.createComponent(ImportCardComponent);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const http = TestBed.inject(HttpTestingController);
    const importButton = () =>
      [...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === 'Import') as HTMLButtonElement;
    const choose = (file: File) => {
      const input = el.querySelector('#import-file') as HTMLInputElement;
      Object.defineProperty(input, 'files', { value: [file], configurable: true });
      input.dispatchEvent(new Event('change'));
      fixture.detectChanges();
    };
    const runImport = async (file: File) => {
      choose(file);
      importButton().click();
      fixture.detectChanges();
      return http.expectOne('/api/admin/students/import');
    };
    const render = async () => {
      await settle();
      fixture.detectChanges();
    };
    return { el, http, importButton, choose, runImport, render };
  }

  const file = new File(['PK'], 'cau-tra-loi.xlsx', { type: XLSX });

  it('shows the title, the hint and a disabled Import until a file is chosen', () => {
    const { el, importButton, choose } = setup();

    expect(el.querySelector('h2')?.textContent?.trim()).toBe('Import từ Excel');
    expect(el.querySelector('.hint')?.textContent?.trim()).toBe(IMPORT_HINT);
    expect(IMPORT_HINT).toBe('Tải file .xlsx câu trả lời Google Form (tối đa 5 MB, 500 dòng). Hồ sơ được tạo ở Chờ kích hoạt, chưa gửi email.');
    expect(el.querySelector('label[for="import-file"]')?.textContent?.trim()).toBe('Chọn file .xlsx');
    expect((el.querySelector('#import-file') as HTMLInputElement).accept).toContain('.xlsx');
    expect(el.querySelector('.file')?.textContent?.trim()).toBe('Chưa chọn file');
    expect(importButton().disabled).toBe(true);

    choose(file);
    expect(el.querySelector('.file')?.textContent?.trim()).toBe('cau-tra-loi.xlsx');
    expect(importButton().disabled).toBe(false);
  });

  // @BR-f02-import-excel-034: 2 dòng hợp lệ → "Đã tạo 2, bỏ qua 0" (hộp xanh, không bảng)
  it('posts the file as multipart field "file" and shows the summary in a green box when nothing was skipped', async () => {
    const { el, runImport, render, importButton } = setup();
    const request = await runImport(file);

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeInstanceOf(FormData);
    expect(((request.request.body as FormData).get('file') as File).name).toBe('cau-tra-loi.xlsx');
    expect(importButton().disabled).toBe(true); // đang gửi
    request.flush({
      created: 2, skipped: 0,
      rows: [
        { row: 2, email: 'lan@gmail.com', status: 'Created', studentId: 's1', errors: [] },
        { row: 3, email: 'quan@gmail.com', status: 'Created', studentId: 's2', errors: [] },
      ],
    } satisfies ImportResult);
    await render();

    const box = el.querySelector('.box')!;
    expect(box.getAttribute('role')).toBe('status');
    expect(box.textContent?.trim()).toBe('Đã tạo 2, bỏ qua 0');
    expect(box.classList.contains('warn')).toBe(false);
    expect(el.querySelector('table')).toBeNull();
    expect(importButton().disabled).toBe(false);
  });

  // @BR-f02-import-excel-034: 3 hợp lệ, 1 trùng email, 1 tuổi sai → "Đã tạo 3, bỏ qua 2" + lý do kèm số dòng Excel
  it('lists every invalid cell of the skipped rows with the Excel row number in a warn box and table', async () => {
    const { el, runImport, render } = setup();
    const request = await runImport(file);
    request.flush({
      created: 3, skipped: 2,
      rows: [
        { row: 2, email: 'a@gmail.com', status: 'Created', studentId: 's1', errors: [] },
        { row: 3, email: 'b@gmail.com', status: 'Created', studentId: 's2', errors: [] },
        { row: 4, email: 'quan@gmail.com', status: 'Skipped', studentId: null,
          errors: [{ column: 'Địa chỉ email', reason: 'email đã có hồ sơ' }] },
        { row: 5, email: 'c@gmail.com', status: 'Created', studentId: 's3', errors: [] },
        { row: 6, email: 'an@gmail.com', status: 'Skipped', studentId: null,
          errors: [{ column: 'Tuổi (Age)', reason: 'phải từ 10 đến 100' }, { column: 'Chiều cao (Height)', reason: 'phải từ 100 đến 250' }] },
      ],
    } satisfies ImportResult);
    await render();

    const box = el.querySelector('.box')!;
    expect(box.textContent?.trim()).toBe('Đã tạo 3, bỏ qua 2');
    expect(box.classList.contains('warn')).toBe(true);
    expect([...el.querySelectorAll('thead th')].map((th) => th.textContent?.trim())).toEqual(['Dòng', 'Email', 'Cột', 'Lý do']);
    const rows = [...el.querySelectorAll('tbody tr')].map((tr) => [...tr.querySelectorAll('td')].map((td) => td.textContent?.trim()));
    expect(rows).toEqual([
      ['4', 'quan@gmail.com', 'Địa chỉ email', 'email đã có hồ sơ'],
      ['6', 'an@gmail.com', 'Tuổi (Age)', 'phải từ 10 đến 100'],
      ['6', 'an@gmail.com', 'Chiều cao (Height)', 'phải từ 100 đến 250'],
    ]);
  });

  // @BR-f02-import-excel-024: dòng trống email → ô Email hiện "—"
  it('shows a dash when the skipped row has no email', async () => {
    const { el, runImport, render } = setup();
    const request = await runImport(file);
    request.flush({
      created: 0, skipped: 1,
      rows: [{ row: 2, email: null, status: 'Skipped', studentId: null, errors: [{ column: 'Địa chỉ email', reason: 'bắt buộc' }] }],
    } satisfies ImportResult);
    await render();

    expect([...el.querySelectorAll('tbody td')].map((td) => td.textContent?.trim())).toEqual(['2', '—', 'Địa chỉ email', 'bắt buộc']);
  });

  // @BR-f02-import-excel-019 (lỗi cả file): hiện title của API, không bảng
  it('shows the API title of a file-level error in a warn box', async () => {
    const { el, runImport, render } = setup();
    const request = await runImport(file);
    request.flush({ code: 'import.missing_column', title: 'Thiếu cột "Địa chỉ email".' }, { status: 400, statusText: 'Bad Request' });
    await render();

    const box = el.querySelector('.box')!;
    expect(box.textContent?.trim()).toBe('Thiếu cột "Địa chỉ email".');
    expect(box.classList.contains('warn')).toBe(true);
    expect(el.querySelector('table')).toBeNull();
  });

  // @BR-f02-import-excel-015: 413 của Kestrel/nginx không có ProblemDetails → câu 5 MB; lỗi mạng → câu thử lại
  it('maps a 413 to the 5 MB message, other failures to a retry message, and clears the old result on a new import', async () => {
    const { el, runImport, render } = setup();
    const tooLarge = await runImport(file);
    tooLarge.flush('<html>413</html>', { status: 413, statusText: 'Payload Too Large' });
    await render();
    expect(el.querySelector('.box')?.textContent?.trim()).toBe(IMPORT_TOO_LARGE);
    expect(IMPORT_TOO_LARGE).toBe('File vượt giới hạn 5 MB.');

    const offline = await runImport(file);
    expect(el.querySelector('.box')).toBeNull(); // kết quả cũ bị xoá khi bắt đầu lần import mới
    offline.error(new ProgressEvent('error'));
    await render();
    expect(el.querySelector('.box')?.textContent?.trim()).toBe(IMPORT_FAILED);
    expect(IMPORT_FAILED).toBe('Không import được file. Vui lòng thử lại.');
  });
});
```

`frontend/projects/admin/src/app/students/student-form.component.spec.ts` — thêm test sau test `offers the prototype actions on create`:
```ts
  // tech-design UI (PO chốt 2026-11-26): card "Import từ Excel" ở đầu trang Tạo hồ sơ, trên form; không có ở Sửa hồ sơ
  it('shows the Excel import card above the form on create only', async () => {
    const { el } = setup('create');
    const card = el.querySelector('app-import-card');
    expect(card?.querySelector('h2')?.textContent?.trim()).toBe('Import từ Excel');
    expect(card!.compareDocumentPosition(el.querySelector('form')!) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();

    TestBed.resetTestingModule();
    const edit = setup('edit', 's1');
    edit.http.expectOne('/api/admin/students/s1').flush(detail());
    await settle();
    edit.fixture.detectChanges();
    expect(edit.el.querySelector('app-import-card')).toBeNull();
  });
```

- [x] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: FAIL — `Cannot find module './import-card.component'` (spec card); `StudentFormComponent › shows the Excel import card above the form on create only` (`card` là `null`).

- [x] **Step 3: Model kết quả import, `StudentsApi.importStudents`, `ImportCardComponent`**

`frontend/projects/admin/src/app/students/student.models.ts` — thêm cuối file:
```ts
/** POST /api/admin/students/import (ImportResultDto) — chỉ dòng có dữ liệu, `row` = số dòng Excel (tiêu đề = 1). */
export type ImportRowStatus = 'Created' | 'Skipped';

/** `column` = tiêu đề cột trong file (dòng đầu), `reason` = lý do tiếng Việt (@BR-f02-import-excel-023). */
export interface ImportCellError {
  column: string;
  reason: string;
}

export interface ImportRow {
  row: number;
  email: string | null;
  status: ImportRowStatus;
  studentId: string | null;
  errors: ImportCellError[];
}

/** @BR-f02-import-excel-034: số dòng đã tạo, số dòng bỏ qua, lý do từng dòng. */
export interface ImportResult {
  created: number;
  skipped: number;
  rows: ImportRow[];
}
```

`frontend/projects/admin/src/app/students/students.api.ts` — sửa import model:
```ts
import { CoachingStatus, CreateStudentBody, ImportResult, StudentDetail, StudentListItem, UpdateStudentBody } from './student.models';
```
và thêm method cuối class:
```ts
  /** F02 import Excel: multipart field `file` (Content-Type do trình duyệt đặt, kèm boundary). */
  importStudents(file: File): Observable<ImportResult> {
    const body = new FormData();
    body.append('file', file, file.name);
    return this.http.post<ImportResult>(this.url('/import'), body);
  }
```

`frontend/projects/admin/src/app/students/import-card.component.ts`:
```ts
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ImportResult } from './student.models';
import { StudentsApi } from './students.api';

/** Câu nguyên văn tech-design "Thiết kế giao diện (UI)". */
export const IMPORT_HINT = 'Tải file .xlsx câu trả lời Google Form (tối đa 5 MB, 500 dòng). Hồ sơ được tạo ở Chờ kích hoạt, chưa gửi email.';
/** 413 của Kestrel/nginx không có ProblemDetails → cùng câu với `import.file_too_large` (@BR-f02-import-excel-015). */
export const IMPORT_TOO_LARGE = 'File vượt giới hạn 5 MB.';
export const IMPORT_FAILED = 'Không import được file. Vui lòng thử lại.';

interface ErrorLine {
  row: number;
  email: string;
  column: string;
  reason: string;
}

/**
 * Card "Import từ Excel" ở đầu trang Tạo hồ sơ (tech-design UI, PO chốt 2026-11-26).
 * Kết quả theo @BR-f02-import-excel-034: "Đã tạo {x}, bỏ qua {y}" + mỗi ô sai một dòng bảng (Dòng, Email, Cột, Lý do).
 */
@Component({
  selector: 'app-import-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="import" aria-labelledby="import-title">
      <h2 id="import-title">Import từ Excel</h2>
      <p class="hint">{{ hint }}</p>
      <div class="row">
        <input id="import-file" class="sr-only" type="file"
               accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" (change)="pick($event)" />
        <label class="choose" for="import-file">Chọn file .xlsx</label>
        <span class="file">{{ file()?.name ?? 'Chưa chọn file' }}</span>
        <button type="button" class="primary" [disabled]="!file() || busy()" (click)="run()">Import</button>
      </div>
      @if (fileError(); as e) {
        <p class="box warn" role="status">{{ e }}</p>
      }
      @if (result(); as r) {
        <p class="box" [class.warn]="r.skipped > 0" role="status">Đã tạo {{ r.created }}, bỏ qua {{ r.skipped }}</p>
        @if (errorLines().length > 0) {
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th scope="col" class="first">Dòng</th>
                  <th scope="col">Email</th>
                  <th scope="col">Cột</th>
                  <th scope="col" class="last">Lý do</th>
                </tr>
              </thead>
              <tbody>
                @for (line of errorLines(); track $index) {
                  <tr>
                    <td class="first num">{{ line.row }}</td>
                    <td>{{ line.email }}</td>
                    <td>{{ line.column }}</td>
                    <td class="last">{{ line.reason }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      }
    </section>
  `,
  styles: `
    :host { display: block; }
    .import { background: var(--surface); border: 1px solid var(--line); border-radius: 12px; padding: 16px 18px;
      display: flex; flex-direction: column; gap: 10px; }
    h2 { margin: 0; font-family: var(--font-head); font-weight: 700; font-size: 22px; }
    .hint { margin: 0; font-size: 14px; line-height: 1.5; color: var(--muted); }
    .row { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; }
    .choose { box-sizing: border-box; height: 44px; padding: 0 22px; border-radius: 999px; border: 1px solid var(--field);
      background: var(--surface); color: var(--ink); font-weight: 600; font-size: 14px; display: inline-flex; align-items: center;
      cursor: pointer; }
    .sr-only:focus-visible + .choose { outline: 2px solid var(--primary); outline-offset: 2px; }
    .file { min-width: 0; font-size: 14px; color: var(--ink2); overflow-wrap: anywhere; }
    .primary { height: 44px; padding: 0 22px; border-radius: 999px; border: 0; background: var(--primary); color: var(--onPrimary);
      font: inherit; font-weight: 600; font-size: 14px; cursor: pointer; }
    .primary:disabled { opacity: .6; cursor: default; }
    .box { margin: 0; border-radius: 12px; padding: 10px 14px; font-size: 14px; font-weight: 600; background: var(--okBg); color: var(--okInk); }
    .box.warn { background: var(--warnBg); color: var(--warn); }
    .table-wrap { overflow-x: auto; border: 1px solid var(--line); border-radius: 14px; background: var(--tableBg); }
    table { width: 100%; min-width: 560px; border-collapse: collapse; font-size: 14px; }
    thead tr { text-align: left; color: var(--muted); font-size: 13px; }
    th { padding: 14px 12px; font-weight: 500; border-bottom: 1px solid var(--line); }
    td { padding: 14px 12px; border-bottom: 1px solid var(--rowLine); vertical-align: top; }
    tbody tr:last-child td { border-bottom: 0; }
    th.first, td.first { padding-left: 18px; }
    th.last, td.last { padding-right: 18px; }
    .num { font-variant-numeric: tabular-nums; }
    .sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
  `,
})
export class ImportCardComponent {
  private readonly api = inject(StudentsApi);

  protected readonly hint = IMPORT_HINT;
  protected readonly file = signal<File | null>(null);
  protected readonly busy = signal(false);
  protected readonly result = signal<ImportResult | null>(null);
  protected readonly fileError = signal<string | null>(null);
  /** Mỗi ô sai của dòng bị bỏ qua là một dòng bảng; dòng đã tạo không hiện. */
  protected readonly errorLines = computed<ErrorLine[]>(() =>
    (this.result()?.rows ?? [])
      .filter((r) => r.status === 'Skipped')
      .flatMap((r) => r.errors.map((e) => ({ row: r.row, email: r.email ?? '—', column: e.column, reason: e.reason }))));

  protected pick(event: Event): void {
    this.file.set((event.target as HTMLInputElement).files?.[0] ?? null);
    this.result.set(null);
    this.fileError.set(null);
  }

  protected async run(): Promise<void> {
    const file = this.file();
    if (!file || this.busy()) return;
    this.busy.set(true);
    this.result.set(null);
    this.fileError.set(null);
    try {
      this.result.set(await firstValueFrom(this.api.importStudents(file)));
    } catch (error) {
      this.fileError.set(fileErrorText(error));
    } finally {
      this.busy.set(false);
    }
  }
}

/** Lỗi cả file (@BR-f02-import-excel-014/015/016/019/021): `title` của ProblemDetails; 413 không có body JSON. */
function fileErrorText(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 413) return IMPORT_TOO_LARGE;
    if (typeof error.error?.title === 'string') return error.error.title;
  }
  return IMPORT_FAILED;
}
```

`frontend/projects/admin/src/app/students/student-form.component.ts` — thêm import và khai báo trong `imports`:
```ts
import { ImportCardComponent } from './import-card.component';
```
```ts
  imports: [RouterLink, ReceiveCardComponent, ImportCardComponent],
```

`frontend/projects/admin/src/app/students/student-form.component.html` — thay:
```html
} @else {
  <div class="layout">
```
bằng (card nằm trong luồng `:host` gap 22px, ngay dưới h1, trên form + aside):
```html
} @else {
  @if (!isEdit()) {
    <app-import-card />
  }
  <div class="layout">
```

`frontend/nginx/coaching-common.inc` — thêm dòng đầu tiên trong khối `location /api/ {`:
```nginx
  client_max_body_size 6m; # @BR-f02-import-excel-015: để API tự từ chối file > 5 MB bằng import.file_too_large
```

- [x] **Step 4: Chạy — kỳ vọng PASS**

Run: `cd frontend && npm run test:ci`
Expected: PASS — `ImportCardComponent` 6 test, `StudentFormComponent` thêm 1 test; không test nào failed.
Run: `cd frontend && npx ng build admin`
Expected: build thành công.
Run: `docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build --wait`
Expected: service `web` build lại được (nginx nạp `coaching-common.inc` mới không lỗi cú pháp), `curl -fs http://localhost:4200/health` thành công.

Kiểm tay trên `http://localhost:4200/hoc-vien/moi` (API Task 4/5) ở **390px** và **1280px**, đối chiếu card aside "Hồ sơ đã điền" của `docs/duobexiuuu-coaching-design.html#AdTaoHoSo` và bảng của `#Admin-Dashboard`:
- Card nằm giữa h1 "Tạo hồ sơ học viên" và câu "Điền theo câu trả lời…", rộng bằng vùng main (1280px: trải ngang cả form + aside; 390px: full width, không tràn ngang trang); nền trắng, viền `--line`, radius 12, padding `16px 18px`; h2 Barlow Condensed 700 22px giống "Hồ sơ đã điền".
- Hàng nút: "Chọn file .xlsx" 44px viền `--field` radius 999 chữ 14px 600; tên file 14px `--ink2`; "Import" 44px nền `--primary` chữ trắng, mờ `.6` khi chưa chọn file; ở 390px hàng tự xuống dòng, không nút nào bị cắt. Tab tới ô chọn file → nút "Chọn file .xlsx" có viền focus 2px `--primary`.
- Import file 2 dòng hợp lệ → hộp `--okBg`/`--okInk` "Đã tạo 2, bỏ qua 0" (14px 600, radius 12, padding `10px 14px` như câu báo của form). File có dòng lỗi → hộp `--warnBg`/`--warn` + bảng: header 13px `--muted` weight 500 padding `14px 12px` (cột đầu/cuối 18px), dòng `--rowLine`; ở 390px bảng cuộn ngang trong khung radius 14.
- File .csv đổi đuôi .xlsx → hộp vàng "File phải là Excel .xlsx đọc được."; file 6 MB → "File vượt giới hạn 5 MB.".
- `/hoc-vien/:id/sua` không có card.

- [x] **Step 5: Commit**
```bash
git add frontend/projects/admin/src/app/students frontend/nginx/coaching-common.inc
git commit -F - <<'EOF'
feat(admin): "Import từ Excel" card on the create page with per-row result table

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 8: E2E `@web` import Excel (BR-012, BR-034) + cập nhật e2e F01/F02 theo form mới

Bind 2 scenario `@web` của `features/hoc-vien/ho-so/import-excel.feature`: **BR-f02-import-excel-012** "Coach import file hợp lệ thì hồ sơ được tạo" và **BR-f02-import-excel-034** "Coach thấy kết quả import". File .xlsx sinh lúc chạy trong Node. `frontend/package.json` chưa có package đọc/ghi xlsx hay zip (không có `xlsx`, `exceljs`, `jszip`, `fflate`) → thêm **`fflate@0.8.2`** (MIT, không phụ thuộc, ~30 KB, chỉ devDependency cho e2e) để nén zip; XML SpreadsheetML (workbook, sheet, sharedStrings, styles tối thiểu) tự viết trong `e2e/support/xlsx.ts`. Cập nhật step F01-P1 `FULL_REGISTRATION` đang gửi khoá cũ (sau Task 1 hồ sơ "đủ mục bắt buộc" sẽ thiếu `weightHeightBeforeLifting`/`bodyPhotoUrl` nên gửi thông tin đăng nhập bị từ chối). Step F02 `@web` (BR-003 chỉ họ tên + email, BR-024 tìm kiếm) không dùng mục bị bỏ — không đổi.

**Files:**
- Modify: `frontend/package.json`, `frontend/package-lock.json` (devDependency `fflate` `0.8.2`, pin chính xác)
- Create: `frontend/e2e/support/xlsx.ts`, `frontend/e2e/steps/import.steps.ts`
- Modify: `frontend/e2e/support/world.ts`, `frontend/e2e/steps/account.steps.ts`

**Interfaces:**
- Consumes: Task 4/5 — `POST /api/admin/students/import` → `ImportResultDto` (`rows[].studentId` khi `Created`); lý do "email đã có hồ sơ" ở cột "Địa chỉ email", "phải từ 10 đến 100" ở cột "Tuổi (Age)"; cột theo tiêu đề nguyên văn file mẫu Google Form ("Dấu thời gian" bị bỏ qua, "Địa chỉ email", "Họ Và Tên (Name)", "Tuổi (Age)"). Task 7 — region "Import từ Excel", nhãn "Chọn file .xlsx", nút "Import", `role=status` "Đã tạo {x}, bỏ qua {y}", `tbody tr` × (Dòng, Email, Cột, Lý do). Task 1 — `registration` khoá mới. F02/F01-P1 — step `coach {string} đã đăng nhập`, link "Tạo hồ sơ học viên", `GET /api/admin/students/{id}`, `POST /api/admin/students` (201), `POST /api/auth/login` (`app: 'admin'`).
- Produces: `buildXlsx(rows: XlsxCell[][]): Buffer`; `E2EWorld.importFile`, `importRows`, `importSkipped`, `importResult`; step defs `@web`: `một file Excel theo mẫu Google Form có {int} dòng học viên hợp lệ`, `coach import file đó`, `{int} hồ sơ học viên mới được tạo, có đúng họ tên và email trong file`, `một file Excel có {int} dòng học viên: {int} dòng hợp lệ, {int} dòng trùng email đã có, {int} dòng tuổi sai`, `coach thấy đã tạo {int}, bỏ qua {int}`, `coach thấy lý do của từng dòng bị bỏ qua kèm số dòng Excel`.

**Owns:** —

- [ ] **Step 1: Thêm `fflate`; chạy e2e import — kỳ vọng FAIL (undefined)**

Run: `cd frontend && npm install --save-dev --save-exact fflate@0.8.2`
Expected: `frontend/package.json` `devDependencies` có `"fflate": "0.8.2"`; `package-lock.json` thêm đúng 1 package `node_modules/fflate` (license MIT); `npm ls fflate` → `fflate@0.8.2`.

Run (compose chạy Task 1–7, `deploy/.env` có `AUTH_PERMIT_PER_MINUTE=1000`, `EMAIL_PROVIDER=Capture`):
`cp -n deploy/.env.example deploy/.env; sed -i 's/^AUTH_PERMIT_PER_MINUTE=.*/AUTH_PERMIT_PER_MINUTE=1000/' deploy/.env` rồi `docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build --wait`
Run: `cd frontend && npm run e2e -- --tags "@web and (@BR-f02-import-excel-012 or @BR-f02-import-excel-034)"`
Expected: FAIL — `2 scenarios (2 undefined)` (`một file Excel theo mẫu Google Form có 2 dòng học viên hợp lệ`, `coach import file đó`, `một file Excel có 5 dòng học viên: 3 dòng hợp lệ, 1 dòng trùng email đã có, 1 dòng tuổi sai`, …).

- [ ] **Step 2: Bộ ghi .xlsx tối thiểu**

`frontend/e2e/support/xlsx.ts`:
```ts
// Ghi .xlsx 1 sheet cho e2e import (@BR-f02-import-excel-012 @BR-f02-import-excel-034): zip bằng fflate (MIT),
// XML SpreadsheetML tối thiểu mà ClosedXML đọc được — chuỗi qua sharedStrings, số là ô số, ô trống bỏ qua.
import { strToU8, zipSync } from 'fflate';

export type XlsxCell = string | number | null | undefined;

const MAIN = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main';
const REL = 'http://schemas.openxmlformats.org/officeDocument/2006/relationships';
const PKG_REL = 'http://schemas.openxmlformats.org/package/2006/relationships';
const CT = 'application/vnd.openxmlformats-officedocument.spreadsheetml';
const XML = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>';

const escape = (s: string) =>
  s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

/** 0 → A, 25 → Z, 26 → AA. */
function column(index: number): string {
  let n = index + 1;
  let name = '';
  while (n > 0) {
    const m = (n - 1) % 26;
    name = String.fromCharCode(65 + m) + name;
    n = Math.floor((n - 1) / 26);
  }
  return name;
}

/** `rows[0]` là dòng tiêu đề (dòng Excel 1); `rows[i]` là dòng Excel i + 1. */
export function buildXlsx(rows: XlsxCell[][]): Buffer {
  const strings: string[] = [];
  const indexOf = new Map<string, number>();
  const shared = (s: string) => {
    let i = indexOf.get(s);
    if (i === undefined) {
      i = strings.length;
      strings.push(s);
      indexOf.set(s, i);
    }
    return i;
  };
  const sheetData = rows.map((cells, r) => {
    const xml = cells.map((value, c) => {
      if (value === null || value === undefined || value === '') return '';
      const ref = `${column(c)}${r + 1}`;
      return typeof value === 'number'
        ? `<c r="${ref}"><v>${value}</v></c>`
        : `<c r="${ref}" t="s"><v>${shared(value)}</v></c>`;
    }).join('');
    return `<row r="${r + 1}">${xml}</row>`;
  }).join('');

  const parts: Record<string, string> = {
    '[Content_Types].xml': `<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">`
      + `<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>`
      + `<Default Extension="xml" ContentType="application/xml"/>`
      + `<Override PartName="/xl/workbook.xml" ContentType="${CT}.sheet.main+xml"/>`
      + `<Override PartName="/xl/worksheets/sheet1.xml" ContentType="${CT}.worksheet+xml"/>`
      + `<Override PartName="/xl/sharedStrings.xml" ContentType="${CT}.sharedStrings+xml"/>`
      + `<Override PartName="/xl/styles.xml" ContentType="${CT}.styles+xml"/>`
      + `</Types>`,
    '_rels/.rels': `<Relationships xmlns="${PKG_REL}">`
      + `<Relationship Id="rId1" Type="${REL}/officeDocument" Target="xl/workbook.xml"/></Relationships>`,
    'xl/workbook.xml': `<workbook xmlns="${MAIN}" xmlns:r="${REL}"><sheets>`
      + `<sheet name="Câu trả lời biểu mẫu 1" sheetId="1" r:id="rId1"/></sheets></workbook>`,
    'xl/_rels/workbook.xml.rels': `<Relationships xmlns="${PKG_REL}">`
      + `<Relationship Id="rId1" Type="${REL}/worksheet" Target="worksheets/sheet1.xml"/>`
      + `<Relationship Id="rId2" Type="${REL}/sharedStrings" Target="sharedStrings.xml"/>`
      + `<Relationship Id="rId3" Type="${REL}/styles" Target="styles.xml"/></Relationships>`,
    'xl/worksheets/sheet1.xml': `<worksheet xmlns="${MAIN}"><sheetData>${sheetData}</sheetData></worksheet>`,
    'xl/sharedStrings.xml': `<sst xmlns="${MAIN}" count="${strings.length}" uniqueCount="${strings.length}">`
      + strings.map((s) => `<si><t xml:space="preserve">${escape(s)}</t></si>`).join('') + `</sst>`,
    'xl/styles.xml': `<styleSheet xmlns="${MAIN}">`
      + `<fonts count="1"><font><sz val="11"/><name val="Calibri"/></font></fonts>`
      + `<fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>`
      + `<borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>`
      + `<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>`
      + `<cellXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/></cellXfs>`
      + `</styleSheet>`,
  };
  const files = Object.fromEntries(Object.entries(parts).map(([path, xml]) => [path, strToU8(XML + xml)]));
  return Buffer.from(zipSync(files, { level: 6 }));
}
```

Kiểm nhanh file sinh ra mở được (chỉ là zip hợp lệ có đủ part; ClosedXML đọc được sẽ được chứng minh ở Step 4):
Run: `cd frontend && node --import tsx -e "import('./e2e/support/xlsx.ts').then(({buildXlsx})=>{const p=require('node:path').join(require('node:os').tmpdir(),'e2e-import.xlsx');const b=buildXlsx([['Địa chỉ email','Tuổi (Age)'],['lan@gmail.com',28]]);require('node:fs').writeFileSync(p,b);console.log(p,b.length,b.subarray(0,2).toString())})"`
Expected: in ra đường dẫn, số byte > 500 và `PK`; mở file đó bằng Excel/LibreOffice thấy 2 cột, ô `B2` = 28 (số). Xoá file sau khi xem.

- [ ] **Step 3: World giữ file import; step defs `@web`; F01-P1 dùng khoá mới**

`frontend/e2e/support/world.ts` — thêm import đầu file:
```ts
import type { ImportedLearner, ImportFile, ImportResultBody, SkippedLine } from '../steps/import.steps';
```
và trong `class E2EWorld`, thêm sau `tempPassword = '';`:
```ts
  /** F02 import Excel: file sinh trong scenario, các dòng học viên trong file, dòng dự kiến bị bỏ qua, kết quả API. */
  importFile: ImportFile | null = null;
  importRows: ImportedLearner[] = [];
  importSkipped: SkippedLine[] = [];
  importResult: ImportResultBody | null = null;
```

`frontend/e2e/steps/import.steps.ts`:
```ts
import { Given, Then, When } from '@cucumber/cucumber';
import { expect } from '../support/expect';
import { ADMIN, ADMIN_URL } from '../support/env';
import { buildXlsx } from '../support/xlsx';
import { E2EWorld } from '../support/world';

/** Tiêu đề nguyên văn file câu trả lời Google Form của PO (docs/FORM hồ sơ excel để import.xlsx). */
const COL_TIME = 'Dấu thời gian';
const COL_EMAIL = 'Địa chỉ email';
const COL_NAME = 'Họ Và Tên (Name)';
const COL_AGE = 'Tuổi (Age)';
const HEADERS = [COL_TIME, COL_EMAIL, COL_NAME, COL_AGE];
const XLSX = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';

export interface ImportedLearner { fullName: string; email: string; age: number; }
export interface ImportFile { name: string; mimeType: string; buffer: Buffer; }
export interface SkippedLine { row: number; email: string; column: string; reason: string; }
export interface ImportResultBody {
  created: number;
  skipped: number;
  rows: { row: number; email: string | null; status: 'Created' | 'Skipped'; studentId: string | null; errors: { column: string; reason: string }[] }[];
}

const NAMES = ['Nguyễn Thị Lan', 'Phạm Hoàng Quân', 'Trần Minh An', 'Lê Thu Hà', 'Võ Đức Huy'];

/** DB của compose giữ dữ liệu giữa các lần chạy → email duy nhất cho mỗi dòng. */
const uniqueEmail = (local: string) => `${local}+e2e${Date.now()}${Math.floor(Math.random() * 100000)}@gmail.com`;

function learner(i: number, age = 28): ImportedLearner {
  return { fullName: NAMES[i % NAMES.length], email: uniqueEmail(`import${i}`), age };
}

function useFile(world: E2EWorld, learners: ImportedLearner[]): void {
  world.importRows = learners;
  world.importFile = {
    name: 'cau-tra-loi-google-form.xlsx',
    mimeType: XLSX,
    buffer: buildXlsx([HEADERS, ...learners.map((l) => ['26/11/2026 09:00:00', l.email, l.fullName, l.age])]),
  };
}

async function adminToken(world: E2EWorld): Promise<string> {
  const res = await world.page.request.post(`${ADMIN_URL}/api/auth/login`,
    { data: { email: ADMIN.email, password: ADMIN.password, app: 'admin' } });
  expect(res.ok()).toBe(true);
  return ((await res.json()) as { accessToken: string }).accessToken;
}

const importCard = (world: E2EWorld) => world.page.getByRole('region', { name: 'Import từ Excel' });

// @BR-f02-import-excel-012
Given('một file Excel theo mẫu Google Form có {int} dòng học viên hợp lệ', function (this: E2EWorld, count: number) {
  useFile(this, Array.from({ length: count }, (_, i) => learner(i)));
});

// @BR-f02-import-excel-034: dòng Excel 2.. theo thứ tự: hợp lệ, rồi trùng email đã có, rồi tuổi sai (5 — ngoài 10–100)
Given('một file Excel có {int} dòng học viên: {int} dòng hợp lệ, {int} dòng trùng email đã có, {int} dòng tuổi sai',
  async function (this: E2EWorld, total: number, valid: number, duplicate: number, badAge: number) {
    expect(valid + duplicate + badAge).toBe(total);
    const token = await adminToken(this);
    const learners: ImportedLearner[] = [];
    for (let i = 0; i < valid; i++) learners.push(learner(i));
    for (let i = 0; i < duplicate; i++) {
      const existing = learner(valid + i);
      const res = await this.page.request.post(`${ADMIN_URL}/api/admin/students`, {
        data: { fullName: 'Học viên có sẵn', email: existing.email },
        headers: { authorization: `Bearer ${token}` },
      });
      expect(res.status()).toBe(201);
      learners.push(existing);
      this.importSkipped.push({ row: learners.length + 1, email: existing.email, column: COL_EMAIL, reason: 'email đã có hồ sơ' });
    }
    for (let i = 0; i < badAge; i++) {
      const wrong = learner(valid + duplicate + i, 5);
      learners.push(wrong);
      this.importSkipped.push({ row: learners.length + 1, email: wrong.email, column: COL_AGE, reason: 'phải từ 10 đến 100' });
    }
    useFile(this, learners);
  });

When('coach import file đó', async function (this: E2EWorld) {
  await this.page.getByRole('link', { name: 'Tạo hồ sơ học viên' }).first().click();
  await expect(this.page.getByRole('heading', { name: 'Tạo hồ sơ học viên', level: 1 })).toBeVisible();
  const card = importCard(this);
  await card.getByLabel('Chọn file .xlsx').setInputFiles(this.importFile!);
  await expect(card.getByText(this.importFile!.name, { exact: true })).toBeVisible();
  const [response] = await Promise.all([
    this.page.waitForResponse((r) => r.url().endsWith('/api/admin/students/import') && r.request().method() === 'POST'),
    card.getByRole('button', { name: 'Import', exact: true }).click(),
  ]);
  expect(response.status()).toBe(200);
  this.importResult = (await response.json()) as ImportResultBody;
  await expect(card.getByRole('status')).toBeVisible();
});

// @BR-f02-import-excel-012: coach đã đăng nhập import được — hồ sơ tạo đúng họ tên, email
Then('{int} hồ sơ học viên mới được tạo, có đúng họ tên và email trong file', async function (this: E2EWorld, count: number) {
  await expect(importCard(this).getByRole('status')).toHaveText(`Đã tạo ${count}, bỏ qua 0`);
  const created = this.importResult!.rows.filter((r) => r.status === 'Created');
  expect(created).toHaveLength(count);
  const token = await adminToken(this);
  for (const l of this.importRows) {
    const row = created.find((r) => r.email?.trim().toLowerCase() === l.email.toLowerCase());
    expect(row?.studentId, `không thấy hồ sơ tạo cho ${l.email}`).toBeTruthy();
    const res = await this.page.request.get(`${ADMIN_URL}/api/admin/students/${row!.studentId}`,
      { headers: { authorization: `Bearer ${token}` } });
    expect(res.ok()).toBe(true);
    const detail = (await res.json()) as { fullName: string; email: string };
    expect(detail.fullName).toBe(l.fullName);
    expect(detail.email.toLowerCase()).toBe(l.email.toLowerCase());
  }
});

// @BR-f02-import-excel-034
Then('coach thấy đã tạo {int}, bỏ qua {int}', async function (this: E2EWorld, created: number, skipped: number) {
  await expect(importCard(this).getByRole('status')).toHaveText(`Đã tạo ${created}, bỏ qua ${skipped}`);
});

Then('coach thấy lý do của từng dòng bị bỏ qua kèm số dòng Excel', async function (this: E2EWorld) {
  const card = importCard(this);
  await expect(card.getByRole('columnheader')).toHaveText(['Dòng', 'Email', 'Cột', 'Lý do']);
  const rows = card.locator('tbody tr');
  await expect(rows).toHaveCount(this.importSkipped.length);
  for (const [i, expected] of this.importSkipped.entries()) {
    const cells = (await rows.nth(i).locator('td').allInnerTexts()).map((t) => t.trim());
    expect([cells[0], cells[1].toLowerCase(), cells[2], cells[3]])
      .toEqual([String(expected.row), expected.email.toLowerCase(), expected.column, expected.reason]);
  }
});
```

`frontend/e2e/steps/account.steps.ts` — thay chú thích + hằng `FULL_REGISTRATION`:
```ts
/** Đủ 24 mục bắt buộc của `registration` (+ họ tên, email = 26), giá trị hợp lệ theo RegistrationForm (Google Form, @ADR-f02-import-excel-001). */
const FULL_REGISTRATION = {
  pack: 'Pack 2', price: 3500000, transferDate: '2026-01-15', address: 'Hà Nội', age: 28, heightCm: 170, weightKg: 72.5,
  trainingYears: '1–2 năm', weeklyTrainingTime: '5 buổi, mỗi buổi 75 phút', weightHeightBeforeLifting: '65 kg, 168 cm',
  trainingGoal: 'Giảm mỡ, giữ cơ', medicalHistory: 'Không', foodBudgetReadiness: 'Sẵn sàng', activityLevel: 'Vừa',
  stressLevel: 'Vừa', sleepDuration: '7–8 tiếng', currentDiet: 'Ăn 3 bữa chính', mealAndTrainingTimes: 'Ăn 7h, 12h, 19h. Tập 17h30–19h.',
  sessionsPerWeek: '4 buổi', programStyle: 'Upper / Lower', bodyPhotoUrl: 'https://drive.google.com/open?id=abc',
  expectations: 'Giáo án rõ ràng', previousCoaching: 'Chưa từng', investmentReason: 'Muốn khỏe hơn',
};
```

(`world.ts` chỉ `import type` từ `import.steps.ts` — bị xoá khi `tsx` biên dịch nên không tạo vòng import lúc chạy. e2e không có bước `tsc` riêng; lỗi cú pháp/kiểu lộ ra ở lần chạy Step 4.)

- [ ] **Step 4: Chạy — kỳ vọng PASS**

Run: `docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build --wait` (build lại `api` Task 1–5 và `web` Task 6–7)
Run: `cd frontend && npm run e2e -- --tags "@web and (@BR-f02-import-excel-012 or @BR-f02-import-excel-034)"`
Expected: `2 scenarios (2 passed)`.
Run: `cd frontend && npm run e2e`
Expected: không scenario nào failed/undefined/ambiguous — gồm F00 (đăng nhập, phiên, so ảnh), F02 `@web` BR-003 + BR-024, F01-P1 `@web` BR-002 (hồ sơ đủ 26 mục bắt buộc với `FULL_REGISTRATION` mới gửi được thông tin đăng nhập), 007, 011, 016, 022 và 2 scenario import mới.
Run: `cd frontend && npm run test:ci && npx ng build admin`
Expected: PASS, build thành công.

Kiểm tay sau khi e2e xanh, ở **390px** và **1280px** trên `http://localhost:4200/hoc-vien/moi`: import lại đúng file đã import (dùng file mẫu của PO `docs/FORM hồ sơ excel để import.xlsx`, chạy 2 lần) → lần 2 hộp vàng "Đã tạo 0, bỏ qua {n}", bảng mỗi dòng "Địa chỉ email" / "email đã có hồ sơ" (@BR-f02-import-excel-025); trang Dashboard thấy các hồ sơ mới ở "Chờ kích hoạt", hộp thư `http://localhost:8080/api/test/emails?to=<email>` trả `[]` (@BR-f02-import-excel-029).

- [ ] **Step 5: Commit**
```bash
git add frontend/package.json frontend/package-lock.json frontend/e2e/support/xlsx.ts frontend/e2e/support/world.ts frontend/e2e/steps/import.steps.ts frontend/e2e/steps/account.steps.ts
git commit -F - <<'EOF'
test(e2e): Excel import web scenarios (BR-f02-import-excel-012, 034); account steps use the Google Form keys

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

> **Kiểm tay sau Task 8 (NFR-f02-import-excel-001):** import file 500 dòng hợp lệ trên local compose (và staging sau khi push) — thời gian phản hồi < 60 s; ghi kết quả vào báo cáo verify-done. Nếu `dotnet restore` không tìm thấy ClosedXML 0.105.1 ở Task 4, dùng bản 0.105.x mới nhất có sẵn (MIT) và ghi lại trong báo cáo.
