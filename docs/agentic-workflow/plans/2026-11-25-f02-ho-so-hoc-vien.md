---
doc-id: 2026-11-25-f02-ho-so-hoc-vien
status: ready
---

# F02 — Hồ sơ học viên & trạng thái coaching: Implementation Plan

> Frontmatter `status: draft | ready` là gate (exec-plan yêu cầu spec + tech-design + plan đều `ready`). User mark Ready sau khi review.

> **For agentic workers:** REQUIRED SUB-SKILL: Use `exec-plan` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Coach tạo, sửa, ghi chú nội bộ, đổi trạng thái coaching và tìm hồ sơ học viên (BR-f02-ho-so-hoc-vien-001→025) qua `/api/admin/students/*` và 3 màn admin; `/api/client/me` trả dữ liệu học viên không có ghi chú nội bộ.

**Architecture:** Aggregate `Student` (Domain) giữ mọi invariant: form đăng ký là owned value object `RegistrationForm` lưu JSON, lịch sử trạng thái là entity con chỉ thêm, `SearchKey` bỏ dấu để tìm kiếm. Tạo hồ sơ = tạo tài khoản Identity "Chờ kích hoạt" + `Student` trong một transaction; mọi lệnh sửa chặn ghi đè bằng `rowversion`. Web admin Angular thêm 4 route (`dashboard`, `hoc-vien/moi`, `hoc-vien/:id`, `hoc-vien/:id/sua`) với một form dùng chung cho tạo và sửa.

**Tech Stack:** .NET 10.0.404, EF Core 10.0.15 (`OwnsOne(...).ToJson()`, `IsRowVersion`), ASP.NET Identity, Liquibase 4.33.0, Reqnroll.xUnit 3.3.4, Testcontainers.MsSql 4.17.0, Microsoft.Extensions.TimeProvider.Testing 10.11.1 · Angular 22.3, Vitest · @cucumber/cucumber 13.3, playwright 1.65.1.

## Global Constraints

- Làm trên nhánh `main`, không worktree (quyết định của user). Không push.
- Dependency rule: `Api → Application → Domain`; `Infrastructure → Application, Domain`; Domain không tham chiếu EF/ASP.NET.
- Không gọi `DateTime.Now`/`DateTime.UtcNow` trong Domain/Application; thời gian đi qua `IClock` (`UtcNow`, `Today` = ngày giờ VN).
- Schema chỉ đổi qua Liquibase `database/changelog/v0.2.0/` (changeset XML → `scripts/*.sql` + `rollback/*.sql`, tag `v0.2.0`); KHÔNG dùng `dotnet ef migrations`.
- Package Microsoft.* giữ dòng 10.0.x qua `backend/Directory.Packages.props`; F02 không thêm package nào.
- Giới hạn độ dài (ADR-f02-ho-so-hoc-vien-009), nguyên văn: họ tên ≤ 100; văn bản một dòng ≤ 200; đoạn văn ≤ 2000; ghi chú nội bộ ≤ 5000; lý do đổi trạng thái ≤ 500; URL ≤ 2048.
- Khoảng số (BR-f02-ho-so-hoc-vien-008), nguyên văn: tuổi là số nguyên 10–100; chiều cao 100–250 cm; cân nặng 20–300 kg; mỡ cơ thể 3–70 %; mỗi vòng đo 10–250 cm; giá là số nguyên từ 0 đồng.
- Mã lỗi: 400 `student.*`/`email.*` (DomainException → `GlobalExceptionHandler`), 404 `student.not_found`, 409 `student.email_taken`, 409 `student.version_conflict`. ProblemDetails luôn có `code`.
- Mọi lệnh sửa (`PUT` hồ sơ, `PUT` ghi chú, `POST` đổi trạng thái) bắt buộc gửi `version` (base64 của `rowversion`).
- Không có endpoint `DELETE` hồ sơ, không có API sửa/xóa lịch sử trạng thái.
- Ghi chú nội bộ không bao giờ có trong DTO của `/api/client/*`.
- Audit `coaching_status.changed` chỉ ghi `From→To`; không ghi lý do, không ghi ghi chú (NFR-f02-ho-so-hoc-vien-002).
- Trạng thái gửi/nhận dạng chuỗi `"Active" | "Paused" | "Ended"` và `"PendingActivation" | "Activated"`; FE hiển thị tiếng Việt: Đang coaching / Tạm dừng / Đã kết thúc, Chờ kích hoạt / Đã kích hoạt.
- FE bám **chính xác** prototype `docs/duobexiuuu-coaching-design.html` (page `Admin-Dashboard`, `AdTaoHoSo`, `AdHoSo`); breakpoint duy nhất 880px; không hard-code màu, luôn `var(--…)`; kiểm ở 390px và 1280px.
- Route admin tiếng Việt (ADR-f02-ho-so-hoc-vien-008): `dashboard`, `hoc-vien/moi`, `hoc-vien/:id`, `hoc-vien/:id/sua`. Bản nháp tạo hồ sơ: `localStorage['dbx.admin.student-draft']`, chỉ ở chế độ tạo, xóa khi lưu thành công hoặc "Xoá trắng hồ sơ" (NFR-f02-ho-so-hoc-vien-004).
- Số liệu nhập dấu phẩy thập phân ("72,5"), gửi API dạng JSON number (72.5); ngày hiển thị `dd/MM/yyyy`.
- Thông báo 409 phía FE, nguyên văn: `Hồ sơ vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.` kèm nút "Tải lại".
- Living docs `features/hoc-vien/ho-so/ho-so-hoc-vien.feature` đã chốt ở to-bdd — KHÔNG sửa nội dung `.feature`. Step `@api` chạy Reqnroll (`Category!=web`), step `@web` chạy CucumberJS.
- NFR-f02-ho-so-hoc-vien-005 (p95 < 500 ms với 500 hồ sơ) không có task riêng: danh sách 1 truy vấn theo index `CoachId, CreatedAt` + `LIKE` trên `SearchKey`, chi tiết 1 truy vấn; kiểm thủ công trên môi trường test sau khi xong Task 10.
- NFR-f02-ho-so-hoc-vien-003 nằm ở hằng số độ dài (Task 1, 2, 4, 5).
- Annotation truy vết: chỗ code/test hiện thực BR/ADR mang comment `// @BR-f02-ho-so-hoc-vien-NNN` / `// @ADR-f02-ho-so-hoc-vien-NNN` (SQL: `-- @ADR-…`, TS: `// @BR-…`).
- Lệnh verify tổng: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"` và `cd frontend && npm run test:ci`.
- Commit message kết thúc bằng đúng hai dòng:
  ```
  Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
  ```

---

## File Structure

```
database/changelog/db.changelog-master.xml                                   (M) include v0.2.0
database/changelog/v0.2.0/{changelog.xml, 0001-students.xml}                 (C)
database/changelog/v0.2.0/scripts/0001-students.sql, rollback/0001-students.rollback.sql (C)
backend/src/Coaching.Domain/Students/
  AccountStatus.cs, CoachingStatus.cs, Student.cs, IStudentRepository.cs     (C, T1; Student/IStudentRepository mở rộng ở T2–T6)
  RegistrationInput.cs, RegistrationForm.cs, RegistrationOptions.cs, RegistrationFields.cs (C, T2)
  CoachingStatusChange.cs                                                    (C, T5)
  SearchText.cs, StudentSummary.cs                                           (C, T6)
backend/src/Coaching.Application/
  Abstractions/IUnitOfWork.cs (M), Common/ConcurrencyConflictException.cs (C, T3), DependencyInjection.cs (M)
  Coaches/CurrentCoach.cs                                                    (C)
  Students/{IStudentAccounts, StudentErrors, StudentDtos, StudentVersion}.cs (C)
  Students/Commands/{CreateStudent, UpdateStudent, UpdateInternalNote, ChangeCoachingStatus}.cs (C)
  Students/Queries/{GetStudent, GetStudentSelf, ListStudents}.cs             (C)
backend/src/Coaching.Infrastructure/
  Persistence/AppDbContext.cs (M), Persistence/Configurations/StudentConfiguration.cs (C)
  Persistence/Repositories/StudentRepository.cs (C), Identity/StudentAccounts.cs (C)
  Seeding/DatabaseInitializer.cs (M), DependencyInjection.cs (M)
backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs (C), Controllers/Client/MeController.cs (M)
backend/tests/Coaching.Domain.Tests/Students/*.cs                            (C)
backend/tests/Coaching.TestSupport/RegistrationSamples.cs                    (C, T2)
backend/tests/Coaching.Acceptance/Support/{AcceptanceContext (M), StudentApi (C), StepText (C), RegistrationLabels (C)}.cs
backend/tests/Coaching.Acceptance/Steps/{StudentCreate, StudentRegistration, StudentEdit, StudentNote, CoachingStatus, StudentList}Steps.cs (C)
backend/tests/Coaching.Api.IntegrationTests/ApiFactory.cs (M), Students/*.cs (C), Architecture/ClientDtoTests.cs (C)
frontend/projects/shared/src/lib/ui/status-badge.component.ts (+spec), icons.ts (M), public-api.ts (M)
frontend/projects/admin/src/app/app.routes.ts (M), shell/admin-shell.component.ts (M), home/** (D)
frontend/projects/admin/src/app/students/
  student.models.ts, student-status.ts, students.api.ts, registration-fields.ts, format.ts
  student-list.component.ts (+spec), student-form.component.{ts,html,scss} (+spec)
  student-profile.component.{ts,html,scss} (+spec), coaching-status-panel.component.ts
frontend/e2e/steps/{login,session}.steps.ts (M), student-profile.steps.ts (C)
CLAUDE.md (M) bảng route
```

---

### Task 1: Tạo hồ sơ tối thiểu — schema v0.2.0, `Student`, tài khoản Chờ kích hoạt, POST/GET

**Files:**
- Create: `database/changelog/v0.2.0/changelog.xml`, `database/changelog/v0.2.0/0001-students.xml`, `database/changelog/v0.2.0/scripts/0001-students.sql`, `database/changelog/v0.2.0/rollback/0001-students.rollback.sql`
- Modify: `database/changelog/db.changelog-master.xml`
- Create: `backend/src/Coaching.Domain/Students/AccountStatus.cs`, `CoachingStatus.cs`, `Student.cs`, `IStudentRepository.cs`
- Create: `backend/src/Coaching.Application/Coaches/CurrentCoach.cs`
- Create: `backend/src/Coaching.Application/Students/IStudentAccounts.cs`, `StudentErrors.cs`, `StudentDtos.cs`, `Commands/CreateStudent.cs`, `Queries/GetStudent.cs`
- Modify: `backend/src/Coaching.Application/Abstractions/IUnitOfWork.cs`, `backend/src/Coaching.Application/DependencyInjection.cs`
- Create: `backend/src/Coaching.Infrastructure/Persistence/Configurations/StudentConfiguration.cs`, `Persistence/Repositories/StudentRepository.cs`, `Identity/StudentAccounts.cs`
- Modify: `backend/src/Coaching.Infrastructure/Persistence/AppDbContext.cs`, `backend/src/Coaching.Infrastructure/DependencyInjection.cs`, `backend/src/Coaching.Infrastructure/Seeding/DatabaseInitializer.cs`
- Create: `backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs`
- Test: `backend/tests/Coaching.Domain.Tests/Students/StudentTests.cs`
- Test: `backend/tests/Coaching.Acceptance/Support/AcceptanceContext.cs` (M), `backend/tests/Coaching.Acceptance/Support/StudentApi.cs`, `backend/tests/Coaching.Acceptance/Steps/StudentCreateSteps.cs`
- Test: `backend/tests/Coaching.Api.IntegrationTests/ApiFactory.cs` (M), `backend/tests/Coaching.Api.IntegrationTests/Students/StudentRequests.cs`, `Students/CreateStudentTests.cs`, `Students/DemoSeedStudentTests.cs`

**Interfaces:**
- Consumes (F00): `Email.Create(string?)`, `DomainException(code, message)`, `AggregateRoot<Guid>`, `ICoachRepository.GetByUserIdAsync`, `ICurrentUser.UserId`, `IClock.UtcNow/Today`, `Result<T>`/`Error`/`ErrorKind`, `ResultExtensions.ToActionResult/ToProblem`, `AdminControllerBase`, `AcceptanceContext.LoginOkAsync/GetAsync/Clock/Host`, `ApiFactory.LoginAsync`.
- Produces:
  - Domain `Coaching.Domain.Students`: `enum AccountStatus { PendingActivation, Activated }`, `enum CoachingStatus { Active, Paused, Ended }`, `Student` (`Id, CoachId, UserId, FullName, Email, AccountStatus, CoachingStatus, CreatedAt`, `const FullNameMaxLength = 100`, `static Student Create(Guid coachId, Guid userId, string? fullName, Email email, DateTimeOffset at)`), `IStudentRepository { Task<Student?> GetAsync(Guid id, Guid coachId, CancellationToken ct = default); void Add(Student student); }`.
  - Application: `CurrentCoach.GetAsync(CancellationToken) → Task<Coach?>`; `IStudentAccounts.CreatePendingAsync(Guid userId, Email email, string fullName, CancellationToken) → Task<Result>`; `StudentErrors.NotFound/CoachNotFound/EmailTaken`; `StudentDetailDto` + `StudentDetailDto.From(Student, Coach)`; `CreateStudentCommand(string? FullName, string? Email)`, `CreateStudentHandler.HandleAsync(cmd, ct) → Result<StudentDetailDto>`; `GetStudentHandler.HandleAsync(Guid id, ct)`; `IUnitOfWork.ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<Result<T>>>, CancellationToken) → Task<Result<T>>`.
  - Infrastructure: `AppDbContext.Students`.
  - API: `POST /api/admin/students` → 201 + `StudentDetailDto`; `GET /api/admin/students/{id}` → 200 / 404.
  - Test: `AcceptanceContext.SendJsonAsync(HttpMethod, string url, object? body, string? accessToken)`; `StudentApi` (`AdminToken`, `Ids`, `LastDetail`, `LoginCoachAsync()`, `MoveClockToAsync(DateTimeOffset)`, `TryCreateAsync(string? fullName, string? email)`, `CreateOkAsync(string fullName, string? email = null)`, `EnsureAsync(string fullName)`, `GetAsync(string fullName)`, `AssertErrorAsync(HttpStatusCode, string code)`, `static NewEmail()`, `static ErrorCodeAsync(HttpResponseMessage)`, `static CoachingStatuses`, `static AccountStatuses`); `ApiFactory.AdminClientAsync()`; `StudentRequests.Url/NewEmail()/CreateAsync(HttpClient, string fullName)/ErrorCodeAsync(...)`.

**Owns:** `@BR-f02-ho-so-hoc-vien-001`, `@BR-f02-ho-so-hoc-vien-002`, `@BR-f02-ho-so-hoc-vien-003`, `@BR-f02-ho-so-hoc-vien-005`, `@BR-f02-ho-so-hoc-vien-006`, `@BR-f02-ho-so-hoc-vien-007` | `@ADR-f02-ho-so-hoc-vien-001`

- [x] **Step 1: Liquibase v0.2.0 — hai bảng mới**

`database/changelog/v0.2.0/scripts/0001-students.sql`:
```sql
-- v0.2.0-0001: hồ sơ học viên + lịch sử trạng thái coaching
-- @ADR-f02-ho-so-hoc-vien-002: mục form trong cột JSON Registration; cột riêng chỉ cho dữ liệu cần truy vấn
-- @ADR-f02-ho-so-hoc-vien-004: Version rowversion chặn ghi đè
CREATE TABLE [Students] (
    [Id] uniqueidentifier NOT NULL,
    [CoachId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [SearchKey] nvarchar(400) NOT NULL CONSTRAINT [DF_Students_SearchKey] DEFAULT N'',
    [AccountStatus] nvarchar(20) NOT NULL,
    [CoachingStatus] nvarchar(20) NOT NULL,
    [Registration] nvarchar(max) NOT NULL CONSTRAINT [DF_Students_Registration] DEFAULT N'{}',
    [InternalNote] nvarchar(max) NULL,
    [InternalNoteUpdatedAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [Version] rowversion NOT NULL,
    CONSTRAINT [PK_Students] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Students_Coaches_CoachId] FOREIGN KEY ([CoachId]) REFERENCES [Coaches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Students_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);
GO
CREATE UNIQUE INDEX [IX_Students_Email] ON [Students] ([Email]);
GO
CREATE UNIQUE INDEX [IX_Students_UserId] ON [Students] ([UserId]);
GO
CREATE INDEX [IX_Students_CoachId_CreatedAt] ON [Students] ([CoachId], [CreatedAt]);
GO
-- @ADR-f02-ho-so-hoc-vien-005: lịch sử trạng thái coaching, chỉ thêm
CREATE TABLE [StudentCoachingStatusChanges] (
    [Id] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [FromStatus] nvarchar(20) NOT NULL,
    [ToStatus] nvarchar(20) NOT NULL,
    [ActorUserId] uniqueidentifier NOT NULL,
    [At] datetimeoffset NOT NULL,
    [Reason] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_StudentCoachingStatusChanges] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentCoachingStatusChanges_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO
CREATE INDEX [IX_StudentCoachingStatusChanges_StudentId_At] ON [StudentCoachingStatusChanges] ([StudentId], [At]);
GO
```

`database/changelog/v0.2.0/rollback/0001-students.rollback.sql`:
```sql
-- Rollback v0.2.0-0001: xóa bảng theo thứ tự ngược khóa ngoại
DROP TABLE IF EXISTS [StudentCoachingStatusChanges];
GO
DROP TABLE IF EXISTS [Students];
GO
```

`database/changelog/v0.2.0/0001-students.xml`:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog http://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-4.33.xsd">
  <changeSet id="v0.2.0-0001-students" author="duc.hm4@kiotviet.com">
    <comment>F02: Students (form đăng ký JSON, rowversion) + StudentCoachingStatusChanges</comment>
    <sqlFile path="scripts/0001-students.sql" relativeToChangelogFile="true" splitStatements="true" endDelimiter="\nGO" stripComments="false"/>
    <rollback>
      <sqlFile path="rollback/0001-students.rollback.sql" relativeToChangelogFile="true" splitStatements="true" endDelimiter="\nGO"/>
    </rollback>
  </changeSet>
</databaseChangeLog>
```

`database/changelog/v0.2.0/changelog.xml`:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!-- v0.2.0 = P1 (F02). Thêm changeset mới TRƯỚC changeset tag. -->
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog http://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-4.33.xsd">
  <include file="0001-students.xml" relativeToChangelogFile="true"/>

  <changeSet id="v0.2.0-tag" author="duc.hm4@kiotviet.com">
    <tagDatabase tag="v0.2.0"/>
  </changeSet>
</databaseChangeLog>
```

`database/changelog/db.changelog-master.xml` — thêm sau dòng include `v0.1.0`:
```xml
  <include file="v0.2.0/changelog.xml" relativeToChangelogFile="true"/>
```

- [x] **Step 2: Viết Domain test cho `Student.Create`**

`backend/tests/Coaching.Domain.Tests/Students/StudentTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class StudentTests
{
    private static readonly Guid CoachId = Guid.NewGuid();
    private static readonly DateTimeOffset At = new(2026, 11, 25, 2, 0, 0, TimeSpan.Zero);
    private static readonly Email LanEmail = Email.Create("lan@gmail.com");

    // @BR-f02-ho-so-hoc-vien-005 @BR-f02-ho-so-hoc-vien-006 @BR-f02-ho-so-hoc-vien-007
    [Fact]
    public void Create_sets_owner_and_default_statuses()
    {
        var userId = Guid.NewGuid();

        var student = Student.Create(CoachId, userId, "  Lan  ", LanEmail, At);

        Assert.NotEqual(Guid.Empty, student.Id);
        Assert.Equal(CoachId, student.CoachId);
        Assert.Equal(userId, student.UserId);
        Assert.Equal("Lan", student.FullName);
        Assert.Equal(LanEmail, student.Email);
        Assert.Equal(AccountStatus.PendingActivation, student.AccountStatus);
        Assert.Equal(CoachingStatus.Active, student.CoachingStatus);
        Assert.Equal(At, student.CreatedAt);
    }

    // @BR-f02-ho-so-hoc-vien-003
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_requires_full_name(string? fullName)
    {
        var ex = Assert.Throws<DomainException>(() => Student.Create(CoachId, Guid.NewGuid(), fullName, LanEmail, At));
        Assert.Equal("student.full_name_required", ex.Code);
    }

    // @BR-f02-ho-so-hoc-vien-003 @ADR-f02-ho-so-hoc-vien-009
    [Fact]
    public void Create_accepts_100_characters_and_rejects_101()
    {
        Assert.Equal(100, Student.Create(CoachId, Guid.NewGuid(), new string('a', 100), LanEmail, At).FullName.Length);

        var ex = Assert.Throws<DomainException>(() =>
            Student.Create(CoachId, Guid.NewGuid(), new string('a', 101), LanEmail, At));
        Assert.Equal("student.full_name_too_long", ex.Code);
    }

    [Fact]
    public void Create_requires_coach_and_login_account()
    {
        Assert.Equal("student.coach_required",
            Assert.Throws<DomainException>(() => Student.Create(Guid.Empty, Guid.NewGuid(), "Lan", LanEmail, At)).Code);
        Assert.Equal("student.user_required",
            Assert.Throws<DomainException>(() => Student.Create(CoachId, Guid.Empty, "Lan", LanEmail, At)).Code);
    }
}
```

- [x] **Step 3: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students"`
Expected: FAIL — build error `The type or namespace name 'Students' does not exist in the namespace 'Coaching.Domain'`.

- [x] **Step 4: Domain — trạng thái, aggregate, repository**

`backend/src/Coaching.Domain/Students/AccountStatus.cs`:
```csharp
namespace Coaching.Domain.Students;

/// <summary>Trạng thái tài khoản đăng nhập của học viên. Chuyển sang Activated do F01 (kích hoạt) xử lý.</summary>
public enum AccountStatus
{
    PendingActivation,
    Activated,
}
```

`backend/src/Coaching.Domain/Students/CoachingStatus.cs`:
```csharp
namespace Coaching.Domain.Students;

public enum CoachingStatus
{
    Active,
    Paused,
    Ended,
}
```

`backend/src/Coaching.Domain/Students/Student.cs`:
```csharp
using Coaching.Domain.Common;

namespace Coaching.Domain.Students;

/// <summary>
/// Hồ sơ học viên: form đăng ký, ghi chú nội bộ, trạng thái tài khoản và trạng thái coaching.
/// Không có thao tác xóa — ngừng coaching bằng trạng thái Ended.
/// </summary>
public sealed class Student : AggregateRoot<Guid>
{
    // @ADR-f02-ho-so-hoc-vien-009
    public const int FullNameMaxLength = 100;

    public Guid CoachId { get; private set; }

    /// <summary>Id tài khoản đăng nhập (Identity user) tạo cùng hồ sơ.</summary>
    public Guid UserId { get; private set; }

    public string FullName { get; private set; } = default!;

    public Email Email { get; private set; } = default!;

    public AccountStatus AccountStatus { get; private set; }

    public CoachingStatus CoachingStatus { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private Student() { }

    private Student(Guid id) : base(id) { }

    public static Student Create(Guid coachId, Guid userId, string? fullName, Email email, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(email);
        if (coachId == Guid.Empty)
            throw new DomainException("student.coach_required", "Hồ sơ phải thuộc một coach.");
        if (userId == Guid.Empty)
            throw new DomainException("student.user_required", "Hồ sơ phải gắn với một tài khoản đăng nhập.");

        return new Student(Guid.CreateVersion7())
        {
            CoachId = coachId, // @BR-f02-ho-so-hoc-vien-007
            UserId = userId,
            FullName = NormalizeFullName(fullName), // @BR-f02-ho-so-hoc-vien-003
            Email = email, // @BR-f02-ho-so-hoc-vien-001 @BR-f02-ho-so-hoc-vien-002: Email đã chuẩn hóa (trim + lowercase)
            AccountStatus = AccountStatus.PendingActivation, // @BR-f02-ho-so-hoc-vien-005
            CoachingStatus = CoachingStatus.Active, // @BR-f02-ho-so-hoc-vien-006
            CreatedAt = at,
        };
    }

    // @BR-f02-ho-so-hoc-vien-003
    private static string NormalizeFullName(string? fullName)
    {
        var name = fullName?.Trim();
        if (string.IsNullOrEmpty(name))
            throw new DomainException("student.full_name_required", "Họ tên là bắt buộc.");
        if (name.Length > FullNameMaxLength)
            throw new DomainException("student.full_name_too_long", $"Họ tên tối đa {FullNameMaxLength} ký tự.");
        return name;
    }
}
```

`backend/src/Coaching.Domain/Students/IStudentRepository.cs`:
```csharp
namespace Coaching.Domain.Students;

public interface IStudentRepository
{
    /// <summary>Hồ sơ thuộc coach; hồ sơ của coach khác coi như không tồn tại.</summary>
    Task<Student?> GetAsync(Guid id, Guid coachId, CancellationToken cancellationToken = default);

    void Add(Student student);
}
```

- [x] **Step 5: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students"`
Expected: `Passed: 6`.

- [x] **Step 6: Hạ tầng test acceptance — đồng hồ cố định, gửi JSON có token, helper học viên tạo kèm `Student`**

`backend/tests/Coaching.Acceptance/Support/AcceptanceContext.cs` — thay toàn bộ:
```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Abstractions;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Coaching.Acceptance.Support;

/// <summary>Trạng thái của 1 scenario (Reqnroll tạo mới cho mỗi scenario).</summary>
public sealed class AcceptanceContext : IDisposable
{
    public const string AdminEmail = "admin@acceptance.local";
    public const string AdminPassword = "Admin@12345";
    public const string AdminName = "Coach Duobexiuuu";

    /// <summary>
    /// Mọi scenario bắt đầu lúc 01/11/2026 00:00 UTC. FakeTimeProvider chỉ tua tiến, nên step "hôm nay là …"
    /// (≥ 01/11/2026) luôn tua được. JwtBearer và mọi nghiệp vụ đã đọc giờ qua TimeProvider (F00 Task 2).
    /// </summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero));

    private ApiHost? _host;

    public Dictionary<string, string?> Settings { get; } = new()
    {
        ["Jwt:SigningKey"] = "acceptance-signing-key-0123456789abcdef",
        ["AdminSeed:Email"] = AdminEmail,
        ["AdminSeed:Password"] = AdminPassword,
        ["AdminSeed:FullName"] = AdminName,
        ["Email:Provider"] = "Log",
        ["RateLimiting:AuthPermitPerMinute"] = "1000",
    };

    public string ConnectionString { get; } = SqlServerHooks.NewDatabaseConnectionString();

    public Exception? StartupError { get; private set; }

    public HttpResponseMessage? LastResponse { get; set; }

    public AuthTokens? Tokens { get; set; }

    public ApiHost Host
    {
        get
        {
            if (_host is null)
                TryStart();
            return _host ?? throw new InvalidOperationException("API không khởi động được.", StartupError);
        }
    }

    /// <summary>Khởi động (lại) API trên cùng database; lỗi khởi động được ghi vào StartupError.</summary>
    public void TryStart()
    {
        _host?.Dispose();
        _host = null;
        StartupError = null;
        var host = new ApiHost(ConnectionString, Settings, Clock);
        try
        {
            _ = host.Services; // buộc host khởi động → seed
            _host = host;
        }
        catch (Exception ex)
        {
            StartupError = ex;
            host.Dispose();
        }
    }

    public HttpClient Client() => Host.CreateClient();

    public async Task<HttpResponseMessage> PostJsonAsync(string url, object body, string? ip = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        if (ip is not null)
            request.Headers.Add("X-Forwarded-For", ip);
        LastResponse = await Client().SendAsync(request);
        return LastResponse;
    }

    /// <summary>Gửi request có body JSON (tùy chọn) và Bearer token (tùy chọn).</summary>
    public async Task<HttpResponseMessage> SendJsonAsync(HttpMethod method, string url, object? body, string? accessToken)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        if (accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        LastResponse = await Client().SendAsync(request);
        return LastResponse;
    }

    public Task<HttpResponseMessage> LoginAsync(string email, string password, string app, string? ip = null) =>
        PostJsonAsync("/api/auth/login", new { email, password, app }, ip);

    public async Task<AuthTokens> LoginOkAsync(string email, string password, string app)
    {
        var response = await LoginAsync(email, password, app);
        response.EnsureSuccessStatusCode();
        Tokens = (await response.Content.ReadFromJsonAsync<AuthTokens>())!;
        return Tokens;
    }

    public async Task<HttpResponseMessage> GetAsync(string url, string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        LastResponse = await Client().SendAsync(request);
        return LastResponse;
    }

    /// <summary>
    /// Tạo học viên đăng nhập được (luồng mời làm ở F01): tài khoản Identity có mật khẩu + hồ sơ Student của coach admin.
    /// </summary>
    public async Task<(string Email, string Password)> CreateStudentAsync()
    {
        var email = $"hocvien-{Guid.NewGuid():N}@acceptance.local";
        const string password = "Student@123";
        await using var scope = Host.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var users = sp.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser { UserName = email, Email = email, FullName = "Học viên Acceptance", EmailConfirmed = true };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
            throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, Roles.Student);

        // @ADR-f02-ho-so-hoc-vien-001: học viên luôn có hồ sơ Student (/api/client/me đọc từ Student)
        var db = sp.GetRequiredService<AppDbContext>();
        var adminEmail = Email.Create(AdminEmail);
        var coach = await db.Coaches.SingleAsync(c => c.Email == adminEmail);
        db.Students.Add(Student.Create(coach.Id, user.Id, user.FullName, Email.Create(email), sp.GetRequiredService<IClock>().UtcNow));
        await db.SaveChangesAsync();
        return (email, password);
    }

    public void Dispose() => _host?.Dispose();
}
```

`backend/tests/Coaching.Acceptance/Support/StudentApi.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Application.Students;

namespace Coaching.Acceptance.Support;

/// <summary>Gọi /api/admin/students bằng phiên coach; nhớ Id hồ sơ theo họ tên trong scenario.</summary>
public sealed class StudentApi(AcceptanceContext ctx)
{
    public static readonly IReadOnlyDictionary<string, string> CoachingStatuses = new Dictionary<string, string>
    {
        ["Đang coaching"] = "Active",
        ["Tạm dừng"] = "Paused",
        ["Đã kết thúc"] = "Ended",
    };

    public static readonly IReadOnlyDictionary<string, string> AccountStatuses = new Dictionary<string, string>
    {
        ["Chờ kích hoạt"] = "PendingActivation",
        ["Đã kích hoạt"] = "Activated",
    };

    public string? AdminToken { get; private set; }

    public Dictionary<string, Guid> Ids { get; } = new();

    public StudentDetailDto? LastDetail { get; private set; }

    public async Task LoginCoachAsync() =>
        AdminToken = (await ctx.LoginOkAsync(AcceptanceContext.AdminEmail, AcceptanceContext.AdminPassword, "admin")).AccessToken;

    /// <summary>Tua đồng hồ tới <paramref name="instant"/> (chỉ tiến) rồi đăng nhập lại để phiên coach còn hạn.</summary>
    public async Task MoveClockToAsync(DateTimeOffset instant)
    {
        if (instant < ctx.Clock.GetUtcNow())
            throw new InvalidOperationException($"Không lùi được đồng hồ từ {ctx.Clock.GetUtcNow():O} về {instant:O}.");
        ctx.Clock.SetUtcNow(instant);
        if (AdminToken is not null)
            await LoginCoachAsync();
    }

    public async Task<HttpResponseMessage> TryCreateAsync(string? fullName, string? email)
    {
        var response = await ctx.SendJsonAsync(HttpMethod.Post, "/api/admin/students", new { fullName, email }, AdminToken);
        await RememberCreatedAsync(response, fullName);
        return response;
    }

    public async Task<StudentDetailDto> CreateOkAsync(string fullName, string? email = null)
    {
        var response = await TryCreateAsync(fullName, email ?? NewEmail());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return LastDetail!;
    }

    public async Task<Guid> EnsureAsync(string fullName) =>
        Ids.TryGetValue(fullName, out var id) ? id : (await CreateOkAsync(fullName)).Id;

    public async Task<StudentDetailDto> GetAsync(string fullName)
    {
        var response = await ctx.GetAsync($"/api/admin/students/{Ids[fullName]}", AdminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        LastDetail = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        return LastDetail;
    }

    public async Task AssertErrorAsync(HttpStatusCode status, string code)
    {
        Assert.Equal(status, ctx.LastResponse!.StatusCode);
        Assert.Equal(code, await ErrorCodeAsync(ctx.LastResponse));
    }

    /// <summary>Email ngẫu nhiên chỉ gồm chữ số hex → không khớp từ khóa tìm kiếm trong living doc.</summary>
    public static string NewEmail() => $"s{Guid.NewGuid():N}@acceptance.local";

    public static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private async Task RememberCreatedAsync(HttpResponseMessage response, string? fullName)
    {
        if (response.StatusCode != HttpStatusCode.Created)
            return;
        LastDetail = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        if (!string.IsNullOrWhiteSpace(fullName))
            Ids[fullName.Trim()] = LastDetail.Id;
    }
}
```

- [x] **Step 7: Viết step defs `@api` cho BR-001, 002, 003, 005, 006, 007**

`backend/tests/Coaching.Acceptance/Steps/StudentCreateSteps.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Coaching.Acceptance.Support;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Coaches;
using Coaching.Domain.Common;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class StudentCreateSteps(AcceptanceContext ctx, StudentApi api)
{
    // Background của ho-so-hoc-vien.feature
    [Given(@"^coach ""([^""]*)"" đã đăng nhập$")]
    public async Task CoachLoggedIn(string coachName)
    {
        Assert.Equal(AcceptanceContext.AdminName, coachName);
        await api.LoginCoachAsync();
    }

    // @BR-f02-ho-so-hoc-vien-001
    [Given(@"^chưa có hồ sơ nào dùng email ""([^""]*)""$")]
    public async Task NoStudentUsesEmail(string email)
    {
        var value = Email.Create(email);
        await using var scope = ctx.Host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Students.AnyAsync(s => s.Email == value));
    }

    // @BR-f02-ho-so-hoc-vien-001
    [Given(@"^email ""([^""]*)"" đã thuộc về (một hồ sơ đang coaching|tài khoản coach|một hồ sơ đã kết thúc coaching)$")]
    public async Task EmailBelongsTo(string email, string owner)
    {
        switch (owner)
        {
            case "một hồ sơ đang coaching":
                await api.CreateOkAsync("Chủ email", email);
                break;
            case "một hồ sơ đã kết thúc coaching":
                var ended = await api.CreateOkAsync("Chủ email", email);
                await using (var scope = ctx.Host.Services.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await db.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE [Students] SET [CoachingStatus] = N'Ended' WHERE [Id] = {ended.Id}");
                }
                break;
            default: // tài khoản coach
                await using (var scope = ctx.Host.Services.CreateAsyncScope())
                {
                    var sp = scope.ServiceProvider;
                    var users = sp.GetRequiredService<UserManager<AppUser>>();
                    var user = new AppUser { UserName = email, Email = email, FullName = "Coach khác", EmailConfirmed = true };
                    Assert.True((await users.CreateAsync(user, "Coach@12345")).Succeeded);
                    Assert.True((await users.AddToRoleAsync(user, Roles.Admin)).Succeeded);
                    var db = sp.GetRequiredService<AppDbContext>();
                    db.Coaches.Add(Coach.Create(user.Id, "Coach khác", Email.Create(email)));
                    await db.SaveChangesAsync();
                }
                break;
        }
    }

    [When(@"^coach tạo hồ sơ học viên với email ""([^""]*)""$")]
    public Task CreateWithEmail(string email) => api.TryCreateAsync("Học viên mới", email);

    [Then(@"^hồ sơ được tạo với email ""([^""]*)""$")]
    public void CreatedWithEmail(string email)
    {
        Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);
        Assert.Equal(email, api.LastDetail!.Email);
    }

    [Then("hồ sơ không được tạo vì email đã được dùng")]
    public Task RejectedEmailTaken() => api.AssertErrorAsync(HttpStatusCode.Conflict, "student.email_taken");

    // @BR-f02-ho-so-hoc-vien-002
    [When(@"^coach tạo hồ sơ học viên tên ""([^""]*)"" mà không có email$")]
    public Task CreateWithoutEmail(string fullName) => api.TryCreateAsync(fullName, null);

    [When(@"^coach tạo hồ sơ học viên tên ""([^""]*)"" với email ""([^""]*)""$")]
    public Task CreateWithNameAndEmail(string fullName, string email) => api.TryCreateAsync(fullName, email);

    [Then("hồ sơ không được lưu vì thiếu email")]
    public Task RejectedMissingEmail() => api.AssertErrorAsync(HttpStatusCode.BadRequest, "email.required");

    [Then("hồ sơ không được lưu vì email không hợp lệ")]
    public Task RejectedInvalidEmail() => api.AssertErrorAsync(HttpStatusCode.BadRequest, "email.invalid");

    // @BR-f02-ho-so-hoc-vien-003
    [When(@"^coach tạo hồ sơ học viên với họ tên chỉ gồm khoảng trắng và email ""([^""]*)""$")]
    public Task CreateWithBlankName(string email) => api.TryCreateAsync("   ", email);

    [Then("hồ sơ không được lưu vì thiếu họ tên")]
    public Task RejectedMissingName() => api.AssertErrorAsync(HttpStatusCode.BadRequest, "student.full_name_required");

    // @BR-f02-ho-so-hoc-vien-005 @BR-f02-ho-so-hoc-vien-006 @BR-f02-ho-so-hoc-vien-007
    [When(@"^coach tạo hồ sơ học viên ""([^""]*)""$")]
    public Task CreateNamed(string fullName) => api.CreateOkAsync(fullName);

    // @BR-f02-ho-so-hoc-vien-005
    [Then(@"^tài khoản của ""([^""]*)"" ở trạng thái ""([^""]*)""$")]
    public async Task AccountStatusIs(string fullName, string label) =>
        Assert.Equal(StudentApi.AccountStatuses[label], (await api.GetAsync(fullName)).AccountStatus);

    // @BR-f02-ho-so-hoc-vien-006 (dùng lại ở BR-017→019, 022)
    [Then(@"^trạng thái coaching của ""([^""]*)"" là ""([^""]*)""$")]
    public async Task CoachingStatusIs(string fullName, string label) =>
        Assert.Equal(StudentApi.CoachingStatuses[label], (await api.GetAsync(fullName)).CoachingStatus);

    // @BR-f02-ho-so-hoc-vien-007
    [Then(@"^hồ sơ của ""([^""]*)"" thuộc coach ""([^""]*)""$")]
    public async Task BelongsToCoach(string fullName, string coachName)
    {
        var profile = await ctx.GetAsync("/api/admin/profile", api.AdminToken);
        var coach = (await profile.Content.ReadFromJsonAsync<CoachProfileDto>())!;
        Assert.Equal(coachName, coach.DisplayName);
        Assert.Equal(coach.Id, (await api.GetAsync(fullName)).CoachId);
    }
}
```

- [x] **Step 8: Viết integration test (seed bị loại + rollback + seed demo)**

`backend/tests/Coaching.Api.IntegrationTests/ApiFactory.cs` — thay toàn bộ:
```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Abstractions;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Coaching.TestSupport;

namespace Coaching.Api.IntegrationTests;

/// <summary>Chạy API thật trên SQL Server thật (container Testcontainers), mỗi lần chạy test một DB mới.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin@12345";

    private readonly DatabaseFixture _database = new();
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        _connectionString = await _database.NewDatabaseAsync();
        _ = Services; // khởi động host → seed admin
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-0123456789abcdef");
        builder.UseSetting("AdminSeed:Email", AdminEmail);
        builder.UseSetting("AdminSeed:Password", AdminPassword);
        builder.UseSetting("Email:Provider", "Log");
        builder.UseSetting("RateLimiting:AuthPermitPerMinute", "1000");
    }

    public async Task<AuthTokens> LoginAsync(string email, string password, ClientApp? app = null)
    {
        var resolvedApp = app ?? (email == AdminEmail ? ClientApp.Admin : ClientApp.Client);
        var response = await CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password, resolvedApp));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokens>())!;
    }

    /// <summary>HttpClient đã gắn token của admin seed.</summary>
    public async Task<HttpClient> AdminClientAsync()
    {
        var tokens = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    /// <summary>Tạo học viên đăng nhập được (luồng mời/kích hoạt làm ở F01): tài khoản có mật khẩu + hồ sơ Student.</summary>
    public async Task<(string Email, string Password)> CreateStudentAsync()
    {
        var email = $"student-{Guid.NewGuid():N}@test.local";
        const string password = "Student@123";

        await using var scope = Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var users = sp.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser { UserName = email, Email = email, FullName = "Học viên Test", EmailConfirmed = true };
        var created = await users.CreateAsync(user, password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        Assert.True((await users.AddToRoleAsync(user, Roles.Student)).Succeeded);

        // @ADR-f02-ho-so-hoc-vien-001: học viên luôn có hồ sơ Student (/api/client/me đọc từ Student)
        var db = sp.GetRequiredService<AppDbContext>();
        var adminEmail = Email.Create(AdminEmail);
        var coach = await db.Coaches.SingleAsync(c => c.Email == adminEmail);
        db.Students.Add(Student.Create(coach.Id, user.Id, user.FullName, Email.Create(email), sp.GetRequiredService<IClock>().UtcNow));
        await db.SaveChangesAsync();
        return (email, password);
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
```

`backend/tests/Coaching.Api.IntegrationTests/Students/StudentRequests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Application.Students;

namespace Coaching.Api.IntegrationTests.Students;

internal static class StudentRequests
{
    public const string Url = "/api/admin/students";

    public static string NewEmail() => $"s{Guid.NewGuid():N}@test.local";

    public static async Task<StudentDetailDto> CreateAsync(HttpClient admin, string fullName = "Học viên Test")
    {
        var response = await admin.PostAsJsonAsync(Url, new { fullName, email = NewEmail() });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
    }

    public static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
```

`backend/tests/Coaching.Api.IntegrationTests/Students/CreateStudentTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Students;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class CreateStudentTests(ApiFactory factory)
{
    // @BR-f02-ho-so-hoc-vien-003: chỉ có họ tên + email vẫn lưu được (scenario @web; kiểm thêm ở API)
    [Fact]
    public async Task Name_and_email_only_is_saved()
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PostAsJsonAsync(StudentRequests.Url, new { fullName = "Lan", email = StudentRequests.NewEmail() });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        Assert.Equal("Lan", dto.FullName);
        Assert.Equal($"/api/admin/students/{dto.Id}", response.Headers.Location!.AbsolutePath, ignoreCase: true);

        var get = await admin.GetAsync($"{StudentRequests.Url}/{dto.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    // @BR-f02-ho-so-hoc-vien-003 (seed loại khỏi living doc): họ tên 101 ký tự
    [Fact]
    public async Task Full_name_longer_than_100_is_rejected()
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PostAsJsonAsync(StudentRequests.Url,
            new { fullName = new string('a', 101), email = StudentRequests.NewEmail() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("student.full_name_too_long", await StudentRequests.ErrorCodeAsync(response));
    }

    // @BR-f02-ho-so-hoc-vien-005 @BR-f02-ho-so-hoc-vien-006 (seed loại khỏi living doc): trạng thái gửi kèm bị bỏ qua
    [Fact]
    public async Task Statuses_sent_in_body_are_ignored()
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PostAsJsonAsync(StudentRequests.Url, new
        {
            fullName = "Lan",
            email = StudentRequests.NewEmail(),
            accountStatus = "Activated",
            coachingStatus = "Ended",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        Assert.Equal("PendingActivation", dto.AccountStatus);
        Assert.Equal("Active", dto.CoachingStatus);
    }

    // @BR-f02-ho-so-hoc-vien-012 tiền đề: hồ sơ không tồn tại → 404
    [Fact]
    public async Task Unknown_student_returns_404()
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.GetAsync($"{StudentRequests.Url}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("student.not_found", await StudentRequests.ErrorCodeAsync(response));
    }

    // @ADR-f02-ho-so-hoc-vien-001: lưu hồ sơ lỗi sau khi đã tạo tài khoản → transaction rollback, không còn tài khoản mồ côi
    [Fact]
    public async Task Account_is_rolled_back_when_saving_student_fails()
    {
        var admin = await factory.AdminClientAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER [TR_Students_Fail] ON [Students] INSTEAD OF INSERT AS THROW 50000, 'boom', 1;");
        try
        {
            var email = StudentRequests.NewEmail();

            var response = await admin.PostAsJsonAsync(StudentRequests.Url, new { fullName = "Lỗi giữa chừng", email });

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.False(await db.Users.AnyAsync(u => u.Email == email));
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER [TR_Students_Fail];");
        }
    }
}
```

`backend/tests/Coaching.Api.IntegrationTests/Students/DemoSeedStudentTests.cs`:
```csharp
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class DemoSeedStudentTests(ApiFactory factory)
{
    private const string DemoEmail = "demo-backfill@test.local";

    private WebApplicationFactory<Program> WithDemoSeed() => factory.WithWebHostBuilder(b =>
    {
        b.UseSetting("DemoSeed:Enabled", "true");
        b.UseSetting("DemoSeed:Email", DemoEmail);
        b.UseSetting("DemoSeed:Password", "Demo@12345");
    });

    // @ADR-f02-ho-so-hoc-vien-001: user demo đã seed ở F00 (chưa có Student) được bổ sung Student; khởi động lại không tạo thêm
    [Fact]
    public async Task Demo_student_gets_a_profile_and_backfill_is_idempotent()
    {
        await using (var first = WithDemoSeed())
            _ = first.Services;

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = await db.Users.Where(u => u.Email == DemoEmail).Select(u => u.Id).SingleAsync();
        Assert.Equal(1, await db.Students.CountAsync(s => s.UserId == userId));

        // Giả lập dữ liệu F00: user demo chưa có Student
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Students] WHERE [UserId] = {userId}");
        await using (var second = WithDemoSeed())
            _ = second.Services;
        await using (var third = WithDemoSeed())
            _ = third.Services;

        Assert.Equal(1, await db.Students.CountAsync(s => s.UserId == userId));
    }
}
```

- [x] **Step 9: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet build Coaching.slnx`
Expected: FAIL — build error `The type or namespace name 'StudentDetailDto' could not be found` (và `AppDbContext.Students` chưa có).

- [x] **Step 10: Application — port, lỗi, DTO, use case**

`backend/src/Coaching.Application/Abstractions/IUnitOfWork.cs` — thay toàn bộ:
```csharp
using Coaching.Application.Common;

namespace Coaching.Application.Abstractions;

/// <summary>Lưu mọi thay đổi của một use case trong một transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// @ADR-f02-ho-so-hoc-vien-001: chạy <paramref name="operation"/> trong transaction tường minh (cho thao tác tự
    /// SaveChanges như UserManager). Commit khi kết quả thành công; rollback khi thất bại hoặc có exception.
    /// </summary>
    Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken = default);
}
```

`backend/src/Coaching.Application/Coaches/CurrentCoach.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Domain.Coaches;

namespace Coaching.Application.Coaches;

/// <summary>Coach của admin đang đăng nhập. Mọi use case hồ sơ học viên lọc theo coach này.</summary>
public sealed class CurrentCoach(ICoachRepository coaches, ICurrentUser currentUser)
{
    public async Task<Coach?> GetAsync(CancellationToken cancellationToken = default) =>
        currentUser.UserId is { } userId ? await coaches.GetByUserIdAsync(userId, cancellationToken) : null;
}
```

`backend/src/Coaching.Application/Students/IStudentAccounts.cs`:
```csharp
using Coaching.Application.Common;
using Coaching.Domain.Common;

namespace Coaching.Application.Students;

/// <summary>
/// @ADR-f02-ho-so-hoc-vien-001: tài khoản đăng nhập của học viên (Identity), tạo "Chờ kích hoạt": role Student,
/// không mật khẩu → chưa đăng nhập được. Email trùng với bất kỳ tài khoản nào (kể cả coach) → StudentErrors.EmailTaken.
/// </summary>
public interface IStudentAccounts
{
    Task<Result> CreatePendingAsync(Guid userId, Email email, string fullName, CancellationToken cancellationToken = default);
}
```

`backend/src/Coaching.Application/Students/StudentErrors.cs`:
```csharp
using Coaching.Application.Common;

namespace Coaching.Application.Students;

public static class StudentErrors
{
    public static readonly Error NotFound =
        new("student.not_found", "Không tìm thấy hồ sơ học viên.", ErrorKind.NotFound);

    public static readonly Error CoachNotFound =
        new("coach.not_found", "Tài khoản này chưa có hồ sơ coach.", ErrorKind.NotFound);

    // @BR-f02-ho-so-hoc-vien-001
    public static readonly Error EmailTaken =
        new("student.email_taken", "Email này đã được dùng cho một tài khoản khác.", ErrorKind.Conflict);
}
```

`backend/src/Coaching.Application/Students/StudentDtos.cs`:
```csharp
using Coaching.Domain.Coaches;
using Coaching.Domain.Students;

namespace Coaching.Application.Students;

/// <summary>Hồ sơ chi tiết cho coach (chỉ dùng ở /api/admin).</summary>
public sealed record StudentDetailDto(
    Guid Id,
    Guid CoachId,
    string FullName,
    string Email,
    string AccountStatus,
    string CoachingStatus,
    DateTimeOffset CreatedAt)
{
    /// <param name="coach">Coach sở hữu hồ sơ — dùng để hiển thị tên người sửa (Task 3, 5).</param>
    public static StudentDetailDto From(Student student, Coach coach) => new(
        student.Id,
        student.CoachId,
        student.FullName,
        student.Email.Value,
        student.AccountStatus.ToString(),
        student.CoachingStatus.ToString(),
        student.CreatedAt);
}
```

`backend/src/Coaching.Application/Students/Commands/CreateStudent.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

/// <summary>Không có trường trạng thái: field lạ trong body bị bỏ qua (@BR-f02-ho-so-hoc-vien-005, @BR-f02-ho-so-hoc-vien-006).</summary>
public sealed record CreateStudentCommand(string? FullName, string? Email);

public sealed class CreateStudentHandler(
    CurrentCoach currentCoach,
    IStudentRepository students,
    IStudentAccounts accounts,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(CreateStudentCommand command, CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;

        var email = Email.Create(command.Email); // @BR-f02-ho-so-hoc-vien-002 → 400 email.required / email.invalid
        // Kiểm hồ sơ TRƯỚC khi tạo tài khoản; Id tài khoản sinh sẵn để Student giữ UserId.
        var student = Student.Create(coach.Id, Guid.CreateVersion7(), command.FullName, email, clock.UtcNow);

        // @ADR-f02-ho-so-hoc-vien-001: tài khoản + hồ sơ cùng một transaction
        return await unitOfWork.ExecuteInTransactionAsync<StudentDetailDto>(async ct =>
        {
            var account = await accounts.CreatePendingAsync(student.UserId, email, student.FullName, ct);
            if (!account.IsSuccess)
                return Result<StudentDetailDto>.Failure(account.Error!); // @BR-f02-ho-so-hoc-vien-001 → 409

            students.Add(student);
            await unitOfWork.SaveChangesAsync(ct);
            return Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach));
        }, cancellationToken);
    }
}
```

`backend/src/Coaching.Application/Students/Queries/GetStudent.cs`:
```csharp
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Queries;

public sealed class GetStudentHandler(CurrentCoach currentCoach, IStudentRepository students)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;

        var student = await students.GetAsync(id, coach.Id, cancellationToken);
        return student is null
            ? StudentErrors.NotFound
            : Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach));
    }
}
```

`backend/src/Coaching.Application/DependencyInjection.cs` — thay toàn bộ:
```csharp
using Coaching.Application.Coaches;
using Coaching.Application.Students.Commands;
using Coaching.Application.Students.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký use case của tầng Application. Quy ước: mỗi use case là một handler
    /// (`XxxHandler.HandleAsync`) đăng ký Scoped, controller inject trực tiếp handler.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetMyCoachProfileHandler>();
        services.AddScoped<CurrentCoach>();
        services.AddScoped<CreateStudentHandler>();
        services.AddScoped<GetStudentHandler>();
        return services;
    }
}
```

- [x] **Step 11: Infrastructure — mapping, repository, tài khoản, transaction, seed demo**

`backend/src/Coaching.Infrastructure/Persistence/Configurations/StudentConfiguration.cs`:
```csharp
using Coaching.Domain.Coaches;
using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Persistence.Configurations;

internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.FullName).HasMaxLength(Student.FullNameMaxLength).IsRequired();
        builder.Property(s => s.Email)
            .HasConversion(e => e.Value, v => Email.Create(v))
            .HasMaxLength(Email.MaxLength)
            .IsRequired();
        // @BR-f02-ho-so-hoc-vien-001: chốt chặn cuối; kiểm trùng chính nằm ở Identity (gồm cả tài khoản coach)
        builder.HasIndex(s => s.Email).IsUnique();

        builder.Property(s => s.AccountStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.CoachingStatus).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.UserId).IsUnique();
        builder.HasIndex(s => new { s.CoachId, s.CreatedAt });
        builder.HasOne<Coach>().WithMany().HasForeignKey(s => s.CoachId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithOne().HasForeignKey<Student>(s => s.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(s => s.DomainEvents);
    }
}
```

`backend/src/Coaching.Infrastructure/Persistence/Repositories/StudentRepository.cs`:
```csharp
using Coaching.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Persistence.Repositories;

internal sealed class StudentRepository(AppDbContext db) : IStudentRepository
{
    public Task<Student?> GetAsync(Guid id, Guid coachId, CancellationToken cancellationToken = default) =>
        db.Students.SingleOrDefaultAsync(s => s.Id == id && s.CoachId == coachId, cancellationToken);

    public void Add(Student student) => db.Students.Add(student);
}
```

`backend/src/Coaching.Infrastructure/Identity/StudentAccounts.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Coaching.Infrastructure.Identity;

/// <summary>@ADR-f02-ho-so-hoc-vien-001: tạo tài khoản Identity "Chờ kích hoạt" (không mật khẩu, role Student).</summary>
internal sealed class StudentAccounts(UserManager<AppUser> users, IClock clock) : IStudentAccounts
{
    public async Task<Result> CreatePendingAsync(Guid userId, Email email, string fullName, CancellationToken cancellationToken = default)
    {
        var user = new AppUser
        {
            Id = userId,
            UserName = email.Value,
            Email = email.Value,
            FullName = fullName,
            EmailConfirmed = false,
            CreatedAt = clock.UtcNow,
        };

        var created = await users.CreateAsync(user);
        if (!created.Succeeded)
        {
            // @BR-f02-ho-so-hoc-vien-001: Identity so NormalizedEmail → khác hoa/thường vẫn là trùng; gồm cả tài khoản coach
            if (created.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
                return Result.Failure(StudentErrors.EmailTaken);
            throw new InvalidOperationException(
                $"Không tạo được tài khoản học viên: {string.Join("; ", created.Errors.Select(e => e.Description))}");
        }

        var role = await users.AddToRoleAsync(user, Roles.Student);
        if (!role.Succeeded)
            throw new InvalidOperationException(
                $"Không gán được role Student: {string.Join("; ", role.Errors.Select(e => e.Description))}");
        return Result.Success();
    }
}
```

`backend/src/Coaching.Infrastructure/Persistence/AppDbContext.cs` — thay toàn bộ:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Common;
using Coaching.Domain.Coaches;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Audit;
using Coaching.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    public DbSet<Coach> Coaches => Set<Coach>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    // @ADR-f02-ho-so-hoc-vien-001: execution strategy (retry) bọc cả transaction; commit chỉ khi kết quả thành công
    public Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken = default) =>
        Database.CreateExecutionStrategy().ExecuteAsync(async ct =>
        {
            await using var transaction = await Database.BeginTransactionAsync(ct);
            var result = await operation(ct);
            if (result.IsSuccess)
                await transaction.CommitAsync(ct);
            else
                await transaction.RollbackAsync(ct);
            return result;
        }, cancellationToken);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

`backend/src/Coaching.Infrastructure/DependencyInjection.cs` — thêm `using Coaching.Application.Students;` và `using Coaching.Domain.Students;` vào đầu file, rồi thêm sau dòng `services.AddScoped<ICoachRepository, CoachRepository>();`:
```csharp
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IStudentAccounts, StudentAccounts>();
```

`backend/src/Coaching.Infrastructure/Seeding/DatabaseInitializer.cs` — thêm `using Coaching.Domain.Students;` vào đầu file, rồi thay khối `// @BR-f00-codebase-020` (từ dòng `// @BR-f00-codebase-020` đến hết `if (demoSeed.Enabled) { … }`) bằng:
```csharp
        // @BR-f00-codebase-020
        if (demoSeed.Enabled)
        {
            ValidateSeedCredentials("DemoSeed", demoSeed.Email, demoSeed.Password);
            var demoEmail = DomainEmail.Create(demoSeed.Email);
            var demo = await EnsureUserAsync(userManager, clock, demoEmail.Value, demoSeed.Password!, demoSeed.FullName, Roles.Student, logger);

            // @ADR-f02-ho-so-hoc-vien-001: học viên demo (seed từ F00) cần hồ sơ Student — bổ sung nếu thiếu, idempotent
            if (!await db.Students.AnyAsync(s => s.UserId == demo.Id, cancellationToken))
            {
                var coach = await db.Coaches.SingleAsync(c => c.UserId == admin.Id, cancellationToken);
                db.Students.Add(Student.Create(coach.Id, demo.Id, demoSeed.FullName, demoEmail, clock.UtcNow));
                await db.SaveChangesAsync(cancellationToken);
            }
        }
```

- [x] **Step 12: API — `StudentsController`**

`backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs`:
```csharp
using Coaching.Api.Common;
using Coaching.Application.Students;
using Coaching.Application.Students.Commands;
using Coaching.Application.Students.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers.Admin;

/// <summary>Hồ sơ học viên của coach đang đăng nhập. Không có endpoint xóa.</summary>
public sealed class StudentsController : AdminControllerBase
{
    [HttpPost]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateStudentCommand command,
        [FromServices] CreateStudentHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : result.Error!.ToProblem(this);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, [FromServices] GetStudentHandler handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, cancellationToken)).ToActionResult(this);
}
```

- [x] **Step 13: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f02-ho-so-hoc-vien-001|Category=BR-f02-ho-so-hoc-vien-002|Category=BR-f02-ho-so-hoc-vien-003|Category=BR-f02-ho-so-hoc-vien-005|Category=BR-f02-ho-so-hoc-vien-006|Category=BR-f02-ho-so-hoc-vien-007)&Category!=web"`
Expected: `Passed: 10`, `Failed: 0` (BR-001: 1 + 3 ví dụ outline; BR-002: 2; BR-003: 1; BR-005/006/007: 1 mỗi rule).
Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Students"`
Expected: `Passed: 6`.
Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed (scenario F00 chạy với đồng hồ bắt đầu 01/11/2026 và helper `CreateStudentAsync` mới).

- [x] **Step 14: Commit**
```bash
git add database/changelog backend/src backend/tests/Coaching.Domain.Tests/Students backend/tests/Coaching.Acceptance backend/tests/Coaching.Api.IntegrationTests
git commit -F - <<'EOF'
feat(students): create student profile with pending account (BR-f02-001..003, 005..007)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---
### Task 2: Form đăng ký — `RegistrationForm` JSON, danh sách lựa chọn, kiểm dữ liệu, mục bắt buộc còn thiếu

**Files:**
- Create: `backend/src/Coaching.Domain/Students/RegistrationInput.cs`, `RegistrationOptions.cs`, `RegistrationFields.cs`, `RegistrationForm.cs`
- Modify: `backend/src/Coaching.Domain/Students/Student.cs`
- Modify: `backend/src/Coaching.Infrastructure/Persistence/Configurations/StudentConfiguration.cs`
- Modify: `backend/src/Coaching.Application/Students/Commands/CreateStudent.cs`, `backend/src/Coaching.Application/Students/StudentDtos.cs`
- Create: `backend/tests/Coaching.TestSupport/RegistrationSamples.cs`; Modify: `backend/tests/Coaching.TestSupport/Coaching.TestSupport.csproj`, `backend/tests/Coaching.Domain.Tests/Coaching.Domain.Tests.csproj`
- Test: `backend/tests/Coaching.Domain.Tests/Students/RegistrationFormTests.cs`
- Test: `backend/tests/Coaching.Acceptance/Support/StepText.cs`, `Support/RegistrationLabels.cs`, `Support/StudentApi.cs` (M), `Steps/StudentRegistrationSteps.cs`
- Test: `backend/tests/Coaching.Api.IntegrationTests/Students/RegistrationValidationTests.cs`

**Interfaces:**
- Consumes: Task 1 — `Student.Create(...)`, `StudentConfiguration`, `CreateStudentHandler`, `StudentDetailDto.From(Student, Coach)`, `StudentApi`, `StudentRequests`, `ApiFactory.AdminClientAsync()`.
- Produces:
  - Domain: `sealed record RegistrationInput` (44 thuộc tính nullable; tên JSON camelCase `pack, price, transferDate, address, age, heightCm, weightKg, chestCm, bellyCm, waistCm, hipCm, rightArmCm, leftArmCm, rightThighCm, leftThighCm, bodyFatPercent, bodyFatMethod, trainingYears, weeklyTrainingTime, weightBeforeLiftingKg, heightThenCm, trainingGoal, currentTraining, medicalHistory, availability, equipment, foodBudgetReadiness, occupation, activityLevel, stressLevel, sleepDuration, caffeine, supplements, currentDiet, mealAndTrainingTimes, sessionsPerWeek, programStyle, photoFrontUrl, photoBackUrl, photoLeftUrl, photoRightUrl, expectations, previousCoaching, investmentReason`); `RegistrationForm.Create(RegistrationInput, DateOnly today)`, `RegistrationForm.CreateEmpty()`, `RegistrationForm.MissingRequired() → IReadOnlyList<string>` (mã mục, theo thứ tự form), `RegistrationForm.ToInput()`; `RegistrationOptions.*`; `RegistrationFields.All`, `RegistrationFields.LabelOf(code)`; `Student.Registration`, `Student.MissingRequiredFields`; `Student.Create(Guid coachId, Guid userId, string? fullName, Email email, DateTimeOffset at, RegistrationForm? registration = null)`.
  - Lỗi 400: `student.<mã mục>.out_of_range | invalid_option | too_long | invalid_url`, `student.transferDate.in_future`.
  - Application: `CreateStudentCommand(string? FullName, string? Email, RegistrationInput? Registration)`; `StudentDetailDto` thêm `RegistrationInput Registration, IReadOnlyList<string> MissingRequired`.
  - Test: `RegistrationSamples.Complete()`; `StepText.Quoted/ParseDate/VietnamTime/VietnamDate`; `RegistrationLabels.Codes/With/AssertValue/ParseNumber`; `StudentApi.TryCreateAsync(string? fullName, string? email, RegistrationInput? registration = null)`, `StudentApi.CreateOkAsync(string fullName, string? email = null, RegistrationInput? registration = null)`; step `Given hôm nay là "dd/MM/yyyy" theo giờ Việt Nam` (dùng lại ở Task 4).

**Owns:** `@BR-f02-ho-so-hoc-vien-004`, `@BR-f02-ho-so-hoc-vien-008`, `@BR-f02-ho-so-hoc-vien-009`, `@BR-f02-ho-so-hoc-vien-010`, `@BR-f02-ho-so-hoc-vien-011` | `@ADR-f02-ho-so-hoc-vien-002`, `@ADR-f02-ho-so-hoc-vien-006`, `@ADR-f02-ho-so-hoc-vien-009`

- [x] **Step 1: Mẫu form đủ mục dùng chung cho test**

`backend/tests/Coaching.TestSupport/Coaching.TestSupport.csproj` — thêm trước `</Project>`:
```xml
  <ItemGroup>
    <ProjectReference Include="..\..\src\Coaching.Domain\Coaching.Domain.csproj" />
  </ItemGroup>
```

`backend/tests/Coaching.Domain.Tests/Coaching.Domain.Tests.csproj` — thêm vào `<ItemGroup>` chứa `ProjectReference`:
```xml
    <ProjectReference Include="..\Coaching.TestSupport\Coaching.TestSupport.csproj" />
```

`backend/tests/Coaching.TestSupport/RegistrationSamples.cs`:
```csharp
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
```

- [x] **Step 2: Viết Domain test cho form**

`backend/tests/Coaching.Domain.Tests/Students/RegistrationFormTests.cs`:
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

    // @BR-f02-ho-so-hoc-vien-004
    [Fact]
    public void Missing_required_lists_exactly_the_empty_required_fields_in_form_order()
    {
        var input = RegistrationSamples.Complete() with
        {
            Pack = null, TrainingGoal = "", PhotoBackUrl = null, Availability = null, // Lịch rảnh không bắt buộc
        };

        Assert.Equal(new[] { "pack", "trainingGoal", "photoBackUrl" }, Create(input).MissingRequired());
    }

    // @BR-f02-ho-so-hoc-vien-004 (seed loại khỏi living doc): mục chỉ chứa khoảng trắng tính là thiếu
    [Fact]
    public void Whitespace_only_required_text_counts_as_missing()
    {
        var form = Create(RegistrationSamples.Complete() with { TrainingGoal = "   " });

        Assert.Null(form.TrainingGoal);
        Assert.Equal(new[] { "trainingGoal" }, form.MissingRequired());
    }

    // @BR-f02-ho-so-hoc-vien-004
    [Fact]
    public void Empty_form_misses_all_28_required_fields() =>
        Assert.Equal(28, RegistrationForm.CreateEmpty().MissingRequired().Count);

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
    [InlineData(3, true)]
    [InlineData(70, true)]
    [InlineData(2.9, false)]
    [InlineData(70.1, false)]
    public void Body_fat_must_be_between_3_and_70(double percent, bool valid)
    {
        var input = new RegistrationInput { BodyFatPercent = (decimal)percent };
        if (valid)
            Assert.Equal((decimal)percent, Create(input).BodyFatPercent);
        else
            Assert.Equal("student.bodyFatPercent.out_of_range", ErrorOf(input));
    }

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
    public void Girths_and_pre_lifting_measurements_use_their_ranges()
    {
        var form = Create(new RegistrationInput { ChestCm = 10m, LeftThighCm = 250m, WeightBeforeLiftingKg = 20m, HeightThenCm = 250m });
        Assert.Equal(10m, form.ChestCm);
        Assert.Equal(250m, form.LeftThighCm);

        Assert.Equal("student.waistCm.out_of_range", ErrorOf(new RegistrationInput { WaistCm = 9.9m }));
        Assert.Equal("student.hipCm.out_of_range", ErrorOf(new RegistrationInput { HipCm = 250.1m }));
        Assert.Equal("student.weightBeforeLiftingKg.out_of_range", ErrorOf(new RegistrationInput { WeightBeforeLiftingKg = 19m }));
        Assert.Equal("student.heightThenCm.out_of_range", ErrorOf(new RegistrationInput { HeightThenCm = 251m }));
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
        Assert.Equal(url, Create(new RegistrationInput { PhotoFrontUrl = url }).PhotoFrontUrl);

    // @BR-f02-ho-so-hoc-vien-011 (seed "javascript:" loại khỏi living doc)
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("drive.google.com/abc")]
    [InlineData("ftp://example.com/anh.jpg")]
    public void Non_web_photo_links_are_rejected(string url) =>
        Assert.Equal("student.photoFrontUrl.invalid_url", ErrorOf(new RegistrationInput { PhotoFrontUrl = url }));

    // @ADR-f02-ho-so-hoc-vien-009
    [Fact]
    public void Text_length_limits_apply()
    {
        Assert.Equal(200, Create(new RegistrationInput { Address = new string('a', 200) }).Address!.Length);
        Assert.Equal("student.address.too_long", ErrorOf(new RegistrationInput { Address = new string('a', 201) }));
        Assert.Equal(2000, Create(new RegistrationInput { TrainingGoal = new string('a', 2000) }).TrainingGoal!.Length);
        Assert.Equal("student.trainingGoal.too_long", ErrorOf(new RegistrationInput { TrainingGoal = new string('a', 2001) }));
        Assert.Equal("student.photoFrontUrl.too_long",
            ErrorOf(new RegistrationInput { PhotoFrontUrl = "https://x.vn/" + new string('a', 2048) }));
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

        Assert.Equal(28, Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Lan", email, at).MissingRequiredFields.Count);
        Assert.Empty(Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Lan", email, at,
            Create(RegistrationSamples.Complete())).MissingRequiredFields);
    }
}
```

- [x] **Step 3: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~RegistrationFormTests"`
Expected: FAIL — build error `The type or namespace name 'RegistrationInput' could not be found`.

- [x] **Step 4: Domain — input, lựa chọn, danh mục mục form, `RegistrationForm`**

`backend/src/Coaching.Domain/Students/RegistrationInput.cs`:
```csharp
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
```

`backend/src/Coaching.Domain/Students/RegistrationOptions.cs`:
```csharp
namespace Coaching.Domain.Students;

/// <summary>
/// @BR-f02-ho-so-hoc-vien-009 @ADR-f02-ho-so-hoc-vien-006: danh sách giá trị cho trước, đúng theo form prototype.
/// FE có bản sao chỉ để hiển thị.
/// </summary>
public static class RegistrationOptions
{
    public static readonly IReadOnlyList<string> Packs = ["Pack 1", "Pack 2", "Pack 3", "Khác"];

    public static readonly IReadOnlyList<string> BodyFatMethods =
        ["Ước lượng bằng mắt", "Cân điện tử", "InBody", "Thước kẹp mỡ", "DEXA", "Khác"];

    public static readonly IReadOnlyList<string> TrainingYears =
        ["Chưa tập", "Dưới 6 tháng", "6–12 tháng", "1–2 năm", "2–3 năm", "3–5 năm", "Trên 5 năm"];

    public static readonly IReadOnlyList<string> FoodBudgetReadiness = ["Sẵn sàng", "Đang cân nhắc", "Chưa sẵn sàng"];

    public static readonly IReadOnlyList<string> ActivityLevels =
        ["Ít vận động (ngồi nhiều)", "Nhẹ", "Vừa", "Nhiều", "Rất nhiều (lao động chân tay)"];

    public static readonly IReadOnlyList<string> StressLevels = ["Thấp", "Vừa", "Cao", "Rất cao"];

    public static readonly IReadOnlyList<string> SleepDurations =
        ["Dưới 5 tiếng", "5–6 tiếng", "6–7 tiếng", "7–8 tiếng", "Trên 8 tiếng"];

    public static readonly IReadOnlyList<string> SessionsPerWeek = ["2 buổi", "3 buổi", "4 buổi", "5 buổi", "6 buổi", "7 buổi"];

    public static readonly IReadOnlyList<string> ProgramStyles =
        ["Push / Pull / Legs", "Upper / Lower", "Full body", "Mỗi buổi một nhóm cơ", "Để coach chọn", "Khác"];
}
```

`backend/src/Coaching.Domain/Students/RegistrationFields.cs`:
```csharp
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
```

`backend/src/Coaching.Domain/Students/RegistrationForm.cs`:
```csharp
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
```

`backend/src/Coaching.Domain/Students/Student.cs` — 3 thay đổi:
1. Thêm sau thuộc tính `CreatedAt`:
```csharp
    // @ADR-f02-ho-so-hoc-vien-002
    public RegistrationForm Registration { get; private set; } = default!;

    /// <summary>@BR-f02-ho-so-hoc-vien-004: hồ sơ luôn cho biết mục bắt buộc còn thiếu.</summary>
    public IReadOnlyList<string> MissingRequiredFields => Registration.MissingRequired();
```
2. Đổi chữ ký `Create` thành:
```csharp
    public static Student Create(Guid coachId, Guid userId, string? fullName, Email email, DateTimeOffset at,
        RegistrationForm? registration = null)
```
3. Thêm vào object initializer của `Create`, sau dòng `CreatedAt = at,`:
```csharp
            Registration = registration ?? RegistrationForm.CreateEmpty(),
```

- [x] **Step 5: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students"`
Expected: không Failed (`RegistrationFormTests` + `StudentTests`).

- [x] **Step 6: Hạ tầng acceptance — ngày giờ VN, nhãn mục form, tạo hồ sơ kèm form**

`backend/tests/Coaching.Acceptance/Support/StepText.cs`:
```csharp
using System.Globalization;
using System.Text.RegularExpressions;

namespace Coaching.Acceptance.Support;

public static class StepText
{
    public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    /// <summary>Các chuỗi trong ngoặc kép theo thứ tự: <c>"a", "b" và "c"</c> → [a, b, c].</summary>
    public static IReadOnlyList<string> Quoted(string text) =>
        Regex.Matches(text, "\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();

    public static DateOnly ParseDate(string ddMMyyyy) =>
        DateOnly.ParseExact(ddMMyyyy, "dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static DateTimeOffset VietnamTime(string ddMMyyyy, int hour, int minute)
    {
        var date = ParseDate(ddMMyyyy);
        return new DateTimeOffset(date.Year, date.Month, date.Day, hour, minute, 0, VietnamOffset);
    }

    public static DateOnly VietnamDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(instant.ToOffset(VietnamOffset).DateTime);
}
```

`backend/tests/Coaching.Acceptance/Support/RegistrationLabels.cs`:
```csharp
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Coaching.Domain.Students;

namespace Coaching.Acceptance.Support;

/// <summary>Tên mục trong living doc → mã mục JSON của `registration`.</summary>
public static class RegistrationLabels
{
    public static readonly IReadOnlyDictionary<string, string> Codes = new Dictionary<string, string>
    {
        ["Gói đã mua"] = "pack",
        ["Giá"] = "price",
        ["Tuổi"] = "age",
        ["Chiều cao"] = "heightCm",
        ["Cân nặng"] = "weightKg",
        ["Mỡ cơ thể"] = "bodyFatPercent",
        ["Mục tiêu tập luyện"] = "trainingGoal",
        ["Số buổi tập mỗi tuần"] = "sessionsPerWeek",
        ["Ảnh mặt trước"] = "photoFrontUrl",
        ["Ảnh mặt sau"] = "photoBackUrl",
    };

    private static readonly HashSet<string> NumericCodes = ["price", "age", "heightCm", "weightKg", "bodyFatPercent"];

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

`backend/tests/Coaching.Acceptance/Support/StudentApi.cs` — thêm `using Coaching.Domain.Students;` vào đầu file, rồi thay hai method `TryCreateAsync` và `CreateOkAsync` bằng:
```csharp
    public async Task<HttpResponseMessage> TryCreateAsync(string? fullName, string? email, RegistrationInput? registration = null)
    {
        var response = await ctx.SendJsonAsync(HttpMethod.Post, "/api/admin/students",
            new { fullName, email, registration }, AdminToken);
        await RememberCreatedAsync(response, fullName);
        return response;
    }

    public async Task<StudentDetailDto> CreateOkAsync(string fullName, string? email = null, RegistrationInput? registration = null)
    {
        var response = await TryCreateAsync(fullName, email ?? NewEmail(), registration);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return LastDetail!;
    }
```

- [x] **Step 7: Viết step defs `@api` cho BR-004, 008, 009, 010, 011**

`backend/tests/Coaching.Acceptance/Steps/StudentRegistrationSteps.cs`:
```csharp
using System.Net;
using Coaching.Acceptance.Support;
using Coaching.Domain.Students;
using Coaching.TestSupport;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class StudentRegistrationSteps(AcceptanceContext ctx, StudentApi api)
{
    // @BR-f02-ho-so-hoc-vien-004
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

    // @BR-f02-ho-so-hoc-vien-008 @BR-f02-ho-so-hoc-vien-009
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

    // @BR-f02-ho-so-hoc-vien-011
    [When(@"^coach lưu hồ sơ học viên có ảnh mặt trước ""([^""]*)""$")]
    public Task SavePhoto(string url) =>
        api.TryCreateAsync("Học viên ảnh", StudentApi.NewEmail(), new RegistrationInput { PhotoFrontUrl = url });

    [Then(@"^hồ sơ ghi nhận ảnh mặt trước ""([^""]*)""$")]
    public void PhotoRecorded(string url)
    {
        Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);
        Assert.Equal(url, api.LastDetail!.Registration.PhotoFrontUrl);
    }

    [Then("hồ sơ không được lưu vì link ảnh không hợp lệ")]
    public Task PhotoInvalid() => api.AssertErrorAsync(HttpStatusCode.BadRequest, "student.photoFrontUrl.invalid_url");
}
```

- [x] **Step 8: Viết integration test (seed bị loại + JSON round-trip)**

`backend/tests/Coaching.Api.IntegrationTests/Students/RegistrationValidationTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Coaching.Application.Students;
using Coaching.TestSupport;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class RegistrationValidationTests(ApiFactory factory)
{
    // @BR-f02-ho-so-hoc-vien-008 (seed loại khỏi living doc): cân nặng "abc"
    [Fact]
    public async Task Non_numeric_weight_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var body = $$"""{"fullName":"Lan","email":"{{StudentRequests.NewEmail()}}","registration":{"weightKg":"abc"}}""";

        var response = await admin.PostAsync(StudentRequests.Url, new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // @BR-f02-ho-so-hoc-vien-009 (seed loại khỏi living doc): giá trị ngoài danh sách
    [Theory]
    [InlineData("sessionsPerWeek", "8 buổi")]
    [InlineData("stressLevel", "Stress-free")]
    public async Task Option_outside_list_is_rejected(string code, string value)
    {
        var admin = await factory.AdminClientAsync();
        var registration = new Dictionary<string, object?> { [code] = value };

        var response = await admin.PostAsJsonAsync(StudentRequests.Url,
            new { fullName = "Lan", email = StudentRequests.NewEmail(), registration });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal($"student.{code}.invalid_option", await StudentRequests.ErrorCodeAsync(response));
    }

    // @ADR-f02-ho-so-hoc-vien-002 @BR-f02-ho-so-hoc-vien-004: form đủ mục lưu vào cột JSON và đọc lại nguyên vẹn
    [Fact]
    public async Task Complete_registration_round_trips_through_json_column()
    {
        var admin = await factory.AdminClientAsync();
        var input = RegistrationSamples.Complete();

        var created = await admin.PostAsJsonAsync(StudentRequests.Url,
            new { fullName = "Lan", email = StudentRequests.NewEmail(), registration = input });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<StudentDetailDto>())!.Id;

        var detail = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{id}"))!;
        Assert.Equal(input, detail.Registration);
        Assert.Empty(detail.MissingRequired);
    }
}
```

- [x] **Step 9: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet build Coaching.slnx`
Expected: FAIL — build error `'StudentDetailDto' does not contain a definition for 'Registration'`.

- [x] **Step 10: Lưu form vào hồ sơ — mapping JSON, use case, DTO**

`backend/src/Coaching.Infrastructure/Persistence/Configurations/StudentConfiguration.cs` — thêm trước dòng `builder.Ignore(s => s.DomainEvents);`:
```csharp
        // @ADR-f02-ho-so-hoc-vien-002: toàn bộ mục form trong 1 cột JSON
        builder.OwnsOne(s => s.Registration, r => r.ToJson("Registration"));
        builder.Navigation(s => s.Registration).IsRequired();
        builder.Ignore(s => s.MissingRequiredFields);
```

`backend/src/Coaching.Application/Students/Commands/CreateStudent.cs` — thay toàn bộ:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

/// <summary>Không có trường trạng thái: field lạ trong body bị bỏ qua (@BR-f02-ho-so-hoc-vien-005, @BR-f02-ho-so-hoc-vien-006).</summary>
public sealed record CreateStudentCommand(string? FullName, string? Email, RegistrationInput? Registration);

public sealed class CreateStudentHandler(
    CurrentCoach currentCoach,
    IStudentRepository students,
    IStudentAccounts accounts,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(CreateStudentCommand command, CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;

        var email = Email.Create(command.Email); // @BR-f02-ho-so-hoc-vien-002 → 400 email.required / email.invalid
        // @ADR-f02-ho-so-hoc-vien-006: luật form ở Domain; @BR-f02-ho-so-hoc-vien-010: "hôm nay" theo giờ VN
        var registration = RegistrationForm.Create(command.Registration ?? new RegistrationInput(), clock.Today);
        // Kiểm hồ sơ TRƯỚC khi tạo tài khoản; Id tài khoản sinh sẵn để Student giữ UserId.
        var student = Student.Create(coach.Id, Guid.CreateVersion7(), command.FullName, email, clock.UtcNow, registration);

        // @ADR-f02-ho-so-hoc-vien-001: tài khoản + hồ sơ cùng một transaction
        return await unitOfWork.ExecuteInTransactionAsync<StudentDetailDto>(async ct =>
        {
            var account = await accounts.CreatePendingAsync(student.UserId, email, student.FullName, ct);
            if (!account.IsSuccess)
                return Result<StudentDetailDto>.Failure(account.Error!); // @BR-f02-ho-so-hoc-vien-001 → 409

            students.Add(student);
            await unitOfWork.SaveChangesAsync(ct);
            return Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach));
        }, cancellationToken);
    }
}
```

`backend/src/Coaching.Application/Students/StudentDtos.cs` — thay toàn bộ:
```csharp
using Coaching.Domain.Coaches;
using Coaching.Domain.Students;

namespace Coaching.Application.Students;

/// <summary>Hồ sơ chi tiết cho coach (chỉ dùng ở /api/admin).</summary>
public sealed record StudentDetailDto(
    Guid Id,
    Guid CoachId,
    string FullName,
    string Email,
    string AccountStatus,
    string CoachingStatus,
    DateTimeOffset CreatedAt,
    RegistrationInput Registration,
    IReadOnlyList<string> MissingRequired)
{
    /// <param name="coach">Coach sở hữu hồ sơ — dùng để hiển thị tên người sửa (Task 3, 5).</param>
    public static StudentDetailDto From(Student student, Coach coach) => new(
        student.Id,
        student.CoachId,
        student.FullName,
        student.Email.Value,
        student.AccountStatus.ToString(),
        student.CoachingStatus.ToString(),
        student.CreatedAt,
        student.Registration.ToInput(),
        student.MissingRequiredFields); // @BR-f02-ho-so-hoc-vien-004
}
```

- [x] **Step 11: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f02-ho-so-hoc-vien-004|Category=BR-f02-ho-so-hoc-vien-008|Category=BR-f02-ho-so-hoc-vien-009|Category=BR-f02-ho-so-hoc-vien-010|Category=BR-f02-ho-so-hoc-vien-011)&Category!=web"`
Expected: `Passed: 17`, `Failed: 0`.
Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Students"`
Expected: `Passed: 10`.
Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed.

- [x] **Step 12: Commit**
```bash
git add backend/src backend/tests
git commit -F - <<'EOF'
feat(students): registration form as JSON value object with validation (BR-f02-004, 008..011)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 3: Sửa hồ sơ — người sửa, thời điểm sửa, chặn ghi đè bằng phiên bản

**Files:**
- Modify: `backend/src/Coaching.Domain/Students/Student.cs`, `backend/src/Coaching.Domain/Students/IStudentRepository.cs`
- Modify: `backend/src/Coaching.Infrastructure/Persistence/Repositories/StudentRepository.cs`, `Persistence/Configurations/StudentConfiguration.cs`, `Persistence/AppDbContext.cs`
- Create: `backend/src/Coaching.Application/Common/ConcurrencyConflictException.cs`, `backend/src/Coaching.Application/Students/StudentVersion.cs`, `backend/src/Coaching.Application/Students/Commands/UpdateStudent.cs`
- Modify: `backend/src/Coaching.Application/Students/StudentErrors.cs`, `StudentDtos.cs`, `backend/src/Coaching.Application/DependencyInjection.cs`
- Modify: `backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs`
- Test: `backend/tests/Coaching.Domain.Tests/Students/StudentUpdateTests.cs`
- Test: `backend/tests/Coaching.Acceptance/Support/StudentApi.cs` (M), `backend/tests/Coaching.Acceptance/Steps/StudentEditSteps.cs`
- Test: `backend/tests/Coaching.Api.IntegrationTests/Students/UpdateStudentTests.cs`

**Interfaces:**
- Consumes: Task 1 — `CurrentCoach`, `IUnitOfWork`, `StudentErrors`, `StudentsController`, `StudentApi`, `StudentRequests`; Task 2 — `RegistrationForm.Create(RegistrationInput, DateOnly)`, `RegistrationInput`, `StepText.VietnamTime`, `RegistrationSamples`.
- Produces:
  - Domain: `Student.UpdatedAt (DateTimeOffset?)`, `Student.UpdatedByUserId (Guid?)`, `Student.Version (byte[])`, `Student.UpdateRegistration(string? fullName, RegistrationForm registration, Guid actorUserId, DateTimeOffset at)`; `IStudentRepository.GetForUpdateAsync(Guid id, Guid coachId, byte[] expectedVersion, CancellationToken ct = default)`.
  - Application: `ConcurrencyConflictException` (ném từ `IUnitOfWork.SaveChangesAsync` khi lệch `Version`); `StudentVersion.Parse(string?) → byte[]?` (internal); `StudentErrors.VersionRequired` (400 `student.version_required`), `StudentErrors.RegistrationRequired` (400 `student.registration_required`), `StudentErrors.VersionConflict` (409 `student.version_conflict`); `StudentDetailDto` thêm `DateTimeOffset? UpdatedAt, string? UpdatedBy, string Version`; `StudentDetailDto.ActorName(Guid userId, Coach coach)`; `UpdateStudentCommand(string? FullName, RegistrationInput? Registration, string? Version)`, `UpdateStudentHandler.HandleAsync(Guid id, UpdateStudentCommand, ct)`.
  - API: `PUT /api/admin/students/{id}` → 200 `StudentDetailDto` / 400 / 404 / 409.
  - Test: `StudentApi.TryUpdateAsync(string fullName, Func<RegistrationInput, RegistrationInput> change, string? version = null)`, `StudentApi.UpdateOkAsync(string fullName, Func<RegistrationInput, RegistrationInput> change)`; step `Given hồ sơ của "X" đã có`, `Given bây giờ là HH:mm ngày "dd/MM/yyyy" giờ Việt Nam`, `When coach sửa mục tiêu tập luyện của "X"` (dùng lại ở Task 4, 5).

**Owns:** `@BR-f02-ho-so-hoc-vien-012`, `@BR-f02-ho-so-hoc-vien-013`, `@BR-f02-ho-so-hoc-vien-015` | `@ADR-f02-ho-so-hoc-vien-004`

- [x] **Step 1: Viết Domain test cho sửa hồ sơ**

`backend/tests/Coaching.Domain.Tests/Students/StudentUpdateTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class StudentUpdateTests
{
    private static readonly DateOnly Today = new(2026, 11, 23);
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 20, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EditedAt = new(2026, 11, 23, 10, 0, 0, TimeSpan.FromHours(7));
    private static readonly Guid CoachUserId = Guid.NewGuid();

    private static Student NewStudent() => Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Lan",
        Email.Create("lan@gmail.com"), CreatedAt, RegistrationForm.Create(new RegistrationInput { WeightKg = 72m }, Today));

    // @BR-f02-ho-so-hoc-vien-012: sửa được mọi mục kể cả số đo ban đầu; email không nằm trong lệnh sửa
    [Fact]
    public void UpdateRegistration_replaces_name_and_measurements_but_keeps_email()
    {
        var student = NewStudent();

        student.UpdateRegistration("Nguyễn Lan", RegistrationForm.Create(new RegistrationInput { WeightKg = 70m }, Today),
            CoachUserId, EditedAt);

        Assert.Equal("Nguyễn Lan", student.FullName);
        Assert.Equal(70m, student.Registration.WeightKg);
        Assert.Equal("lan@gmail.com", student.Email.Value);
    }

    // @BR-f02-ho-so-hoc-vien-013
    [Fact]
    public void UpdateRegistration_records_last_editor_and_time()
    {
        var student = NewStudent();
        Assert.Null(student.UpdatedAt);
        Assert.Null(student.UpdatedByUserId);

        student.UpdateRegistration("Lan", RegistrationForm.CreateEmpty(), CoachUserId, EditedAt);

        Assert.Equal(EditedAt, student.UpdatedAt);
        Assert.Equal(CoachUserId, student.UpdatedByUserId);
    }

    // @BR-f02-ho-so-hoc-vien-003: tên rỗng khi sửa vẫn bị từ chối, hồ sơ giữ nguyên
    [Fact]
    public void UpdateRegistration_rejects_blank_name_and_changes_nothing()
    {
        var student = NewStudent();

        var ex = Assert.Throws<DomainException>(() =>
            student.UpdateRegistration("  ", RegistrationForm.CreateEmpty(), CoachUserId, EditedAt));

        Assert.Equal("student.full_name_required", ex.Code);
        Assert.Equal("Lan", student.FullName);
        Assert.Equal(72m, student.Registration.WeightKg);
        Assert.Null(student.UpdatedAt);
    }
}
```

- [x] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~StudentUpdateTests"`
Expected: FAIL — build error `'Student' does not contain a definition for 'UpdateRegistration'`.

- [x] **Step 3: Domain — sửa hồ sơ, phiên bản**

`backend/src/Coaching.Domain/Students/Student.cs` — thêm sau thuộc tính `MissingRequiredFields`:
```csharp
    // @BR-f02-ho-so-hoc-vien-013: chỉ ghi lần sửa gần nhất, không lưu giá trị cũ
    public DateTimeOffset? UpdatedAt { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    /// <summary>@ADR-f02-ho-so-hoc-vien-004: rowversion do SQL Server sinh, dùng chặn ghi đè.</summary>
    public byte[] Version { get; private set; } = [];
```
và thêm method sau `Create` (trước `NormalizeFullName`):
```csharp
    /// <summary>
    /// @BR-f02-ho-so-hoc-vien-012: sửa họ tên và mọi mục form (kể cả số đo, ảnh ban đầu); email không sửa ở đây.
    /// @BR-f02-ho-so-hoc-vien-013: ghi người sửa và thời điểm sửa gần nhất.
    /// </summary>
    public void UpdateRegistration(string? fullName, RegistrationForm registration, Guid actorUserId, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(registration);
        var name = NormalizeFullName(fullName); // kiểm trước khi đổi gì
        FullName = name;
        Registration = registration;
        UpdatedAt = at;
        UpdatedByUserId = actorUserId;
    }
```

`backend/src/Coaching.Domain/Students/IStudentRepository.cs` — thêm sau `GetAsync`:
```csharp
    /// <summary>
    /// @ADR-f02-ho-so-hoc-vien-004: hồ sơ để sửa; khi lưu, hệ thống so Version trong DB với
    /// <paramref name="expectedVersion"/> và báo xung đột nếu khác.
    /// </summary>
    Task<Student?> GetForUpdateAsync(Guid id, Guid coachId, byte[] expectedVersion, CancellationToken cancellationToken = default);
```

- [x] **Step 4: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~StudentUpdateTests"`
Expected: `Passed: 3` (Domain.Tests chỉ build Domain + TestSupport; `StudentRepository` được bổ sung ở Step 8).

- [x] **Step 5: Viết step defs `@api` cho BR-012, 013, 015**

`backend/tests/Coaching.Acceptance/Support/StudentApi.cs` — thêm 2 method sau `GetAsync`:
```csharp
    /// <summary>PUT hồ sơ: lấy bản hiện tại, áp <paramref name="change"/> lên form, gửi kèm version (mặc định: bản vừa đọc).</summary>
    public async Task<HttpResponseMessage> TryUpdateAsync(string fullName, Func<RegistrationInput, RegistrationInput> change,
        string? version = null)
    {
        var current = await GetAsync(fullName);
        var body = new { fullName = current.FullName, registration = change(current.Registration), version = version ?? current.Version };
        var response = await ctx.SendJsonAsync(HttpMethod.Put, $"/api/admin/students/{Ids[fullName]}", body, AdminToken);
        if (response.IsSuccessStatusCode)
            LastDetail = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        return response;
    }

    public async Task<StudentDetailDto> UpdateOkAsync(string fullName, Func<RegistrationInput, RegistrationInput> change)
    {
        var response = await TryUpdateAsync(fullName, change);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return LastDetail!;
    }
```

`backend/tests/Coaching.Acceptance/Steps/StudentEditSteps.cs`:
```csharp
using System.Net;
using Coaching.Acceptance.Support;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class StudentEditSteps(AcceptanceContext ctx, StudentApi api)
{
    private string? _fullName;
    private string? _openedVersion;

    // @BR-f02-ho-so-hoc-vien-012
    [Given(@"^hồ sơ của ""([^""]*)"" có cân nặng (\d+) kg$")]
    public async Task HasWeight(string fullName, int kg)
    {
        await api.EnsureAsync(fullName);
        await api.UpdateOkAsync(fullName, r => r with { WeightKg = kg });
    }

    [When(@"^coach sửa cân nặng của ""([^""]*)"" thành (\d+) kg$")]
    public Task EditWeight(string fullName, int kg) => api.UpdateOkAsync(fullName, r => r with { WeightKg = kg });

    [Then(@"^hồ sơ của ""([^""]*)"" ghi nhận cân nặng (\d+) kg$")]
    public async Task WeightIs(string fullName, int kg) =>
        Assert.Equal(kg, (await api.GetAsync(fullName)).Registration.WeightKg);

    // @BR-f02-ho-so-hoc-vien-013 (dùng lại ở BR-014)
    [Given(@"^hồ sơ của ""([^""]*)"" đã có$")]
    public Task Exists(string fullName) => api.EnsureAsync(fullName);

    [Given(@"^bây giờ là (\d\d):(\d\d) ngày ""([^""]*)"" giờ Việt Nam$")]
    public Task NowIs(int hour, int minute, string date) => api.MoveClockToAsync(StepText.VietnamTime(date, hour, minute));

    [When(@"^coach sửa mục tiêu tập luyện của ""([^""]*)""$")]
    public Task EditGoal(string fullName) =>
        api.UpdateOkAsync(fullName, r => r with { TrainingGoal = $"Mục tiêu {Guid.NewGuid():N}" });

    [Then(@"^hồ sơ của ""([^""]*)"" cho biết sửa lần cuối lúc (\d\d):(\d\d) ngày ""([^""]*)"" bởi ""([^""]*)""$")]
    public async Task LastEdited(string fullName, int hour, int minute, string date, string editor)
    {
        var detail = await api.GetAsync(fullName);
        Assert.Equal(StepText.VietnamTime(date, hour, minute), detail.UpdatedAt);
        Assert.Equal(editor, detail.UpdatedBy);
    }

    // @BR-f02-ho-so-hoc-vien-015
    [Given(@"^coach đang xem phiên bản mới nhất của hồ sơ ""([^""]*)""$")]
    public async Task ViewingLatest(string fullName)
    {
        await api.EnsureAsync(fullName);
        _openedVersion = (await api.GetAsync(fullName)).Version;
    }

    [When(@"^coach lưu thay đổi mục tiêu tập luyện của ""([^""]*)""$")]
    public Task SaveGoal(string fullName) =>
        api.TryUpdateAsync(fullName, r => r with { TrainingGoal = "Tăng cơ, giảm mỡ" }, _openedVersion);

    [Then("thay đổi được lưu")]
    public void ChangeSaved() => Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);

    [Given(@"^coach mở hồ sơ ""([^""]*)"" ở hai nơi cùng một phiên bản$")]
    public async Task OpenedTwice(string fullName)
    {
        await api.EnsureAsync(fullName);
        _fullName = fullName;
        _openedVersion = (await api.GetAsync(fullName)).Version;
    }

    [Given("thay đổi ở nơi thứ nhất đã được lưu")]
    public async Task FirstPlaceSaved()
    {
        var response = await api.TryUpdateAsync(_fullName!, r => r with { TrainingGoal = "Nơi thứ nhất" }, _openedVersion);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [When("coach lưu thay đổi ở nơi thứ hai")]
    public Task SecondPlaceSaves() =>
        api.TryUpdateAsync(_fullName!, r => r with { TrainingGoal = "Nơi thứ hai" }, _openedVersion);

    [Then("thay đổi ở nơi thứ hai không được lưu vì hồ sơ đã được sửa ở nơi khác")]
    public Task SecondPlaceRejected() => api.AssertErrorAsync(HttpStatusCode.Conflict, "student.version_conflict");

    [Then(@"^hồ sơ của ""([^""]*)"" giữ thay đổi của nơi thứ nhất$")]
    public async Task KeepsFirstPlace(string fullName) =>
        Assert.Equal("Nơi thứ nhất", (await api.GetAsync(fullName)).Registration.TrainingGoal);
}
```

- [x] **Step 6: Viết integration test (seed bị loại + phiên bản)**

`backend/tests/Coaching.Api.IntegrationTests/Students/UpdateStudentTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Students;
using Coaching.Domain.Students;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class UpdateStudentTests(ApiFactory factory)
{
    // @BR-f02-ho-so-hoc-vien-012 (seed loại khỏi living doc): sửa hồ sơ không tồn tại
    [Fact]
    public async Task Updating_unknown_student_returns_404()
    {
        var admin = await factory.AdminClientAsync();
        var existing = await StudentRequests.CreateAsync(admin);

        var response = await admin.PutAsJsonAsync($"{StudentRequests.Url}/{Guid.NewGuid()}",
            new { fullName = "Lan", registration = new RegistrationInput(), version = existing.Version });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("student.not_found", await StudentRequests.ErrorCodeAsync(response));
    }

    // @BR-f02-ho-so-hoc-vien-012: email gửi kèm trong lệnh sửa bị bỏ qua
    [Fact]
    public async Task Email_cannot_be_changed_by_update()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var response = await admin.PutAsJsonAsync($"{StudentRequests.Url}/{student.Id}", new
        {
            fullName = "Lan",
            email = "doi-email@test.local",
            registration = new RegistrationInput { WeightKg = 70m },
            version = student.Version,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        Assert.Equal(student.Email, updated.Email);
        Assert.Equal(70m, updated.Registration.WeightKg);
    }

    // @ADR-f02-ho-so-hoc-vien-004: thiếu version → 400; mỗi lần lưu ra version mới
    [Fact]
    public async Task Version_is_required_and_changes_after_each_save()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var missing = await admin.PutAsJsonAsync($"{StudentRequests.Url}/{student.Id}",
            new { fullName = "Lan", registration = new RegistrationInput() });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal("student.version_required", await StudentRequests.ErrorCodeAsync(missing));

        var saved = await admin.PutAsJsonAsync($"{StudentRequests.Url}/{student.Id}",
            new { fullName = "Lan", registration = new RegistrationInput(), version = student.Version });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.NotEqual(student.Version, (await saved.Content.ReadFromJsonAsync<StudentDetailDto>())!.Version);
    }

    // @BR-f02-ho-so-hoc-vien-015: lưu dựa trên phiên bản cũ → 409, không ghi đè
    [Fact]
    public async Task Stale_version_is_rejected_with_409()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);
        var url = $"{StudentRequests.Url}/{student.Id}";

        var first = await admin.PutAsJsonAsync(url,
            new { fullName = "Lan", registration = new RegistrationInput { TrainingGoal = "Một" }, version = student.Version });
        var second = await admin.PutAsJsonAsync(url,
            new { fullName = "Lan", registration = new RegistrationInput { TrainingGoal = "Hai" }, version = student.Version });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("student.version_conflict", await StudentRequests.ErrorCodeAsync(second));
        Assert.Equal("Một", (await admin.GetFromJsonAsync<StudentDetailDto>(url))!.Registration.TrainingGoal);
    }
}
```

- [x] **Step 7: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet build Coaching.slnx`
Expected: FAIL — build error `'StudentDetailDto' does not contain a definition for 'Version'` / `StudentRepository does not implement interface member GetForUpdateAsync`.

- [x] **Step 8: Hạ tầng phiên bản — mapping, repository, lỗi xung đột**

`backend/src/Coaching.Infrastructure/Persistence/Configurations/StudentConfiguration.cs` — thêm trước dòng `builder.Ignore(s => s.DomainEvents);`:
```csharp
        // @ADR-f02-ho-so-hoc-vien-004: rowversion là concurrency token của mọi lệnh sửa
        builder.Property(s => s.Version).IsRowVersion();
```

`backend/src/Coaching.Infrastructure/Persistence/Repositories/StudentRepository.cs` — thêm method sau `GetAsync`:
```csharp
    public async Task<Student?> GetForUpdateAsync(Guid id, Guid coachId, byte[] expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var student = await GetAsync(id, coachId, cancellationToken);
        if (student is not null)
            db.Entry(student).Property(s => s.Version).OriginalValue = expectedVersion; // @ADR-f02-ho-so-hoc-vien-004
        return student;
    }
```

`backend/src/Coaching.Application/Common/ConcurrencyConflictException.cs`:
```csharp
namespace Coaching.Application.Common;

/// <summary>@ADR-f02-ho-so-hoc-vien-004: dữ liệu đã được lưu ở nơi khác kể từ phiên bản request gửi lên.</summary>
public sealed class ConcurrencyConflictException(Exception innerException)
    : Exception("Dữ liệu đã được sửa ở nơi khác.", innerException);
```

`backend/src/Coaching.Infrastructure/Persistence/AppDbContext.cs` — thêm method sau `ExecuteInTransactionAsync`:
```csharp
    // @ADR-f02-ho-so-hoc-vien-004: use case gọi qua IUnitOfWork nhận lỗi của Application, không phụ thuộc EF.
    // Identity (UserManager) gọi SaveChangesAsync công khai nên vẫn tự xử lý DbUpdateConcurrencyException như cũ.
    async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
    }
```

- [x] **Step 9: Application — lỗi, phiên bản, DTO, use case sửa**

`backend/src/Coaching.Application/Students/StudentErrors.cs` — thêm sau `EmailTaken`:
```csharp

    public static readonly Error VersionRequired =
        new("student.version_required", "Thiếu phiên bản hồ sơ. Tải lại hồ sơ rồi thử lại.");

    public static readonly Error RegistrationRequired =
        new("student.registration_required", "Thiếu nội dung form đăng ký.");

    // @BR-f02-ho-so-hoc-vien-015 @ADR-f02-ho-so-hoc-vien-004
    public static readonly Error VersionConflict =
        new("student.version_conflict", "Hồ sơ vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.", ErrorKind.Conflict);
```

`backend/src/Coaching.Application/Students/StudentVersion.cs`:
```csharp
namespace Coaching.Application.Students;

/// <summary>@ADR-f02-ho-so-hoc-vien-004: version trong API là base64 của rowversion.</summary>
internal static class StudentVersion
{
    public static string Format(byte[] version) => Convert.ToBase64String(version);

    public static byte[]? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out var written) && written > 0 ? buffer[..written] : null;
    }
}
```

`backend/src/Coaching.Application/Students/StudentDtos.cs` — thay toàn bộ:
```csharp
using Coaching.Domain.Coaches;
using Coaching.Domain.Students;

namespace Coaching.Application.Students;

/// <summary>Hồ sơ chi tiết cho coach (chỉ dùng ở /api/admin).</summary>
public sealed record StudentDetailDto(
    Guid Id,
    Guid CoachId,
    string FullName,
    string Email,
    string AccountStatus,
    string CoachingStatus,
    DateTimeOffset CreatedAt,
    RegistrationInput Registration,
    IReadOnlyList<string> MissingRequired,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    string Version)
{
    public static StudentDetailDto From(Student student, Coach coach) => new(
        student.Id,
        student.CoachId,
        student.FullName,
        student.Email.Value,
        student.AccountStatus.ToString(),
        student.CoachingStatus.ToString(),
        student.CreatedAt,
        student.Registration.ToInput(),
        student.MissingRequiredFields, // @BR-f02-ho-so-hoc-vien-004
        student.UpdatedAt, // @BR-f02-ho-so-hoc-vien-013
        student.UpdatedByUserId is { } editor ? ActorName(editor, coach) : null,
        StudentVersion.Format(student.Version)); // @ADR-f02-ho-so-hoc-vien-004

    /// <summary>MVP chỉ có coach sở hữu hồ sơ thao tác trên hồ sơ.</summary>
    public static string ActorName(Guid userId, Coach coach) =>
        userId == coach.UserId ? coach.DisplayName : "Người dùng khác";
}
```

`backend/src/Coaching.Application/Students/Commands/UpdateStudent.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

/// <summary>@BR-f02-ho-so-hoc-vien-012: không có Email — email không sửa ở đây (F01-P1).</summary>
public sealed record UpdateStudentCommand(string? FullName, RegistrationInput? Registration, string? Version);

public sealed class UpdateStudentHandler(
    CurrentCoach currentCoach,
    IStudentRepository students,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(Guid id, UpdateStudentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;
        if (StudentVersion.Parse(command.Version) is not { } version)
            return StudentErrors.VersionRequired;
        if (command.Registration is null)
            return StudentErrors.RegistrationRequired;

        // @BR-f02-ho-so-hoc-vien-012: hồ sơ không tồn tại (hoặc của coach khác) → 404
        if (await students.GetForUpdateAsync(id, coach.Id, version, cancellationToken) is not { } student)
            return StudentErrors.NotFound;

        var registration = RegistrationForm.Create(command.Registration, clock.Today);
        student.UpdateRegistration(command.FullName, registration, coach.UserId, clock.UtcNow); // @BR-f02-ho-so-hoc-vien-013

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return StudentErrors.VersionConflict; // @BR-f02-ho-so-hoc-vien-015 @ADR-f02-ho-so-hoc-vien-004
        }

        return Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach));
    }
}
```

`backend/src/Coaching.Application/DependencyInjection.cs` — thêm sau `services.AddScoped<GetStudentHandler>();`:
```csharp
        services.AddScoped<UpdateStudentHandler>();
```

`backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs` — thêm action sau `Get`:
```csharp

    [HttpPut("{id:guid}")]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentCommand command,
        [FromServices] UpdateStudentHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, command, cancellationToken)).ToActionResult(this);
```

- [x] **Step 10: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f02-ho-so-hoc-vien-012|Category=BR-f02-ho-so-hoc-vien-013|Category=BR-f02-ho-so-hoc-vien-015)&Category!=web"`
Expected: `Passed: 4`, `Failed: 0`.
Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Students"`
Expected: `Passed: 14`.
Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed.

- [x] **Step 11: Commit**
```bash
git add backend/src backend/tests
git commit -F - <<'EOF'
feat(students): update profile with editor tracking and rowversion conflicts (BR-f02-012, 013, 015)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 4: Ghi chú nội bộ + dữ liệu phía học viên không có ghi chú

**Files:**
- Modify: `backend/src/Coaching.Domain/Students/Student.cs`, `backend/src/Coaching.Domain/Students/IStudentRepository.cs`
- Modify: `backend/src/Coaching.Infrastructure/Persistence/Repositories/StudentRepository.cs`
- Create: `backend/src/Coaching.Application/Students/Commands/UpdateInternalNote.cs`, `backend/src/Coaching.Application/Students/Queries/GetStudentSelf.cs`
- Modify: `backend/src/Coaching.Application/Students/StudentDtos.cs`, `backend/src/Coaching.Application/Students/Commands/CreateStudent.cs`, `backend/src/Coaching.Application/DependencyInjection.cs`
- Modify: `backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs`, `backend/src/Coaching.Api/Controllers/Client/MeController.cs`
- Test: `backend/tests/Coaching.Domain.Tests/Students/InternalNoteTests.cs`
- Test: `backend/tests/Coaching.Acceptance/Support/StudentApi.cs` (M), `backend/tests/Coaching.Acceptance/Steps/StudentNoteSteps.cs`
- Test: `backend/tests/Coaching.Api.IntegrationTests/Architecture/ClientDtoTests.cs`, `backend/tests/Coaching.Api.IntegrationTests/Students/InternalNoteTests.cs`

**Interfaces:**
- Consumes: Task 1 — `CurrentCoach`, `StudentApi`, `StudentRequests`, `ApiFactory.CreateStudentAsync()` (học viên có `Student`); Task 2 — `StepText`, step `Given hôm nay là …`; Task 3 — `IStudentRepository.GetForUpdateAsync`, `ConcurrencyConflictException`, `StudentVersion.Parse`, `StudentErrors.VersionRequired/VersionConflict`, step `Given hồ sơ của "X" đã có`, `When coach sửa mục tiêu tập luyện của "X"`.
- Produces:
  - Domain: `Student.InternalNote (string?)`, `Student.InternalNoteUpdatedAt (DateTimeOffset?)`, `Student.UpdateInternalNote(string? note, DateTimeOffset at)`, `const Student.InternalNoteMaxLength = 5000`; `IStudentRepository.GetByUserIdAsync(Guid userId, CancellationToken ct = default)`.
  - Lỗi 400: `student.internal_note_too_long`.
  - Application: `StudentDetailDto` thêm `string? InternalNote, DateTimeOffset? InternalNoteUpdatedAt`; `CreateStudentCommand(string? FullName, string? Email, RegistrationInput? Registration, string? InternalNote = null)` — ghi chú (nếu có) lưu cùng request/transaction tạo hồ sơ; `UpdateInternalNoteCommand(string? Note, string? Version)`, `UpdateInternalNoteHandler.HandleAsync(Guid id, cmd, ct)`; `StudentSelfDto(string FullName, string Email, string CoachingStatus, string AccountStatus)`, `GetStudentSelfHandler.HandleAsync(ct)`.
  - API: `PUT /api/admin/students/{id}/internal-note` → 200 `StudentDetailDto`; `GET /api/client/me` → `StudentSelfDto` (404 `student.not_found` nếu user chưa có `Student`).
  - Test: `StudentApi.UpdateNoteOkAsync(string fullName, string? note)`.

**Owns:** `@BR-f02-ho-so-hoc-vien-014`, `@BR-f02-ho-so-hoc-vien-016` | `@ADR-f02-ho-so-hoc-vien-007`

- [x] **Step 1: Viết Domain test cho ghi chú nội bộ**

`backend/tests/Coaching.Domain.Tests/Students/InternalNoteTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class InternalNoteTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 20, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Nov23 = new(2026, 11, 23, 9, 0, 0, TimeSpan.FromHours(7));
    private static readonly DateTimeOffset Nov24 = new(2026, 11, 24, 9, 0, 0, TimeSpan.FromHours(7));

    private static Student NewStudent() =>
        Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Lan", Email.Create("lan@gmail.com"), CreatedAt);

    // @BR-f02-ho-so-hoc-vien-014
    [Fact]
    public void UpdateInternalNote_sets_note_and_its_own_time()
    {
        var student = NewStudent();

        student.UpdateInternalNote("  Tránh lunge sâu vì gối phải  ", Nov23);

        Assert.Equal("Tránh lunge sâu vì gối phải", student.InternalNote);
        Assert.Equal(Nov23, student.InternalNoteUpdatedAt);
        Assert.Null(student.UpdatedAt); // ghi chú không tính là sửa hồ sơ
    }

    // @BR-f02-ho-so-hoc-vien-014: thời điểm sửa ghi chú chỉ đổi khi ghi chú đổi
    [Fact]
    public void Same_note_keeps_previous_time()
    {
        var student = NewStudent();
        student.UpdateInternalNote("Theo dõi vai trái", Nov23);

        student.UpdateInternalNote(" Theo dõi vai trái ", Nov24);

        Assert.Equal(Nov23, student.InternalNoteUpdatedAt);
    }

    // @BR-f02-ho-so-hoc-vien-014
    [Fact]
    public void Editing_registration_does_not_touch_note_time()
    {
        var student = NewStudent();
        student.UpdateInternalNote("Theo dõi vai trái", Nov23);

        student.UpdateRegistration("Lan", RegistrationForm.CreateEmpty(), Guid.NewGuid(), Nov24);

        Assert.Equal(Nov23, student.InternalNoteUpdatedAt);
        Assert.Equal(Nov24, student.UpdatedAt);
    }

    // @BR-f02-ho-so-hoc-vien-014
    [Fact]
    public void Blank_note_clears_the_note()
    {
        var student = NewStudent();
        student.UpdateInternalNote("Theo dõi vai trái", Nov23);

        student.UpdateInternalNote("   ", Nov24);

        Assert.Null(student.InternalNote);
        Assert.Equal(Nov24, student.InternalNoteUpdatedAt);
    }

    // @ADR-f02-ho-so-hoc-vien-009: ghi chú nội bộ ≤ 5000
    [Fact]
    public void Note_longer_than_5000_is_rejected()
    {
        var student = NewStudent();
        student.UpdateInternalNote(new string('a', 5000), Nov23);

        var ex = Assert.Throws<DomainException>(() => student.UpdateInternalNote(new string('b', 5001), Nov24));

        Assert.Equal("student.internal_note_too_long", ex.Code);
        Assert.Equal(new string('a', 5000), student.InternalNote);
    }
}
```

- [x] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~InternalNoteTests"`
Expected: FAIL — build error `'Student' does not contain a definition for 'UpdateInternalNote'`.

- [x] **Step 3: Domain — ghi chú nội bộ, tra hồ sơ theo tài khoản**

`backend/src/Coaching.Domain/Students/Student.cs` — thêm sau `public const int FullNameMaxLength = 100;`:
```csharp
    public const int InternalNoteMaxLength = 5000; // @ADR-f02-ho-so-hoc-vien-009
```
thêm sau thuộc tính `Version`:
```csharp
    /// <summary>Chỉ coach thấy — không bao giờ có trong dữ liệu trả cho học viên (@BR-f02-ho-so-hoc-vien-016).</summary>
    public string? InternalNote { get; private set; }

    /// <summary>@BR-f02-ho-so-hoc-vien-014: thời điểm sửa ghi chú, tách khỏi UpdatedAt của hồ sơ.</summary>
    public DateTimeOffset? InternalNoteUpdatedAt { get; private set; }
```
và thêm method sau `UpdateRegistration`:
```csharp
    /// <summary>@BR-f02-ho-so-hoc-vien-014: thời điểm sửa ghi chú chỉ đổi khi nội dung ghi chú đổi.</summary>
    public void UpdateInternalNote(string? note, DateTimeOffset at)
    {
        var value = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (value is not null && value.Length > InternalNoteMaxLength)
            throw new DomainException("student.internal_note_too_long",
                $"Ghi chú nội bộ tối đa {InternalNoteMaxLength} ký tự.");
        if (value == InternalNote)
            return;
        InternalNote = value;
        InternalNoteUpdatedAt = at;
    }
```

`backend/src/Coaching.Domain/Students/IStudentRepository.cs` — thêm sau `GetForUpdateAsync`:
```csharp
    /// <summary>Hồ sơ của tài khoản đăng nhập (dùng cho /api/client — lọc theo user trong token).</summary>
    Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
```

- [x] **Step 4: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~InternalNoteTests"`
Expected: `Passed: 5`.

- [x] **Step 5: Viết step defs `@api` cho BR-014, BR-016**

`backend/tests/Coaching.Acceptance/Support/StudentApi.cs` — thêm method sau `UpdateOkAsync`:
```csharp
    public async Task<StudentDetailDto> UpdateNoteOkAsync(string fullName, string? note)
    {
        var current = await GetAsync(fullName);
        var response = await ctx.SendJsonAsync(HttpMethod.Put, $"/api/admin/students/{Ids[fullName]}/internal-note",
            new { note, version = current.Version }, AdminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        LastDetail = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        return LastDetail;
    }
```

`backend/tests/Coaching.Acceptance/Steps/StudentNoteSteps.cs`:
```csharp
using System.Net;
using System.Text.Json;
using Coaching.Acceptance.Support;
using Coaching.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class StudentNoteSteps(AcceptanceContext ctx, StudentApi api)
{
    private string? _note;
    private string? _selfJson;

    // @BR-f02-ho-so-hoc-vien-014
    [When(@"^coach sửa ghi chú nội bộ của ""([^""]*)""$")]
    public Task EditNote(string fullName) => api.UpdateNoteOkAsync(fullName, $"Ghi chú {Guid.NewGuid():N}");

    [Then(@"^ghi chú nội bộ của ""([^""]*)"" cho biết sửa lần cuối ngày ""([^""]*)""$")]
    [Then(@"^ghi chú nội bộ của ""([^""]*)"" vẫn cho biết sửa lần cuối ngày ""([^""]*)""$")]
    public async Task NoteEditedOn(string fullName, string date)
    {
        var detail = await api.GetAsync(fullName);
        Assert.NotNull(detail.InternalNoteUpdatedAt);
        Assert.Equal(StepText.ParseDate(date), StepText.VietnamDate(detail.InternalNoteUpdatedAt!.Value));
    }

    [Given(@"^ghi chú nội bộ của ""([^""]*)"" sửa lần cuối ngày ""([^""]*)""$")]
    public async Task NoteEditedEarlier(string fullName, string date)
    {
        await api.MoveClockToAsync(StepText.VietnamTime(date, 9, 0));
        await api.EnsureAsync(fullName);
        await api.UpdateNoteOkAsync(fullName, "Ghi chú ban đầu");
    }

    // @BR-f02-ho-so-hoc-vien-016
    [Given(@"^ghi chú nội bộ của ""([^""]*)"" là ""([^""]*)""$")]
    public async Task NoteIs(string fullName, string note)
    {
        await api.EnsureAsync(fullName);
        await api.UpdateNoteOkAsync(fullName, note);
        _note = note;
    }

    [When(@"^coach xem hồ sơ của ""([^""]*)""$")]
    public Task CoachViews(string fullName) => api.GetAsync(fullName);

    [Then(@"^coach thấy ghi chú nội bộ ""([^""]*)""$")]
    public void CoachSeesNote(string note) => Assert.Equal(note, api.LastDetail!.InternalNote);

    [When(@"^""([^""]*)"" lấy dữ liệu của chính mình$")]
    public async Task StudentFetchesOwnData(string fullName)
    {
        var detail = await api.GetAsync(fullName);
        const string password = "Student@123";
        await using (var scope = ctx.Host.Services.CreateAsyncScope())
        {
            // Kích hoạt tài khoản là việc của F01; ở đây đặt mật khẩu trực tiếp để học viên đăng nhập được.
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = (await users.FindByEmailAsync(detail.Email))!;
            Assert.True((await users.AddPasswordAsync(user, password)).Succeeded);
        }

        var tokens = await ctx.LoginOkAsync(detail.Email, password, "client");
        var response = await ctx.GetAsync("/api/client/me", tokens.AccessToken);
        _selfJson = await response.Content.ReadAsStringAsync();
    }

    [Then(@"^dữ liệu ""([^""]*)"" nhận được không chứa ghi chú nội bộ$")]
    public void OwnDataHasNoNote(string fullName)
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        var names = new List<string>();
        var values = new List<string>();
        using (var json = JsonDocument.Parse(_selfJson!))
            Collect(json.RootElement, names, values);

        Assert.Contains(fullName, values); // đúng dữ liệu của học viên
        Assert.DoesNotContain(names, n => n.Contains("note", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(_note!, values);
    }

    private static void Collect(JsonElement element, List<string> names, List<string> values)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    names.Add(property.Name);
                    Collect(property.Value, names, values);
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    Collect(item, names, values);
                break;
            case JsonValueKind.String:
                values.Add(element.GetString()!);
                break;
        }
    }
}
```

- [x] **Step 6: Viết test kiến trúc DTO phía học viên + integration test ghi chú**

`backend/tests/Coaching.Api.IntegrationTests/Architecture/ClientDtoTests.cs`:
```csharp
using System.Reflection;
using Coaching.Api.Controllers.Client;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.IntegrationTests.Architecture;

/// <summary>
/// @NFR-f02-ho-so-hoc-vien-001 @ADR-f02-ho-so-hoc-vien-007 @BR-f02-ho-so-hoc-vien-016:
/// mọi type trả về từ Controllers/Client/** không có property nào chứa "Note" (kể cả type lồng nhau).
/// </summary>
public sealed class ClientDtoTests
{
    [Fact]
    public void Client_api_dtos_never_expose_a_note()
    {
        var actions = typeof(ClientControllerBase).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ClientControllerBase)) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .ToList();
        Assert.NotEmpty(actions);

        var offenders = new List<string>();
        foreach (var action in actions)
        {
            var where = $"{action.DeclaringType!.Name}.{action.Name}";
            var produced = action.GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Select(a => a.Type)
                .Where(t => t != typeof(void))
                .ToList();
            Assert.True(produced.Count > 0, $"{where} phải khai báo [ProducesResponseType<T>] để test quét được DTO.");
            foreach (var type in produced)
                Collect(type, where, offenders, []);
        }

        Assert.Empty(offenders);
    }

    private static void Collect(Type type, string path, List<string> offenders, HashSet<Type> seen)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string) || type.IsPrimitive || type.IsEnum || !seen.Add(type))
            return;
        if (type.IsArray)
        {
            Collect(type.GetElementType()!, path, offenders, seen);
            return;
        }
        if (type.Namespace?.StartsWith("System", StringComparison.Ordinal) == true)
        {
            foreach (var argument in type.GetGenericArguments())
                Collect(argument, path, offenders, seen);
            return;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.Name.Contains("Note", StringComparison.OrdinalIgnoreCase))
                offenders.Add($"{path}: {type.Name}.{property.Name}");
            Collect(property.PropertyType, $"{path} > {type.Name}.{property.Name}", offenders, seen);
        }
    }
}
```

`backend/tests/Coaching.Api.IntegrationTests/Students/InternalNoteTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Students;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class InternalNoteTests(ApiFactory factory)
{
    // @BR-f02-ho-so-hoc-vien-014: sửa ghi chú có thời điểm riêng; sửa form sau đó không đổi thời điểm ghi chú
    [Fact]
    public async Task Note_time_changes_only_when_note_changes()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);
        var url = $"{StudentRequests.Url}/{student.Id}";

        var noted = await admin.PutAsJsonAsync($"{url}/internal-note", new { note = "Theo dõi vai trái", version = student.Version });
        Assert.Equal(HttpStatusCode.OK, noted.StatusCode);
        var afterNote = (await noted.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        Assert.Equal("Theo dõi vai trái", afterNote.InternalNote);
        Assert.NotNull(afterNote.InternalNoteUpdatedAt);

        var edited = await admin.PutAsJsonAsync(url,
            new { fullName = student.FullName, registration = new RegistrationInput { TrainingGoal = "Tăng cơ" }, version = afterNote.Version });
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        Assert.Equal(afterNote.InternalNoteUpdatedAt, (await edited.Content.ReadFromJsonAsync<StudentDetailDto>())!.InternalNoteUpdatedAt);
    }

    // @ADR-f02-ho-so-hoc-vien-009: ghi chú 5001 ký tự → 400
    [Fact]
    public async Task Note_longer_than_5000_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var response = await admin.PutAsJsonAsync($"{StudentRequests.Url}/{student.Id}/internal-note",
            new { note = new string('a', 5001), version = student.Version });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("student.internal_note_too_long", await StudentRequests.ErrorCodeAsync(response));
    }

    // @ADR-f02-ho-so-hoc-vien-007: /api/client/me đọc từ Student của chính học viên trong token
    [Fact]
    public async Task Client_me_returns_own_student_profile()
    {
        var (email, password) = await factory.CreateStudentAsync();
        var tokens = await factory.LoginAsync(email, password);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);

        var me = (await client.GetFromJsonAsync<StudentSelfProbe>("/api/client/me"))!;

        Assert.Equal(email, me.Email);
        Assert.Equal("Học viên Test", me.FullName);
        Assert.Equal("Active", me.CoachingStatus);
        Assert.Equal("PendingActivation", me.AccountStatus);
    }

    // @BR-f02-ho-so-hoc-vien-014 @BR-f02-ho-so-hoc-vien-016: tạo hồ sơ kèm ghi chú trong cùng request
    // → coach thấy ghi chú + thời điểm sửa ghi chú; học viên vẫn không nhận được ghi chú
    [Fact]
    public async Task Create_with_note_stores_note_but_client_never_sees_it()
    {
        var admin = await factory.AdminClientAsync();
        var email = StudentRequests.NewEmail();

        var created = await admin.PostAsJsonAsync(StudentRequests.Url,
            new { fullName = "Lan", email, internalNote = "  Tránh lunge sâu vì gối phải  " });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dto = (await created.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        Assert.Equal("Tránh lunge sâu vì gối phải", dto.InternalNote);
        Assert.Equal(dto.CreatedAt, dto.InternalNoteUpdatedAt);

        const string password = "Student@123";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            // Kích hoạt là việc của F01; đặt mật khẩu trực tiếp để học viên đăng nhập được.
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            Assert.True((await users.AddPasswordAsync((await users.FindByEmailAsync(email))!, password)).Succeeded);
        }
        var tokens = await factory.LoginAsync(email, password);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);

        var json = await client.GetStringAsync("/api/client/me");

        Assert.DoesNotContain("note", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lunge", json, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record StudentSelfProbe(string FullName, string Email, string CoachingStatus, string AccountStatus);
}
```

- [x] **Step 7: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet build Coaching.slnx`
Expected: FAIL — build error `'StudentDetailDto' does not contain a definition for 'InternalNote'` / `StudentRepository does not implement interface member GetByUserIdAsync`.

- [x] **Step 8: Repository, DTO, use case, endpoint**

`backend/src/Coaching.Infrastructure/Persistence/Repositories/StudentRepository.cs` — thêm method sau `GetForUpdateAsync`:
```csharp
    public Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Students.AsNoTracking().SingleOrDefaultAsync(s => s.UserId == userId, cancellationToken);
```

`backend/src/Coaching.Application/Students/StudentDtos.cs` — thay toàn bộ:
```csharp
using Coaching.Domain.Coaches;
using Coaching.Domain.Students;

namespace Coaching.Application.Students;

/// <summary>Hồ sơ chi tiết cho coach — CHỈ dùng ở /api/admin (có ghi chú nội bộ).</summary>
public sealed record StudentDetailDto(
    Guid Id,
    Guid CoachId,
    string FullName,
    string Email,
    string AccountStatus,
    string CoachingStatus,
    DateTimeOffset CreatedAt,
    RegistrationInput Registration,
    IReadOnlyList<string> MissingRequired,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    string Version,
    string? InternalNote,
    DateTimeOffset? InternalNoteUpdatedAt)
{
    public static StudentDetailDto From(Student student, Coach coach) => new(
        student.Id,
        student.CoachId,
        student.FullName,
        student.Email.Value,
        student.AccountStatus.ToString(),
        student.CoachingStatus.ToString(),
        student.CreatedAt,
        student.Registration.ToInput(),
        student.MissingRequiredFields, // @BR-f02-ho-so-hoc-vien-004
        student.UpdatedAt, // @BR-f02-ho-so-hoc-vien-013
        student.UpdatedByUserId is { } editor ? ActorName(editor, coach) : null,
        StudentVersion.Format(student.Version), // @ADR-f02-ho-so-hoc-vien-004
        student.InternalNote, // @BR-f02-ho-so-hoc-vien-016: coach thấy ghi chú
        student.InternalNoteUpdatedAt); // @BR-f02-ho-so-hoc-vien-014

    /// <summary>MVP chỉ có coach sở hữu hồ sơ thao tác trên hồ sơ.</summary>
    public static string ActorName(Guid userId, Coach coach) =>
        userId == coach.UserId ? coach.DisplayName : "Người dùng khác";
}
```

`backend/src/Coaching.Application/Students/Commands/UpdateInternalNote.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

public sealed record UpdateInternalNoteCommand(string? Note, string? Version);

/// <summary>Ghi chú nội bộ không ghi audit (NFR-f02-ho-so-hoc-vien-002: audit không lưu nội dung ghi chú).</summary>
public sealed class UpdateInternalNoteHandler(
    CurrentCoach currentCoach,
    IStudentRepository students,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(Guid id, UpdateInternalNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;
        if (StudentVersion.Parse(command.Version) is not { } version)
            return StudentErrors.VersionRequired;
        if (await students.GetForUpdateAsync(id, coach.Id, version, cancellationToken) is not { } student)
            return StudentErrors.NotFound;

        student.UpdateInternalNote(command.Note, clock.UtcNow); // @BR-f02-ho-so-hoc-vien-014

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return StudentErrors.VersionConflict;
        }

        return Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach));
    }
}
```

`backend/src/Coaching.Application/Students/Queries/GetStudentSelf.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Queries;

/// <summary>
/// @ADR-f02-ho-so-hoc-vien-007 @BR-f02-ho-so-hoc-vien-016: DTO riêng cho phía học viên — KHÔNG có ghi chú nội bộ.
/// Giữ FullName, Email cho web học viên hiện tại (F00).
/// </summary>
public sealed record StudentSelfDto(string FullName, string Email, string CoachingStatus, string AccountStatus);

public sealed class GetStudentSelfHandler(IStudentRepository students, ICurrentUser currentUser)
{
    public async Task<Result<StudentSelfDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return new Error("auth.unauthenticated", "Chưa đăng nhập.", ErrorKind.Unauthorized);

        var student = await students.GetByUserIdAsync(userId, cancellationToken); // lọc theo user trong token
        return student is null
            ? StudentErrors.NotFound
            : Result<StudentSelfDto>.Success(new StudentSelfDto(
                student.FullName,
                student.Email.Value,
                student.CoachingStatus.ToString(),
                student.AccountStatus.ToString()));
    }
}
```

`backend/src/Coaching.Application/Students/Commands/CreateStudent.cs` — thay toàn bộ (tạo hồ sơ nhận thêm `internalNote`, lưu cùng request/transaction):
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
    IStudentRepository students,
    IStudentAccounts accounts,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(CreateStudentCommand command, CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;

        var email = Email.Create(command.Email); // @BR-f02-ho-so-hoc-vien-002 → 400 email.required / email.invalid
        // @ADR-f02-ho-so-hoc-vien-006: luật form ở Domain; @BR-f02-ho-so-hoc-vien-010: "hôm nay" theo giờ VN
        var registration = RegistrationForm.Create(command.Registration ?? new RegistrationInput(), clock.Today);
        var now = clock.UtcNow;
        // Kiểm hồ sơ TRƯỚC khi tạo tài khoản; Id tài khoản sinh sẵn để Student giữ UserId.
        var student = Student.Create(coach.Id, Guid.CreateVersion7(), command.FullName, email, now, registration);
        // @BR-f02-ho-so-hoc-vien-014: ghi chú lúc tạo có thời điểm sửa ghi chú = thời điểm tạo; quá 5000 ký tự → 400 trước khi tạo tài khoản
        student.UpdateInternalNote(command.InternalNote, now);

        // @ADR-f02-ho-so-hoc-vien-001: tài khoản + hồ sơ (kèm ghi chú) cùng một transaction
        return await unitOfWork.ExecuteInTransactionAsync<StudentDetailDto>(async ct =>
        {
            var account = await accounts.CreatePendingAsync(student.UserId, email, student.FullName, ct);
            if (!account.IsSuccess)
                return Result<StudentDetailDto>.Failure(account.Error!); // @BR-f02-ho-so-hoc-vien-001 → 409

            students.Add(student);
            await unitOfWork.SaveChangesAsync(ct);
            return Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach));
        }, cancellationToken);
    }
}
```
(`UpdateInternalNote` với ghi chú rỗng/null không đổi gì → `InternalNoteUpdatedAt` vẫn null.)

`backend/src/Coaching.Application/DependencyInjection.cs` — thêm sau `services.AddScoped<UpdateStudentHandler>();`:
```csharp
        services.AddScoped<UpdateInternalNoteHandler>();
        services.AddScoped<GetStudentSelfHandler>();
```

`backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs` — thêm action sau `Update`:
```csharp

    [HttpPut("{id:guid}/internal-note")]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateInternalNote(Guid id, [FromBody] UpdateInternalNoteCommand command,
        [FromServices] UpdateInternalNoteHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, command, cancellationToken)).ToActionResult(this);
```

`backend/src/Coaching.Api/Controllers/Client/MeController.cs` — thay toàn bộ:
```csharp
using Coaching.Api.Common;
using Coaching.Application.Students.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers.Client;

public sealed class MeController(GetStudentSelfHandler getSelf) : ClientControllerBase
{
    /// <summary>
    /// Hồ sơ của học viên đang đăng nhập.
    /// @ADR-f02-ho-so-hoc-vien-007 @BR-f02-ho-so-hoc-vien-016: DTO riêng phía học viên, không có ghi chú nội bộ.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<StudentSelfDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        (await getSelf.HandleAsync(cancellationToken)).ToActionResult(this);
}
```

- [x] **Step 9: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f02-ho-so-hoc-vien-014|Category=BR-f02-ho-so-hoc-vien-016)&Category!=web"`
Expected: `Passed: 4`, `Failed: 0`.
Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Students|FullyQualifiedName~ClientDtoTests"`
Expected: `Passed: 19`.
Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed (F00 `AccessSteps` "học viên thấy đúng email và họ tên của mình" đọc `/api/client/me` mới — helper đã tạo `Student` cùng họ tên).

- [x] **Step 10: Commit**
```bash
git add backend/src backend/tests
git commit -F - <<'EOF'
feat(students): internal note and client self DTO without note (BR-f02-014, 016)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 5: Trạng thái coaching — đổi có lý do, lịch sử chỉ thêm, audit

**Files:**
- Create: `backend/src/Coaching.Domain/Students/CoachingStatusChange.cs`
- Modify: `backend/src/Coaching.Domain/Students/Student.cs`
- Modify: `backend/src/Coaching.Infrastructure/Persistence/Configurations/StudentConfiguration.cs`
- Create: `backend/src/Coaching.Application/Students/Commands/ChangeCoachingStatus.cs`
- Modify: `backend/src/Coaching.Application/Students/StudentDtos.cs`, `StudentErrors.cs`, `backend/src/Coaching.Application/DependencyInjection.cs`
- Modify: `backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs`
- Test: `backend/tests/Coaching.Domain.Tests/Students/CoachingStatusTests.cs`
- Test: `backend/tests/Coaching.Acceptance/Support/StudentApi.cs` (M), `backend/tests/Coaching.Acceptance/Steps/CoachingStatusSteps.cs`
- Test: `backend/tests/Coaching.Api.IntegrationTests/Students/CoachingStatusTests.cs`

**Interfaces:**
- Consumes: Task 1 — `CurrentCoach`, `IAuditLog.Record(action, entityType, entityId, details)` (F00), `StudentApi`, `StudentRequests`, step `Then trạng thái coaching của "X" là "Y"`; Task 3 — `GetForUpdateAsync`, `ConcurrencyConflictException`, `StudentVersion`, `StudentErrors.VersionRequired/VersionConflict`, `StudentDetailDto.ActorName`, `StudentApi.UpdateOkAsync`; Task 4 — `StudentDetailDto` hiện tại, `StudentApi.UpdateNoteOkAsync`.
- Produces:
  - Domain: `CoachingStatusChange : Entity<Guid>` (`FromStatus, ToStatus, ActorUserId, At, Reason`); `Student.StatusHistory (IReadOnlyList<CoachingStatusChange>)`; `Student.ChangeCoachingStatus(CoachingStatus newStatus, string? reason, Guid actorUserId, DateTimeOffset at)`; `const Student.ReasonMaxLength = 500`.
  - Lỗi 400: `student.status_invalid`, `student.status_unchanged`, `student.reason_required`, `student.reason_too_long`.
  - Application: `StatusChangeDto(string From, string To, string Actor, DateTimeOffset At, string Reason)`; `StudentDetailDto` thêm `IReadOnlyList<StatusChangeDto> StatusHistory` (mới nhất trước); `ChangeCoachingStatusCommand(string? Status, string? Reason, string? Version)`, `ChangeCoachingStatusHandler.HandleAsync(Guid id, cmd, ct)`; audit `coaching_status.changed` / `Student` / `From→To`.
  - API: `POST /api/admin/students/{id}/coaching-status` → 200 `StudentDetailDto` / 400 / 404 / 409.
  - Test: `StudentApi.TryChangeStatusAsync(string fullName, string status, string reason, string? version = null)`; step `Given trạng thái coaching của "X" là "Y"` (dùng lại ở Task 6).

**Owns:** `@BR-f02-ho-so-hoc-vien-017`, `@BR-f02-ho-so-hoc-vien-018`, `@BR-f02-ho-so-hoc-vien-019`, `@BR-f02-ho-so-hoc-vien-020`, `@BR-f02-ho-so-hoc-vien-021`, `@BR-f02-ho-so-hoc-vien-022` | `@ADR-f02-ho-so-hoc-vien-005`

- [x] **Step 1: Viết Domain test cho đổi trạng thái**

`backend/tests/Coaching.Domain.Tests/Students/CoachingStatusTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class CoachingStatusTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 20, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset At = new(2026, 11, 25, 9, 0, 0, TimeSpan.FromHours(7));
    private static readonly Guid Actor = Guid.NewGuid();

    private static Student NewStudent() => Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Lan",
        Email.Create("lan@gmail.com"), CreatedAt, RegistrationForm.Create(new RegistrationInput { TrainingGoal = "Giảm mỡ, giữ cơ" },
            new DateOnly(2026, 11, 20)));

    // @BR-f02-ho-so-hoc-vien-017 @BR-f02-ho-so-hoc-vien-020 @ADR-f02-ho-so-hoc-vien-005
    [Fact]
    public void Change_adds_exactly_one_history_row_with_from_to_actor_time_reason()
    {
        var student = NewStudent();

        student.ChangeCoachingStatus(CoachingStatus.Paused, "  Đi công tác  ", Actor, At);

        Assert.Equal(CoachingStatus.Paused, student.CoachingStatus);
        var row = Assert.Single(student.StatusHistory);
        Assert.Equal(CoachingStatus.Active, row.FromStatus);
        Assert.Equal(CoachingStatus.Paused, row.ToStatus);
        Assert.Equal(Actor, row.ActorUserId);
        Assert.Equal(At, row.At);
        Assert.Equal("Đi công tác", row.Reason);
    }

    // @BR-f02-ho-so-hoc-vien-017 @BR-f02-ho-so-hoc-vien-020: bị từ chối thì không thêm lịch sử
    [Fact]
    public void Same_status_is_rejected_without_history()
    {
        var student = NewStudent();

        var ex = Assert.Throws<DomainException>(() => student.ChangeCoachingStatus(CoachingStatus.Active, "Nhầm", Actor, At));

        Assert.Equal("student.status_unchanged", ex.Code);
        Assert.Empty(student.StatusHistory);
    }

    // @BR-f02-ho-so-hoc-vien-018 @BR-f02-ho-so-hoc-vien-020
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_reason_is_rejected_and_status_unchanged(string? reason)
    {
        var student = NewStudent();

        var ex = Assert.Throws<DomainException>(() => student.ChangeCoachingStatus(CoachingStatus.Paused, reason, Actor, At));

        Assert.Equal("student.reason_required", ex.Code);
        Assert.Equal(CoachingStatus.Active, student.CoachingStatus);
        Assert.Empty(student.StatusHistory);
    }

    // @BR-f02-ho-so-hoc-vien-018 @ADR-f02-ho-so-hoc-vien-009: lý do ≤ 500 (seed 501 loại khỏi living doc)
    [Fact]
    public void Reason_up_to_500_characters()
    {
        var student = NewStudent();
        student.ChangeCoachingStatus(CoachingStatus.Paused, new string('a', 500), Actor, At);

        var ex = Assert.Throws<DomainException>(() =>
            student.ChangeCoachingStatus(CoachingStatus.Ended, new string('a', 501), Actor, At));

        Assert.Equal("student.reason_too_long", ex.Code);
        Assert.Equal(CoachingStatus.Paused, student.CoachingStatus);
        Assert.Single(student.StatusHistory);
    }

    // @BR-f02-ho-so-hoc-vien-019: đổi được theo mọi chiều
    [Theory]
    [InlineData(CoachingStatus.Active, CoachingStatus.Paused)]
    [InlineData(CoachingStatus.Active, CoachingStatus.Ended)]
    [InlineData(CoachingStatus.Paused, CoachingStatus.Active)]
    [InlineData(CoachingStatus.Paused, CoachingStatus.Ended)]
    [InlineData(CoachingStatus.Ended, CoachingStatus.Active)]
    [InlineData(CoachingStatus.Ended, CoachingStatus.Paused)]
    public void Every_direction_is_allowed(CoachingStatus from, CoachingStatus to)
    {
        var student = NewStudent();
        if (from != CoachingStatus.Active)
            student.ChangeCoachingStatus(from, "Chuẩn bị", Actor, At);

        student.ChangeCoachingStatus(to, "Đổi", Actor, At.AddMinutes(1));

        Assert.Equal(to, student.CoachingStatus);
    }

    // @BR-f02-ho-so-hoc-vien-019: mở lại hồ sơ đã kết thúc, dữ liệu cũ giữ nguyên
    [Fact]
    public void Reopening_ended_student_keeps_data()
    {
        var student = NewStudent();
        student.ChangeCoachingStatus(CoachingStatus.Ended, "Hết gói", Actor, At);

        student.ChangeCoachingStatus(CoachingStatus.Active, "Mua gói mới", Actor, At.AddDays(1));

        Assert.Equal(CoachingStatus.Active, student.CoachingStatus);
        Assert.Equal("Giảm mỡ, giữ cơ", student.Registration.TrainingGoal);
        Assert.Equal(2, student.StatusHistory.Count);
    }

    // @BR-f02-ho-so-hoc-vien-022: tài khoản Chờ kích hoạt vẫn đổi được trạng thái coaching
    [Fact]
    public void Pending_account_can_change_coaching_status()
    {
        var student = NewStudent();
        Assert.Equal(AccountStatus.PendingActivation, student.AccountStatus);

        student.ChangeCoachingStatus(CoachingStatus.Paused, "Chưa chuyển khoản đủ", Actor, At);

        Assert.Equal(CoachingStatus.Paused, student.CoachingStatus);
    }

    // @BR-f02-ho-so-hoc-vien-021: lịch sử không sửa, không xóa được từ ngoài aggregate
    [Fact]
    public void History_is_read_only()
    {
        var student = NewStudent();
        student.ChangeCoachingStatus(CoachingStatus.Paused, "Đi công tác", Actor, At);

        var list = Assert.IsAssignableFrom<IList<CoachingStatusChange>>(student.StatusHistory);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
    }

    [Fact]
    public void Undefined_status_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => NewStudent().ChangeCoachingStatus((CoachingStatus)99, "x", Actor, At));
        Assert.Equal("student.status_invalid", ex.Code);
    }
}
```

- [x] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~CoachingStatusTests"`
Expected: FAIL — build error `'Student' does not contain a definition for 'ChangeCoachingStatus'`.

- [x] **Step 3: Domain — lịch sử trạng thái, đổi trạng thái**

`backend/src/Coaching.Domain/Students/CoachingStatusChange.cs`:
```csharp
using Coaching.Domain.Common;

namespace Coaching.Domain.Students;

/// <summary>
/// @ADR-f02-ho-so-hoc-vien-005 @BR-f02-ho-so-hoc-vien-020: một dòng lịch sử đổi trạng thái coaching.
/// @BR-f02-ho-so-hoc-vien-021: không có setter công khai, chỉ Student tạo được → không sửa/xóa.
/// </summary>
public sealed class CoachingStatusChange : Entity<Guid>
{
    public CoachingStatus FromStatus { get; private set; }

    public CoachingStatus ToStatus { get; private set; }

    public Guid ActorUserId { get; private set; }

    public DateTimeOffset At { get; private set; }

    public string Reason { get; private set; } = default!;

    private CoachingStatusChange() { }

    internal CoachingStatusChange(CoachingStatus from, CoachingStatus to, Guid actorUserId, DateTimeOffset at, string reason)
        : base(Guid.CreateVersion7())
    {
        FromStatus = from;
        ToStatus = to;
        ActorUserId = actorUserId;
        At = at;
        Reason = reason;
    }
}
```

`backend/src/Coaching.Domain/Students/Student.cs` — thêm sau `public const int InternalNoteMaxLength = 5000; …`:
```csharp
    public const int ReasonMaxLength = 500; // @ADR-f02-ho-so-hoc-vien-009

    private readonly List<CoachingStatusChange> _statusHistory = [];
```
thêm sau thuộc tính `InternalNoteUpdatedAt`:
```csharp
    /// <summary>@BR-f02-ho-so-hoc-vien-021: chỉ đọc; chỉ ChangeCoachingStatus thêm dòng.</summary>
    public IReadOnlyList<CoachingStatusChange> StatusHistory => _statusHistory.AsReadOnly();
```
và thêm method sau `UpdateInternalNote`:
```csharp
    /// <summary>
    /// @BR-f02-ho-so-hoc-vien-017: chỉ sang trạng thái khác trạng thái hiện tại.
    /// @BR-f02-ho-so-hoc-vien-018: cần lý do 1–500 ký tự (sau trim).
    /// @BR-f02-ho-so-hoc-vien-019: Active / Paused / Ended đổi qua lại theo mọi chiều; dữ liệu khác giữ nguyên.
    /// @BR-f02-ho-so-hoc-vien-020 @ADR-f02-ho-so-hoc-vien-005: thành công ⇒ thêm đúng một dòng lịch sử; thất bại ⇒ không thêm.
    /// @BR-f02-ho-so-hoc-vien-022: không phụ thuộc trạng thái tài khoản.
    /// </summary>
    public void ChangeCoachingStatus(CoachingStatus newStatus, string? reason, Guid actorUserId, DateTimeOffset at)
    {
        if (!Enum.IsDefined(newStatus))
            throw new DomainException("student.status_invalid", "Trạng thái coaching không hợp lệ.");
        if (newStatus == CoachingStatus)
            throw new DomainException("student.status_unchanged", "Trạng thái coaching không thay đổi.");

        var text = reason?.Trim();
        if (string.IsNullOrEmpty(text))
            throw new DomainException("student.reason_required", "Cần nhập lý do đổi trạng thái coaching.");
        if (text.Length > ReasonMaxLength)
            throw new DomainException("student.reason_too_long", $"Lý do tối đa {ReasonMaxLength} ký tự.");

        _statusHistory.Add(new CoachingStatusChange(CoachingStatus, newStatus, actorUserId, at, text));
        CoachingStatus = newStatus;
    }
```

- [x] **Step 4: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~CoachingStatusTests"`
Expected: `Passed: 16`.

- [x] **Step 5: Viết step defs `@api` cho BR-017→022**

`backend/tests/Coaching.Acceptance/Support/StudentApi.cs` — thêm method sau `UpdateNoteOkAsync`:
```csharp
    public async Task<HttpResponseMessage> TryChangeStatusAsync(string fullName, string status, string reason, string? version = null)
    {
        var current = await GetAsync(fullName);
        var response = await ctx.SendJsonAsync(HttpMethod.Post, $"/api/admin/students/{Ids[fullName]}/coaching-status",
            new { status, reason, version = version ?? current.Version }, AdminToken);
        if (response.IsSuccessStatusCode)
            LastDetail = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
        return response;
    }
```

`backend/tests/Coaching.Acceptance/Steps/CoachingStatusSteps.cs`:
```csharp
using System.Globalization;
using System.Net;
using Coaching.Acceptance.Support;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class CoachingStatusSteps(AcceptanceContext ctx, StudentApi api)
{
    private int _historyBefore;

    // @BR-f02-ho-so-hoc-vien-017 @BR-f02-ho-so-hoc-vien-018 @BR-f02-ho-so-hoc-vien-019 (dùng lại ở BR-023, 025)
    [Given(@"^trạng thái coaching của ""([^""]*)"" là ""([^""]*)""$")]
    public async Task StatusIs(string fullName, string label)
    {
        await api.EnsureAsync(fullName);
        var target = StudentApi.CoachingStatuses[label];
        if ((await api.GetAsync(fullName)).CoachingStatus != target)
            Assert.Equal(HttpStatusCode.OK, (await api.TryChangeStatusAsync(fullName, target, "Chuẩn bị kịch bản")).StatusCode);
    }

    [When(@"^coach đổi trạng thái coaching của ""([^""]*)"" sang ""([^""]*)"" với lý do ""([^""]*)""$")]
    public Task Change(string fullName, string label, string reason) => ChangeAsync(fullName, label, reason);

    [When(@"^coach đổi trạng thái coaching của ""([^""]*)"" sang ""([^""]*)"" với lý do chỉ gồm khoảng trắng$")]
    public Task ChangeWithBlankReason(string fullName, string label) => ChangeAsync(fullName, label, "   ");

    [Then("việc đổi trạng thái bị từ chối vì trạng thái không thay đổi")]
    public Task RejectedUnchanged() => api.AssertErrorAsync(HttpStatusCode.BadRequest, "student.status_unchanged");

    [Then("việc đổi trạng thái bị từ chối vì thiếu lý do")]
    public Task RejectedMissingReason() => api.AssertErrorAsync(HttpStatusCode.BadRequest, "student.reason_required");

    [Then(@"^trạng thái coaching của ""([^""]*)"" vẫn là ""([^""]*)""$")]
    public async Task StatusStillIs(string fullName, string label) =>
        Assert.Equal(StudentApi.CoachingStatuses[label], (await api.GetAsync(fullName)).CoachingStatus);

    // @BR-f02-ho-so-hoc-vien-019
    [Given(@"^hồ sơ của ""([^""]*)"" có mục tiêu tập luyện ""([^""]*)""$")]
    public async Task HasGoal(string fullName, string goal)
    {
        await api.EnsureAsync(fullName);
        await api.UpdateOkAsync(fullName, r => r with { TrainingGoal = goal });
    }

    // @BR-f02-ho-so-hoc-vien-019 @BR-f02-ho-so-hoc-vien-022
    [Then(@"^hồ sơ của ""([^""]*)"" (?:vẫn có|có) mục tiêu tập luyện ""([^""]*)""$")]
    public async Task GoalIs(string fullName, string goal) =>
        Assert.Equal(goal, (await api.GetAsync(fullName)).Registration.TrainingGoal);

    // @BR-f02-ho-so-hoc-vien-020
    [Then(@"^lịch sử trạng thái của ""([^""]*)"" có thêm đúng một dòng:$")]
    public async Task HistoryGainedOneRow(string fullName, DataTable table)
    {
        var detail = await api.GetAsync(fullName);
        Assert.Equal(_historyBefore + 1, detail.StatusHistory.Count);

        var expected = table.Rows.Single();
        var newest = detail.StatusHistory[0];
        Assert.Equal(StudentApi.CoachingStatuses[expected["tu"]], newest.From);
        Assert.Equal(StudentApi.CoachingStatuses[expected["sang"]], newest.To);
        Assert.Equal(expected["nguoi_thuc_hien"], newest.Actor);
        Assert.Equal(expected["thoi_diem"],
            newest.At.ToOffset(StepText.VietnamOffset).ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture));
        Assert.Equal(expected["ly_do"], newest.Reason);
    }

    // @BR-f02-ho-so-hoc-vien-021
    [Given(@"^""([^""]*)"" đã được chuyển ""([^""]*)"" sang ""([^""]*)"" rồi ""([^""]*)"" sang ""([^""]*)""$")]
    public async Task ChangedTwice(string fullName, string from1, string to1, string from2, string to2)
    {
        await api.EnsureAsync(fullName);
        Assert.Equal(StudentApi.CoachingStatuses[from1], (await api.GetAsync(fullName)).CoachingStatus);
        Assert.Equal(HttpStatusCode.OK, (await api.TryChangeStatusAsync(fullName, StudentApi.CoachingStatuses[to1], "Lần 1")).StatusCode);
        ctx.Clock.Advance(TimeSpan.FromMinutes(1)); // hai lần đổi khác thời điểm → thứ tự "mới nhất trước" xác định
        Assert.Equal(StudentApi.CoachingStatuses[from2], api.LastDetail!.CoachingStatus);
        Assert.Equal(HttpStatusCode.OK, (await api.TryChangeStatusAsync(fullName, StudentApi.CoachingStatuses[to2], "Lần 2")).StatusCode);
    }

    [When(@"^coach xem lịch sử trạng thái của ""([^""]*)""$")]
    public Task ViewHistory(string fullName) => api.GetAsync(fullName);

    [Then(@"^lịch sử có (\d+) dòng, dòng ""([^""]*)"" sang ""([^""]*)"" đứng đầu$")]
    public void HistoryNewestFirst(int count, string from, string to)
    {
        var history = api.LastDetail!.StatusHistory;
        Assert.Equal(count, history.Count);
        Assert.Equal(StudentApi.CoachingStatuses[from], history[0].From);
        Assert.Equal(StudentApi.CoachingStatuses[to], history[0].To);
    }

    // @BR-f02-ho-so-hoc-vien-022
    [Given(@"^tài khoản của ""([^""]*)"" ở trạng thái ""([^""]*)""$")]
    public async Task AccountStatusIs(string fullName, string label)
    {
        await api.EnsureAsync(fullName);
        Assert.Equal(StudentApi.AccountStatuses[label], (await api.GetAsync(fullName)).AccountStatus);
    }

    [When(@"^coach sửa mục tiêu tập luyện của ""([^""]*)"" thành ""([^""]*)""$")]
    public Task EditGoalTo(string fullName, string goal) => api.UpdateOkAsync(fullName, r => r with { TrainingGoal = goal });

    private async Task ChangeAsync(string fullName, string label, string reason)
    {
        await api.EnsureAsync(fullName);
        _historyBefore = (await api.GetAsync(fullName)).StatusHistory.Count;
        await api.TryChangeStatusAsync(fullName, StudentApi.CoachingStatuses[label], reason);
    }
}
```

- [x] **Step 6: Viết integration test (seed bị loại + audit)**

`backend/tests/Coaching.Api.IntegrationTests/Students/CoachingStatusTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Students;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class CoachingStatusTests(ApiFactory factory)
{
    private static string StatusUrl(Guid id) => $"{StudentRequests.Url}/{id}/coaching-status";

    // @BR-f02-ho-so-hoc-vien-015 (seed loại khỏi living doc; cần endpoint của task này):
    // hai lệnh đổi trạng thái cùng một phiên bản → chỉ một thành công, lịch sử 1 dòng
    [Fact]
    public async Task Two_status_changes_from_the_same_version_only_one_succeeds()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var first = await admin.PostAsJsonAsync(StatusUrl(student.Id), new { status = "Paused", reason = "Đi công tác", version = student.Version });
        var second = await admin.PostAsJsonAsync(StatusUrl(student.Id), new { status = "Ended", reason = "Hết gói", version = student.Version });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("student.version_conflict", await StudentRequests.ErrorCodeAsync(second));
        var detail = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{student.Id}"))!;
        Assert.Equal("Paused", detail.CoachingStatus);
        Assert.Single(detail.StatusHistory);
    }

    // @BR-f02-ho-so-hoc-vien-018 (seed loại khỏi living doc): lý do 501 ký tự
    [Fact]
    public async Task Reason_longer_than_500_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var response = await admin.PostAsJsonAsync(StatusUrl(student.Id),
            new { status = "Paused", reason = new string('a', 501), version = student.Version });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("student.reason_too_long", await StudentRequests.ErrorCodeAsync(response));
    }

    // @BR-f02-ho-so-hoc-vien-020 (seed loại khỏi living doc): đổi bị từ chối không thêm lịch sử
    [Fact]
    public async Task Rejected_change_adds_no_history()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var response = await admin.PostAsJsonAsync(StatusUrl(student.Id),
            new { status = "Active", reason = "Nhầm", version = student.Version });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("student.status_unchanged", await StudentRequests.ErrorCodeAsync(response));
        var detail = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{student.Id}"))!;
        Assert.Empty(detail.StatusHistory);
    }

    [Theory]
    [InlineData("Archived")]
    [InlineData("1")]
    public async Task Unknown_status_is_rejected(string status)
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var response = await admin.PostAsJsonAsync(StatusUrl(student.Id), new { status, reason = "x", version = student.Version });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("student.status_invalid", await StudentRequests.ErrorCodeAsync(response));
    }

    // @BR-f02-ho-so-hoc-vien-021 (seed loại khỏi living doc): không có API sửa/xóa lịch sử
    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task History_cannot_be_edited_or_deleted(string method)
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var response = await admin.SendAsync(new HttpRequestMessage(new HttpMethod(method), StatusUrl(student.Id)));

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    // NFR-f02-ho-so-hoc-vien-002: mỗi lần đổi trạng thái có đúng 1 dòng audit (người + thời điểm), không lưu lý do/ghi chú
    [Fact]
    public async Task Status_change_is_audited_without_reason_or_note()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);
        var noted = await admin.PutAsJsonAsync($"{StudentRequests.Url}/{student.Id}/internal-note",
            new { note = "Ghi chú bí mật", version = student.Version });
        var version = (await noted.Content.ReadFromJsonAsync<StudentDetailDto>())!.Version;

        var changed = await admin.PostAsJsonAsync(StatusUrl(student.Id), new { status = "Paused", reason = "Lý do riêng tư", version });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = Assert.Single(await db.AuditLogs
            .Where(a => a.EntityType == "Student" && a.EntityId == student.Id.ToString())
            .ToListAsync());
        Assert.Equal("coaching_status.changed", entry.Action);
        Assert.Equal("Active→Paused", entry.Details);
        Assert.Equal(ApiFactory.AdminEmail, entry.ActorEmail);
        Assert.NotEqual(default, entry.OccurredAt);
        Assert.DoesNotContain("Lý do riêng tư", entry.Details);
        Assert.DoesNotContain("Ghi chú bí mật", entry.Details);
    }
}
```

- [x] **Step 7: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet build Coaching.slnx`
Expected: FAIL — build error `'StudentDetailDto' does not contain a definition for 'StatusHistory'`.

- [x] **Step 8: Mapping lịch sử, DTO, use case, endpoint**

`backend/src/Coaching.Infrastructure/Persistence/Configurations/StudentConfiguration.cs` — thêm trước dòng `builder.Ignore(s => s.DomainEvents);`:
```csharp
        // @ADR-f02-ho-so-hoc-vien-005: lịch sử là entity con, bảng riêng, chỉ thêm
        builder.OwnsMany(s => s.StatusHistory, h =>
        {
            h.ToTable("StudentCoachingStatusChanges");
            h.WithOwner().HasForeignKey("StudentId");
            h.HasKey(c => c.Id);
            h.Property(c => c.Id).ValueGeneratedNever();
            h.Property(c => c.FromStatus).HasConversion<string>().HasMaxLength(20);
            h.Property(c => c.ToStatus).HasConversion<string>().HasMaxLength(20);
            h.Property(c => c.Reason).HasMaxLength(Student.ReasonMaxLength).IsRequired();
            h.HasIndex("StudentId", nameof(CoachingStatusChange.At));
        });
        builder.Navigation(s => s.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
```

`backend/src/Coaching.Application/Students/StudentErrors.cs` — thêm sau `VersionConflict`:
```csharp

    public static readonly Error StatusInvalid =
        new("student.status_invalid", "Trạng thái coaching không hợp lệ.");
```

`backend/src/Coaching.Application/Students/StudentDtos.cs` — thay toàn bộ:
```csharp
using Coaching.Domain.Coaches;
using Coaching.Domain.Students;

namespace Coaching.Application.Students;

/// <summary>Một dòng lịch sử trạng thái coaching (@BR-f02-ho-so-hoc-vien-020).</summary>
public sealed record StatusChangeDto(string From, string To, string Actor, DateTimeOffset At, string Reason);

/// <summary>Hồ sơ chi tiết cho coach — CHỈ dùng ở /api/admin (có ghi chú nội bộ).</summary>
public sealed record StudentDetailDto(
    Guid Id,
    Guid CoachId,
    string FullName,
    string Email,
    string AccountStatus,
    string CoachingStatus,
    DateTimeOffset CreatedAt,
    RegistrationInput Registration,
    IReadOnlyList<string> MissingRequired,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    string Version,
    string? InternalNote,
    DateTimeOffset? InternalNoteUpdatedAt,
    IReadOnlyList<StatusChangeDto> StatusHistory)
{
    public static StudentDetailDto From(Student student, Coach coach) => new(
        student.Id,
        student.CoachId,
        student.FullName,
        student.Email.Value,
        student.AccountStatus.ToString(),
        student.CoachingStatus.ToString(),
        student.CreatedAt,
        student.Registration.ToInput(),
        student.MissingRequiredFields, // @BR-f02-ho-so-hoc-vien-004
        student.UpdatedAt, // @BR-f02-ho-so-hoc-vien-013
        student.UpdatedByUserId is { } editor ? ActorName(editor, coach) : null,
        StudentVersion.Format(student.Version), // @ADR-f02-ho-so-hoc-vien-004
        student.InternalNote, // @BR-f02-ho-so-hoc-vien-016: coach thấy ghi chú
        student.InternalNoteUpdatedAt, // @BR-f02-ho-so-hoc-vien-014
        student.StatusHistory // @BR-f02-ho-so-hoc-vien-021: mới nhất trước
            .OrderByDescending(c => c.At)
            .ThenByDescending(c => c.Id)
            .Select(c => new StatusChangeDto(c.FromStatus.ToString(), c.ToStatus.ToString(),
                ActorName(c.ActorUserId, coach), c.At, c.Reason))
            .ToList());

    /// <summary>MVP chỉ có coach sở hữu hồ sơ thao tác trên hồ sơ.</summary>
    public static string ActorName(Guid userId, Coach coach) =>
        userId == coach.UserId ? coach.DisplayName : "Người dùng khác";
}
```

`backend/src/Coaching.Application/Students/Commands/ChangeCoachingStatus.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

/// <param name="Status">"Active" | "Paused" | "Ended".</param>
public sealed record ChangeCoachingStatusCommand(string? Status, string? Reason, string? Version);

public sealed class ChangeCoachingStatusHandler(
    CurrentCoach currentCoach,
    IStudentRepository students,
    IUnitOfWork unitOfWork,
    IAuditLog audit,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(Guid id, ChangeCoachingStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Status is null || !Enum.GetNames<CoachingStatus>().Contains(command.Status))
            return StudentErrors.StatusInvalid;
        var status = Enum.Parse<CoachingStatus>(command.Status);

        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;
        if (StudentVersion.Parse(command.Version) is not { } version)
            return StudentErrors.VersionRequired;
        if (await students.GetForUpdateAsync(id, coach.Id, version, cancellationToken) is not { } student)
            return StudentErrors.NotFound;

        var from = student.CoachingStatus;
        // @BR-f02-ho-so-hoc-vien-017..022 @ADR-f02-ho-so-hoc-vien-005: invariant nằm trong aggregate
        student.ChangeCoachingStatus(status, command.Reason, coach.UserId, clock.UtcNow);
        // NFR-f02-ho-so-hoc-vien-002: audit chỉ ghi From→To (người + thời điểm do IAuditLog lấy) — không lý do, không ghi chú
        audit.Record("coaching_status.changed", "Student", student.Id.ToString(), $"{from}→{status}");

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return StudentErrors.VersionConflict; // @BR-f02-ho-so-hoc-vien-015: hai lệnh cùng phiên bản → chỉ một thành công
        }

        return Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach));
    }
}
```

`backend/src/Coaching.Application/DependencyInjection.cs` — thêm sau `services.AddScoped<GetStudentSelfHandler>();`:
```csharp
        services.AddScoped<ChangeCoachingStatusHandler>();
```

`backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs` — thêm action sau `UpdateInternalNote`:
```csharp

    // @BR-f02-ho-so-hoc-vien-021: route này chỉ có POST — PUT/DELETE → 405, không sửa/xóa được lịch sử
    [HttpPost("{id:guid}/coaching-status")]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeCoachingStatus(Guid id, [FromBody] ChangeCoachingStatusCommand command,
        [FromServices] ChangeCoachingStatusHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, command, cancellationToken)).ToActionResult(this);
```

- [x] **Step 9: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f02-ho-so-hoc-vien-017|Category=BR-f02-ho-so-hoc-vien-018|Category=BR-f02-ho-so-hoc-vien-019|Category=BR-f02-ho-so-hoc-vien-020|Category=BR-f02-ho-so-hoc-vien-021|Category=BR-f02-ho-so-hoc-vien-022)&Category!=web"`
Expected: `Passed: 11`, `Failed: 0`.
Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Students|FullyQualifiedName~ClientDtoTests"`
Expected: `Passed: 27`.
Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed.

- [x] **Step 10: Commit**
```bash
git add backend/src backend/tests
git commit -F - <<'EOF'
feat(students): coaching status changes with reason, append-only history, audit (BR-f02-017..022)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 6: Danh sách + tìm kiếm không dấu — `SearchKey`, GET danh sách, không có xóa

**Files:**
- Create: `backend/src/Coaching.Domain/Students/SearchText.cs`, `backend/src/Coaching.Domain/Students/StudentSummary.cs`
- Modify: `backend/src/Coaching.Domain/Students/Student.cs`, `backend/src/Coaching.Domain/Students/IStudentRepository.cs`
- Modify: `backend/src/Coaching.Infrastructure/Persistence/Repositories/StudentRepository.cs`, `Persistence/Configurations/StudentConfiguration.cs`, `Seeding/DatabaseInitializer.cs`
- Create: `backend/src/Coaching.Application/Students/Queries/ListStudents.cs`
- Modify: `backend/src/Coaching.Application/DependencyInjection.cs`, `backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs`
- Test: `backend/tests/Coaching.Domain.Tests/Students/SearchTextTests.cs`
- Test: `backend/tests/Coaching.Acceptance/Support/StudentApi.cs` (M), `backend/tests/Coaching.Acceptance/Steps/StudentListSteps.cs`
- Test: `backend/tests/Coaching.Api.IntegrationTests/Students/StudentListTests.cs`

**Interfaces:**
- Consumes: Task 1 — `CurrentCoach`, `StudentApi`, `StudentRequests`, `DatabaseInitializer`; Task 2 — `StepText.Quoted`; Task 3 — `Student.UpdateRegistration`; Task 5 — step `Given trạng thái coaching của "X" là "Y"`, `When coach đổi trạng thái coaching của …`.
- Produces:
  - Domain: `SearchText.Normalize(string?) → string` (đ/Đ → d, bỏ dấu, lowercase, trim); `Student.SearchKey`, `Student.RefreshSearchKey()`, `const Student.SearchKeyMaxLength = 400`; `StudentSummary(Guid Id, string FullName, string Email, CoachingStatus CoachingStatus, AccountStatus AccountStatus, DateTimeOffset CreatedAt)`; `IStudentRepository.ListAsync(Guid coachId, string normalizedQuery, CancellationToken ct = default) → Task<IReadOnlyList<StudentSummary>>`.
  - Application: `StudentListItemDto(Guid Id, string FullName, string Email, string CoachingStatus, string AccountStatus, DateTimeOffset CreatedAt)`; `ListStudentsHandler.HandleAsync(string? query, ct) → Result<IReadOnlyList<StudentListItemDto>>`.
  - API: `GET /api/admin/students?q=` → 200 `StudentListItemDto[]`, mới tạo trước.
  - Test: `StudentApi.ListAsync(string? q) → Task<IReadOnlyList<StudentListItemDto>>`.

**Owns:** `@BR-f02-ho-so-hoc-vien-023`, `@BR-f02-ho-so-hoc-vien-024`, `@BR-f02-ho-so-hoc-vien-025` | `@ADR-f02-ho-so-hoc-vien-003`

- [x] **Step 1: Viết Domain test cho chuẩn hóa tìm kiếm**

`backend/tests/Coaching.Domain.Tests/Students/SearchTextTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class SearchTextTests
{
    // @ADR-f02-ho-so-hoc-vien-003 @BR-f02-ho-so-hoc-vien-024: không phân biệt hoa/thường, không phân biệt dấu, đ = d
    [Theory]
    [InlineData("Phạm Hoàng Quân", "pham hoang quan")]
    [InlineData("Quân", "quan")]
    [InlineData("  ĐỨC ", "duc")]
    [InlineData("Hoàng Minh Đức", "hoang minh duc")]
    [InlineData("Nguyễn Thị Ánh Tuyết", "nguyen thi anh tuyet")]
    [InlineData("HQuanBilly@Gmail.com", "hquanbilly@gmail.com")]
    public void Normalize_strips_vietnamese_marks_and_lowercases(string input, string expected) =>
        Assert.Equal(expected, SearchText.Normalize(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_of_blank_is_empty(string? input) => Assert.Equal(string.Empty, SearchText.Normalize(input));

    // @ADR-f02-ho-so-hoc-vien-003: SearchKey gồm họ tên + email và cập nhật khi đổi tên
    [Fact]
    public void Student_search_key_covers_name_and_email_and_follows_rename()
    {
        var at = new DateTimeOffset(2026, 11, 25, 2, 0, 0, TimeSpan.Zero);
        var student = Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Phạm Hoàng Quân", Email.Create("hquanbilly@gmail.com"), at);
        Assert.Equal("pham hoang quan hquanbilly@gmail.com", student.SearchKey);

        student.UpdateRegistration("Hoàng Minh Đức", RegistrationForm.CreateEmpty(), Guid.NewGuid(), at);

        Assert.Equal("hoang minh duc hquanbilly@gmail.com", student.SearchKey);
    }
}
```

- [x] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~SearchTextTests"`
Expected: FAIL — build error `The name 'SearchText' does not exist in the current context`.

- [x] **Step 3: Domain — `SearchText`, `SearchKey`, tóm tắt danh sách**

`backend/src/Coaching.Domain/Students/SearchText.cs`:
```csharp
using System.Globalization;
using System.Text;

namespace Coaching.Domain.Students;

/// <summary>
/// @ADR-f02-ho-so-hoc-vien-003: chuẩn hóa để tìm không dấu. Collation *_CI_AI của SQL Server không coi "đ" = "d",
/// nên tự bỏ dấu ở Domain: đ/Đ → d, tách dấu (FormD) rồi bỏ dấu kết hợp, lowercase.
/// </summary>
public static class SearchText
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var decomposed = text.Trim().Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
```

`backend/src/Coaching.Domain/Students/StudentSummary.cs`:
```csharp
namespace Coaching.Domain.Students;

/// <summary>Một dòng danh sách học viên (@BR-f02-ho-so-hoc-vien-023): tên, email, trạng thái coaching, trạng thái tài khoản.</summary>
public sealed record StudentSummary(
    Guid Id,
    string FullName,
    string Email,
    CoachingStatus CoachingStatus,
    AccountStatus AccountStatus,
    DateTimeOffset CreatedAt);
```

`backend/src/Coaching.Domain/Students/Student.cs` — 4 thay đổi:
1. Thêm sau `public const int ReasonMaxLength = 500; …`:
```csharp
    public const int SearchKeyMaxLength = 400; // họ tên 100 + " " + email 256
```
2. Thêm sau thuộc tính `Email`:
```csharp
    /// <summary>@ADR-f02-ho-so-hoc-vien-003: họ tên + email đã bỏ dấu, lowercase — chỉ dùng để tìm kiếm.</summary>
    public string SearchKey { get; private set; } = string.Empty;
```
3. Thay toàn bộ method `Create` bằng:
```csharp
    public static Student Create(Guid coachId, Guid userId, string? fullName, Email email, DateTimeOffset at,
        RegistrationForm? registration = null)
    {
        ArgumentNullException.ThrowIfNull(email);
        if (coachId == Guid.Empty)
            throw new DomainException("student.coach_required", "Hồ sơ phải thuộc một coach.");
        if (userId == Guid.Empty)
            throw new DomainException("student.user_required", "Hồ sơ phải gắn với một tài khoản đăng nhập.");

        var student = new Student(Guid.CreateVersion7())
        {
            CoachId = coachId, // @BR-f02-ho-so-hoc-vien-007
            UserId = userId,
            FullName = NormalizeFullName(fullName), // @BR-f02-ho-so-hoc-vien-003
            Email = email, // @BR-f02-ho-so-hoc-vien-001 @BR-f02-ho-so-hoc-vien-002: Email đã chuẩn hóa (trim + lowercase)
            AccountStatus = AccountStatus.PendingActivation, // @BR-f02-ho-so-hoc-vien-005
            CoachingStatus = CoachingStatus.Active, // @BR-f02-ho-so-hoc-vien-006
            CreatedAt = at,
            Registration = registration ?? RegistrationForm.CreateEmpty(),
        };
        student.RefreshSearchKey();
        return student;
    }
```
4. Trong `UpdateRegistration`, thêm sau dòng `FullName = name;`:
```csharp
        RefreshSearchKey();
```
và thêm method sau `ChangeCoachingStatus`:
```csharp
    /// <summary>@ADR-f02-ho-so-hoc-vien-003: tính lại SearchKey (gọi khi tạo, khi đổi tên, và khi backfill lúc khởi động).</summary>
    public void RefreshSearchKey() => SearchKey = SearchText.Normalize($"{FullName} {Email.Value}");
```

`backend/src/Coaching.Domain/Students/IStudentRepository.cs` — thêm sau `GetByUserIdAsync`:
```csharp
    /// <summary>
    /// @BR-f02-ho-so-hoc-vien-023: mọi hồ sơ của coach ở mọi trạng thái, tạo sau đứng trước.
    /// @BR-f02-ho-so-hoc-vien-024: <paramref name="normalizedQuery"/> (đã qua SearchText.Normalize) rỗng = không lọc.
    /// </summary>
    Task<IReadOnlyList<StudentSummary>> ListAsync(Guid coachId, string normalizedQuery, CancellationToken cancellationToken = default);
```

- [x] **Step 4: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students"`
Expected: không Failed (`SearchTextTests` 10 test mới).

- [x] **Step 5: Viết step defs `@api` cho BR-023, 024, 025**

`backend/tests/Coaching.Acceptance/Support/StudentApi.cs` — thêm method sau `TryChangeStatusAsync`:
```csharp
    public async Task<IReadOnlyList<StudentListItemDto>> ListAsync(string? q)
    {
        var url = string.IsNullOrEmpty(q) ? "/api/admin/students" : $"/api/admin/students?q={Uri.EscapeDataString(q)}";
        var response = await ctx.GetAsync(url, AdminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<StudentListItemDto>>())!;
    }
```
và thêm `using Coaching.Application.Students.Queries;` vào đầu file.

`backend/tests/Coaching.Acceptance/Steps/StudentListSteps.cs`:
```csharp
using Coaching.Acceptance.Support;
using Coaching.Application.Students.Queries;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class StudentListSteps(AcceptanceContext ctx, StudentApi api)
{
    private IReadOnlyList<StudentListItemDto>? _rows;

    // @BR-f02-ho-so-hoc-vien-023
    [Given(@"^coach đã tạo lần lượt hồ sơ (.+)$")]
    public async Task CreatedInOrder(string names)
    {
        foreach (var name in StepText.Quoted(names))
        {
            await api.CreateOkAsync(name);
            ctx.Clock.Advance(TimeSpan.FromSeconds(1)); // CreatedAt khác nhau → thứ tự xác định
        }
    }

    [When("coach xem danh sách học viên")]
    public async Task ViewList() => _rows = await api.ListAsync(null);

    [Then(@"^danh sách lần lượt là (.+)$")]
    public void ListInOrder(string names) =>
        Assert.Equal(StepText.Quoted(names), _rows!.Select(r => r.FullName).ToList());

    [Then("mỗi dòng cho biết tên, email, trạng thái coaching và trạng thái tài khoản")]
    public void RowsShowNameEmailAndStatuses() => Assert.All(_rows!, row =>
    {
        Assert.False(string.IsNullOrWhiteSpace(row.FullName));
        Assert.Contains("@", row.Email);
        Assert.Contains(row.CoachingStatus, StudentApi.CoachingStatuses.Values);
        Assert.Contains(row.AccountStatus, StudentApi.AccountStatuses.Values);
    });

    // @BR-f02-ho-so-hoc-vien-024
    [Given(@"^có hồ sơ ""([^""]*)"" với email ""([^""]*)""$")]
    public Task HasStudent(string fullName, string email) => api.CreateOkAsync(fullName, email);

    [When(@"^coach tìm học viên với từ khóa ""([^""]*)""$")]
    public async Task Search(string keyword) => _rows = await api.ListAsync(keyword);

    [Then(@"^kết quả có hồ sơ ""([^""]*)""$")]
    public void ResultContains(string fullName) => Assert.Contains(_rows!, r => r.FullName == fullName);

    [Then("kết quả tìm kiếm rỗng")]
    public void ResultEmpty() => Assert.Empty(_rows!);

    // @BR-f02-ho-so-hoc-vien-025
    [Then(@"^hồ sơ của ""([^""]*)"" vẫn có trong danh sách học viên$")]
    public async Task StillListed(string fullName) =>
        Assert.Contains(await api.ListAsync(null), r => r.Id == api.Ids[fullName]);
}
```

- [x] **Step 6: Viết integration test (không có xóa, tìm "đ", backfill)**

`backend/tests/Coaching.Api.IntegrationTests/Students/StudentListTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Students.Queries;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class StudentListTests(ApiFactory factory)
{
    private static async Task<List<StudentListItemDto>> ListAsync(HttpClient admin, string? q = null) =>
        (await admin.GetFromJsonAsync<List<StudentListItemDto>>(
            q is null ? StudentRequests.Url : $"{StudentRequests.Url}?q={Uri.EscapeDataString(q)}"))!;

    // @BR-f02-ho-so-hoc-vien-025 (seed loại khỏi living doc): không có API xóa hồ sơ
    [Fact]
    public async Task Delete_student_is_not_allowed()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin);

        var response = await admin.DeleteAsync($"{StudentRequests.Url}/{student.Id}");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Contains(await ListAsync(admin), r => r.Id == student.Id);
    }

    // @BR-f02-ho-so-hoc-vien-023: hồ sơ tạo sau đứng trước
    [Fact]
    public async Task Newest_student_comes_first()
    {
        var admin = await factory.AdminClientAsync();
        var older = await StudentRequests.CreateAsync(admin, "Học viên cũ");
        var newer = await StudentRequests.CreateAsync(admin, "Học viên mới");

        var ids = (await ListAsync(admin)).Select(r => r.Id).ToList();

        Assert.True(ids.IndexOf(newer.Id) < ids.IndexOf(older.Id));
    }

    // @BR-f02-ho-so-hoc-vien-024 @ADR-f02-ho-so-hoc-vien-003: "duc" tìm ra "Đức" (collation CI_AI không làm được)
    [Theory]
    [InlineData("duc")]
    [InlineData("ĐỨC")]
    [InlineData("minh d")]
    public async Task Search_ignores_case_and_vietnamese_marks_including_d(string keyword)
    {
        var admin = await factory.AdminClientAsync();
        var duc = await StudentRequests.CreateAsync(admin, "Hoàng Minh Đức");

        Assert.Contains(await ListAsync(admin, keyword), r => r.Id == duc.Id);
    }

    // @ADR-f02-ho-so-hoc-vien-003: hồ sơ tạo trước khi có SearchKey được tính lại khi khởi động
    [Fact]
    public async Task Startup_backfills_empty_search_keys()
    {
        var admin = await factory.AdminClientAsync();
        var student = await StudentRequests.CreateAsync(admin, "Trần Thị Bích Ngọc");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Students] SET [SearchKey] = N'' WHERE [Id] = {student.Id}");
        }
        Assert.DoesNotContain(await ListAsync(admin, "bich ngoc"), r => r.Id == student.Id);

        await using (var restarted = factory.WithWebHostBuilder(_ => { }))
            _ = restarted.Services; // khởi động lại → DatabaseInitializer

        Assert.Contains(await ListAsync(admin, "bich ngoc"), r => r.Id == student.Id);
    }
}
```

- [x] **Step 7: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet build Coaching.slnx`
Expected: FAIL — build error `The type or namespace name 'StudentListItemDto' could not be found` / `StudentRepository does not implement interface member ListAsync`.

- [x] **Step 8: Mapping, repository, use case, endpoint, backfill**

`backend/src/Coaching.Infrastructure/Persistence/Configurations/StudentConfiguration.cs` — thêm trước dòng `builder.Ignore(s => s.DomainEvents);`:
```csharp
        // @ADR-f02-ho-so-hoc-vien-003
        builder.Property(s => s.SearchKey).HasMaxLength(Student.SearchKeyMaxLength).IsRequired();
```

`backend/src/Coaching.Infrastructure/Persistence/Repositories/StudentRepository.cs` — thêm method sau `GetByUserIdAsync`:
```csharp
    // @BR-f02-ho-so-hoc-vien-023 @BR-f02-ho-so-hoc-vien-024 @ADR-f02-ho-so-hoc-vien-003:
    // 1 truy vấn theo index (CoachId, CreatedAt) + LIKE '%q%' trên SearchKey (EF escape ký tự đặc biệt của LIKE)
    public async Task<IReadOnlyList<StudentSummary>> ListAsync(Guid coachId, string normalizedQuery,
        CancellationToken cancellationToken = default)
    {
        var query = db.Students.AsNoTracking().Where(s => s.CoachId == coachId);
        if (normalizedQuery.Length > 0)
            query = query.Where(s => s.SearchKey.Contains(normalizedQuery));

        var rows = await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new { s.Id, s.FullName, s.Email, s.CoachingStatus, s.AccountStatus, s.CreatedAt })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new StudentSummary(r.Id, r.FullName, r.Email.Value, r.CoachingStatus, r.AccountStatus, r.CreatedAt))
            .ToList();
    }
```

`backend/src/Coaching.Application/Students/Queries/ListStudents.cs`:
```csharp
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Queries;

/// <summary>@BR-f02-ho-so-hoc-vien-023: mỗi dòng có tên, email, trạng thái coaching, trạng thái tài khoản.</summary>
public sealed record StudentListItemDto(
    Guid Id,
    string FullName,
    string Email,
    string CoachingStatus,
    string AccountStatus,
    DateTimeOffset CreatedAt);

public sealed class ListStudentsHandler(CurrentCoach currentCoach, IStudentRepository students)
{
    public async Task<Result<IReadOnlyList<StudentListItemDto>>> HandleAsync(string? query,
        CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;

        // @BR-f02-ho-so-hoc-vien-024: từ khóa chuẩn hóa giống SearchKey (không dấu, lowercase)
        var rows = await students.ListAsync(coach.Id, SearchText.Normalize(query), cancellationToken);
        IReadOnlyList<StudentListItemDto> items = rows
            .Select(r => new StudentListItemDto(r.Id, r.FullName, r.Email,
                r.CoachingStatus.ToString(), r.AccountStatus.ToString(), r.CreatedAt))
            .ToList();
        return Result<IReadOnlyList<StudentListItemDto>>.Success(items);
    }
}
```

`backend/src/Coaching.Application/DependencyInjection.cs` — thêm sau `services.AddScoped<ChangeCoachingStatusHandler>();`:
```csharp
        services.AddScoped<ListStudentsHandler>();
```

`backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs` — thêm action trước `Create`:
```csharp
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StudentListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? q, [FromServices] ListStudentsHandler handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(q, cancellationToken)).ToActionResult(this);

```
(`using Coaching.Application.Students.Queries;` đã có.) Không thêm action `[HttpDelete]` — @BR-f02-ho-so-hoc-vien-025.

`backend/src/Coaching.Infrastructure/Seeding/DatabaseInitializer.cs` — thêm vào cuối `InitializeAsync`, sau khối `if (demoSeed.Enabled) { … }`:
```csharp

        // @ADR-f02-ho-so-hoc-vien-003: hồ sơ tạo trước khi có SearchKey (cột mặc định '') được tính lại một lần
        var withoutSearchKey = await db.Students.Where(s => s.SearchKey == "").ToListAsync(cancellationToken);
        foreach (var student in withoutSearchKey)
            student.RefreshSearchKey();
        if (withoutSearchKey.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
```

- [x] **Step 9: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f02-ho-so-hoc-vien-023|Category=BR-f02-ho-so-hoc-vien-024|Category=BR-f02-ho-so-hoc-vien-025)&Category!=web"`
Expected: `Passed: 3`, `Failed: 0`.
Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "Category!=web&Category!=manual"`
Expected: không Failed — toàn bộ 49 scenario `@api` của F02 + scenario F00.
Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Students|FullyQualifiedName~ClientDtoTests"`
Expected: `Passed: 33`.
Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed.

- [x] **Step 10: Commit**
```bash
git add backend/src backend/tests
git commit -F - <<'EOF'
feat(students): list and accent-insensitive search via SearchKey, no delete (BR-f02-023..025)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 7: [UI] Dashboard danh sách học viên — trang đích của web quản trị

Màn hình (tech-design §3 "Thiết kế giao diện (UI)"): **Dashboard (danh sách tạm)** — `/dashboard`, style bảng của page `Admin-Dashboard` trong `docs/duobexiuuu-coaching-design.html`.

**Files:**
- Create: `frontend/projects/shared/src/lib/ui/status-badge.component.ts`, `frontend/projects/shared/src/lib/ui/status-badge.component.spec.ts`
- Create: `frontend/projects/shared/src/lib/format/initials.ts`, `frontend/projects/shared/src/lib/format/initials.spec.ts`
- Modify: `frontend/projects/shared/src/lib/ui/icons.ts`, `frontend/projects/shared/src/public-api.ts`
- Create: `frontend/projects/admin/src/app/students/student.models.ts`, `student-status.ts`, `students.api.ts`, `student-list.component.ts`, `student-list.component.spec.ts`
- Modify: `frontend/projects/admin/src/app/app.routes.ts`, `frontend/projects/admin/src/app/shell/admin-shell.component.ts`; Create: `frontend/projects/admin/src/app/shell/admin-shell.component.spec.ts`
- Delete: `frontend/projects/admin/src/app/home/admin-home.component.ts`, `frontend/projects/admin/src/app/home/admin-home.component.spec.ts`
- Modify: `frontend/e2e/steps/login.steps.ts`, `frontend/e2e/steps/session.steps.ts`

**Interfaces:**
- Consumes: Task 6 — `GET /api/admin/students?q=` → `StudentListItemDto[]` (`id, fullName, email, coachingStatus, accountStatus, createdAt`); F00 — `GET /api/admin/profile` (`CoachProfileDto.displayName`), `AuthService.apiUrl(path)`, `APP_CONFIG`, `CLIENT_APP`, `ICONS`, `formatVnDate(date, withYear)`, `UserMenuComponent`.
- Produces:
  - shared: `type BadgeTone = 'ok' | 'warn' | 'paused' | 'ended'`; `<dbx-status-badge [tone] [dense]>` (`StatusBadgeComponent`); `initialsOf(name: string): string` ("Phạm Hoàng Quân" → "PQ", cùng luật với UserMenu); `ICONS.plus`, `ICONS.search`.
  - admin: `type CoachingStatus = 'Active' | 'Paused' | 'Ended'`, `type AccountStatus = 'PendingActivation' | 'Activated'`, `interface StudentListItem`; `COACHING_STATUS_LABEL`, `ACCOUNT_STATUS_LABEL`, `COACHING_STATUS_TONE`, `ACCOUNT_STATUS_TONE`; `StudentsApi.list(q: string): Observable<StudentListItem[]>`; route `dashboard` (lazy `StudentListComponent`), `''` và `trang-chu` → `dashboard`.

**Owns:** `@ADR-f02-ho-so-hoc-vien-008`

- [x] **Step 1: Viết spec cho `StatusBadge` và `initialsOf`**

`frontend/projects/shared/src/lib/ui/status-badge.component.spec.ts`:
```ts
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { StatusBadgeComponent } from './status-badge.component';

@Component({
  imports: [StatusBadgeComponent],
  template: `<dbx-status-badge tone="warn">Chờ kích hoạt</dbx-status-badge><dbx-status-badge tone="ok" [dense]="true">Đang coaching</dbx-status-badge>`,
})
class HostComponent {}

describe('StatusBadgeComponent', () => {
  it('renders the label with its tone and density', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const [warn, ok] = fixture.nativeElement.querySelectorAll('.badge') as NodeListOf<HTMLElement>;
    expect(warn.textContent?.trim()).toBe('Chờ kích hoạt');
    expect(warn.classList).toContain('warn');
    expect(warn.classList).not.toContain('dense');
    expect(ok.classList).toContain('ok');
    expect(ok.classList).toContain('dense');
  });
});
```

`frontend/projects/shared/src/lib/format/initials.spec.ts`:
```ts
import { describe, expect, it } from 'vitest';
import { initialsOf } from './initials';

describe('initialsOf', () => {
  it('takes first letters of the first and last word', () => {
    expect(initialsOf('Phạm Hoàng Quân')).toBe('PQ');
    expect(initialsOf('  lan ')).toBe('L');
    expect(initialsOf('')).toBe('?');
  });
});
```

- [x] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test shared --watch=false`
Expected: FAIL — `Cannot find module './status-badge.component'` và `'./initials'`.

- [x] **Step 3: shared — badge, initials, icon**

`frontend/projects/shared/src/lib/ui/status-badge.component.ts`:
```ts
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type BadgeTone = 'ok' | 'warn' | 'paused' | 'ended';

/** Chip trạng thái — style lấy từ prototype (Admin-Dashboard statusStyle; AdHoSo header = dense). */
@Component({
  selector: 'dbx-status-badge',
  template: `<span class="badge" [class]="tone()" [class.dense]="dense()"><ng-content /></span>`,
  styles: `
    .badge { display: inline-block; border-radius: 999px; padding: 4px 10px; font-size: 12px; font-weight: 600; white-space: nowrap; }
    .dense { padding: 3px 10px; }
    .ok { background: var(--okBg); color: var(--ok); }
    .ok.dense { color: var(--okInk); }
    .warn { background: var(--warnBg); color: var(--warn); }
    .paused { background: var(--pausedBg); color: var(--ink2); }
    .ended { background: var(--pausedBg); color: var(--muted); }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusBadgeComponent {
  readonly tone = input.required<BadgeTone>();
  readonly dense = input(false);
}
```

`frontend/projects/shared/src/lib/format/initials.ts`:
```ts
/** "Phạm Hoàng Quân" → "PQ": chữ đầu của từ đầu + từ cuối (cùng luật với UserMenu, theo prototype). */
export function initialsOf(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return '?';
  const picked = words.length === 1 ? [words[0]] : [words[0], words[words.length - 1]];
  return picked.map((w) => w[0]).join('').toUpperCase();
}
```

`frontend/projects/shared/src/lib/ui/icons.ts` — thay toàn bộ:
```ts
// SVG path lấy nguyên văn từ prototype (stroke currentColor, width 2)
export const ICONS = {
  home: '<rect x="3" y="3" width="7" height="9" rx="2"></rect><rect x="14" y="3" width="7" height="5" rx="2"></rect><rect x="14" y="12" width="7" height="9" rx="2"></rect><rect x="3" y="16" width="7" height="5" rx="2"></rect>',
  today: '<circle cx="12" cy="12" r="4"></circle><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"></path>',
  plus: '<path d="M12 5v14M5 12h14"></path>',
  search: '<circle cx="11" cy="11" r="7"></circle><path d="M20 20l-3.5-3.5"></path>',
} as const;
```

`frontend/projects/shared/src/public-api.ts` — thêm cuối file:
```ts
export * from './lib/format/initials';
export * from './lib/ui/status-badge.component';
```

- [x] **Step 4: Chạy — kỳ vọng PASS**

Run: `cd frontend && npx ng test shared --watch=false`
Expected: PASS, không test nào Failed.

- [x] **Step 5: Viết spec cho màn Dashboard**

`frontend/projects/admin/src/app/students/student-list.component.spec.ts`:
```ts
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { StudentListComponent } from './student-list.component';
import { StudentListItem } from './student.models';

const ROWS: StudentListItem[] = [
  { id: 'b', fullName: 'Phạm Hoàng Quân', email: 'hquanbilly@gmail.com', coachingStatus: 'Active', accountStatus: 'PendingActivation', createdAt: '2026-11-25T02:00:00+00:00' },
  { id: 'a', fullName: 'Nam', email: 'nam@gmail.com', coachingStatus: 'Ended', accountStatus: 'Activated', createdAt: '2026-11-24T02:00:00+00:00' },
];

describe('StudentListComponent', () => {
  afterEach(() => vi.useRealTimers());

  function setup() {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    const fixture = TestBed.createComponent(StudentListComponent);
    fixture.detectChanges();
    return { fixture, http: TestBed.inject(HttpTestingController), el: fixture.nativeElement as HTMLElement };
  }

  function type(el: HTMLElement, text: string) {
    const input = el.querySelector('input[type=search]') as HTMLInputElement;
    input.value = text;
    input.dispatchEvent(new Event('input'));
  }

  it('lists every student in server order with Vietnamese statuses and profile links', () => {
    const { fixture, http, el } = setup();
    http.expectOne('/api/admin/students').flush(ROWS);
    fixture.detectChanges();

    const rows = [...el.querySelectorAll('tbody tr')].map((tr) => tr.textContent ?? '');
    expect(rows).toHaveLength(2);
    expect(rows[0]).toContain('Phạm Hoàng Quân');
    expect(rows[0]).toContain('hquanbilly@gmail.com');
    expect(rows[0]).toContain('Đang coaching');
    expect(rows[0]).toContain('Chờ kích hoạt');
    expect(rows[1]).toContain('Đã kết thúc');
    expect(rows[1]).toContain('Đã kích hoạt');
    expect(el.querySelector('tbody a')?.getAttribute('href')).toBe('/hoc-vien/b');
    expect(el.querySelector('a.create')?.getAttribute('href')).toBe('/hoc-vien/moi');
    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Học viên');
  });

  it('searches with the typed keyword after typing pauses', () => {
    vi.useFakeTimers();
    const { fixture, http, el } = setup();
    http.expectOne('/api/admin/students').flush(ROWS);

    type(el, 'Quân');
    vi.advanceTimersByTime(250);
    http.expectOne((r) => r.url === '/api/admin/students' && r.params.get('q') === 'Quân').flush([ROWS[0]]);
    fixture.detectChanges();

    expect(el.querySelectorAll('tbody tr')).toHaveLength(1);
  });

  it('shows the empty search state', () => {
    vi.useFakeTimers();
    const { fixture, http, el } = setup();
    http.expectOne('/api/admin/students').flush(ROWS);

    type(el, 'zzz');
    vi.advanceTimersByTime(250);
    http.expectOne((r) => r.params.get('q') === 'zzz').flush([]);
    fixture.detectChanges();

    expect(el.querySelector('table')).toBeNull();
    expect(el.textContent).toContain('Không tìm thấy học viên phù hợp.');
  });

  it('shows an error line when the list cannot be loaded', () => {
    const { fixture, http, el } = setup();
    http.expectOne('/api/admin/students').flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(el.textContent).toContain('Không tải được danh sách học viên. Vui lòng tải lại trang.');
  });
});
```

- [x] **Step 6: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: FAIL — `Cannot find module './student-list.component'`.

- [x] **Step 7: Model, nhãn trạng thái, API, màn Dashboard**

`frontend/projects/admin/src/app/students/student.models.ts`:
```ts
export type CoachingStatus = 'Active' | 'Paused' | 'Ended';
export type AccountStatus = 'PendingActivation' | 'Activated';

/** GET /api/admin/students — mỗi dòng có tên, email, trạng thái coaching, trạng thái tài khoản. */
export interface StudentListItem {
  id: string;
  fullName: string;
  email: string;
  coachingStatus: CoachingStatus;
  accountStatus: AccountStatus;
  createdAt: string;
}
```

`frontend/projects/admin/src/app/students/student-status.ts`:
```ts
import { BadgeTone } from '@coaching/shared';
import { AccountStatus, CoachingStatus } from './student.models';

export const COACHING_STATUS_LABEL: Record<CoachingStatus, string> = {
  Active: 'Đang coaching',
  Paused: 'Tạm dừng',
  Ended: 'Đã kết thúc',
};

export const ACCOUNT_STATUS_LABEL: Record<AccountStatus, string> = {
  PendingActivation: 'Chờ kích hoạt',
  Activated: 'Đã kích hoạt',
};

export const COACHING_STATUS_TONE: Record<CoachingStatus, BadgeTone> = { Active: 'ok', Paused: 'paused', Ended: 'ended' };

export const ACCOUNT_STATUS_TONE: Record<AccountStatus, BadgeTone> = { PendingActivation: 'warn', Activated: 'ok' };
```

`frontend/projects/admin/src/app/students/students.api.ts`:
```ts
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AuthService } from '@coaching/shared';
import { Observable } from 'rxjs';
import { StudentListItem } from './student.models';

@Injectable({ providedIn: 'root' })
export class StudentsApi {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  private url(path = ''): string {
    return this.auth.apiUrl(`/api/admin/students${path}`);
  }

  list(q: string): Observable<StudentListItem[]> {
    const query = q.trim();
    return this.http.get<StudentListItem[]>(this.url(), query ? { params: { q: query } } : {});
  }
}
```

`frontend/projects/admin/src/app/students/student-list.component.ts`:
```ts
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { ICONS, StatusBadgeComponent, formatVnDate, initialsOf } from '@coaching/shared';
import { Subject, catchError, debounceTime, distinctUntilChanged, map, merge, of, switchMap, tap } from 'rxjs';
import {
  ACCOUNT_STATUS_LABEL, ACCOUNT_STATUS_TONE, COACHING_STATUS_LABEL, COACHING_STATUS_TONE,
} from './student-status';
import { StudentListItem } from './student.models';
import { StudentsApi } from './students.api';

/**
 * Dashboard (danh sách tạm) — @ADR-f02-ho-so-hoc-vien-008: trang đích sau đăng nhập.
 * Style bảng/ô tìm/nút theo page Admin-Dashboard của prototype; chưa có bộ lọc con (F08).
 */
@Component({
  selector: 'app-student-list',
  imports: [RouterLink, StatusBadgeComponent],
  template: `
    <div class="head">
      <div>
        <p class="date">{{ today }}</p>
        <h1>Học viên</h1>
      </div>
      <a class="create" routerLink="/hoc-vien/moi">
        <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
             stroke-linecap="round" [innerHTML]="plusIcon"></svg>Tạo hồ sơ học viên</a>
    </div>

    <div class="search">
      <svg aria-hidden="true" class="search-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           stroke-width="2" stroke-linecap="round" [innerHTML]="searchIcon"></svg>
      <label for="q" class="sr-only">Tìm học viên</label>
      <input id="q" type="search" placeholder="Tìm theo tên hoặc email" (input)="onSearch($any($event.target).value)" />
    </div>

    <div class="table-wrap">
      @if (rows(); as list) {
        @if (list.length > 0) {
          <table>
            <thead>
              <tr>
                <th scope="col" class="first">Họ tên</th>
                <th scope="col">Email</th>
                <th scope="col">Trạng thái coaching</th>
                <th scope="col" class="last">Tài khoản</th>
              </tr>
            </thead>
            <tbody>
              @for (s of list; track s.id) {
                <tr>
                  <td class="first">
                    <div class="who">
                      <span class="ini" aria-hidden="true">{{ initials(s.fullName) }}</span>
                      <a class="name" [routerLink]="['/hoc-vien', s.id]">{{ s.fullName }}</a>
                    </div>
                  </td>
                  <td class="email">{{ s.email }}</td>
                  <td><dbx-status-badge [tone]="coachingTone[s.coachingStatus]">{{ coachingLabel[s.coachingStatus] }}</dbx-status-badge></td>
                  <td class="last"><dbx-status-badge [tone]="accountTone[s.accountStatus]">{{ accountLabel[s.accountStatus] }}</dbx-status-badge></td>
                </tr>
              }
            </tbody>
          </table>
        } @else {
          <p class="empty">{{ query() ? 'Không tìm thấy học viên phù hợp.' : 'Chưa có hồ sơ học viên nào.' }}</p>
        }
      } @else if (failed()) {
        <p class="empty" role="alert">Không tải được danh sách học viên. Vui lòng tải lại trang.</p>
      }
    </div>
  `,
  styles: `
    :host { display: flex; flex-direction: column; gap: 24px; }
    .head { display: flex; flex-wrap: wrap; justify-content: space-between; align-items: flex-end; gap: 16px; }
    .date { margin: 0; font-size: 14px; color: var(--muted); }
    h1 { margin: 4px 0 0; font-family: var(--font-head); font-weight: 800; font-size: 48px; line-height: 1.05; }
    .create { height: 48px; padding: 0 22px; border-radius: 999px; border: 0; background: var(--primary); color: var(--onPrimary);
      font-family: var(--font-head); font-weight: 700; font-size: 20px; text-decoration: none; display: inline-flex; align-items: center; gap: 8px; }
    .create:hover { color: var(--onPrimary); }
    .search { position: relative; }
    .search-icon { color: var(--muted); position: absolute; left: 14px; top: 15px; }
    .sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
    input { width: 100%; box-sizing: border-box; height: 48px; border-radius: 14px; border: 1px solid var(--field);
      background: var(--surface); color: var(--ink); padding: 0 14px 0 42px; font: inherit; font-size: 15px; }
    .table-wrap { overflow-x: auto; border: 1px solid var(--line); border-radius: 14px; background: var(--tableBg); }
    table { width: 100%; min-width: 880px; border-collapse: collapse; font-size: 14px; }
    thead tr { text-align: left; color: var(--muted); font-size: 13px; }
    th { padding: 14px 12px; font-weight: 500; border-bottom: 1px solid var(--line); }
    td { padding: 14px 12px; border-bottom: 1px solid var(--rowLine); }
    th.first, td.first { padding-left: 18px; }
    th.last, td.last { padding-right: 18px; }
    tbody tr:hover { background: var(--rowHover); }
    .who { display: flex; align-items: center; gap: 12px; }
    .ini { width: 36px; height: 36px; flex-shrink: 0; border-radius: 50%; background: var(--soft); color: var(--primary);
      display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 13px; }
    .name { font-weight: 600; color: var(--ink); text-decoration: none; }
    .name:hover { color: var(--linkHover); }
    .email { color: var(--ink2); }
    .empty { margin: 0; padding: 32px; text-align: center; color: var(--muted); }
    @media (max-width: 879.98px) { h1 { font-size: 38px; } }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentListComponent {
  private readonly api = inject(StudentsApi);
  private readonly typed$ = new Subject<string>();
  private readonly sanitizer = inject(DomSanitizer);

  protected readonly today = formatVnDate(new Date(), true);
  protected readonly plusIcon = this.sanitizer.bypassSecurityTrustHtml(ICONS.plus);
  protected readonly searchIcon = this.sanitizer.bypassSecurityTrustHtml(ICONS.search);
  protected readonly coachingLabel = COACHING_STATUS_LABEL;
  protected readonly coachingTone = COACHING_STATUS_TONE;
  protected readonly accountLabel = ACCOUNT_STATUS_LABEL;
  protected readonly accountTone = ACCOUNT_STATUS_TONE;
  protected readonly initials = initialsOf;

  protected readonly rows = signal<StudentListItem[] | null>(null);
  protected readonly failed = signal(false);
  protected readonly query = signal('');

  constructor() {
    // Tải ngay lần đầu; khi gõ thì chờ 250ms ngừng gõ rồi mới tìm (từ khóa được server chuẩn hóa không dấu).
    merge(of(''), this.typed$.pipe(debounceTime(250)))
      .pipe(
        map((q) => q.trim()),
        distinctUntilChanged(),
        tap((q) => this.query.set(q)),
        switchMap((q) => this.api.list(q).pipe(catchError(() => of(null)))),
        takeUntilDestroyed(),
      )
      .subscribe((rows) => {
        this.rows.set(rows);
        this.failed.set(rows === null);
      });
  }

  protected onSearch(value: string): void {
    this.typed$.next(value);
  }
}
```

- [x] **Step 8: Route `dashboard` làm trang đích + mục rail "Dashboard"**

`frontend/projects/admin/src/app/app.routes.ts` — thay toàn bộ:
```ts
import { Routes } from '@angular/router';
import { LoginPageComponent, authGuard, guestGuard } from '@coaching/shared';
import { AdminShellComponent } from './shell/admin-shell.component';

// @ADR-f02-ho-so-hoc-vien-008: route tiếng Việt; dashboard (danh sách tạm) là trang đích thay trang-chu
export const routes: Routes = [
  { path: 'login', component: LoginPageComponent, canActivate: [guestGuard], title: 'Đăng nhập — Khu vực coach' },
  {
    path: '',
    component: AdminShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'trang-chu', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () => import('./students/student-list.component').then((m) => m.StudentListComponent),
        title: 'Học viên — Khu vực coach',
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
```

`frontend/projects/admin/src/app/shell/admin-shell.component.spec.ts` (tên coach ở rail lấy từ hệ thống — thay "Xin chào, {coach}" của trang chủ cũ; giữ step thủ công F00 "admin thấy tên coach của mình lấy từ hệ thống"):
```ts
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { describe, expect, it } from 'vitest';
import { AdminShellComponent } from './admin-shell.component';

describe('AdminShellComponent', () => {
  function setup() {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    const fixture = TestBed.createComponent(AdminShellComponent);
    fixture.detectChanges();
    return { fixture, http: TestBed.inject(HttpTestingController), el: fixture.nativeElement as HTMLElement };
  }

  it('shows the coach name from /api/admin/profile in the rail user menu', () => {
    const { fixture, http, el } = setup();
    http.expectOne('/api/admin/profile').flush({ id: '1', displayName: 'Coach Duobexiuuu', email: 'c@x.vn' });
    fixture.detectChanges();

    expect(el.querySelector('aside.rail dbx-user-menu')?.textContent).toContain('Coach Duobexiuuu');
    expect(el.querySelector('nav a')?.textContent?.trim()).toBe('Dashboard');
  });

  it('keeps the rail usable when the profile cannot be loaded', () => {
    const { fixture, http, el } = setup();
    http.expectOne('/api/admin/profile').flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(el.querySelector('aside.rail dbx-user-menu')).not.toBeNull();
  });
});
```

`frontend/projects/admin/src/app/shell/admin-shell.component.ts` — thay toàn bộ (rail chỉ có "Dashboard"; tên coach lấy từ `/api/admin/profile`, dự phòng tên trong token):
```ts
import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DomSanitizer } from '@angular/platform-browser';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService, ICONS, UserMenuComponent } from '@coaching/shared';
import { catchError, map, of } from 'rxjs';

interface CoachProfile { id: string; displayName: string; email: string; }

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, UserMenuComponent],
  template: `
    <div class="root">
      <aside class="rail">
        <div class="brand">
          <p class="sig">duobexiuuu</p>
          <p class="area muted">Khu vực coach</p>
        </div>
        <nav aria-label="Admin">
          <a routerLink="/dashboard" routerLinkActive #dash="routerLinkActive" [attr.aria-current]="dash.isActive ? 'page' : null">
            <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" stroke-linejoin="round" [innerHTML]="homeIcon"></svg>Dashboard</a>
        </nav>
        <div class="user-block">
          <dbx-user-menu [name]="coachName() ?? auth.user()?.fullName ?? ''" roleLabel="Quản trị viên" />
        </div>
      </aside>
      <main><router-outlet /></main>
    </div>
  `,
  styles: `
    .root { min-height: 100vh; box-sizing: border-box; background: var(--bg); color: var(--ink); font-family: var(--font-body); display: flex; flex-wrap: wrap; }
    .rail { flex: 1 1 220px; max-width: 260px; min-width: 220px; box-sizing: border-box; background: var(--chrome);
      border-right: 1px solid var(--line); padding: 24px 16px; display: flex; flex-direction: column; gap: 28px; }
    .brand { padding: 0 8px; }
    .sig { margin: 0; font-family: var(--font-sig); font-size: 34px; color: var(--sig); line-height: 1; }
    .area { margin: 4px 0 0; font-size: 13px; color: var(--muted); }
    nav { display: flex; flex-direction: column; gap: 2px; }
    nav a { text-decoration: none; display: flex; align-items: center; gap: 10px; padding: 11px 12px; border-radius: 12px; font-size: 15px; font-weight: 600; }
    .user-block { margin-top: auto; }
    main { flex: 999 1 560px; min-width: 0; box-sizing: border-box; padding: 32px clamp(16px, 3vw, 48px) 48px; display: flex; flex-direction: column; gap: 24px; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminShellComponent {
  private readonly sanitizer = inject(DomSanitizer);
  protected readonly auth = inject(AuthService);
  protected readonly homeIcon = this.sanitizer.bypassSecurityTrustHtml(ICONS.home);
  /** Tên coach lấy từ hệ thống (hồ sơ coach) — thay lời chào ở trang chủ cũ. */
  protected readonly coachName = toSignal(
    inject(HttpClient).get<CoachProfile>(this.auth.apiUrl('/api/admin/profile')).pipe(
      map((p) => p.displayName),
      catchError(() => of(null)),
    ),
    { initialValue: null },
  );
}
```

Xóa `frontend/projects/admin/src/app/home/admin-home.component.ts` và `frontend/projects/admin/src/app/home/admin-home.component.spec.ts` (trang chủ cũ được thay bằng dashboard; rail chỉ hiện mục đã có trang). Tên coach lấy từ hệ thống giờ nằm ở user menu của rail (spec ở trên).

- [x] **Step 9: Cập nhật e2e F00 theo trang đích mới**

`frontend/e2e/steps/login.steps.ts` — thay step `admin vào được khu vực coach` bằng:
```ts
Then('admin vào được khu vực coach', async function (this: E2EWorld) {
  await expect(this.page.getByText('Khu vực coach', { exact: true })).toBeVisible();
  await expect(this.page.getByRole('heading', { name: 'Học viên', exact: true })).toBeVisible();
});
```

`frontend/e2e/steps/session.steps.ts` — 6 chỗ:
1. Trong `loginAdmin`, thay dòng `await expect(page.getByRole('heading', { name: /^Xin chào,/ })).toBeVisible();` bằng:
```ts
  await expect(page.getByRole('heading', { name: 'Học viên', exact: true })).toBeVisible();
```
2. Thay toàn bộ 3 step của `// @BR-f00-codebase-010` (trang đích giờ gọi `/api/admin/students` thay `/api/admin/profile`):
```ts
// @BR-f00-codebase-010: server trả 401 thật cho request đầu (token không hợp lệ) → web tự gia hạn
Given('admin đang làm việc và lượt truy cập vừa hết hạn', async function (this: E2EWorld) {
  await loginAdmin(this);
  this.rejectedOnce = false;
  this.refreshStatus = null;
  await this.page.route('**/api/admin/students', async (route, request) => {
    if (!this.rejectedOnce) {
      this.rejectedOnce = true;
      await route.continue({ headers: { ...request.headers(), authorization: 'Bearer het-han' } });
    } else {
      await route.continue();
    }
  });
});

When('admin xem hồ sơ coach của mình', async function (this: E2EWorld) {
  const rejected = this.page.waitForResponse((r) => r.url().includes('/api/admin/students') && r.status() === 401);
  const refreshed = this.page.waitForResponse((r) => r.url().includes('/api/auth/refresh'));
  await Promise.all([rejected, refreshed, this.page.goto(`${ADMIN_URL}/dashboard`)]);
  this.refreshStatus = (await refreshed).status();
});

Then('admin thấy hồ sơ coach mà không phải đăng nhập lại', async function (this: E2EWorld) {
  expect(this.rejectedOnce, 'request đầu bị trả 401').toBe(true);
  expect(this.refreshStatus, '/api/auth/refresh trả 200').toBe(200);
  await expect(this.page.getByRole('heading', { name: 'Học viên', exact: true })).toBeVisible();
  await expect(this.page).not.toHaveURL(/\/login/);
});
```
3. Trong step `admin đang mở web quản trị ở hai tab của cùng trình duyệt`, thay 2 dòng thân sau `await loginAdmin(this);` bằng:
```ts
  const second = await this.openPage(`${ADMIN_URL}/dashboard`);
  await expect(second.getByRole('heading', { name: 'Học viên', exact: true })).toBeVisible();
```
4. Trong step `admin chưa đăng nhập và mở một trang con của khu vực coach`, đổi `this.expectedUrl = \`${ADMIN_URL}/trang-chu?tu=e2e\`;` thành:
```ts
  this.expectedUrl = `${ADMIN_URL}/dashboard?tu=e2e`;
```
5. Trong step `admin đăng nhập web quản trị thành công`, thay dòng heading bằng:
```ts
  await expect(this.page.getByRole('heading', { name: 'Học viên', exact: true })).toBeVisible();
```
6. Thay step `admin được đưa về trang chủ của web quản trị` bằng:
```ts
Then('admin được đưa về trang chủ của web quản trị', async function (this: E2EWorld) {
  await expect(this.page).toHaveURL(`${ADMIN_URL}/dashboard`);
});
```

- [x] **Step 10: Chạy — kỳ vọng PASS**

Run: `cd frontend && npm run test:ci`
Expected: PASS cả `shared`, `admin`, `client` (không còn spec `admin-home`).
Run: `cd frontend && npx ng build admin`
Expected: build thành công, có lazy chunk `student-list-component`.
Kiểm tay (`npx ng serve admin` + API local): đăng nhập → vào `/dashboard`; so với page `Admin-Dashboard` của prototype ở 390px và 1280px (rail, nút "Tạo hồ sơ học viên", ô tìm 48px radius 14, bảng: header 13px muted, cell 14px 12px, dòng `--rowLine`, hover `--rowHover`).

- [x] **Step 11: Commit**
```bash
git add frontend/projects/shared frontend/projects/admin frontend/e2e/steps/login.steps.ts frontend/e2e/steps/session.steps.ts
git commit -F - <<'EOF'
feat(admin): student dashboard list with search as landing page (ADR-f02-008)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 8: [UI] Tạo hồ sơ + Sửa hồ sơ — `StudentFormComponent` dùng chung

> Lưu ý: route `/hoc-vien/:id` đến Task 9 mới có. Ở Task 8, sau khi lưu, trình duyệt sẽ rơi vào route `**` rồi về `/dashboard`. Đây là hành vi đúng tạm thời, không phải lỗi.

Màn hình (tech-design §3 "Thiết kế giao diện (UI)"): **Tạo hồ sơ học viên** (`/hoc-vien/moi`, page `AdTaoHoSo`) và **Sửa hồ sơ** (`/hoc-vien/:id/sua`, dùng lại form `AdTaoHoSo`, điền sẵn, email chỉ xem, nút "Lưu thay đổi").

**Files:**
- Create: `frontend/projects/admin/src/app/students/registration-fields.ts`, `registration-fields.spec.ts`
- Create: `frontend/projects/admin/src/app/students/student-form.component.ts`, `student-form.component.html`, `student-form.component.scss`, `student-form.component.spec.ts`
- Modify: `frontend/projects/admin/src/app/students/student.models.ts`, `frontend/projects/admin/src/app/students/students.api.ts`
- Modify: `frontend/projects/admin/src/app/app.routes.ts`, `frontend/projects/admin/src/app/shell/admin-shell.component.ts`
- Modify: `frontend/projects/shared/src/lib/ui/icons.ts`

**Interfaces:**
- Consumes: Task 1–5 — `POST /api/admin/students` (`{ fullName, email, registration, internalNote? }` → 201 `StudentDetailDto`, 409 `student.email_taken`), `GET /api/admin/students/{id}`, `PUT /api/admin/students/{id}` (`{ fullName, registration, version }`, 409 `student.version_conflict`), `PUT /api/admin/students/{id}/internal-note` (`{ note, version }`); Task 7 — `StudentsApi`, `student.models.ts`, `ACCOUNT_STATUS_LABEL`, route config.
- Produces:
  - `student.models.ts`: `RegistrationValue`, `Registration`, `StatusChange`, `StudentDetail`, `CreateStudentBody`, `UpdateStudentBody`.
  - `StudentsApi.get(id)`, `create(body)`, `update(id, body)`, `updateNote(id, note, version)` → `Observable<StudentDetail>`.
  - `registration-fields.ts`: `FieldType`, `FieldDef`, `SectionDef`, `FORM_SECTIONS` (7 nhóm, 47 mục), `FORM_FIELDS`, `REGISTRATION_FIELDS` (44 mục của `registration`), `FIELD_LABEL`, `parseDecimal`, `parseInteger`, `toInputText`, `toPayload`, `toFormValues`, `FormValues`, `FormPayload`.
  - `StudentFormComponent` (input `mode: 'create' | 'edit'` từ route data, `id` từ route param), `DRAFT_KEY = 'dbx.admin.student-draft'`.
  - Route `hoc-vien/moi`, `hoc-vien/:id/sua`; rail thêm "Tạo hồ sơ học viên"; `ICONS.userPlus`.

**Owns:** —

- [x] **Step 1: Viết spec cho danh mục mục form và chuyển đổi số**

`frontend/projects/admin/src/app/students/registration-fields.spec.ts`:
```ts
import { describe, expect, it } from 'vitest';
import {
  FORM_SECTIONS, REGISTRATION_FIELDS, parseDecimal, parseInteger, toFormValues, toPayload,
} from './registration-fields';
import { StudentDetail } from './student.models';

describe('registration fields', () => {
  it('follows the prototype groups plus Lịch rảnh and Dụng cụ, without Gán giáo án', () => {
    expect(FORM_SECTIONS.map((s) => s.title)).toEqual([
      'Tài khoản và gói', 'Số đo cơ thể hiện tại', 'Tập luyện hiện tại', 'Ăn uống, sinh hoạt và môi trường sống',
      'Chương trình tập sắp tới', 'Kỳ vọng và cảm nhận', 'Chỉ coach thấy',
    ]);
    const training = FORM_SECTIONS[2].fields.map((f) => f.label);
    expect(training).toContain('Lịch rảnh');
    expect(training).toContain('Dụng cụ');
    expect(FORM_SECTIONS.flatMap((s) => s.fields).some((f) => f.label === 'Gán giáo án')).toBe(false);
    expect(REGISTRATION_FIELDS).toHaveLength(44);
    expect(FORM_SECTIONS.flatMap((s) => s.fields).filter((f) => f.required)).toHaveLength(30);
  });

  // @BR-f02-ho-so-hoc-vien-008: dấu phẩy thập phân
  it('parses decimal comma and grouped integers', () => {
    expect(parseDecimal('72,5')).toBe(72.5);
    expect(parseDecimal('250.1')).toBe(250.1);
    expect(parseDecimal('abc')).toBeNull();
    expect(parseInteger('3.500.000')).toBe(3500000);
    expect(parseInteger('28')).toBe(28);
    expect(parseInteger('2,5')).toBeNull();
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

  it('shows stored numbers with a decimal comma', () => {
    const values = toFormValues({ fullName: 'Lan', email: 'lan@gmail.com', internalNote: null,
      registration: { weightKg: 72.5, age: 28, transferDate: '2026-11-25' } } as unknown as StudentDetail);
    expect(values['weightKg']).toBe('72,5');
    expect(values['age']).toBe('28');
    expect(values['transferDate']).toBe('2026-11-25');
    expect(values['internalNote']).toBe('');
  });
});
```

- [x] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: FAIL — `Cannot find module './registration-fields'`.

- [x] **Step 3: Model chi tiết, API, danh mục mục form**

`frontend/projects/admin/src/app/students/student.models.ts` — thêm cuối file:
```ts

export type RegistrationValue = string | number | null;

/** `registration` của API: object phẳng theo mã mục (pack, price, transferDate, weightKg, …). */
export type Registration = Record<string, RegistrationValue>;

export interface StatusChange {
  from: CoachingStatus;
  to: CoachingStatus;
  actor: string;
  at: string;
  reason: string;
}

/** GET /api/admin/students/{id}. */
export interface StudentDetail {
  id: string;
  coachId: string;
  fullName: string;
  email: string;
  accountStatus: AccountStatus;
  coachingStatus: CoachingStatus;
  createdAt: string;
  registration: Registration;
  missingRequired: string[];
  updatedAt: string | null;
  updatedBy: string | null;
  version: string;
  internalNote: string | null;
  internalNoteUpdatedAt: string | null;
  statusHistory: StatusChange[];
}

export interface CreateStudentBody {
  fullName: string;
  email: string;
  registration: Registration;
  /** Tùy chọn — server lưu cùng transaction tạo hồ sơ. */
  internalNote?: string;
}

export interface UpdateStudentBody {
  fullName: string;
  registration: Registration;
  version: string;
}
```

`frontend/projects/admin/src/app/students/students.api.ts` — thay toàn bộ:
```ts
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AuthService } from '@coaching/shared';
import { Observable } from 'rxjs';
import { CreateStudentBody, StudentDetail, StudentListItem, UpdateStudentBody } from './student.models';

@Injectable({ providedIn: 'root' })
export class StudentsApi {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  private url(path = ''): string {
    return this.auth.apiUrl(`/api/admin/students${path}`);
  }

  list(q: string): Observable<StudentListItem[]> {
    const query = q.trim();
    return this.http.get<StudentListItem[]>(this.url(), query ? { params: { q: query } } : {});
  }

  get(id: string): Observable<StudentDetail> {
    return this.http.get<StudentDetail>(this.url(`/${id}`));
  }

  create(body: CreateStudentBody): Observable<StudentDetail> {
    return this.http.post<StudentDetail>(this.url(), body);
  }

  update(id: string, body: UpdateStudentBody): Observable<StudentDetail> {
    return this.http.put<StudentDetail>(this.url(`/${id}`), body);
  }

  updateNote(id: string, note: string, version: string): Observable<StudentDetail> {
    return this.http.put<StudentDetail>(this.url(`/${id}/internal-note`), { note, version });
  }
}
```

`frontend/projects/admin/src/app/students/registration-fields.ts`:
```ts
import { Registration, RegistrationValue, StudentDetail } from './student.models';

export type FieldType = 'text' | 'decimal' | 'integer' | 'date' | 'area' | 'select';

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
 * Form đăng ký theo page AdTaoHoSo (thứ tự, nhãn, placeholder, đơn vị nguyên văn prototype) + "Lịch rảnh", "Dụng cụ"
 * (không bắt buộc) trong "Tập luyện hiện tại"; bỏ "Gán giáo án" (F05). Danh sách lựa chọn là bản sao để hiển thị —
 * server kiểm lại (RegistrationOptions).
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
      { key: 'bodyFatPercent', label: 'Mỡ cơ thể', type: 'decimal', required: false, unit: '%' },
      { key: 'bodyFatMethod', label: 'Cách đo mỡ', type: 'select', required: false,
        options: ['Ước lượng bằng mắt', 'Cân điện tử', 'InBody', 'Thước kẹp mỡ', 'DEXA', 'Khác'] },
    ],
  },
  {
    id: 'tap', title: 'Tập luyện hiện tại',
    fields: [
      { key: 'trainingYears', label: 'Số năm tập luyện', type: 'select', required: true,
        options: ['Chưa tập', 'Dưới 6 tháng', '6–12 tháng', '1–2 năm', '2–3 năm', '3–5 năm', 'Trên 5 năm'] },
      { key: 'weeklyTrainingTime', label: 'Thời gian dành cho tập mỗi tuần', type: 'text', required: true, placeholder: 'VD: 5 buổi, mỗi buổi 75 phút' },
      { key: 'weightBeforeLiftingKg', label: 'Cân nặng trước khi tập tạ', type: 'decimal', required: true, unit: 'kg' },
      { key: 'heightThenCm', label: 'Chiều cao khi đó', type: 'decimal', required: true, unit: 'cm' },
      { key: 'trainingGoal', label: 'Mục tiêu tập luyện', type: 'area', required: true, wide: true, rows: 2 },
      { key: 'currentTraining', label: 'Chế độ tập hiện tại', type: 'area', required: false, wide: true, rows: 4, placeholder: 'Lịch chia nhóm cơ, bài, số set, rep…' },
      { key: 'medicalHistory', label: 'Tiền sử bệnh lý và chấn thương', type: 'area', required: true, wide: true, rows: 2, placeholder: 'Ghi "Không" nếu không có' },
      { key: 'availability', label: 'Lịch rảnh', type: 'area', required: false, wide: true, rows: 2, placeholder: 'VD: T3–T7 buổi tối, CN buổi sáng' },
      { key: 'equipment', label: 'Dụng cụ', type: 'area', required: false, wide: true, rows: 2, placeholder: 'VD: Phòng gym đầy đủ' },
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
      { key: 'photoFrontUrl', label: 'Ảnh mặt trước (link)', type: 'text', required: true, placeholder: 'Link Google Drive' },
      { key: 'photoBackUrl', label: 'Ảnh mặt sau (link)', type: 'text', required: true, placeholder: 'Link Google Drive' },
      { key: 'photoLeftUrl', label: 'Ảnh bên trái (link)', type: 'text', required: true, placeholder: 'Link Google Drive' },
      { key: 'photoRightUrl', label: 'Ảnh bên phải (link)', type: 'text', required: true, placeholder: 'Link Google Drive' },
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

/** 44 mục của `registration` (bỏ họ tên, email, ghi chú nội bộ). */
export const REGISTRATION_FIELDS: readonly FieldDef[] = FORM_FIELDS.filter((f) => !NOT_REGISTRATION.has(f.key));

/** Nhãn hiển thị theo mã mục (dùng cho "Mục bắt buộc còn thiếu"). */
export const FIELD_LABEL: Readonly<Record<string, string>> = Object.fromEntries(
  FORM_FIELDS.map((f) => [f.key, f.label.replace(/ \(link\)$/, '')]),
);

export type FormValues = Record<string, string>;

export interface FormPayload {
  fullName: string;
  email: string;
  registration: Registration;
  internalNote: string;
}

/** "72,5" → 72.5 (dấu phẩy thập phân). Không phải số → null. */
export function parseDecimal(text: string): number | null {
  const normalized = text.replace(/\s/g, '').replace(',', '.');
  return /^-?\d+(\.\d+)?$/.test(normalized) ? Number(normalized) : null;
}

/** "3.500.000" → 3500000 (dấu chấm phân nhóm). Không phải số nguyên → null. */
export function parseInteger(text: string): number | null {
  const normalized = text.replace(/[\s.]/g, '');
  return /^-?\d+$/.test(normalized) ? Number(normalized) : null;
}

export function toInputText(value: RegistrationValue | undefined, type: FieldType): string {
  if (value === null || value === undefined) return '';
  if (type === 'decimal' && typeof value === 'number') return String(value).replace('.', ',');
  return String(value);
}

/** Giá trị ô nhập → body API. Ô số sai định dạng → lỗi "Nhập số hợp lệ" (không gửi). Kiểm khoảng do server. */
export function toPayload(values: FormValues): { payload: FormPayload; errors: Record<string, string> } {
  const errors: Record<string, string> = {};
  const registration: Registration = {};
  for (const field of REGISTRATION_FIELDS) {
    const raw = (values[field.key] ?? '').trim();
    if (!raw) {
      registration[field.key] = null;
    } else if (field.type === 'decimal' || field.type === 'integer') {
      const parsed = field.type === 'decimal' ? parseDecimal(raw) : parseInteger(raw);
      if (parsed === null) errors[field.key] = 'Nhập số hợp lệ';
      registration[field.key] = parsed;
    } else {
      registration[field.key] = raw;
    }
  }
  return {
    payload: {
      fullName: (values['fullName'] ?? '').trim(),
      email: (values['email'] ?? '').trim(),
      registration,
      internalNote: (values['internalNote'] ?? '').trim(),
    },
    errors,
  };
}

export function toFormValues(detail: StudentDetail): FormValues {
  const values: FormValues = {
    fullName: detail.fullName,
    email: detail.email,
    internalNote: detail.internalNote ?? '',
  };
  for (const field of REGISTRATION_FIELDS) values[field.key] = toInputText(detail.registration[field.key], field.type);
  return values;
}
```

- [x] **Step 4: Chạy — kỳ vọng PASS**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: PASS (`registration fields` 4 test + spec của Task 7).

- [x] **Step 5: Viết spec cho form tạo/sửa**

`frontend/projects/admin/src/app/students/student-form.component.spec.ts`:
```ts
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { DRAFT_KEY, StudentFormComponent } from './student-form.component';
import { StudentDetail } from './student.models';

const settle = () => new Promise((resolve) => setTimeout(resolve, 0));

function detail(overrides: Partial<StudentDetail> = {}): StudentDetail {
  return {
    id: 's1', coachId: 'c1', fullName: 'Lan', email: 'lan@gmail.com', accountStatus: 'PendingActivation', coachingStatus: 'Active',
    createdAt: '2026-11-25T02:00:00+00:00', registration: { weightKg: 72.5, sessionsPerWeek: '4 buổi' }, missingRequired: [],
    updatedAt: null, updatedBy: null, version: 'AAAAAAAAB9E=', internalNote: null, internalNoteUpdatedAt: null, statusHistory: [],
    ...overrides,
  };
}

describe('StudentFormComponent', () => {
  beforeEach(() => localStorage.clear());

  function setup(mode: 'create' | 'edit', id?: string) {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(StudentFormComponent);
    fixture.componentRef.setInput('mode', mode);
    if (id) fixture.componentRef.setInput('id', id);
    fixture.detectChanges();
    return { fixture, http: TestBed.inject(HttpTestingController), el: fixture.nativeElement as HTMLElement, navigate };
  }

  function fill(el: HTMLElement, key: string, value: string) {
    const control = el.querySelector(`#hs-${key}`) as HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement;
    control.value = value;
    control.dispatchEvent(new Event(control instanceof HTMLSelectElement ? 'change' : 'input'));
  }

  const submit = (el: HTMLElement) => el.querySelector('form')!.dispatchEvent(new Event('submit'));

  // @BR-f02-ho-so-hoc-vien-003 + NFR-f02-ho-so-hoc-vien-004: lưu thành công → xóa bản nháp, mở hồ sơ
  it('saves a profile with only name and email, clears the draft and opens the profile', async () => {
    const { http, el, navigate } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    expect(localStorage.getItem(DRAFT_KEY)).toContain('lan@gmail.com');

    submit(el);
    const request = http.expectOne('/api/admin/students');
    expect(request.request.method).toBe('POST');
    expect(request.request.body.fullName).toBe('Lan');
    expect(request.request.body.email).toBe('lan@gmail.com');
    expect(request.request.body.registration['weightKg']).toBeNull();
    request.flush(detail());
    await settle();

    expect(localStorage.getItem(DRAFT_KEY)).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/hoc-vien/s1');
  });

  // @BR-f02-ho-so-hoc-vien-014: ghi chú nội bộ gửi trong cùng POST tạo hồ sơ, không gọi endpoint ghi chú
  it('sends the internal note in the same create request', async () => {
    const { http, el, navigate } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    fill(el, 'internalNote', ' Tránh lunge sâu vì gối phải ');

    submit(el);
    const request = http.expectOne('/api/admin/students');
    expect(request.request.body.internalNote).toBe('Tránh lunge sâu vì gối phải');
    request.flush(detail({ internalNote: 'Tránh lunge sâu vì gối phải' }));
    await settle();

    http.expectNone('/api/admin/students/s1/internal-note');
    expect(navigate).toHaveBeenCalledWith('/hoc-vien/s1');
  });

  // NFR-f02-ho-so-hoc-vien-004: bản nháp chỉ nằm trên trình duyệt của coach
  it('restores the draft kept on this browser', () => {
    localStorage.setItem(DRAFT_KEY, JSON.stringify({ fullName: 'Quân', weightKg: '72,5' }));
    const { el } = setup('create');

    expect((el.querySelector('#hs-fullName') as HTMLInputElement).value).toBe('Quân');
    expect((el.querySelector('#hs-weightKg') as HTMLInputElement).value).toBe('72,5');
    expect(el.textContent).toContain('2/30 mục bắt buộc');
  });

  it('"Xoá trắng hồ sơ" clears the form and the draft', () => {
    localStorage.setItem(DRAFT_KEY, JSON.stringify({ fullName: 'Quân' }));
    const { fixture, el } = setup('create');

    (el.querySelector('button.clear') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(localStorage.getItem(DRAFT_KEY)).toBeNull();
    expect((el.querySelector('#hs-fullName') as HTMLInputElement).value).toBe('');
  });

  // @BR-f02-ho-so-hoc-vien-008: "72,5" gửi 72.5; "3.500.000" gửi 3500000
  it('sends decimal-comma numbers as JSON numbers', () => {
    const { http, el } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    fill(el, 'weightKg', '72,5');
    fill(el, 'price', '3.500.000');

    submit(el);

    const body = http.expectOne('/api/admin/students').request.body;
    expect(body.registration['weightKg']).toBe(72.5);
    expect(body.registration['price']).toBe(3500000);
  });

  // @BR-f02-ho-so-hoc-vien-008 (seed "abc"): ô số không hợp lệ → không gửi, đánh dấu ô
  it('does not send a non-numeric weight and marks the field', () => {
    const { fixture, http, el } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    fill(el, 'weightKg', 'abc');

    submit(el);
    fixture.detectChanges();

    http.expectNone('/api/admin/students');
    expect(el.querySelector('#hs-weightKg')?.getAttribute('aria-invalid')).toBe('true');
    expect(el.textContent).toContain('Nhập số hợp lệ');
  });

  // @BR-f02-ho-so-hoc-vien-009 (seed giá trị ngoài danh sách): select chỉ có lựa chọn cho trước
  it('offers only the allowed options for sessions per week', () => {
    const { el } = setup('create');
    const options = [...el.querySelectorAll('#hs-sessionsPerWeek option')].map((o) => o.textContent?.trim());
    expect(options).toEqual(['Chọn…', '2 buổi', '3 buổi', '4 buổi', '5 buổi', '6 buổi', '7 buổi']);
  });

  // @BR-f02-ho-so-hoc-vien-001: server báo email đã dùng → hiện thông báo, giữ bản nháp
  it('shows the server message when the email is already used', async () => {
    const { fixture, http, el, navigate } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'an@mail.com');

    submit(el);
    http.expectOne('/api/admin/students').flush(
      { title: 'Email này đã được dùng cho một tài khoản khác.', code: 'student.email_taken' },
      { status: 409, statusText: 'Conflict' });
    await settle();
    fixture.detectChanges();

    expect(el.textContent).toContain('Email này đã được dùng cho một tài khoản khác.');
    expect(navigate).not.toHaveBeenCalled();
    expect(localStorage.getItem(DRAFT_KEY)).toContain('an@mail.com');
  });

  // @BR-f02-ho-so-hoc-vien-012 @BR-f02-ho-so-hoc-vien-015: sửa gửi version, email chỉ xem, 409 → tải lại
  it('edits with the version, keeps email read-only and offers reload on conflict', async () => {
    const { fixture, http, el } = setup('edit', 's1');
    http.expectOne('/api/admin/students/s1').flush(detail());
    await settle();
    fixture.detectChanges();

    expect((el.querySelector('#hs-weightKg') as HTMLInputElement).value).toBe('72,5');
    expect((el.querySelector('#hs-email') as HTMLInputElement).readOnly).toBe(true);
    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Sửa hồ sơ');

    fill(el, 'weightKg', '70');
    expect(localStorage.getItem(DRAFT_KEY)).toBeNull(); // bản nháp chỉ ở chế độ tạo
    submit(el);
    const put = http.expectOne('/api/admin/students/s1');
    expect(put.request.method).toBe('PUT');
    expect(put.request.body.version).toBe('AAAAAAAAB9E=');
    expect(put.request.body.registration['weightKg']).toBe(70);
    expect(put.request.body.email).toBeUndefined();
    put.flush({ title: 'Hồ sơ vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.', code: 'student.version_conflict' },
      { status: 409, statusText: 'Conflict' });
    await settle();
    fixture.detectChanges();

    expect(el.textContent).toContain('Hồ sơ vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.');
    (el.querySelector('button.reload') as HTMLButtonElement).click();
    http.expectOne('/api/admin/students/s1').flush(detail({ version: 'AAAAAAAAB9I=' }));
    await settle();
    fixture.detectChanges();
    expect(el.querySelector('button.reload')).toBeNull();
  });
});
```

- [x] **Step 6: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: FAIL — `Cannot find module './student-form.component'`.

- [x] **Step 7: `StudentFormComponent`**

`frontend/projects/admin/src/app/students/student-form.component.ts`:
```ts
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { FORM_SECTIONS, FieldDef, FormPayload, FormValues, toFormValues, toPayload } from './registration-fields';
import { ACCOUNT_STATUS_LABEL } from './student-status';
import { StudentDetail } from './student.models';
import { StudentsApi } from './students.api';

/** @ADR-f02-ho-so-hoc-vien-008 / NFR-f02-ho-so-hoc-vien-004: bản nháp chỉ ở chế độ tạo, chỉ trên trình duyệt này. */
export const DRAFT_KEY = 'dbx.admin.student-draft';

/**
 * @ADR-f02-ho-so-hoc-vien-008: một form cho "Tạo hồ sơ học viên" (hoc-vien/moi) và "Sửa hồ sơ" (hoc-vien/:id/sua).
 * Mục bắt buộc còn thiếu KHÔNG chặn lưu (@BR-f02-ho-so-hoc-vien-004); thanh "x/y mục bắt buộc" chỉ để theo dõi.
 */
@Component({
  selector: 'app-student-form',
  imports: [RouterLink],
  templateUrl: './student-form.component.html',
  styleUrl: './student-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentFormComponent implements OnInit {
  /** Route data: 'create' | 'edit'. */
  readonly mode = input<'create' | 'edit'>('create');
  /** Route param `:id` (chế độ sửa). */
  readonly id = input<string>();

  private readonly api = inject(StudentsApi);
  private readonly router = inject(Router);

  protected readonly values = signal<FormValues>({});
  protected readonly errors = signal<Record<string, string>>({});
  protected readonly message = signal<string | null>(null);
  protected readonly conflict = signal(false);
  protected readonly saving = signal(false);
  protected readonly loaded = signal<StudentDetail | null>(null);
  protected readonly loadFailed = signal(false);
  protected readonly isEdit = computed(() => this.mode() === 'edit');
  protected readonly accountLabel = ACCOUNT_STATUS_LABEL;

  protected readonly progress = computed(() => {
    const values = this.values();
    const filled = (f: FieldDef) => (values[f.key] ?? '').trim() !== '';
    let requiredAll = 0;
    let requiredDone = 0;
    let all = 0;
    let done = 0;
    const sections = FORM_SECTIONS.map((section) => {
      const required = section.fields.filter((f) => f.required);
      const requiredFilled = required.filter(filled).length;
      const anyFilled = section.fields.filter(filled).length;
      requiredAll += required.length;
      requiredDone += requiredFilled;
      all += section.fields.length;
      done += anyFilled;
      return {
        ...section,
        full: required.length ? requiredFilled === required.length : anyFilled > 0,
        count: required.length ? `${requiredFilled}/${required.length} bắt buộc` : `${anyFilled}/${section.fields.length} mục`,
      };
    });
    return {
      sections,
      totalText: `${requiredDone}/${requiredAll} mục bắt buộc, ${done}/${all} mục tổng`,
      percent: Math.round((100 * requiredDone) / requiredAll),
      complete: requiredDone === requiredAll,
    };
  });

  ngOnInit(): void {
    if (this.isEdit()) void this.load();
    else this.values.set(readDraft());
  }

  protected setValue(key: string, value: string): void {
    const next = { ...this.values(), [key]: value };
    this.values.set(next);
    if (this.errors()[key]) {
      const rest = { ...this.errors() };
      delete rest[key];
      this.errors.set(rest);
    }
    this.message.set(null);
    if (!this.isEdit()) writeDraft(next);
  }

  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    const { payload, errors } = toPayload(this.values());
    this.errors.set(errors);
    if (Object.keys(errors).length > 0) {
      this.message.set('Có ô số liệu chưa đúng. Sửa các ô được đánh dấu đỏ.');
      return;
    }

    this.saving.set(true);
    this.message.set(null);
    this.conflict.set(false);
    try {
      const saved = this.isEdit() ? await this.saveEdit(payload) : await this.saveCreate(payload);
      await this.router.navigateByUrl(`/hoc-vien/${saved.id}`);
    } catch (error) {
      // @BR-f02-ho-so-hoc-vien-015: 409 khi sửa → yêu cầu tải lại; lỗi khác hiện câu báo của server (ProblemDetails.title)
      if (this.isEdit() && error instanceof HttpErrorResponse && error.status === 409) this.conflict.set(true);
      else this.message.set(problemTitle(error));
    } finally {
      this.saving.set(false);
    }
  }

  protected async load(): Promise<void> {
    this.conflict.set(false);
    this.message.set(null);
    try {
      const detail = await firstValueFrom(this.api.get(this.id()!));
      this.loaded.set(detail);
      this.values.set(toFormValues(detail));
      this.errors.set({});
      this.loadFailed.set(false);
    } catch {
      this.loadFailed.set(true);
    }
  }

  protected clear(): void {
    clearDraft();
    this.values.set({});
    this.errors.set({});
    this.message.set(null);
  }

  private async saveCreate(payload: FormPayload): Promise<StudentDetail> {
    // Ghi chú nội bộ đi cùng request tạo hồ sơ (một transaction phía server), không gọi thêm endpoint ghi chú
    const created = await firstValueFrom(this.api.create({
      fullName: payload.fullName, email: payload.email, registration: payload.registration,
      ...(payload.internalNote ? { internalNote: payload.internalNote } : {}),
    }));
    clearDraft(); // NFR-f02-ho-so-hoc-vien-004: lưu thành công → xóa bản nháp
    return created;
  }

  private async saveEdit(payload: FormPayload): Promise<StudentDetail> {
    const current = this.loaded()!;
    const saved = await firstValueFrom(this.api.update(current.id, {
      fullName: payload.fullName, registration: payload.registration, version: current.version,
    }));
    if ((payload.internalNote || null) === (current.internalNote ?? null)) return saved;
    return firstValueFrom(this.api.updateNote(saved.id, payload.internalNote, saved.version));
  }
}

function readDraft(): FormValues {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(DRAFT_KEY) ?? '{}');
    return parsed && typeof parsed === 'object' ? (parsed as FormValues) : {};
  } catch {
    return {};
  }
}

function writeDraft(values: FormValues): void {
  try {
    localStorage.setItem(DRAFT_KEY, JSON.stringify(values));
  } catch {
    /* bộ nhớ bị chặn/đầy: bỏ qua, form vẫn dùng được */
  }
}

function clearDraft(): void {
  try {
    localStorage.removeItem(DRAFT_KEY);
  } catch {
    /* bỏ qua */
  }
}

function problemTitle(error: unknown): string {
  if (error instanceof HttpErrorResponse && typeof error.error?.title === 'string') return error.error.title;
  return 'Không lưu được hồ sơ. Vui lòng thử lại.';
}
```

`frontend/projects/admin/src/app/students/student-form.component.html`:
```html
<nav aria-label="Đường dẫn" class="crumbs">
  <a routerLink="/dashboard">Học viên</a><span aria-hidden="true">/</span>
  <span class="here">{{ isEdit() ? 'Sửa hồ sơ' : 'Tạo hồ sơ' }}</span>
</nav>
<h1>{{ isEdit() ? 'Sửa hồ sơ' : 'Tạo hồ sơ học viên' }}</h1>

@if (loadFailed()) {
  <p class="msg" role="status">Không tải được hồ sơ. Vui lòng tải lại trang.</p>
} @else {
  <div class="layout">
    <form novalidate (submit)="submit($event)">
      <p class="intro">Điền theo câu trả lời của học viên trong form đăng ký. Mục có dấu <span class="star">*</span> là bắt buộc trước khi gửi tài khoản.@if (!isEdit()) { Hồ sơ tự lưu trên máy này.}</p>

      @for (sec of progress().sections; track sec.id) {
        <section class="card" [attr.aria-labelledby]="'sec-' + sec.id">
          <div class="sec-head">
            <h2 [id]="'sec-' + sec.id">{{ sec.title }}</h2>
            <span class="count" [class.full]="sec.full">{{ sec.count }}</span>
          </div>
          @if (sec.hint) {
            <p class="hint">{{ sec.hint }}</p>
          }
          <div class="grid">
            @for (f of sec.fields; track f.key) {
              <div class="field" [class.wide]="f.wide">
                <label [for]="'hs-' + f.key">{{ f.label }}@if (f.required) {<span aria-hidden="true" class="req"> *</span>}</label>
                @switch (f.type) {
                  @case ('date') {
                    <input class="ctl" type="date" [id]="'hs-' + f.key" [value]="values()[f.key] ?? ''"
                           [class.err]="!!errors()[f.key]" [attr.aria-invalid]="!!errors()[f.key]"
                           (input)="setValue(f.key, $any($event.target).value)" />
                  }
                  @case ('area') {
                    <textarea class="ctl" [id]="'hs-' + f.key" [rows]="f.rows ?? 3" [placeholder]="f.placeholder ?? ''"
                              [value]="values()[f.key] ?? ''" [class.err]="!!errors()[f.key]" [attr.aria-invalid]="!!errors()[f.key]"
                              (input)="setValue(f.key, $any($event.target).value)"></textarea>
                  }
                  @case ('select') {
                    <select class="ctl" [id]="'hs-' + f.key" [class.err]="!!errors()[f.key]" [attr.aria-invalid]="!!errors()[f.key]"
                            (change)="setValue(f.key, $any($event.target).value)">
                      <option value="">Chọn…</option>
                      @for (o of f.options ?? []; track o) {
                        <option [value]="o" [selected]="values()[f.key] === o">{{ o }}</option>
                      }
                    </select>
                  }
                  @default {
                    <div class="with-unit">
                      <input class="ctl" type="text" [id]="'hs-' + f.key" [value]="values()[f.key] ?? ''" [placeholder]="f.placeholder ?? ''"
                             [attr.inputmode]="f.type === 'decimal' || f.type === 'integer' ? 'decimal' : null"
                             [readOnly]="isEdit() && f.key === 'email'"
                             [class.err]="!!errors()[f.key]" [attr.aria-invalid]="!!errors()[f.key]"
                             (input)="setValue(f.key, $any($event.target).value)" />
                      @if (f.unit) {
                        <span class="unit">{{ f.unit }}</span>
                      }
                    </div>
                  }
                }
                @if (f.note) {
                  <span class="note">{{ f.note }}</span>
                }
                @if (errors()[f.key]; as e) {
                  <span class="field-err">{{ e }}</span>
                }
              </div>
            }
          </div>
        </section>
      }

      @if (conflict()) {
        <p class="msg" role="status">Hồ sơ vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.
          <button type="button" class="reload" (click)="load()">Tải lại</button></p>
      }
      @if (message(); as m) {
        <p class="msg" role="status">{{ m }}</p>
      }
      <div class="actions">
        <button type="submit" class="primary" [disabled]="saving()">{{ isEdit() ? 'Lưu thay đổi' : 'Lưu hồ sơ' }}</button>
        @if (!isEdit()) {
          <button type="button" class="clear" (click)="clear()">Xoá trắng hồ sơ</button>
        }
      </div>
    </form>

    <aside>
      <section class="side">
        <h2 class="side-title">Hồ sơ đã điền</h2>
        <div class="bar" aria-hidden="true">
          <div class="fill" [class.complete]="progress().complete" [style.width.%]="progress().percent"></div>
        </div>
        <p class="total">{{ progress().totalText }}</p>
        <ul>
          @for (sec of progress().sections; track sec.id) {
            <li><span>{{ sec.title }}</span><span class="count" [class.full]="sec.full">{{ sec.count }}</span></li>
          }
        </ul>
      </section>
      <section class="side account">
        <h2>Trạng thái tài khoản</h2>
        @if (loaded(); as d) {
          <span class="acct" [class.pending]="d.accountStatus === 'PendingActivation'">{{ accountLabel[d.accountStatus] }}</span>
        } @else {
          <span class="acct">Chưa tạo</span>
          <span class="acct-note">Hồ sơ ở trạng thái Chờ kích hoạt sau khi lưu.</span>
        }
      </section>
    </aside>
  </div>
}
```

`frontend/projects/admin/src/app/students/student-form.component.scss` (giá trị nguyên văn page `AdTaoHoSo`):
```scss
:host { display: flex; flex-direction: column; gap: 22px; }
.crumbs { font-size: 14px; color: var(--muted); display: flex; gap: 8px; }
.crumbs a { color: var(--muted); }
.crumbs .here { color: var(--ink); }
h1 { margin: 0; font-family: var(--font-head); font-weight: 800; font-size: 46px; line-height: 1.05; }
.layout { display: flex; flex-wrap: wrap; gap: 24px; align-items: flex-start; }
form { flex: 999 1 560px; min-width: 0; display: flex; flex-direction: column; gap: 16px; }
.intro { margin: 0; font-size: 14px; color: var(--muted); }
.star { color: var(--primary); font-weight: 700; }
.card { background: var(--surface); border: 1px solid var(--line); border-radius: 12px; padding: 20px 22px; display: flex; flex-direction: column; gap: 14px; }
.sec-head { display: flex; flex-wrap: wrap; align-items: baseline; justify-content: space-between; gap: 6px 12px; }
.card h2 { margin: 0; font-family: var(--font-head); font-weight: 700; font-size: 26px; line-height: 1.1; }
.count { font-size: 12px; font-weight: 600; border-radius: 999px; padding: 2px 10px; white-space: nowrap; background: var(--pausedBg); color: var(--ink2); }
.count.full { background: var(--okBg); color: var(--okInk); }
.hint { margin: -6px 0 0; font-size: 13px; color: var(--muted); }
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 14px 16px; }
.field { display: flex; flex-direction: column; gap: 6px; min-width: 0; }
.field.wide { grid-column: 1 / -1; }
label { font-size: 14px; font-weight: 600; line-height: 1.35; }
.req { color: var(--primary); }
.ctl { box-sizing: border-box; width: 100%; border-radius: 14px; background: var(--surface); color: var(--ink); font: inherit; font-size: 15px; border: 1px solid var(--field); }
.ctl.err { border-color: var(--alert); }
input.ctl { height: 46px; padding: 0 14px; }
input.ctl[readonly] { background: var(--bg); color: var(--ink2); }
select.ctl { height: 46px; padding: 0 12px; }
textarea.ctl { padding: 12px 14px; line-height: 1.5; resize: vertical; }
.with-unit { display: flex; align-items: center; gap: 8px; }
.unit { font-size: 14px; color: var(--muted); white-space: nowrap; }
.note { font-size: 12px; color: var(--muted); }
.field-err { font-size: 12px; color: var(--alert); font-weight: 600; }
.msg { margin: 0; border-radius: 12px; padding: 10px 14px; font-size: 14px; font-weight: 600; background: var(--warnBg); color: var(--warn); }
.reload { margin-left: 8px; height: 36px; padding: 0 14px; border-radius: 999px; border: 1px solid var(--field); background: var(--surface);
  color: var(--ink); font: inherit; font-weight: 600; font-size: 14px; cursor: pointer; }
.actions { display: flex; flex-wrap: wrap; gap: 10px; }
.primary { height: 50px; padding: 0 26px; border-radius: 999px; border: 0; background: var(--primary); color: var(--onPrimary);
  font-family: var(--font-head); font-weight: 700; font-size: 20px; cursor: pointer; }
.primary:disabled { opacity: .6; cursor: default; }
.clear { height: 50px; padding: 0 18px; border-radius: 999px; border: 0; background: transparent; color: var(--muted);
  font: inherit; font-size: 15px; cursor: pointer; text-decoration: underline; }
aside { flex: 1 1 280px; max-width: 380px; position: sticky; top: 20px; display: flex; flex-direction: column; gap: 12px; }
.side { background: var(--surface); border: 1px solid var(--line); border-radius: 12px; padding: 16px 18px; display: flex; flex-direction: column; gap: 10px; }
.side-title { margin: 0; font-family: var(--font-head); font-weight: 700; font-size: 22px; }
.bar { height: 8px; border-radius: 999px; background: var(--pausedBg); overflow: hidden; }
.fill { height: 100%; border-radius: 999px; background: var(--primary); }
.fill.complete { background: var(--ok); }
.total { margin: 0; font-size: 14px; color: var(--ink2); }
.side ul { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 6px; font-size: 14px; }
.side li { display: flex; justify-content: space-between; gap: 10px; }
.account { gap: 8px; }
.account h2 { margin: 0; font-size: 15px; font-weight: 600; }
.acct { align-self: flex-start; border-radius: 999px; padding: 4px 12px; font-size: 13px; font-weight: 600; background: var(--pausedBg); color: var(--ink2); }
.acct.pending { background: var(--warnBg); color: var(--warn); }
.acct-note { font-size: 13px; color: var(--muted); }
@media (max-width: 879.98px) { h1 { font-size: 38px; } }
```

- [x] **Step 8: Route + mục rail "Tạo hồ sơ học viên"**

`frontend/projects/shared/src/lib/ui/icons.ts` — thêm vào object `ICONS`, sau `search`:
```ts
  userPlus: '<circle cx="10" cy="8" r="4"></circle><path d="M3 21c0-4 3-6 7-6 1.5 0 2.8.3 3.9.8M19 14v6M16 17h6"></path>',
```

`frontend/projects/admin/src/app/app.routes.ts` — thêm vào `children`, sau route `dashboard`:
```ts
      {
        path: 'hoc-vien/moi',
        loadComponent: () => import('./students/student-form.component').then((m) => m.StudentFormComponent),
        data: { mode: 'create' },
        title: 'Tạo hồ sơ học viên — Khu vực coach',
      },
      {
        path: 'hoc-vien/:id/sua',
        loadComponent: () => import('./students/student-form.component').then((m) => m.StudentFormComponent),
        data: { mode: 'edit' },
        title: 'Sửa hồ sơ — Khu vực coach',
      },
```

`frontend/projects/admin/src/app/shell/admin-shell.component.ts` — thay toàn bộ (thêm mục rail "Tạo hồ sơ học viên"; giữ tên coach lấy từ `/api/admin/profile` của Task 7):
```ts
import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DomSanitizer } from '@angular/platform-browser';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService, ICONS, UserMenuComponent } from '@coaching/shared';
import { catchError, map, of } from 'rxjs';

interface CoachProfile { id: string; displayName: string; email: string; }

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, UserMenuComponent],
  template: `
    <div class="root">
      <aside class="rail">
        <div class="brand">
          <p class="sig">duobexiuuu</p>
          <p class="area muted">Khu vực coach</p>
        </div>
        <nav aria-label="Admin">
          <a routerLink="/dashboard" routerLinkActive #dash="routerLinkActive" [attr.aria-current]="dash.isActive ? 'page' : null">
            <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" stroke-linejoin="round" [innerHTML]="homeIcon"></svg>Dashboard</a>
          <a routerLink="/hoc-vien/moi" routerLinkActive #create="routerLinkActive" [attr.aria-current]="create.isActive ? 'page' : null">
            <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" stroke-linejoin="round" [innerHTML]="userPlusIcon"></svg>Tạo hồ sơ học viên</a>
        </nav>
        <div class="user-block">
          <dbx-user-menu [name]="coachName() ?? auth.user()?.fullName ?? ''" roleLabel="Quản trị viên" />
        </div>
      </aside>
      <main><router-outlet /></main>
    </div>
  `,
  styles: `
    .root { min-height: 100vh; box-sizing: border-box; background: var(--bg); color: var(--ink); font-family: var(--font-body); display: flex; flex-wrap: wrap; }
    .rail { flex: 1 1 220px; max-width: 260px; min-width: 220px; box-sizing: border-box; background: var(--chrome);
      border-right: 1px solid var(--line); padding: 24px 16px; display: flex; flex-direction: column; gap: 28px; }
    .brand { padding: 0 8px; }
    .sig { margin: 0; font-family: var(--font-sig); font-size: 34px; color: var(--sig); line-height: 1; }
    .area { margin: 4px 0 0; font-size: 13px; color: var(--muted); }
    nav { display: flex; flex-direction: column; gap: 2px; }
    nav a { text-decoration: none; display: flex; align-items: center; gap: 10px; padding: 11px 12px; border-radius: 12px; font-size: 15px; font-weight: 600; }
    .user-block { margin-top: auto; }
    main { flex: 999 1 560px; min-width: 0; box-sizing: border-box; padding: 32px clamp(16px, 3vw, 48px) 48px; display: flex; flex-direction: column; gap: 24px; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminShellComponent {
  private readonly sanitizer = inject(DomSanitizer);
  protected readonly auth = inject(AuthService);
  protected readonly homeIcon = this.sanitizer.bypassSecurityTrustHtml(ICONS.home);
  protected readonly userPlusIcon = this.sanitizer.bypassSecurityTrustHtml(ICONS.userPlus);
  /** Tên coach lấy từ hệ thống (hồ sơ coach). */
  protected readonly coachName = toSignal(
    inject(HttpClient).get<CoachProfile>(this.auth.apiUrl('/api/admin/profile')).pipe(
      map((p) => p.displayName),
      catchError(() => of(null)),
    ),
    { initialValue: null },
  );
}
```

- [x] **Step 9: Chạy — kỳ vọng PASS**

Run: `cd frontend && npm run test:ci`
Expected: PASS (`StudentFormComponent` 9 test, `registration fields` 4 test).
Run: `cd frontend && npx ng build admin`
Expected: build thành công.
Kiểm tay (`npx ng serve admin` + API local): tạo hồ sơ chỉ có họ tên + email → chuyển `/hoc-vien/<id>`; gõ dở → tải lại trang vẫn còn bản nháp; "Xoá trắng hồ sơ" xóa nháp. So với page `AdTaoHoSo` ở 390px và 1280px (card nhóm radius 12 padding 20px 22px, ô 46px radius 14, nhãn 14px 600, badge "x/y bắt buộc", aside "Hồ sơ đã điền" + "Trạng thái tài khoản").

- [x] **Step 10: Commit**
```bash
git add frontend/projects/shared/src/lib/ui/icons.ts frontend/projects/admin
git commit -F - <<'EOF'
feat(admin): shared create/edit student form with draft and conflict reload

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 9: [UI] Hồ sơ học viên — header, đổi trạng thái có lý do, lịch sử, ghi chú nội bộ, mục còn thiếu

Màn hình (tech-design §3 "Thiết kế giao diện (UI)"): **Hồ sơ học viên** — `/hoc-vien/:id`, page `AdHoSo`; khung lý do và card "Lịch sử coaching" theo phần *suy ra, PO duyệt 2026-11-25* của tech-design. Ẩn các khối của spec khác (tab, Chương trình và phase, Chỉ tiêu, Hoạt động gần đây, So sánh, Tài khoản đăng nhập, "Gửi link đặt lại mật khẩu", "Soạn phase").

**Files:**
- Create: `frontend/projects/admin/src/app/students/format.ts`, `format.spec.ts`
- Create: `frontend/projects/admin/src/app/students/coaching-status-panel.component.ts`
- Create: `frontend/projects/admin/src/app/students/student-profile.component.ts`, `student-profile.component.html`, `student-profile.component.scss`, `student-profile.component.spec.ts`
- Modify: `frontend/projects/admin/src/app/students/students.api.ts`, `frontend/projects/admin/src/app/app.routes.ts`

**Interfaces:**
- Consumes: Task 4 — `PUT /api/admin/students/{id}/internal-note`; Task 5 — `POST /api/admin/students/{id}/coaching-status` (`{ status, reason, version }` → `StudentDetailDto`, 400 `student.reason_required`…, 409 `student.version_conflict`); Task 7 — `StatusBadgeComponent`, `initialsOf`, nhãn/tone trạng thái; Task 8 — `StudentsApi.get/updateNote`, `StudentDetail`, `FORM_SECTIONS`, `FieldDef`, `FIELD_LABEL`.
- Produces: `StudentsApi.changeStatus(id, status: CoachingStatus, reason: string, version: string): Observable<StudentDetail>`; `formatVnDay(iso)` ("dd/MM" giờ VN), `formatVnDayTime(iso)` ("dd/MM HH:mm" giờ VN), `formatDateOnly('yyyy-MM-dd')` ("dd/MM/yyyy"), `formatNumber(n)` (vi-VN, "72,5"); `<app-coaching-status-panel [action] [error] [busy] (confirmed) (cancelled)>`; `StudentProfileComponent` (input `id`); route `hoc-vien/:id`.

**Owns:** —

- [x] **Step 1: Viết spec cho định dạng ngày giờ VN và số**

`frontend/projects/admin/src/app/students/format.spec.ts`:
```ts
import { describe, expect, it } from 'vitest';
import { formatDateOnly, formatNumber, formatVnDay, formatVnDayTime } from './format';

describe('format', () => {
  it('formats instants in Vietnam time', () => {
    expect(formatVnDay('2026-11-23T02:00:00+00:00')).toBe('23/11');
    expect(formatVnDay('2026-11-24T23:30:00Z')).toBe('25/11');
    expect(formatVnDayTime('2026-11-25T02:00:00+00:00')).toBe('25/11 09:00');
  });

  it('formats dates and numbers the Vietnamese way', () => {
    expect(formatDateOnly('2026-11-25')).toBe('25/11/2026');
    expect(formatNumber(72.5)).toBe('72,5');
    expect(formatNumber(3500000)).toBe('3.500.000');
  });
});
```

- [x] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: FAIL — `Cannot find module './format'`.

- [x] **Step 3: `format.ts`**

`frontend/projects/admin/src/app/students/format.ts`:
```ts
const pad = (n: number) => String(n).padStart(2, '0');
const VIETNAM_OFFSET_MS = 7 * 60 * 60 * 1000; // UTC+7 cố định, không giờ mùa hè

function vietnamParts(iso: string) {
  const d = new Date(Date.parse(iso) + VIETNAM_OFFSET_MS);
  return { day: pad(d.getUTCDate()), month: pad(d.getUTCMonth() + 1), hour: pad(d.getUTCHours()), minute: pad(d.getUTCMinutes()) };
}

/** "dd/MM" theo giờ Việt Nam (vd. "Sửa lần cuối 23/11."). */
export function formatVnDay(iso: string): string {
  const p = vietnamParts(iso);
  return `${p.day}/${p.month}`;
}

/** "dd/MM HH:mm" theo giờ Việt Nam (dòng lịch sử, kiểu card "Tài khoản đăng nhập" của prototype). */
export function formatVnDayTime(iso: string): string {
  const p = vietnamParts(iso);
  return `${p.day}/${p.month} ${p.hour}:${p.minute}`;
}

/** "2026-11-25" → "25/11/2026". */
export function formatDateOnly(value: string): string {
  const [year, month, day] = value.split('-');
  return `${day}/${month}/${year}`;
}

const NUMBER = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 });

/** 72.5 → "72,5"; 3500000 → "3.500.000". */
export function formatNumber(value: number): string {
  return NUMBER.format(value);
}
```

- [x] **Step 4: Chạy — kỳ vọng PASS**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: PASS.

- [x] **Step 5: Viết spec cho màn Hồ sơ học viên**

`frontend/projects/admin/src/app/students/student-profile.component.spec.ts`:
```ts
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { describe, expect, it } from 'vitest';
import { StudentProfileComponent } from './student-profile.component';
import { StudentDetail } from './student.models';

const settle = () => new Promise((resolve) => setTimeout(resolve, 0));

function detail(overrides: Partial<StudentDetail> = {}): StudentDetail {
  return {
    id: 's1', coachId: 'c1', fullName: 'Lan', email: 'lan@gmail.com', accountStatus: 'PendingActivation', coachingStatus: 'Active',
    createdAt: '2026-11-20T02:00:00+00:00',
    registration: { trainingGoal: 'Giảm mỡ, giữ cơ', availability: 'T3–T7 buổi tối', equipment: null, weightKg: 72.5 },
    missingRequired: ['pack', 'photoBackUrl'], updatedAt: null, updatedBy: null, version: 'AAAAAAAAB9E=',
    internalNote: null, internalNoteUpdatedAt: null, statusHistory: [],
    ...overrides,
  };
}

describe('StudentProfileComponent', () => {
  async function setup(initial: StudentDetail = detail()) {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    const fixture = TestBed.createComponent(StudentProfileComponent);
    fixture.componentRef.setInput('id', 's1');
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/admin/students/s1').flush(initial);
    await settle();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const buttons = () => [...el.querySelectorAll('button')].map((b) => b.textContent?.trim());
    const click = (text: string) => ([...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === text) as HTMLButtonElement).click();
    return { fixture, http, el, buttons, click };
  }

  it('shows the header, statuses, actions of an active student and missing required fields', async () => {
    const { el, buttons } = await setup();

    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Lan');
    expect(el.querySelector('.meta')?.textContent).toContain('lan@gmail.com');
    expect(el.querySelector('.meta')?.textContent).toContain('Đang coaching');
    expect(el.querySelector('.meta')?.textContent).toContain('Chờ kích hoạt');
    expect(buttons()).toContain('Tạm dừng');
    expect(buttons()).toContain('Kết thúc');
    expect(buttons()).not.toContain('Tiếp tục coaching');
    expect(el.textContent).toContain('Giảm mỡ, giữ cơ');
    expect(el.querySelector('.missing')?.textContent).toContain('Gói đã mua');
    expect(el.querySelector('.missing')?.textContent).toContain('Ảnh mặt sau');
    expect(el.querySelector('a.edit')?.getAttribute('href')).toBe('/hoc-vien/s1/sua');
    expect(el.textContent).toContain('Chưa đổi trạng thái lần nào.');
  });

  // @BR-f02-ho-so-hoc-vien-017 @BR-f02-ho-so-hoc-vien-020: đổi trạng thái có lý do, thêm một dòng lịch sử
  it('changes the coaching status with a reason and shows the new history row', async () => {
    const { fixture, http, el, buttons, click } = await setup();

    click('Tạm dừng');
    fixture.detectChanges();
    const reason = el.querySelector('#status-reason') as HTMLTextAreaElement;
    reason.value = 'Đi công tác';
    reason.dispatchEvent(new Event('input'));
    click('Xác nhận');

    const post = http.expectOne('/api/admin/students/s1/coaching-status');
    expect(post.request.method).toBe('POST');
    expect(post.request.body).toEqual({ status: 'Paused', reason: 'Đi công tác', version: 'AAAAAAAAB9E=' });
    post.flush(detail({
      coachingStatus: 'Paused', version: 'AAAAAAAAB9I=',
      statusHistory: [{ from: 'Active', to: 'Paused', actor: 'Coach Duobexiuuu', at: '2026-11-25T02:00:00+00:00', reason: 'Đi công tác' }],
    }));
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('.meta')?.textContent).toContain('Tạm dừng');
    const row = el.querySelector('.history li')?.textContent ?? '';
    expect(row).toContain('25/11 09:00');
    expect(row).toContain('Đang coaching → Tạm dừng');
    expect(row).toContain('Đi công tác');
    expect(row).toContain('Coach Duobexiuuu');
    expect(el.querySelector('#status-reason')).toBeNull();
    expect(buttons()).toContain('Tiếp tục coaching');
  });

  // @BR-f02-ho-so-hoc-vien-018: lý do rỗng → không gửi
  it('requires a reason before sending', async () => {
    const { fixture, http, el, click } = await setup();

    click('Kết thúc');
    fixture.detectChanges();
    click('Xác nhận');
    fixture.detectChanges();

    http.expectNone('/api/admin/students/s1/coaching-status');
    expect(el.textContent).toContain('Cần nhập lý do đổi trạng thái coaching.');
  });

  // @BR-f02-ho-so-hoc-vien-014: sửa ghi chú tại chỗ, hiện ngày sửa ghi chú
  it('edits the internal note in place', async () => {
    const { fixture, http, el, click } = await setup();
    expect(el.textContent).toContain('Chưa có ghi chú.');

    click('Sửa ghi chú');
    fixture.detectChanges();
    const note = el.querySelector('#internal-note') as HTMLTextAreaElement;
    note.value = 'Tránh lunge sâu vì gối phải';
    note.dispatchEvent(new Event('input'));
    click('Lưu ghi chú');

    const put = http.expectOne('/api/admin/students/s1/internal-note');
    expect(put.request.body).toEqual({ note: 'Tránh lunge sâu vì gối phải', version: 'AAAAAAAAB9E=' });
    put.flush(detail({ internalNote: 'Tránh lunge sâu vì gối phải', internalNoteUpdatedAt: '2026-11-23T02:00:00+00:00' }));
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('.note-text')?.textContent?.trim()).toBe('Tránh lunge sâu vì gối phải');
    expect(el.querySelector('.note-meta')?.textContent?.trim()).toBe('Chỉ coach thấy. Sửa lần cuối 23/11.');
  });

  // @BR-f02-ho-so-hoc-vien-015: hồ sơ đã đổi ở nơi khác → báo và tải lại
  it('asks to reload when the profile was changed elsewhere', async () => {
    const { fixture, http, el, click } = await setup();

    click('Tạm dừng');
    fixture.detectChanges();
    const reason = el.querySelector('#status-reason') as HTMLTextAreaElement;
    reason.value = 'Đi công tác';
    reason.dispatchEvent(new Event('input'));
    click('Xác nhận');
    http.expectOne('/api/admin/students/s1/coaching-status').flush(
      { title: 'Hồ sơ vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.', code: 'student.version_conflict' },
      { status: 409, statusText: 'Conflict' });
    await settle();
    fixture.detectChanges();

    expect(el.textContent).toContain('Hồ sơ vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.');
    click('Tải lại');
    http.expectOne('/api/admin/students/s1').flush(detail({ coachingStatus: 'Ended' }));
    await settle();
    fixture.detectChanges();
    expect(el.querySelector('.meta')?.textContent).toContain('Đã kết thúc');
    expect(el.querySelector('button.reload')).toBeNull();
  });
});
```

- [x] **Step 6: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: FAIL — `Cannot find module './student-profile.component'`.

- [x] **Step 7: API đổi trạng thái, khung lý do, màn Hồ sơ**

`frontend/projects/admin/src/app/students/students.api.ts` — thêm `CoachingStatus` vào import từ `./student.models`, rồi thêm method sau `updateNote`:
```ts

  changeStatus(id: string, status: CoachingStatus, reason: string, version: string): Observable<StudentDetail> {
    return this.http.post<StudentDetail>(this.url(`/${id}/coaching-status`), { status, reason, version });
  }
```

`frontend/projects/admin/src/app/students/coaching-status-panel.component.ts`:
```ts
import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

/**
 * Khung lý do đổi trạng thái coaching — mở ngay dưới header (tech-design: suy ra, PO duyệt 2026-11-25).
 * @BR-f02-ho-so-hoc-vien-018: lý do bắt buộc, tối đa 500 ký tự (server kiểm lại).
 */
@Component({
  selector: 'app-coaching-status-panel',
  template: `
    <section class="panel" aria-labelledby="status-panel-title">
      <h2 id="status-panel-title">{{ action() }}</h2>
      <label for="status-reason">Lý do</label>
      <textarea id="status-reason" rows="3" maxlength="500" [value]="reason()"
                (input)="reason.set($any($event.target).value)"></textarea>
      @if (localError() ?? error(); as message) {
        <p class="msg" role="status">{{ message }}</p>
      }
      <div class="actions">
        <button type="button" class="primary" [disabled]="busy()" (click)="submit()">Xác nhận</button>
        <button type="button" class="secondary" (click)="cancelled.emit()">Huỷ</button>
      </div>
    </section>
  `,
  styles: `
    .panel { background: var(--surface); border: 1px solid var(--line); border-radius: 12px; padding: 16px 18px; display: flex; flex-direction: column; gap: 10px; }
    h2 { margin: 0; font-size: 16px; font-weight: 600; }
    label { font-size: 14px; font-weight: 600; line-height: 1.35; }
    textarea { box-sizing: border-box; width: 100%; border-radius: 14px; background: var(--surface); color: var(--ink); font: inherit;
      font-size: 15px; border: 1px solid var(--field); padding: 12px 14px; line-height: 1.5; resize: vertical; }
    .msg { margin: 0; border-radius: 12px; padding: 10px 14px; font-size: 14px; font-weight: 600; background: var(--warnBg); color: var(--warn); }
    .actions { display: flex; flex-wrap: wrap; gap: 8px; }
    .primary { height: 44px; padding: 0 20px; border-radius: 999px; border: 0; background: var(--primary); color: var(--onPrimary);
      font: inherit; font-weight: 600; font-size: 15px; cursor: pointer; }
    .primary:disabled { opacity: .6; cursor: default; }
    .secondary { height: 44px; padding: 0 22px; border-radius: 999px; border: 1px solid var(--field); background: var(--surface);
      color: var(--ink); font: inherit; font-weight: 600; font-size: 15px; cursor: pointer; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CoachingStatusPanelComponent {
  /** Tên hành động, vd. "Tạm dừng". */
  readonly action = input.required<string>();
  /** Lỗi từ server (ProblemDetails.title). */
  readonly error = input<string | null>(null);
  readonly busy = input(false);
  readonly confirmed = output<string>();
  readonly cancelled = output<void>();

  protected readonly reason = signal('');
  protected readonly localError = signal<string | null>(null);

  protected submit(): void {
    const text = this.reason().trim();
    if (!text) {
      this.localError.set('Cần nhập lý do đổi trạng thái coaching.');
      return;
    }
    this.localError.set(null);
    this.confirmed.emit(text);
  }
}
```

`frontend/projects/admin/src/app/students/student-profile.component.ts`:
```ts
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StatusBadgeComponent, initialsOf } from '@coaching/shared';
import { Observable, firstValueFrom } from 'rxjs';
import { CoachingStatusPanelComponent } from './coaching-status-panel.component';
import { formatDateOnly, formatNumber, formatVnDay, formatVnDayTime } from './format';
import { FIELD_LABEL, FORM_SECTIONS, FieldDef } from './registration-fields';
import { ACCOUNT_STATUS_LABEL, ACCOUNT_STATUS_TONE, COACHING_STATUS_LABEL, COACHING_STATUS_TONE } from './student-status';
import { CoachingStatus, StudentDetail } from './student.models';
import { StudentsApi } from './students.api';

interface StatusAction {
  label: string;
  to: CoachingStatus;
}

/** Nút theo trạng thái hiện tại — đổi được theo mọi chiều (@BR-f02-ho-so-hoc-vien-019), không đổi sang chính nó (@BR-f02-ho-so-hoc-vien-017). */
const ACTIONS: Record<CoachingStatus, StatusAction[]> = {
  Active: [{ label: 'Tạm dừng', to: 'Paused' }, { label: 'Kết thúc', to: 'Ended' }],
  Paused: [{ label: 'Tiếp tục coaching', to: 'Active' }, { label: 'Kết thúc', to: 'Ended' }],
  Ended: [{ label: 'Tiếp tục coaching', to: 'Active' }],
};

/** Mục form hiển thị ở "Xem đủ form đăng ký" (bỏ họ tên, email — đã ở header — và nhóm ghi chú). */
const REGISTRATION_SECTIONS = FORM_SECTIONS.filter((s) => s.id !== 'nb').map((s) => ({
  ...s,
  fields: s.fields.filter((f) => f.key !== 'fullName' && f.key !== 'email'),
}));

@Component({
  selector: 'app-student-profile',
  imports: [RouterLink, StatusBadgeComponent, CoachingStatusPanelComponent],
  templateUrl: './student-profile.component.html',
  styleUrl: './student-profile.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentProfileComponent implements OnInit {
  /** Route param `:id`. */
  readonly id = input.required<string>();

  private readonly api = inject(StudentsApi);

  protected readonly detail = signal<StudentDetail | null>(null);
  protected readonly loadFailed = signal(false);
  protected readonly conflict = signal(false);
  protected readonly busy = signal(false);
  protected readonly pending = signal<StatusAction | null>(null);
  protected readonly statusError = signal<string | null>(null);
  protected readonly editingNote = signal(false);
  protected readonly noteDraft = signal('');
  protected readonly noteError = signal<string | null>(null);
  protected readonly actions = computed(() => {
    const d = this.detail();
    return d ? ACTIONS[d.coachingStatus] : [];
  });

  protected readonly sections = REGISTRATION_SECTIONS;
  protected readonly coachingLabel = COACHING_STATUS_LABEL;
  protected readonly coachingTone = COACHING_STATUS_TONE;
  protected readonly accountLabel = ACCOUNT_STATUS_LABEL;
  protected readonly accountTone = ACCOUNT_STATUS_TONE;
  protected readonly initials = initialsOf;
  protected readonly formatDay = formatVnDay;
  protected readonly formatDayTime = formatVnDayTime;

  ngOnInit(): void {
    void this.load();
  }

  protected async load(): Promise<void> {
    this.conflict.set(false);
    try {
      this.detail.set(await firstValueFrom(this.api.get(this.id())));
      this.loadFailed.set(false);
    } catch {
      this.loadFailed.set(true);
    }
  }

  protected openPanel(action: StatusAction): void {
    this.statusError.set(null);
    this.pending.set(action);
  }

  protected closePanel(): void {
    this.pending.set(null);
    this.statusError.set(null);
  }

  // @BR-f02-ho-so-hoc-vien-017 @BR-f02-ho-so-hoc-vien-018 @BR-f02-ho-so-hoc-vien-020
  protected async changeStatus(to: CoachingStatus, reason: string): Promise<void> {
    const d = this.detail()!;
    const saved = await this.save(this.api.changeStatus(d.id, to, reason, d.version), (m) => this.statusError.set(m));
    if (saved) this.closePanel();
  }

  protected startNote(d: StudentDetail): void {
    this.noteDraft.set(d.internalNote ?? '');
    this.noteError.set(null);
    this.editingNote.set(true);
  }

  // @BR-f02-ho-so-hoc-vien-014
  protected async saveNote(): Promise<void> {
    const d = this.detail()!;
    const saved = await this.save(this.api.updateNote(d.id, this.noteDraft(), d.version), (m) => this.noteError.set(m));
    if (saved) this.editingNote.set(false);
  }

  protected text(d: StudentDetail, key: string): string {
    const value = d.registration[key];
    return typeof value === 'string' && value ? value : 'Chưa có';
  }

  protected display(d: StudentDetail, field: FieldDef): string {
    const value = d.registration[field.key];
    if (value === null || value === undefined || value === '') return '—';
    if (typeof value === 'number') return field.unit ? `${formatNumber(value)} ${field.unit}` : formatNumber(value);
    if (field.type === 'date') return formatDateOnly(value);
    return value;
  }

  protected isPhoto(field: FieldDef): boolean {
    return field.key.startsWith('photo');
  }

  protected label(code: string): string {
    return FIELD_LABEL[code] ?? code;
  }

  /** Gửi lệnh sửa; 409 → yêu cầu tải lại (@BR-f02-ho-so-hoc-vien-015); lỗi khác → câu báo của server. */
  private async save(request: Observable<StudentDetail>, onError: (message: string) => void): Promise<boolean> {
    this.busy.set(true);
    try {
      this.detail.set(await firstValueFrom(request));
      return true;
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 409) {
        this.conflict.set(true);
        this.pending.set(null);
        this.editingNote.set(false);
      } else {
        onError(error instanceof HttpErrorResponse && typeof error.error?.title === 'string'
          ? error.error.title
          : 'Không lưu được. Vui lòng thử lại.');
      }
      return false;
    } finally {
      this.busy.set(false);
    }
  }
}
```

`frontend/projects/admin/src/app/students/student-profile.component.html`:
```html
@if (detail(); as d) {
  <nav aria-label="Đường dẫn" class="crumbs">
    <a routerLink="/dashboard">Học viên</a><span aria-hidden="true">/</span><span class="here">{{ d.fullName }}</span>
  </nav>

  <header class="head">
    <div class="who">
      <div class="avatar" aria-hidden="true">{{ initials(d.fullName) }}</div>
      <div>
        <h1>{{ d.fullName }}</h1>
        <p class="meta">
          <span>{{ d.email }}</span>
          <dbx-status-badge [tone]="coachingTone[d.coachingStatus]" [dense]="true">{{ coachingLabel[d.coachingStatus] }}</dbx-status-badge>
          <dbx-status-badge [tone]="accountTone[d.accountStatus]" [dense]="true">{{ accountLabel[d.accountStatus] }}</dbx-status-badge>
        </p>
      </div>
    </div>
    <div class="head-actions">
      @for (a of actions(); track a.to) {
        <button type="button" class="secondary" [attr.aria-expanded]="pending()?.to === a.to" (click)="openPanel(a)">{{ a.label }}</button>
      }
    </div>
  </header>

  @if (pending(); as p) {
    <app-coaching-status-panel [action]="p.label" [error]="statusError()" [busy]="busy()"
                               (confirmed)="changeStatus(p.to, $event)" (cancelled)="closePanel()" />
  }
  @if (conflict()) {
    <p class="msg" role="status">Hồ sơ vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.
      <button type="button" class="reload" (click)="load()">Tải lại</button></p>
  }

  <div class="layout">
    <div class="main-col">
      <section class="card">
        <h2>Thông tin học viên</h2>
        <dl>
          <div><dt>Mục tiêu</dt><dd>{{ text(d, 'trainingGoal') }}</dd></div>
          <div><dt>Lịch rảnh</dt><dd>{{ text(d, 'availability') }}</dd></div>
          <div><dt>Dụng cụ</dt><dd>{{ text(d, 'equipment') }}</dd></div>
        </dl>
        <details>
          <summary>Xem đủ form đăng ký</summary>
          @for (sec of sections; track sec.id) {
            <h3>{{ sec.title }}</h3>
            <dl>
              @for (f of sec.fields; track f.key) {
                <div>
                  <dt>{{ f.label }}</dt>
                  <dd>
                    @if (isPhoto(f) && d.registration[f.key]) {
                      <a [href]="d.registration[f.key]" target="_blank" rel="noopener noreferrer">Mở ảnh</a>
                    } @else {
                      {{ display(d, f) }}
                    }
                  </dd>
                </div>
              }
            </dl>
          }
        </details>
        <a class="edit" [routerLink]="['/hoc-vien', d.id, 'sua']">Sửa thông tin</a>
        @if (d.updatedAt) {
          <span class="small">Sửa lần cuối {{ formatDayTime(d.updatedAt) }}@if (d.updatedBy) { bởi {{ d.updatedBy }}}.</span>
        }
      </section>

      <section class="card">
        <h2>Mục bắt buộc còn thiếu</h2>
        @if (d.missingRequired.length > 0) {
          <ul class="missing">
            @for (code of d.missingRequired; track code) {
              <li>{{ label(code) }}</li>
            }
          </ul>
        } @else {
          <p class="small">Hồ sơ đã đủ mục bắt buộc.</p>
        }
      </section>
    </div>

    <aside>
      <section class="note">
        <h2>Ghi chú nội bộ</h2>
        @if (editingNote()) {
          <label for="internal-note" class="sr-only">Ghi chú nội bộ</label>
          <textarea id="internal-note" rows="4" maxlength="5000" [value]="noteDraft()"
                    (input)="noteDraft.set($any($event.target).value)"></textarea>
          <div class="actions">
            <button type="button" class="primary" [disabled]="busy()" (click)="saveNote()">Lưu ghi chú</button>
            <button type="button" class="secondary" (click)="editingNote.set(false)">Huỷ</button>
          </div>
        } @else {
          <p class="note-text">{{ d.internalNote ?? 'Chưa có ghi chú.' }}</p>
          <button type="button" class="link-btn" (click)="startNote(d)">Sửa ghi chú</button>
        }
        <span class="note-meta">Chỉ coach thấy.@if (d.internalNoteUpdatedAt) { Sửa lần cuối {{ formatDay(d.internalNoteUpdatedAt) }}.}</span>
        @if (noteError(); as e) {
          <p class="msg" role="status">{{ e }}</p>
        }
      </section>

      <section class="card history">
        <h2 class="history-title">Lịch sử coaching</h2>
        @if (d.statusHistory.length > 0) {
          <ol>
            @for (h of d.statusHistory; track $index) {
              <li><strong>{{ formatDayTime(h.at) }}</strong> {{ coachingLabel[h.from] }} → {{ coachingLabel[h.to] }}. {{ h.reason }} <span class="by">({{ h.actor }})</span></li>
            }
          </ol>
        } @else {
          <p class="small">Chưa đổi trạng thái lần nào.</p>
        }
      </section>
    </aside>
  </div>
} @else if (loadFailed()) {
  <p class="msg" role="status">Không tải được hồ sơ. Vui lòng tải lại trang.</p>
}
```

`frontend/projects/admin/src/app/students/student-profile.component.scss` (giá trị nguyên văn page `AdHoSo`):
```scss
:host { display: flex; flex-direction: column; gap: 22px; }
.crumbs { font-size: 14px; color: var(--muted); display: flex; gap: 8px; }
.crumbs a { color: var(--muted); }
.crumbs .here { color: var(--ink); }
.head { display: flex; flex-wrap: wrap; justify-content: space-between; gap: 16px; align-items: center; }
.who { display: flex; align-items: center; gap: 16px; }
.avatar { width: 64px; height: 64px; border-radius: 50%; background: var(--soft); color: var(--navInk); display: flex; align-items: center;
  justify-content: center; font-family: var(--font-head); font-weight: 800; font-size: 29px; flex: none; }
h1 { margin: 0; font-family: var(--font-head); font-weight: 800; font-size: 43px; line-height: 1.05; }
.meta { margin: 4px 0 0; font-size: 14px; color: var(--ink2); display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
.head-actions { display: flex; flex-wrap: wrap; gap: 8px; }
.secondary { height: 44px; padding: 0 22px; border-radius: 999px; border: 1px solid var(--field); background: var(--surface); color: var(--ink);
  font: inherit; font-weight: 600; font-size: 15px; cursor: pointer; }
.primary { height: 44px; padding: 0 20px; border-radius: 999px; border: 0; background: var(--primary); color: var(--onPrimary);
  font: inherit; font-weight: 600; font-size: 15px; cursor: pointer; }
.primary:disabled { opacity: .6; cursor: default; }
.msg { margin: 0; border-radius: 12px; padding: 10px 14px; font-size: 14px; font-weight: 600; background: var(--warnBg); color: var(--warn); }
.reload { margin-left: 8px; height: 36px; padding: 0 14px; border-radius: 999px; border: 1px solid var(--field); background: var(--surface);
  color: var(--ink); font: inherit; font-weight: 600; font-size: 14px; cursor: pointer; }
.layout { display: flex; flex-wrap: wrap; gap: 22px; align-items: flex-start; }
.main-col { flex: 999 1 520px; min-width: 0; display: flex; flex-direction: column; gap: 18px; }
aside { flex: 1 1 300px; max-width: 400px; display: flex; flex-direction: column; gap: 12px; }
.card { background: var(--surface); border: 1px solid var(--line); border-radius: 12px; padding: 16px 18px; display: flex; flex-direction: column; gap: 12px; }
.card h2 { margin: 0; font-size: 16px; font-weight: 600; }
dl { margin: 0; display: flex; flex-direction: column; gap: 10px; font-size: 14px; }
dt { color: var(--muted); font-size: 12px; }
dd { margin: 2px 0 0; }
summary { font-size: 14px; font-weight: 600; color: var(--primary); cursor: pointer; }
details h3 { margin: 14px 0 8px; font-size: 14px; font-weight: 600; }
.edit { align-self: flex-start; height: 40px; padding: 0 22px; border-radius: 999px; border: 1px solid var(--field); background: var(--surface);
  color: var(--ink); font-weight: 600; font-size: 14px; display: inline-flex; align-items: center; text-decoration: none; }
.edit:hover { color: var(--ink); }
.small { font-size: 13px; color: var(--muted); margin: 0; }
.missing { margin: 0; padding-left: 20px; display: flex; flex-direction: column; gap: 6px; font-size: 14px; line-height: 1.5; }
.note { background: var(--warnBg); border-radius: 12px; padding: 16px 18px; display: flex; flex-direction: column; gap: 8px; }
.note h2 { margin: 0; font-size: 16px; font-weight: 600; color: var(--warn); }
.note-text { margin: 0; font-size: 14px; line-height: 1.55; white-space: pre-line; }
.note-meta { font-size: 12px; color: var(--ink2); }
.note textarea { box-sizing: border-box; width: 100%; border-radius: 14px; background: var(--surface); color: var(--ink); font: inherit;
  font-size: 15px; border: 1px solid var(--field); padding: 12px 14px; line-height: 1.5; resize: vertical; }
.note .actions { display: flex; flex-wrap: wrap; gap: 8px; }
.link-btn { align-self: flex-start; border: 0; background: transparent; padding: 0; color: var(--primary); font: inherit; font-size: 14px;
  font-weight: 600; cursor: pointer; text-decoration: underline; }
.history-title { font-family: var(--font-head); font-weight: 700; font-size: 24px !important; }
.history ol { margin: 0; padding: 0 0 0 18px; border-left: 2px solid var(--soft); list-style: none; display: flex; flex-direction: column; gap: 10px; font-size: 14px; }
.by { color: var(--muted); }
.sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
@media (max-width: 879.98px) { h1 { font-size: 38px; } }
```

`frontend/projects/admin/src/app/app.routes.ts` — thêm vào `children`, sau route `hoc-vien/moi` (đặt trước `hoc-vien/:id/sua` hay sau đều được; `moi` phải đứng trước `:id`):
```ts
      {
        path: 'hoc-vien/:id',
        loadComponent: () => import('./students/student-profile.component').then((m) => m.StudentProfileComponent),
        title: 'Hồ sơ học viên — Khu vực coach',
      },
```

- [x] **Step 8: Chạy — kỳ vọng PASS**

Run: `cd frontend && npm run test:ci`
Expected: PASS (`StudentProfileComponent` 5 test, `format` 2 test).
Run: `cd frontend && npx ng build admin`
Expected: build thành công.
Kiểm tay: mở một hồ sơ → bấm "Tạm dừng" → khung lý do mở dưới header → "Xác nhận" → chip "Tạm dừng", dòng mới trong "Lịch sử coaching"; sửa ghi chú tại chỗ → "Chỉ coach thấy. Sửa lần cuối dd/MM."; "Sửa thông tin" mở `/hoc-vien/:id/sua`. So với page `AdHoSo` ở 390px và 1280px (avatar 64px, h1 43px, nút 44px, card radius 12 padding 16px 18px, ghi chú nền `--warnBg`).

- [x] **Step 9: Commit**
```bash
git add frontend/projects/admin
git commit -F - <<'EOF'
feat(admin): student profile with coaching status panel, history and internal note

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 10: E2E `@web` cho hồ sơ học viên + cập nhật bảng route trong CLAUDE.md

**Files:**
- Create: `frontend/e2e/steps/student-profile.steps.ts`
- Modify: `CLAUDE.md` (bảng "Page key → Route gợi ý")

**Interfaces:**
- Consumes: Task 7 — `/dashboard`, ô "Tìm học viên", tên học viên là link; Task 8 — `/hoc-vien/moi`, nhãn "Họ và tên", "Email đăng nhập", nút "Lưu hồ sơ", rail "Tạo hồ sơ học viên"; Task 9 — `/hoc-vien/:id` có `h1` = họ tên; F00 — `E2EWorld.login(page, email, password)`, `ADMIN`, `ADMIN_URL`, `expect`.
- Produces: step `@web` cho `Background` + scenario `@web` của `@BR-f02-ho-so-hoc-vien-003` và `@BR-f02-ho-so-hoc-vien-024`.

**Owns:** —

- [ ] **Step 1: Chạy e2e của F02 — kỳ vọng FAIL**

Run: `docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build --wait` rồi `cd frontend && npm run e2e -- --tags "@BR-f02-ho-so-hoc-vien-003 or @BR-f02-ho-so-hoc-vien-024"`
Expected: FAIL — 4 scenario `Undefined` (`coach "Coach Duobexiuuu" đã đăng nhập`, `coach tạo hồ sơ học viên chỉ với họ tên … và email …`, `có hồ sơ … với email …`, …).

- [ ] **Step 2: Viết step defs `@web`**

`frontend/e2e/steps/student-profile.steps.ts`:
```ts
import { Given, Then, When } from '@cucumber/cucumber';
import { expect } from '../support/expect';
import { ADMIN, ADMIN_URL } from '../support/env';
import { E2EWorld } from '../support/world';

/** DB của compose giữ dữ liệu giữa các lần chạy → thêm hậu tố vào email để không trùng (BR-f02-ho-so-hoc-vien-001). */
const uniqueEmail = (email: string) =>
  email.replace('@', `+e2e${Date.now()}${Math.floor(Math.random() * 1000)}@`);

// Background của features/hoc-vien/ho-so/ho-so-hoc-vien.feature (admin seed của compose là coach duy nhất)
Given('coach {string} đã đăng nhập', async function (this: E2EWorld, _coachName: string) {
  await this.page.goto(`${ADMIN_URL}/login`);
  await this.login(this.page, ADMIN.email, ADMIN.password);
  await expect(this.page.getByRole('heading', { name: 'Học viên', exact: true })).toBeVisible();
});

// @BR-f02-ho-so-hoc-vien-003
When('coach tạo hồ sơ học viên chỉ với họ tên {string} và email {string}',
  async function (this: E2EWorld, fullName: string, email: string) {
    await this.page.getByRole('link', { name: 'Tạo hồ sơ học viên' }).first().click();
    await expect(this.page.getByRole('heading', { name: 'Tạo hồ sơ học viên', level: 1 })).toBeVisible();
    await this.page.getByLabel('Họ và tên').fill(fullName);
    await this.page.getByLabel('Email đăng nhập').fill(uniqueEmail(email));
    await this.page.getByRole('button', { name: 'Lưu hồ sơ' }).click();
  });

Then('hồ sơ của {string} được lưu', async function (this: E2EWorld, fullName: string) {
  await expect(this.page).toHaveURL(/\/hoc-vien\/[0-9a-f-]{36}$/);
  await expect(this.page.getByRole('heading', { level: 1, name: fullName, exact: true })).toBeVisible();
  expect(await this.page.evaluate(() => localStorage.getItem('dbx.admin.student-draft'))).toBeNull();
});

// @BR-f02-ho-so-hoc-vien-024
Given('có hồ sơ {string} với email {string}', async function (this: E2EWorld, fullName: string, email: string) {
  const login = await this.page.request.post(`${ADMIN_URL}/api/auth/login`,
    { data: { email: ADMIN.email, password: ADMIN.password, app: 'admin' } });
  expect(login.ok()).toBe(true);
  const { accessToken } = (await login.json()) as { accessToken: string };
  const created = await this.page.request.post(`${ADMIN_URL}/api/admin/students`, {
    data: { fullName, email: uniqueEmail(email) },
    headers: { authorization: `Bearer ${accessToken}` },
  });
  expect(created.status()).toBe(201);
});

When('coach tìm học viên với từ khóa {string}', async function (this: E2EWorld, keyword: string) {
  await this.page.goto(`${ADMIN_URL}/dashboard`);
  await expect(this.page.getByRole('heading', { name: 'Học viên', exact: true })).toBeVisible();
  await this.page.getByLabel('Tìm học viên').fill(keyword);
});

Then('kết quả có hồ sơ {string}', async function (this: E2EWorld, fullName: string) {
  await expect(this.page.getByRole('link', { name: fullName, exact: true }).first()).toBeVisible();
});
```

- [ ] **Step 3: Chạy — kỳ vọng PASS**

Run: `cd frontend && npm run e2e -- --tags "@BR-f02-ho-so-hoc-vien-003 or @BR-f02-ho-so-hoc-vien-024"`
Expected: `4 scenarios (4 passed)` (BR-003: 1; BR-024: 3 ví dụ "quan", "Quân", "hquan").
Run: `cd frontend && npm run e2e`
Expected: không scenario nào failed (gồm scenario `@web` F00 đã cập nhật ở Task 7).

- [ ] **Step 4: Cập nhật bảng route trong CLAUDE.md**

`CLAUDE.md` — trong bảng "Page key", thay 2 dòng:
```markdown
| `AdTaoHoSo` | AD01 Tạo hồ sơ học viên | admin | `/students/new` |
| `AdHoSo` | AD01/AD03 Hồ sơ học viên | admin | `/students/:id` |
```
bằng:
```markdown
| `AdTaoHoSo` | AD01 Tạo hồ sơ học viên | admin | `/hoc-vien/moi` |
| `AdHoSo` | AD01/AD03 Hồ sơ học viên | admin | `/hoc-vien/:id` (sửa: `/hoc-vien/:id/sua`) |
```
và thêm ngay sau bảng (trước dòng "Dữ liệu mẫu (giáo án, dinh dưỡng, thuật ngữ)…"):
```markdown
Route web admin dùng tiếng Việt (ADR-f02-ho-so-hoc-vien-008): đã có `/dashboard` (trang đích), `/hoc-vien/moi`, `/hoc-vien/:id`, `/hoc-vien/:id/sua`. Route của màn chưa làm trong bảng vẫn là gợi ý — khi làm, đặt tiếng Việt dưới tiền tố `/hoc-vien/:id/…`.
```

- [ ] **Step 5: Nghiệm thu toàn bộ F02**

Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed.
Run: `cd frontend && npm run test:ci && npm run e2e`
Expected: không Failed.
Kiểm tay (ghi kết quả vào mô tả commit/MR, không chặn commit):
- So 3 màn (Dashboard, Tạo/Sửa hồ sơ, Hồ sơ học viên) với prototype ở 390px và 1280px.
- NFR-f02-ho-so-hoc-vien-005: trên môi trường test có 500 hồ sơ, đo `GET /api/admin/students` và `GET /api/admin/students/{id}` — p95 < 500 ms.

- [ ] **Step 6: Commit**
```bash
git add frontend/e2e/steps/student-profile.steps.ts CLAUDE.md
git commit -F - <<'EOF'
test(e2e): student profile web scenarios (BR-f02-003, 024); docs: Vietnamese admin routes

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```
