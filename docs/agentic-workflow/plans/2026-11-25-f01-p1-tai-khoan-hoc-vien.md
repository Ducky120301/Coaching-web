---
doc-id: 2026-11-25-f01-p1-tai-khoan-hoc-vien
status: ready
---

# F01-P1 — Tài khoản học viên: Implementation Plan

> Frontmatter `status: draft | ready` là gate (exec-plan yêu cầu spec + tech-design + plan đều `ready`). User mark Ready sau khi review.

> **For agentic workers:** REQUIRED SUB-SKILL: Use `exec-plan` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Coach gửi/gửi lại thông tin đăng nhập, sửa email trước kích hoạt, gửi link đặt lại; học viên kích hoạt bằng mật khẩu tạm, tự đặt lại mật khẩu qua Quên mật khẩu (BR-f01-p1-tai-khoan-hoc-vien-001→027).

**Architecture:** Mật khẩu tạm là mật khẩu Identity; hạn dùng, thời điểm gửi/kích hoạt và lịch sử tài khoản (entity con chỉ thêm) nằm trong aggregate `Student`. Đăng nhập bằng mật khẩu tạm trả mã kích hoạt thay vì phiên; mã kích hoạt và link đặt lại lưu băm trong bảng `AccountTokens`. Email gửi trong transaction (lỗi → rollback); Quên mật khẩu đi hàng đợi nền và luôn trả cùng một câu. Provider email `Capture` + `GET /api/test/emails` cho e2e.

**Tech Stack:** .NET 10, EF Core 10, ASP.NET Identity, Liquibase 4.33.0, Reqnroll.xUnit, Testcontainers.MsSql, Microsoft.Extensions.TimeProvider.Testing · Angular 22 (zoneless), Vitest · @cucumber/cucumber + Playwright.

## Global Constraints

- Làm trên nhánh `main`, không worktree (quyết định của user). Không push.
- Dependency rule: `Api → Application → Domain`; `Infrastructure → Application, Domain`; Domain không tham chiếu EF/ASP.NET.
- Không gọi `DateTime.Now`/`DateTime.UtcNow` trong Domain/Application; thời gian đi qua `IClock`.
- Schema chỉ đổi qua Liquibase `database/changelog/v0.3.0/` (changeset XML → `scripts/*.sql` + `rollback/*.sql`); KHÔNG dùng `dotnet ef migrations`. Không thêm package NuGet/npm mới.
- Thời hạn (nguyên văn spec): mật khẩu tạm hết hiệu lực khi đủ 72 giờ kể từ lúc gửi; link đặt lại hết hiệu lực khi đủ 30 phút kể từ lúc tạo; mã kích hoạt 30 phút nhưng không quá hạn mật khẩu tạm. Hết hạn khi `now >= ExpiresAt`.
- Mật khẩu riêng dài 8–128 ký tự, hai lần nhập phải giống nhau, giữ đúng từng ký tự (không trim ở API lẫn FE).
- Quên mật khẩu: chỉ gửi cho học viên Đã kích hoạt; mỗi tài khoản tối đa 1 email / 60 giây; luôn `202` với câu nguyên văn: `Nếu email này thuộc tài khoản học viên đã kích hoạt, link đặt lại mật khẩu đã được gửi tới email đó. Link có hiệu lực trong 30 phút. Không thấy email thì kiểm tra mục Spam.`
- Đăng nhập thất bại (kể cả mật khẩu tạm hết hạn/đã thay, mật khẩu tạm trên web quản trị) dùng đúng `AuthErrors.InvalidCredentials` của F00.
- Không response nào (admin hay client) chứa mật khẩu tạm, mật khẩu riêng hay token đặt lại; mật khẩu tạm và token chỉ lưu dạng băm; ở Production log email không ghi thân thư (NFR-f01-p1-tai-khoan-hoc-vien-001).
- Địa chỉ trong email lấy từ `App:ClientBaseUrl`; trống → `account.client_url_missing` (503), không gửi (NFR-f01-p1-tai-khoan-hoc-vien-003).
- Provider `Capture` cấm ở môi trường `Production`; `/api/test/emails` chỉ tồn tại khi provider là `Capture`.
- Mã lỗi và câu chữ: theo bảng API Contracts của tech-design, nguyên văn. `ErrorKind.Unavailable` → 503.
- Lịch sử tài khoản chỉ thêm; 5 loại `CredentialsSent`, `CredentialsResent`, `EmailChanged`, `ResetLinkSent`, `Activated`; gửi lỗi hoặc học viên tự xin Quên mật khẩu không thêm dòng.
- Route web học viên tiếng Việt: `/kich-hoat`, `/quen-mat-khau`, `/dat-lai-mat-khau` (token sau `#t=`).
- FE bám **chính xác** prototype `docs/duobexiuuu-coaching-design.html` (page `Main`, `AdTaoHoSo`, `AdHoSo`); breakpoint duy nhất 880px; không hard-code màu; kiểm ở 390px và 1280px. Câu chữ UI theo mục "Thiết kế giao diện (UI)" của tech-design.
- Living docs `features/hoc-vien/tai-khoan/*.feature` và `features/hoc-vien/ho-so/ho-so-hoc-vien.feature` đã chốt — KHÔNG sửa nội dung `.feature`. Step `@api` chạy Reqnroll, step `@web` chạy CucumberJS.
- `reqnroll.json` `missingOrPendingStepsOutcome: Error`: scenario thuộc task sau fail `MissingStepDefinition` cho tới khi được bind; mọi lỗi khác phải sửa.
- NFR-f01-p1-tai-khoan-hoc-vien-004 (p95 < 500 ms) và NFR-f01-p1-tai-khoan-hoc-vien-005 (coach duyệt email) kiểm thủ công sau Task 10.
- Annotation truy vết: `// @BR-f01-p1-tai-khoan-hoc-vien-<số>` / `// @ADR-f01-p1-tai-khoan-hoc-vien-<số>` tại code thực thi và trên test (SQL `-- @ADR-…`, TS `// @…`).
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
| 1 | Prefactor: schema v0.3.0, trường tài khoản + entity lịch sử, `AccountState`, `AccountInfoDto`, `ErrorKind.Unavailable` | ADR-006 |
| 2 | Hạ tầng email: provider Capture + endpoint test + chốt Production, hàng đợi, nội dung email + `App:ClientBaseUrl` | ADR-007, 008 |
| 3 | Kho token `AccountTokens` | ADR-003 |
| 4 | Coach gửi/gửi lại thông tin đăng nhập + đăng nhập bằng mật khẩu tạm trả mã kích hoạt; `Student.Activate` + seed demo Đã kích hoạt | BR-001–006, 012, 025; ADR-001, 002, 004 |
| 5 | Kích hoạt: đặt mật khẩu riêng, trạng thái tài khoản cho coach | BR-011, 013–016, 026 |
| 6 | Sửa email trước kích hoạt | BR-007–010, 027 |
| 7 | Link đặt lại (coach), Quên mật khẩu, đặt lại mật khẩu, thu hồi phiên | BR-017–024; ADR-005, 009 |
| 8 | [UI] Web học viên: Đăng nhập, Kích hoạt, Quên mật khẩu, Đặt lại mật khẩu | ADR-010 |
| 9 | [UI] Web admin: Tạo hồ sơ, Hồ sơ (card tài khoản), Sửa hồ sơ (email) | — |
| 10 | e2e `@web` + cấu hình compose/CI + CLAUDE.md | — |

---

### Task 1: Prefactor — schema v0.3.0, trường tài khoản + `StudentAccountEvent`, `AccountState`, `AccountInfoDto`, `ErrorKind.Unavailable`

**Files:**
- Create: `database/changelog/v0.3.0/changelog.xml`, `database/changelog/v0.3.0/0001-student-accounts.xml`, `database/changelog/v0.3.0/scripts/0001-student-accounts.sql`, `database/changelog/v0.3.0/rollback/0001-student-accounts.rollback.sql`
- Modify: `database/changelog/db.changelog-master.xml`
- Create: `backend/src/Coaching.Domain/Students/AccountEventType.cs`, `AccountState.cs`, `StudentAccountEvent.cs`
- Modify: `backend/src/Coaching.Domain/Students/Student.cs`
- Modify: `backend/src/Coaching.Application/Common/Result.cs`, `backend/src/Coaching.Application/Students/StudentDtos.cs`
- Modify: `backend/src/Coaching.Application/Students/Queries/GetStudent.cs`, `Commands/CreateStudent.cs`, `Commands/UpdateStudent.cs`, `Commands/UpdateInternalNote.cs`, `Commands/ChangeCoachingStatus.cs`
- Modify: `backend/src/Coaching.Infrastructure/Persistence/Configurations/StudentConfiguration.cs`
- Modify: `backend/src/Coaching.Api/Common/ResultExtensions.cs`
- Test: `backend/tests/Coaching.Domain.Tests/Students/StudentAccountTests.cs`
- Test: `backend/tests/Coaching.Application.Tests/Students/StudentDetailDtoTests.cs`
- Test: `backend/tests/Coaching.Api.IntegrationTests/Students/AccountSchemaTests.cs`, `backend/tests/Coaching.Api.IntegrationTests/Common/ResultExtensionsTests.cs`

**Interfaces:**
- Consumes (F00/F02): `Student` (`_statusHistory` + `OwnsMany` làm mẫu), `Entity<Guid>`, `Email.MaxLength`, `StudentDetailDto.From`, `IClock.UtcNow`, `Error`/`ErrorKind`, `ResultExtensions.ToProblem`, `StudentRequests.CreateAsync/Url`, `ApiFactory.AdminClientAsync`.
- Produces:
  - DB: `Students.CredentialsSentAt/TempPasswordExpiresAt/ActivatedAt`, bảng `StudentAccountEvents`, bảng `AccountTokens` (entity EF ở Task 3), tag `v0.3.0`.
  - Domain `Coaching.Domain.Students`: `enum AccountEventType { CredentialsSent, CredentialsResent, EmailChanged, ResetLinkSent, Activated }`, `enum AccountState { NotSent, Sent, TempPasswordExpired, Activated }`, `StudentAccountEvent : Entity<Guid>` (`Type, At, ActorUserId?, FromEmail?, ToEmail?`, `internal` ctor `(AccountEventType type, DateTimeOffset at, Guid? actorUserId, string? fromEmail = null, string? toEmail = null)`), `Student.TempPasswordLifetime` (72 h), `Student.CredentialsSentAt/TempPasswordExpiresAt/ActivatedAt`, `Student.AccountEvents`, `Student.GetAccountState(DateTimeOffset now)`. Task 4–7 thêm các hàm nghiệp vụ ghi vào `_accountEvents`.
  - Application: `ErrorKind.Unavailable`; `AccountEventDto(string Type, DateTimeOffset At, string? FromEmail, string? ToEmail)`; `AccountInfoDto(string State, DateTimeOffset? CredentialsSentAt, DateTimeOffset? TempPasswordExpiresAt, DateTimeOffset? ActivatedAt, IReadOnlyList<AccountEventDto> History)` + `AccountInfoDto.From(Student, DateTimeOffset now)`; `StudentDetailDto` thêm tham số cuối `AccountInfoDto Account`; `StudentDetailDto.From(Student student, Coach coach, DateTimeOffset now)`; `GetStudentHandler(CurrentCoach, IStudentRepository, IClock)`.
  - API: `ErrorKind.Unavailable` → 503 ProblemDetails có `code`.

**Owns:** — | `@ADR-f01-p1-tai-khoan-hoc-vien-006`

- [x] **Step 1: Liquibase v0.3.0 — 3 cột `Students`, bảng `StudentAccountEvents`, bảng `AccountTokens`**

`database/changelog/v0.3.0/scripts/0001-student-accounts.sql`:
```sql
-- v0.3.0-0001: tài khoản học viên (F01-P1) — thời điểm gửi/hạn mật khẩu tạm/kích hoạt, lịch sử tài khoản, token kích hoạt + đặt lại
ALTER TABLE [Students] ADD
    [CredentialsSentAt] datetimeoffset NULL,
    [TempPasswordExpiresAt] datetimeoffset NULL,
    [ActivatedAt] datetimeoffset NULL;
GO
-- @ADR-f01-p1-tai-khoan-hoc-vien-006: lịch sử tài khoản là entity con của Student, chỉ thêm
CREATE TABLE [StudentAccountEvents] (
    [Id] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [Type] nvarchar(30) NOT NULL,
    [At] datetimeoffset NOT NULL,
    [ActorUserId] uniqueidentifier NULL,
    [FromEmail] nvarchar(256) NULL,
    [ToEmail] nvarchar(256) NULL,
    CONSTRAINT [PK_StudentAccountEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentAccountEvents_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO
CREATE INDEX [IX_StudentAccountEvents_StudentId_At] ON [StudentAccountEvents] ([StudentId], [At]);
GO
-- Token kích hoạt + link đặt lại: chỉ lưu SHA-256 (hex 64 ký tự). Entity EF + store ở Task 3.
CREATE TABLE [AccountTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Purpose] nvarchar(20) NOT NULL,
    [Source] nvarchar(10) NOT NULL,
    [TokenHash] nvarchar(64) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [ExpiresAt] datetimeoffset NOT NULL,
    [ConsumedAt] datetimeoffset NULL,
    [RevokedAt] datetimeoffset NULL,
    CONSTRAINT [PK_AccountTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AccountTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO
CREATE UNIQUE INDEX [IX_AccountTokens_TokenHash] ON [AccountTokens] ([TokenHash]);
GO
CREATE INDEX [IX_AccountTokens_UserId_Purpose] ON [AccountTokens] ([UserId], [Purpose]);
GO
```

`database/changelog/v0.3.0/rollback/0001-student-accounts.rollback.sql`:
```sql
-- Rollback v0.3.0-0001: xóa 2 bảng rồi 3 cột
DROP TABLE IF EXISTS [AccountTokens];
GO
DROP TABLE IF EXISTS [StudentAccountEvents];
GO
ALTER TABLE [Students] DROP COLUMN [ActivatedAt], [TempPasswordExpiresAt], [CredentialsSentAt];
GO
```

`database/changelog/v0.3.0/0001-student-accounts.xml`:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog http://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-4.33.xsd">
  <changeSet id="v0.3.0-0001-student-accounts" author="duc.hm4@kiotviet.com">
    <comment>F01-P1: Students (CredentialsSentAt, TempPasswordExpiresAt, ActivatedAt) + StudentAccountEvents + AccountTokens</comment>
    <sqlFile path="scripts/0001-student-accounts.sql" relativeToChangelogFile="true" splitStatements="true" endDelimiter="\nGO" stripComments="false"/>
    <rollback>
      <sqlFile path="rollback/0001-student-accounts.rollback.sql" relativeToChangelogFile="true" splitStatements="true" endDelimiter="\nGO"/>
    </rollback>
  </changeSet>
</databaseChangeLog>
```

`database/changelog/v0.3.0/changelog.xml`:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!-- v0.3.0 = P1 (F01-P1 tài khoản học viên). Thêm changeset mới TRƯỚC changeset tag. -->
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog http://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-4.33.xsd">
  <include file="0001-student-accounts.xml" relativeToChangelogFile="true"/>

  <changeSet id="v0.3.0-tag" author="duc.hm4@kiotviet.com">
    <tagDatabase tag="v0.3.0"/>
  </changeSet>
</databaseChangeLog>
```

`database/changelog/db.changelog-master.xml` — thêm sau dòng include `v0.2.0`:
```xml
  <include file="v0.3.0/changelog.xml" relativeToChangelogFile="true"/>
```

- [x] **Step 2: Viết Domain test — hồ sơ mới chưa gửi, chưa có lịch sử tài khoản**

`backend/tests/Coaching.Domain.Tests/Students/StudentAccountTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class StudentAccountTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 25, 2, 0, 0, TimeSpan.Zero);

    private static Student NewStudent() =>
        Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Lan", Email.Create("lan@gmail.com"), CreatedAt);

    // @ADR-f01-p1-tai-khoan-hoc-vien-006: hồ sơ mới chưa có dòng lịch sử tài khoản nào
    [Fact]
    public void New_student_has_not_sent_state_and_empty_account_history()
    {
        var student = NewStudent();

        Assert.Equal(AccountState.NotSent, student.GetAccountState(CreatedAt.AddDays(10)));
        Assert.Null(student.CredentialsSentAt);
        Assert.Null(student.TempPasswordExpiresAt);
        Assert.Null(student.ActivatedAt);
        Assert.Empty(student.AccountEvents);
    }

    [Fact]
    public void Temp_password_lifetime_is_72_hours()
    {
        Assert.Equal(TimeSpan.FromHours(72), Student.TempPasswordLifetime);
    }
}
```

- [x] **Step 3: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students.StudentAccountTests"`
Expected: FAIL — build error `The type or namespace name 'AccountState' could not be found` và `'Student' does not contain a definition for 'GetAccountState'`.

- [x] **Step 4: Domain — loại sự kiện, trạng thái tài khoản, entity lịch sử, trường mới của `Student`**

`backend/src/Coaching.Domain/Students/AccountEventType.cs`:
```csharp
namespace Coaching.Domain.Students;

/// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-006: 5 loại dòng lịch sử tài khoản.</summary>
public enum AccountEventType
{
    CredentialsSent,
    CredentialsResent,
    EmailChanged,
    ResetLinkSent,
    Activated,
}
```

`backend/src/Coaching.Domain/Students/AccountState.cs`:
```csharp
namespace Coaching.Domain.Students;

/// <summary>Trạng thái tài khoản coach nhìn thấy trên hồ sơ (tính theo thời điểm hiện tại).</summary>
public enum AccountState
{
    NotSent,
    Sent,
    TempPasswordExpired,
    Activated,
}
```

`backend/src/Coaching.Domain/Students/StudentAccountEvent.cs`:
```csharp
using Coaching.Domain.Common;

namespace Coaching.Domain.Students;

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-006: một dòng lịch sử tài khoản. Không có setter công khai, chỉ Student tạo được
/// → không sửa/xóa. Không bao giờ chứa mật khẩu hay token.
/// </summary>
public sealed class StudentAccountEvent : Entity<Guid>
{
    public AccountEventType Type { get; private set; }

    public DateTimeOffset At { get; private set; }

    /// <summary>Người thực hiện: coach (gửi, sửa email, gửi link) hoặc chính học viên (kích hoạt).</summary>
    public Guid? ActorUserId { get; private set; }

    /// <summary>Chỉ có ở EmailChanged.</summary>
    public string? FromEmail { get; private set; }

    /// <summary>Chỉ có ở EmailChanged.</summary>
    public string? ToEmail { get; private set; }

    private StudentAccountEvent() { }

    internal StudentAccountEvent(AccountEventType type, DateTimeOffset at, Guid? actorUserId,
        string? fromEmail = null, string? toEmail = null)
        : base(Guid.CreateVersion7())
    {
        Type = type;
        At = at;
        ActorUserId = actorUserId;
        FromEmail = fromEmail;
        ToEmail = toEmail;
    }
}
```

`backend/src/Coaching.Domain/Students/Student.cs` — sửa 3 chỗ:

(a) thay dòng `private readonly List<CoachingStatusChange> _statusHistory = [];` bằng:
```csharp
    /// <summary>Mật khẩu tạm sống 72 giờ kể từ lần gửi gần nhất.</summary>
    public static readonly TimeSpan TempPasswordLifetime = TimeSpan.FromHours(72);

    private readonly List<CoachingStatusChange> _statusHistory = [];

    // @ADR-f01-p1-tai-khoan-hoc-vien-006
    private readonly List<StudentAccountEvent> _accountEvents = [];
```

(b) thêm ngay sau property `StatusHistory` (dòng `public IReadOnlyList<CoachingStatusChange> StatusHistory => _statusHistory.AsReadOnly();`):
```csharp

    /// <summary>Lần gửi thông tin đăng nhập gần nhất (null = chưa gửi).</summary>
    public DateTimeOffset? CredentialsSentAt { get; private set; }

    /// <summary>Hạn mật khẩu tạm đang dùng; null khi chưa gửi hoặc đã kích hoạt.</summary>
    public DateTimeOffset? TempPasswordExpiresAt { get; private set; }

    public DateTimeOffset? ActivatedAt { get; private set; }

    /// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-006: chỉ đọc; chỉ các hàm nghiệp vụ của Student thêm dòng.</summary>
    public IReadOnlyList<StudentAccountEvent> AccountEvents => _accountEvents.AsReadOnly();

    /// <summary>Trạng thái tài khoản tại thời điểm <paramref name="now"/>; mật khẩu tạm hết hạn khi now &gt;= hạn.</summary>
    public AccountState GetAccountState(DateTimeOffset now)
    {
        if (AccountStatus == AccountStatus.Activated)
            return AccountState.Activated;
        if (CredentialsSentAt is null)
            return AccountState.NotSent;
        return TempPasswordExpiresAt is { } expiresAt && now >= expiresAt
            ? AccountState.TempPasswordExpired
            : AccountState.Sent;
    }
```

(c) không đổi `Create`: trường mới mặc định null, danh sách rỗng.

- [x] **Step 5: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students.StudentAccountTests"`
Expected: `Passed: 2`, `Failed: 0`.

- [x] **Step 6: Viết test DTO, 503 và schema**

`backend/tests/Coaching.Application.Tests/Students/StudentDetailDtoTests.cs`:
```csharp
using Coaching.Application.Students;
using Coaching.Domain.Coaches;
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Tests.Students;

public sealed class StudentDetailDtoTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 2, 0, 0, TimeSpan.Zero);

    // @ADR-f01-p1-tai-khoan-hoc-vien-006: hồ sơ trả kèm trạng thái + lịch sử tài khoản (rỗng khi mới tạo)
    [Fact]
    public void Detail_contains_account_info_of_new_student()
    {
        var coach = Coach.Create(Guid.NewGuid(), "Coach Duobexiuuu", Email.Create("coach@duobexiuuu.vn"));
        var student = Student.Create(coach.Id, Guid.NewGuid(), "Lan", Email.Create("lan@gmail.com"), Now);

        var dto = StudentDetailDto.From(student, coach, Now);

        Assert.Equal("NotSent", dto.Account.State);
        Assert.Null(dto.Account.CredentialsSentAt);
        Assert.Null(dto.Account.TempPasswordExpiresAt);
        Assert.Null(dto.Account.ActivatedAt);
        Assert.Empty(dto.Account.History);
    }
}
```

`backend/tests/Coaching.Api.IntegrationTests/Common/ResultExtensionsTests.cs`:
```csharp
using Coaching.Api.Common;
using Coaching.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.IntegrationTests.Common;

public sealed class ResultExtensionsTests
{
    [Fact]
    public void Unavailable_error_maps_to_503_problem_with_code()
    {
        var error = new Error("account.email_send_failed", "Chưa gửi được email. Vui lòng thử lại sau.", ErrorKind.Unavailable);

        var result = Assert.IsType<ObjectResult>(error.ToProblem(new StubController()));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem.Status);
        Assert.Equal("account.email_send_failed", problem.Extensions["code"]);
        Assert.Equal("Chưa gửi được email. Vui lòng thử lại sau.", problem.Title);
    }

    private sealed class StubController : ControllerBase;
}
```

`backend/tests/Coaching.Api.IntegrationTests/Students/AccountSchemaTests.cs`:
```csharp
using System.Net.Http.Json;
using Coaching.Application.Students;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Students;

[Collection(ApiCollection.Name)]
public sealed class AccountSchemaTests(ApiFactory factory)
{
    // @ADR-f01-p1-tai-khoan-hoc-vien-006: tạo + đọc lại hồ sơ đi qua cột mới và bảng StudentAccountEvents
    [Fact]
    public async Task New_student_detail_has_not_sent_account_and_empty_history()
    {
        var admin = await factory.AdminClientAsync();
        var created = await StudentRequests.CreateAsync(admin, "Lan");

        Assert.Equal("NotSent", created.Account.State);
        Assert.Empty(created.Account.History);

        var loaded = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{created.Id}"))!;
        Assert.Equal("NotSent", loaded.Account.State);
        Assert.Null(loaded.Account.CredentialsSentAt);
        Assert.Null(loaded.Account.TempPasswordExpiresAt);
        Assert.Null(loaded.Account.ActivatedAt);
        Assert.Empty(loaded.Account.History);
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-006: changeset v0.3.0 tạo đủ bảng/cột (AccountTokens dùng ở Task 3)
    [Fact]
    public async Task Changelog_v0_3_0_creates_account_tables_and_columns()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        async Task<List<string>> ColumnsOf(string table) => await db.Database
            .SqlQuery<string>($"SELECT COLUMN_NAME AS [Value] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = {table}")
            .ToListAsync();

        var students = await ColumnsOf("Students");
        Assert.Contains("CredentialsSentAt", students);
        Assert.Contains("TempPasswordExpiresAt", students);
        Assert.Contains("ActivatedAt", students);
        Assert.Equal(
            new[] { "ActorUserId", "At", "FromEmail", "Id", "StudentId", "ToEmail", "Type" },
            (await ColumnsOf("StudentAccountEvents")).Order().ToList());
        Assert.Equal(
            new[] { "ConsumedAt", "CreatedAt", "ExpiresAt", "Id", "Purpose", "RevokedAt", "Source", "TokenHash", "UserId" },
            (await ColumnsOf("AccountTokens")).Order().ToList());
    }
}
```

- [x] **Step 7: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Application.Tests --filter "FullyQualifiedName~StudentDetailDtoTests"`
Expected: FAIL — build error `No overload for method 'From' takes 3 arguments` và `The type or namespace name 'AccountInfoDto' could not be found`.
Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~AccountSchemaTests|FullyQualifiedName~ResultExtensionsTests"`
Expected: FAIL — build error `'ErrorKind' does not contain a definition for 'Unavailable'` và `'StudentDetailDto' does not contain a definition for 'Account'`.

- [x] **Step 8: Application + Api — `ErrorKind.Unavailable` → 503, `AccountInfoDto`, `From(student, coach, now)` ở mọi caller**

`backend/src/Coaching.Application/Common/Result.cs` — thay enum `ErrorKind`:
```csharp
public enum ErrorKind
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,

    /// <summary>Dịch vụ phụ thuộc (email, cấu hình) chưa sẵn sàng → 503.</summary>
    Unavailable,
}
```

`backend/src/Coaching.Api/Common/ResultExtensions.cs` — trong `switch` của `ToProblem`, thêm trước dòng `_ => StatusCodes.Status400BadRequest,`:
```csharp
            ErrorKind.Unavailable => StatusCodes.Status503ServiceUnavailable,
```

`backend/src/Coaching.Application/Students/StudentDtos.cs` — thay toàn bộ:
```csharp
using Coaching.Domain.Coaches;
using Coaching.Domain.Students;

namespace Coaching.Application.Students;

/// <summary>Một dòng lịch sử trạng thái coaching (@BR-f02-ho-so-hoc-vien-020).</summary>
public sealed record StatusChangeDto(string From, string To, string Actor, DateTimeOffset At, string Reason);

/// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-006: một dòng lịch sử tài khoản — không có mật khẩu hay token.</summary>
public sealed record AccountEventDto(string Type, DateTimeOffset At, string? FromEmail, string? ToEmail);

/// <summary>Trạng thái + lịch sử tài khoản đăng nhập. History xếp mới nhất trước.</summary>
public sealed record AccountInfoDto(
    string State,
    DateTimeOffset? CredentialsSentAt,
    DateTimeOffset? TempPasswordExpiresAt,
    DateTimeOffset? ActivatedAt,
    IReadOnlyList<AccountEventDto> History)
{
    public static AccountInfoDto From(Student student, DateTimeOffset now) => new(
        student.GetAccountState(now).ToString(),
        student.CredentialsSentAt,
        student.TempPasswordExpiresAt,
        student.ActivatedAt,
        student.AccountEvents // @ADR-f01-p1-tai-khoan-hoc-vien-006: mới nhất trước
            .OrderByDescending(e => e.At)
            .ThenByDescending(e => e.Id)
            .Select(e => new AccountEventDto(e.Type.ToString(), e.At, e.FromEmail, e.ToEmail))
            .ToList());
}

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
    IReadOnlyList<StatusChangeDto> StatusHistory,
    AccountInfoDto Account)
{
    /// <param name="now">Thời điểm hiện tại (IClock.UtcNow) — để tính mật khẩu tạm đã hết hạn chưa.</param>
    public static StudentDetailDto From(Student student, Coach coach, DateTimeOffset now) => new(
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
            .ToList(),
        AccountInfoDto.From(student, now)); // @ADR-f01-p1-tai-khoan-hoc-vien-006

    /// <summary>MVP chỉ có coach sở hữu hồ sơ thao tác trên hồ sơ.</summary>
    public static string ActorName(Guid userId, Coach coach) =>
        userId == coach.UserId ? coach.DisplayName : "Người dùng khác";
}
```

`backend/src/Coaching.Application/Students/Queries/GetStudent.cs` — thay toàn bộ:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Queries;

public sealed class GetStudentHandler(CurrentCoach currentCoach, IStudentRepository students, IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;

        var student = await students.GetAsync(id, coach.Id, cancellationToken);
        return student is null
            ? StudentErrors.NotFound
            : Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach, clock.UtcNow));
    }
}
```

Bốn handler còn lại đã inject `IClock clock` — chỉ đổi lời gọi:
- `backend/src/Coaching.Application/Students/Commands/CreateStudent.cs`: `StudentDetailDto.From(student, coach)` → `StudentDetailDto.From(student, coach, now)` (biến `now` đã có trong hàm).
- `backend/src/Coaching.Application/Students/Commands/UpdateStudent.cs`: `StudentDetailDto.From(student, coach)` → `StudentDetailDto.From(student, coach, clock.UtcNow)`.
- `backend/src/Coaching.Application/Students/Commands/UpdateInternalNote.cs`: `StudentDetailDto.From(student, coach)` → `StudentDetailDto.From(student, coach, clock.UtcNow)`.
- `backend/src/Coaching.Application/Students/Commands/ChangeCoachingStatus.cs`: `StudentDetailDto.From(student, coach)` → `StudentDetailDto.From(student, coach, clock.UtcNow)`.

Kiểm không còn caller cũ:
Run: `grep -rn "StudentDetailDto.From(student, coach)" backend/src backend/tests`
Expected: không có dòng nào.

- [x] **Step 9: Infrastructure — map `AccountEvents` vào `StudentAccountEvents`**

`backend/src/Coaching.Infrastructure/Persistence/Configurations/StudentConfiguration.cs` — thêm ngay sau dòng `builder.Navigation(s => s.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);`:
```csharp

        // @ADR-f01-p1-tai-khoan-hoc-vien-006: lịch sử tài khoản là entity con, bảng riêng, chỉ thêm
        builder.OwnsMany(s => s.AccountEvents, e =>
        {
            e.ToTable("StudentAccountEvents");
            e.WithOwner().HasForeignKey("StudentId");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.FromEmail).HasMaxLength(Email.MaxLength);
            e.Property(x => x.ToEmail).HasMaxLength(Email.MaxLength);
            e.HasIndex("StudentId", nameof(StudentAccountEvent.At));
        });
        builder.Navigation(s => s.AccountEvents).UsePropertyAccessMode(PropertyAccessMode.Field);
```
(`CredentialsSentAt`, `TempPasswordExpiresAt`, `ActivatedAt` map theo convention — `datetimeoffset NULL`; `TempPasswordLifetime` là static nên EF bỏ qua.)

- [x] **Step 10: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Application.Tests --filter "FullyQualifiedName~StudentDetailDtoTests"`
Expected: `Passed: 1`, `Failed: 0`.
Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~AccountSchemaTests|FullyQualifiedName~ResultExtensionsTests"`
Expected: `Passed: 3`, `Failed: 0`.
Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed — mọi test F00/F02 (Domain, Application, Integration, Acceptance) vẫn xanh; scenario F01-P1 chưa có (`.feature` của F01-P1 nằm ngoài acceptance cho tới khi bind — nếu đã include thì chỉ được fail `MissingStepDefinition`, theo interim rule).

- [x] **Step 11: Commit**
```bash
git add database/changelog backend/src backend/tests/Coaching.Domain.Tests/Students/StudentAccountTests.cs backend/tests/Coaching.Application.Tests/Students backend/tests/Coaching.Api.IntegrationTests/Students/AccountSchemaTests.cs backend/tests/Coaching.Api.IntegrationTests/Common
git commit -F - <<'EOF'
refactor(students): schema v0.3.0, account fields + account history entity, AccountInfoDto, 503 errors (ADR-f01-p1-006)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 2: Hạ tầng email — provider `Capture` + `/api/test/emails` + 2 chốt, hàng đợi, `AccountEmails` + `App:ClientBaseUrl`, `Log` an toàn ở Production

**Files:**
- Create: `backend/src/Coaching.Application/Abstractions/IEmailQueue.cs`
- Create: `backend/src/Coaching.Application/Accounts/IAccountEmails.cs`, `backend/src/Coaching.Application/Accounts/AccountErrors.cs`
- Create: `backend/src/Coaching.Infrastructure/Emailing/EmailProviders.cs`, `EmailOutbox.cs`, `CapturingEmailSender.cs`, `QueuedEmailSender.cs`, `EmailQueueWorker.cs`, `AppOptions.cs`, `AccountEmails.cs`
- Modify: `backend/src/Coaching.Infrastructure/Emailing/LoggingEmailSender.cs`, `backend/src/Coaching.Infrastructure/Emailing/EmailOptions.cs`, `backend/src/Coaching.Infrastructure/DependencyInjection.cs`, `backend/src/Coaching.Infrastructure/Coaching.Infrastructure.csproj`
- Create: `backend/src/Coaching.Api/Controllers/TestEmailsController.cs`, `backend/src/Coaching.Api/Common/ExcludeControllersFeatureProvider.cs`
- Modify: `backend/src/Coaching.Api/Program.cs`, `backend/src/Coaching.Api/appsettings.json`, `backend/src/Coaching.Api/appsettings.Development.json`
- Test: `backend/tests/Coaching.Api.IntegrationTests/ApiFactory.cs` (M)
- Test: `backend/tests/Coaching.Api.IntegrationTests/Emailing/CaptureProviderTests.cs`, `Emailing/EmailQueueTests.cs`, `Emailing/AccountEmailsTests.cs`, `Emailing/LoggingEmailSenderTests.cs`

**Interfaces:**
- Consumes: `IEmailSender`/`EmailMessage(To, Subject, HtmlBody, TextBody?)`, `EmailOptions`, `IClock.UtcNow`, `VietnamClock.Offset`, `Error`/`ErrorKind.Unavailable`/`Result<T>` (Task 1), `ApiFactory`, `ApiCollection`.
- Produces:
  - Application: `IEmailQueue { ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default); }`; `IAccountEmails { Result<EmailMessage> Credentials(string toEmail, string fullName, string temporaryPassword, DateTimeOffset expiresAt); Result<EmailMessage> PasswordReset(string toEmail, string fullName, string rawToken); }`; `AccountErrors.ClientUrlMissing` + `AccountErrors.EmailSendFailed` (file `Accounts/AccountErrors.cs` tạo ở đây; Task 4 thêm `AlreadyActivated`, `RequiredFieldsMissing(int)`, Task 6 `EmailLocked`, Task 7 `NotActivated`).
  - Infrastructure: `CapturedEmail(string To, string Subject, string TextBody, string HtmlBody, DateTimeOffset SentAt)`; `EmailOutbox` (singleton: `All`, `FailNext`, `Clear()`, `WaitForAsync(string to, int count = 1, TimeSpan? timeout = null)`, `static IsTo(CapturedEmail, string)` so email không phân biệt hoa thường); `CapturingEmailSender : IEmailSender`; `QueuedEmailSender : IEmailQueue` + `EmailQueueWorker : BackgroundService`; `AppOptions { const SectionName = "App"; string? ClientBaseUrl }`; `AccountEmails : IAccountEmails` (`const CredentialsSubject`, `const PasswordResetSubject`); `EmailProviders { Log, Resend, Capture, Resolve(IConfiguration), IsCapture(IConfiguration) }`; `AddInfrastructure(IServiceCollection, IConfiguration, IHostEnvironment)`.
  - API: `GET /api/test/emails?to=` → `200 [{ to, subject, textBody, sentAt }]`, chỉ tồn tại khi `Email:Provider=Capture`; `Capture` + `Production` → dừng khởi động.
  - Test: `ApiFactory.ClientBaseUrl` (`"http://client.test"`), `ApiFactory.Outbox`; `ApiFactory` mặc định `Email:Provider=Capture`.

**Owns:** — | `@ADR-f01-p1-tai-khoan-hoc-vien-007`, `@ADR-f01-p1-tai-khoan-hoc-vien-008`

- [x] **Step 1: Viết integration test — provider Capture, hai chốt, endpoint test**

`backend/tests/Coaching.Api.IntegrationTests/ApiFactory.cs` — thêm `using Coaching.Infrastructure.Emailing;` vào đầu file; thêm sau dòng `public const string AdminPassword = "Admin@12345";`:
```csharp
    public const string ClientBaseUrl = "http://client.test";

    /// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-007: hộp thư trong bộ nhớ của host test.</summary>
    public EmailOutbox Outbox => Services.GetRequiredService<EmailOutbox>();
```
rồi thay dòng `builder.UseSetting("Email:Provider", "Log");` bằng:
```csharp
        // @ADR-f01-p1-tai-khoan-hoc-vien-007: test đọc mật khẩu tạm / link đặt lại từ hộp thư trong bộ nhớ
        builder.UseSetting("Email:Provider", "Capture");
        // @ADR-f01-p1-tai-khoan-hoc-vien-008
        builder.UseSetting("App:ClientBaseUrl", ClientBaseUrl);
```

`backend/tests/Coaching.Api.IntegrationTests/Emailing/CaptureProviderTests.cs`:
```csharp
using System.Net;
using System.Text.Json;
using Coaching.Application.Abstractions;
using Coaching.Infrastructure.Emailing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Emailing;

[Collection(ApiCollection.Name)]
public sealed class CaptureProviderTests(ApiFactory factory)
{
    private static string NewAddress() => $"mail-{Guid.NewGuid():N}@test.local";

    private async Task SendAsync(string to, string subject)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IEmailSender>()
            .SendAsync(new EmailMessage(to, subject, $"<p>{subject}</p>", $"Thân thư {subject}"));
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-007: chốt 1 — Capture ở Production thì API không khởi động
    [Fact]
    public void Capture_provider_in_production_stops_startup()
    {
        var ex = Assert.ThrowsAny<Exception>(() => factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.UseSetting("Email:Provider", "Capture");
        }).CreateClient());

        Assert.Contains("Email:Provider=Capture", ex.ToString());
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-007: chốt 2 — provider khác Capture thì không có endpoint đọc thư
    [Theory]
    [InlineData("Log")]
    [InlineData("Resend")]
    public async Task Test_emails_endpoint_is_absent_for_other_providers(string provider)
    {
        await using var host = factory.WithWebHostBuilder(b => b.UseSetting("Email:Provider", provider));

        var response = await host.CreateClient().GetAsync("/api/test/emails?to=a@test.local");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(host.Services.GetService<EmailOutbox>());
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-007: e2e đọc thư theo người nhận, không cần đăng nhập, không trả HTML
    [Fact]
    public async Task Test_emails_endpoint_lists_captured_mail_of_recipient()
    {
        var to = NewAddress();
        await SendAsync(to, "Thư một");
        await SendAsync(NewAddress(), "Thư người khác");
        await SendAsync(to.ToUpperInvariant(), "Thư hai");

        var response = await factory.CreateClient().GetAsync($"/api/test/emails?to={Uri.EscapeDataString(to)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var mails = json.RootElement.EnumerateArray().ToList();
        Assert.Equal(new[] { "Thư một", "Thư hai" }, mails.Select(m => m.GetProperty("subject").GetString()).ToList());
        Assert.Equal("Thân thư Thư một", mails[0].GetProperty("textBody").GetString());
        Assert.Equal(to, mails[0].GetProperty("to").GetString());
        Assert.True(mails[0].TryGetProperty("sentAt", out _));
        Assert.False(mails[0].TryGetProperty("htmlBody", out _));
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-007: FailNext mô phỏng dịch vụ email lỗi đúng một lần
    [Fact]
    public async Task FailNext_fails_exactly_one_send_and_captures_nothing()
    {
        var to = NewAddress();
        factory.Outbox.FailNext = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => SendAsync(to, "Lỗi"));
        Assert.False(factory.Outbox.FailNext);
        Assert.DoesNotContain(factory.Outbox.All, m => m.To == to);

        await SendAsync(to, "Lần sau");
        var mail = await factory.Outbox.WaitForAsync(to);
        Assert.Equal("Lần sau", mail.Subject);
    }
}
```

`backend/tests/Coaching.Api.IntegrationTests/Emailing/EmailQueueTests.cs`:
```csharp
using Coaching.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Emailing;

[Collection(ApiCollection.Name)]
public sealed class EmailQueueTests(ApiFactory factory)
{
    private static string NewAddress() => $"queue-{Guid.NewGuid():N}@test.local";

    private ValueTask EnqueueAsync(string to, string subject) =>
        factory.Services.GetRequiredService<IEmailQueue>().EnqueueAsync(new EmailMessage(to, subject, "<p>x</p>", "x"));

    // Hàng đợi trong process: thư được gửi nền qua IEmailSender đang cấu hình
    [Fact]
    public async Task Enqueued_mail_is_sent_in_background()
    {
        var to = NewAddress();

        await EnqueueAsync(to, "Thư hàng đợi");

        var mail = await factory.Outbox.WaitForAsync(to);
        Assert.Equal("Thư hàng đợi", mail.Subject);
    }

    // Gửi lỗi chỉ mất thư đó (log Warning), worker vẫn chạy tiếp
    [Fact]
    public async Task Failed_mail_is_dropped_and_worker_keeps_sending()
    {
        var failed = NewAddress();
        var next = NewAddress();
        factory.Outbox.FailNext = true;

        await EnqueueAsync(failed, "Sẽ lỗi");
        await EnqueueAsync(next, "Vẫn gửi");

        Assert.Equal("Vẫn gửi", (await factory.Outbox.WaitForAsync(next)).Subject);
        Assert.DoesNotContain(factory.Outbox.All, m => m.To == failed);
    }
}
```

`backend/tests/Coaching.Api.IntegrationTests/Emailing/AccountEmailsTests.cs`:
```csharp
using Coaching.Application.Accounts;
using Coaching.Infrastructure.Emailing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Coaching.Api.IntegrationTests.Emailing;

[Collection(ApiCollection.Name)]
public sealed class AccountEmailsTests(ApiFactory factory)
{
    private static AccountEmails Emails(string? baseUrl) =>
        new(Options.Create(new AppOptions { ClientBaseUrl = baseUrl }));

    // @ADR-f01-p1-tai-khoan-hoc-vien-008: địa chỉ đăng nhập lấy từ App:ClientBaseUrl; hạn 72 giờ ghi theo giờ VN
    [Fact]
    public void Credentials_email_has_login_url_account_and_temp_password()
    {
        var expiresAt = new DateTimeOffset(2026, 11, 28, 2, 0, 0, TimeSpan.Zero); // 09:00 28/11 giờ VN

        var result = Emails("https://hv.duobexiuuu.vn/").Credentials("lan@gmail.com", "Lan", "K7m-PqR4-tW9x", expiresAt);

        Assert.True(result.IsSuccess);
        var mail = result.Value;
        Assert.Equal("lan@gmail.com", mail.To);
        Assert.Equal("Thông tin đăng nhập Duobexiuuu Coaching", mail.Subject);
        Assert.Contains("Chào Lan,", mail.TextBody);
        Assert.Contains("https://hv.duobexiuuu.vn/login", mail.TextBody);
        Assert.DoesNotContain("vn//login", mail.TextBody);
        Assert.Contains("Email: lan@gmail.com", mail.TextBody);
        Assert.Contains("Mật khẩu tạm: K7m-PqR4-tW9x", mail.TextBody);
        Assert.Contains("Mật khẩu tạm có hiệu lực 72 giờ (đến 09:00 28/11/2026). Lần đầu đăng nhập, bạn sẽ đặt mật khẩu riêng.", mail.TextBody);
        Assert.Contains("Coach Duobexiuuu", mail.TextBody);
        Assert.Contains("https://hv.duobexiuuu.vn/login", mail.HtmlBody);
        Assert.Contains("K7m-PqR4-tW9x", mail.HtmlBody);
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-008: link đặt lại dạng <ClientBaseUrl>/dat-lai-mat-khau#t=<token>
    [Fact]
    public void Password_reset_email_has_fragment_link_and_30_minute_notice()
    {
        var result = Emails("https://hv.duobexiuuu.vn").PasswordReset("lan@gmail.com", "Lan", "abc_DEF-123");

        Assert.True(result.IsSuccess);
        var mail = result.Value;
        Assert.Equal("Đặt lại mật khẩu Duobexiuuu Coaching", mail.Subject);
        Assert.Contains("https://hv.duobexiuuu.vn/dat-lai-mat-khau#t=abc_DEF-123", mail.TextBody);
        Assert.Contains("Link có hiệu lực 30 phút và chỉ dùng được một lần. Nếu bạn không yêu cầu, hãy bỏ qua email này.", mail.TextBody);
        Assert.Contains("https://hv.duobexiuuu.vn/dat-lai-mat-khau#t=abc_DEF-123", mail.HtmlBody);
    }

    // @NFR-f01-p1-tai-khoan-hoc-vien-003 @ADR-f01-p1-tai-khoan-hoc-vien-008: thiếu/hỏng cấu hình → không tạo thư, báo lỗi rõ
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("hv.duobexiuuu.vn")]
    [InlineData("ftp://hv.duobexiuuu.vn")]
    public void Missing_or_invalid_client_base_url_returns_client_url_missing(string? baseUrl)
    {
        var emails = Emails(baseUrl);

        Assert.Equal(AccountErrors.ClientUrlMissing,
            emails.Credentials("lan@gmail.com", "Lan", "K7m-PqR4-tW9x", DateTimeOffset.UnixEpoch).Error);
        Assert.Equal(AccountErrors.ClientUrlMissing, emails.PasswordReset("lan@gmail.com", "Lan", "abc").Error);
    }

    // @NFR-f01-p1-tai-khoan-hoc-vien-003: API vẫn khởi động khi thiếu App:ClientBaseUrl; IAccountEmails trả 503 client_url_missing
    [Fact]
    public async Task Host_without_client_base_url_starts_and_reports_client_url_missing()
    {
        await using var host = factory.WithWebHostBuilder(b => b.UseSetting("App:ClientBaseUrl", ""));
        await using var scope = host.Services.CreateAsyncScope();

        var result = scope.ServiceProvider.GetRequiredService<IAccountEmails>()
            .PasswordReset("lan@gmail.com", "Lan", "abc");

        Assert.Equal("account.client_url_missing", result.Error!.Code);
        Assert.Equal("Chưa cấu hình địa chỉ web học viên nên chưa gửi được email.", result.Error.Message);
        Assert.Equal(Coaching.Application.Common.ErrorKind.Unavailable, result.Error.Kind);
    }
}
```

`backend/tests/Coaching.Api.IntegrationTests/Emailing/LoggingEmailSenderTests.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Infrastructure.Emailing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Coaching.Api.IntegrationTests.Emailing;

public sealed class LoggingEmailSenderTests
{
    private static readonly EmailMessage Mail =
        new("lan@gmail.com", "Thông tin đăng nhập Duobexiuuu Coaching", "<p>Mật khẩu tạm: K7m-PqR4</p>", "Mật khẩu tạm: K7m-PqR4");

    // @NFR-f01-p1-tai-khoan-hoc-vien-001: ở Production chỉ ghi người nhận + tiêu đề, không ghi thân thư
    [Fact]
    public async Task Production_logs_recipient_and_subject_only()
    {
        var logger = new ListLogger();

        await new LoggingEmailSender(logger, new Env(Environments.Production)).SendAsync(Mail);

        var line = Assert.Single(logger.Messages);
        Assert.Contains("lan@gmail.com", line);
        Assert.Contains("Thông tin đăng nhập Duobexiuuu Coaching", line);
        Assert.DoesNotContain("K7m-PqR4", line);
    }

    // @NFR-f01-p1-tai-khoan-hoc-vien-001: môi trường thử ghi cả thân thư để kiểm tra
    [Fact]
    public async Task Non_production_logs_body_for_testing()
    {
        var logger = new ListLogger();

        await new LoggingEmailSender(logger, new Env(Environments.Development)).SendAsync(Mail);

        Assert.Contains("K7m-PqR4", Assert.Single(logger.Messages));
    }

    private sealed class ListLogger : ILogger<LoggingEmailSender>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }

    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "Coaching.Api";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
```

- [x] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Emailing"`
Expected: FAIL — build error `The type or namespace name 'EmailOutbox' could not be found`, `The type or namespace name 'IEmailQueue' could not be found`, `The type or namespace name 'Accounts' does not exist in the namespace 'Coaching.Application'`.

- [x] **Step 3: Application — port hàng đợi, port nội dung email, lỗi tài khoản**

`backend/src/Coaching.Application/Abstractions/IEmailQueue.cs`:
```csharp
namespace Coaching.Application.Abstractions;

/// <summary>Đẩy email vào hàng đợi trong process để gửi nền (người gọi không chờ, không biết kết quả gửi).</summary>
public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default);
}
```

`backend/src/Coaching.Application/Accounts/IAccountEmails.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Common;

namespace Coaching.Application.Accounts;

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-008: soạn email tài khoản (text + HTML) với địa chỉ web học viên từ cấu hình.
/// Thiếu cấu hình → AccountErrors.ClientUrlMissing, không tạo thư.
/// </summary>
public interface IAccountEmails
{
    Result<EmailMessage> Credentials(string toEmail, string fullName, string temporaryPassword, DateTimeOffset expiresAt);

    Result<EmailMessage> PasswordReset(string toEmail, string fullName, string rawToken);
}
```

`backend/src/Coaching.Application/Accounts/AccountErrors.cs`:
```csharp
using Coaching.Application.Common;

namespace Coaching.Application.Accounts;

/// <summary>Lỗi của thao tác tài khoản học viên. Task 4/6/7 thêm các lỗi nghiệp vụ.</summary>
public static class AccountErrors
{
    public static readonly Error EmailSendFailed =
        new("account.email_send_failed", "Chưa gửi được email. Vui lòng thử lại sau.", ErrorKind.Unavailable);

    // @NFR-f01-p1-tai-khoan-hoc-vien-003 @ADR-f01-p1-tai-khoan-hoc-vien-008
    public static readonly Error ClientUrlMissing =
        new("account.client_url_missing", "Chưa cấu hình địa chỉ web học viên nên chưa gửi được email.", ErrorKind.Unavailable);
}
```

- [x] **Step 4: Infrastructure — provider Capture, hộp thư, hàng đợi, nội dung email, Log an toàn**

`backend/src/Coaching.Infrastructure/Emailing/EmailProviders.cs`:
```csharp
using Microsoft.Extensions.Configuration;

namespace Coaching.Infrastructure.Emailing;

/// <summary>Giá trị hợp lệ của Email:Provider.</summary>
public static class EmailProviders
{
    public const string Log = "Log";
    public const string Resend = "Resend";

    /// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-007: lưu thư trong bộ nhớ, đọc qua GET /api/test/emails. Cấm ở Production.</summary>
    public const string Capture = "Capture";

    public static string Resolve(IConfiguration configuration) =>
        configuration.GetSection(EmailOptions.SectionName)[nameof(EmailOptions.Provider)] is { Length: > 0 } provider
            ? provider
            : Log;

    public static bool IsCapture(IConfiguration configuration) =>
        string.Equals(Resolve(configuration), Capture, StringComparison.OrdinalIgnoreCase);
}
```

`backend/src/Coaching.Infrastructure/Emailing/EmailOptions.cs` — thay doc comment của `Provider`:
```csharp
    /// <summary>"Log" (chỉ ghi log), "Resend" (gửi thật) hoặc "Capture" (dev/e2e: hộp thư trong bộ nhớ, cấm ở Production).</summary>
    public string Provider { get; set; } = "Log";
```

`backend/src/Coaching.Infrastructure/Emailing/EmailOutbox.cs`:
```csharp
namespace Coaching.Infrastructure.Emailing;

public sealed record CapturedEmail(string To, string Subject, string TextBody, string HtmlBody, DateTimeOffset SentAt);

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-007: hộp thư trong bộ nhớ của provider Capture (singleton, thread-safe).
/// FailNext = true → lần gửi kế tiếp ném lỗi rồi tự tắt (mô phỏng dịch vụ email lỗi).
/// </summary>
public sealed class EmailOutbox
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly Lock _gate = new();
    private readonly List<CapturedEmail> _emails = [];
    private TaskCompletionSource _changed = NewSignal();
    private bool _failNext;

    public IReadOnlyList<CapturedEmail> All
    {
        get
        {
            lock (_gate)
                return _emails.ToList();
        }
    }

    public bool FailNext
    {
        get
        {
            lock (_gate)
                return _failNext;
        }
        set
        {
            lock (_gate)
                _failNext = value;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _emails.Clear();
            _failNext = false;
        }
    }

    /// <summary>Chờ tới khi có ít nhất <paramref name="count"/> thư gửi tới <paramref name="to"/>; trả thư mới nhất.</summary>
    public async Task<CapturedEmail> WaitForAsync(string to, int count = 1, TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? DefaultTimeout);
        while (true)
        {
            Task changed;
            lock (_gate)
            {
                var matches = _emails.Where(e => IsTo(e, to)).ToList();
                if (matches.Count >= count)
                    return matches[^1];
                changed = _changed.Task;
            }

            try
            {
                await changed.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Không thấy đủ {count} email gửi tới {to} trong {(timeout ?? DefaultTimeout).TotalSeconds} giây.");
            }
        }
    }

    public static bool IsTo(CapturedEmail email, string to) =>
        string.Equals(email.To, to, StringComparison.OrdinalIgnoreCase);

    internal bool TryConsumeFailure()
    {
        lock (_gate)
        {
            if (!_failNext)
                return false;
            _failNext = false;
            return true;
        }
    }

    internal void Add(CapturedEmail email)
    {
        TaskCompletionSource changed;
        lock (_gate)
        {
            _emails.Add(email);
            changed = _changed;
            _changed = NewSignal();
        }
        changed.TrySetResult();
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
```

`backend/src/Coaching.Infrastructure/Emailing/CapturingEmailSender.cs`:
```csharp
using Coaching.Application.Abstractions;

namespace Coaching.Infrastructure.Emailing;

/// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-007: Email:Provider=Capture — không gửi ra ngoài, lưu vào EmailOutbox.</summary>
internal sealed class CapturingEmailSender(EmailOutbox outbox, IClock clock) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (outbox.TryConsumeFailure())
            throw new InvalidOperationException("Email:Provider=Capture mô phỏng dịch vụ email lỗi (FailNext).");

        outbox.Add(new CapturedEmail(message.To, message.Subject, message.TextBody ?? string.Empty, message.HtmlBody, clock.UtcNow));
        return Task.CompletedTask;
    }
}
```

`backend/src/Coaching.Infrastructure/Emailing/QueuedEmailSender.cs`:
```csharp
using System.Threading.Channels;
using Coaching.Application.Abstractions;

namespace Coaching.Infrastructure.Emailing;

/// <summary>Hàng đợi email trong process (singleton). Thư trong hàng đợi mất khi process khởi động lại.</summary>
internal sealed class QueuedEmailSender : IEmailQueue
{
    private readonly Channel<EmailMessage> _channel =
        Channel.CreateUnbounded<EmailMessage>(new UnboundedChannelOptions { SingleReader = true });

    internal ChannelReader<EmailMessage> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _channel.Writer.WriteAsync(message, ct);
    }
}
```

`backend/src/Coaching.Infrastructure/Emailing/EmailQueueWorker.cs`:
```csharp
using Coaching.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Coaching.Infrastructure.Emailing;

/// <summary>Gửi nền từng thư trong hàng đợi qua IEmailSender đang cấu hình. Lỗi → log Warning (không ghi thân thư), gửi tiếp thư sau.</summary>
internal sealed class EmailQueueWorker(QueuedEmailSender queue, IServiceScopeFactory scopes, ILogger<EmailQueueWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IEmailSender>().SendAsync(message, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Gửi email trong hàng đợi lỗi: To={To} Subject={Subject}", message.To, message.Subject);
            }
        }
    }
}
```

`backend/src/Coaching.Infrastructure/Emailing/AppOptions.cs`:
```csharp
namespace Coaching.Infrastructure.Emailing;

/// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-008: cấu hình "App" (Render: App__ClientBaseUrl).</summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>Địa chỉ public của web học viên, ví dụ "https://hv.duobexiuuu.vn". Trống → không gửi email tài khoản.</summary>
    public string? ClientBaseUrl { get; set; }
}
```

`backend/src/Coaching.Infrastructure/Emailing/AccountEmails.cs`:
```csharp
using System.Globalization;
using System.Net;
using Coaching.Application.Abstractions;
using Coaching.Application.Accounts;
using Coaching.Application.Common;
using Coaching.Infrastructure.Time;
using Microsoft.Extensions.Options;

namespace Coaching.Infrastructure.Emailing;

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-008: nội dung 2 email tài khoản (bản nháp, coach duyệt trước khi bật Resend —
/// NFR-f01-p1-tai-khoan-hoc-vien-005). Địa chỉ lấy từ App:ClientBaseUrl.
/// </summary>
internal sealed class AccountEmails(IOptions<AppOptions> options) : IAccountEmails
{
    public const string CredentialsSubject = "Thông tin đăng nhập Duobexiuuu Coaching";
    public const string PasswordResetSubject = "Đặt lại mật khẩu Duobexiuuu Coaching";
    private const string Signature = "Coach Duobexiuuu";

    public Result<EmailMessage> Credentials(string toEmail, string fullName, string temporaryPassword, DateTimeOffset expiresAt)
    {
        if (ClientBaseUrl() is not { } baseUrl)
            return AccountErrors.ClientUrlMissing; // @NFR-f01-p1-tai-khoan-hoc-vien-003

        var loginUrl = $"{baseUrl}/login";
        var expires = expiresAt.ToOffset(VietnamClock.Offset).ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture);
        const string lifetime = "Mật khẩu tạm có hiệu lực 72 giờ";
        const string firstLogin = "Lần đầu đăng nhập, bạn sẽ đặt mật khẩu riêng.";

        var text = string.Join('\n',
            $"Chào {fullName},",
            "",
            "Coach đã tạo tài khoản Duobexiuuu Coaching cho bạn.",
            "",
            $"Địa chỉ đăng nhập: {loginUrl}",
            $"Email: {toEmail}",
            $"Mật khẩu tạm: {temporaryPassword}",
            "",
            $"{lifetime} (đến {expires}). {firstLogin}",
            "",
            Signature);

        var html =
            $"<p>Chào {Encode(fullName)},</p>" +
            "<p>Coach đã tạo tài khoản Duobexiuuu Coaching cho bạn.</p>" +
            $"<p>Địa chỉ đăng nhập: <a href=\"{Encode(loginUrl)}\">{Encode(loginUrl)}</a><br>" +
            $"Email: {Encode(toEmail)}<br>" +
            $"Mật khẩu tạm: <strong>{Encode(temporaryPassword)}</strong></p>" +
            $"<p>{lifetime} (đến {expires}). {firstLogin}</p>" +
            $"<p>{Signature}</p>";

        return new EmailMessage(toEmail, CredentialsSubject, html, text);
    }

    public Result<EmailMessage> PasswordReset(string toEmail, string fullName, string rawToken)
    {
        if (ClientBaseUrl() is not { } baseUrl)
            return AccountErrors.ClientUrlMissing; // @NFR-f01-p1-tai-khoan-hoc-vien-003

        // Token nằm sau '#': không lên server, không lọt vào Referer/log
        var link = $"{baseUrl}/dat-lai-mat-khau#t={Uri.EscapeDataString(rawToken)}";
        const string notice = "Link có hiệu lực 30 phút và chỉ dùng được một lần. Nếu bạn không yêu cầu, hãy bỏ qua email này.";

        var text = string.Join('\n',
            $"Chào {fullName},",
            "",
            $"Bấm vào link sau để đặt mật khẩu mới cho tài khoản {toEmail}:",
            link,
            "",
            notice,
            "",
            Signature);

        var html =
            $"<p>Chào {Encode(fullName)},</p>" +
            $"<p>Bấm vào link sau để đặt mật khẩu mới cho tài khoản {Encode(toEmail)}:</p>" +
            $"<p><a href=\"{Encode(link)}\">{Encode(link)}</a></p>" +
            $"<p>{notice}</p>" +
            $"<p>{Signature}</p>";

        return new EmailMessage(toEmail, PasswordResetSubject, html, text);
    }

    /// <summary>Địa chỉ tuyệt đối http/https, bỏ dấu '/' cuối; trống hoặc hỏng → null (không gửi địa chỉ hỏng).</summary>
    private string? ClientBaseUrl()
    {
        var value = options.Value.ClientBaseUrl?.Trim().TrimEnd('/');
        return !string.IsNullOrEmpty(value)
            && Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? value
                : null;
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
```

> Ghi chú: test `Credentials_email_has_login_url_account_and_temp_password` kiểm câu "(đến 09:00 28/11/2026)" — định dạng `HH:mm dd/MM/yyyy` theo giờ VN.

`backend/src/Coaching.Infrastructure/Emailing/LoggingEmailSender.cs` — thay toàn bộ:
```csharp
using Coaching.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Coaching.Infrastructure.Emailing;

/// <summary>Không gửi email thật, chỉ ghi log.</summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger, IHostEnvironment environment) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        // @NFR-f01-p1-tai-khoan-hoc-vien-001: thân thư có mật khẩu tạm / link đặt lại → Production chỉ ghi người nhận + tiêu đề
        if (environment.IsProduction())
            logger.LogInformation("[Email:Log] To={To} Subject={Subject}", message.To, message.Subject);
        else
            logger.LogInformation("[Email:Log] To={To} Subject={Subject}\n{Body}",
                message.To, message.Subject, message.TextBody ?? message.HtmlBody);
        return Task.CompletedTask;
    }
}
```

`backend/src/Coaching.Infrastructure/Coaching.Infrastructure.csproj` — thêm trước `</Project>`:
```xml
  <ItemGroup>
    <InternalsVisibleTo Include="Coaching.Api.IntegrationTests" />
  </ItemGroup>
```

`backend/src/Coaching.Infrastructure/DependencyInjection.cs`:
- thêm `using Coaching.Application.Accounts;` và `using Microsoft.Extensions.Hosting;` vào đầu file;
- đổi chữ ký `public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)` thành:
```csharp
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment environment)
```
- đổi dòng `AddEmail(services, configuration);` thành `AddEmail(services, configuration, environment);`
- thay toàn bộ hàm `AddEmail` bằng:
```csharp
    private static void AddEmail(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var provider = EmailProviders.Resolve(configuration);
        if (EmailProviders.IsCapture(configuration))
        {
            // @ADR-f01-p1-tai-khoan-hoc-vien-007: chốt 1 — hộp thư đọc được qua API, không bao giờ bật ở Production
            if (environment.IsProduction())
                throw new InvalidOperationException(
                    "Email:Provider=Capture chỉ dùng cho dev/e2e, không được bật ở môi trường Production.");
            services.AddSingleton<EmailOutbox>();
            services.AddScoped<IEmailSender, CapturingEmailSender>();
        }
        else if (string.Equals(provider, EmailProviders.Resend, StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IEmailSender, ResendEmailSender>(client =>
                client.BaseAddress = new Uri("https://api.resend.com/"));
        }
        else
        {
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        }

        // Hàng đợi trong process cho email không cần chờ kết quả (Quên mật khẩu)
        services.AddSingleton<QueuedEmailSender>();
        services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<QueuedEmailSender>());
        services.AddHostedService<EmailQueueWorker>();

        // @ADR-f01-p1-tai-khoan-hoc-vien-008: thiếu App:ClientBaseUrl API vẫn chạy, chỉ lần gửi trả 503
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));
        services.AddSingleton<IAccountEmails, AccountEmails>();
    }
```

- [x] **Step 5: API — `TestEmailsController` chỉ map khi Capture, truyền môi trường vào Infrastructure, cấu hình `App`**

`backend/src/Coaching.Api/Common/ExcludeControllersFeatureProvider.cs`:
```csharp
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Coaching.Api.Common;

/// <summary>Bỏ controller khỏi MVC (route không tồn tại → 404). Chạy sau ControllerFeatureProvider mặc định.</summary>
internal sealed class ExcludeControllersFeatureProvider(params Type[] excluded) : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var type in excluded)
            feature.Controllers.Remove(type.GetTypeInfo());
    }
}
```

`backend/src/Coaching.Api/Controllers/TestEmailsController.cs`:
```csharp
using Coaching.Infrastructure.Emailing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers;

public sealed record TestEmailDto(string To, string Subject, string TextBody, DateTimeOffset SentAt);

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-007: e2e đọc thư của provider Capture. Chỉ được map khi Email:Provider=Capture
/// (Program.cs loại controller này ở mọi provider khác); Capture bị cấm ở Production.
/// </summary>
[ApiController]
[Route("api/test/emails")]
[AllowAnonymous]
public sealed class TestEmailsController(EmailOutbox outbox) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TestEmailDto>>(StatusCodes.Status200OK)]
    public IActionResult List([FromQuery] string? to) =>
        Ok(outbox.All
            .Where(e => to is null || EmailOutbox.IsTo(e, to))
            .Select(e => new TestEmailDto(e.To, e.Subject, e.TextBody, e.SentAt))
            .ToList());
}
```

`backend/src/Coaching.Api/Program.cs`:
- thêm `using Coaching.Api.Controllers;` và `using Coaching.Infrastructure.Emailing;` vào đầu file;
- đổi `builder.Services.AddInfrastructure(builder.Configuration);` thành:
```csharp
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
```
- đổi `builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>` thành:
```csharp
// @ADR-f01-p1-tai-khoan-hoc-vien-007: chốt 2 — /api/test/emails chỉ tồn tại khi Email:Provider=Capture
var captureEmails = EmailProviders.IsCapture(builder.Configuration);
builder.Services.AddControllers()
    .ConfigureApplicationPartManager(parts =>
    {
        if (!captureEmails)
            parts.FeatureProviders.Add(new ExcludeControllersFeatureProvider(typeof(TestEmailsController)));
    })
    .ConfigureApiBehaviorOptions(options =>
```
(phần thân lambda `InvalidModelStateResponseFactory` giữ nguyên.)

`backend/src/Coaching.Api/appsettings.json` — thêm sau khối `"Email": { … },`:
```json
  "App": {
    "ClientBaseUrl": ""
  },
```

`backend/src/Coaching.Api/appsettings.Development.json` — thêm sau khối `"DemoSeed": { … },`:
```json
  "App": {
    "ClientBaseUrl": "http://localhost:4300"
  },
```

- [x] **Step 6: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Emailing"`
Expected: `Passed: 16`, `Failed: 0` (CaptureProvider 5, EmailQueue 2, AccountEmails 7, LoggingEmailSender 2).
Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed (integration test cũ chạy với provider Capture; acceptance vẫn `Email:Provider=Log`).

- [x] **Step 7: Commit**
```bash
git add backend/src backend/tests/Coaching.Api.IntegrationTests/ApiFactory.cs backend/tests/Coaching.Api.IntegrationTests/Emailing
git commit -F - <<'EOF'
feat(email): capture provider with test endpoint and production guard, email queue, account email content (ADR-f01-p1-007, 008)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 3: Kho token tài khoản — `AccountToken`, cấu hình EF, `AccountTokenStore : IAccountTokens`

**Files:**
- Create: `backend/src/Coaching.Application/Accounts/AccountTokens.cs`
- Create: `backend/src/Coaching.Infrastructure/Identity/AccountToken.cs`, `backend/src/Coaching.Infrastructure/Identity/AccountTokenStore.cs`, `backend/src/Coaching.Infrastructure/Persistence/Configurations/AccountTokenConfiguration.cs`
- Modify: `backend/src/Coaching.Infrastructure/Identity/TokenHasher.cs`, `backend/src/Coaching.Infrastructure/Persistence/AppDbContext.cs`, `backend/src/Coaching.Infrastructure/DependencyInjection.cs`
- Test: `backend/tests/Coaching.Api.IntegrationTests/Accounts/AccountTokenStoreTests.cs`

**Interfaces:**
- Consumes: bảng `AccountTokens` (Task 1), `TokenHasher.Hash`, `IClock.UtcNow`, `AppDbContext`, `AppUser`, `ApiFactory` (`WithWebHostBuilder`), `InternalsVisibleTo` Infrastructure → IntegrationTests (Task 2).
- Produces:
  - Application `Coaching.Application.Accounts`: `enum AccountTokenPurpose { Activation, PasswordReset }`, `enum AccountTokenSource { Login, Coach, Self }`, `IAccountTokens` (`IssueAsync`, `FindValidAsync`, `ConsumeAsync`, `RevokeAllAsync`, `LastIssuedAtAsync` — chữ ký đúng skeleton).
  - Infrastructure: `AccountToken` (entity, public setters như `RefreshToken`), `AppDbContext.AccountTokens`, `AccountTokenStore` (Scoped, ghi ngay bằng `SaveChangesAsync`/`ExecuteUpdateAsync` — chạy trong transaction của handler nếu có), `TokenHasher.NewToken(int byteCount = 64)`.

**Owns:** — | `@ADR-f01-p1-tai-khoan-hoc-vien-003`

- [x] **Step 1: Viết integration test cho kho token**

`backend/tests/Coaching.Api.IntegrationTests/Accounts/AccountTokenStoreTests.cs`:
```csharp
using System.Security.Cryptography;
using System.Text;
using Coaching.Application.Accounts;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Coaching.Api.IntegrationTests.Accounts;

[Collection(ApiCollection.Name)]
public sealed class AccountTokenStoreTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 11, 25, 2, 0, 0, TimeSpan.Zero);

    private readonly MutableTimeProvider _time = new(Start);
    private readonly WebApplicationFactory<Program> _host;

    public AccountTokenStoreTests(ApiFactory factory) =>
        _host = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(_time);
        }));

    public void Dispose() => _host.Dispose();

    // @ADR-f01-p1-tai-khoan-hoc-vien-003 @NFR-f01-p1-tai-khoan-hoc-vien-001: trả token gốc 32 byte base64url, DB chỉ có SHA-256
    [Fact]
    public async Task Issue_returns_raw_token_and_stores_only_its_hash()
    {
        var userId = await NewUserAsync();

        var raw = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.Activation, AccountTokenSource.Login, Start.AddMinutes(30)));

        Assert.Equal(43, raw.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", raw);
        var row = await Db(db => db.AccountTokens.AsNoTracking().SingleAsync(t => t.UserId == userId));
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))), row.TokenHash);
        Assert.NotEqual(raw, row.TokenHash);
        Assert.Equal(AccountTokenPurpose.Activation, row.Purpose);
        Assert.Equal(AccountTokenSource.Login, row.Source);
        Assert.Equal(Start, row.CreatedAt);
        Assert.Equal(Start.AddMinutes(30), row.ExpiresAt);
        Assert.Null(row.ConsumedAt);
        Assert.Null(row.RevokedAt);
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-003: hết hạn khi now >= ExpiresAt (đúng mốc là hết hạn)
    [Fact]
    public async Task Token_is_valid_until_just_before_expiry_and_invalid_at_expiry()
    {
        var userId = await NewUserAsync();
        var expiresAt = Start.AddMinutes(30);
        var raw = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self, expiresAt));

        _time.Set(expiresAt.AddSeconds(-1));
        Assert.Equal(userId, await Tokens(t => t.FindValidAsync(raw, AccountTokenPurpose.PasswordReset)));

        _time.Set(expiresAt);
        Assert.Null(await Tokens(t => t.FindValidAsync(raw, AccountTokenPurpose.PasswordReset)));
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-003: token chỉ hợp lệ cho đúng mục đích; token rỗng/lạ không hợp lệ
    [Fact]
    public async Task Wrong_purpose_blank_or_unknown_token_is_invalid()
    {
        var userId = await NewUserAsync();
        var raw = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.Activation, AccountTokenSource.Login, Start.AddMinutes(30)));

        Assert.Null(await Tokens(t => t.FindValidAsync(raw, AccountTokenPurpose.PasswordReset)));
        Assert.Null(await Tokens(t => t.FindValidAsync(null, AccountTokenPurpose.Activation)));
        Assert.Null(await Tokens(t => t.FindValidAsync("", AccountTokenPurpose.Activation)));
        Assert.Null(await Tokens(t => t.FindValidAsync("khong-phai-token", AccountTokenPurpose.Activation)));
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-003: dùng một lần — đã tiêu thì không hợp lệ; tiêu lại không lỗi, không đổi mốc
    [Fact]
    public async Task Consumed_token_is_invalid_and_consume_is_idempotent()
    {
        var userId = await NewUserAsync();
        var raw = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Coach, Start.AddMinutes(30)));

        await Tokens(t => t.ConsumeAsync(raw));
        _time.Set(Start.AddMinutes(1));
        await Tokens(t => t.ConsumeAsync(raw));

        Assert.Null(await Tokens(t => t.FindValidAsync(raw, AccountTokenPurpose.PasswordReset)));
        var row = await Db(db => db.AccountTokens.AsNoTracking().SingleAsync(t => t.UserId == userId));
        Assert.Equal(Start, row.ConsumedAt);
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-003: token mới hủy token cũ cùng user + mục đích, không đụng mục đích khác
    [Fact]
    public async Task Issuing_revokes_previous_live_token_of_same_purpose_only()
    {
        var userId = await NewUserAsync();
        var first = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Coach, Start.AddMinutes(30)));
        var activation = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.Activation, AccountTokenSource.Login, Start.AddMinutes(30)));
        _time.Set(Start.AddMinutes(5));

        var second = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self, Start.AddMinutes(35)));

        Assert.Null(await Tokens(t => t.FindValidAsync(first, AccountTokenPurpose.PasswordReset)));
        Assert.Equal(userId, await Tokens(t => t.FindValidAsync(second, AccountTokenPurpose.PasswordReset)));
        Assert.Equal(userId, await Tokens(t => t.FindValidAsync(activation, AccountTokenPurpose.Activation)));
        var revoked = await Db(db => db.AccountTokens.AsNoTracking()
            .SingleAsync(t => t.UserId == userId && t.Purpose == AccountTokenPurpose.PasswordReset && t.RevokedAt != null));
        Assert.Equal(Start.AddMinutes(5), revoked.RevokedAt);
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-003: hủy hết token của một mục đích (gửi lại thông tin, sửa email)
    [Fact]
    public async Task RevokeAll_revokes_only_the_given_purpose()
    {
        var userId = await NewUserAsync();
        var activation = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.Activation, AccountTokenSource.Login, Start.AddMinutes(30)));
        var reset = await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Coach, Start.AddMinutes(30)));

        await Tokens(t => t.RevokeAllAsync(userId, AccountTokenPurpose.Activation));

        Assert.Null(await Tokens(t => t.FindValidAsync(activation, AccountTokenPurpose.Activation)));
        Assert.Equal(userId, await Tokens(t => t.FindValidAsync(reset, AccountTokenPurpose.PasswordReset)));
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-003: thời điểm phát token gần nhất theo user + mục đích + nguồn (dùng cho giới hạn 60 giây)
    [Fact]
    public async Task LastIssuedAt_returns_latest_issue_time_per_purpose_and_source()
    {
        var userId = await NewUserAsync();
        Assert.Null(await Tokens(t => t.LastIssuedAtAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self)));

        await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self, Start.AddMinutes(30)));
        _time.Set(Start.AddMinutes(2));
        await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Coach, Start.AddMinutes(32)));

        Assert.Equal(Start, await Tokens(t => t.LastIssuedAtAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self)));
        Assert.Equal(Start.AddMinutes(2), await Tokens(t => t.LastIssuedAtAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Coach)));

        _time.Set(Start.AddMinutes(3));
        await Tokens(t => t.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self, Start.AddMinutes(33)));
        Assert.Equal(Start.AddMinutes(3), await Tokens(t => t.LastIssuedAtAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self)));
        Assert.Null(await Tokens(t => t.LastIssuedAtAsync(userId, AccountTokenPurpose.Activation, AccountTokenSource.Self)));
    }

    private async Task<T> Tokens<T>(Func<IAccountTokens, Task<T>> action)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<IAccountTokens>());
    }

    private async Task Tokens(Func<IAccountTokens, Task> action)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<IAccountTokens>());
    }

    private async Task<T> Db<T>(Func<AppDbContext, Task<T>> query)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private async Task<Guid> NewUserAsync()
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var email = $"token-{Guid.NewGuid():N}@test.local";
        var user = new AppUser { UserName = email, Email = email, FullName = "Token Test" };
        var created = await users.CreateAsync(user);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        return user.Id;
    }

    private sealed class MutableTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public void Set(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
```

- [x] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~AccountTokenStoreTests"`
Expected: FAIL — build error `The type or namespace name 'IAccountTokens' could not be found`, `The type or namespace name 'AccountTokenPurpose' could not be found`, `'AppDbContext' does not contain a definition for 'AccountTokens'`.

- [x] **Step 3: Application — port `IAccountTokens`**

`backend/src/Coaching.Application/Accounts/AccountTokens.cs`:
```csharp
namespace Coaching.Application.Accounts;

public enum AccountTokenPurpose
{
    Activation,
    PasswordReset,
}

/// <summary>Ai khiến token được phát: đăng nhập bằng mật khẩu tạm, coach bấm gửi, học viên tự xin.</summary>
public enum AccountTokenSource
{
    Login,
    Coach,
    Self,
}

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-003: mã kích hoạt + link đặt lại. Chỉ lưu SHA-256; dùng một lần;
/// phát token mới thì token còn hiệu lực cùng user + mục đích bị hủy; hết hạn khi now &gt;= ExpiresAt.
/// Ghi ngay vào DB — gọi trong IUnitOfWork.ExecuteInTransactionAsync nếu cần đi cùng thay đổi khác.
/// </summary>
public interface IAccountTokens
{
    /// <summary>Hủy mọi token còn hiệu lực cùng user + mục đích, phát token mới; trả token gốc (32 byte ngẫu nhiên, base64url).</summary>
    Task<string> IssueAsync(Guid userId, AccountTokenPurpose purpose, AccountTokenSource source, DateTimeOffset expiresAt,
        CancellationToken ct = default);

    /// <summary>UserId nếu token đúng mục đích, chưa tiêu, chưa hủy và now &lt; ExpiresAt; ngược lại null. Không tiêu token.</summary>
    Task<Guid?> FindValidAsync(string? rawToken, AccountTokenPurpose purpose, CancellationToken ct = default);

    /// <summary>Đánh dấu đã dùng (lần đầu); gọi lại không đổi gì.</summary>
    Task ConsumeAsync(string rawToken, CancellationToken ct = default);

    Task RevokeAllAsync(Guid userId, AccountTokenPurpose purpose, CancellationToken ct = default);

    /// <summary>Thời điểm phát token gần nhất theo user + mục đích + nguồn; null nếu chưa phát lần nào.</summary>
    Task<DateTimeOffset?> LastIssuedAtAsync(Guid userId, AccountTokenPurpose purpose, AccountTokenSource source,
        CancellationToken ct = default);
}
```

- [x] **Step 4: Infrastructure — entity, cấu hình EF, store, đăng ký DI**

`backend/src/Coaching.Infrastructure/Identity/TokenHasher.cs` — thay dòng `public static string NewToken() => Base64UrlEncode(RandomNumberGenerator.GetBytes(64));` bằng:
```csharp
    /// <summary>Token ngẫu nhiên base64url. Refresh token giữ 64 byte; token tài khoản dùng 32 byte.</summary>
    public static string NewToken(int byteCount = 64) => Base64UrlEncode(RandomNumberGenerator.GetBytes(byteCount));
```

`backend/src/Coaching.Infrastructure/Identity/AccountToken.cs`:
```csharp
using Coaching.Application.Accounts;

namespace Coaching.Infrastructure.Identity;

/// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-003: mã kích hoạt / link đặt lại — chỉ lưu hash SHA-256 (hex 64 ký tự).</summary>
public sealed class AccountToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UserId { get; set; }

    public AccountTokenPurpose Purpose { get; set; }

    public AccountTokenSource Source { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}
```

`backend/src/Coaching.Infrastructure/Persistence/Configurations/AccountTokenConfiguration.cs`:
```csharp
using Coaching.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Persistence.Configurations;

// @ADR-f01-p1-tai-khoan-hoc-vien-003: bảng AccountTokens tạo ở changeset v0.3.0-0001
internal sealed class AccountTokenConfiguration : IEntityTypeConfiguration<AccountToken>
{
    public void Configure(EntityTypeBuilder<AccountToken> builder)
    {
        builder.ToTable("AccountTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Purpose).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Source).HasConversion<string>().HasMaxLength(10);
        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => new { t.UserId, t.Purpose });
        builder.HasOne<AppUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
```

`backend/src/Coaching.Infrastructure/Persistence/AppDbContext.cs` — thêm sau dòng `public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();`:
```csharp

    public DbSet<AccountToken> AccountTokens => Set<AccountToken>();
```

`backend/src/Coaching.Infrastructure/Identity/AccountTokenStore.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Accounts;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Identity;

/// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-003: kho token tài khoản dùng chung cho kích hoạt và đặt lại mật khẩu.</summary>
internal sealed class AccountTokenStore(AppDbContext db, IClock clock) : IAccountTokens
{
    public const int TokenBytes = 32;

    public async Task<string> IssueAsync(Guid userId, AccountTokenPurpose purpose, AccountTokenSource source,
        DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        // @ADR-f01-p1-tai-khoan-hoc-vien-003: có token mới thì token cũ cùng mục đích hết hiệu lực
        await LiveTokens(userId, purpose, now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, (DateTimeOffset?)now), ct);

        var raw = TokenHasher.NewToken(TokenBytes);
        db.AccountTokens.Add(new AccountToken
        {
            UserId = userId,
            Purpose = purpose,
            Source = source,
            TokenHash = TokenHasher.Hash(raw), // @NFR-f01-p1-tai-khoan-hoc-vien-001: chỉ lưu hash
            CreatedAt = now,
            ExpiresAt = expiresAt,
        });
        await db.SaveChangesAsync(ct);
        return raw;
    }

    public async Task<Guid?> FindValidAsync(string? rawToken, AccountTokenPurpose purpose, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return null;
        var hash = TokenHasher.Hash(rawToken);
        var now = clock.UtcNow;
        // @ADR-f01-p1-tai-khoan-hoc-vien-003: hết hạn khi now >= ExpiresAt
        return await db.AccountTokens.AsNoTracking()
            .Where(t => t.TokenHash == hash && t.Purpose == purpose
                && t.ConsumedAt == null && t.RevokedAt == null && t.ExpiresAt > now)
            .Select(t => (Guid?)t.UserId)
            .SingleOrDefaultAsync(ct);
    }

    public async Task ConsumeAsync(string rawToken, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        var hash = TokenHasher.Hash(rawToken);
        var now = clock.UtcNow;
        await db.AccountTokens
            .Where(t => t.TokenHash == hash && t.ConsumedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ConsumedAt, (DateTimeOffset?)now), ct);
    }

    public async Task RevokeAllAsync(Guid userId, AccountTokenPurpose purpose, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        await LiveTokens(userId, purpose, now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, (DateTimeOffset?)now), ct);
    }

    public Task<DateTimeOffset?> LastIssuedAtAsync(Guid userId, AccountTokenPurpose purpose, AccountTokenSource source,
        CancellationToken ct = default) =>
        db.AccountTokens.AsNoTracking()
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.Source == source)
            .MaxAsync(t => (DateTimeOffset?)t.CreatedAt, ct);

    private IQueryable<AccountToken> LiveTokens(Guid userId, AccountTokenPurpose purpose, DateTimeOffset now) =>
        db.AccountTokens.Where(t => t.UserId == userId && t.Purpose == purpose
            && t.ConsumedAt == null && t.RevokedAt == null && t.ExpiresAt > now);
}
```

`backend/src/Coaching.Infrastructure/DependencyInjection.cs` — thêm sau dòng `services.AddScoped<IStudentAccounts, StudentAccounts>();` (`using Coaching.Application.Accounts;` đã thêm ở Task 2):
```csharp
        services.AddScoped<IAccountTokens, AccountTokenStore>(); // @ADR-f01-p1-tai-khoan-hoc-vien-003
```

- [x] **Step 5: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~AccountTokenStoreTests"`
Expected: `Passed: 7`, `Failed: 0`.
Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed (refresh token F00 vẫn dùng `TokenHasher.NewToken()` 64 byte).

- [x] **Step 6: Commit**
```bash
git add backend/src backend/tests/Coaching.Api.IntegrationTests/Accounts
git commit -F - <<'EOF'
feat(accounts): account token store for activation and password reset — hashed, single use, newest wins (ADR-f01-p1-003)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 4: Coach gửi / gửi lại thông tin đăng nhập + đăng nhập bằng mật khẩu tạm trả mã kích hoạt

**Files:**
- Modify: `backend/src/Coaching.Domain/Students/Student.cs`
- Modify: `backend/src/Coaching.Application/Accounts/AccountErrors.cs` (file đã có từ Task 2 với `ClientUrlMissing`)
- Create: `backend/src/Coaching.Application/Accounts/IAccountCredentials.cs`, `AccountTokenLifetimes.cs`, `AccountMail.cs`, `CredentialsMailer.cs`
- Create: `backend/src/Coaching.Application/Students/Commands/SendCredentials.cs`
- Modify: `backend/src/Coaching.Application/Auth/AuthModels.cs`, `backend/src/Coaching.Application/Auth/IAuthService.cs`, `backend/src/Coaching.Application/DependencyInjection.cs`
- Create: `backend/src/Coaching.Infrastructure/Identity/AccountCredentials.cs`
- Modify: `backend/src/Coaching.Infrastructure/Identity/AuthService.cs`, `backend/src/Coaching.Infrastructure/DependencyInjection.cs`, `backend/src/Coaching.Infrastructure/Seeding/DatabaseInitializer.cs`
- Modify: `backend/src/Coaching.Api/Controllers/AuthController.cs`, `backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs`
- Test: `backend/tests/Coaching.Domain.Tests/Students/SendCredentialsTests.cs`
- Test: `backend/tests/Coaching.Acceptance/Support/AcceptanceContext.cs` (M), `backend/tests/Coaching.Acceptance/Support/AccountApi.cs` (C), `backend/tests/Coaching.Acceptance/Steps/CredentialsSteps.cs` (C)
- Test: `backend/tests/Coaching.Api.IntegrationTests/ApiFactory.cs` (M), `Accounts/AccountRequests.cs` (C), `Accounts/SendCredentialsTests.cs` (C), `Accounts/TempPasswordLoginTests.cs` (C), `Architecture/AccountSecretsDtoTests.cs` (C), `Students/DemoSeedStudentTests.cs` (M)

**Interfaces:**
- Consumes:
  - Task 1 — `Student.CredentialsSentAt/TempPasswordExpiresAt/ActivatedAt`, `Student.TempPasswordLifetime`, `Student.AccountEvents` (list riêng `_accountEvents`), `StudentAccountEvent(AccountEventType, DateTimeOffset, Guid?, string? = null, string? = null)` (internal ctor), `AccountEventType`, `Student.GetAccountState(now)`, `AccountInfoDto`/`AccountEventDto`, `StudentDetailDto.From(Student, Coach, DateTimeOffset now)`, `ErrorKind.Unavailable` → 503.
  - Task 2 — `IAccountEmails.Credentials(...)`, `AccountErrors.ClientUrlMissing`, `EmailOutbox` (`All`, `FailNext`) + `CapturedEmail` (namespace `Coaching.Infrastructure.Emailing`), `Email:Provider=Capture`, `App:ClientBaseUrl`. Thư thông tin đăng nhập: tiêu đề `Thông tin đăng nhập Duobexiuuu Coaching`, `TextBody` có dòng `Mật khẩu tạm: <mật khẩu>`, địa chỉ `<ClientBaseUrl>/login`, email tài khoản và câu hạn "72 giờ".
  - Task 3 — `IAccountTokens.IssueAsync/RevokeAllAsync`, `AccountTokenPurpose`, `AccountTokenSource`.
  - F02 — `CurrentCoach`, `IStudentRepository.GetAsync`, `StudentErrors.*`, `ConcurrencyConflictException`, `StudentApi`, `RegistrationLabels`, `RegistrationSamples.Complete()`, step `Given coach "…" đã đăng nhập`, F00 step `Then đăng nhập bị từ chối`, `Then hệ thống từ chối với thông báo "…"`.
- Produces:
  - Domain: `Student.IsTempPasswordValid(DateTimeOffset now)`, `Student.RecordCredentialsSent(Guid actorUserId, DateTimeOffset at)`, `Student.Activate(DateTimeOffset at)` (kéo từ Task 5 lên — xem lệch so với skeleton).
  - Application: `AccountErrors.AlreadyActivated/RequiredFieldsMissing(int)` (`EmailSendFailed` có sẵn từ Task 2); `IAccountCredentials.SetTemporaryPasswordAsync`; `AccountTokenLifetimes.Activation` (30 phút); `AccountMail.SendAsync(IEmailSender, Result<EmailMessage>, CancellationToken) → Task<Result>` (internal); `CredentialsMailer.SendAsync(Student, CancellationToken) → Task<Result>`; `SendCredentialsHandler.HandleAsync(Guid id, CancellationToken) → Result<StudentDetailDto>`; `ActivationChallenge`, `LoginOutcome`; `IAuthService.LoginAsync → Task<Result<LoginOutcome>>`.
  - Infrastructure: `AccountCredentials : IAccountCredentials` (`TemporaryPasswordLength = 12`); `AuthService` trả `ActivationChallenge`; seed demo kích hoạt học viên demo còn Chờ kích hoạt.
  - API: `POST /api/admin/students/{id}/credentials` → 200 `StudentDetailDto` / 400 / 404 / 409 / 503; `POST /api/auth/login` → 200 `AuthTokens` hoặc 200 `{ activationRequired, activationToken, activationTokenExpiresAt }`.
  - Test: `AcceptanceContext.ClientBaseUrl`; `AccountApi` (`StudentName`, `DefaultOwnPassword`, `CredentialsSubject`, `Emails`, `TempPasswords`, `FocusTemp`, `ActivationToken`, `OwnPassword`, `MailCountBefore`, `HistoryBefore`, `SentBody`, `LastBody`, `LastDetail`, `Outbox`, `Email()`, `Id()`, `Mails()`, `CredentialMails()`, `CreateAsync()`, `TrySendCredentialsAsync()`, `SendOkAsync()`, `SendThenWaitAsync()`, `ViewAsync()`, `StudentLoginAsync()`, `AssertActivationChallengeAsync()`, `AssertProblemTitleAsync()`, `CreateActivatedAsync()`, `TempPassword()`, `Urls()`); `ApiFactory.ClientBaseUrl/Capture/Outbox/CaptureAdminClientAsync()`; `AccountRequests` (`CredentialsSubject`, `CreateCompleteAsync`, `SendCredentialsAsync`, `SendCredentialsOkAsync`, `LoginAsync`, `TempPassword`); step `When coach gửi thông tin đăng nhập cho học viên đó`, `When học viên đăng nhập web học viên bằng mật khẩu tạm đó`, `Then học viên được đưa tới bước đặt mật khẩu riêng`, `Given dịch vụ email đang không gửi được thư`, `Then coach được báo thông tin đăng nhập chưa gửi được`, `Then mật khẩu tạm đã gửi trước đó vẫn dùng được`, `When coach xem hồ sơ của học viên đó`, `Given một học viên có tài khoản Đã kích hoạt`, `Given một (hồ sơ) học viên đã được gửi thông tin đăng nhập(, tài khoản Chờ kích hoạt)` (dùng lại ở Task 5–7).

**Owns:** `@BR-f01-p1-tai-khoan-hoc-vien-001`, `@BR-f01-p1-tai-khoan-hoc-vien-002`, `@BR-f01-p1-tai-khoan-hoc-vien-003`, `@BR-f01-p1-tai-khoan-hoc-vien-004`, `@BR-f01-p1-tai-khoan-hoc-vien-005`, `@BR-f01-p1-tai-khoan-hoc-vien-006`, `@BR-f01-p1-tai-khoan-hoc-vien-012`, `@BR-f01-p1-tai-khoan-hoc-vien-025` | `@ADR-f01-p1-tai-khoan-hoc-vien-001`, `@ADR-f01-p1-tai-khoan-hoc-vien-002`, `@ADR-f01-p1-tai-khoan-hoc-vien-004`

> Vì sao `Student.Activate` và seed demo kích hoạt nằm ở task này: từ task này, học viên Chờ kích hoạt chỉ đăng nhập được bằng mật khẩu tạm còn hạn (ADR-f01-p1-tai-khoan-hoc-vien-002). Helper test F00/F02 (`CreateStudentAsync`) và học viên demo (BR-f00-codebase-020) đều là học viên có mật khẩu riêng nhưng đang Chờ kích hoạt, nên phải được kích hoạt ngay trong task này để các scenario cũ vẫn xanh. Task 5 thêm annotation BR-f01-p1-tai-khoan-hoc-vien-016 và use case kích hoạt.

- [x] **Step 1: Viết Domain test cho gửi thông tin đăng nhập và hạn mật khẩu tạm**

`backend/tests/Coaching.Domain.Tests/Students/SendCredentialsTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.TestSupport;

namespace Coaching.Domain.Tests.Students;

public sealed class SendCredentialsTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 20, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SentAt = new(2026, 11, 25, 9, 0, 0, TimeSpan.FromHours(7));
    private static readonly Guid Coach = Guid.NewGuid();

    private static Student CompleteStudent() => Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Quân",
        Email.Create("quan@gmail.com"), CreatedAt, RegistrationForm.Create(RegistrationSamples.Complete(), new DateOnly(2026, 11, 20)));

    // @BR-f01-p1-tai-khoan-hoc-vien-002: lần gửi đầu cần đủ mục bắt buộc; bị từ chối thì không đổi gì
    [Fact]
    public void First_send_needs_every_required_field()
    {
        var student = Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Quân", Email.Create("quan@gmail.com"), CreatedAt);
        var missing = student.MissingRequiredFields.Count;

        var ex = Assert.Throws<DomainException>(() => student.RecordCredentialsSent(Coach, SentAt));

        Assert.Equal("account.required_fields_missing", ex.Code);
        Assert.Equal($"Còn {missing} mục bắt buộc chưa điền.", ex.Message);
        Assert.Null(student.CredentialsSentAt);
        Assert.Null(student.TempPasswordExpiresAt);
        Assert.Empty(student.AccountEvents);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-004: hạn mật khẩu tạm = lúc gửi + 72 giờ
    [Fact]
    public void First_send_sets_72_hour_expiry_and_records_CredentialsSent()
    {
        var student = CompleteStudent();

        student.RecordCredentialsSent(Coach, SentAt);

        Assert.Equal(SentAt, student.CredentialsSentAt);
        Assert.Equal(SentAt.AddHours(72), student.TempPasswordExpiresAt);
        var row = Assert.Single(student.AccountEvents);
        Assert.Equal(AccountEventType.CredentialsSent, row.Type);
        Assert.Equal(SentAt, row.At);
        Assert.Equal(Coach, row.ActorUserId);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-002 @BR-f01-p1-tai-khoan-hoc-vien-005: gửi lại không kiểm mục bắt buộc, hạn tính lại từ lần gửi sau cùng
    [Fact]
    public void Resend_skips_required_fields_and_records_CredentialsResent()
    {
        var student = CompleteStudent();
        student.RecordCredentialsSent(Coach, SentAt);
        student.UpdateRegistration("Quân", RegistrationForm.CreateEmpty(), Coach, SentAt.AddHours(1));
        var resentAt = SentAt.AddHours(2);

        student.RecordCredentialsSent(Coach, resentAt);

        Assert.Equal(resentAt, student.CredentialsSentAt);
        Assert.Equal(resentAt.AddHours(72), student.TempPasswordExpiresAt);
        Assert.Equal(new[] { AccountEventType.CredentialsSent, AccountEventType.CredentialsResent },
            student.AccountEvents.Select(e => e.Type).ToArray());
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-001: tài khoản đã kích hoạt → từ chối, không thêm lịch sử
    [Fact]
    public void Send_to_activated_account_is_rejected()
    {
        var student = CompleteStudent();
        student.Activate(SentAt);
        var events = student.AccountEvents.Count;

        var ex = Assert.Throws<DomainException>(() => student.RecordCredentialsSent(Coach, SentAt.AddHours(1)));

        Assert.Equal("account.already_activated", ex.Code);
        Assert.Equal(events, student.AccountEvents.Count);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-001: gửi được ở mọi trạng thái coaching
    [Theory]
    [InlineData(CoachingStatus.Paused)]
    [InlineData(CoachingStatus.Ended)]
    public void Send_works_in_every_coaching_status(CoachingStatus status)
    {
        var student = CompleteStudent();
        student.ChangeCoachingStatus(status, "Kịch bản", Coach, SentAt);

        student.RecordCredentialsSent(Coach, SentAt);

        Assert.Equal(SentAt, student.CredentialsSentAt);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-004: 71 giờ 59 phút còn dùng được; đúng 72 giờ và 73 giờ thì hết
    [Fact]
    public void Temp_password_expires_exactly_at_72_hours()
    {
        var student = CompleteStudent();
        Assert.False(student.IsTempPasswordValid(SentAt));

        student.RecordCredentialsSent(Coach, SentAt);

        Assert.True(student.IsTempPasswordValid(SentAt.AddHours(48)));
        Assert.True(student.IsTempPasswordValid(SentAt.AddHours(71).AddMinutes(59)));
        Assert.False(student.IsTempPasswordValid(SentAt.AddHours(72)));
        Assert.False(student.IsTempPasswordValid(SentAt.AddHours(73)));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-012: sau khi kích hoạt, mật khẩu tạm không còn hiệu lực
    [Fact]
    public void Activated_account_has_no_valid_temp_password()
    {
        var student = CompleteStudent();
        student.RecordCredentialsSent(Coach, SentAt);

        student.Activate(SentAt.AddHours(1));

        Assert.Equal(AccountStatus.Activated, student.AccountStatus);
        Assert.Null(student.TempPasswordExpiresAt);
        Assert.False(student.IsTempPasswordValid(SentAt.AddHours(2)));
    }
}
```

- [x] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students.SendCredentialsTests"`
Expected: FAIL — build error `'Student' does not contain a definition for 'RecordCredentialsSent'` (và `IsTempPasswordValid`, `Activate`).

- [x] **Step 3: Domain — `IsTempPasswordValid`, `RecordCredentialsSent`, `Activate`**

`backend/src/Coaching.Domain/Students/Student.cs` — thêm 3 method sau method `ChangeCoachingStatus` (trước `RefreshSearchKey`):
```csharp
    /// <summary>
    /// @BR-f01-p1-tai-khoan-hoc-vien-004: mật khẩu tạm còn hiệu lực khi tài khoản Chờ kích hoạt và now &lt; hạn
    /// (hết hạn khi now >= hạn — đúng 72 giờ là hết).
    /// @BR-f01-p1-tai-khoan-hoc-vien-012: tài khoản đã kích hoạt không còn mật khẩu tạm nào dùng được.
    /// </summary>
    public bool IsTempPasswordValid(DateTimeOffset now) =>
        AccountStatus == AccountStatus.PendingActivation && TempPasswordExpiresAt is { } expiresAt && now < expiresAt;

    /// <summary>
    /// Coach gửi (lần đầu) hoặc gửi lại thông tin đăng nhập. Mật khẩu tạm do IAccountCredentials đặt trong cùng transaction
    /// (@ADR-f01-p1-tai-khoan-hoc-vien-001: mật khẩu tạm là mật khẩu Identity, hạn dùng nằm ở đây).
    /// </summary>
    public void RecordCredentialsSent(Guid actorUserId, DateTimeOffset at)
    {
        // @BR-f01-p1-tai-khoan-hoc-vien-001: chỉ tài khoản Chờ kích hoạt, ở mọi trạng thái coaching
        if (AccountStatus == AccountStatus.Activated)
            throw new DomainException("account.already_activated", "Tài khoản đã kích hoạt. Chỉ gửi được link đặt lại mật khẩu.");

        var first = CredentialsSentAt is null;
        // @BR-f01-p1-tai-khoan-hoc-vien-002: chỉ lần gửi đầu mới cần đủ mục bắt buộc
        var missing = MissingRequiredFields.Count;
        if (first && missing > 0)
            throw new DomainException("account.required_fields_missing", $"Còn {missing} mục bắt buộc chưa điền.");

        CredentialsSentAt = at;
        TempPasswordExpiresAt = at + TempPasswordLifetime; // @BR-f01-p1-tai-khoan-hoc-vien-004
        // @BR-f01-p1-tai-khoan-hoc-vien-005: gửi lại = mật khẩu tạm mới, mật khẩu tạm cũ bị huỷ
        _accountEvents.Add(new StudentAccountEvent(
            first ? AccountEventType.CredentialsSent : AccountEventType.CredentialsResent, at, actorUserId));
    }

    /// <summary>
    /// Chờ kích hoạt → Đã kích hoạt, ghi thời điểm kích hoạt; mật khẩu tạm hết hiệu lực ngay.
    /// Không kiểm mật khẩu tạm ở đây: use case kích hoạt kiểm (Task 5); seed học viên demo kích hoạt trực tiếp vì demo đã có mật khẩu riêng.
    /// </summary>
    public void Activate(DateTimeOffset at)
    {
        if (AccountStatus == AccountStatus.Activated)
            throw new DomainException("account.already_activated", "Tài khoản đã kích hoạt.");
        AccountStatus = AccountStatus.Activated;
        ActivatedAt = at;
        TempPasswordExpiresAt = null;
        _accountEvents.Add(new StudentAccountEvent(AccountEventType.Activated, at, UserId));
    }
```

- [x] **Step 4: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students.SendCredentialsTests"`
Expected: `Passed: 8`.

- [x] **Step 5: Hạ tầng acceptance — provider Capture, học viên helper đã kích hoạt, `AccountApi`**

`backend/tests/Coaching.Acceptance/Support/AcceptanceContext.cs`:
- Thêm hằng số ngay dưới `public const string AdminName = "Coach Duobexiuuu";`:
```csharp

    /// <summary>App:ClientBaseUrl của scenario — địa chỉ trong email (NFR-f01-p1-tai-khoan-hoc-vien-003).</summary>
    public const string ClientBaseUrl = "http://client.acceptance.local";
```
- Trong `Settings`, thay dòng `["Email:Provider"] = "Log",` bằng:
```csharp
        // Thư F01-P1 đọc qua EmailOutbox (singleton trong DI); EmailOutbox.FailNext mô phỏng dịch vụ email lỗi
        ["Email:Provider"] = "Capture",
        ["App:ClientBaseUrl"] = ClientBaseUrl,
```
- Trong `CreateStudentAsync`, thay dòng
```csharp
        db.Students.Add(Student.Create(coach.Id, user.Id, user.FullName, Email.Create(email), sp.GetRequiredService<IClock>().UtcNow));
```
bằng:
```csharp
        var now = sp.GetRequiredService<IClock>().UtcNow;
        var student = Student.Create(coach.Id, user.Id, user.FullName, Email.Create(email), now);
        // F01-P1: học viên có mật khẩu riêng là học viên Đã kích hoạt — Chờ kích hoạt chỉ đăng nhập được bằng mật khẩu tạm
        student.Activate(now);
        db.Students.Add(student);
```

`backend/tests/Coaching.Acceptance/Support/AccountApi.cs`:
```csharp
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Coaching.Application.Abstractions;
using Coaching.Application.Auth;
using Coaching.Application.Students;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Emailing;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Coaching.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Acceptance.Support;

/// <summary>
/// Tài khoản học viên trong scenario F01-P1: hồ sơ "học viên đó", gửi thông tin đăng nhập, đọc thư capture
/// (Email:Provider=Capture → EmailOutbox singleton trong DI), đăng nhập bằng mật khẩu tạm.
/// </summary>
public sealed partial class AccountApi(AcceptanceContext ctx, StudentApi api)
{
    /// <summary>Họ tên của "học viên đó" trong mọi scenario tài khoản.</summary>
    public const string StudentName = "Học viên tài khoản";

    public const string DefaultOwnPassword = "Coach2025";

    public const string CredentialsSubject = "Thông tin đăng nhập Duobexiuuu Coaching";

    /// <summary>Email hiện tại theo họ tên hồ sơ (cập nhật khi gửi / sửa email thành công).</summary>
    public Dictionary<string, string> Emails { get; } = new();

    /// <summary>Mật khẩu tạm của các lần gửi THÀNH CÔNG, theo thứ tự (đọc từ thư capture).</summary>
    public List<string> TempPasswords { get; } = [];

    /// <summary>"mật khẩu tạm đó" của scenario.</summary>
    public string? FocusTemp { get; set; }

    /// <summary>Mã kích hoạt của lần đăng nhập bằng mật khẩu tạm gần nhất.</summary>
    public string? ActivationToken { get; private set; }

    /// <summary>Mật khẩu riêng hiện tại của học viên.</summary>
    public string? OwnPassword { get; set; }

    /// <summary>Số thư cùng loại tới học viên ngay trước thao tác gửi gần nhất.</summary>
    public int MailCountBefore { get; private set; }

    /// <summary>Số dòng lịch sử tài khoản trước thao tác đang kiểm.</summary>
    public int HistoryBefore { get; set; }

    public string SentBody { get; set; } = string.Empty;

    public string LastBody { get; private set; } = string.Empty;

    public StudentDetailDto? LastDetail { get; private set; }

    public EmailOutbox Outbox => ctx.Host.Services.GetRequiredService<EmailOutbox>();

    public string Email(string name = StudentName) => Emails[name];

    public Guid Id(string name = StudentName) => api.Ids[name];

    public IReadOnlyList<CapturedEmail> Mails(string to, string subject) =>
        Outbox.All.Where(m => string.Equals(m.To, to.Trim(), StringComparison.OrdinalIgnoreCase) && m.Subject == subject).ToList();

    public IReadOnlyList<CapturedEmail> CredentialMails(string? to = null) => Mails(to ?? Email(), CredentialsSubject);

    /// <summary>Hồ sơ đủ mục bắt buộc (mặc định), tài khoản Chờ kích hoạt, chưa gửi lần nào.</summary>
    public async Task<StudentDetailDto> CreateAsync(string name = StudentName, string? email = null,
        RegistrationInput? registration = null)
    {
        await api.LoginCoachAsync();
        var detail = await api.CreateOkAsync(name, email ?? StudentApi.NewEmail(), registration ?? RegistrationSamples.Complete());
        Emails[name] = detail.Email;
        return detail;
    }

    public async Task<HttpResponseMessage> TrySendCredentialsAsync(string name = StudentName)
    {
        await api.LoginCoachAsync(); // đồng hồ có thể đã tua quá hạn access token
        MailCountBefore = CredentialMails(Emails[name]).Count;
        var response = await ctx.SendJsonAsync(HttpMethod.Post, $"/api/admin/students/{api.Ids[name]}/credentials", new { },
            api.AdminToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var detail = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
            Emails[name] = detail.Email;
            TempPasswords.Add(TempPassword(CredentialMails(detail.Email)[^1]));
        }

        return response;
    }

    public async Task SendOkAsync(string name = StudentName)
    {
        Assert.Equal(HttpStatusCode.OK, (await TrySendCredentialsAsync(name)).StatusCode);
        FocusTemp = TempPasswords[^1];
    }

    /// <summary>Tạo hồ sơ, gửi thông tin đăng nhập rồi tua đồng hồ <paramref name="elapsed"/>.</summary>
    public async Task SendThenWaitAsync(TimeSpan elapsed)
    {
        await CreateAsync();
        await SendOkAsync();
        ctx.Clock.Advance(elapsed);
    }

    /// <summary>Coach xem hồ sơ: nhớ cả body thô (để kiểm không có mật khẩu) và DTO.</summary>
    public async Task<StudentDetailDto> ViewAsync(string name = StudentName)
    {
        await api.LoginCoachAsync();
        var response = await ctx.GetAsync($"/api/admin/students/{api.Ids[name]}", api.AdminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        LastBody = await response.Content.ReadAsStringAsync();
        LastDetail = JsonSerializer.Deserialize<StudentDetailDto>(LastBody, JsonSerializerOptions.Web)!;
        return LastDetail;
    }

    /// <summary>Đăng nhập bằng email của "học viên đó" (hoặc <paramref name="email"/>); nhớ mã kích hoạt hoặc phiên.</summary>
    public async Task<HttpResponseMessage> StudentLoginAsync(string password, string app = "client", string? email = null)
    {
        var response = await ctx.LoginAsync(email ?? Email(), password, app);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (json.RootElement.TryGetProperty("activationRequired", out var required) && required.GetBoolean())
                ActivationToken = json.RootElement.GetProperty("activationToken").GetString();
            else
                ctx.Tokens = json.RootElement.Deserialize<AuthTokens>(JsonSerializerOptions.Web);
        }

        return response;
    }

    /// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-002: 200 có mã kích hoạt, KHÔNG có phiên.</summary>
    public async Task AssertActivationChallengeAsync()
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        using var json = JsonDocument.Parse(await ctx.LastResponse.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("activationRequired").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("activationToken").GetString()));
        Assert.False(json.RootElement.TryGetProperty("accessToken", out _));
        Assert.False(json.RootElement.TryGetProperty("refreshToken", out _));
    }

    public async Task AssertProblemTitleAsync(string title)
    {
        var problem = await ctx.LastResponse!.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(title, problem!.Title);
    }

    /// <summary>
    /// Học viên Đã kích hoạt với mật khẩu riêng <paramref name="password"/>. Task 4 kích hoạt thẳng trong DB
    /// (API kích hoạt có ở Task 5 — Task 5 thay thân hàm bằng luồng thật).
    /// </summary>
    public async Task<StudentDetailDto> CreateActivatedAsync(string name = StudentName, string? email = null,
        string password = DefaultOwnPassword)
    {
        var detail = await CreateAsync(name, email);
        await using var scope = ctx.Host.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var users = sp.GetRequiredService<UserManager<AppUser>>();
        var student = await db.Students.SingleAsync(s => s.Id == detail.Id);
        var user = (await users.FindByIdAsync(student.UserId.ToString()))!;
        Assert.True((await users.AddPasswordAsync(user, password)).Succeeded);
        student.Activate(sp.GetRequiredService<IClock>().UtcNow);
        await db.SaveChangesAsync();
        OwnPassword = password;
        return detail;
    }

    /// <summary>Mật khẩu tạm trong thư (dòng "Mật khẩu tạm: …" của AccountEmails).</summary>
    public static string TempPassword(CapturedEmail mail) =>
        TempPasswordPattern().Match(mail.TextBody) is { Success: true } match
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"Thư '{mail.Subject}' tới {mail.To} không có dòng mật khẩu tạm.");

    /// <summary>Mọi địa chỉ http(s) trong thân thư (bỏ dấu câu dính cuối).</summary>
    public static IReadOnlyList<string> Urls(string body) =>
        UrlPattern().Matches(body).Select(m => m.Value.TrimEnd('.', ',', ')')).ToList();

    [GeneratedRegex(@"Mật khẩu tạm:\s*(\S+)")]
    private static partial Regex TempPasswordPattern();

    [GeneratedRegex(@"https?://[^\s""'<>]+")]
    private static partial Regex UrlPattern();
}
```

> `System.Diagnostics` dùng ở Task 7 (`Stopwatch` trong `WaitForMailsAsync`); giữ `using` từ bây giờ để Task 7 chỉ thêm member.

- [x] **Step 6: Viết step defs `@api` cho BR-001→006, BR-012, BR-025**

`backend/tests/Coaching.Acceptance/Steps/CredentialsSteps.cs`:
```csharp
using System.Net;
using Coaching.Acceptance.Support;
using Coaching.TestSupport;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class CredentialsSteps(AcceptanceContext ctx, StudentApi api, AccountApi account)
{
    /// <summary>Mã mục bắt buộc dùng để làm trống khi scenario cần "còn N mục bắt buộc trống".</summary>
    private static readonly string[] RequiredCodes = ["pack", "price", "age", "heightCm", "weightKg"];

    // @BR-f01-p1-tai-khoan-hoc-vien-001
    [Given(@"^một hồ sơ học viên đủ mục bắt buộc, tài khoản Chờ kích hoạt, coaching ""([^""]*)""$")]
    public async Task CompletePendingWithStatus(string label)
    {
        await account.CreateAsync();
        var status = StudentApi.CoachingStatuses[label];
        if (status != "Active")
        {
            var changed = await api.TryChangeStatusAsync(AccountApi.StudentName, status, "Chuẩn bị kịch bản");
            Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        }
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-001 @BR-f01-p1-tai-khoan-hoc-vien-002 @BR-f01-p1-tai-khoan-hoc-vien-003 @BR-f01-p1-tai-khoan-hoc-vien-006
    [When("coach gửi thông tin đăng nhập cho học viên đó")]
    [When("coach gửi lại thông tin đăng nhập cho học viên đó")]
    public Task Send() => account.TrySendCredentialsAsync();

    // @BR-f01-p1-tai-khoan-hoc-vien-001 @BR-f01-p1-tai-khoan-hoc-vien-002
    [Then(@"^học viên đó nhận được email thông tin đăng nhập(?: mới)?$")]
    public void ReceivedCredentials()
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        Assert.Equal(account.MailCountBefore + 1, account.CredentialMails().Count);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-001 (dùng lại ở BR-007, BR-017)
    [Given("một học viên có tài khoản Đã kích hoạt")]
    public Task ActivatedStudent() => account.CreateActivatedAsync();

    [Then("thông tin đăng nhập không được gửi vì tài khoản đã kích hoạt")]
    public async Task RejectedActivated()
    {
        await api.AssertErrorAsync(HttpStatusCode.Conflict, "account.already_activated");
        Assert.Equal(account.MailCountBefore, account.CredentialMails().Count);
    }

    [Then("coach được hướng dẫn gửi link đặt lại mật khẩu thay thế")]
    public Task GuidedToResetLink() =>
        account.AssertProblemTitleAsync("Tài khoản đã kích hoạt. Chỉ gửi được link đặt lại mật khẩu.");

    // @BR-f01-p1-tai-khoan-hoc-vien-002 (dùng lại ở BR-006, BR-010)
    [Given("một hồ sơ học viên đủ mục bắt buộc, chưa gửi thông tin đăng nhập lần nào")]
    public Task CompleteNeverSent() => account.CreateAsync();

    [Given(@"^một hồ sơ học viên còn ""(\d+)"" mục bắt buộc trống, chưa gửi thông tin đăng nhập lần nào$")]
    public async Task MissingRequired(int count)
    {
        var input = RegistrationSamples.Complete();
        foreach (var code in RequiredCodes.Take(count))
            input = RegistrationLabels.With(input, code, null);
        var detail = await account.CreateAsync(registration: input);
        Assert.Equal(count, detail.MissingRequired.Count);
    }

    [Then(@"^thông tin đăng nhập không được gửi vì còn ""(\d+)"" mục bắt buộc chưa điền$")]
    public async Task RejectedMissing(int count)
    {
        await api.AssertErrorAsync(HttpStatusCode.BadRequest, "account.required_fields_missing");
        await account.AssertProblemTitleAsync($"Còn {count} mục bắt buộc chưa điền.");
        Assert.Equal(account.MailCountBefore, account.CredentialMails().Count);
    }

    [Then("hồ sơ đó vẫn được lưu")]
    public async Task ProfileStillSaved()
    {
        var detail = await account.ViewAsync();
        Assert.Equal("NotSent", detail.Account.State);
        Assert.NotEmpty(detail.MissingRequired);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-002 @BR-f01-p1-tai-khoan-hoc-vien-006 (dùng lại ở BR-008)
    [Given(@"^một (?:hồ sơ )?học viên đã được gửi thông tin đăng nhập(?:, tài khoản Chờ kích hoạt)?$")]
    public async Task CreatedAndSent()
    {
        await account.CreateAsync();
        await account.SendOkAsync();
    }

    [Given("sau đó coach để trống một mục bắt buộc của hồ sơ đó")]
    public async Task ClearOneRequired()
    {
        await api.LoginCoachAsync();
        var detail = await api.UpdateOkAsync(AccountApi.StudentName, r => RegistrationLabels.With(r, "trainingGoal", null));
        Assert.Contains("trainingGoal", detail.MissingRequired);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-003
    [Given(@"^một hồ sơ học viên đủ mục bắt buộc với email ""([^""]*)"", tài khoản Chờ kích hoạt$")]
    public Task CompleteWithEmail(string email) => account.CreateAsync(email: email);

    [Then(@"^email ""([^""]*)"" nhận được địa chỉ đăng nhập web học viên, email tài khoản ""([^""]*)"", một mật khẩu tạm và hạn dùng ""(\d+)"" giờ$")]
    public void CredentialsMailContent(string to, string accountEmail, int hours)
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        var mail = Assert.Single(account.CredentialMails(to));
        var temp = AccountApi.TempPassword(mail);
        Assert.Contains($"{AcceptanceContext.ClientBaseUrl}/login", mail.TextBody);
        Assert.Contains(accountEmail, mail.TextBody);
        Assert.Contains($"{hours} giờ", mail.TextBody);
        Assert.Contains(temp, mail.HtmlBody);
        Assert.Contains($"{AcceptanceContext.ClientBaseUrl}/login", mail.HtmlBody);
    }

    [Then("địa chỉ đăng nhập trong email không chứa mật khẩu tạm hay mã bí mật nào")]
    public void LoginUrlHasNoSecret()
    {
        var mail = account.CredentialMails()[^1];
        var temp = AccountApi.TempPassword(mail);
        var urls = AccountApi.Urls(mail.TextBody).Concat(AccountApi.Urls(mail.HtmlBody)).ToList();
        Assert.Contains($"{AcceptanceContext.ClientBaseUrl}/login", urls);
        Assert.All(urls, url =>
        {
            Assert.DoesNotContain(temp, url);
            Assert.DoesNotContain("?", url);
            Assert.DoesNotContain("#", url);
        });
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-004 @BR-f01-p1-tai-khoan-hoc-vien-012
    [Given(@"^học viên được gửi mật khẩu tạm cách đây ""(\d+)"" giờ ""(\d+)"" phút$")]
    public Task SentHoursMinutesAgo(int hours, int minutes) =>
        account.SendThenWaitAsync(TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes));

    [Given(@"^(?:một )?học viên được gửi mật khẩu tạm cách đây (?:đúng )?""(\d+)"" giờ$")]
    public Task SentHoursAgo(int hours) => account.SendThenWaitAsync(TimeSpan.FromHours(hours));

    // @BR-f01-p1-tai-khoan-hoc-vien-004 @BR-f01-p1-tai-khoan-hoc-vien-012 (dùng lại ở BR-016)
    [When("học viên đăng nhập web học viên bằng mật khẩu tạm đó")]
    public Task LoginWithFocusTemp() => account.StudentLoginAsync(account.FocusTemp!);

    // @BR-f01-p1-tai-khoan-hoc-vien-004 @BR-f01-p1-tai-khoan-hoc-vien-005
    [Then("học viên được đưa tới bước đặt mật khẩu riêng")]
    public Task AtActivationStep() => account.AssertActivationChallengeAsync();

    // @BR-f01-p1-tai-khoan-hoc-vien-005
    [Given("coach đã gửi thông tin đăng nhập cho học viên hai lần liên tiếp")]
    public async Task SentTwice()
    {
        await account.CreateAsync();
        await account.SendOkAsync();
        await account.SendOkAsync();
    }

    [When(@"^học viên đăng nhập web học viên bằng mật khẩu tạm của lần gửi (sau cùng|trước)$")]
    public Task LoginWithSend(string which) =>
        account.StudentLoginAsync(which == "sau cùng" ? account.TempPasswords[^1] : account.TempPasswords[^2]);

    // @BR-f01-p1-tai-khoan-hoc-vien-012
    [Given("một học viên có mật khẩu tạm của lần gửi trước, coach đã gửi lại")]
    public async Task HasPreviousTemp()
    {
        await account.CreateAsync();
        await account.SendOkAsync();
        await account.SendOkAsync();
        account.FocusTemp = account.TempPasswords[^2];
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-006 (dùng lại ở BR-009)
    [Given("dịch vụ email đang không gửi được thư")]
    public async Task EmailServiceDown()
    {
        account.HistoryBefore = (await account.ViewAsync()).Account.History.Count;
        account.Outbox.FailNext = true;
    }

    [Then("coach được báo thông tin đăng nhập chưa gửi được")]
    public async Task ToldNotSent()
    {
        await api.AssertErrorAsync(HttpStatusCode.ServiceUnavailable, "account.email_send_failed");
        await account.AssertProblemTitleAsync("Chưa gửi được email. Vui lòng thử lại sau.");
    }

    [Then("mật khẩu tạm đã gửi trước đó vẫn dùng được")]
    public async Task PreviousTempStillWorks()
    {
        await account.StudentLoginAsync(account.TempPasswords[^1]);
        await account.AssertActivationChallengeAsync();
    }

    [Then("lịch sử tài khoản của học viên đó không có thêm dòng nào")]
    public async Task HistoryUnchanged() =>
        Assert.Equal(account.HistoryBefore, (await account.ViewAsync()).Account.History.Count);

    [Then("coach thấy tài khoản vẫn chưa được gửi thông tin đăng nhập")]
    public async Task StillNotSent()
    {
        var info = (await account.ViewAsync()).Account;
        Assert.Equal("NotSent", info.State);
        Assert.Null(info.CredentialsSentAt);
        Assert.Empty(info.History);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-025
    [Given("coach vừa gửi thông tin đăng nhập cho một học viên")]
    public async Task JustSent()
    {
        await account.CreateAsync();
        await account.SendOkAsync();
        account.SentBody = await ctx.LastResponse!.Content.ReadAsStringAsync();
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-025 (dùng lại ở BR-026)
    [When("coach xem hồ sơ của học viên đó")]
    public Task ViewProfile() => account.ViewAsync();

    [Then("coach thấy trạng thái tài khoản của học viên đó")]
    public void SeesAccountState()
    {
        Assert.Equal("Sent", account.LastDetail!.Account.State);
        Assert.NotNull(account.LastDetail.Account.CredentialsSentAt);
        Assert.NotNull(account.LastDetail.Account.TempPasswordExpiresAt);
    }

    [Then("coach không thấy mật khẩu tạm nào của học viên đó")]
    public void NoTempPasswordVisible()
    {
        var temp = Assert.Single(account.TempPasswords);
        Assert.DoesNotContain(temp, account.SentBody);
        Assert.DoesNotContain(temp, account.LastBody);
    }
}
```

- [x] **Step 7: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f01-p1-tai-khoan-hoc-vien-001|Category=BR-f01-p1-tai-khoan-hoc-vien-002|Category=BR-f01-p1-tai-khoan-hoc-vien-003|Category=BR-f01-p1-tai-khoan-hoc-vien-004|Category=BR-f01-p1-tai-khoan-hoc-vien-005|Category=BR-f01-p1-tai-khoan-hoc-vien-006|Category=BR-f01-p1-tai-khoan-hoc-vien-012|Category=BR-f01-p1-tai-khoan-hoc-vien-025)&Category!=web"`
Expected: FAIL — build xanh (Domain đã có `Activate` từ Step 3), lỗi lúc chạy: `Assert.Equal() Failure: Expected: OK, Actual: NotFound` ở `POST /api/admin/students/{id}/credentials` (route chưa có); scenario BR-004/BR-012 đỏ ở cùng bước gửi.

- [x] **Step 8: Application — lỗi, port mật khẩu, thời hạn mã kích hoạt, gửi thư trong transaction**

`backend/src/Coaching.Application/Accounts/AccountErrors.cs` — thêm 2 member vào đầu class `AccountErrors` (giữ nguyên `ClientUrlMissing` và `EmailSendFailed` đã có từ Task 2; chỉ thêm comment `// @BR-f01-p1-tai-khoan-hoc-vien-006 @ADR-f01-p1-tai-khoan-hoc-vien-004` ngay trên dòng khai báo `EmailSendFailed` sẵn có):
```csharp
    // @BR-f01-p1-tai-khoan-hoc-vien-001
    public static readonly Error AlreadyActivated = new(
        "account.already_activated", "Tài khoản đã kích hoạt. Chỉ gửi được link đặt lại mật khẩu.", ErrorKind.Conflict);

    // @BR-f01-p1-tai-khoan-hoc-vien-002
    public static Error RequiredFieldsMissing(int count) =>
        new("account.required_fields_missing", $"Còn {count} mục bắt buộc chưa điền.", ErrorKind.Validation);

```

`backend/src/Coaching.Application/Accounts/IAccountCredentials.cs`:
```csharp
namespace Coaching.Application.Accounts;

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-001: mật khẩu (tạm hoặc riêng) là mật khẩu Identity của tài khoản — chỉ lưu dạng hash.
/// Cài đặt bằng UserManager ở Infrastructure; các lệnh tự SaveChanges nên luôn gọi trong IUnitOfWork.ExecuteInTransactionAsync.
/// </summary>
public interface IAccountCredentials
{
    /// <summary>Sinh mật khẩu tạm mới, thay mật khẩu hiện tại, trả mật khẩu gốc để đưa vào email (không lưu ở đâu khác).</summary>
    Task<string> SetTemporaryPasswordAsync(Guid userId, CancellationToken ct = default);
}
```

`backend/src/Coaching.Application/Accounts/AccountTokenLifetimes.cs`:
```csharp
namespace Coaching.Application.Accounts;

public static class AccountTokenLifetimes
{
    /// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-002: mã kích hoạt sống 30 phút nhưng không quá hạn mật khẩu tạm.</summary>
    public static readonly TimeSpan Activation = TimeSpan.FromMinutes(30);
}
```

`backend/src/Coaching.Application/Accounts/AccountMail.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Common;

namespace Coaching.Application.Accounts;

internal static class AccountMail
{
    /// <summary>
    /// @ADR-f01-p1-tai-khoan-hoc-vien-004: gửi là bước cuối trước commit; lỗi soạn thư (thiếu App:ClientBaseUrl) hoặc lỗi dịch vụ
    /// email thành Result lỗi để use case trả về → ExecuteInTransactionAsync rollback (không thành 500).
    /// </summary>
    public static async Task<Result> SendAsync(IEmailSender sender, Result<EmailMessage> message, CancellationToken ct)
    {
        if (!message.IsSuccess)
            return Result.Failure(message.Error!);
        try
        {
            await sender.SendAsync(message.Value, ct);
            return Result.Success();
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return Result.Failure(AccountErrors.EmailSendFailed); // @BR-f01-p1-tai-khoan-hoc-vien-006
        }
    }
}
```

`backend/src/Coaching.Application/Accounts/CredentialsMailer.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Accounts;

/// <summary>
/// Đặt mật khẩu tạm mới + hủy mã kích hoạt đang có + gửi thư thông tin đăng nhập. Dùng chung cho gửi / gửi lại (Task 4)
/// và sửa email của hồ sơ đã gửi (Task 6). Gọi SAU Student.RecordCredentialsSent / ChangeEmail và SaveChanges, trong transaction.
/// </summary>
public sealed class CredentialsMailer(
    IAccountCredentials credentials,
    IAccountTokens tokens,
    IAccountEmails emails,
    IEmailSender sender)
{
    public async Task<Result> SendAsync(Student student, CancellationToken ct)
    {
        // @BR-f01-p1-tai-khoan-hoc-vien-003 @BR-f01-p1-tai-khoan-hoc-vien-005: mỗi lần gửi một mật khẩu tạm mới, mật khẩu cũ mất hiệu lực
        var temporaryPassword = await credentials.SetTemporaryPasswordAsync(student.UserId, ct);
        // @BR-f01-p1-tai-khoan-hoc-vien-005: đang đặt mật khẩu riêng bằng mật khẩu tạm cũ → mã kích hoạt cũ bị hủy
        await tokens.RevokeAllAsync(student.UserId, AccountTokenPurpose.Activation, ct);
        return await AccountMail.SendAsync(sender,
            emails.Credentials(student.Email.Value, student.FullName, temporaryPassword, student.TempPasswordExpiresAt!.Value), ct);
    }
}
```

`backend/src/Coaching.Application/Students/Commands/SendCredentials.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Accounts;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

/// <summary>Coach gửi / gửi lại thông tin đăng nhập (mật khẩu tạm 72 giờ) tới email của học viên.</summary>
public sealed class SendCredentialsHandler(
    CurrentCoach currentCoach,
    IStudentRepository students,
    CredentialsMailer mailer,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;

        // @ADR-f01-p1-tai-khoan-hoc-vien-004: đổi hồ sơ + mật khẩu + gửi thư trong một transaction, thư là bước cuối trước commit.
        // Đọc hồ sơ BÊN TRONG operation: execution strategy chạy lại operation khi lỗi tạm thời.
        return await unitOfWork.ExecuteInTransactionAsync<StudentDetailDto>(async ct =>
        {
            if (await students.GetAsync(id, coach.Id, ct) is not { } student)
                return Result<StudentDetailDto>.Failure(StudentErrors.NotFound);
            // Kiểm trước để trả đúng mã: DomainException của aggregate luôn map ra 400
            if (student.AccountStatus == AccountStatus.Activated)
                return Result<StudentDetailDto>.Failure(AccountErrors.AlreadyActivated); // @BR-f01-p1-tai-khoan-hoc-vien-001 → 409
            if (student.CredentialsSentAt is null && student.MissingRequiredFields.Count > 0)
                return Result<StudentDetailDto>.Failure(
                    AccountErrors.RequiredFieldsMissing(student.MissingRequiredFields.Count)); // @BR-f01-p1-tai-khoan-hoc-vien-002

            var now = clock.UtcNow;
            student.RecordCredentialsSent(coach.UserId, now);
            try
            {
                // Lưu hồ sơ TRƯỚC khi UserManager tự SaveChanges → xung đột phiên bản đi qua ConcurrencyConflictException
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (ConcurrencyConflictException)
            {
                return Result<StudentDetailDto>.Failure(StudentErrors.VersionConflict);
            }

            // @BR-f01-p1-tai-khoan-hoc-vien-006: gửi lỗi → trả lỗi → rollback: mật khẩu tạm cũ, hạn cũ, lịch sử giữ nguyên
            var sent = await mailer.SendAsync(student, ct);
            return sent.IsSuccess
                ? Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach, now)) // @BR-f01-p1-tai-khoan-hoc-vien-025: không có mật khẩu
                : Result<StudentDetailDto>.Failure(sent.Error!);
        }, cancellationToken);
    }
}
```

`backend/src/Coaching.Application/Auth/AuthModels.cs` — thêm cuối file:
```csharp

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-002: đăng nhập bằng mật khẩu tạm còn hạn — KHÔNG có phiên (không JWT),
/// chỉ có mã kích hoạt để đặt mật khẩu riêng. JSON: { activationRequired: true, activationToken, activationTokenExpiresAt }.
/// </summary>
public sealed record ActivationChallenge(bool ActivationRequired, string ActivationToken, DateTimeOffset ActivationTokenExpiresAt);

/// <summary>Kết quả đăng nhập thành công: đúng một trong hai có giá trị.</summary>
public sealed record LoginOutcome(AuthTokens? Tokens, ActivationChallenge? Challenge);
```

`backend/src/Coaching.Application/Auth/IAuthService.cs` — thay dòng `Task<Result<AuthTokens>> LoginAsync(...)` bằng:
```csharp
    /// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-002: học viên Chờ kích hoạt + mật khẩu tạm còn hạn → Challenge thay vì Tokens.</summary>
    Task<Result<LoginOutcome>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
```

`backend/src/Coaching.Application/DependencyInjection.cs` — thêm `using Coaching.Application.Accounts;` vào đầu file, rồi thêm trước `return services;`:
```csharp
        services.AddScoped<CredentialsMailer>();
        services.AddScoped<SendCredentialsHandler>();
```

- [x] **Step 9: Infrastructure — `AccountCredentials`, đăng nhập trả mã kích hoạt, seed demo kích hoạt**

`backend/src/Coaching.Infrastructure/Identity/AccountCredentials.cs`:
```csharp
using System.Security.Cryptography;
using Coaching.Application.Accounts;
using Microsoft.AspNetCore.Identity;

namespace Coaching.Infrastructure.Identity;

/// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-001: mật khẩu tạm / riêng là mật khẩu Identity — DB chỉ có PasswordHash.</summary>
internal sealed class AccountCredentials(UserManager<AppUser> users) : IAccountCredentials
{
    public const int TemporaryPasswordLength = 12;

    /// <summary>Bỏ ký tự dễ nhầm khi đọc từ email: 0/O/o, 1/l/I.</summary>
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

    // @BR-f01-p1-tai-khoan-hoc-vien-003 @BR-f01-p1-tai-khoan-hoc-vien-005: mỗi lần gửi một mật khẩu tạm mới thay mật khẩu cũ
    public async Task<string> SetTemporaryPasswordAsync(Guid userId, CancellationToken ct = default)
    {
        var password = RandomNumberGenerator.GetString(Alphabet, TemporaryPasswordLength);
        await ReplacePasswordAsync(await FindAsync(userId), password);
        return password;
    }

    private async Task<AppUser> FindAsync(Guid userId) =>
        await users.FindByIdAsync(userId.ToString())
        ?? throw new InvalidOperationException($"Không tìm thấy tài khoản {userId}.");

    private async Task ReplacePasswordAsync(AppUser user, string password)
    {
        if (await users.HasPasswordAsync(user))
            EnsureSucceeded(await users.RemovePasswordAsync(user), "xóa mật khẩu cũ");
        EnsureSucceeded(await users.AddPasswordAsync(user, password), "đặt mật khẩu mới");
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Không thể {action}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }
}
```

`backend/src/Coaching.Infrastructure/Identity/AuthService.cs` — thay toàn bộ:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Accounts;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Coaching.Infrastructure.Identity;

internal sealed class AuthService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    JwtTokenGenerator tokenGenerator,
    IOptions<JwtOptions> jwtOptions,
    IAccountTokens accountTokens,
    IClock clock) : IAuthService
{
    // @BR-f00-codebase-007
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    // @BR-f00-codebase-011 @BR-f00-codebase-012
    public static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(30);

    public async Task<Result<LoginOutcome>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
            return AuthErrors.InvalidCredentials;

        if (request.App is not { } app)
            return AuthErrors.AppRequired;

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            return AuthErrors.InvalidCredentials;

        // @ADR-f00-codebase-005: tự kiểm khóa theo IClock thay vì IsLockedOutAsync (đồng hồ thật)
        var now = clock.UtcNow;
        if (IsLockedOut(user, now))
            return AuthErrors.InvalidCredentials; // @BR-f00-codebase-006: không lộ trạng thái khóa

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await RegisterFailedAttemptAsync(user, now, cancellationToken);
            return AuthErrors.InvalidCredentials;
        }

        // @ADR-f01-p1-tai-khoan-hoc-vien-002 @ADR-f01-p1-tai-khoan-hoc-vien-001: học viên Chờ kích hoạt chỉ có mật khẩu tạm.
        // Kiểm TRƯỚC vai trò: mật khẩu tạm trên web quản trị nhận thông báo chung, không phải WrongApp.
        if (await FindPendingStudentAsync(user.Id, cancellationToken) is { } pending)
            return await ChallengeAsync(user, pending, app, now, cancellationToken);

        // @ADR-f00-codebase-001 @BR-f00-codebase-004 @BR-f00-codebase-005: sai web → không cấp phiên.
        // Kiểm SAU khi mật khẩu đúng: người chưa biết mật khẩu chỉ nhận thông báo chung (@BR-f00-codebase-006).
        var requiredRole = app == ClientApp.Admin ? Roles.Admin : Roles.Student;
        if (!await userManager.IsInRoleAsync(user, requiredRole))
            return AuthErrors.WrongApp(app);

        await ResetFailedAttemptsAsync(user);
        return new LoginOutcome(await IssueTokensAsync(user, cancellationToken), null);
    }

    private Task<Student?> FindPendingStudentAsync(Guid userId, CancellationToken ct) =>
        db.Students.AsNoTracking()
            .SingleOrDefaultAsync(s => s.UserId == userId && s.AccountStatus == AccountStatus.PendingActivation, ct);

    private async Task<Result<LoginOutcome>> ChallengeAsync(AppUser user, Student pending, ClientApp app, DateTimeOffset now,
        CancellationToken ct)
    {
        // @BR-f01-p1-tai-khoan-hoc-vien-004 @BR-f01-p1-tai-khoan-hoc-vien-012: mật khẩu tạm hết hạn → thông báo chung, tính là một lần sai
        if (!pending.IsTempPasswordValid(now))
        {
            await RegisterFailedAttemptAsync(user, now, ct);
            return AuthErrors.InvalidCredentials;
        }

        // @ADR-f01-p1-tai-khoan-hoc-vien-002: mật khẩu tạm không mở web quản trị
        if (app == ClientApp.Admin)
            return AuthErrors.InvalidCredentials;

        await ResetFailedAttemptsAsync(user);
        // @ADR-f01-p1-tai-khoan-hoc-vien-002: không cấp JWT; mã kích hoạt 30 phút, không quá hạn mật khẩu tạm
        var cap = now + AccountTokenLifetimes.Activation;
        var tempExpiry = pending.TempPasswordExpiresAt!.Value;
        var expiresAt = tempExpiry < cap ? tempExpiry : cap;
        var token = await accountTokens.IssueAsync(user.Id, AccountTokenPurpose.Activation, AccountTokenSource.Login, expiresAt, ct);
        return new LoginOutcome(null, new ActivationChallenge(true, token, expiresAt));
    }

    private static bool IsLockedOut(AppUser user, DateTimeOffset now) =>
        user.LockoutEnd is { } end && end > now;

    // @BR-f00-codebase-007: chỉ xóa bộ đếm khi mật khẩu đúng VÀ đúng web
    private async Task ResetFailedAttemptsAsync(AppUser user)
    {
        if (user.AccessFailedCount != 0 || user.LockoutEnd is not null)
        {
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            await userManager.UpdateAsync(user);
        }
    }

    // @BR-f00-codebase-007: sai 5 lần liên tiếp → khóa 5 phút.
    // Một câu UPDATE nguyên tử: cả hai vế SET tính từ giá trị cũ của dòng → không mất lần đếm khi đăng nhập song song.
    private async Task RegisterFailedAttemptAsync(AppUser user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var lockUntil = now.Add(LockoutDuration);
        await db.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(s => s
            .SetProperty(u => u.LockoutEnd, u => u.AccessFailedCount + 1 >= MaxFailedAttempts ? lockUntil : u.LockoutEnd)
            .SetProperty(u => u.AccessFailedCount, u => u.AccessFailedCount + 1 >= MaxFailedAttempts ? 0 : u.AccessFailedCount + 1),
            cancellationToken);
    }

    public async Task<Result<AuthTokens>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return AuthErrors.InvalidRefreshToken;

        var now = clock.UtcNow;
        var hash = TokenHasher.Hash(request.RefreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (stored is null)
            return AuthErrors.InvalidRefreshToken;

        if (stored.RevokedAt is { } revokedAt)
        {
            if (stored.RevokedReason != RefreshTokenRevokedReason.Rotated)
                return AuthErrors.InvalidRefreshToken; // @BR-f00-codebase-013: phiên đã đăng xuất / đã bị thu hồi

            if (now - revokedAt <= ReuseGracePeriod)
            {
                // @ADR-f00-codebase-003 @BR-f00-codebase-011: gửi trùng trong 30s → cấp cặp mới, không thu hồi
                // Chuỗi chỉ được có MỘT nhánh sống: xoay từ lá mới nhất của chuỗi.
                var leaf = await FindLiveLeafAsync(stored, now, cancellationToken);
                var graceUser = leaf is null ? null : await FindActiveUserAsync(stored.UserId);
                return graceUser is null
                    ? AuthErrors.InvalidRefreshToken
                    : await IssueTokensAsync(graceUser, cancellationToken, rotateFrom: leaf);
            }

            // @BR-f00-codebase-012: gửi lại sau 30s → coi là lộ mã, thu hồi mọi phiên
            await RevokeAllAsync(stored.UserId, now, cancellationToken);
            return AuthErrors.InvalidRefreshToken;
        }

        if (!stored.IsActive(now))
            return AuthErrors.InvalidRefreshToken;

        var user = await FindActiveUserAsync(stored.UserId);
        if (user is null)
            return AuthErrors.InvalidRefreshToken;

        return await IssueTokensAsync(user, cancellationToken, rotateFrom: stored);
    }

    // @ADR-f00-codebase-003: lần theo ReplacedByTokenHash (tối đa 5 bước) tới hậu duệ mới nhất còn hiệu lực.
    private async Task<RefreshToken?> FindLiveLeafAsync(RefreshToken start, DateTimeOffset now, CancellationToken ct)
    {
        var current = start;
        for (var hop = 0; hop < 5 && current.ReplacedByTokenHash is { } next; hop++)
        {
            var child = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == next, ct);
            if (child is null)
                return null;
            if (child.RevokedAt is null)
                return child.IsActive(now) ? child : null;
            current = child;
        }

        return null;
    }

    // Khóa đăng nhập (BR-007) chỉ áp cho đăng nhập; mã làm mới hợp lệ đã chứng minh quyền sở hữu phiên → vẫn gia hạn được.
    private async Task<AppUser?> FindActiveUserAsync(Guid userId) =>
        await userManager.FindByIdAsync(userId.ToString());

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var hash = TokenHasher.Hash(refreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = clock.UtcNow;
            stored.RevokedReason = RefreshTokenRevokedReason.Logout; // @BR-f00-codebase-013
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<AuthTokens> IssueTokensAsync(AppUser user, CancellationToken cancellationToken,
        RefreshToken? rotateFrom = null)
    {
        var now = clock.UtcNow;
        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var (accessToken, accessExpiresAt) = tokenGenerator.CreateAccessToken(user, roles);

        var refreshToken = TokenHasher.NewToken();
        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenHasher.Hash(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenDays),
        };
        db.RefreshTokens.Add(refreshEntity);

        if (rotateFrom is not null)
        {
            rotateFrom.RevokedAt = now;
            rotateFrom.RevokedReason = RefreshTokenRevokedReason.Rotated;
            rotateFrom.ReplacedByTokenHash = refreshEntity.TokenHash;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new AuthTokens(
            accessToken,
            accessExpiresAt,
            refreshToken,
            refreshEntity.ExpiresAt,
            new AuthUser(user.Id, user.Email ?? string.Empty, user.FullName, roles));
    }

    private async Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokedReason, RefreshTokenRevokedReason.ReuseDetected), cancellationToken);
    }
}
```

`backend/src/Coaching.Infrastructure/DependencyInjection.cs` — thêm `using Coaching.Application.Accounts;` vào đầu file, rồi thêm sau dòng `services.AddScoped<IStudentAccounts, StudentAccounts>();`:
```csharp
        services.AddScoped<IAccountCredentials, AccountCredentials>();
```

`backend/src/Coaching.Infrastructure/Seeding/DatabaseInitializer.cs` — thay khối `// @ADR-f02-ho-so-hoc-vien-001: học viên demo …` (từ dòng comment đó đến hết `if (!await db.Students.AnyAsync(s => s.UserId == demo.Id, …)) { … }`) bằng:
```csharp
            // @ADR-f02-ho-so-hoc-vien-001: học viên demo (seed từ F00) cần hồ sơ Student — bổ sung nếu thiếu, idempotent
            var demoStudent = await db.Students.SingleOrDefaultAsync(s => s.UserId == demo.Id, cancellationToken);
            if (demoStudent is null)
            {
                var coach = await db.Coaches.SingleAsync(c => c.UserId == admin.Id, cancellationToken);
                demoStudent = Student.Create(coach.Id, demo.Id, demoSeed.FullName, demoEmail, clock.UtcNow);
                db.Students.Add(demoStudent);
            }

            // @BR-f00-codebase-020: demo đã có mật khẩu riêng từ seed → phải Đã kích hoạt mới đăng nhập được
            // (Chờ kích hoạt chỉ vào bằng mật khẩu tạm — ADR-f01-p1-tai-khoan-hoc-vien-002). Chỉ kích hoạt khi còn Chờ → idempotent.
            if (demoStudent.AccountStatus == AccountStatus.PendingActivation)
                demoStudent.Activate(clock.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
```

- [x] **Step 10: API — route gửi thông tin đăng nhập + body đăng nhập hai dạng**

`backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs` — thêm action cuối class:
```csharp

    // @BR-f01-p1-tai-khoan-hoc-vien-025: trả StudentDetailDto — mật khẩu tạm chỉ có trong email
    [HttpPost("{id:guid}/credentials")]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendCredentials(Guid id, [FromServices] SendCredentialsHandler handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, cancellationToken)).ToActionResult(this);
```

`backend/src/Coaching.Api/Controllers/AuthController.cs` — thay action `Login` bằng:
```csharp
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return result.Error!.ToProblem(this);
        // @ADR-f01-p1-tai-khoan-hoc-vien-002: dạng cũ AuthTokens giữ nguyên; dạng mới { activationRequired, activationToken, activationTokenExpiresAt }
        return result.Value.Tokens is { } tokens ? Ok(tokens) : Ok(result.Value.Challenge);
    }
```

Run: `cd backend && grep -rn "LoginAsync(" src --include=*.cs`
Expected: chỉ còn `IAuthService.cs`, `AuthService.cs`, `AuthController.cs` — không có nơi gọi nào khác cần sửa (test gọi qua HTTP, body `AuthTokens` không đổi).

- [x] **Step 11: Chạy acceptance — kỳ vọng PASS (trừ BR-005 sc.3 của Task 5)**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f01-p1-tai-khoan-hoc-vien-001|Category=BR-f01-p1-tai-khoan-hoc-vien-002|Category=BR-f01-p1-tai-khoan-hoc-vien-003|Category=BR-f01-p1-tai-khoan-hoc-vien-004|Category=BR-f01-p1-tai-khoan-hoc-vien-005|Category=BR-f01-p1-tai-khoan-hoc-vien-006|Category=BR-f01-p1-tai-khoan-hoc-vien-012|Category=BR-f01-p1-tai-khoan-hoc-vien-025)&Category!=web"`
Expected: `Passed: 16`, `Failed: 1` — chỉ scenario "Đang đặt mật khẩu riêng bằng mật khẩu tạm cũ thì không lưu được" (BR-005) báo `MissingStepDefinition` (bind ở Task 5). (BR-001: 3 ví dụ + 1; BR-002: 2; BR-003: 1; BR-004: 2; BR-005: 2; BR-006: 2; BR-012: 2 ví dụ; BR-025: 1.)

- [x] **Step 12: Integration test — helper Capture, mật khẩu tạm, đăng nhập bằng mật khẩu tạm, DTO không có bí mật, demo kích hoạt**

`backend/tests/Coaching.Api.IntegrationTests/ApiFactory.cs`:
- Task 2 đã đổi `ApiFactory` sang `Email:Provider=Capture` + `App:ClientBaseUrl` (`ClientBaseUrl`, `Outbox` đã có). Chỉ thêm hai alias sau method `AdminClientAsync()` để test F01-P1 đọc rõ ý:
```csharp
    /// <summary>Host test đã chạy Email:Provider=Capture (Task 2) — alias để test F01-P1 ghi rõ ý đọc thư.</summary>
    public WebApplicationFactory<Program> Capture => this;

    /// <summary>HttpClient admin seed trên host Capture.</summary>
    public Task<HttpClient> CaptureAdminClientAsync() => AdminClientAsync();
```
- Trong `CreateStudentAsync`, thay dòng
```csharp
        db.Students.Add(Student.Create(coach.Id, user.Id, user.FullName, Email.Create(email), sp.GetRequiredService<IClock>().UtcNow));
```
bằng:
```csharp
        var now = sp.GetRequiredService<IClock>().UtcNow;
        var student = Student.Create(coach.Id, user.Id, user.FullName, Email.Create(email), now);
        // F01-P1: học viên có mật khẩu riêng là học viên Đã kích hoạt — Chờ kích hoạt chỉ đăng nhập được bằng mật khẩu tạm
        student.Activate(now);
        db.Students.Add(student);
```

`backend/tests/Coaching.Api.IntegrationTests/Accounts/AccountRequests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Coaching.Application.Students;
using Coaching.Infrastructure.Emailing;
using Coaching.TestSupport;

namespace Coaching.Api.IntegrationTests.Accounts;

internal static partial class AccountRequests
{
    public const string CredentialsSubject = "Thông tin đăng nhập Duobexiuuu Coaching";

    /// <summary>Hồ sơ đủ mục bắt buộc, tài khoản Chờ kích hoạt.</summary>
    public static async Task<StudentDetailDto> CreateCompleteAsync(HttpClient admin, string? email = null)
    {
        var response = await admin.PostAsJsonAsync("/api/admin/students", new
        {
            fullName = "Học viên tài khoản",
            email = email ?? $"s{Guid.NewGuid():N}@test.local",
            registration = RegistrationSamples.Complete(),
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
    }

    public static Task<HttpResponseMessage> SendCredentialsAsync(HttpClient admin, Guid id) =>
        admin.PostAsJsonAsync($"/api/admin/students/{id}/credentials", new { });

    /// <summary>Gửi thông tin đăng nhập thành công; trả mật khẩu tạm đọc từ thư capture mới nhất tới học viên.</summary>
    public static async Task<string> SendCredentialsOkAsync(ApiFactory factory, HttpClient admin, StudentDetailDto student)
    {
        var response = await SendCredentialsAsync(admin, student.Id);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return TempPassword(factory.Outbox.All.Last(m => m.To == student.Email && m.Subject == CredentialsSubject));
    }

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password, string app = "client") =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password, app });

    public static string TempPassword(CapturedEmail mail) =>
        TempPasswordPattern().Match(mail.TextBody) is { Success: true } match
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"Thư tới {mail.To} không có dòng mật khẩu tạm.");

    [GeneratedRegex(@"Mật khẩu tạm:\s*(\S+)")]
    private static partial Regex TempPasswordPattern();
}
```

`backend/tests/Coaching.Api.IntegrationTests/Accounts/SendCredentialsTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Students;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Accounts;

[Collection(ApiCollection.Name)]
public sealed class SendCredentialsTests(ApiFactory factory)
{
    // @BR-f01-p1-tai-khoan-hoc-vien-025: kết quả gửi và hồ sơ coach xem không bao giờ chứa mật khẩu tạm trong thư
    [Fact]
    public async Task Response_and_profile_never_contain_the_temporary_password()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);

        var sent = await AccountRequests.SendCredentialsAsync(admin, student.Id);

        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        var temp = AccountRequests.TempPassword(factory.Outbox.All.Last(m => m.To == student.Email));
        Assert.DoesNotContain(temp, await sent.Content.ReadAsStringAsync());
        Assert.DoesNotContain(temp, await admin.GetStringAsync($"{StudentRequests.Url}/{student.Id}"));
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-001: mật khẩu tạm là mật khẩu Identity — DB chỉ có hash
    [Fact]
    public async Task Temporary_password_is_stored_only_as_the_identity_hash()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);

        var temp = await AccountRequests.SendCredentialsOkAsync(factory, admin, student);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var userId = await db.Students.Where(s => s.Id == student.Id).Select(s => s.UserId).SingleAsync();
        var user = (await users.FindByIdAsync(userId.ToString()))!;
        Assert.NotNull(user.PasswordHash);
        Assert.DoesNotContain(temp, user.PasswordHash);
        Assert.True(await users.CheckPasswordAsync(user, temp));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-006 @ADR-f01-p1-tai-khoan-hoc-vien-004: dịch vụ email lỗi → 503, rollback hết
    [Fact]
    public async Task Email_failure_rolls_back_the_send()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);
        factory.Outbox.FailNext = true;

        var response = await AccountRequests.SendCredentialsAsync(admin, student.Id);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("account.email_send_failed", await StudentRequests.ErrorCodeAsync(response));
        var detail = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{student.Id}"))!;
        Assert.Equal("NotSent", detail.Account.State);
        Assert.Null(detail.Account.TempPasswordExpiresAt);
        Assert.Empty(detail.Account.History);
        Assert.DoesNotContain(factory.Outbox.All, m => m.To == student.Email);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var userId = await db.Students.Where(s => s.Id == student.Id).Select(s => s.UserId).SingleAsync();
        Assert.False(await users.HasPasswordAsync((await users.FindByIdAsync(userId.ToString()))!));
    }
}
```

`backend/tests/Coaching.Api.IntegrationTests/Accounts/TempPasswordLoginTests.cs`:
```csharp
using System.Net;
using System.Text.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Accounts;

[Collection(ApiCollection.Name)]
public sealed class TempPasswordLoginTests(ApiFactory factory)
{
    private async Task<(Guid StudentId, Guid UserId, string Email, string Temp)> SentAsync()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);
        var temp = await AccountRequests.SendCredentialsOkAsync(factory, admin, student);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = await db.Students.Where(s => s.Id == student.Id).Select(s => s.UserId).SingleAsync();
        return (student.Id, userId, student.Email, temp);
    }

    private async Task SetTempExpiryAsync(Guid studentId, DateTimeOffset expiresAt)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Students] SET [TempPasswordExpiresAt] = {expiresAt} WHERE [Id] = {studentId}");
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-002: mật khẩu tạm còn hạn → 200 có mã kích hoạt, không có phiên nào được cấp
    [Fact]
    public async Task Valid_temp_password_returns_activation_challenge_without_session()
    {
        var (_, userId, email, temp) = await SentAsync();
        var before = DateTimeOffset.UtcNow;

        var response = await AccountRequests.LoginAsync(factory.Capture.CreateClient(), email, temp);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("activationRequired").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("activationToken").GetString()));
        Assert.False(json.RootElement.TryGetProperty("accessToken", out _));
        var expiresAt = json.RootElement.GetProperty("activationTokenExpiresAt").GetDateTimeOffset();
        Assert.InRange(expiresAt, before.AddMinutes(29), DateTimeOffset.UtcNow.AddMinutes(30));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.RefreshTokens.AnyAsync(t => t.UserId == userId));
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-002: mật khẩu tạm trên web quản trị → 401 chung, không phải 403 WrongApp
    [Fact]
    public async Task Temp_password_on_admin_web_returns_generic_401()
    {
        var (_, _, email, temp) = await SentAsync();

        var response = await AccountRequests.LoginAsync(factory.Capture.CreateClient(), email, temp, "admin");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.invalid_credentials", await StudentRequests.ErrorCodeAsync(response));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-012 @ADR-f01-p1-tai-khoan-hoc-vien-002: hết hạn → 401 chung và tính là một lần sai
    [Fact]
    public async Task Expired_temp_password_is_generic_401_and_counts_as_failed_attempt()
    {
        var (studentId, userId, email, temp) = await SentAsync();
        await SetTempExpiryAsync(studentId, DateTimeOffset.UtcNow.AddMinutes(-1));

        var response = await AccountRequests.LoginAsync(factory.Capture.CreateClient(), email, temp);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.invalid_credentials", await StudentRequests.ErrorCodeAsync(response));
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var failed = (await users.FindByIdAsync(userId.ToString()))!.AccessFailedCount;
        Assert.Equal(1, failed);
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-002: mã kích hoạt không sống quá hạn mật khẩu tạm
    [Fact]
    public async Task Activation_token_never_outlives_the_temp_password()
    {
        var (studentId, _, email, temp) = await SentAsync();
        var tempExpiry = DateTimeOffset.UtcNow.AddMinutes(10);
        await SetTempExpiryAsync(studentId, tempExpiry);

        var response = await AccountRequests.LoginAsync(factory.Capture.CreateClient(), email, temp);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(tempExpiry, json.RootElement.GetProperty("activationTokenExpiresAt").GetDateTimeOffset());
    }
}
```

`backend/tests/Coaching.Api.IntegrationTests/Architecture/AccountSecretsDtoTests.cs`:
```csharp
using System.Reflection;
using Coaching.Api.Controllers.Admin;
using Coaching.Api.Controllers.Client;
using Coaching.Application.Students;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.IntegrationTests.Architecture;

/// <summary>
/// @BR-f01-p1-tai-khoan-hoc-vien-025: mọi type trả về từ /api/admin/** và /api/client/** không có property chuỗi nào mang
/// mật khẩu hay mã bí mật (tên chứa Password / Token / Secret), kể cả type lồng nhau. Mốc thời gian như TempPasswordExpiresAt được phép.
/// </summary>
public sealed class AccountSecretsDtoTests
{
    private static readonly string[] SecretWords = ["Password", "Token", "Secret"];

    [Fact]
    public void Admin_and_client_dtos_never_carry_a_password_or_token()
    {
        var produced = typeof(Program).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && (t.IsSubclassOf(typeof(AdminControllerBase)) || t.IsSubclassOf(typeof(ClientControllerBase))))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .SelectMany(a => a.GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Where(p => p.Type != typeof(void))
                .Select(p => (Where: $"{a.DeclaringType!.Name}.{a.Name}", p.Type)))
            .ToList();
        Assert.Contains(produced, p => p.Type == typeof(StudentDetailDto));

        var offenders = new List<string>();
        foreach (var (where, type) in produced)
            Collect(type, where, offenders, []);

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
            if (property.PropertyType == typeof(string)
                && SecretWords.Any(w => property.Name.Contains(w, StringComparison.OrdinalIgnoreCase)))
                offenders.Add($"{path}: {type.Name}.{property.Name}");
            Collect(property.PropertyType, $"{path} > {type.Name}.{property.Name}", offenders, seen);
        }
    }
}
```

`backend/tests/Coaching.Api.IntegrationTests/Students/DemoSeedStudentTests.cs`:
- Thêm vào đầu file:
```csharp
using System.Net;
using System.Net.Http.Json;
using Coaching.Domain.Common;
using Coaching.Domain.Students;
```
- Trong test `Demo_student_gets_a_profile_and_backfill_is_idempotent`, thay dòng
```csharp
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Students] WHERE [UserId] = {userId}");
```
bằng (từ Task 4 demo có dòng lịch sử "Activated" → xóa dòng con trước):
```csharp
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM [StudentAccountEvents] WHERE [StudentId] IN (SELECT [Id] FROM [Students] WHERE [UserId] = {userId})");
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Students] WHERE [UserId] = {userId}");
```
- Thêm test cuối class:
```csharp

    // @BR-f00-codebase-020: học viên demo (mật khẩu riêng từ seed) được kích hoạt → vẫn đăng nhập được dưới luật mật khẩu tạm của F01-P1
    [Fact]
    public async Task Demo_student_is_activated_and_can_log_in()
    {
        await using var host = WithDemoSeed();

        var response = await host.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = DemoEmail, password = "Demo@12345", app = "client" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("accessToken", await response.Content.ReadAsStringAsync());
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var email = Email.Create(DemoEmail);
        Assert.Equal(AccountStatus.Activated,
            await db.Students.Where(s => s.Email == email).Select(s => s.AccountStatus).SingleAsync());
    }
```

- [x] **Step 13: Chạy integration — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Accounts|FullyQualifiedName~AccountSecretsDtoTests|FullyQualifiedName~DemoSeedStudentTests"`
Expected: `Passed: 10` (SendCredentialsTests 3, TempPasswordLoginTests 4, AccountSecretsDtoTests 1, DemoSeedStudentTests 2).

- [x] **Step 14: Verify toàn bộ**

Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: Domain, Application, Integration, scenario F00/F02 và scenario BR-001→006, 012, 025 của F01-P1 xanh. Chỉ được đỏ các scenario `@api` của Task 5–7 với lỗi `MissingStepDefinition` (kich-hoat BR-011, 013, 014, 015, 016; thong-tin-dang-nhap BR-005 sc.3, BR-007→010, 026, 027; toàn bộ dat-lai-mat-khau). Mọi lỗi khác phải sửa trước khi commit.

- [x] **Step 15: Commit**
```bash
git add backend/src backend/tests
git commit -F - <<'EOF'
feat(accounts): coach sends credentials, temp-password login returns activation challenge (BR-f01-p1-001..006, 012, 025)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 5: Kích hoạt — đặt mật khẩu riêng, `PasswordPolicy`, vào thẳng; trạng thái tài khoản cho coach

**Files:**
- Create: `backend/src/Coaching.Domain/Students/PasswordCheck.cs`, `backend/src/Coaching.Domain/Students/PasswordPolicy.cs`
- Modify: `backend/src/Coaching.Domain/Students/Student.cs` (annotation), `backend/src/Coaching.Domain/Students/IStudentRepository.cs`
- Modify: `backend/src/Coaching.Application/Auth/AuthErrors.cs`, `backend/src/Coaching.Application/Auth/IAuthService.cs`, `backend/src/Coaching.Application/Accounts/IAccountCredentials.cs`, `backend/src/Coaching.Application/DependencyInjection.cs`
- Create: `backend/src/Coaching.Application/Accounts/Commands/ActivateAccount.cs`
- Modify: `backend/src/Coaching.Infrastructure/Identity/AccountCredentials.cs`, `backend/src/Coaching.Infrastructure/Identity/AuthService.cs`, `backend/src/Coaching.Infrastructure/Persistence/Repositories/StudentRepository.cs`
- Modify: `backend/src/Coaching.Api/Controllers/AuthController.cs`
- Test: `backend/tests/Coaching.Domain.Tests/Students/PasswordPolicyTests.cs`, `backend/tests/Coaching.Domain.Tests/Students/ActivationTests.cs`
- Test: `backend/tests/Coaching.Application.Tests/Auth/PasswordErrorsTests.cs`
- Test: `backend/tests/Coaching.Acceptance/Support/AccountApi.cs` (M), `backend/tests/Coaching.Acceptance/Steps/ActivationSteps.cs` (C)
- Test: `backend/tests/Coaching.Api.IntegrationTests/Accounts/AccountRequests.cs` (M), `Accounts/ActivationTests.cs` (C)

**Interfaces:**
- Consumes: Task 1 — `Student.GetAccountState(now)`, `AccountState`, `AccountInfoDto(State, CredentialsSentAt, TempPasswordExpiresAt, ActivatedAt, History)`; Task 3 — `IAccountTokens.FindValidAsync/ConsumeAsync`; Task 4 — `Student.Activate(at)`, `Student.IsTempPasswordValid(now)`, `IAccountCredentials`, `AccountCredentials.ReplacePasswordAsync/FindAsync` (private), `AuthService.IssueTokensAsync` (private), `ActivationChallenge`, `AccountApi` + steps `When học viên đăng nhập web học viên bằng mật khẩu tạm đó`, `When coach xem hồ sơ của học viên đó`, `Then đăng nhập bị từ chối` (F00), `Then hệ thống từ chối với thông báo "…"` (F00); `AccountRequests.CreateCompleteAsync/SendCredentialsOkAsync/LoginAsync`; seed demo kích hoạt (đã làm ở Task 4).
- Produces:
  - Domain: `enum PasswordCheck { Ok, TooShort, TooLong, Mismatch }`; `static class PasswordPolicy { const MinLength = 8; const MaxLength = 128; PasswordCheck Check(string? password, string? confirm) }`; `IStudentRepository.GetTrackedByUserIdAsync(Guid userId, CancellationToken)`.
  - Application: `AuthErrors.ActivationInvalid/PasswordTooShort/PasswordTooLong/PasswordMismatch`, `AuthErrors.ForPassword(PasswordCheck) → Error?`; `IAccountCredentials.SetOwnPasswordAsync(Guid userId, string password, CancellationToken)`; `IAuthService.IssueSessionAsync(Guid userId, CancellationToken) → Task<AuthTokens>`; `ActivateAccountCommand(string? ActivationToken, string? Password, string? ConfirmPassword)`, `ActivateAccountHandler.HandleAsync(cmd, ct) → Result<AuthTokens>`.
  - API: `POST /api/auth/activate` → 200 `AuthTokens` / 400 `auth.password_*` / 401 `auth.activation_invalid` (rate limit `auth`).
  - Test: `AccountApi.TryActivateAsync(password, confirm)`, `ReachActivationStepAsync()`, `AssertStudentSessionAsync()`, `StateLabel(AccountInfoDto)`, `CreateActivatedAsync` chạy luồng thật; `AccountRequests.ReachActivationAsync/ActivateAsync/CreateActivatedAsync`; step `When học viên đăng nhập web học viên bằng mật khẩu "…"`, `Then học viên vào được khu vực học viên`, `When học viên lưu mật khẩu riêng "…"` (dùng lại ở Task 6–7).

**Owns:** `@BR-f01-p1-tai-khoan-hoc-vien-011`, `@BR-f01-p1-tai-khoan-hoc-vien-013`, `@BR-f01-p1-tai-khoan-hoc-vien-014`, `@BR-f01-p1-tai-khoan-hoc-vien-015`, `@BR-f01-p1-tai-khoan-hoc-vien-016`, `@BR-f01-p1-tai-khoan-hoc-vien-026` | —

- [ ] **Step 1: Viết Domain test cho `PasswordPolicy`, kích hoạt và trạng thái tài khoản**

`backend/tests/Coaching.Domain.Tests/Students/PasswordPolicyTests.cs`:
```csharp
using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class PasswordPolicyTests
{
    // @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-015: 8–128 ký tự, giữ nguyên dấu và khoảng trắng
    [Theory]
    [InlineData("abcdefgh")]
    [InlineData("Coach2026")]
    [InlineData(" Coach2026 ")]
    [InlineData("Mậtkhẩu1")]
    public void Accepts_8_to_128_characters_as_typed(string password) =>
        Assert.Equal(PasswordCheck.Ok, PasswordPolicy.Check(password, password));

    // @BR-f01-p1-tai-khoan-hoc-vien-013
    [Fact]
    public void Accepts_exactly_128_characters()
    {
        var password = new string('a', 128);
        Assert.Equal(PasswordCheck.Ok, PasswordPolicy.Check(password, password));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-013
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc123")]
    [InlineData("abcdefg")]
    public void Rejects_shorter_than_8(string? password) =>
        Assert.Equal(PasswordCheck.TooShort, PasswordPolicy.Check(password, password));

    // @BR-f01-p1-tai-khoan-hoc-vien-013
    [Fact]
    public void Rejects_129_characters()
    {
        var password = new string('a', 129);
        Assert.Equal(PasswordCheck.TooLong, PasswordPolicy.Check(password, password));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-014 @BR-f01-p1-tai-khoan-hoc-vien-015: so khớp từng ký tự (ordinal), không trim
    [Theory]
    [InlineData("Coach2026", "Coach2027")]
    [InlineData(" Coach2026 ", "Coach2026")]
    [InlineData("Coach2026", "coach2026")]
    [InlineData("Coach2026", null)]
    public void Rejects_confirmation_that_differs_by_any_character(string password, string? confirm) =>
        Assert.Equal(PasswordCheck.Mismatch, PasswordPolicy.Check(password, confirm));
}
```

`backend/tests/Coaching.Domain.Tests/Students/ActivationTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.TestSupport;

namespace Coaching.Domain.Tests.Students;

public sealed class ActivationTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 1, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SentAt = new(2026, 11, 1, 9, 0, 0, TimeSpan.FromHours(7));
    private static readonly Guid Coach = Guid.NewGuid();

    private static Student SentStudent()
    {
        var student = Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Quân", Email.Create("quan@gmail.com"), CreatedAt,
            RegistrationForm.Create(RegistrationSamples.Complete(), new DateOnly(2026, 11, 1)));
        student.RecordCredentialsSent(Coach, SentAt);
        return student;
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-016: Đã kích hoạt kèm thời điểm, mật khẩu tạm hết hiệu lực ngay, thêm đúng một dòng lịch sử
    [Fact]
    public void Activate_moves_pending_to_activated_and_retires_temp_password()
    {
        var student = SentStudent();
        var at = SentAt.AddHours(5);

        student.Activate(at);

        Assert.Equal(AccountStatus.Activated, student.AccountStatus);
        Assert.Equal(at, student.ActivatedAt);
        Assert.Null(student.TempPasswordExpiresAt);
        Assert.False(student.IsTempPasswordValid(at));
        var row = student.AccountEvents[^1];
        Assert.Equal(AccountEventType.Activated, row.Type);
        Assert.Equal(at, row.At);
        Assert.Equal(student.UserId, row.ActorUserId);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-016: chỉ Chờ kích hoạt mới kích hoạt được
    [Fact]
    public void Activate_twice_is_rejected_without_history()
    {
        var student = SentStudent();
        student.Activate(SentAt.AddHours(1));
        var events = student.AccountEvents.Count;

        var ex = Assert.Throws<DomainException>(() => student.Activate(SentAt.AddHours(2)));

        Assert.Equal("account.already_activated", ex.Code);
        Assert.Equal(events, student.AccountEvents.Count);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-026: chưa gửi → đã gửi → hết hạn ở đúng 72 giờ → đã kích hoạt
    [Fact]
    public void Account_state_follows_send_expiry_and_activation()
    {
        var student = Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Quân", Email.Create("quan@gmail.com"), CreatedAt,
            RegistrationForm.Create(RegistrationSamples.Complete(), new DateOnly(2026, 11, 1)));
        Assert.Equal(AccountState.NotSent, student.GetAccountState(SentAt));

        student.RecordCredentialsSent(Coach, SentAt);

        Assert.Equal(AccountState.Sent, student.GetAccountState(SentAt.AddHours(24)));
        Assert.Equal(AccountState.TempPasswordExpired, student.GetAccountState(SentAt.AddHours(72)));
        student.Activate(SentAt.AddHours(80));
        Assert.Equal(AccountState.Activated, student.GetAccountState(SentAt.AddHours(81)));
    }
}
```

- [ ] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students.PasswordPolicyTests|FullyQualifiedName~Coaching.Domain.Tests.Students.ActivationTests"`
Expected: FAIL — build error `The name 'PasswordPolicy' does not exist in the current context` / `The type or namespace name 'PasswordCheck' could not be found`.

- [ ] **Step 3: Domain — `PasswordCheck`, `PasswordPolicy`, annotation trạng thái + kích hoạt, repository**

`backend/src/Coaching.Domain/Students/PasswordCheck.cs`:
```csharp
namespace Coaching.Domain.Students;

public enum PasswordCheck
{
    Ok,
    TooShort,
    TooLong,
    Mismatch,
}
```

`backend/src/Coaching.Domain/Students/PasswordPolicy.cs`:
```csharp
namespace Coaching.Domain.Students;

/// <summary>Luật mật khẩu riêng — dùng cho cả kích hoạt (Task 5) và đặt lại qua link (Task 7).</summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    /// <summary>
    /// @BR-f01-p1-tai-khoan-hoc-vien-013: 8–128 ký tự (string.Length). @BR-f01-p1-tai-khoan-hoc-vien-015: không trim, giữ dấu.
    /// @BR-f01-p1-tai-khoan-hoc-vien-014: hai lần nhập khớp theo so sánh ordinal.
    /// </summary>
    public static PasswordCheck Check(string? password, string? confirm)
    {
        var length = password?.Length ?? 0;
        if (length < MinLength)
            return PasswordCheck.TooShort;
        if (length > MaxLength)
            return PasswordCheck.TooLong;
        return string.Equals(password, confirm, StringComparison.Ordinal) ? PasswordCheck.Ok : PasswordCheck.Mismatch;
    }
}
```

`backend/src/Coaching.Domain/Students/Student.cs`:
- Thêm dòng `// @BR-f01-p1-tai-khoan-hoc-vien-026: chưa gửi / đã gửi / mật khẩu tạm đã hết hạn / đã kích hoạt` ngay trên khai báo `public AccountState GetAccountState(DateTimeOffset now)` (Task 1).
- Trong `Activate`, thay dòng `if (AccountStatus == AccountStatus.Activated)` (dòng đầu thân hàm `Activate`) bằng:
```csharp
        // @BR-f01-p1-tai-khoan-hoc-vien-016: chỉ Chờ kích hoạt → Đã kích hoạt; ghi thời điểm, mật khẩu tạm hết hiệu lực ngay
        if (AccountStatus == AccountStatus.Activated)
```

`backend/src/Coaching.Domain/Students/IStudentRepository.cs` — thêm sau `GetByUserIdAsync`:
```csharp

    /// <summary>
    /// Hồ sơ của tài khoản đăng nhập, được theo dõi để sửa (kích hoạt). Không lọc theo coach: quyền đến từ mã kích hoạt đã băm.
    /// </summary>
    Task<Student?> GetTrackedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
```

`backend/src/Coaching.Infrastructure/Persistence/Repositories/StudentRepository.cs` — thêm sau `GetByUserIdAsync`:
```csharp

    public Task<Student?> GetTrackedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Students.SingleOrDefaultAsync(s => s.UserId == userId, cancellationToken);
```

- [ ] **Step 4: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students.PasswordPolicyTests|FullyQualifiedName~Coaching.Domain.Tests.Students.ActivationTests"`
Expected: `Passed: 17` (PasswordPolicyTests 14, ActivationTests 3).

- [ ] **Step 5: Viết Application test cho lỗi mật khẩu / mã kích hoạt**

`backend/tests/Coaching.Application.Tests/Auth/PasswordErrorsTests.cs`:
```csharp
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Tests.Auth;

public sealed class PasswordErrorsTests
{
    // @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-014
    [Theory]
    [InlineData(PasswordCheck.TooShort, "auth.password_too_short", "Mật khẩu cần ít nhất 8 ký tự.")]
    [InlineData(PasswordCheck.TooLong, "auth.password_too_long", "Mật khẩu tối đa 128 ký tự.")]
    [InlineData(PasswordCheck.Mismatch, "auth.password_mismatch", "Hai lần nhập mật khẩu không khớp.")]
    public void Password_check_maps_to_a_400_error(PasswordCheck check, string code, string message)
    {
        var error = AuthErrors.ForPassword(check);

        Assert.NotNull(error);
        Assert.Equal(code, error.Code);
        Assert.Equal(message, error.Message);
        Assert.Equal(ErrorKind.Validation, error.Kind);
    }

    [Fact]
    public void Valid_password_has_no_error() => Assert.Null(AuthErrors.ForPassword(PasswordCheck.Ok));

    // @BR-f01-p1-tai-khoan-hoc-vien-011: không có phiên hợp lệ để đặt mật khẩu → 401, hướng dẫn đăng nhập lại bằng mật khẩu tạm mới nhất
    [Fact]
    public void Activation_invalid_is_401_with_relogin_message()
    {
        Assert.Equal("auth.activation_invalid", AuthErrors.ActivationInvalid.Code);
        Assert.Equal("Phiên kích hoạt đã hết hạn. Vui lòng đăng nhập lại bằng mật khẩu tạm mới nhất.", AuthErrors.ActivationInvalid.Message);
        Assert.Equal(ErrorKind.Unauthorized, AuthErrors.ActivationInvalid.Kind);
    }
}
```

Run: `cd backend && dotnet test tests/Coaching.Application.Tests --filter "FullyQualifiedName~PasswordErrorsTests"`
Expected: FAIL — build error `'AuthErrors' does not contain a definition for 'ForPassword'`.

- [ ] **Step 6: Application — lỗi, port mật khẩu riêng, cấp phiên, use case kích hoạt**

`backend/src/Coaching.Application/Auth/AuthErrors.cs` — thêm `using Coaching.Domain.Students;` vào đầu file, rồi thêm cuối class:
```csharp

    // @BR-f01-p1-tai-khoan-hoc-vien-011 @BR-f01-p1-tai-khoan-hoc-vien-016: mã kích hoạt hết hạn / đã bị hủy (coach gửi lại, sửa email) / đã dùng
    public static readonly Error ActivationInvalid = new(
        "auth.activation_invalid",
        "Phiên kích hoạt đã hết hạn. Vui lòng đăng nhập lại bằng mật khẩu tạm mới nhất.",
        ErrorKind.Unauthorized);

    // @BR-f01-p1-tai-khoan-hoc-vien-013
    public static readonly Error PasswordTooShort = new("auth.password_too_short", "Mật khẩu cần ít nhất 8 ký tự.");

    // @BR-f01-p1-tai-khoan-hoc-vien-013
    public static readonly Error PasswordTooLong = new("auth.password_too_long", "Mật khẩu tối đa 128 ký tự.");

    // @BR-f01-p1-tai-khoan-hoc-vien-014
    public static readonly Error PasswordMismatch = new("auth.password_mismatch", "Hai lần nhập mật khẩu không khớp.");

    /// <summary>Lỗi 400 tương ứng kết quả PasswordPolicy.Check; null khi mật khẩu hợp lệ. Dùng cho kích hoạt và đặt lại.</summary>
    public static Error? ForPassword(PasswordCheck check) => check switch
    {
        PasswordCheck.TooShort => PasswordTooShort,
        PasswordCheck.TooLong => PasswordTooLong,
        PasswordCheck.Mismatch => PasswordMismatch,
        _ => null,
    };
```

`backend/src/Coaching.Application/Auth/IAuthService.cs` — thêm sau `LoginAsync`:
```csharp

    /// <summary>Cấp phiên (access + refresh token) cho tài khoản vừa kích hoạt — học viên vào thẳng (@BR-f01-p1-tai-khoan-hoc-vien-016).</summary>
    Task<AuthTokens> IssueSessionAsync(Guid userId, CancellationToken cancellationToken = default);
```

`backend/src/Coaching.Application/Accounts/IAccountCredentials.cs` — thêm sau `SetTemporaryPasswordAsync`:
```csharp

    /// <summary>Đặt mật khẩu riêng đúng từng ký tự (đã qua PasswordPolicy); mật khẩu cũ (kể cả mật khẩu tạm) mất hiệu lực.</summary>
    Task SetOwnPasswordAsync(Guid userId, string password, CancellationToken ct = default);
```

`backend/src/Coaching.Application/Accounts/Commands/ActivateAccount.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Accounts.Commands;

public sealed record ActivateAccountCommand(string? ActivationToken, string? Password, string? ConfirmPassword);

/// <summary>Học viên (đã đăng nhập bằng mật khẩu tạm còn hạn) đặt mật khẩu riêng → Đã kích hoạt → vào thẳng.</summary>
public sealed class ActivateAccountHandler(
    IAccountTokens tokens,
    IStudentRepository students,
    IAccountCredentials credentials,
    IAuthService auth,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<AuthTokens>> HandleAsync(ActivateAccountCommand command, CancellationToken cancellationToken = default)
    {
        // @BR-f01-p1-tai-khoan-hoc-vien-011: quyền đặt mật khẩu chỉ đến từ mã kích hoạt còn hiệu lực (không có JWT)
        if (await tokens.FindValidAsync(command.ActivationToken, AccountTokenPurpose.Activation, cancellationToken) is not { } userId)
            return AuthErrors.ActivationInvalid;

        // @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-014: lỗi mật khẩu trả 400 và KHÔNG tiêu mã kích hoạt
        if (AuthErrors.ForPassword(PasswordPolicy.Check(command.Password, command.ConfirmPassword)) is { } invalid)
            return invalid;

        var activated = await unitOfWork.ExecuteInTransactionAsync<Guid>(async ct =>
        {
            var now = clock.UtcNow;
            if (await students.GetTrackedByUserIdAsync(userId, ct) is not { } student || !student.IsTempPasswordValid(now))
                return Result<Guid>.Failure(AuthErrors.ActivationInvalid);

            student.Activate(now); // @BR-f01-p1-tai-khoan-hoc-vien-016
            await unitOfWork.SaveChangesAsync(ct);
            // @BR-f01-p1-tai-khoan-hoc-vien-015: lưu đúng từng ký tự; @BR-f01-p1-tai-khoan-hoc-vien-016: mật khẩu tạm mất hiệu lực ngay
            await credentials.SetOwnPasswordAsync(userId, command.Password!, ct);
            await tokens.ConsumeAsync(command.ActivationToken!, ct);
            return Result<Guid>.Success(userId);
        }, cancellationToken);
        if (!activated.IsSuccess)
            return activated.Error!;

        return await auth.IssueSessionAsync(userId, cancellationToken); // @BR-f01-p1-tai-khoan-hoc-vien-016: vào thẳng
    }
}
```

`backend/src/Coaching.Application/DependencyInjection.cs` — thêm `using Coaching.Application.Accounts.Commands;` vào đầu file, rồi thêm trước `return services;`:
```csharp
        services.AddScoped<ActivateAccountHandler>();
```

- [ ] **Step 7: Infrastructure + API — mật khẩu riêng, cấp phiên, route kích hoạt**

`backend/src/Coaching.Infrastructure/Identity/AccountCredentials.cs` — thêm sau `SetTemporaryPasswordAsync`:
```csharp

    // @BR-f01-p1-tai-khoan-hoc-vien-015: lưu đúng từng ký tự (UserManager không trim); @BR-f01-p1-tai-khoan-hoc-vien-016: mật khẩu tạm bị thay
    public async Task SetOwnPasswordAsync(Guid userId, string password, CancellationToken ct = default) =>
        await ReplacePasswordAsync(await FindAsync(userId), password);
```

`backend/src/Coaching.Infrastructure/Identity/AuthService.cs`:
- Thay dòng `        // @ADR-f01-p1-tai-khoan-hoc-vien-002: mật khẩu tạm không mở web quản trị` bằng:
```csharp
        // @ADR-f01-p1-tai-khoan-hoc-vien-002 @BR-f01-p1-tai-khoan-hoc-vien-011: mật khẩu tạm không mở web quản trị (thông báo chung)
```
- Thay dòng `        // @ADR-f01-p1-tai-khoan-hoc-vien-002: không cấp JWT; mã kích hoạt 30 phút, không quá hạn mật khẩu tạm` bằng:
```csharp
        // @ADR-f01-p1-tai-khoan-hoc-vien-002 @BR-f01-p1-tai-khoan-hoc-vien-011: không cấp JWT → chưa gọi được API dữ liệu nào;
        // mã kích hoạt 30 phút, không quá hạn mật khẩu tạm
```
- Thêm method sau `LoginAsync`:
```csharp

    public async Task<AuthTokens> IssueSessionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"Không tìm thấy tài khoản {userId}.");
        return await IssueTokensAsync(user, cancellationToken);
    }
```

`backend/src/Coaching.Api/Controllers/AuthController.cs` — thêm `using Coaching.Application.Accounts.Commands;` vào đầu file, rồi thêm action sau `Login`:
```csharp

    // @BR-f01-p1-tai-khoan-hoc-vien-016: kích hoạt thành công → 200 AuthTokens (vào thẳng); không yêu cầu JWT, quyền đến từ mã kích hoạt
    [HttpPost("activate")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Activate(ActivateAccountCommand command, [FromServices] ActivateAccountHandler handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(command, cancellationToken)).ToActionResult(this);
```

Run: `cd backend && dotnet test tests/Coaching.Application.Tests --filter "FullyQualifiedName~PasswordErrorsTests"`
Expected: `Passed: 5`.

- [ ] **Step 8: Acceptance — `AccountApi` kích hoạt bằng luồng thật + step defs BR-005 sc.3, BR-011, 013–016, 026**

`backend/tests/Coaching.Acceptance/Support/AccountApi.cs`:
- Thêm `using System.Globalization;` vào đầu file.
- Thay toàn bộ method `CreateActivatedAsync` (cả comment `<summary>` phía trên) bằng:
```csharp
    /// <summary>Học viên Đã kích hoạt qua luồng thật: coach gửi → đăng nhập bằng mật khẩu tạm → đặt mật khẩu riêng.</summary>
    public async Task<StudentDetailDto> CreateActivatedAsync(string name = StudentName, string? email = null,
        string password = DefaultOwnPassword)
    {
        var detail = await CreateAsync(name, email);
        await SendOkAsync(name);
        await StudentLoginAsync(TempPasswords[^1], email: Emails[name]);
        await AssertActivationChallengeAsync();
        Assert.Equal(HttpStatusCode.OK, (await TryActivateAsync(password, password)).StatusCode);
        return detail;
    }

    /// <summary>Hồ sơ mới, coach gửi, học viên đăng nhập bằng mật khẩu tạm → đang ở bước đặt mật khẩu riêng.</summary>
    public async Task ReachActivationStepAsync()
    {
        await CreateAsync();
        await SendOkAsync();
        await StudentLoginAsync(TempPasswords[^1]);
        await AssertActivationChallengeAsync();
    }

    /// <summary>POST /api/auth/activate bằng mã kích hoạt của lần đăng nhập gần nhất.</summary>
    public async Task<HttpResponseMessage> TryActivateAsync(string password, string confirmPassword)
    {
        var response = await ctx.PostJsonAsync("/api/auth/activate",
            new { activationToken = ActivationToken, password, confirmPassword });
        if (response.StatusCode == HttpStatusCode.OK)
            OwnPassword = password;
        return response;
    }

    /// <summary>Phản hồi gần nhất là phiên học viên (AuthTokens) gọi được /api/client.</summary>
    public async Task AssertStudentSessionAsync()
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        var tokens = (await ctx.LastResponse.Content.ReadFromJsonAsync<AuthTokens>())!;
        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));
        ctx.Tokens = tokens;
        var me = await ctx.GetAsync("/api/client/me", tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    /// <summary>@BR-f01-p1-tai-khoan-hoc-vien-026: câu trạng thái tài khoản coach thấy, theo giờ Việt Nam.</summary>
    public static string StateLabel(AccountInfoDto account) => account.State switch
    {
        "NotSent" => "Chưa gửi thông tin đăng nhập",
        "Sent" => $"Đã gửi lúc {Vietnam(account.CredentialsSentAt!.Value, "dd'/'MM HH':'mm")}, "
            + $"mật khẩu tạm còn hạn đến {Vietnam(account.TempPasswordExpiresAt!.Value, "dd'/'MM HH':'mm")}",
        "TempPasswordExpired" => "Mật khẩu tạm đã hết hạn",
        "Activated" => $"Đã kích hoạt ngày {Vietnam(account.ActivatedAt!.Value, "dd'/'MM'/'yyyy")}",
        _ => throw new InvalidOperationException($"Trạng thái tài khoản lạ: {account.State}"),
    };

    private static string Vietnam(DateTimeOffset instant, string format) =>
        instant.ToOffset(StepText.VietnamOffset).ToString(format, CultureInfo.InvariantCulture);
```

`backend/tests/Coaching.Acceptance/Steps/ActivationSteps.cs`:
```csharp
using System.Net;
using Coaching.Acceptance.Support;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class ActivationSteps(AcceptanceContext ctx, StudentApi api, AccountApi account)
{
    // @BR-f01-p1-tai-khoan-hoc-vien-011 @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-014 (dùng cho BR-005 sc.3)
    [Given("học viên đã đăng nhập bằng mật khẩu tạm và đang ở bước đặt mật khẩu riêng")]
    [Given("học viên đã đăng nhập bằng mật khẩu tạm và chưa đặt mật khẩu riêng")]
    [Given("học viên đang đặt mật khẩu riêng bằng mật khẩu tạm còn hiệu lực")]
    public Task AtActivationStep() => account.ReachActivationStepAsync();

    // BR-005 sc.3 (rule của Task 4): coach gửi lại khi học viên đang đặt mật khẩu bằng mật khẩu tạm cũ
    [Given("sau đó coach gửi lại thông tin đăng nhập cho học viên đó")]
    public Task CoachResends() => account.SendOkAsync();

    [When("học viên lưu một mật khẩu riêng hợp lệ")]
    public Task SaveValidPassword() => account.TryActivateAsync("Coach2026", "Coach2026");

    [Then("mật khẩu riêng không được lưu")]
    public Task NotSaved() => api.AssertErrorAsync(HttpStatusCode.Unauthorized, "auth.activation_invalid");

    [Then("học viên phải đăng nhập lại bằng mật khẩu tạm mới")]
    public async Task MustLogInWithNewTemp()
    {
        await account.AssertProblemTitleAsync("Phiên kích hoạt đã hết hạn. Vui lòng đăng nhập lại bằng mật khẩu tạm mới nhất.");
        Assert.Equal("PendingActivation", (await account.ViewAsync()).AccountStatus);
        await account.StudentLoginAsync(account.TempPasswords[^1]);
        await account.AssertActivationChallengeAsync();
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-011: thứ duy nhất học viên có là mã kích hoạt — không phải JWT
    [When("học viên xem hồ sơ của mình")]
    public Task ViewOwnProfile() => ctx.GetAsync("/api/client/me", account.ActivationToken);

    [Then("học viên bị từ chối vì cần đặt mật khẩu riêng trước")]
    public async Task RejectedUntilActivated()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, ctx.LastResponse!.StatusCode);
        Assert.Equal("PendingActivation", (await account.ViewAsync()).AccountStatus);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-011
    [Given("một học viên vừa được gửi mật khẩu tạm")]
    public async Task JustSentTemp()
    {
        await account.CreateAsync();
        await account.SendOkAsync();
    }

    [When("học viên đăng nhập web quản trị bằng mật khẩu tạm đó")]
    public Task LoginAdminWithTemp() => account.StudentLoginAsync(account.FocusTemp!, "admin");

    // @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-014
    [When(@"^học viên lưu mật khẩu riêng ""([^""]*)""$")]
    public Task SavePassword(string password) => account.TryActivateAsync(password, password);

    [When(@"^học viên lưu một mật khẩu riêng dài ""(\d+)"" ký tự$")]
    public Task SavePasswordOfLength(int length)
    {
        var password = new string('a', length);
        return account.TryActivateAsync(password, password);
    }

    [When(@"^học viên lưu mật khẩu riêng ""([^""]*)"", nhập lại ""([^""]*)""$")]
    public Task SavePasswordWithConfirmation(string password, string confirm) => account.TryActivateAsync(password, confirm);

    [Then("mật khẩu riêng được lưu")]
    public Task Saved() => account.AssertStudentSessionAsync();

    [Then(@"^mật khẩu riêng không được lưu vì cần ít nhất ""(\d+)"" ký tự$")]
    public async Task TooShort(int min)
    {
        await api.AssertErrorAsync(HttpStatusCode.BadRequest, "auth.password_too_short");
        await account.AssertProblemTitleAsync($"Mật khẩu cần ít nhất {min} ký tự.");
    }

    [Then(@"^mật khẩu riêng không được lưu vì dài quá ""(\d+)"" ký tự$")]
    public async Task TooLong(int max)
    {
        await api.AssertErrorAsync(HttpStatusCode.BadRequest, "auth.password_too_long");
        await account.AssertProblemTitleAsync($"Mật khẩu tối đa {max} ký tự.");
    }

    [Then("mật khẩu riêng không được lưu vì hai lần nhập không khớp")]
    public async Task Mismatch()
    {
        await api.AssertErrorAsync(HttpStatusCode.BadRequest, "auth.password_mismatch");
        await account.AssertProblemTitleAsync("Hai lần nhập mật khẩu không khớp.");
    }

    [Then("tài khoản của học viên vẫn Chờ kích hoạt")]
    public async Task StillPending() => Assert.Equal("PendingActivation", (await account.ViewAsync()).AccountStatus);

    // @BR-f01-p1-tai-khoan-hoc-vien-015
    [Given(@"^học viên đã kích hoạt tài khoản với mật khẩu riêng ""([^""]*)""$")]
    public Task ActivatedWithPassword(string password) => account.CreateActivatedAsync(password: password);

    // @BR-f01-p1-tai-khoan-hoc-vien-015 (dùng lại ở BR-021)
    [When(@"^học viên đăng nhập web học viên bằng mật khẩu ""([^""]*)""$")]
    public Task LoginWithPassword(string password) => account.StudentLoginAsync(password);

    // @BR-f01-p1-tai-khoan-hoc-vien-015 @BR-f01-p1-tai-khoan-hoc-vien-016
    [Then("học viên vào được khu vực học viên")]
    public Task InStudentArea() => account.AssertStudentSessionAsync();

    // @BR-f01-p1-tai-khoan-hoc-vien-016
    [Given("học viên đã kích hoạt tài khoản bằng mật khẩu tạm còn hiệu lực")]
    public async Task ActivatedWithValidTemp()
    {
        await account.ReachActivationStepAsync();
        account.FocusTemp = account.TempPasswords[^1];
        Assert.Equal(HttpStatusCode.OK, (await account.TryActivateAsync("Coach2026", "Coach2026")).StatusCode);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-026
    [Given("một học viên chưa được gửi thông tin đăng nhập lần nào")]
    public Task NeverSent() => account.CreateAsync();

    [Given(@"^một học viên được gửi thông tin đăng nhập lúc (\d\d)/(\d\d) (\d\d):(\d\d), bây giờ là (\d\d)/(\d\d) (\d\d):(\d\d)$")]
    public async Task SentAtThenNow(int sentDay, int sentMonth, int sentHour, int sentMinute,
        int nowDay, int nowMonth, int nowHour, int nowMinute)
    {
        var year = ctx.Clock.GetUtcNow().Year;
        await account.CreateAsync();
        await api.MoveClockToAsync(new DateTimeOffset(year, sentMonth, sentDay, sentHour, sentMinute, 0, StepText.VietnamOffset));
        await account.SendOkAsync();
        await api.MoveClockToAsync(new DateTimeOffset(year, nowMonth, nowDay, nowHour, nowMinute, 0, StepText.VietnamOffset));
    }

    [Given(@"^một học viên kích hoạt tài khoản ngày (\d\d/\d\d/\d{4})$")]
    public async Task ActivatedOn(string date)
    {
        await account.CreateActivatedAsync();
        // FakeTimeProvider chỉ tua tiến (mọi scenario bắt đầu 01/11/2026) → ghi thẳng thời điểm kích hoạt trong quá khứ
        var activatedAt = StepText.VietnamTime(date, 9, 0);
        await using var scope = ctx.Host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Students] SET [ActivatedAt] = {activatedAt} WHERE [Id] = {account.Id()}");
    }

    [Then(@"^coach thấy trạng thái tài khoản ""([^""]*)""$")]
    public void SeesAccountState(string label) => Assert.Equal(label, AccountApi.StateLabel(account.LastDetail!.Account));
}
```

- [ ] **Step 9: Chạy acceptance — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f01-p1-tai-khoan-hoc-vien-005|Category=BR-f01-p1-tai-khoan-hoc-vien-011|Category=BR-f01-p1-tai-khoan-hoc-vien-013|Category=BR-f01-p1-tai-khoan-hoc-vien-014|Category=BR-f01-p1-tai-khoan-hoc-vien-015|Category=BR-f01-p1-tai-khoan-hoc-vien-016|Category=BR-f01-p1-tai-khoan-hoc-vien-026)&Category!=web"`
Expected: `Passed: 18`, `Failed: 0` (BR-005: 3; BR-011: 2; BR-013: 3; BR-014: 2; BR-015: 2 ví dụ + 1; BR-016: 1; BR-026: 4 ví dụ).
Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f01-p1-tai-khoan-hoc-vien-001|Category=BR-f01-p1-tai-khoan-hoc-vien-012)&Category!=web"`
Expected: `Passed: 6` (BR-001 sc.2 và BR-012 vẫn xanh khi `CreateActivatedAsync` chạy luồng thật).

- [ ] **Step 10: Integration test kích hoạt**

`backend/tests/Coaching.Api.IntegrationTests/Accounts/AccountRequests.cs`:
- Thêm `using System.Text.Json;` vào đầu file.
- Thêm cuối class (trước `[GeneratedRegex…]`):
```csharp
    /// <summary>Hồ sơ mới → gửi thông tin → đăng nhập bằng mật khẩu tạm; trả mật khẩu tạm và mã kích hoạt.</summary>
    public static async Task<(StudentDetailDto Student, string Temp, string ActivationToken)> ReachActivationAsync(ApiFactory factory)
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await CreateCompleteAsync(admin);
        var temp = await SendCredentialsOkAsync(factory, admin, student);
        var login = await LoginAsync(factory.Capture.CreateClient(), student.Email, temp);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return (student, temp, json.RootElement.GetProperty("activationToken").GetString()!);
    }

    public static Task<HttpResponseMessage> ActivateAsync(HttpClient client, string token, string password, string confirmPassword) =>
        client.PostAsJsonAsync("/api/auth/activate", new { activationToken = token, password, confirmPassword });

    /// <summary>Học viên Đã kích hoạt qua luồng thật với mật khẩu riêng <paramref name="password"/>.</summary>
    public static async Task<StudentDetailDto> CreateActivatedAsync(ApiFactory factory, string password = "Coach2025")
    {
        var (student, _, token) = await ReachActivationAsync(factory);
        var response = await ActivateAsync(factory.Capture.CreateClient(), token, password, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return student;
    }

```

`backend/tests/Coaching.Api.IntegrationTests/Accounts/ActivationTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Auth;
using Coaching.Application.Students;

namespace Coaching.Api.IntegrationTests.Accounts;

[Collection(ApiCollection.Name)]
public sealed class ActivationTests(ApiFactory factory)
{
    // @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-014: lỗi mật khẩu không tiêu mã kích hoạt
    [Fact]
    public async Task Invalid_password_keeps_the_activation_token()
    {
        var (_, _, token) = await AccountRequests.ReachActivationAsync(factory);
        var client = factory.Capture.CreateClient();

        var tooShort = await AccountRequests.ActivateAsync(client, token, "abc123", "abc123");
        var mismatch = await AccountRequests.ActivateAsync(client, token, "Coach2026", "Coach2027");
        var saved = await AccountRequests.ActivateAsync(client, token, "Coach2026", "Coach2026");

        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        Assert.Equal("auth.password_too_short", await StudentRequests.ErrorCodeAsync(tooShort));
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
        Assert.Equal("auth.password_mismatch", await StudentRequests.ErrorCodeAsync(mismatch));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-016: vào thẳng, Đã kích hoạt kèm thời điểm; mật khẩu tạm và mã kích hoạt hết hiệu lực
    [Fact]
    public async Task Activation_signs_in_and_retires_temp_password_and_token()
    {
        var (student, temp, token) = await AccountRequests.ReachActivationAsync(factory);
        var client = factory.Capture.CreateClient();

        var response = await AccountRequests.ActivateAsync(client, token, "Coach2026", "Coach2026");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = (await response.Content.ReadFromJsonAsync<AuthTokens>())!;
        using var me = new HttpRequestMessage(HttpMethod.Get, "/api/client/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(me)).StatusCode);

        var admin = await factory.CaptureAdminClientAsync();
        var detail = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{student.Id}"))!;
        Assert.Equal("Activated", detail.AccountStatus);
        Assert.Equal("Activated", detail.Account.State);
        Assert.NotNull(detail.Account.ActivatedAt);
        Assert.Equal("Activated", detail.Account.History[0].Type);

        var tempLogin = await AccountRequests.LoginAsync(client, student.Email, temp);
        Assert.Equal(HttpStatusCode.Unauthorized, tempLogin.StatusCode);
        var reuse = await AccountRequests.ActivateAsync(client, token, "Coach2027", "Coach2027");
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Equal("auth.activation_invalid", await StudentRequests.ErrorCodeAsync(reuse));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-015: giữ đúng từng ký tự, kể cả dấu và khoảng trắng đầu/cuối
    [Fact]
    public async Task Own_password_is_kept_exactly_as_typed()
    {
        const string password = " Mậtkhẩu 1 ";
        var student = await AccountRequests.CreateActivatedAsync(factory, password);
        var client = factory.Capture.CreateClient();

        var exact = await AccountRequests.LoginAsync(client, student.Email, password);
        var trimmed = await AccountRequests.LoginAsync(client, student.Email, password.Trim());

        Assert.Equal(HttpStatusCode.OK, exact.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, trimmed.StatusCode);
    }
}
```

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Accounts"`
Expected: `Passed: 10` (SendCredentialsTests 3, TempPasswordLoginTests 4, ActivationTests 3).

- [ ] **Step 11: Verify toàn bộ**

Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed ngoài các scenario `@api` của Task 6–7 báo `MissingStepDefinition` (thong-tin-dang-nhap BR-007→010, BR-027; toàn bộ dat-lai-mat-khau). Toàn bộ kich-hoat, thong-tin-dang-nhap BR-001→006, 012, 025, 026 và F00/F02 xanh.

- [ ] **Step 12: Commit**
```bash
git add backend/src backend/tests
git commit -F - <<'EOF'
feat(accounts): student activation with own password and account state for coach (BR-f01-p1-011, 013..016, 026)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 6: Sửa email trước khi kích hoạt — đổi cả tài khoản đăng nhập, gửi lại khi hồ sơ đã gửi, lịch sử tài khoản

**Files:**
- Modify: `backend/src/Coaching.Domain/Students/Student.cs`
- Modify: `backend/src/Coaching.Application/Accounts/AccountErrors.cs`, `backend/src/Coaching.Application/Accounts/IAccountCredentials.cs`, `backend/src/Coaching.Application/DependencyInjection.cs`
- Create: `backend/src/Coaching.Application/Students/Commands/ChangeStudentEmail.cs`
- Modify: `backend/src/Coaching.Infrastructure/Identity/AccountCredentials.cs`
- Modify: `backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs`
- Test: `backend/tests/Coaching.Domain.Tests/Students/ChangeEmailTests.cs`
- Test: `backend/tests/Coaching.Acceptance/Support/AccountApi.cs` (M), `backend/tests/Coaching.Acceptance/Steps/EmailChangeSteps.cs` (C)
- Test: `backend/tests/Coaching.Api.IntegrationTests/Accounts/AccountRequests.cs` (M), `Accounts/ChangeEmailTests.cs` (C)

**Interfaces:**
- Consumes: Task 1 — `Student.AccountEvents`, `StudentAccountEvent.FromEmail/ToEmail`, `AccountEventDto(Type, At, FromEmail, ToEmail)` (History mới nhất trước); Task 4 — `CredentialsMailer.SendAsync`, `IAccountCredentials`, `AccountCredentials.FindAsync` (private), `AccountApi` (`CreateAsync`, `SendOkAsync`, `ViewAsync`, `StudentLoginAsync`, `AssertActivationChallengeAsync`, `AssertProblemTitleAsync`, `CredentialMails`, `TempPasswords`, `HistoryBefore`), steps `Given dịch vụ email đang không gửi được thư`, `Then coach được báo thông tin đăng nhập chưa gửi được`, `Then mật khẩu tạm đã gửi trước đó vẫn dùng được`, `Given một học viên có tài khoản Đã kích hoạt`, `Given một (hồ sơ) học viên đã được gửi thông tin đăng nhập(, tài khoản Chờ kích hoạt)`, `When coach gửi thông tin đăng nhập cho học viên đó`, `Given một hồ sơ học viên đủ mục bắt buộc, chưa gửi thông tin đăng nhập lần nào`, F00 `Then đăng nhập bị từ chối`; Task 5 — `AccountApi.TryActivateAsync`; F02 — `StudentVersion.Parse`, `IStudentRepository.GetForUpdateAsync`, `StudentErrors.EmailTaken/VersionRequired/VersionConflict/NotFound`, step `Given email "…" đã thuộc về (… | tài khoản coach | …)` (StudentCreateSteps).
- Produces:
  - Domain: `Student.ChangeEmail(Email newEmail, Guid actorUserId, DateTimeOffset at) → bool` (true = hồ sơ đã từng gửi → phải gửi thông tin mới; cùng email → không đổi gì, trả false, không thêm lịch sử).
  - Application: `AccountErrors.EmailLocked`; `IAccountCredentials.ChangeEmailAsync(Guid userId, Email email, CancellationToken) → Task<Result>`; `ChangeStudentEmailCommand(string? Email, string? Version)`, `ChangeStudentEmailHandler.HandleAsync(Guid id, cmd, ct) → Result<StudentDetailDto>`.
  - API: `PUT /api/admin/students/{id}/email` → 200 `StudentDetailDto` / 400 `email.*` / 404 / 409 `account.email_locked` · `student.email_taken` · `student.version_conflict` / 503.
  - Test: `AccountApi.PreviousEmail`, `AccountApi.TryChangeEmailAsync(string email, string name = StudentName)`; `AccountRequests.ChangeEmailAsync(HttpClient admin, Guid id, string email, string version)`; step `Given một hồ sơ học viên tài khoản Chờ kích hoạt` (dùng lại ở Task 7).

**Owns:** `@BR-f01-p1-tai-khoan-hoc-vien-007`, `@BR-f01-p1-tai-khoan-hoc-vien-008`, `@BR-f01-p1-tai-khoan-hoc-vien-009`, `@BR-f01-p1-tai-khoan-hoc-vien-010`, `@BR-f01-p1-tai-khoan-hoc-vien-027` | —

- [ ] **Step 1: Viết Domain test cho sửa email và lịch sử tài khoản**

`backend/tests/Coaching.Domain.Tests/Students/ChangeEmailTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.TestSupport;

namespace Coaching.Domain.Tests.Students;

public sealed class ChangeEmailTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 1, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset At = new(2026, 11, 2, 9, 0, 0, TimeSpan.FromHours(7));
    private static readonly Guid Coach = Guid.NewGuid();
    private static readonly Email Typo = Email.Create("quan@gmial.com");
    private static readonly Email Fixed = Email.Create("quan@gmail.com");

    private static Student NewStudent() => Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Quân", Typo, CreatedAt,
        RegistrationForm.Create(RegistrationSamples.Complete(), new DateOnly(2026, 11, 1)));

    // @BR-f01-p1-tai-khoan-hoc-vien-010: chưa gửi lần nào → chỉ đổi email (và khóa tìm kiếm), không cần gửi thư
    [Fact]
    public void Change_before_any_send_only_changes_the_email()
    {
        var student = NewStudent();

        var mustSend = student.ChangeEmail(Fixed, Coach, At);

        Assert.False(mustSend);
        Assert.Equal(Fixed, student.Email);
        Assert.Contains("quan@gmail.com", student.SearchKey);
        Assert.Null(student.CredentialsSentAt);
        var row = Assert.Single(student.AccountEvents);
        Assert.Equal(AccountEventType.EmailChanged, row.Type);
        Assert.Equal("quan@gmial.com", row.FromEmail);
        Assert.Equal("quan@gmail.com", row.ToEmail);
        Assert.Equal(Coach, row.ActorUserId);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-009: đã gửi → phải gửi thông tin mới tới email mới; hạn 72 giờ tính lại từ lúc sửa
    [Fact]
    public void Change_after_send_asks_for_new_credentials_with_fresh_expiry()
    {
        var student = NewStudent();
        student.RecordCredentialsSent(Coach, At);
        var changedAt = At.AddHours(1);

        var mustSend = student.ChangeEmail(Fixed, Coach, changedAt);

        Assert.True(mustSend);
        Assert.Equal(changedAt, student.CredentialsSentAt);
        Assert.Equal(changedAt.AddHours(72), student.TempPasswordExpiresAt);
        Assert.Equal(new[] { AccountEventType.CredentialsSent, AccountEventType.EmailChanged },
            student.AccountEvents.Select(e => e.Type).ToArray());
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-007: đã kích hoạt → không sửa được email, không thêm lịch sử
    [Fact]
    public void Change_on_activated_account_is_rejected()
    {
        var student = NewStudent();
        student.RecordCredentialsSent(Coach, At);
        student.Activate(At.AddHours(1));
        var events = student.AccountEvents.Count;

        var ex = Assert.Throws<DomainException>(() => student.ChangeEmail(Fixed, Coach, At.AddHours(2)));

        Assert.Equal("account.email_locked", ex.Code);
        Assert.Equal(Typo, student.Email);
        Assert.Equal(events, student.AccountEvents.Count);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-027: cùng email (sau chuẩn hóa) không phải là một lần sửa → không thêm dòng
    [Fact]
    public void Same_email_changes_nothing()
    {
        var student = NewStudent();
        student.RecordCredentialsSent(Coach, At);

        var mustSend = student.ChangeEmail(Email.Create(" QUAN@gmial.com "), Coach, At.AddHours(1));

        Assert.False(mustSend);
        Assert.Equal(At, student.CredentialsSentAt);
        Assert.Single(student.AccountEvents);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-027: mỗi việc thêm đúng một dòng theo thứ tự; việc bị từ chối không thêm dòng
    [Fact]
    public void Account_history_gains_exactly_one_line_per_action_in_order()
    {
        var student = NewStudent();

        student.RecordCredentialsSent(Coach, At);
        student.RecordCredentialsSent(Coach, At.AddHours(1));
        student.ChangeEmail(Fixed, Coach, At.AddHours(2));
        student.Activate(At.AddHours(3));
        Assert.Throws<DomainException>(() => student.RecordCredentialsSent(Coach, At.AddHours(4)));

        Assert.Equal(
            new[] { AccountEventType.CredentialsSent, AccountEventType.CredentialsResent, AccountEventType.EmailChanged, AccountEventType.Activated },
            student.AccountEvents.Select(e => e.Type).ToArray());
        Assert.Equal(new[] { At, At.AddHours(1), At.AddHours(2), At.AddHours(3) }, student.AccountEvents.Select(e => e.At).ToArray());
    }
}
```

- [ ] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students.ChangeEmailTests"`
Expected: FAIL — build error `'Student' does not contain a definition for 'ChangeEmail'`.

- [ ] **Step 3: Domain — `ChangeEmail` + annotation lịch sử chỉ thêm**

`backend/src/Coaching.Domain/Students/Student.cs`:
- Thêm dòng sau ngay trên khai báo property `AccountEvents` (Task 1):
```csharp
    // @BR-f01-p1-tai-khoan-hoc-vien-027: chỉ thêm — gửi lần đầu, gửi lại, sửa email, gửi link đặt lại, kích hoạt: mỗi việc đúng một dòng; không sửa, không xóa
```
- Thêm method sau `Activate`:
```csharp

    /// <summary>
    /// Coach sửa email trước khi kích hoạt. Trả true khi hồ sơ đã từng được gửi thông tin đăng nhập: use case phải gửi thông tin
    /// mới tới email mới trong cùng transaction. Cùng email (sau chuẩn hóa) → không đổi gì, không thêm lịch sử, trả false.
    /// </summary>
    public bool ChangeEmail(Email newEmail, Guid actorUserId, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(newEmail);
        // @BR-f01-p1-tai-khoan-hoc-vien-007: chỉ sửa được khi Chờ kích hoạt
        if (AccountStatus == AccountStatus.Activated)
            throw new DomainException("account.email_locked", "Tài khoản đã kích hoạt, không sửa được email.");
        if (newEmail == Email)
            return false;

        var wasSent = CredentialsSentAt is not null;
        // @BR-f01-p1-tai-khoan-hoc-vien-027: một dòng "sửa email từ … sang …"
        _accountEvents.Add(new StudentAccountEvent(AccountEventType.EmailChanged, at, actorUserId, Email.Value, newEmail.Value));
        Email = newEmail;
        RefreshSearchKey();
        if (wasSent)
        {
            // @BR-f01-p1-tai-khoan-hoc-vien-009: thông tin đăng nhập mới đi cùng lần sửa → hạn 72 giờ tính lại từ lúc sửa
            CredentialsSentAt = at;
            TempPasswordExpiresAt = at + TempPasswordLifetime;
        }

        return wasSent; // @BR-f01-p1-tai-khoan-hoc-vien-010: chưa gửi lần nào → false, không gửi thư
    }
```

- [ ] **Step 4: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students.ChangeEmailTests"`
Expected: `Passed: 5`.

- [ ] **Step 5: Acceptance — `AccountApi.TryChangeEmailAsync` + step defs BR-007→010, BR-027**

`backend/tests/Coaching.Acceptance/Support/AccountApi.cs` — thêm vào class (sau `TryActivateAsync`):
```csharp

    /// <summary>Email của hồ sơ ngay trước lần sửa gần nhất.</summary>
    public string? PreviousEmail { get; private set; }

    /// <summary>PUT email kèm version vừa đọc; ghi lại email cũ, số dòng lịch sử và mật khẩu tạm mới (nếu có thư mới).</summary>
    public async Task<HttpResponseMessage> TryChangeEmailAsync(string email, string name = StudentName)
    {
        var current = await ViewAsync(name);
        PreviousEmail = current.Email;
        HistoryBefore = current.Account.History.Count;
        var credentialMailsBefore = Outbox.All.Count(m => m.Subject == CredentialsSubject);
        var response = await ctx.SendJsonAsync(HttpMethod.Put, $"/api/admin/students/{api.Ids[name]}/email",
            new { email, version = current.Version }, api.AdminToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var detail = (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!;
            Emails[name] = detail.Email;
            if (Outbox.All.Count(m => m.Subject == CredentialsSubject) > credentialMailsBefore)
                TempPasswords.Add(TempPassword(CredentialMails(detail.Email)[^1]));
        }

        return response;
    }
```

`backend/tests/Coaching.Acceptance/Steps/EmailChangeSteps.cs`:
```csharp
using System.Net;
using Coaching.Acceptance.Support;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class EmailChangeSteps(AcceptanceContext ctx, StudentApi api, AccountApi account)
{
    private static readonly IReadOnlyDictionary<string, string> HistoryTypes = new Dictionary<string, string>
    {
        ["gửi lần đầu"] = "CredentialsSent",
        ["gửi lại kèm ghi chú mật khẩu tạm cũ bị hủy"] = "CredentialsResent",
        ["học viên đặt mật khẩu riêng"] = "Activated",
    };

    // @BR-f01-p1-tai-khoan-hoc-vien-007 @BR-f01-p1-tai-khoan-hoc-vien-010
    [Given(@"^một hồ sơ học viên với email ""([^""]*)"", (?:tài khoản Chờ kích hoạt|chưa gửi thông tin đăng nhập lần nào)$")]
    public Task PendingWithEmail(string email) => account.CreateAsync(email: email);

    // @BR-f01-p1-tai-khoan-hoc-vien-007 @BR-f01-p1-tai-khoan-hoc-vien-008 @BR-f01-p1-tai-khoan-hoc-vien-009 @BR-f01-p1-tai-khoan-hoc-vien-027
    [When(@"^coach sửa email của học viên đó thành ""([^""]*)""$")]
    public Task ChangeEmail(string email) => account.TryChangeEmailAsync(email);

    // @BR-f01-p1-tai-khoan-hoc-vien-009 @BR-f01-p1-tai-khoan-hoc-vien-010
    [Given(@"^coach đã sửa email của (?:học viên|hồ sơ) đó thành ""([^""]*)""$")]
    public async Task ChangedEmail(string email) =>
        Assert.Equal(HttpStatusCode.OK, (await account.TryChangeEmailAsync(email)).StatusCode);

    // @BR-f01-p1-tai-khoan-hoc-vien-007 @BR-f01-p1-tai-khoan-hoc-vien-009 @BR-f01-p1-tai-khoan-hoc-vien-010
    [Then(@"^hồ sơ đó (?:vẫn )?có email ""([^""]*)""$")]
    public async Task HasEmail(string email) => Assert.Equal(email, (await account.ViewAsync()).Email);

    // @BR-f01-p1-tai-khoan-hoc-vien-007
    [When("coach sửa email của học viên đó sang một email chưa ai dùng")]
    public Task ChangeToUnusedEmail() => account.TryChangeEmailAsync(StudentApi.NewEmail());

    [Then("email của học viên đó không đổi vì tài khoản đã kích hoạt")]
    public async Task LockedEmailUnchanged()
    {
        await api.AssertErrorAsync(HttpStatusCode.Conflict, "account.email_locked");
        await account.AssertProblemTitleAsync("Tài khoản đã kích hoạt, không sửa được email.");
        Assert.Equal(account.PreviousEmail, (await account.ViewAsync()).Email);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-008 — ví dụ "tài khoản coach" dùng step có sẵn của StudentCreateSteps
    [Given(@"^email ""([^""]*)"" đã thuộc về một hồ sơ khác$")]
    public Task EmailOwnedByAnotherProfile(string email) => account.CreateAsync("Chủ email", email);

    [Then("email của học viên đó không đổi vì email đã được dùng")]
    public async Task TakenEmailUnchanged()
    {
        await api.AssertErrorAsync(HttpStatusCode.Conflict, "student.email_taken");
        Assert.Equal(account.PreviousEmail, (await account.ViewAsync()).Email);
    }

    [Then("mật khẩu tạm đã gửi cho học viên đó vẫn dùng được")]
    public async Task SentTempStillWorks()
    {
        await account.StudentLoginAsync(account.TempPasswords[^1]);
        await account.AssertActivationChallengeAsync();
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-008 (dùng lại ở BR-017)
    [Given("một hồ sơ học viên tài khoản Chờ kích hoạt")]
    public Task PendingProfile() => account.CreateAsync();

    [Then("email của học viên đó không đổi vì email không hợp lệ")]
    public async Task InvalidEmailUnchanged()
    {
        await api.AssertErrorAsync(HttpStatusCode.BadRequest, "email.invalid");
        Assert.Equal(account.PreviousEmail, (await account.ViewAsync()).Email);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-009 @BR-f01-p1-tai-khoan-hoc-vien-027
    [Given(@"^một học viên với email ""([^""]*)"" đã được gửi thông tin đăng nhập$")]
    public async Task SentWithEmail(string email)
    {
        await account.CreateAsync(email: email);
        await account.SendOkAsync();
        ctx.Clock.Advance(TimeSpan.FromMinutes(1)); // lần sửa sau có thời điểm khác → thứ tự "mới nhất trước" xác định
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-009 @BR-f01-p1-tai-khoan-hoc-vien-010
    [Then(@"^email ""([^""]*)"" nhận được email thông tin đăng nhập(?: mới)?$")]
    public void CredentialsMailTo(string email)
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        Assert.Single(account.CredentialMails(email));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-009
    [When(@"^học viên đăng nhập web học viên bằng email ""([^""]*)"" và mật khẩu tạm cũ$")]
    public Task LoginWithOldTemp(string email) => account.StudentLoginAsync(account.TempPasswords[^2], email: email);

    // @BR-f01-p1-tai-khoan-hoc-vien-010
    [Then("không email nào được gửi cho học viên đó")]
    public void NoMailSent() =>
        Assert.DoesNotContain(account.Outbox.All, m => m.To == account.PreviousEmail || m.To == account.Email());

    // @BR-f01-p1-tai-khoan-hoc-vien-027
    [Given("coach đã gửi thông tin đăng nhập lần đầu, rồi gửi lại cho một học viên")]
    public async Task SentThenResent()
    {
        await account.CreateAsync();
        await account.SendOkAsync();
        ctx.Clock.Advance(TimeSpan.FromMinutes(1));
        await account.SendOkAsync();
    }

    [When("học viên đó kích hoạt tài khoản bằng mật khẩu tạm mới nhất")]
    public async Task ActivateWithLatestTemp()
    {
        ctx.Clock.Advance(TimeSpan.FromMinutes(1));
        await account.StudentLoginAsync(account.TempPasswords[^1]);
        await account.AssertActivationChallengeAsync();
        Assert.Equal(HttpStatusCode.OK, (await account.TryActivateAsync("Coach2026", "Coach2026")).StatusCode);
    }

    [Then(@"^lịch sử tài khoản của học viên đó có ""(\d+)"" dòng theo thứ tự: (.+)$")]
    public async Task HistoryInOrder(int count, string order)
    {
        var history = (await account.ViewAsync()).Account.History; // mới nhất trước
        var expected = order.Split(", ").Select(label => HistoryTypes[label]).ToList();
        Assert.Equal(count, expected.Count);
        Assert.Equal(expected, history.AsEnumerable().Reverse().Select(e => e.Type).ToList());
    }

    [Then(@"^lịch sử tài khoản của học viên đó có thêm dòng sửa email từ ""([^""]*)"" sang ""([^""]*)""$")]
    public async Task EmailChangeRow(string from, string to)
    {
        var history = (await account.ViewAsync()).Account.History;
        Assert.Equal(account.HistoryBefore + 1, history.Count);
        Assert.Equal("EmailChanged", history[0].Type);
        Assert.Equal(from, history[0].FromEmail);
        Assert.Equal(to, history[0].ToEmail);
    }
}
```

- [ ] **Step 6: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f01-p1-tai-khoan-hoc-vien-007|Category=BR-f01-p1-tai-khoan-hoc-vien-008|Category=BR-f01-p1-tai-khoan-hoc-vien-009|Category=BR-f01-p1-tai-khoan-hoc-vien-010|Category=BR-f01-p1-tai-khoan-hoc-vien-027)&Category!=web"`
Expected: FAIL — các scenario sửa email nhận `404 NotFound` thay vì `200/409/400` ở `PUT /api/admin/students/{id}/email` (route chưa có); riêng BR-027 sc.1 (chỉ gửi + kích hoạt) đã PASS.

- [ ] **Step 7: Application + Infrastructure + API — đổi email ở cả Identity và hồ sơ trong một transaction**

`backend/src/Coaching.Application/Accounts/AccountErrors.cs` — thêm member:
```csharp

    // @BR-f01-p1-tai-khoan-hoc-vien-007
    public static readonly Error EmailLocked = new(
        "account.email_locked", "Tài khoản đã kích hoạt, không sửa được email.", ErrorKind.Conflict);
```

`backend/src/Coaching.Application/Accounts/IAccountCredentials.cs` — thêm `using Coaching.Application.Common;` và `using Coaching.Domain.Common;` vào đầu file, rồi thêm sau `SetOwnPasswordAsync`:
```csharp

    /// <summary>
    /// Đổi email đăng nhập (Email + UserName) của tài khoản. Trùng với bất kỳ tài khoản nào (kể cả coach, không phân biệt
    /// hoa/thường) → StudentErrors.EmailTaken.
    /// </summary>
    Task<Result> ChangeEmailAsync(Guid userId, Email email, CancellationToken ct = default);
```

`backend/src/Coaching.Application/Students/Commands/ChangeStudentEmail.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Accounts;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

/// <summary>@BR-f01-p1-tai-khoan-hoc-vien-007: email đi route riêng (UpdateStudentCommand vẫn không có email).</summary>
public sealed record ChangeStudentEmailCommand(string? Email, string? Version);

public sealed class ChangeStudentEmailHandler(
    CurrentCoach currentCoach,
    IStudentRepository students,
    IAccountCredentials credentials,
    CredentialsMailer mailer,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(Guid id, ChangeStudentEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;
        if (StudentVersion.Parse(command.Version) is not { } version)
            return StudentErrors.VersionRequired;
        // @BR-f01-p1-tai-khoan-hoc-vien-008: trim + lowercase; sai định dạng → 400 email.invalid
        var email = Email.Create(command.Email);

        // @BR-f01-p1-tai-khoan-hoc-vien-009: Identity + hồ sơ + mật khẩu tạm mới + thư trong một transaction — lỗi ở đâu cũng giữ email cũ
        return await unitOfWork.ExecuteInTransactionAsync<StudentDetailDto>(async ct =>
        {
            if (await students.GetForUpdateAsync(id, coach.Id, version, ct) is not { } student)
                return Result<StudentDetailDto>.Failure(StudentErrors.NotFound);
            if (student.AccountStatus == AccountStatus.Activated)
                return Result<StudentDetailDto>.Failure(AccountErrors.EmailLocked); // @BR-f01-p1-tai-khoan-hoc-vien-007 → 409

            var now = clock.UtcNow;
            if (student.Email == email)
                return Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach, now)); // không đổi gì

            // @BR-f01-p1-tai-khoan-hoc-vien-008: kiểm trùng ở Identity (gồm tài khoản coach) TRƯỚC khi đổi hồ sơ
            var account = await credentials.ChangeEmailAsync(student.UserId, email, ct);
            if (!account.IsSuccess)
                return Result<StudentDetailDto>.Failure(account.Error!);

            var mustSend = student.ChangeEmail(email, coach.UserId, now);
            try
            {
                await unitOfWork.SaveChangesAsync(ct); // trước mọi lệnh UserManager tiếp theo → xung đột phiên bản thành 409
            }
            catch (ConcurrencyConflictException)
            {
                return Result<StudentDetailDto>.Failure(StudentErrors.VersionConflict);
            }

            // @BR-f01-p1-tai-khoan-hoc-vien-009: đã gửi → mật khẩu tạm mới (mật khẩu cũ + mã kích hoạt cũ bị hủy) tới email mới
            // @BR-f01-p1-tai-khoan-hoc-vien-010: chưa gửi lần nào → không gửi thư
            if (mustSend && await mailer.SendAsync(student, ct) is { IsSuccess: false } sent)
                return Result<StudentDetailDto>.Failure(sent.Error!);

            return Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach, now));
        }, cancellationToken);
    }
}
```

`backend/src/Coaching.Application/DependencyInjection.cs` — thêm trước `return services;`:
```csharp
        services.AddScoped<ChangeStudentEmailHandler>();
```

`backend/src/Coaching.Infrastructure/Identity/AccountCredentials.cs`:
- Thêm vào đầu file:
```csharp
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
```
- Thêm method sau `SetOwnPasswordAsync`:
```csharp

    // @BR-f01-p1-tai-khoan-hoc-vien-008: UserValidator so NormalizedEmail / NormalizedUserName → khác hoa/thường vẫn trùng; gồm cả coach
    public async Task<Result> ChangeEmailAsync(Guid userId, Email email, CancellationToken ct = default)
    {
        var user = await FindAsync(userId);
        user.Email = email.Value;
        user.UserName = email.Value; // tên đăng nhập = email (như StudentAccounts.CreatePendingAsync)

        IdentityResult updated;
        try
        {
            updated = await users.UpdateAsync(user);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Result.Failure(StudentErrors.EmailTaken); // hai request đua nhau qua bước kiểm trùng → chỉ mục duy nhất chặn
        }

        if (updated.Succeeded)
            return Result.Success();
        if (updated.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
            return Result.Failure(StudentErrors.EmailTaken);
        throw new InvalidOperationException(
            $"Không đổi được email đăng nhập: {string.Join("; ", updated.Errors.Select(e => e.Description))}");
    }
```

`backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs` — thêm action cuối class:
```csharp

    // @BR-f01-p1-tai-khoan-hoc-vien-007: sửa email chỉ khi Chờ kích hoạt; route riêng, bắt buộc version như mọi lệnh sửa
    [HttpPut("{id:guid}/email")]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeEmail(Guid id, [FromBody] ChangeStudentEmailCommand command,
        [FromServices] ChangeStudentEmailHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, command, cancellationToken)).ToActionResult(this);
```

- [ ] **Step 8: Chạy acceptance — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f01-p1-tai-khoan-hoc-vien-007|Category=BR-f01-p1-tai-khoan-hoc-vien-008|Category=BR-f01-p1-tai-khoan-hoc-vien-009|Category=BR-f01-p1-tai-khoan-hoc-vien-010|Category=BR-f01-p1-tai-khoan-hoc-vien-027)&Category!=web"`
Expected: `Passed: 12`, `Failed: 0` (BR-007: 1; BR-008: 2 ví dụ + 1; BR-009: 1 + 2 ví dụ + 1; BR-010: 2; BR-027: 2).

- [ ] **Step 9: Integration test sửa email**

`backend/tests/Coaching.Api.IntegrationTests/Accounts/AccountRequests.cs` — thêm cuối class (trước `[GeneratedRegex…]`):
```csharp
    public static Task<HttpResponseMessage> ChangeEmailAsync(HttpClient admin, Guid id, string email, string version) =>
        admin.PutAsJsonAsync($"/api/admin/students/{id}/email", new { email, version });

```

`backend/tests/Coaching.Api.IntegrationTests/Accounts/ChangeEmailTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Students;
using Coaching.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Accounts;

[Collection(ApiCollection.Name)]
public sealed class ChangeEmailTests(ApiFactory factory)
{
    private static async Task<StudentDetailDto> GetAsync(HttpClient admin, Guid id) =>
        (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{id}"))!;

    // @BR-f01-p1-tai-khoan-hoc-vien-008 @BR-f01-p1-tai-khoan-hoc-vien-009: email chuẩn hóa; đăng nhập chuyển sang email mới
    // với mật khẩu tạm mới — email cũ và mật khẩu tạm cũ đều hết dùng được; Identity đổi cả Email lẫn UserName
    [Fact]
    public async Task Changing_email_of_sent_profile_moves_login_to_the_new_email()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);
        var oldTemp = await AccountRequests.SendCredentialsOkAsync(factory, admin, student);
        var newEmail = $"s{Guid.NewGuid():N}@test.local";

        var response = await AccountRequests.ChangeEmailAsync(admin, student.Id, $" {newEmail.ToUpperInvariant()} ",
            (await GetAsync(admin, student.Id)).Version);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(newEmail, (await response.Content.ReadFromJsonAsync<StudentDetailDto>())!.Email);
        var newTemp = AccountRequests.TempPassword(factory.Outbox.All.Last(m => m.To == newEmail));
        Assert.NotEqual(oldTemp, newTemp);
        var client = factory.Capture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await AccountRequests.LoginAsync(client, student.Email, oldTemp)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await AccountRequests.LoginAsync(client, newEmail, oldTemp)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AccountRequests.LoginAsync(client, newEmail, newTemp)).StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        Assert.Null(await users.FindByEmailAsync(student.Email));
        Assert.Equal(newEmail, (await users.FindByEmailAsync(newEmail))!.UserName);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-009: gửi tới email mới lỗi → hồ sơ VÀ tài khoản đăng nhập giữ email cũ, mật khẩu tạm cũ còn dùng được
    [Fact]
    public async Task Email_failure_keeps_the_old_email_in_profile_and_login()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);
        var oldTemp = await AccountRequests.SendCredentialsOkAsync(factory, admin, student);
        var newEmail = $"s{Guid.NewGuid():N}@test.local";
        factory.Outbox.FailNext = true;

        var response = await AccountRequests.ChangeEmailAsync(admin, student.Id, newEmail, (await GetAsync(admin, student.Id)).Version);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("account.email_send_failed", await StudentRequests.ErrorCodeAsync(response));
        var detail = await GetAsync(admin, student.Id);
        Assert.Equal(student.Email, detail.Email);
        Assert.Single(detail.Account.History);
        var login = await AccountRequests.LoginAsync(factory.Capture.CreateClient(), student.Email, oldTemp);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        Assert.Null(await users.FindByEmailAsync(newEmail));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-027: gửi lại đúng email hiện tại không phải lần sửa → không thêm dòng, không gửi thư
    [Fact]
    public async Task Same_email_is_a_no_op()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);
        await AccountRequests.SendCredentialsOkAsync(factory, admin, student);
        var mailsBefore = factory.Outbox.All.Count(m => m.To == student.Email);

        var response = await AccountRequests.ChangeEmailAsync(admin, student.Id, student.Email.ToUpperInvariant(),
            (await GetAsync(admin, student.Id)).Version);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single((await GetAsync(admin, student.Id)).Account.History);
        Assert.Equal(mailsBefore, factory.Outbox.All.Count(m => m.To == student.Email));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-008: email của tài khoản coach → 409, hồ sơ giữ email cũ
    [Fact]
    public async Task Email_of_the_coach_account_is_taken()
    {
        var admin = await factory.CaptureAdminClientAsync();
        var student = await AccountRequests.CreateCompleteAsync(admin);

        var response = await AccountRequests.ChangeEmailAsync(admin, student.Id, ApiFactory.AdminEmail,
            (await GetAsync(admin, student.Id)).Version);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("student.email_taken", await StudentRequests.ErrorCodeAsync(response));
        Assert.Equal(student.Email, (await GetAsync(admin, student.Id)).Email);
    }
}
```

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Accounts"`
Expected: `Passed: 14` (SendCredentialsTests 3, TempPasswordLoginTests 4, ActivationTests 3, ChangeEmailTests 4).

- [ ] **Step 10: Verify toàn bộ**

Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed ngoài các scenario `@api` của Task 7 báo `MissingStepDefinition` (toàn bộ dat-lai-mat-khau.feature). thong-tin-dang-nhap.feature và kich-hoat.feature xanh hết; F00/F02 xanh (ho-so-hoc-vien BR-f02-ho-so-hoc-vien-012 vẫn không sửa email qua `PUT` hồ sơ).

- [ ] **Step 11: Commit**
```bash
git add backend/src backend/tests
git commit -F - <<'EOF'
feat(accounts): change student email before activation with resend and account history (BR-f01-p1-007..010, 027)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 7: Đặt lại mật khẩu — coach gửi link, Quên mật khẩu, đặt lại bằng link, kết thúc mọi phiên

**Files:**
- Modify: `backend/src/Coaching.Domain/Students/Student.cs`
- Modify: `backend/src/Coaching.Application/Accounts/AccountErrors.cs`, `AccountTokenLifetimes.cs`, `IAccountCredentials.cs`; `backend/src/Coaching.Application/Auth/AuthErrors.cs`; `backend/src/Coaching.Application/DependencyInjection.cs`
- Create: `backend/src/Coaching.Application/Accounts/ISessionRevoker.cs`, `Accounts/Commands/RequestPasswordReset.cs`, `Accounts/Commands/ResetPassword.cs`, `backend/src/Coaching.Application/Students/Commands/SendPasswordResetLink.cs`
- Create: `backend/src/Coaching.Infrastructure/Identity/SessionRevoker.cs`
- Modify: `backend/src/Coaching.Infrastructure/Identity/RefreshTokenRevokedReason.cs`, `AuthService.cs`, `AccountCredentials.cs`; `backend/src/Coaching.Infrastructure/DependencyInjection.cs`
- Modify: `backend/src/Coaching.Api/Controllers/AuthController.cs`, `backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs`
- Test: `backend/tests/Coaching.Domain.Tests/Students/ResetLinkTests.cs`
- Test: `backend/tests/Coaching.Acceptance/Support/AccountApi.cs` (M), `backend/tests/Coaching.Acceptance/Steps/PasswordResetSteps.cs` (C)
- Test: `backend/tests/Coaching.Api.IntegrationTests/Accounts/AccountRequests.cs` (M), `Accounts/PasswordResetTests.cs` (C)

**Interfaces:**
- Consumes: Task 2 — `IAccountEmails.PasswordReset(toEmail, fullName, rawToken)` (tiêu đề `Đặt lại mật khẩu Duobexiuuu Coaching`, link `<ClientBaseUrl>/dat-lai-mat-khau#t=<token>`), `IEmailQueue.EnqueueAsync` (+ `EmailQueueWorker` gửi nền); Task 3 — `IAccountTokens.IssueAsync/FindValidAsync/ConsumeAsync/RevokeAllAsync/LastIssuedAtAsync`, `AccountTokenSource.Coach/Self`; Task 4 — `AccountMail.SendAsync`, `AccountTokenLifetimes`, `AccountErrors`, `IAccountCredentials`, `AccountApi`, `AccountRequests`, `ApiFactory.Capture/Outbox/CaptureAdminClientAsync`; Task 5 — `PasswordPolicy`, `AuthErrors.ForPassword`, `IAccountCredentials.SetOwnPasswordAsync`, `AccountApi.CreateActivatedAsync/AssertStudentSessionAsync/OwnPassword`, `AccountRequests.CreateActivatedAsync`, step `When học viên đăng nhập web học viên bằng mật khẩu "…"`; Task 6 — step `Given một hồ sơ học viên tài khoản Chờ kích hoạt`; F00 — `Given coach "…" đã đăng nhập`, `Then đăng nhập bị từ chối`, `AuthService.RevokeAllAsync` (private, được tách ra ở đây).
- Produces:
  - Domain: `Student.RecordResetLinkSent(Guid actorUserId, DateTimeOffset at)`.
  - Application: `AccountErrors.NotActivated`; `AuthErrors.ResetLinkInvalid`; `AccountTokenLifetimes.PasswordReset` (30 phút), `AccountTokenLifetimes.SelfResetInterval` (60 giây); `IAccountCredentials.FindUserIdByEmailAsync(string email, CancellationToken) → Task<Guid?>`; `ISessionRevoker.RevokeAllAsync(Guid userId, CancellationToken)`; `SendPasswordResetLinkHandler.HandleAsync(Guid id, ct) → Result<StudentDetailDto>`; `ForgotPasswordCommand(string? Email)`, `ForgotPasswordResponse(string Message)` + `GenericMessage` + `Generic`; `RequestPasswordResetHandler.HandleAsync(cmd, ct) → Task<ForgotPasswordResponse>`; `ResetPasswordCommand(string? Token, string? Password, string? ConfirmPassword)`, `ResetPasswordHandler.HandleAsync(cmd, ct) → Result`.
  - Infrastructure: `RefreshTokenRevokedReason.PasswordReset`; `SessionRevoker : ISessionRevoker` (+ `RevokeAllAsync(Guid, RefreshTokenRevokedReason, CancellationToken)` cho F00).
  - API: `POST /api/admin/students/{id}/password-reset-link` → 200 / 404 / 409 `account.not_activated` / 503; `POST /api/auth/forgot-password` → luôn 202 `{ message }`; `POST /api/auth/reset-password` → 204 / 400 `auth.reset_link_invalid` · `auth.password_*` (rate limit `auth`).
  - Test: `AccountApi.ResetSubject/QueueSettle/ResetTokens/ResetMails()/TrySendResetLinkAsync()/ForgotAsync()/ForgotOkAsync()/TryResetAsync()/WaitForMailsAsync()/AssertForgotGenericAsync()/CreateCoachAccountAsync()/ResetToken()`; `AccountRequests.ResetSubject/ForgotAsync/ResetAsync/SendResetLinkAsync/ResetToken/WaitForMailsAsync`.

**Owns:** `@BR-f01-p1-tai-khoan-hoc-vien-017`, `@BR-f01-p1-tai-khoan-hoc-vien-018`, `@BR-f01-p1-tai-khoan-hoc-vien-019`, `@BR-f01-p1-tai-khoan-hoc-vien-020`, `@BR-f01-p1-tai-khoan-hoc-vien-021`, `@BR-f01-p1-tai-khoan-hoc-vien-022`, `@BR-f01-p1-tai-khoan-hoc-vien-023`, `@BR-f01-p1-tai-khoan-hoc-vien-024` | `@ADR-f01-p1-tai-khoan-hoc-vien-005`, `@ADR-f01-p1-tai-khoan-hoc-vien-009`

- [ ] **Step 1: Viết Domain test cho ghi nhận link đặt lại**

`backend/tests/Coaching.Domain.Tests/Students/ResetLinkTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.TestSupport;

namespace Coaching.Domain.Tests.Students;

public sealed class ResetLinkTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 1, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset At = new(2026, 11, 3, 9, 0, 0, TimeSpan.FromHours(7));
    private static readonly Guid Coach = Guid.NewGuid();

    private static Student NewStudent() => Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Quân", Email.Create("quan@gmail.com"),
        CreatedAt, RegistrationForm.Create(RegistrationSamples.Complete(), new DateOnly(2026, 11, 1)));

    // @BR-f01-p1-tai-khoan-hoc-vien-017: Chờ kích hoạt → từ chối (dùng gửi thông tin đăng nhập), không thêm lịch sử
    [Fact]
    public void Reset_link_for_pending_account_is_rejected()
    {
        var student = NewStudent();
        student.RecordCredentialsSent(Coach, At);

        var ex = Assert.Throws<DomainException>(() => student.RecordResetLinkSent(Coach, At.AddHours(1)));

        Assert.Equal("account.not_activated", ex.Code);
        Assert.Equal("Tài khoản chưa kích hoạt. Hãy gửi thông tin đăng nhập.", ex.Message);
        Assert.Single(student.AccountEvents);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-017: Đã kích hoạt → gửi được ở mọi trạng thái coaching
    [Theory]
    [InlineData(CoachingStatus.Active)]
    [InlineData(CoachingStatus.Paused)]
    [InlineData(CoachingStatus.Ended)]
    public void Reset_link_is_recorded_in_every_coaching_status(CoachingStatus status)
    {
        var student = NewStudent();
        student.RecordCredentialsSent(Coach, At);
        student.Activate(At.AddHours(1));
        if (status != CoachingStatus.Active)
            student.ChangeCoachingStatus(status, "Kịch bản", Coach, At.AddHours(2));

        student.RecordResetLinkSent(Coach, At.AddHours(3));

        var row = student.AccountEvents[^1];
        Assert.Equal(AccountEventType.ResetLinkSent, row.Type);
        Assert.Equal(At.AddHours(3), row.At);
        Assert.Equal(Coach, row.ActorUserId);
    }
}
```

- [ ] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students.ResetLinkTests"`
Expected: FAIL — build error `'Student' does not contain a definition for 'RecordResetLinkSent'`.

- [ ] **Step 3: Domain — `RecordResetLinkSent`**

`backend/src/Coaching.Domain/Students/Student.cs` — thêm method sau `ChangeEmail`:
```csharp

    /// <summary>Coach gửi link đặt lại mật khẩu. Link (token) do IAccountTokens tạo trong cùng transaction.</summary>
    public void RecordResetLinkSent(Guid actorUserId, DateTimeOffset at)
    {
        // @BR-f01-p1-tai-khoan-hoc-vien-017: chỉ tài khoản Đã kích hoạt, ở mọi trạng thái coaching
        if (AccountStatus != AccountStatus.Activated)
            throw new DomainException("account.not_activated", "Tài khoản chưa kích hoạt. Hãy gửi thông tin đăng nhập.");
        _accountEvents.Add(new StudentAccountEvent(AccountEventType.ResetLinkSent, at, actorUserId));
    }
```

- [ ] **Step 4: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Students.ResetLinkTests"`
Expected: `Passed: 4`.

- [ ] **Step 5: Acceptance — `AccountApi` đặt lại mật khẩu + step defs dat-lai-mat-khau**

`backend/tests/Coaching.Acceptance/Support/AccountApi.cs`:
- Thêm vào đầu file:
```csharp
using Coaching.Application.Accounts.Commands;
using Coaching.Application.Common;
using Coaching.Domain.Coaches;
```
- Thêm vào class (sau `TryChangeEmailAsync`):
```csharp

    public const string ResetSubject = "Đặt lại mật khẩu Duobexiuuu Coaching";

    /// <summary>@ADR-f01-p1-tai-khoan-hoc-vien-005: thư Quên mật khẩu gửi nền — chờ hàng đợi chạy hết trước khi khẳng định "không có thư".</summary>
    public static readonly TimeSpan QueueSettle = TimeSpan.FromMilliseconds(500);

    /// <summary>Token trong các link đặt lại học viên đã nhận, theo thứ tự.</summary>
    public List<string> ResetTokens { get; } = [];

    public IReadOnlyList<CapturedEmail> ResetMails(string? to = null) => Mails(to ?? Email(), ResetSubject);

    public async Task<HttpResponseMessage> TrySendResetLinkAsync(string name = StudentName)
    {
        await api.LoginCoachAsync();
        MailCountBefore = ResetMails(Emails[name]).Count;
        var response = await ctx.SendJsonAsync(HttpMethod.Post, $"/api/admin/students/{api.Ids[name]}/password-reset-link",
            new { }, api.AdminToken);
        if (response.StatusCode == HttpStatusCode.OK)
            ResetTokens.Add(ResetToken(ResetMails(Emails[name])[^1]));
        return response;
    }

    public Task<HttpResponseMessage> ForgotAsync(string email) => ctx.PostJsonAsync("/api/auth/forgot-password", new { email });

    /// <summary>"học viên đó" tự xin link qua Quên mật khẩu và nhận được thư; nhớ token của link.</summary>
    public async Task ForgotOkAsync()
    {
        var before = ResetMails().Count;
        Assert.Equal(HttpStatusCode.Accepted, (await ForgotAsync(Email())).StatusCode);
        var mails = await WaitForMailsAsync(Email(), ResetSubject, before + 1);
        ResetTokens.Add(ResetToken(mails[^1]));
    }

    public async Task<HttpResponseMessage> TryResetAsync(string token, string password, string confirmPassword)
    {
        var response = await ctx.PostJsonAsync("/api/auth/reset-password", new { token, password, confirmPassword });
        if (response.StatusCode == HttpStatusCode.NoContent)
            OwnPassword = password;
        return response;
    }

    /// <summary>Chờ (đồng hồ thật, tối đa 5 giây) tới khi có ít nhất <paramref name="count"/> thư cùng tiêu đề tới <paramref name="to"/>.</summary>
    public async Task<IReadOnlyList<CapturedEmail>> WaitForMailsAsync(string to, string subject, int count)
    {
        var watch = Stopwatch.StartNew();
        while (true)
        {
            var mails = Mails(to, subject);
            if (mails.Count >= count)
                return mails;
            if (watch.Elapsed > TimeSpan.FromSeconds(5))
                throw new TimeoutException($"Sau 5 giây mới có {mails.Count}/{count} thư '{subject}' tới {to}.");
            await Task.Delay(50);
        }
    }

    /// <summary>@BR-f01-p1-tai-khoan-hoc-vien-023: 202 và body chỉ có đúng một câu chung.</summary>
    public async Task AssertForgotGenericAsync()
    {
        Assert.Equal(HttpStatusCode.Accepted, ctx.LastResponse!.StatusCode);
        using var json = JsonDocument.Parse(await ctx.LastResponse.Content.ReadAsStringAsync());
        var property = Assert.Single(json.RootElement.EnumerateObject());
        Assert.Equal("message", property.Name);
        Assert.Equal(ForgotPasswordResponse.GenericMessage, property.Value.GetString());
    }

    /// <summary>Tài khoản coach (role Admin + hồ sơ Coach) với <paramref name="email"/>.</summary>
    public async Task CreateCoachAccountAsync(string email)
    {
        await using var scope = ctx.Host.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var users = sp.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser { UserName = email, Email = email, FullName = "Coach khác", EmailConfirmed = true };
        Assert.True((await users.CreateAsync(user, "Coach@12345")).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, Roles.Admin)).Succeeded);
        var db = sp.GetRequiredService<AppDbContext>();
        db.Coaches.Add(Coach.Create(user.Id, "Coach khác", global::Coaching.Domain.Common.Email.Create(email)));
        await db.SaveChangesAsync();
    }

    /// <summary>Token sau "#t=" trong link đặt lại (dạng link theo ADR-f01-p1-tai-khoan-hoc-vien-010 của Task 8).</summary>
    public static string ResetToken(CapturedEmail mail) =>
        ResetTokenPattern().Match(mail.TextBody) is { Success: true } match
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"Thư '{mail.Subject}' tới {mail.To} không có link đặt lại.");

    [GeneratedRegex(@"/dat-lai-mat-khau#t=([A-Za-z0-9_-]+)")]
    private static partial Regex ResetTokenPattern();
```

`backend/tests/Coaching.Acceptance/Steps/PasswordResetSteps.cs`:
```csharp
using System.Net;
using Coaching.Acceptance.Support;
using Coaching.Application.Auth;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class PasswordResetSteps(AcceptanceContext ctx, StudentApi api, AccountApi account)
{
    private AuthTokens? _phone;

    // @BR-f01-p1-tai-khoan-hoc-vien-017
    [Given(@"^một học viên có tài khoản Đã kích hoạt, coaching ""([^""]*)""$")]
    public async Task ActivatedWithCoachingStatus(string label)
    {
        await account.CreateActivatedAsync();
        await api.LoginCoachAsync();
        var status = StudentApi.CoachingStatuses[label];
        if (status != "Active")
        {
            var changed = await api.TryChangeStatusAsync(AccountApi.StudentName, status, "Chuẩn bị kịch bản");
            Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        }
    }

    [When("coach gửi link đặt lại mật khẩu cho học viên đó")]
    public Task SendResetLink() => account.TrySendResetLinkAsync();

    [Then("học viên đó nhận được email có link đặt lại mật khẩu")]
    public void ReceivedResetLink()
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        var mails = account.ResetMails();
        Assert.Equal(account.MailCountBefore + 1, mails.Count);
        Assert.Contains($"{AcceptanceContext.ClientBaseUrl}/dat-lai-mat-khau#t=", mails[^1].TextBody);
    }

    [Then("link đặt lại không được gửi vì tài khoản chưa kích hoạt")]
    public async Task RejectedNotActivated()
    {
        await api.AssertErrorAsync(HttpStatusCode.Conflict, "account.not_activated");
        Assert.Equal(account.MailCountBefore, account.ResetMails().Count);
    }

    [Then("coach được hướng dẫn gửi thông tin đăng nhập thay thế")]
    public Task GuidedToCredentials() =>
        account.AssertProblemTitleAsync("Tài khoản chưa kích hoạt. Hãy gửi thông tin đăng nhập.");

    // @BR-f01-p1-tai-khoan-hoc-vien-018
    [Given(@"^học viên nhận link đặt lại mật khẩu cách đây (?:đúng )?""(\d+)"" phút$")]
    public async Task GotLinkMinutesAgo(int minutes)
    {
        await account.CreateActivatedAsync();
        await account.ForgotOkAsync();
        ctx.Clock.Advance(TimeSpan.FromMinutes(minutes));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-018 @BR-f01-p1-tai-khoan-hoc-vien-019
    [When(@"^học viên đặt lại mật khẩu ""([^""]*)"" bằng link đó$")]
    public Task ResetWithThatLink(string password) => account.TryResetAsync(account.ResetTokens[^1], password, password);

    // @BR-f01-p1-tai-khoan-hoc-vien-018 @BR-f01-p1-tai-khoan-hoc-vien-019 @BR-f01-p1-tai-khoan-hoc-vien-020
    [Then(@"^học viên đăng nhập được bằng mật khẩu ""([^""]*)""$")]
    public async Task CanLogInWith(string password)
    {
        Assert.Equal(HttpStatusCode.NoContent, ctx.LastResponse!.StatusCode);
        await account.StudentLoginAsync(password);
        await account.AssertStudentSessionAsync();
    }

    [Then("mật khẩu không được đặt lại vì link đã hết hiệu lực")]
    public async Task LinkNoLongerValid()
    {
        await api.AssertErrorAsync(HttpStatusCode.BadRequest, "auth.reset_link_invalid");
        await account.AssertProblemTitleAsync("Link đặt lại đã hết hạn hoặc đã được dùng. Hãy xin link mới.");
        await account.StudentLoginAsync(account.OwnPassword!); // mật khẩu hiện tại vẫn là mật khẩu đăng nhập được
        await account.AssertStudentSessionAsync();
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-019
    [Given("học viên đã đặt lại mật khẩu thành công bằng một link còn hạn")]
    public async Task ResetOnce()
    {
        await account.CreateActivatedAsync();
        await account.ForgotOkAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await account.TryResetAsync(account.ResetTokens[^1], "Coach2026", "Coach2026")).StatusCode);
    }

    [Given(@"^học viên đã dùng một link còn hạn để đặt mật khẩu ""([^""]*)"" và bị từ chối$")]
    public async Task ResetRejected(string password)
    {
        await account.CreateActivatedAsync();
        await account.ForgotOkAsync();
        var response = await account.TryResetAsync(account.ResetTokens[^1], password, password);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("auth.password_too_short", await StudentApi.ErrorCodeAsync(response));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-020
    [Given("học viên đã nhận link đặt lại mật khẩu hai lần")]
    public async Task GotTwoLinks()
    {
        await account.CreateActivatedAsync();
        await account.ForgotOkAsync();
        ctx.Clock.Advance(TimeSpan.FromSeconds(61)); // qua giới hạn 60 giây của BR-024
        await account.ForgotOkAsync();
        Assert.Equal(2, account.ResetTokens.Count);
    }

    [When(@"^học viên đặt lại mật khẩu ""([^""]*)"" bằng link lần (một|hai)$")]
    public Task ResetWithLink(string password, string which) =>
        account.TryResetAsync(account.ResetTokens[which == "một" ? 0 : 1], password, password);

    // @BR-f01-p1-tai-khoan-hoc-vien-021
    [Given("học viên đang đăng nhập web học viên trên điện thoại")]
    public async Task LoggedInOnPhone()
    {
        await account.CreateActivatedAsync();
        Assert.Equal(HttpStatusCode.OK, (await account.StudentLoginAsync(account.OwnPassword!)).StatusCode);
        _phone = ctx.Tokens;
    }

    [When("học viên đặt lại mật khẩu bằng link trên máy tính")]
    public async Task ResetOnComputer()
    {
        await account.ForgotOkAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await account.TryResetAsync(account.ResetTokens[^1], "Coach2026", "Coach2026")).StatusCode);
    }

    [Then("phiên đăng nhập trên điện thoại kết thúc, học viên phải đăng nhập lại")]
    public async Task PhoneSessionEnded()
    {
        var refresh = await ctx.PostJsonAsync("/api/auth/refresh", new { refreshToken = _phone!.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Given(@"^học viên đã đặt lại mật khẩu từ ""([^""]*)"" sang ""([^""]*)""$")]
    public async Task ResetFromTo(string from, string to)
    {
        await account.CreateActivatedAsync(password: from);
        await account.ForgotOkAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await account.TryResetAsync(account.ResetTokens[^1], to, to)).StatusCode);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-022 @BR-f01-p1-tai-khoan-hoc-vien-023
    [Given(@"^email ""([^""]*)"" là (email chưa có tài khoản nào|email của học viên còn Chờ kích hoạt|email của tài khoản coach|email của học viên Đã kích hoạt)$")]
    public async Task EmailIs(string email, string kind)
    {
        switch (kind)
        {
            case "email của học viên còn Chờ kích hoạt":
                await account.CreateAsync(email: email);
                break;
            case "email của tài khoản coach":
                await account.CreateCoachAccountAsync(email);
                break;
            case "email của học viên Đã kích hoạt":
                await account.CreateActivatedAsync(email: email);
                break;
            default: // email chưa có tài khoản nào
                break;
        }
    }

    [When(@"^một người xin đặt lại mật khẩu cho email ""([^""]*)""$")]
    public Task SomeoneAsksForReset(string email) => account.ForgotAsync(email);

    [Then(@"^không email nào được gửi tới ""([^""]*)""$")]
    public async Task NoMailTo(string email)
    {
        Assert.Equal(HttpStatusCode.Accepted, ctx.LastResponse!.StatusCode);
        await Task.Delay(AccountApi.QueueSettle);
        Assert.DoesNotContain(account.Outbox.All, m => string.Equals(m.To, email.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-023 @BR-f01-p1-tai-khoan-hoc-vien-024
    [Then("người đó nhận thông báo chung của Quên mật khẩu, không phân biệt được email có tài khoản hay không")]
    [Then("lần xin đó vẫn nhận thông báo chung của Quên mật khẩu")]
    public Task GenericForgotMessage() => account.AssertForgotGenericAsync();

    // @BR-f01-p1-tai-khoan-hoc-vien-024
    [Given(@"^học viên Đã kích hoạt đã xin đặt lại mật khẩu cách đây ""(\d+)"" phút$")]
    public async Task AskedMinutesAgo(int minutes)
    {
        await account.CreateActivatedAsync();
        await account.ForgotOkAsync();
        ctx.Clock.Advance(TimeSpan.FromMinutes(minutes));
    }

    [When("học viên xin đặt lại mật khẩu lần nữa")]
    public Task AskAgain() => account.ForgotAsync(account.Email());

    [Then("học viên nhận được email đặt lại thứ hai")]
    public async Task ReceivedSecondResetMail() =>
        Assert.Equal(2, (await account.WaitForMailsAsync(account.Email(), AccountApi.ResetSubject, 2)).Count);

    [Given(@"^học viên Đã kích hoạt đã xin đặt lại mật khẩu ""(\d+)"" lần trong ""(\d+)"" giây vừa qua$")]
    public async Task AskedTimesWithinSeconds(int times, int seconds)
    {
        await account.CreateActivatedAsync();
        for (var i = 0; i < times; i++)
        {
            if (i > 0)
                ctx.Clock.Advance(TimeSpan.FromSeconds((double)seconds / (times - 1)));
            Assert.Equal(HttpStatusCode.Accepted, (await account.ForgotAsync(account.Email())).StatusCode);
        }
    }

    [When(@"^học viên xin đặt lại mật khẩu lần thứ ""(\d+)""$")]
    public Task AskNthTime(int nth)
    {
        Assert.True(nth > 1);
        return account.ForgotAsync(account.Email());
    }

    [Then(@"^học viên chỉ nhận được ""(\d+)"" email đặt lại$")]
    public async Task OnlyResetMails(int count)
    {
        await account.WaitForMailsAsync(account.Email(), AccountApi.ResetSubject, count);
        await Task.Delay(AccountApi.QueueSettle);
        Assert.Equal(count, account.ResetMails().Count);
    }
}
```

- [ ] **Step 6: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f01-p1-tai-khoan-hoc-vien-017|Category=BR-f01-p1-tai-khoan-hoc-vien-018|Category=BR-f01-p1-tai-khoan-hoc-vien-019|Category=BR-f01-p1-tai-khoan-hoc-vien-020|Category=BR-f01-p1-tai-khoan-hoc-vien-021|Category=BR-f01-p1-tai-khoan-hoc-vien-022|Category=BR-f01-p1-tai-khoan-hoc-vien-023|Category=BR-f01-p1-tai-khoan-hoc-vien-024)&Category!=web"`
Expected: FAIL — build error `The type or namespace name 'ForgotPasswordResponse' could not be found` (Application chưa có `Accounts/Commands/RequestPasswordReset.cs`).

- [ ] **Step 7: Application — lỗi, thời hạn, port, ba use case**

`backend/src/Coaching.Application/Accounts/AccountErrors.cs` — thêm member:
```csharp

    // @BR-f01-p1-tai-khoan-hoc-vien-017
    public static readonly Error NotActivated = new(
        "account.not_activated", "Tài khoản chưa kích hoạt. Hãy gửi thông tin đăng nhập.", ErrorKind.Conflict);
```

`backend/src/Coaching.Application/Auth/AuthErrors.cs` — thêm cuối class:
```csharp

    // @BR-f01-p1-tai-khoan-hoc-vien-018 @BR-f01-p1-tai-khoan-hoc-vien-019 @BR-f01-p1-tai-khoan-hoc-vien-020: hết hạn / đã dùng / đã có link mới → cùng một lỗi
    public static readonly Error ResetLinkInvalid = new(
        "auth.reset_link_invalid", "Link đặt lại đã hết hạn hoặc đã được dùng. Hãy xin link mới.");
```

`backend/src/Coaching.Application/Accounts/AccountTokenLifetimes.cs` — thêm vào class:
```csharp

    /// <summary>@BR-f01-p1-tai-khoan-hoc-vien-018: link đặt lại hết hiệu lực khi đủ 30 phút (now >= ExpiresAt).</summary>
    public static readonly TimeSpan PasswordReset = TimeSpan.FromMinutes(30);

    /// <summary>@BR-f01-p1-tai-khoan-hoc-vien-024 @ADR-f01-p1-tai-khoan-hoc-vien-005: mỗi tài khoản tối đa một thư Quên mật khẩu mỗi 60 giây.</summary>
    public static readonly TimeSpan SelfResetInterval = TimeSpan.FromSeconds(60);
```

`backend/src/Coaching.Application/Accounts/IAccountCredentials.cs` — thêm sau `ChangeEmailAsync`:
```csharp

    /// <summary>Id tài khoản có email này (so khớp đã chuẩn hóa, không phân biệt hoa/thường); null nếu không có.</summary>
    Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken ct = default);
```

`backend/src/Coaching.Application/Accounts/ISessionRevoker.cs`:
```csharp
namespace Coaching.Application.Accounts;

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-009: kết thúc mọi phiên (thu hồi mọi refresh token còn sống) của tài khoản sau khi đặt lại mật khẩu.
/// Access token còn sống tối đa 15 phút (F00); lần làm mới kế tiếp bị từ chối.
/// </summary>
public interface ISessionRevoker
{
    Task RevokeAllAsync(Guid userId, CancellationToken ct = default);
}
```

`backend/src/Coaching.Application/Students/Commands/SendPasswordResetLink.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Accounts;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Students.Commands;

/// <summary>Coach gửi link đặt lại mật khẩu (30 phút, dùng một lần) tới email của học viên đã kích hoạt.</summary>
public sealed class SendPasswordResetLinkHandler(
    CurrentCoach currentCoach,
    IStudentRepository students,
    IAccountTokens tokens,
    IAccountEmails emails,
    IEmailSender sender,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<StudentDetailDto>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await currentCoach.GetAsync(cancellationToken) is not { } coach)
            return StudentErrors.CoachNotFound;

        // Gửi lỗi → rollback: không có link mới, lịch sử không thêm dòng (cùng mẫu ADR-f01-p1-tai-khoan-hoc-vien-004 của Task 4)
        return await unitOfWork.ExecuteInTransactionAsync<StudentDetailDto>(async ct =>
        {
            if (await students.GetAsync(id, coach.Id, ct) is not { } student)
                return Result<StudentDetailDto>.Failure(StudentErrors.NotFound);
            if (student.AccountStatus != AccountStatus.Activated)
                return Result<StudentDetailDto>.Failure(AccountErrors.NotActivated); // @BR-f01-p1-tai-khoan-hoc-vien-017 → 409

            var now = clock.UtcNow;
            student.RecordResetLinkSent(coach.UserId, now);
            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (ConcurrencyConflictException)
            {
                return Result<StudentDetailDto>.Failure(StudentErrors.VersionConflict);
            }

            // @BR-f01-p1-tai-khoan-hoc-vien-018: 30 phút; @BR-f01-p1-tai-khoan-hoc-vien-020: link mới hủy mọi link cũ (IssueAsync)
            var rawToken = await tokens.IssueAsync(student.UserId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Coach,
                now + AccountTokenLifetimes.PasswordReset, ct);
            var sent = await AccountMail.SendAsync(sender, emails.PasswordReset(student.Email.Value, student.FullName, rawToken), ct);
            return sent.IsSuccess
                ? Result<StudentDetailDto>.Success(StudentDetailDto.From(student, coach, now))
                : Result<StudentDetailDto>.Failure(sent.Error!);
        }, cancellationToken);
    }
}
```

`backend/src/Coaching.Application/Accounts/Commands/RequestPasswordReset.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Domain.Students;

namespace Coaching.Application.Accounts.Commands;

public sealed record ForgotPasswordCommand(string? Email);

/// <summary>@BR-f01-p1-tai-khoan-hoc-vien-023: kết quả duy nhất của Quên mật khẩu, mọi trường hợp như nhau.</summary>
public sealed record ForgotPasswordResponse(string Message)
{
    public const string GenericMessage =
        "Nếu email này thuộc tài khoản học viên đã kích hoạt, link đặt lại mật khẩu đã được gửi tới email đó. Link có hiệu lực trong 30 phút. Không thấy email thì kiểm tra mục Spam.";

    public static ForgotPasswordResponse Generic { get; } = new(GenericMessage);
}

/// <summary>Học viên tự xin link đặt lại qua Quên mật khẩu. Không ghi lịch sử tài khoản (chỉ có vết trong AccountTokens).</summary>
public sealed class RequestPasswordResetHandler(
    IAccountCredentials credentials,
    IStudentRepository students,
    IAccountTokens tokens,
    IAccountEmails emails,
    IEmailQueue queue,
    IClock clock)
{
    public async Task<ForgotPasswordResponse> HandleAsync(ForgotPasswordCommand command, CancellationToken cancellationToken = default)
    {
        await TryIssueAsync(command.Email, cancellationToken);
        return ForgotPasswordResponse.Generic; // @BR-f01-p1-tai-khoan-hoc-vien-023: không lộ email nào có tài khoản
    }

    private async Task TryIssueAsync(string? email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;
        // @BR-f01-p1-tai-khoan-hoc-vien-022: chỉ học viên Đã kích hoạt; email bỏ khoảng trắng đầu/cuối, không phân biệt hoa/thường.
        // Email lạ, học viên Chờ kích hoạt, tài khoản coach (không có Student) → không gửi gì
        if (await credentials.FindUserIdByEmailAsync(email.Trim(), ct) is not { } userId)
            return;
        if (await students.GetByUserIdAsync(userId, ct) is not { AccountStatus: AccountStatus.Activated } student)
            return;

        var now = clock.UtcNow;
        // @BR-f01-p1-tai-khoan-hoc-vien-024 @ADR-f01-p1-tai-khoan-hoc-vien-005: theo tài khoản, dựa trên link tự xin gần nhất
        if (await tokens.LastIssuedAtAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self, ct) is { } last
            && now - last < AccountTokenLifetimes.SelfResetInterval)
            return;

        // @BR-f01-p1-tai-khoan-hoc-vien-018 @BR-f01-p1-tai-khoan-hoc-vien-020: link 30 phút, hủy mọi link cũ (kể cả link coach gửi)
        var rawToken = await tokens.IssueAsync(userId, AccountTokenPurpose.PasswordReset, AccountTokenSource.Self,
            now + AccountTokenLifetimes.PasswordReset, ct);
        var message = emails.PasswordReset(student.Email.Value, student.FullName, rawToken);
        if (message.IsSuccess)
            await queue.EnqueueAsync(message.Value, ct); // @ADR-f01-p1-tai-khoan-hoc-vien-005: gửi nền, phản hồi không chờ dịch vụ email
    }
}
```

`backend/src/Coaching.Application/Accounts/Commands/ResetPassword.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Accounts.Commands;

public sealed record ResetPasswordCommand(string? Token, string? Password, string? ConfirmPassword);

/// <summary>Đặt mật khẩu mới bằng link đặt lại (coach gửi hoặc tự xin).</summary>
public sealed class ResetPasswordHandler(
    IAccountTokens tokens,
    IAccountCredentials credentials,
    ISessionRevoker sessions,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        // @BR-f01-p1-tai-khoan-hoc-vien-018 (now >= hạn), @BR-f01-p1-tai-khoan-hoc-vien-019 (đã dùng), @BR-f01-p1-tai-khoan-hoc-vien-020 (đã có link mới)
        if (await tokens.FindValidAsync(command.Token, AccountTokenPurpose.PasswordReset, cancellationToken) is not { } userId)
            return Result.Failure(AuthErrors.ResetLinkInvalid);

        // @BR-f01-p1-tai-khoan-hoc-vien-019: mật khẩu không hợp lệ → 400 và KHÔNG tiêu link (luật mật khẩu của kích hoạt áp nguyên)
        if (AuthErrors.ForPassword(PasswordPolicy.Check(command.Password, command.ConfirmPassword)) is { } invalid)
            return Result.Failure(invalid);

        var reset = await unitOfWork.ExecuteInTransactionAsync<Guid>(async ct =>
        {
            await credentials.SetOwnPasswordAsync(userId, command.Password!, ct);
            await tokens.ConsumeAsync(command.Token!, ct); // @BR-f01-p1-tai-khoan-hoc-vien-019: chỉ thành công một lần
            await tokens.RevokeAllAsync(userId, AccountTokenPurpose.PasswordReset, ct);
            // @BR-f01-p1-tai-khoan-hoc-vien-021 @ADR-f01-p1-tai-khoan-hoc-vien-009: mọi phiên đang có kết thúc, chỉ mật khẩu mới đăng nhập được
            await sessions.RevokeAllAsync(userId, ct);
            return Result<Guid>.Success(userId);
        }, cancellationToken);
        return reset.IsSuccess ? Result.Success() : Result.Failure(reset.Error!);
    }
}
```

`backend/src/Coaching.Application/DependencyInjection.cs` — thêm trước `return services;`:
```csharp
        services.AddScoped<SendPasswordResetLinkHandler>();
        services.AddScoped<RequestPasswordResetHandler>();
        services.AddScoped<ResetPasswordHandler>();
```

- [ ] **Step 8: Infrastructure — tách `SessionRevoker`, lý do thu hồi mới, tìm tài khoản theo email**

`backend/src/Coaching.Infrastructure/Identity/RefreshTokenRevokedReason.cs` — thay toàn bộ:
```csharp
namespace Coaching.Infrastructure.Identity;

/// <summary>
/// @ADR-f00-codebase-003: phân biệt thu hồi do xoay vòng / đăng xuất / phát hiện lộ mã.
/// @ADR-f01-p1-tai-khoan-hoc-vien-009: PasswordReset — thu hồi vì học viên đặt lại mật khẩu (cột nvarchar(20) đủ chỗ, không đổi schema).
/// </summary>
public enum RefreshTokenRevokedReason
{
    Rotated,
    Logout,
    ReuseDetected,
    PasswordReset,
}
```

`backend/src/Coaching.Infrastructure/Identity/SessionRevoker.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Accounts;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Identity;

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-009: thu hồi mọi refresh token còn sống của tài khoản — dùng chung cho đặt lại mật khẩu
/// (ISessionRevoker) và phát hiện lộ mã của F00 (AuthService, @BR-f00-codebase-012). Chạy trong transaction hiện tại nếu có.
/// </summary>
internal sealed class SessionRevoker(AppDbContext db, IClock clock) : ISessionRevoker
{
    // @BR-f01-p1-tai-khoan-hoc-vien-021
    public Task RevokeAllAsync(Guid userId, CancellationToken ct = default) =>
        RevokeAllAsync(userId, RefreshTokenRevokedReason.PasswordReset, ct);

    public async Task RevokeAllAsync(Guid userId, RefreshTokenRevokedReason reason, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokedReason, reason), ct);
    }
}
```

`backend/src/Coaching.Infrastructure/Identity/AuthService.cs`:
- Trong primary constructor, thêm tham số `SessionRevoker sessions,` ngay trước `IClock clock) : IAuthService`.
- Trong `RefreshAsync`, thay dòng `            await RevokeAllAsync(stored.UserId, now, cancellationToken);` bằng:
```csharp
            await sessions.RevokeAllAsync(stored.UserId, RefreshTokenRevokedReason.ReuseDetected, cancellationToken);
```
- Xóa method private `RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)` ở cuối class (logic đã chuyển sang `SessionRevoker`).

`backend/src/Coaching.Infrastructure/Identity/AccountCredentials.cs` — thêm sau `ChangeEmailAsync`:
```csharp

    // @BR-f01-p1-tai-khoan-hoc-vien-022: FindByEmailAsync so NormalizedEmail → "QUAN@gmail.com " (đã trim) khớp quan@gmail.com
    public async Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken ct = default) =>
        string.IsNullOrWhiteSpace(email) ? null : (await users.FindByEmailAsync(email.Trim()))?.Id;
```

`backend/src/Coaching.Infrastructure/DependencyInjection.cs` — thêm sau dòng `services.AddScoped<IAccountCredentials, AccountCredentials>();`:
```csharp
        services.AddScoped<SessionRevoker>();
        services.AddScoped<ISessionRevoker>(sp => sp.GetRequiredService<SessionRevoker>());
```

- [ ] **Step 9: API — route coach gửi link, Quên mật khẩu, đặt lại mật khẩu**

`backend/src/Coaching.Api/Controllers/Admin/StudentsController.cs` — thêm action cuối class:
```csharp

    // @BR-f01-p1-tai-khoan-hoc-vien-017: chỉ tài khoản Đã kích hoạt, mọi trạng thái coaching; link chỉ có trong email
    [HttpPost("{id:guid}/password-reset-link")]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendPasswordResetLink(Guid id, [FromServices] SendPasswordResetLinkHandler handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, cancellationToken)).ToActionResult(this);
```

`backend/src/Coaching.Api/Controllers/AuthController.cs` — thêm action sau `Activate`:
```csharp

    // @BR-f01-p1-tai-khoan-hoc-vien-023 @ADR-f01-p1-tai-khoan-hoc-vien-005: luôn 202 cùng một body; thư gửi nền.
    // Giới hạn theo IP là policy `auth`; giới hạn 60 giây theo tài khoản nằm trong use case (@BR-f01-p1-tai-khoan-hoc-vien-024).
    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<ForgotPasswordResponse>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, [FromServices] RequestPasswordResetHandler handler,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status202Accepted, await handler.HandleAsync(command, cancellationToken));

    // @BR-f01-p1-tai-khoan-hoc-vien-018 @BR-f01-p1-tai-khoan-hoc-vien-019 @BR-f01-p1-tai-khoan-hoc-vien-021: không cần JWT, quyền đến từ token đã băm
    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, [FromServices] ResetPasswordHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? NoContent() : result.Error!.ToProblem(this);
    }
```

- [ ] **Step 10: Chạy acceptance — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f01-p1-tai-khoan-hoc-vien-017|Category=BR-f01-p1-tai-khoan-hoc-vien-018|Category=BR-f01-p1-tai-khoan-hoc-vien-019|Category=BR-f01-p1-tai-khoan-hoc-vien-020|Category=BR-f01-p1-tai-khoan-hoc-vien-021|Category=BR-f01-p1-tai-khoan-hoc-vien-022|Category=BR-f01-p1-tai-khoan-hoc-vien-023|Category=BR-f01-p1-tai-khoan-hoc-vien-024)&Category!=web"`
Expected: `Passed: 20`, `Failed: 0` (BR-017: 2 ví dụ + 1; BR-018: 2; BR-019: 2; BR-020: 2; BR-021: 2; BR-022: 3 ví dụ; BR-023: 4 ví dụ; BR-024: 2).
Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "Category=BR-f00-codebase-012"`
Expected: PASS — phát hiện lộ mã của F00 vẫn thu hồi mọi phiên qua `SessionRevoker`.

- [ ] **Step 11: Integration test đặt lại mật khẩu**

`backend/tests/Coaching.Api.IntegrationTests/Accounts/AccountRequests.cs`:
- Thêm `using System.Diagnostics;` vào đầu file.
- Thêm hằng số dưới `CredentialsSubject`:
```csharp
    public const string ResetSubject = "Đặt lại mật khẩu Duobexiuuu Coaching";
```
- Thêm cuối class (trước `[GeneratedRegex(@"Mật khẩu tạm:\s*(\S+)")]`):
```csharp
    public static Task<HttpResponseMessage> SendResetLinkAsync(HttpClient admin, Guid id) =>
        admin.PostAsJsonAsync($"/api/admin/students/{id}/password-reset-link", new { });

    public static Task<HttpResponseMessage> ForgotAsync(HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

    public static Task<HttpResponseMessage> ResetAsync(HttpClient client, string token, string password, string confirmPassword) =>
        client.PostAsJsonAsync("/api/auth/reset-password", new { token, password, confirmPassword });

    public static string ResetToken(CapturedEmail mail) =>
        ResetTokenPattern().Match(mail.TextBody) is { Success: true } match
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"Thư tới {mail.To} không có link đặt lại.");

    /// <summary>Chờ (tối đa 5 giây) tới khi có ít nhất <paramref name="count"/> thư cùng tiêu đề tới <paramref name="to"/>.</summary>
    public static async Task<IReadOnlyList<CapturedEmail>> WaitForMailsAsync(EmailOutbox outbox, string to, string subject, int count)
    {
        var watch = Stopwatch.StartNew();
        while (true)
        {
            var mails = outbox.All.Where(m => m.To == to && m.Subject == subject).ToList();
            if (mails.Count >= count)
                return mails;
            if (watch.Elapsed > TimeSpan.FromSeconds(5))
                throw new TimeoutException($"Sau 5 giây mới có {mails.Count}/{count} thư '{subject}' tới {to}.");
            await Task.Delay(50);
        }
    }

    [GeneratedRegex(@"/dat-lai-mat-khau#t=([A-Za-z0-9_-]+)")]
    private static partial Regex ResetTokenPattern();

```

`backend/tests/Coaching.Api.IntegrationTests/Accounts/PasswordResetTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Accounts.Commands;
using Coaching.Application.Auth;
using Coaching.Application.Students;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Accounts;

[Collection(ApiCollection.Name)]
public sealed class PasswordResetTests(ApiFactory factory)
{
    private static async Task<AuthTokens> LoginTokensAsync(HttpClient client, string email, string password)
    {
        var response = await AccountRequests.LoginAsync(client, email, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthTokens>())!;
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-023 @ADR-f01-p1-tai-khoan-hoc-vien-005: học viên đã kích hoạt, email lạ, học viên Chờ kích hoạt,
    // email coach → cùng status, cùng body từng byte (NFR-f01-p1-tai-khoan-hoc-vien-002, phần không đo thời gian)
    [Fact]
    public async Task Forgot_password_answers_identically_for_every_email_kind()
    {
        var activated = await AccountRequests.CreateActivatedAsync(factory);
        var admin = await factory.CaptureAdminClientAsync();
        var pending = await AccountRequests.CreateCompleteAsync(admin);
        string[] emails = [activated.Email, $"la-{Guid.NewGuid():N}@test.local", pending.Email, ApiFactory.AdminEmail];
        var client = factory.Capture.CreateClient();

        var answers = new List<(HttpStatusCode Status, string Body)>();
        foreach (var email in emails)
        {
            var response = await AccountRequests.ForgotAsync(client, email);
            answers.Add((response.StatusCode, await response.Content.ReadAsStringAsync()));
        }

        Assert.All(answers, a => Assert.Equal(HttpStatusCode.Accepted, a.Status));
        Assert.Single(answers.Select(a => a.Body).Distinct());
        using var json = JsonDocument.Parse(answers[0].Body);
        Assert.Equal(ForgotPasswordResponse.GenericMessage, json.RootElement.GetProperty("message").GetString());
    }

    // @ADR-f01-p1-tai-khoan-hoc-vien-005 — seed LOẠI của BR-f01-p1-tai-khoan-hoc-vien-027: học viên tự xin link → lịch sử tài khoản không thêm dòng
    [Fact]
    public async Task Self_forgot_request_adds_no_account_history_line()
    {
        var student = await AccountRequests.CreateActivatedAsync(factory);
        var admin = await factory.CaptureAdminClientAsync();
        var before = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{student.Id}"))!.Account.History.Count;

        var response = await AccountRequests.ForgotAsync(factory.Capture.CreateClient(), student.Email);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await AccountRequests.WaitForMailsAsync(factory.Outbox, student.Email, AccountRequests.ResetSubject, 1);
        var after = (await admin.GetFromJsonAsync<StudentDetailDto>($"{StudentRequests.Url}/{student.Id}"))!.Account.History.Count;
        Assert.Equal(before, after);
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-021 @ADR-f01-p1-tai-khoan-hoc-vien-009: đặt lại xong → mọi refresh token bị thu hồi với lý do PasswordReset
    [Fact]
    public async Task Password_reset_revokes_every_session()
    {
        var student = await AccountRequests.CreateActivatedAsync(factory, "Coach2025");
        var client = factory.Capture.CreateClient();
        var phone = await LoginTokensAsync(client, student.Email, "Coach2025");
        var laptop = await LoginTokensAsync(client, student.Email, "Coach2025");
        var admin = await factory.CaptureAdminClientAsync();
        Assert.Equal(HttpStatusCode.OK, (await AccountRequests.SendResetLinkAsync(admin, student.Id)).StatusCode);
        var token = AccountRequests.ResetToken(
            factory.Outbox.All.Last(m => m.To == student.Email && m.Subject == AccountRequests.ResetSubject));

        var reset = await AccountRequests.ResetAsync(client, token, "Coach2026", "Coach2026");

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        foreach (var session in new[] { phone, laptop })
        {
            var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = session.RefreshToken });
            Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = await db.Students.Where(s => s.Id == student.Id).Select(s => s.UserId).SingleAsync();
        var sessions = await db.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
        Assert.NotEmpty(sessions);
        Assert.All(sessions, t => Assert.Equal(RefreshTokenRevokedReason.PasswordReset, t.RevokedReason));
    }

    // @BR-f01-p1-tai-khoan-hoc-vien-020: link tự xin mới hủy link coach gửi trước đó
    [Fact]
    public async Task New_self_link_supersedes_the_coach_link()
    {
        var student = await AccountRequests.CreateActivatedAsync(factory);
        var admin = await factory.CaptureAdminClientAsync();
        var client = factory.Capture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await AccountRequests.SendResetLinkAsync(admin, student.Id)).StatusCode);
        var coachToken = AccountRequests.ResetToken(
            factory.Outbox.All.Last(m => m.To == student.Email && m.Subject == AccountRequests.ResetSubject));
        Assert.Equal(HttpStatusCode.Accepted, (await AccountRequests.ForgotAsync(client, student.Email)).StatusCode);
        var selfToken = AccountRequests.ResetToken(
            (await AccountRequests.WaitForMailsAsync(factory.Outbox, student.Email, AccountRequests.ResetSubject, 2))[^1]);

        var withCoachLink = await AccountRequests.ResetAsync(client, coachToken, "Coach2026", "Coach2026");
        var withSelfLink = await AccountRequests.ResetAsync(client, selfToken, "Coach2026", "Coach2026");

        Assert.Equal(HttpStatusCode.BadRequest, withCoachLink.StatusCode);
        Assert.Equal("auth.reset_link_invalid", await StudentRequests.ErrorCodeAsync(withCoachLink));
        Assert.Equal(HttpStatusCode.NoContent, withSelfLink.StatusCode);
    }
}
```

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Accounts"`
Expected: `Passed: 18` (SendCredentialsTests 3, TempPasswordLoginTests 4, ActivationTests 3, ChangeEmailTests 4, PasswordResetTests 4).
Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~RefreshGraceTests|FullyQualifiedName~AuthAndAuthorizationTests"`
Expected: không Failed (thu hồi khi lộ mã của F00 đi qua `SessionRevoker`).

- [ ] **Step 12: Verify toàn bộ**

Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed và không còn `MissingStepDefinition` nào — mọi scenario `@api` của thong-tin-dang-nhap, kich-hoat, dat-lai-mat-khau cùng F00/F02 xanh (scenario `@web` chỉ chạy ở Task 10).

- [ ] **Step 13: Commit**
```bash
git add backend/src backend/tests
git commit -F - <<'EOF'
feat(accounts): password reset link, forgot password and reset with session revocation (BR-f01-p1-017..024)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

---

### Task 8: [UI] Web học viên — Đăng nhập (+ "Quên mật khẩu?"), Kích hoạt, Quên mật khẩu, Đặt lại mật khẩu

Màn hình (tech-design §3 "Thiết kế giao diện (UI)", page `Main` của prototype): **Đăng nhập** (`/login`, thêm "Quên mật khẩu?" chỉ ở web học viên; mật khẩu tạm → `/kich-hoat`), **Kích hoạt** (`/kich-hoat`, state `isActivate`), **Quên mật khẩu** (`/quen-mat-khau`, state `isForgot`), **Đặt lại mật khẩu** (`/dat-lai-mat-khau#t=…`, *suy ra* — dùng lại bố cục Kích hoạt). Bìa (cover) + khung form tách thành `AuthFrameComponent` dùng chung cho 4 màn; style form tách thành `auth-form.scss` (giá trị nguyên văn page `Main`). Web quản trị giữ nguyên hình học trang đăng nhập (test so ảnh F00 vẫn dùng admin `/login`).

**Files:**
- Create: `frontend/projects/shared/src/lib/auth/activation-store.ts`
- Create: `frontend/projects/shared/src/lib/account/login-notice.ts`, `account-errors.ts`, `password-checklist.component.ts`
- Create: `frontend/projects/shared/src/lib/account/activate-page.component.ts`, `activate-page.component.spec.ts`
- Create: `frontend/projects/shared/src/lib/account/forgot-password-page.component.ts`, `forgot-password-page.component.spec.ts`
- Create: `frontend/projects/shared/src/lib/account/reset-password-page.component.ts`, `reset-password-page.component.spec.ts`
- Create: `frontend/projects/shared/src/lib/login/auth-frame.component.ts`, `auth-frame.component.scss`, `auth-form.scss`
- Modify: `frontend/projects/shared/src/lib/auth/models.ts`, `auth.service.ts`, `auth.service.spec.ts`
- Modify: `frontend/projects/shared/src/lib/login/login-page.component.ts`, `login-page.component.html`, `login-page.component.spec.ts`
- Delete: `frontend/projects/shared/src/lib/login/login-page.component.scss` (nội dung chuyển sang `auth-frame.component.scss` + `auth-form.scss`)
- Modify: `frontend/projects/shared/src/public-api.ts`, `frontend/projects/client/src/app/app.routes.ts`

**Interfaces:**
- Consumes: Task 4 — `POST /api/auth/login` trả `200 AuthTokens` hoặc `200 { activationRequired: true, activationToken, activationTokenExpiresAt }`; Task 5 — `POST /api/auth/activate` (`{ activationToken, password, confirmPassword }` → `200 AuthTokens`; `auth.activation_invalid` 401, `auth.password_too_short|too_long|mismatch` 400); Task 7 — `POST /api/auth/forgot-password` (`{ email }` → `202 { message }`), `POST /api/auth/reset-password` (`{ token, password, confirmPassword }` → 204; `auth.reset_link_invalid` 400). ProblemDetails có `title` (câu tiếng Việt) và `code` (`ResultExtensions`). F00 — `AuthService`, `TokenStore`, `RefreshCoordinator`, `guestGuard`, `CLIENT_APP`, `LoginPageComponent`.
- Produces:
  - `models.ts`: `ActivationChallenge`, `LoginResult = { kind: 'session' } | { kind: 'activation'; token: string; expiresAt: string }`.
  - `AuthService.login(email, password): Promise<LoginResult>`; `activate(activationToken, password, confirmPassword): Promise<void>`; `forgotPassword(email): Promise<string>`; `resetPassword(token, password, confirmPassword): Promise<void>` (xoá phiên cục bộ nếu có).
  - `ActivationStore` (`current()`, `set(ticket)`, `clear()`), `activationGuard`; `LoginNoticeStore` (`set`, `take`); `problemCode(error)`, `accountErrorMessage(error)`.
  - `AuthFrameComponent` (`dbx-auth-frame`), `PasswordChecklistComponent` (`dbx-password-checklist`), `ActivatePageComponent`, `ForgotPasswordPageComponent`, `ResetPasswordPageComponent` (`RESET_DONE_NOTICE`, `RESET_LINK_INVALID`).
  - Route web học viên `/kich-hoat`, `/quen-mat-khau`, `/dat-lai-mat-khau`; nút "Quên mật khẩu?" chỉ ở web học viên.

**Owns:** `@ADR-f01-p1-tai-khoan-hoc-vien-010`

- [ ] **Step 1: Viết spec cho AuthService ở web học viên (đăng nhập bằng mật khẩu tạm, kích hoạt, quên / đặt lại mật khẩu)**

`frontend/projects/shared/src/lib/auth/auth.service.spec.ts` — thêm import ở đầu file:
```ts
import { ActivationStore } from './activation-store';
```
và thêm `describe` mới ở cuối file:
```ts
describe('AuthService (web học viên)', () => {
  let auth: AuthService;
  let backend: HttpTestingController;
  const tokens = {
    accessToken: 'a-9', accessTokenExpiresAt: '', refreshToken: 'refresh-9', refreshTokenExpiresAt: '',
    user: { id: '2', email: 'quan@gmail.com', fullName: 'Phạm Hoàng Quân', roles: ['Student'] },
  };

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'client' },
        provideRouter([{ path: 'login', children: [] }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    auth = TestBed.inject(AuthService);
    backend = TestBed.inject(HttpTestingController);
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-011: mật khẩu tạm → mã kích hoạt trong bộ nhớ, chưa có phiên
  it('login bằng mật khẩu tạm trả về bước kích hoạt, không lưu phiên', async () => {
    const p = auth.login('quan@gmail.com', 'Tam8K2qXyZab');
    backend.expectOne('/api/auth/login').flush({
      activationRequired: true, activationToken: 'act-1', activationTokenExpiresAt: '2026-11-25T03:00:00Z',
    });

    expect(await p).toEqual({ kind: 'activation', token: 'act-1', expiresAt: '2026-11-25T03:00:00Z' });
    expect(TestBed.inject(ActivationStore).current()).toEqual({ token: 'act-1', expiresAt: '2026-11-25T03:00:00Z' });
    expect(auth.user()).toBeNull();
    expect(localStorage.getItem('dbx.client.rt')).toBeNull();
  });

  it('login bằng mật khẩu riêng trả về phiên như F00', async () => {
    const p = auth.login('quan@gmail.com', 'Coach2026');
    backend.expectOne('/api/auth/login').flush(tokens);

    expect(await p).toEqual({ kind: 'session' });
    expect(auth.user()?.fullName).toBe('Phạm Hoàng Quân');
    expect(localStorage.getItem('dbx.client.rt')).toBe('refresh-9');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-015 @BR-f01-p1-tai-khoan-hoc-vien-016: gửi nguyên văn (không trim), lưu xong vào thẳng
  it('activate gửi mã kích hoạt và hai lần nhập nguyên văn rồi vào phiên', async () => {
    const store = TestBed.inject(ActivationStore);
    store.set({ token: 'act-1', expiresAt: '2026-11-25T03:00:00Z' });

    const p = auth.activate('act-1', ' Coach2026 ', ' Coach2026 ');
    const request = backend.expectOne('/api/auth/activate');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ activationToken: 'act-1', password: ' Coach2026 ', confirmPassword: ' Coach2026 ' });
    request.flush(tokens);
    await p;

    expect(auth.user()).not.toBeNull();
    expect(store.current()).toBeNull();
    expect(localStorage.getItem('dbx.client.rt')).toBe('refresh-9');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-023
  it('forgotPassword trả về câu chung của server', async () => {
    const message = 'Nếu email này thuộc tài khoản học viên đã kích hoạt, link đặt lại mật khẩu đã được gửi tới email đó. Link có hiệu lực trong 30 phút. Không thấy email thì kiểm tra mục Spam.';
    const p = auth.forgotPassword('QUAN@gmail.com');
    const request = backend.expectOne('/api/auth/forgot-password');
    expect(request.request.body).toEqual({ email: 'QUAN@gmail.com' });
    request.flush({ message }, { status: 202, statusText: 'Accepted' });

    expect(await p).toBe(message);
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-021: đặt lại xong → phiên trên máy này cũng kết thúc
  it('resetPassword gửi token + mật khẩu và xoá phiên đang có trên máy này', async () => {
    localStorage.setItem('dbx.client.rt', 'refresh-old');
    const p = auth.resetPassword('tok-1', 'Coach2026', 'Coach2026');
    const request = backend.expectOne('/api/auth/reset-password');
    expect(request.request.body).toEqual({ token: 'tok-1', password: 'Coach2026', confirmPassword: 'Coach2026' });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await p;

    expect(localStorage.getItem('dbx.client.rt')).toBeNull();
    expect(auth.user()).toBeNull();
  });
});
```

- [ ] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test shared --watch=false`
Expected: FAIL — `Cannot find module './activation-store'` (và `auth.activate is not a function`…).

- [ ] **Step 3: Model `LoginResult`, `ActivationStore`, `LoginNoticeStore`, AuthService**

`frontend/projects/shared/src/lib/auth/models.ts` — thêm cuối file:
```ts

/** @ADR-f01-p1-tai-khoan-hoc-vien-002: đăng nhập bằng mật khẩu tạm còn hạn → mã kích hoạt, không có phiên. */
export interface ActivationChallenge {
  activationRequired: true;
  activationToken: string;
  activationTokenExpiresAt: string;
}

export type LoginResult = { kind: 'session' } | { kind: 'activation'; token: string; expiresAt: string };
```

`frontend/projects/shared/src/lib/auth/activation-store.ts`:
```ts
import { Injectable, inject, signal } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export interface ActivationTicket {
  token: string;
  expiresAt: string;
}

// @ADR-f01-p1-tai-khoan-hoc-vien-010: mã kích hoạt chỉ giữ trong bộ nhớ của FE — tải lại /kich-hoat là mất,
// học viên đăng nhập lại bằng mật khẩu tạm.
@Injectable({ providedIn: 'root' })
export class ActivationStore {
  private readonly ticket = signal<ActivationTicket | null>(null);
  readonly current = this.ticket.asReadonly();

  set(ticket: ActivationTicket): void {
    this.ticket.set(ticket);
  }

  clear(): void {
    this.ticket.set(null);
  }
}

/** Vào /kich-hoat mà không có mã kích hoạt → về /login. */
export const activationGuard: CanActivateFn = () =>
  inject(ActivationStore).current() ? true : inject(Router).createUrlTree(['/login']);
```

`frontend/projects/shared/src/lib/account/login-notice.ts`:
```ts
import { Injectable } from '@angular/core';

export interface LoginNotice {
  tone: 'ok' | 'error';
  text: string;
}

/** Câu báo mang từ màn khác sang trang đăng nhập (đặt lại xong, phiên kích hoạt hết hạn); đọc một lần. */
@Injectable({ providedIn: 'root' })
export class LoginNoticeStore {
  private notice: LoginNotice | null = null;

  set(notice: LoginNotice): void {
    this.notice = notice;
  }

  take(): LoginNotice | null {
    const notice = this.notice;
    this.notice = null;
    return notice;
  }
}
```

`frontend/projects/shared/src/lib/auth/auth.service.ts` — sửa import:
```ts
import { ActivationChallenge, AuthTokens, AuthUser, LoginResult } from './models';
import { ActivationStore } from './activation-store';
```
thêm field sau `private readonly router = inject(Router);`:
```ts
  private readonly activation = inject(ActivationStore);
```
thay toàn bộ hàm `login` bằng:
```ts
  async login(email: string, password: string): Promise<LoginResult> {
    const body = await firstValueFrom(
      this.http.post<AuthTokens | ActivationChallenge>(this.apiUrl('/api/auth/login'), { email, password, app: this.app }, this.noAuth),
    );
    // @BR-f01-p1-tai-khoan-hoc-vien-011: mật khẩu tạm → chỉ có mã kích hoạt, chưa có phiên (không JWT, không refresh token)
    if (isActivationChallenge(body)) {
      this.activation.set({ token: body.activationToken, expiresAt: body.activationTokenExpiresAt });
      return { kind: 'activation', token: body.activationToken, expiresAt: body.activationTokenExpiresAt };
    }
    this.activation.clear();
    this.accept(body);
    return { kind: 'session' };
  }

  /** @BR-f01-p1-tai-khoan-hoc-vien-015: mật khẩu gửi nguyên văn, không trim. Thành công → API trả AuthTokens, vào thẳng. */
  async activate(activationToken: string, password: string, confirmPassword: string): Promise<void> {
    const tokens = await firstValueFrom(
      this.http.post<AuthTokens>(this.apiUrl('/api/auth/activate'), { activationToken, password, confirmPassword }, this.noAuth),
    );
    this.activation.clear();
    this.accept(tokens);
  }

  /** @BR-f01-p1-tai-khoan-hoc-vien-023: server luôn trả cùng một câu (202). */
  async forgotPassword(email: string): Promise<string> {
    const body = await firstValueFrom(
      this.http.post<{ message: string }>(this.apiUrl('/api/auth/forgot-password'), { email }, this.noAuth),
    );
    return body.message;
  }

  /** @BR-f01-p1-tai-khoan-hoc-vien-021: server thu hồi mọi phiên; phiên cục bộ (nếu có) cũng xoá để vào lại /login. */
  async resetPassword(token: string, password: string, confirmPassword: string): Promise<void> {
    await firstValueFrom(
      this.http.post<void>(this.apiUrl('/api/auth/reset-password'), { token, password, confirmPassword }, this.noAuth),
    );
    if (this.hasSession()) {
      this.endSession(false);
      this.coordinator.broadcastLogout();
    }
  }
```
và thêm cuối file (ngoài class):
```ts

function isActivationChallenge(body: AuthTokens | ActivationChallenge): body is ActivationChallenge {
  return (body as ActivationChallenge).activationRequired === true;
}
```

`frontend/projects/shared/src/public-api.ts` — thêm sau dòng `export * from './lib/auth/provide-auth';`:
```ts
export * from './lib/auth/activation-store';
export * from './lib/account/login-notice';
```

- [ ] **Step 4: Chạy — kỳ vọng PASS**

Run: `cd frontend && npx ng test shared --watch=false`
Expected: PASS (`AuthService (web học viên)` 5 test; `AuthService` F00 vẫn xanh). `LoginPageComponent` spec F00 vẫn xanh vì `loginImpl` trả `Promise<void>` chỉ lệch kiểu ở spec — nếu `tsc` của vitest báo lỗi kiểu thì làm tiếp Step 5 trước khi chạy lại.

- [ ] **Step 5: Viết spec cho trang Đăng nhập ("Quên mật khẩu?" chỉ ở web học viên, mật khẩu tạm → /kich-hoat, câu báo mang sang)**

`frontend/projects/shared/src/lib/login/login-page.component.spec.ts` (thay toàn bộ):
```ts
import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { LoginNoticeStore } from '../account/login-notice';
import { AuthService } from '../auth/auth.service';
import { CLIENT_APP, ClientApp } from '../auth/client-app';
import { LoginResult } from '../auth/models';
import { LoginPageComponent } from './login-page.component';

describe('LoginPageComponent', () => {
  // Không dùng vi.fn() cho login: mock rejected của vi.fn() bị vitest 5 báo thành lỗi unhandled dù component đã bắt.
  let loginImpl: () => Promise<LoginResult> = () => Promise.resolve({ kind: 'session' });
  const auth = { login: (_email: string, _password: string) => loginImpl() };

  function setup(returnUrl: string | null, app: ClientApp = 'admin', before?: () => void) {
    TestBed.configureTestingModule({
      imports: [LoginPageComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: auth },
        { provide: CLIENT_APP, useValue: app },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(returnUrl ? { returnUrl } : {}) } } },
      ],
    });
    before?.();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(LoginPageComponent);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const button = (text: string) => [...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === text) as HTMLButtonElement | undefined;
    return { fixture, el, navigate, button };
  }

  beforeEach(() => { loginImpl = () => Promise.resolve({ kind: 'session' }); });

  // @BR-f00-codebase-004 @BR-f00-codebase-005 @ADR-f00-codebase-001
  it('hiển thị đúng thông báo API trả về khi bị từ chối', async () => {
    loginImpl = () => Promise.reject(new HttpErrorResponse({
      status: 403, error: { title: 'Tài khoản này không có quyền vào khu vực coach.' },
    }));
    const { fixture, el } = setup(null);
    await fixture.componentInstance.submit();
    fixture.detectChanges();
    expect(el.querySelector('[role="alert"]')?.textContent).toContain('Tài khoản này không có quyền vào khu vực coach.');
  });

  it('đăng nhập xong đi tới returnUrl an toàn', async () => {
    const { fixture, navigate } = setup('/trang-chu?tu=e2e');
    await fixture.componentInstance.submit();
    expect(navigate).toHaveBeenCalledWith('/trang-chu?tu=e2e');
  });

  it('mất kết nối thì báo tiếng Việt', async () => {
    loginImpl = () => Promise.reject(new HttpErrorResponse({ status: 0 }));
    const { fixture, el } = setup(null);
    await fixture.componentInstance.submit();
    fixture.detectChanges();
    expect(el.querySelector('[role="alert"]')?.textContent).toContain('Không kết nối được hệ thống. Vui lòng thử lại.');
  });

  // @ADR-f00-codebase-001
  it.each([500, 400])('lỗi %i thì hiện thông báo chung, không lộ tiêu đề từ API', async (status) => {
    loginImpl = () => Promise.reject(new HttpErrorResponse({ status, error: { title: 'chi tiết nội bộ' } }));
    const { fixture, el } = setup(null);
    await fixture.componentInstance.submit();
    fixture.detectChanges();
    const text = el.querySelector('[role="alert"]')?.textContent;
    expect(text).toContain('Đã có lỗi xảy ra. Vui lòng thử lại sau.');
    expect(text).not.toContain('chi tiết nội bộ');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-011: mật khẩu tạm còn hạn → bước đặt mật khẩu riêng, không đi returnUrl
  it('đăng nhập bằng mật khẩu tạm thì chuyển sang /kich-hoat', async () => {
    loginImpl = () => Promise.resolve({ kind: 'activation', token: 'act-1', expiresAt: '2026-11-25T03:00:00Z' });
    const { fixture, navigate } = setup('/hom-nay', 'client');
    await fixture.componentInstance.submit();
    expect(navigate).toHaveBeenCalledWith('/kich-hoat');
    expect(navigate).not.toHaveBeenCalledWith('/hom-nay');
  });

  it('web học viên có nút "Quên mật khẩu?" mở /quen-mat-khau', () => {
    const { button, navigate } = setup(null, 'client');
    button('Quên mật khẩu?')!.click();
    expect(navigate).toHaveBeenCalledWith('/quen-mat-khau');
  });

  it('web quản trị không có nút "Quên mật khẩu?"', () => {
    const { button } = setup(null, 'admin');
    expect(button('Quên mật khẩu?')).toBeUndefined();
  });

  it('hiện câu báo thành công mang từ màn Đặt lại mật khẩu', () => {
    const { el } = setup(null, 'client', () =>
      TestBed.inject(LoginNoticeStore).set({ tone: 'ok', text: 'Đã đặt lại mật khẩu. Đăng nhập bằng mật khẩu mới.' }));
    expect(el.querySelector('[role="status"]')?.textContent?.trim()).toBe('Đã đặt lại mật khẩu. Đăng nhập bằng mật khẩu mới.');
    expect(el.querySelector('[role="alert"]')).toBeNull();
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-005: mã kích hoạt bị huỷ → báo lỗi ở trang đăng nhập
  it('hiện câu lỗi mang từ màn Kích hoạt', () => {
    const { el } = setup(null, 'client', () => TestBed.inject(LoginNoticeStore).set({
      tone: 'error', text: 'Phiên kích hoạt đã hết hạn. Vui lòng đăng nhập lại bằng mật khẩu tạm mới nhất.' }));
    expect(el.querySelector('[role="alert"]')?.textContent?.trim())
      .toBe('Phiên kích hoạt đã hết hạn. Vui lòng đăng nhập lại bằng mật khẩu tạm mới nhất.');
  });
});
```

- [ ] **Step 6: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test shared --watch=false`
Expected: FAIL — 5 test mới đỏ (không có nút "Quên mật khẩu?", không chuyển `/kich-hoat`, không có `[role="status"]`…).

- [ ] **Step 7: `AuthFrameComponent`, `auth-form.scss`, trang Đăng nhập**

`frontend/projects/shared/src/lib/login/auth-frame.component.ts`:
```ts
import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Bìa thương hiệu + khung form của page "Main" (đăng nhập, kích hoạt, quên / đặt lại mật khẩu). */
@Component({
  selector: 'dbx-auth-frame',
  template: `
    <div class="page">
      <section class="cover" aria-label="Thương hiệu">
        <p class="domain">Unfiltered Coaching Team</p>
        <div class="cover-mid">
          <p class="sig" aria-hidden="true">duobexiuuu</p>
          <p class="headline cover-type">online<br>physique<br>coaching</p>
        </div>
        <div class="cover-foot">
          <span>@duobexiuuu/instagram</span>
          <span>Hotline (Zalo): 0366 868 658</span>
        </div>
      </section>
      <section class="panel" aria-label="Đăng nhập"><ng-content /></section>
    </div>
  `,
  styleUrl: './auth-frame.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthFrameComponent {}
```

`frontend/projects/shared/src/lib/login/auth-frame.component.scss`:
```scss
// Giá trị lấy nguyên văn từ prototype page "Main"
:host { display: block; }
.page {
  min-height: 100vh; box-sizing: border-box; background: var(--bg); color: var(--ink);
  font-family: var(--font-body); padding: 24px; display: flex; flex-wrap: wrap; gap: 24px; align-items: stretch;
}
.cover {
  flex: 1 1 520px; min-height: 420px; background: var(--coverBg); color: var(--coverInk); border-radius: 14px;
  padding: 40px 48px; box-sizing: border-box; display: flex; flex-direction: column; justify-content: space-between;
  position: relative; overflow: hidden;
}
.domain { margin: 0; font-family: var(--font-head); font-weight: 700; font-size: 20px; letter-spacing: .01em; }
.cover-mid { position: relative; padding: 24px 0; }
.sig {
  margin: 0 0 -.16em; padding: 0 .2em 0 0; display: inline-block; transform: rotate(-6deg); transform-origin: left bottom;
  font-family: var(--font-sig); font-size: clamp(58px, 7.6vw, 108px); color: var(--sig); line-height: 1;
}
.headline {
  margin: 0; font-family: var(--font-head); font-weight: 800; font-size: clamp(56px, 9.6vw, 148px);
  line-height: .84; letter-spacing: -.01em; text-transform: uppercase;
}
.cover-foot { display: flex; flex-wrap: wrap; gap: 8px 32px; font-size: 14px; color: var(--coverMuted); }
.panel {
  flex: 1 1 400px; max-width: 560px; display: flex; flex-direction: column; justify-content: center;
  padding: 32px clamp(8px, 4vw, 56px); box-sizing: border-box;
}
@media (prefers-reduced-motion: no-preference) {
  .sig { animation: sigIn .9s cubic-bezier(.2, .7, .2, 1) .2s both; }
}
@keyframes sigIn { from { clip-path: inset(0 100% 0 0); } to { clip-path: inset(0 0 0 0); } }
```

`frontend/projects/shared/src/lib/login/auth-form.scss` (style form dùng chung cho 4 màn, nguyên văn page "Main"):
```scss
// Giá trị lấy nguyên văn từ prototype page "Main" (isLogin / isActivate / isForgot)
.stack-24 { display: flex; flex-direction: column; gap: 24px; }
.stack-16 { display: flex; flex-direction: column; gap: 16px; }
.stack-8 { display: flex; flex-direction: column; gap: 8px; }
h1 { margin: 0; font-family: var(--font-head); font-weight: 800; font-size: 48px; line-height: 1.05; }
.lead { margin: 0; color: var(--muted); font-size: 15px; line-height: 1.6; }
.kicker { margin: 0; color: var(--primary); font-size: 14px; font-weight: 600; }
label { font-size: 14px; font-weight: 600; }
// min-height giữ hình học của web quản trị (không có nút "Quên mật khẩu?") bằng hàng có nút ở web học viên
.label-row { display: flex; justify-content: space-between; align-items: center; min-height: 26px; }
input {
  height: 52px; border-radius: 14px; border: 1px solid var(--field); background: var(--surface); color: var(--ink);
  padding: 0 16px; font: inherit; font-size: 16px;
}
.submit {
  height: 54px; border-radius: 999px; border: 0; background: var(--primary); color: var(--onPrimary);
  font-family: var(--font-head); font-weight: 700; font-size: 22px; cursor: pointer; margin-top: 8px;
}
.submit.tight { margin-top: 0; }
.submit:disabled { opacity: .7; cursor: progress; }
.link-btn { background: none; border: 0; padding: 4px 0; color: var(--primary); font: inherit; font-size: 14px; cursor: pointer; }
.back {
  align-self: flex-start; background: none; border: 0; padding: 8px 0; color: var(--primary); font: inherit; font-size: 15px; cursor: pointer;
}
.link-btn:hover, .back:hover { color: var(--linkHover); }
.error { margin: 0; padding: 16px; border-radius: 12px; background: var(--warnBg); color: var(--warn); font-size: 14px; line-height: 1.6; }
.ok { margin: 0; padding: 16px; border-radius: 12px; background: var(--okBg); color: var(--okInk); font-size: 14px; line-height: 1.6; }
.hint { margin: 0; padding: 16px; border-radius: 12px; background: var(--surface); color: var(--ink2); font-size: 14px; line-height: 1.6; }
@media (max-width: 879.98px) { h1 { font-size: 38px; } }
```

> Ghi chú: `h1` < 880px = 38px theo CLAUDE.md (prototype `Main` không có breakpoint riêng; trang đăng nhập F00 trước đây giữ 48px ở mọi độ rộng — nếu test so ảnh `màn đăng nhập` 390px vượt ngưỡng sau thay đổi này thì bỏ dòng `@media` cuối để giữ đúng prototype, prototype là nguồn chuẩn).

Xoá `frontend/projects/shared/src/lib/login/login-page.component.scss`:
```bash
git rm frontend/projects/shared/src/lib/login/login-page.component.scss
```

`frontend/projects/shared/src/lib/login/login-page.component.html` (thay toàn bộ):
```html
<dbx-auth-frame>
  <div class="stack-24">
    <div class="stack-8">
      <h1>Lift the fxxking weights.</h1>
      <p class="lead">Đăng nhập để xem buổi tập hôm nay, ghi nhật ký và nộp review tuần cho Coach Duobexiuuu nhaa.</p>
    </div>
    @if (notice(); as message) {
      <p class="ok" role="status">{{ message }}</p>
    }
    <form class="stack-16" (ngSubmit)="submit()">
      <div class="stack-8">
        <label for="em">Email</label>
        <input id="em" name="email" type="email" autocomplete="email" required [(ngModel)]="email">
      </div>
      <div class="stack-8">
        <div class="label-row">
          <label for="pw">Mật khẩu</label>
          @if (showForgot) {
            <button type="button" class="link-btn" (click)="goForgot()">Quên mật khẩu?</button>
          }
        </div>
        <input id="pw" name="password" type="password" autocomplete="current-password" required [(ngModel)]="password">
      </div>
      @if (error(); as message) {
        <p class="error" role="alert">{{ message }}</p>
      }
      <button type="submit" class="submit" [disabled]="submitting()">Đăng nhập</button>
    </form>
    <p class="hint">Lần đầu vào? Dùng mật khẩu tạm trong email mời của coach. Sau khi đăng nhập, bạn sẽ đặt mật khẩu riêng.</p>
  </div>
</dbx-auth-frame>
```

`frontend/projects/shared/src/lib/login/login-page.component.ts` (thay toàn bộ):
```ts
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LoginNoticeStore } from '../account/login-notice';
import { AuthService } from '../auth/auth.service';
import { CLIENT_APP } from '../auth/client-app';
import { safeReturnUrl } from '../auth/return-url';
import { AuthFrameComponent } from './auth-frame.component';

function messageFor(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return 'Không kết nối được hệ thống. Vui lòng thử lại.';
    const title = (error.error as { title?: unknown } | null)?.title;
    if ([401, 403, 429].includes(error.status) && typeof title === 'string') return title;
  }
  return 'Đã có lỗi xảy ra. Vui lòng thử lại sau.';
}

// @BR-f00-codebase-004 @BR-f00-codebase-005: hiển thị thông báo từ chối do API trả về (sai web → 403).
@Component({
  selector: 'dbx-login-page',
  imports: [FormsModule, AuthFrameComponent],
  templateUrl: './login-page.component.html',
  styleUrl: './auth-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPageComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly initial = inject(LoginNoticeStore).take();

  /** "Quên mật khẩu?" chỉ có ở web học viên (tech-design F01-P1 "Thiết kế giao diện (UI)"). */
  protected readonly showForgot = inject(CLIENT_APP) === 'client';

  email = '';
  password = '';
  readonly submitting = signal(false);
  readonly notice = signal<string | null>(this.initial?.tone === 'ok' ? this.initial.text : null);
  readonly error = signal<string | null>(this.initial?.tone === 'error' ? this.initial.text : null);

  async submit(): Promise<void> {
    if (this.submitting()) return;
    this.submitting.set(true);
    this.error.set(null);
    this.notice.set(null);
    try {
      const result = await this.auth.login(this.email, this.password);
      // @BR-f01-p1-tai-khoan-hoc-vien-011: mật khẩu tạm chỉ dẫn tới bước đặt mật khẩu riêng
      if (result.kind === 'activation') await this.router.navigateByUrl('/kich-hoat');
      else await this.router.navigateByUrl(safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl')));
    } catch (e) {
      this.error.set(messageFor(e));
    } finally {
      this.submitting.set(false);
    }
  }

  protected goForgot(): void {
    void this.router.navigateByUrl('/quen-mat-khau');
  }
}
```

`frontend/projects/shared/src/public-api.ts` — thêm sau dòng `export * from './lib/login/login-page.component';`:
```ts
export * from './lib/login/auth-frame.component';
```

- [ ] **Step 8: Chạy — kỳ vọng PASS**

Run: `cd frontend && npx ng test shared --watch=false`
Expected: PASS (`LoginPageComponent` 10 test).

- [ ] **Step 9: Viết spec cho 3 màn Kích hoạt, Quên mật khẩu, Đặt lại mật khẩu**

`frontend/projects/shared/src/lib/account/activate-page.component.spec.ts`:
```ts
import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ActivationStore, activationGuard } from '../auth/activation-store';
import { AuthService } from '../auth/auth.service';
import { ActivatePageComponent } from './activate-page.component';
import { LoginNoticeStore } from './login-notice';

describe('ActivatePageComponent', () => {
  let activateImpl: () => Promise<void> = () => Promise.resolve();
  let calls: unknown[][] = [];
  const auth = { activate: (...args: unknown[]) => { calls.push(args); return activateImpl(); } };

  beforeEach(() => { activateImpl = () => Promise.resolve(); calls = []; });

  async function setup() {
    TestBed.configureTestingModule({
      imports: [ActivatePageComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
    });
    TestBed.inject(ActivationStore).set({ token: 'act-1', expiresAt: '2026-11-25T03:00:00Z' });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(ActivatePageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const type = (id: string, value: string) => {
      const input = el.querySelector(`#${id}`) as HTMLInputElement;
      input.value = value;
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
    };
    return { fixture, el, navigate, type };
  }

  it('đúng câu chữ page Main (isActivate)', async () => {
    const { el } = await setup();
    expect(el.querySelector('.kicker')?.textContent?.trim()).toBe('Bước cuối để kích hoạt tài khoản');
    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Đặt mật khẩu riêng');
    expect(el.querySelector('label[for="np"]')?.textContent?.trim()).toBe('Mật khẩu mới');
    expect(el.querySelector('label[for="np2"]')?.textContent?.trim()).toBe('Nhập lại mật khẩu');
    expect(el.querySelector('button[type="submit"]')?.textContent?.trim()).toBe('Lưu mật khẩu và vào trang Hôm nay');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-014: tick khi đạt, đếm đúng từng ký tự
  it('checklist tick khi đủ 8 ký tự và hai lần nhập trùng nhau', async () => {
    const { el, type } = await setup();
    expect(el.querySelectorAll('dbx-password-checklist li.met')).toHaveLength(0);
    type('np', 'abcdefg');
    expect(el.querySelectorAll('dbx-password-checklist li.met')).toHaveLength(0);
    type('np', 'abcdefgh');
    type('np2', 'abcdefgh');
    expect(el.querySelectorAll('dbx-password-checklist li.met')).toHaveLength(2);
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-015 @BR-f01-p1-tai-khoan-hoc-vien-016: gửi nguyên văn, lưu xong vào trang Hôm nay
  it('lưu mật khẩu nguyên văn rồi vào /hom-nay', async () => {
    const { fixture, navigate, type } = await setup();
    type('np', ' Coach2026 ');
    type('np2', ' Coach2026 ');
    await fixture.componentInstance.submit();
    expect(calls[0]).toEqual(['act-1', ' Coach2026 ', ' Coach2026 ']);
    expect(navigate).toHaveBeenCalledWith('/hom-nay');
  });

  it('mật khẩu không hợp lệ thì hiện câu của API, ở lại trang', async () => {
    activateImpl = () => Promise.reject(new HttpErrorResponse({
      status: 400, error: { title: 'Mật khẩu cần ít nhất 8 ký tự.', code: 'auth.password_too_short' } }));
    const { fixture, el, navigate } = await setup();
    await fixture.componentInstance.submit();
    fixture.detectChanges();
    expect(el.querySelector('[role="alert"]')?.textContent?.trim()).toBe('Mật khẩu cần ít nhất 8 ký tự.');
    expect(navigate).not.toHaveBeenCalled();
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-005: coach gửi lại giữa chừng → mã kích hoạt bị huỷ → đăng nhập lại
  it('mã kích hoạt bị huỷ thì về /login kèm câu lỗi', async () => {
    const title = 'Phiên kích hoạt đã hết hạn. Vui lòng đăng nhập lại bằng mật khẩu tạm mới nhất.';
    activateImpl = () => Promise.reject(new HttpErrorResponse({ status: 401, error: { title, code: 'auth.activation_invalid' } }));
    const { fixture, navigate } = await setup();
    await fixture.componentInstance.submit();
    expect(navigate).toHaveBeenCalledWith('/login');
    expect(TestBed.inject(ActivationStore).current()).toBeNull();
    expect(TestBed.inject(LoginNoticeStore).take()).toEqual({ tone: 'error', text: title });
  });

  it('vào /kich-hoat mà không có mã kích hoạt thì về /login', () => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const result = TestBed.runInInjectionContext(() => activationGuard({} as never, {} as never));
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/login');
  });
});
```

`frontend/projects/shared/src/lib/account/forgot-password-page.component.spec.ts`:
```ts
import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthService } from '../auth/auth.service';
import { ForgotPasswordPageComponent } from './forgot-password-page.component';

const GENERIC = 'Nếu email này thuộc tài khoản học viên đã kích hoạt, link đặt lại mật khẩu đã được gửi tới email đó. Link có hiệu lực trong 30 phút. Không thấy email thì kiểm tra mục Spam.';

describe('ForgotPasswordPageComponent', () => {
  let forgotImpl: () => Promise<string> = () => Promise.resolve(GENERIC);
  let emails: string[] = [];
  const auth = { forgotPassword: (email: string) => { emails.push(email); return forgotImpl(); } };

  beforeEach(() => { forgotImpl = () => Promise.resolve(GENERIC); emails = []; });

  async function setup() {
    TestBed.configureTestingModule({
      imports: [ForgotPasswordPageComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
    });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(ForgotPasswordPageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    return { fixture, el: fixture.nativeElement as HTMLElement, navigate };
  }

  // @BR-f01-p1-tai-khoan-hoc-vien-023: luôn hiện câu chung của server trong hộp role=status
  it('gửi email đã nhập và hiện câu chung', async () => {
    const { fixture, el } = await setup();
    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Quên mật khẩu');
    const input = el.querySelector('#fe') as HTMLInputElement;
    input.value = 'QUAN@gmail.com';
    input.dispatchEvent(new Event('input'));
    await fixture.componentInstance.submit();
    fixture.detectChanges();

    expect(emails).toEqual(['QUAN@gmail.com']);
    expect(el.querySelector('[role="status"]')?.textContent?.trim()).toBe(GENERIC);
  });

  it('bị giới hạn tần suất thì hiện câu của API', async () => {
    forgotImpl = () => Promise.reject(new HttpErrorResponse({ status: 429, error: { title: 'Bạn thao tác quá nhanh. Vui lòng thử lại sau.' } }));
    const { fixture, el } = await setup();
    await fixture.componentInstance.submit();
    fixture.detectChanges();
    expect(el.querySelector('[role="alert"]')?.textContent?.trim()).toBe('Bạn thao tác quá nhanh. Vui lòng thử lại sau.');
    expect(el.querySelector('[role="status"]')).toBeNull();
  });

  it('"Quay lại đăng nhập" mở /login', async () => {
    const { el, navigate } = await setup();
    ([...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === 'Quay lại đăng nhập') as HTMLButtonElement).click();
    expect(navigate).toHaveBeenCalledWith('/login');
  });
});
```

`frontend/projects/shared/src/lib/account/reset-password-page.component.spec.ts`:
```ts
import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthService } from '../auth/auth.service';
import { LoginNoticeStore } from './login-notice';
import { RESET_DONE_NOTICE, RESET_LINK_INVALID, ResetPasswordPageComponent } from './reset-password-page.component';

describe('ResetPasswordPageComponent', () => {
  let resetImpl: () => Promise<void> = () => Promise.resolve();
  let calls: unknown[][] = [];
  const auth = { resetPassword: (...args: unknown[]) => { calls.push(args); return resetImpl(); } };

  beforeEach(() => { resetImpl = () => Promise.resolve(); calls = []; });

  async function setup(fragment: string | null) {
    TestBed.configureTestingModule({
      imports: [ResetPasswordPageComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: auth },
        { provide: ActivatedRoute, useValue: { snapshot: { fragment } } },
      ],
    });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(ResetPasswordPageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const type = (id: string, value: string) => {
      const input = el.querySelector(`#${id}`) as HTMLInputElement;
      input.value = value;
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
    };
    return { fixture, el, navigate, type };
  }

  // @ADR-f01-p1-tai-khoan-hoc-vien-010: token đọc từ phần sau '#t=' của link
  it('đọc token từ #t=, lưu xong về /login với câu báo thành công', async () => {
    const { fixture, el, navigate, type } = await setup('t=tok-1_Ab');
    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Đặt lại mật khẩu');
    expect(el.querySelector('.lead')?.textContent?.trim()).toBe('Link có hiệu lực 30 phút và chỉ dùng được một lần.');
    type('rp', 'Coach2026');
    type('rp2', 'Coach2026');
    await fixture.componentInstance.submit();

    expect(calls[0]).toEqual(['tok-1_Ab', 'Coach2026', 'Coach2026']);
    expect(TestBed.inject(LoginNoticeStore).take()).toEqual({ tone: 'ok', text: RESET_DONE_NOTICE });
    expect(navigate).toHaveBeenCalledWith('/login');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-019 @BR-f01-p1-tai-khoan-hoc-vien-020: link hỏng → hộp cảnh báo + "Xin link mới"
  it('link hết hạn hoặc đã dùng thì ẩn form, hiện câu của API và nút "Xin link mới"', async () => {
    resetImpl = () => Promise.reject(new HttpErrorResponse({
      status: 400, error: { title: RESET_LINK_INVALID, code: 'auth.reset_link_invalid' } }));
    const { fixture, el, navigate } = await setup('t=tok-old');
    await fixture.componentInstance.submit();
    fixture.detectChanges();

    expect(el.querySelector('[role="alert"]')?.textContent?.trim()).toBe(RESET_LINK_INVALID);
    expect(el.querySelector('form')).toBeNull();
    ([...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === 'Xin link mới') as HTMLButtonElement).click();
    expect(navigate).toHaveBeenCalledWith('/quen-mat-khau');
  });

  it('link không có token thì báo link hỏng ngay', async () => {
    const { el } = await setup(null);
    expect(el.querySelector('[role="alert"]')?.textContent?.trim()).toBe(RESET_LINK_INVALID);
    expect(el.querySelector('form')).toBeNull();
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-014: lỗi mật khẩu không làm mất link — form vẫn còn
  it('hai lần nhập không khớp thì hiện câu của API, form vẫn còn', async () => {
    resetImpl = () => Promise.reject(new HttpErrorResponse({
      status: 400, error: { title: 'Hai lần nhập mật khẩu không khớp.', code: 'auth.password_mismatch' } }));
    const { fixture, el } = await setup('t=tok-1');
    await fixture.componentInstance.submit();
    fixture.detectChanges();
    expect(el.querySelector('[role="alert"]')?.textContent?.trim()).toBe('Hai lần nhập mật khẩu không khớp.');
    expect(el.querySelector('form')).not.toBeNull();
  });
});
```

- [ ] **Step 10: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test shared --watch=false`
Expected: FAIL — `Cannot find module './activate-page.component'`, `'./forgot-password-page.component'`, `'./reset-password-page.component'`.

- [ ] **Step 11: Lỗi dùng chung, checklist mật khẩu, 3 màn, route web học viên**

`frontend/projects/shared/src/lib/account/account-errors.ts`:
```ts
import { HttpErrorResponse } from '@angular/common/http';

export const CONNECTION_ERROR = 'Không kết nối được hệ thống. Vui lòng thử lại.';
export const GENERIC_ERROR = 'Đã có lỗi xảy ra. Vui lòng thử lại sau.';

/** Mã lỗi nghiệp vụ trong ProblemDetails (`code`). */
export function problemCode(error: unknown): string | null {
  const code = error instanceof HttpErrorResponse ? (error.error as { code?: unknown } | null)?.code : null;
  return typeof code === 'string' ? code : null;
}

/** Màn tài khoản: lỗi nghiệp vụ (400/401/429) hiện đúng câu của API; mạng → câu kết nối; còn lại câu chung. */
export function accountErrorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return CONNECTION_ERROR;
    const title = (error.error as { title?: unknown } | null)?.title;
    if ([400, 401, 429].includes(error.status) && typeof title === 'string') return title;
  }
  return GENERIC_ERROR;
}
```

`frontend/projects/shared/src/lib/account/password-checklist.component.ts`:
```ts
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Checklist dưới 2 ô mật khẩu (page "Main", isActivate): tick màu --ok khi đạt. */
@Component({
  selector: 'dbx-password-checklist',
  template: `
    <ul>
      <li [class.met]="longEnough()">
        <svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
             stroke-linecap="round" stroke-linejoin="round"><path d="M5 12l5 5 9-10"></path></svg>
        <span class="sr-only">{{ longEnough() ? 'Đã đạt: ' : 'Chưa đạt: ' }}</span>Ít nhất 8 ký tự
      </li>
      <li [class.met]="matches()">
        <svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
             stroke-linecap="round" stroke-linejoin="round"><path d="M5 12l5 5 9-10"></path></svg>
        <span class="sr-only">{{ matches() ? 'Đã đạt: ' : 'Chưa đạt: ' }}</span>Hai lần nhập trùng nhau
      </li>
    </ul>
  `,
  styles: `
    :host { display: block; }
    ul { margin: 0; padding: 0; list-style: none; display: flex; flex-direction: column; gap: 6px; font-size: 14px; color: var(--ink2); }
    li { display: flex; gap: 8px; align-items: center; }
    svg { color: var(--faint); flex: none; }
    li.met svg { color: var(--ok); }
    .sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PasswordChecklistComponent {
  readonly password = input.required<string>();
  readonly confirm = input.required<string>();
  // @BR-f01-p1-tai-khoan-hoc-vien-013 @BR-f01-p1-tai-khoan-hoc-vien-014: đếm đúng từng ký tự (không trim), so khớp nguyên văn
  protected readonly longEnough = computed(() => this.password().length >= 8);
  protected readonly matches = computed(() => this.password().length > 0 && this.password() === this.confirm());
}
```

`frontend/projects/shared/src/lib/account/activate-page.component.ts`:
```ts
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ActivationStore } from '../auth/activation-store';
import { AuthService } from '../auth/auth.service';
import { AuthFrameComponent } from '../login/auth-frame.component';
import { accountErrorMessage, problemCode } from './account-errors';
import { LoginNoticeStore } from './login-notice';
import { PasswordChecklistComponent } from './password-checklist.component';

/** Màn "Kích hoạt" — page "Main" state isActivate. */
@Component({
  selector: 'dbx-activate-page',
  imports: [FormsModule, AuthFrameComponent, PasswordChecklistComponent],
  template: `
    <dbx-auth-frame>
      <div class="stack-24">
        <div class="stack-8">
          <p class="kicker">Bước cuối để kích hoạt tài khoản</p>
          <h1>Đặt mật khẩu riêng</h1>
          <p class="lead">Mật khẩu tạm sẽ hết hiệu lực ngay khi bạn lưu. Lần sau đăng nhập bằng email và mật khẩu này.</p>
        </div>
        <form class="stack-16" (ngSubmit)="submit()">
          <div class="stack-8">
            <label for="np">Mật khẩu mới</label>
            <input id="np" name="password" type="password" autocomplete="new-password" required
                   [ngModel]="password()" (ngModelChange)="password.set($event)">
          </div>
          <div class="stack-8">
            <label for="np2">Nhập lại mật khẩu</label>
            <input id="np2" name="confirm" type="password" autocomplete="new-password" required
                   [ngModel]="confirm()" (ngModelChange)="confirm.set($event)">
          </div>
          <dbx-password-checklist [password]="password()" [confirm]="confirm()" />
          @if (error(); as message) {
            <p class="error" role="alert">{{ message }}</p>
          }
          <button type="submit" class="submit" [disabled]="submitting()">Lưu mật khẩu và vào trang Hôm nay</button>
        </form>
      </div>
    </dbx-auth-frame>
  `,
  styleUrl: '../login/auth-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivatePageComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly activation = inject(ActivationStore);
  private readonly notices = inject(LoginNoticeStore);

  readonly password = signal('');
  readonly confirm = signal('');
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  // @BR-f01-p1-tai-khoan-hoc-vien-016: lưu xong → tài khoản Đã kích hoạt, vào thẳng trang Hôm nay
  async submit(): Promise<void> {
    const ticket = this.activation.current();
    if (!ticket) {
      await this.router.navigateByUrl('/login');
      return;
    }
    if (this.submitting()) return;
    this.submitting.set(true);
    this.error.set(null);
    try {
      await this.auth.activate(ticket.token, this.password(), this.confirm());
      await this.router.navigateByUrl('/hom-nay');
    } catch (e) {
      // @BR-f01-p1-tai-khoan-hoc-vien-005: mã kích hoạt bị huỷ (coach gửi lại / sửa email) → đăng nhập lại bằng mật khẩu tạm mới
      if (problemCode(e) === 'auth.activation_invalid') {
        this.activation.clear();
        this.notices.set({ tone: 'error', text: accountErrorMessage(e) });
        await this.router.navigateByUrl('/login');
      } else {
        this.error.set(accountErrorMessage(e));
      }
    } finally {
      this.submitting.set(false);
    }
  }
}
```

`frontend/projects/shared/src/lib/account/forgot-password-page.component.ts`:
```ts
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { AuthFrameComponent } from '../login/auth-frame.component';
import { accountErrorMessage } from './account-errors';

/** Màn "Quên mật khẩu" — page "Main" state isForgot. */
@Component({
  selector: 'dbx-forgot-password-page',
  imports: [FormsModule, AuthFrameComponent],
  template: `
    <dbx-auth-frame>
      <div class="stack-24">
        <div class="stack-8">
          <h1>Quên mật khẩu</h1>
          <p class="lead">Nhập email bạn dùng để đăng nhập. Hệ thống gửi link đặt lại mật khẩu vào email đó.</p>
        </div>
        <form class="stack-16" (ngSubmit)="submit()">
          <div class="stack-8">
            <label for="fe">Email</label>
            <input id="fe" name="email" type="email" autocomplete="email" required
                   [ngModel]="email()" (ngModelChange)="email.set($event)">
          </div>
          @if (error(); as message) {
            <p class="error" role="alert">{{ message }}</p>
          }
          <button type="submit" class="submit tight" [disabled]="submitting()">Gửi link đặt lại</button>
        </form>
        @if (sent(); as message) {
          <p class="ok" role="status">{{ message }}</p>
        }
        <button type="button" class="back" (click)="goLogin()">Quay lại đăng nhập</button>
      </div>
    </dbx-auth-frame>
  `,
  styleUrl: '../login/auth-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForgotPasswordPageComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly email = signal('');
  readonly sent = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  // @BR-f01-p1-tai-khoan-hoc-vien-023: luôn hiện câu chung server trả về, không phân biệt email có tài khoản hay không
  async submit(): Promise<void> {
    if (this.submitting()) return;
    this.submitting.set(true);
    this.error.set(null);
    this.sent.set(null);
    try {
      this.sent.set(await this.auth.forgotPassword(this.email()));
    } catch (e) {
      this.error.set(accountErrorMessage(e));
    } finally {
      this.submitting.set(false);
    }
  }

  protected goLogin(): void {
    void this.router.navigateByUrl('/login');
  }
}
```

`frontend/projects/shared/src/lib/account/reset-password-page.component.ts`:
```ts
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { AuthFrameComponent } from '../login/auth-frame.component';
import { accountErrorMessage, problemCode } from './account-errors';
import { LoginNoticeStore } from './login-notice';
import { PasswordChecklistComponent } from './password-checklist.component';

export const RESET_DONE_NOTICE = 'Đã đặt lại mật khẩu. Đăng nhập bằng mật khẩu mới.';
export const RESET_LINK_INVALID = 'Link đặt lại đã hết hạn hoặc đã được dùng. Hãy xin link mới.';

/**
 * Màn "Đặt lại mật khẩu" (suy ra — dùng lại bố cục Kích hoạt của page "Main").
 * @ADR-f01-p1-tai-khoan-hoc-vien-010: link `<ClientBaseUrl>/dat-lai-mat-khau#t=<token>` — token sau '#' không lên server,
 * không lọt vào Referer hay log.
 */
@Component({
  selector: 'dbx-reset-password-page',
  imports: [FormsModule, AuthFrameComponent, PasswordChecklistComponent],
  template: `
    <dbx-auth-frame>
      <div class="stack-24">
        <div class="stack-8">
          <h1>Đặt lại mật khẩu</h1>
          <p class="lead">Link có hiệu lực 30 phút và chỉ dùng được một lần.</p>
        </div>
        @if (linkError(); as message) {
          <p class="error" role="alert">{{ message }}</p>
          <button type="button" class="back" (click)="requestNewLink()">Xin link mới</button>
        } @else {
          <form class="stack-16" (ngSubmit)="submit()">
            <div class="stack-8">
              <label for="rp">Mật khẩu mới</label>
              <input id="rp" name="password" type="password" autocomplete="new-password" required
                     [ngModel]="password()" (ngModelChange)="password.set($event)">
            </div>
            <div class="stack-8">
              <label for="rp2">Nhập lại mật khẩu</label>
              <input id="rp2" name="confirm" type="password" autocomplete="new-password" required
                     [ngModel]="confirm()" (ngModelChange)="confirm.set($event)">
            </div>
            <dbx-password-checklist [password]="password()" [confirm]="confirm()" />
            @if (error(); as message) {
              <p class="error" role="alert">{{ message }}</p>
            }
            <button type="submit" class="submit" [disabled]="submitting()">Lưu mật khẩu mới</button>
          </form>
        }
      </div>
    </dbx-auth-frame>
  `,
  styleUrl: '../login/auth-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResetPasswordPageComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notices = inject(LoginNoticeStore);
  private readonly token = new URLSearchParams(inject(ActivatedRoute).snapshot.fragment ?? '').get('t');

  readonly password = signal('');
  readonly confirm = signal('');
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly linkError = signal<string | null>(this.token ? null : RESET_LINK_INVALID);

  async submit(): Promise<void> {
    if (!this.token || this.submitting()) return;
    this.submitting.set(true);
    this.error.set(null);
    try {
      await this.auth.resetPassword(this.token, this.password(), this.confirm());
      this.notices.set({ tone: 'ok', text: RESET_DONE_NOTICE });
      await this.router.navigateByUrl('/login');
    } catch (e) {
      // @BR-f01-p1-tai-khoan-hoc-vien-018 @BR-f01-p1-tai-khoan-hoc-vien-019: link hỏng → xin link mới; lỗi mật khẩu giữ link
      if (problemCode(e) === 'auth.reset_link_invalid') this.linkError.set(accountErrorMessage(e));
      else this.error.set(accountErrorMessage(e));
    } finally {
      this.submitting.set(false);
    }
  }

  protected requestNewLink(): void {
    void this.router.navigateByUrl('/quen-mat-khau');
  }
}
```

`frontend/projects/shared/src/public-api.ts` — thêm cuối file:
```ts
export * from './lib/account/account-errors';
export * from './lib/account/password-checklist.component';
export * from './lib/account/activate-page.component';
export * from './lib/account/forgot-password-page.component';
export * from './lib/account/reset-password-page.component';
```

`frontend/projects/client/src/app/app.routes.ts` (thay toàn bộ):
```ts
import { Routes } from '@angular/router';
import {
  ActivatePageComponent, ForgotPasswordPageComponent, LoginPageComponent, ResetPasswordPageComponent,
  activationGuard, authGuard, guestGuard,
} from '@coaching/shared';
import { ClientHomeComponent } from './home/client-home.component';
import { ClientShellComponent } from './shell/client-shell.component';

// @ADR-f01-p1-tai-khoan-hoc-vien-010: route tiếng Việt; mã kích hoạt chỉ trong bộ nhớ, token đặt lại nằm sau '#t='.
// /dat-lai-mat-khau không dùng guestGuard: học viên đang đăng nhập trên máy này vẫn đặt lại được (phiên bị xoá sau khi lưu).
export const routes: Routes = [
  { path: 'login', component: LoginPageComponent, canActivate: [guestGuard], title: 'Đăng nhập — Duobexiuuu Coaching' },
  { path: 'kich-hoat', component: ActivatePageComponent, canActivate: [guestGuard, activationGuard], title: 'Kích hoạt tài khoản — Duobexiuuu Coaching' },
  { path: 'quen-mat-khau', component: ForgotPasswordPageComponent, canActivate: [guestGuard], title: 'Quên mật khẩu — Duobexiuuu Coaching' },
  { path: 'dat-lai-mat-khau', component: ResetPasswordPageComponent, title: 'Đặt lại mật khẩu — Duobexiuuu Coaching' },
  {
    path: '',
    component: ClientShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'hom-nay' },
      { path: 'hom-nay', component: ClientHomeComponent, title: 'Hôm nay — Duobexiuuu Coaching' },
    ],
  },
  { path: '**', redirectTo: '' },
];
```

- [ ] **Step 12: Chạy — kỳ vọng PASS**

Run: `cd frontend && npm run test:ci`
Expected: PASS (`ActivatePageComponent` 6 test, `ForgotPasswordPageComponent` 3 test, `ResetPasswordPageComponent` 4 test, `LoginPageComponent` 10 test, `AuthService (web học viên)` 5 test; admin/client không đổi).
Run: `cd frontend && npx ng build client && npx ng build admin`
Expected: build thành công cả hai.
Kiểm tay (`npx ng serve client` + API local Task 1–7, `Email:Provider=Capture`): đăng nhập bằng mật khẩu tạm đọc ở `GET /api/test/emails?to=` → `/kich-hoat`; tải lại `/kich-hoat` → về `/login`; lưu "Coach2026" → `/hom-nay`; "Quên mật khẩu?" → `/quen-mat-khau` → hộp xanh câu chung; mở link đặt lại → lưu → `/login` hộp xanh "Đã đặt lại mật khẩu. Đăng nhập bằng mật khẩu mới."; mở lại link cũ → hộp `--warnBg` + "Xin link mới". So với page `Main` (`docs/duobexiuuu-coaching-design.html#Main&frame`: login → bấm "Quên mật khẩu?" → isForgot; submit login → isActivate) ở 390px và 1280px: ô 52px radius 14, nút chính 54px Barlow 700 22px, "Quên mật khẩu?" 14px `--primary` cùng hàng nhãn, checklist tick 16px gap 8, hộp `role=status` nền `--okBg` chữ `--okInk` padding 16 radius 12, "Quay lại đăng nhập" 15px. Web quản trị `/login` không có "Quên mật khẩu?", hình học không đổi.

- [ ] **Step 13: Commit**
```bash
git add frontend/projects/shared frontend/projects/client
git commit -F - <<'EOF'
feat(client): activation, forgot and reset password screens; login routes temp password to activation

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 9: [UI] Web admin — Tạo hồ sơ "Lưu và gửi thông tin đăng nhập", Hồ sơ (card "Tài khoản đăng nhập" + nút gửi), Sửa hồ sơ (email)

Màn hình (tech-design §3 "Thiết kế giao diện (UI)"): **Tạo hồ sơ** (`/hoc-vien/moi`, page `AdTaoHoSo`: nút chính "Lưu và gửi thông tin đăng nhập", "Chỉ lưu hồ sơ", câu báo của prototype, aside "Trạng thái tài khoản" `stText`/`stNote` + card "Học viên sẽ nhận gì"), **Hồ sơ** (`/hoc-vien/:id`, page `AdHoSo`: "Kích hoạt dd/MM/yyyy" sau chip coaching, nút phụ theo trạng thái tài khoản, card "Tài khoản đăng nhập" kiểu `isTaiKhoan`), **Sửa hồ sơ** (`/hoc-vien/:id/sua`: email sửa được khi Chờ kích hoạt, `PUT /email` trước rồi `PUT` hồ sơ, ghi chú khi đã gửi thông tin).

**Files:**
- Create: `frontend/projects/admin/src/app/students/account-card.component.ts`, `account-card.component.spec.ts`
- Modify: `frontend/projects/admin/src/app/students/student.models.ts`, `students.api.ts`, `format.ts`, `format.spec.ts`
- Modify: `frontend/projects/admin/src/app/students/student-profile.component.ts`, `student-profile.component.html`, `student-profile.component.scss`, `student-profile.component.spec.ts`
- Modify: `frontend/projects/admin/src/app/students/student-form.component.ts`, `student-form.component.html`, `student-form.component.scss`, `student-form.component.spec.ts`

**Interfaces:**
- Consumes: Task 1 — `StudentDetailDto.account` = `AccountInfoDto(State, CredentialsSentAt, TempPasswordExpiresAt, ActivatedAt, History[])`, `History` mới nhất trước, `AccountEventDto(Type, At, FromEmail, ToEmail)` (enum trả dạng tên); Task 4 — `POST /api/admin/students/{id}/credentials` (`{}` → 200 `StudentDetailDto`; 409 `account.already_activated`, 400 `account.required_fields_missing`, 503 `account.email_send_failed` / `account.client_url_missing`); Task 6 — `PUT /api/admin/students/{id}/email` (`{ email, version }` → 200; 409 `account.email_locked` / `student.email_taken` / `student.version_conflict`, 400 email sai, 503); Task 7 — `POST /api/admin/students/{id}/password-reset-link` (`{}` → 200; 409 `account.not_activated`, 503). F02 — `StudentsApi`, `StudentFormComponent`, `StudentProfileComponent`, `format.ts`.
- Produces: `AccountState`, `AccountEventType`, `AccountEvent`, `AccountInfo`, `StudentDetail.account`; `StudentsApi.sendCredentials(id)`, `changeEmail(id, email, version)`, `sendResetLink(id)` → `Observable<StudentDetail>`; `formatVnFullDate(iso)` ("dd/MM/yyyy" giờ VN), `formatVnTime(iso)` ("HH:mm" giờ VN); `<app-account-card [account]>`, `accountStateText(account)`, `accountEventText(event)`; `SENT_MESSAGE`, `SAVED_NOT_SENT_MESSAGE`, `EMAIL_CHANGE_NOTE` (student-form). Trang hồ sơ đọc `history.state.notice` (chuỗi) khi mở.

**Owns:** —

- [ ] **Step 1: Viết spec cho định dạng ngày và card "Tài khoản đăng nhập"**

`frontend/projects/admin/src/app/students/format.spec.ts` — sửa import và thêm test:
```ts
import { formatDateOnly, formatNumber, formatVnDay, formatVnDayTime, formatVnFullDate, formatVnTime } from './format';
```
```ts
  it('formats full dates and times in Vietnam time', () => {
    expect(formatVnFullDate('2026-10-13T01:12:00+00:00')).toBe('13/10/2026');
    expect(formatVnFullDate('2026-12-31T18:00:00Z')).toBe('01/01/2027');
    expect(formatVnTime('2026-11-25T02:05:00+00:00')).toBe('09:05');
  });
```

`frontend/projects/admin/src/app/students/account-card.component.spec.ts`:
```ts
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { AccountCardComponent, accountEventText, accountStateText } from './account-card.component';
import { AccountInfo } from './student.models';

const base: AccountInfo = { state: 'NotSent', credentialsSentAt: null, tempPasswordExpiresAt: null, activatedAt: null, history: [] };

describe('account card', () => {
  // @BR-f01-p1-tai-khoan-hoc-vien-026: câu trạng thái theo living docs
  it('states the account the way the spec says', () => {
    expect(accountStateText(base)).toBe('Chưa gửi thông tin đăng nhập');
    expect(accountStateText({ ...base, state: 'Sent', credentialsSentAt: '2026-11-01T02:00:00+00:00', tempPasswordExpiresAt: '2026-11-04T02:00:00+00:00' }))
      .toBe('Đã gửi lúc 01/11 09:00, mật khẩu tạm còn hạn đến 04/11 09:00');
    expect(accountStateText({ ...base, state: 'TempPasswordExpired', credentialsSentAt: '2026-11-01T02:00:00+00:00' })).toBe('Mật khẩu tạm đã hết hạn');
    expect(accountStateText({ ...base, state: 'Activated', activatedAt: '2026-10-13T01:12:00+00:00' })).toBe('Đã kích hoạt ngày 13/10/2026');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-027: một câu cho mỗi loại sự kiện (tech-design / prototype AdHoSo)
  it('writes one sentence per history event type', () => {
    const at = '2026-10-12T02:05:00+00:00';
    expect(accountEventText({ type: 'CredentialsSent', at, fromEmail: null, toEmail: null })).toBe('Gửi thông tin đăng nhập lần đầu.');
    expect(accountEventText({ type: 'CredentialsResent', at, fromEmail: null, toEmail: null })).toBe('Gửi lại thông tin đăng nhập. Mật khẩu tạm cũ bị huỷ.');
    expect(accountEventText({ type: 'EmailChanged', at, fromEmail: 'quan@gmial.com', toEmail: 'quan@gmail.com' })).toBe('Sửa email từ quan@gmial.com sang quan@gmail.com.');
    expect(accountEventText({ type: 'ResetLinkSent', at, fromEmail: null, toEmail: null })).toBe('Gửi link đặt lại mật khẩu.');
    expect(accountEventText({ type: 'Activated', at, fromEmail: null, toEmail: null })).toBe('Học viên đặt mật khẩu riêng. Tài khoản Đã kích hoạt.');
  });

  it('renders the state line, the history (newest first as given) and the closing note', () => {
    const fixture = TestBed.createComponent(AccountCardComponent);
    fixture.componentRef.setInput('account', {
      ...base, state: 'Activated', activatedAt: '2026-10-13T01:12:00+00:00',
      history: [
        { type: 'Activated', at: '2026-10-13T01:12:00+00:00', fromEmail: null, toEmail: null },
        { type: 'CredentialsSent', at: '2026-10-12T02:05:00+00:00', fromEmail: null, toEmail: null },
      ],
    } satisfies AccountInfo);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.querySelector('h2')?.textContent?.trim()).toBe('Tài khoản đăng nhập');
    expect(el.querySelector('.state')?.textContent?.trim()).toBe('Đã kích hoạt ngày 13/10/2026');
    const rows = [...el.querySelectorAll('ol li')].map((li) => li.textContent?.replace(/\s+/g, ' ').trim());
    expect(rows).toEqual(['13/10 08:12 Học viên đặt mật khẩu riêng. Tài khoản Đã kích hoạt.', '12/10 09:05 Gửi thông tin đăng nhập lần đầu.']);
    expect(el.querySelector('.foot')?.textContent?.trim()).toBe('Mật khẩu riêng do học viên quản lý. Coach chỉ gửi được link đặt lại mật khẩu qua email.');
  });
});
```

- [ ] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: FAIL — `Cannot find module './account-card.component'`, `formatVnFullDate is not exported`.

- [ ] **Step 3: Model tài khoản, `format.ts`, `AccountCardComponent`**

`frontend/projects/admin/src/app/students/student.models.ts` — thêm trước `/** GET /api/admin/students/{id}. */`:
```ts
/** F01-P1: trạng thái tài khoản tính ở server theo giờ hiện tại (@BR-f01-p1-tai-khoan-hoc-vien-026). */
export type AccountState = 'NotSent' | 'Sent' | 'TempPasswordExpired' | 'Activated';
export type AccountEventType = 'CredentialsSent' | 'CredentialsResent' | 'EmailChanged' | 'ResetLinkSent' | 'Activated';

export interface AccountEvent {
  type: AccountEventType;
  at: string;
  fromEmail: string | null;
  toEmail: string | null;
}

/** `account` của StudentDetailDto — không có mật khẩu tạm hay token (@BR-f01-p1-tai-khoan-hoc-vien-025). History mới nhất trước. */
export interface AccountInfo {
  state: AccountState;
  credentialsSentAt: string | null;
  tempPasswordExpiresAt: string | null;
  activatedAt: string | null;
  history: AccountEvent[];
}

```
và trong `interface StudentDetail` thêm dòng cuối (sau `statusHistory: StatusChange[];`):
```ts
  account: AccountInfo;
```

`frontend/projects/admin/src/app/students/format.ts` — thay hàm `vietnamParts` và thêm 2 hàm:
```ts
function vietnamParts(iso: string) {
  const d = new Date(Date.parse(iso) + VIETNAM_OFFSET_MS);
  return {
    day: pad(d.getUTCDate()), month: pad(d.getUTCMonth() + 1), year: String(d.getUTCFullYear()),
    hour: pad(d.getUTCHours()), minute: pad(d.getUTCMinutes()),
  };
}

/** "dd/MM/yyyy" theo giờ Việt Nam (vd. "Kích hoạt 13/10/2026"). */
export function formatVnFullDate(iso: string): string {
  const p = vietnamParts(iso);
  return `${p.day}/${p.month}/${p.year}`;
}

/** "HH:mm" theo giờ Việt Nam (vd. stNote "Gửi đến … lúc 09:05."). */
export function formatVnTime(iso: string): string {
  const p = vietnamParts(iso);
  return `${p.hour}:${p.minute}`;
}
```

`frontend/projects/admin/src/app/students/account-card.component.ts`:
```ts
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { formatVnDayTime, formatVnFullDate } from './format';
import { AccountEvent, AccountInfo } from './student.models';

/** Dòng trạng thái — câu theo living docs @BR-f01-p1-tai-khoan-hoc-vien-026. */
export function accountStateText(account: AccountInfo): string {
  switch (account.state) {
    case 'NotSent':
      return 'Chưa gửi thông tin đăng nhập';
    case 'Sent':
      return `Đã gửi lúc ${formatVnDayTime(account.credentialsSentAt!)}, mật khẩu tạm còn hạn đến ${formatVnDayTime(account.tempPasswordExpiresAt!)}`;
    case 'TempPasswordExpired':
      return 'Mật khẩu tạm đã hết hạn';
    case 'Activated':
      return `Đã kích hoạt ngày ${formatVnFullDate(account.activatedAt!)}`;
  }
}

/** Câu của từng dòng lịch sử (@BR-f01-p1-tai-khoan-hoc-vien-027), nguyên văn tech-design / prototype AdHoSo. */
export function accountEventText(event: AccountEvent): string {
  switch (event.type) {
    case 'CredentialsSent':
      return 'Gửi thông tin đăng nhập lần đầu.';
    case 'CredentialsResent':
      return 'Gửi lại thông tin đăng nhập. Mật khẩu tạm cũ bị huỷ.';
    case 'EmailChanged':
      return `Sửa email từ ${event.fromEmail} sang ${event.toEmail}.`;
    case 'ResetLinkSent':
      return 'Gửi link đặt lại mật khẩu.';
    case 'Activated':
      return 'Học viên đặt mật khẩu riêng. Tài khoản Đã kích hoạt.';
  }
}

/** Card "Tài khoản đăng nhập" — style nguyên văn tab `isTaiKhoan` của page AdHoSo. */
@Component({
  selector: 'app-account-card',
  template: `
    <section class="card" aria-labelledby="account-title">
      <h2 id="account-title">Tài khoản đăng nhập</h2>
      <p class="state">{{ stateText() }}</p>
      @if (account().history.length > 0) {
        <ol>
          @for (e of account().history; track $index) {
            <li><strong>{{ dayTime(e.at) }}</strong> {{ eventText(e) }}</li>
          }
        </ol>
      }
      <p class="foot">Mật khẩu riêng do học viên quản lý. Coach chỉ gửi được link đặt lại mật khẩu qua email.</p>
    </section>
  `,
  styles: `
    :host { display: block; }
    .card { background: var(--surface); border: 1px solid var(--line); border-radius: 12px; padding: 16px 18px; display: flex;
      flex-direction: column; gap: 12px; }
    h2 { margin: 0; font-family: var(--font-head); font-weight: 700; font-size: 24px; }
    .state { margin: 0; font-size: 14px; font-weight: 600; color: var(--ink2); }
    ol { margin: 0; padding: 0 0 0 18px; border-left: 2px solid var(--soft); list-style: none; display: flex; flex-direction: column;
      gap: 10px; font-size: 14px; }
    .foot { margin: 0; font-size: 13px; color: var(--muted); }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountCardComponent {
  readonly account = input.required<AccountInfo>();
  protected readonly stateText = computed(() => accountStateText(this.account()));
  protected readonly eventText = accountEventText;
  protected readonly dayTime = formatVnDayTime;
}
```

- [ ] **Step 4: Chạy — kỳ vọng PASS (card + format), FAIL biên dịch ở fixture cũ**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: `account card` 3 test và `format` 3 test PASS; `student-form.component.spec.ts` / `student-profile.component.spec.ts` báo lỗi kiểu thiếu `account` trong fixture `detail()` — sửa ở Step 5 và Step 9.

- [ ] **Step 5: Viết spec cho Hồ sơ (nút gửi theo trạng thái, card, "Kích hoạt dd/MM/yyyy", câu báo)**

`frontend/projects/admin/src/app/students/student-profile.component.spec.ts`:
- sửa import model:
```ts
import { AccountInfo, StudentDetail } from './student.models';
```
- thêm sau `const settle = …`:
```ts
const notSent: AccountInfo = { state: 'NotSent', credentialsSentAt: null, tempPasswordExpiresAt: null, activatedAt: null, history: [] };
const sent: AccountInfo = {
  state: 'Sent', credentialsSentAt: '2026-11-25T02:00:00+00:00', tempPasswordExpiresAt: '2026-11-28T02:00:00+00:00', activatedAt: null,
  history: [{ type: 'CredentialsSent', at: '2026-11-25T02:00:00+00:00', fromEmail: null, toEmail: null }],
};
const activated: AccountInfo = {
  state: 'Activated', credentialsSentAt: '2026-10-12T02:05:00+00:00', tempPasswordExpiresAt: null, activatedAt: '2026-10-13T01:12:00+00:00',
  history: [
    { type: 'Activated', at: '2026-10-13T01:12:00+00:00', fromEmail: null, toEmail: null },
    { type: 'EmailChanged', at: '2026-10-12T14:30:00+00:00', fromEmail: 'lan@gmial.com', toEmail: 'lan@gmail.com' },
    { type: 'CredentialsSent', at: '2026-10-12T02:05:00+00:00', fromEmail: null, toEmail: null },
  ],
};
```
- trong `detail()`, dòng `internalNote: null, internalNoteUpdatedAt: null, statusHistory: [],` đổi thành:
```ts
    internalNote: null, internalNoteUpdatedAt: null, statusHistory: [], account: notSent,
```
- thêm các test sau vào cuối `describe('StudentProfileComponent', …)`:
```ts
  // @BR-f01-p1-tai-khoan-hoc-vien-001 @BR-f01-p1-tai-khoan-hoc-vien-026: chưa gửi → nút "Gửi thông tin đăng nhập"; gửi xong card cập nhật
  it('sends the login details and shows the new account state and history', async () => {
    const { fixture, http, el, buttons, click } = await setup();
    expect(el.querySelector('app-account-card .state')?.textContent?.trim()).toBe('Chưa gửi thông tin đăng nhập');

    click('Gửi thông tin đăng nhập');
    const post = http.expectOne('/api/admin/students/s1/credentials');
    expect(post.request.method).toBe('POST');
    expect(post.request.body).toEqual({});
    post.flush(detail({ account: sent, version: 'AAAAAAAAB9I=' }));
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('app-account-card .state')?.textContent?.trim())
      .toBe('Đã gửi lúc 25/11 09:00, mật khẩu tạm còn hạn đến 28/11 09:00');
    expect(el.querySelector('app-account-card ol')?.textContent).toContain('Gửi thông tin đăng nhập lần đầu.');
    expect(el.querySelector('p.msg.ok[role="status"]')?.textContent?.trim()).toBe('Đã gửi thông tin đăng nhập.');
    expect(buttons()).toContain('Gửi lại thông tin đăng nhập');
    expect(buttons()).not.toContain('Gửi thông tin đăng nhập');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-006: gửi lỗi → câu báo của server, trạng thái giữ nguyên
  it('shows the server message when the email could not be sent', async () => {
    const { fixture, http, el, click } = await setup(detail({ account: sent }));

    click('Gửi lại thông tin đăng nhập');
    http.expectOne('/api/admin/students/s1/credentials').flush(
      { title: 'Chưa gửi được email. Vui lòng thử lại sau.', code: 'account.email_send_failed' },
      { status: 503, statusText: 'Service Unavailable' });
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('p.msg[role="status"]')?.textContent?.trim()).toBe('Chưa gửi được email. Vui lòng thử lại sau.');
    expect(el.querySelector('p.msg.ok')).toBeNull();
    expect(el.querySelector('app-account-card .state')?.textContent?.trim())
      .toBe('Đã gửi lúc 25/11 09:00, mật khẩu tạm còn hạn đến 28/11 09:00');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-017 @BR-f01-p1-tai-khoan-hoc-vien-026: đã kích hoạt → header "Kích hoạt dd/MM/yyyy", nút gửi link đặt lại
  it('shows the activation date and sends a reset link for an activated account', async () => {
    const { fixture, http, el, buttons, click } = await setup(detail({ accountStatus: 'Activated', account: activated }));

    expect(el.querySelector('.meta .activated')?.textContent?.trim()).toBe('Kích hoạt 13/10/2026');
    expect(el.querySelector('.meta')?.textContent).not.toContain('Chờ kích hoạt');
    expect(buttons()).not.toContain('Gửi lại thông tin đăng nhập');
    expect(el.querySelector('app-account-card ol')?.textContent).toContain('Sửa email từ lan@gmial.com sang lan@gmail.com.');

    click('Gửi link đặt lại mật khẩu');
    const post = http.expectOne('/api/admin/students/s1/password-reset-link');
    expect(post.request.method).toBe('POST');
    post.flush(detail({
      accountStatus: 'Activated',
      account: { ...activated, history: [{ type: 'ResetLinkSent', at: '2026-11-25T02:00:00+00:00', fromEmail: null, toEmail: null }, ...activated.history] },
    }));
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('p.msg.ok')?.textContent?.trim()).toBe('Đã gửi link đặt lại mật khẩu.');
    expect(el.querySelector('app-account-card ol li')?.textContent).toContain('Gửi link đặt lại mật khẩu.');
  });

  it('shows the notice handed over by the create form', async () => {
    history.replaceState({ notice: 'Đã lưu hồ sơ nhưng chưa gửi được thông tin đăng nhập. Vui lòng thử lại.' }, '');
    try {
      const { el } = await setup();
      expect(el.querySelector('p.msg[role="status"]')?.textContent?.trim())
        .toBe('Đã lưu hồ sơ nhưng chưa gửi được thông tin đăng nhập. Vui lòng thử lại.');
    } finally {
      history.replaceState(null, '');
    }
  });
```

- [ ] **Step 6: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: FAIL — 4 test mới đỏ (không có nút "Gửi thông tin đăng nhập", không có `app-account-card`…).

- [ ] **Step 7: API gửi/sửa email/link đặt lại, màn Hồ sơ**

`frontend/projects/admin/src/app/students/students.api.ts` — thêm cuối class:
```ts

  /** F01-P1: gửi / gửi lại thông tin đăng nhập → hồ sơ mới nhất (có `account`). */
  sendCredentials(id: string): Observable<StudentDetail> {
    return this.http.post<StudentDetail>(this.url(`/${id}/credentials`), {});
  }

  /** F01-P1: sửa email khi tài khoản Chờ kích hoạt (đã gửi thông tin → server gửi thông tin mới tới email mới). */
  changeEmail(id: string, email: string, version: string): Observable<StudentDetail> {
    return this.http.put<StudentDetail>(this.url(`/${id}/email`), { email, version });
  }

  /** F01-P1: gửi link đặt lại mật khẩu cho tài khoản Đã kích hoạt. */
  sendResetLink(id: string): Observable<StudentDetail> {
    return this.http.post<StudentDetail>(this.url(`/${id}/password-reset-link`), {});
  }
```

`frontend/projects/admin/src/app/students/student-profile.component.ts`:
- sửa import:
```ts
import { AccountCardComponent } from './account-card.component';
import { CoachingStatusPanelComponent } from './coaching-status-panel.component';
import { formatDateOnly, formatNumber, formatVnDay, formatVnDayTime, formatVnFullDate } from './format';
import { FIELD_LABEL, FORM_SECTIONS, FieldDef } from './registration-fields';
import { ACCOUNT_STATUS_LABEL, ACCOUNT_STATUS_TONE, COACHING_STATUS_LABEL, COACHING_STATUS_TONE } from './student-status';
import { AccountState, CoachingStatus, StudentDetail } from './student.models';
```
- thêm sau hằng `ACTIONS`:
```ts
interface AccountAction {
  label: string;
  kind: 'credentials' | 'reset';
  done: string;
}

/** Nút phụ theo trạng thái tài khoản (tech-design F01-P1 UI): chưa gửi / đã gửi, hết hạn / đã kích hoạt. */
const ACCOUNT_ACTIONS: Record<AccountState, AccountAction> = {
  NotSent: { label: 'Gửi thông tin đăng nhập', kind: 'credentials', done: 'Đã gửi thông tin đăng nhập.' },
  Sent: { label: 'Gửi lại thông tin đăng nhập', kind: 'credentials', done: 'Đã gửi lại thông tin đăng nhập.' },
  TempPasswordExpired: { label: 'Gửi lại thông tin đăng nhập', kind: 'credentials', done: 'Đã gửi lại thông tin đăng nhập.' },
  Activated: { label: 'Gửi link đặt lại mật khẩu', kind: 'reset', done: 'Đã gửi link đặt lại mật khẩu.' },
};
```
- `imports` của `@Component` đổi thành:
```ts
  imports: [RouterLink, StatusBadgeComponent, CoachingStatusPanelComponent, AccountCardComponent],
```
- thêm field sau `protected readonly actions = computed(…);`:
```ts
  protected readonly accountAction = computed(() => {
    const d = this.detail();
    return d ? ACCOUNT_ACTIONS[d.account.state] : null;
  });
  protected readonly accountMessage = signal<{ ok: boolean; text: string } | null>(null);
  protected readonly fullDate = formatVnFullDate;
```
- thay `ngOnInit`:
```ts
  ngOnInit(): void {
    // Câu báo do form Tạo hồ sơ chuyển sang (lưu được nhưng chưa gửi được thông tin đăng nhập)
    const notice = (history.state as { notice?: unknown } | null)?.notice;
    if (typeof notice === 'string') this.accountMessage.set({ ok: false, text: notice });
    void this.load();
  }
```
- thêm method sau `closePanel()`:
```ts
  // @BR-f01-p1-tai-khoan-hoc-vien-001 @BR-f01-p1-tai-khoan-hoc-vien-006 @BR-f01-p1-tai-khoan-hoc-vien-017
  protected async runAccountAction(action: AccountAction): Promise<void> {
    const d = this.detail()!;
    this.busy.set(true);
    this.accountMessage.set(null);
    try {
      const request = action.kind === 'reset' ? this.api.sendResetLink(d.id) : this.api.sendCredentials(d.id);
      this.detail.set(await firstValueFrom(request));
      this.accountMessage.set({ ok: true, text: action.done });
    } catch (error) {
      this.accountMessage.set({
        ok: false,
        text: error instanceof HttpErrorResponse && typeof error.error?.title === 'string'
          ? error.error.title
          : 'Chưa gửi được email. Vui lòng thử lại sau.',
      });
      // 409 (đã kích hoạt / chưa kích hoạt): trạng thái đã đổi ở nơi khác → tải lại để nút đúng trạng thái
      if (error instanceof HttpErrorResponse && error.status === 409) await this.load();
    } finally {
      this.busy.set(false);
    }
  }
```

`frontend/projects/admin/src/app/students/student-profile.component.html`:
- thay khối `<p class="meta">…</p>`:
```html
        <p class="meta">
          <span>{{ d.email }}</span>
          <dbx-status-badge [tone]="coachingTone[d.coachingStatus]" [dense]="true">{{ coachingLabel[d.coachingStatus] }}</dbx-status-badge>
          @if (d.account.activatedAt; as activatedAt) {
            <span class="activated">Kích hoạt {{ fullDate(activatedAt) }}</span>
          } @else {
            <dbx-status-badge [tone]="accountTone[d.accountStatus]" [dense]="true">{{ accountLabel[d.accountStatus] }}</dbx-status-badge>
          }
        </p>
```
- thay khối `<div class="head-actions">…</div>`:
```html
    <div class="head-actions">
      @if (accountAction(); as a) {
        <button type="button" class="secondary" [disabled]="busy()" (click)="runAccountAction(a)">{{ a.label }}</button>
      }
      @for (a of actions(); track a.to) {
        <button type="button" class="secondary" [attr.aria-expanded]="pending()?.to === a.to" (click)="openPanel(a)">{{ a.label }}</button>
      }
    </div>
```
- ngay sau `</header>` thêm:
```html

  @if (accountMessage(); as m) {
    <p class="msg" [class.ok]="m.ok" role="status">{{ m.text }}</p>
  }
```
- trong `<aside>`, ngay trước `<section class="card history">` thêm:
```html
      <app-account-card [account]="d.account" />

```

`frontend/projects/admin/src/app/students/student-profile.component.scss` — thêm sau dòng `.msg { … }`:
```scss
.msg.ok { background: var(--okBg); color: var(--okInk); }
.activated { font-size: 13px; color: var(--muted); }
.secondary:disabled { opacity: .6; cursor: default; }
```

- [ ] **Step 8: Chạy — kỳ vọng PASS (hồ sơ)**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: `StudentProfileComponent` 13 test PASS (9 cũ + 4 mới); `student-form.component.spec.ts` vẫn lỗi kiểu fixture — sửa ở Step 9.

- [ ] **Step 9: Viết spec cho form Tạo / Sửa hồ sơ (Lưu và gửi, Chỉ lưu, email sửa được khi Chờ kích hoạt)**

`frontend/projects/admin/src/app/students/student-form.component.spec.ts` (thay toàn bộ):
```ts
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { FORM_FIELDS } from './registration-fields';
import { DRAFT_KEY, EMAIL_CHANGE_NOTE, SAVED_NOT_SENT_MESSAGE, SENT_MESSAGE, StudentFormComponent } from './student-form.component';
import { AccountInfo, StudentDetail } from './student.models';

const settle = () => new Promise((resolve) => setTimeout(resolve, 0));

const notSent: AccountInfo = { state: 'NotSent', credentialsSentAt: null, tempPasswordExpiresAt: null, activatedAt: null, history: [] };
const sent: AccountInfo = {
  state: 'Sent', credentialsSentAt: '2026-11-25T02:00:00+00:00', tempPasswordExpiresAt: '2026-11-28T02:00:00+00:00', activatedAt: null,
  history: [{ type: 'CredentialsSent', at: '2026-11-25T02:00:00+00:00', fromEmail: null, toEmail: null }],
};

function detail(overrides: Partial<StudentDetail> = {}): StudentDetail {
  return {
    id: 's1', coachId: 'c1', fullName: 'Lan', email: 'lan@gmail.com', accountStatus: 'PendingActivation', coachingStatus: 'Active',
    createdAt: '2026-11-25T02:00:00+00:00', registration: { weightKg: 72.5, sessionsPerWeek: '4 buổi' }, missingRequired: [],
    updatedAt: null, updatedBy: null, version: 'AAAAAAAAB9E=', internalNote: null, internalNoteUpdatedAt: null, statusHistory: [],
    account: notSent,
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

  /** Điền đủ 30 mục bắt buộc bằng giá trị hợp lệ. */
  function fillAllRequired(el: HTMLElement) {
    for (const f of FORM_FIELDS.filter((x) => x.required)) {
      const value = f.key === 'email' ? 'lan@gmail.com'
        : f.type === 'select' ? f.options![0]
        : f.type === 'date' ? '2026-11-01'
        : f.type === 'integer' ? '28'
        : f.type === 'decimal' ? '170'
        : f.key.startsWith('photo') ? 'https://drive.google.com/x' : 'Lan';
      fill(el, f.key, value);
    }
  }

  const submit = (el: HTMLElement) => el.querySelector('form')!.dispatchEvent(new Event('submit'));
  const saveOnly = (el: HTMLElement) => (el.querySelector('button.save-only') as HTMLButtonElement).click();

  it('offers the prototype actions on create', () => {
    const { el } = setup('create');
    const labels = [...el.querySelectorAll('.actions button')].map((b) => b.textContent?.trim());
    expect(labels).toEqual(['Lưu và gửi thông tin đăng nhập', 'Chỉ lưu hồ sơ', 'Xoá trắng hồ sơ']);
    expect(el.querySelector('.account')?.textContent).toContain('Chưa tạo');
    expect(el.querySelector('.account')?.textContent).toContain('Hồ sơ ở trạng thái Chờ kích hoạt sau khi gửi.');
    expect(el.querySelector('.receive h2')?.textContent?.trim()).toBe('Học viên sẽ nhận gì');
  });

  // @BR-f02-ho-so-hoc-vien-003 + NFR-f02-ho-so-hoc-vien-004: "Chỉ lưu hồ sơ" → xóa bản nháp, mở hồ sơ
  it('"Chỉ lưu hồ sơ" saves a profile with only name and email, clears the draft and opens the profile', async () => {
    const { http, el, navigate } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    expect(localStorage.getItem(DRAFT_KEY)).toContain('lan@gmail.com');

    saveOnly(el);
    const request = http.expectOne('/api/admin/students');
    expect(request.request.method).toBe('POST');
    expect(request.request.body.fullName).toBe('Lan');
    expect(request.request.body.email).toBe('lan@gmail.com');
    expect(request.request.body.registration['weightKg']).toBeNull();
    request.flush(detail());
    await settle();

    http.expectNone('/api/admin/students/s1/credentials');
    expect(localStorage.getItem(DRAFT_KEY)).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/hoc-vien/s1');
  });

  // @BR-f02-ho-so-hoc-vien-014: ghi chú nội bộ gửi trong cùng POST tạo hồ sơ
  it('sends the internal note in the same create request', async () => {
    const { http, el, navigate } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    fill(el, 'internalNote', ' Tránh lunge sâu vì gối phải ');

    saveOnly(el);
    const request = http.expectOne('/api/admin/students');
    expect(request.request.body.internalNote).toBe('Tránh lunge sâu vì gối phải');
    request.flush(detail({ internalNote: 'Tránh lunge sâu vì gối phải' }));
    await settle();

    http.expectNone('/api/admin/students/s1/internal-note');
    expect(navigate).toHaveBeenCalledWith('/hoc-vien/s1');
  });

  // NFR-f02-ho-so-hoc-vien-004
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

  // @BR-f02-ho-so-hoc-vien-008
  it('sends decimal-comma numbers as JSON numbers', () => {
    const { http, el } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    fill(el, 'weightKg', '72,5');
    fill(el, 'price', '3.500.000');

    saveOnly(el);

    const body = http.expectOne('/api/admin/students').request.body;
    expect(body.registration['weightKg']).toBe(72.5);
    expect(body.registration['price']).toBe(3500000);
  });

  // @BR-f02-ho-so-hoc-vien-008 (seed "abc")
  it('does not send a non-numeric weight and marks the field', () => {
    const { fixture, http, el } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');
    fill(el, 'weightKg', 'abc');

    saveOnly(el);
    fixture.detectChanges();

    http.expectNone('/api/admin/students');
    expect(el.querySelector('#hs-weightKg')?.getAttribute('aria-invalid')).toBe('true');
    expect(el.textContent).toContain('Nhập số hợp lệ');
  });

  // @BR-f02-ho-so-hoc-vien-009
  it('offers only the allowed options for sessions per week', () => {
    const { el } = setup('create');
    const options = [...el.querySelectorAll('#hs-sessionsPerWeek option')].map((o) => o.textContent?.trim());
    expect(options).toEqual(['Chọn…', '2 buổi', '3 buổi', '4 buổi', '5 buổi', '6 buổi', '7 buổi']);
  });

  // @BR-f02-ho-so-hoc-vien-001
  it('shows the server message when the email is already used', async () => {
    const { fixture, http, el, navigate } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'an@mail.com');

    saveOnly(el);
    http.expectOne('/api/admin/students').flush(
      { title: 'Email này đã được dùng cho một tài khoản khác.', code: 'student.email_taken' },
      { status: 409, statusText: 'Conflict' });
    await settle();
    fixture.detectChanges();

    expect(el.textContent).toContain('Email này đã được dùng cho một tài khoản khác.');
    expect(navigate).not.toHaveBeenCalled();
    expect(localStorage.getItem(DRAFT_KEY)).toContain('an@mail.com');
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-002: lần gửi đầu cần đủ mục bắt buộc → hồ sơ vẫn được lưu, không gửi, báo số mục thiếu
  it('"Lưu và gửi" with missing required fields saves the profile but does not send', async () => {
    const { http, el, navigate } = setup('create');
    fill(el, 'fullName', 'Lan');
    fill(el, 'email', 'lan@gmail.com');

    submit(el);
    http.expectOne('/api/admin/students').flush(detail());
    await new Promise((r) => setTimeout(r));

    http.expectNone((req) => req.url.endsWith('/credentials'));
    expect(navigate).toHaveBeenCalledWith(`/hoc-vien/${detail().id}`, {
      state: { notice: 'Đã lưu hồ sơ. Còn 28 mục bắt buộc chưa điền nên chưa gửi thông tin đăng nhập.' },
    });
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-001: tạo rồi gửi thông tin đăng nhập; aside đổi sang "Đã gửi thông tin, chờ kích hoạt"
  it('"Lưu và gửi" creates the profile, sends the login details and shows the prototype message', async () => {
    const { fixture, http, el, navigate } = setup('create');
    fillAllRequired(el);

    submit(el);
    http.expectOne('/api/admin/students').flush(detail());
    await settle();
    const post = http.expectOne('/api/admin/students/s1/credentials');
    expect(post.request.method).toBe('POST');
    post.flush(detail({ account: sent }));
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('p.msg.ok[role="status"]')?.textContent?.trim()).toBe(SENT_MESSAGE);
    expect(el.querySelector('.account')?.textContent).toContain('Đã gửi thông tin, chờ kích hoạt');
    expect(el.querySelector('.account')?.textContent).toContain('Gửi đến lan@gmail.com lúc 09:00. Mật khẩu tạm hết hạn sau 72 giờ.');
    expect((el.querySelector('button[type="submit"]') as HTMLButtonElement).disabled).toBe(true);
    expect(localStorage.getItem(DRAFT_KEY)).toBeNull();
    expect(navigate).not.toHaveBeenCalled();
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-006: hồ sơ đã lưu nhưng gửi lỗi → sang trang hồ sơ kèm câu báo
  it('opens the profile with a notice when saving worked but sending failed', async () => {
    const { http, el, navigate } = setup('create');
    fillAllRequired(el);

    submit(el);
    http.expectOne('/api/admin/students').flush(detail());
    await settle();
    http.expectOne('/api/admin/students/s1/credentials').flush(
      { title: 'Chưa gửi được email. Vui lòng thử lại sau.', code: 'account.email_send_failed' },
      { status: 503, statusText: 'Service Unavailable' });
    await settle();

    expect(navigate).toHaveBeenCalledWith('/hoc-vien/s1', { state: { notice: SAVED_NOT_SENT_MESSAGE } });
  });

  // @BR-f02-ho-so-hoc-vien-012 @BR-f02-ho-so-hoc-vien-015 @BR-f01-p1-tai-khoan-hoc-vien-007: đã kích hoạt → email chỉ xem
  it('edits with the version, keeps email read-only once activated and offers reload on conflict', async () => {
    const { fixture, http, el } = setup('edit', 's1');
    http.expectOne('/api/admin/students/s1').flush(detail({ accountStatus: 'Activated' }));
    await settle();
    fixture.detectChanges();

    expect((el.querySelector('#hs-weightKg') as HTMLInputElement).value).toBe('72,5');
    expect((el.querySelector('#hs-email') as HTMLInputElement).readOnly).toBe(true);
    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Sửa hồ sơ');

    fill(el, 'weightKg', '70');
    expect(localStorage.getItem(DRAFT_KEY)).toBeNull();
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
    http.expectOne('/api/admin/students/s1').flush(detail({ accountStatus: 'Activated', version: 'AAAAAAAAB9I=' }));
    await settle();
    fixture.detectChanges();
    expect(el.querySelector('button.reload')).toBeNull();
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-007 @BR-f01-p1-tai-khoan-hoc-vien-009: Chờ kích hoạt → sửa email qua PUT /email trước, rồi PUT hồ sơ
  it('changes the email of a pending account first, then saves the profile with the new version', async () => {
    const { fixture, http, el, navigate } = setup('edit', 's1');
    http.expectOne('/api/admin/students/s1').flush(detail({ email: 'lan@gmial.com', account: sent }));
    await settle();
    fixture.detectChanges();

    expect((el.querySelector('#hs-email') as HTMLInputElement).readOnly).toBe(false);
    expect(el.textContent).toContain(EMAIL_CHANGE_NOTE);

    fill(el, 'email', 'lan@gmail.com');
    submit(el);
    const email = http.expectOne('/api/admin/students/s1/email');
    expect(email.request.method).toBe('PUT');
    expect(email.request.body).toEqual({ email: 'lan@gmail.com', version: 'AAAAAAAAB9E=' });
    email.flush(detail({ email: 'lan@gmail.com', version: 'V2', account: sent }));
    await settle();
    const put = http.expectOne('/api/admin/students/s1');
    expect(put.request.method).toBe('PUT');
    expect(put.request.body.version).toBe('V2');
    put.flush(detail({ email: 'lan@gmail.com', version: 'V3', account: sent }));
    await settle();

    expect(navigate).toHaveBeenCalledWith('/hoc-vien/s1');
  });

  it('does not show the email-change note before any login details were sent, and skips PUT /email when unchanged', async () => {
    const { fixture, http, el } = setup('edit', 's1');
    http.expectOne('/api/admin/students/s1').flush(detail());
    await settle();
    fixture.detectChanges();

    expect(el.textContent).not.toContain(EMAIL_CHANGE_NOTE);
    submit(el);
    http.expectNone('/api/admin/students/s1/email');
    http.expectOne('/api/admin/students/s1').flush(detail({ version: 'V2' }));
    await settle();
  });

  // @BR-f01-p1-tai-khoan-hoc-vien-008: email đã dùng → câu của server, không phải banner xung đột phiên bản
  it('shows the server message, not the reload banner, when the new email is taken', async () => {
    const { fixture, http, el } = setup('edit', 's1');
    http.expectOne('/api/admin/students/s1').flush(detail());
    await settle();
    fixture.detectChanges();

    fill(el, 'email', 'an@mail.com');
    submit(el);
    http.expectOne('/api/admin/students/s1/email').flush(
      { title: 'Email này đã được dùng cho một tài khoản khác.', code: 'student.email_taken' },
      { status: 409, statusText: 'Conflict' });
    await settle();
    fixture.detectChanges();

    expect(el.querySelector('p.msg')?.textContent).toContain('Email này đã được dùng cho một tài khoản khác.');
    expect(el.querySelector('button.reload')).toBeNull();
    http.expectNone('/api/admin/students/s1');
  });

  // @BR-f02-ho-so-hoc-vien-012
  it('sends the internal note with the version returned by the profile PUT', async () => {
    const { fixture, http, el } = setup('edit', 's1');
    http.expectOne('/api/admin/students/s1').flush(detail());
    await settle();
    fixture.detectChanges();

    fill(el, 'internalNote', 'Tránh lunge sâu');
    submit(el);
    http.expectOne('/api/admin/students/s1').flush(detail({ version: 'V2' }));
    await settle();
    const note = http.expectOne('/api/admin/students/s1/internal-note');
    expect(note.request.method).toBe('PUT');
    expect(note.request.body).toEqual({ note: 'Tránh lunge sâu', version: 'V2' });
    note.flush(detail({ version: 'V3', internalNote: 'Tránh lunge sâu' }));
    await settle();
  });

  it('retries with the fresh version when the note PUT failed after the profile PUT succeeded', async () => {
    const { fixture, http, el } = setup('edit', 's1');
    http.expectOne('/api/admin/students/s1').flush(detail());
    await settle();
    fixture.detectChanges();

    fill(el, 'internalNote', 'Tránh lunge sâu');
    submit(el);
    http.expectOne('/api/admin/students/s1').flush(detail({ version: 'V2' }));
    await settle();
    http.expectOne('/api/admin/students/s1/internal-note').flush({ title: 'Lỗi' }, { status: 500, statusText: 'Server Error' });
    await settle();
    fixture.detectChanges();

    submit(el);
    const retry = http.expectOne('/api/admin/students/s1');
    expect(retry.request.body.version).toBe('V2');
    retry.flush(detail({ version: 'V4' }));
    await settle();
    const note = http.expectOne('/api/admin/students/s1/internal-note');
    expect(note.request.body.version).toBe('V4');
    note.flush(detail({ version: 'V5', internalNote: 'Tránh lunge sâu' }));
    await settle();
  });

  it('disables submit and ignores submit in edit mode until the profile is loaded', async () => {
    const { fixture, http, el } = setup('edit', 's1');
    const button = el.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
    submit(el);
    await settle();
    http.expectOne('/api/admin/students/s1').flush(detail());
    await settle();
    fixture.detectChanges();
    expect(button.disabled).toBe(false);
    http.verify();
  });
});
```

- [ ] **Step 10: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: FAIL — `EMAIL_CHANGE_NOTE`/`SENT_MESSAGE`/`SAVED_NOT_SENT_MESSAGE` không được export; không có `button.save-only`…

- [ ] **Step 11: Form Tạo / Sửa hồ sơ**

`frontend/projects/admin/src/app/students/student-form.component.ts` (thay toàn bộ):
```ts
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { formatVnTime } from './format';
import { FORM_SECTIONS, FieldDef, FormPayload, FormValues, toFormValues, toPayload } from './registration-fields';
import { ACCOUNT_STATUS_LABEL } from './student-status';
import { StudentDetail } from './student.models';
import { StudentsApi } from './students.api';

/** @ADR-f02-ho-so-hoc-vien-008 / NFR-f02-ho-so-hoc-vien-004: bản nháp chỉ ở chế độ tạo, chỉ trên trình duyệt này. */
export const DRAFT_KEY = 'dbx.admin.student-draft';
/** Câu báo nguyên văn prototype AdTaoHoSo / tech-design F01-P1 "Thiết kế giao diện (UI)". */
export const SENT_MESSAGE = 'Đã lưu hồ sơ và gửi thông tin đăng nhập.';
export const SAVED_NOT_SENT_MESSAGE = 'Đã lưu hồ sơ nhưng chưa gửi được thông tin đăng nhập. Vui lòng thử lại.';
export const EMAIL_CHANGE_NOTE = 'Đổi email sẽ huỷ mật khẩu tạm cũ và gửi thông tin đăng nhập mới tới email mới.';

type Tone = 'ok' | 'warn';

/**
 * @ADR-f02-ho-so-hoc-vien-008: một form cho "Tạo hồ sơ học viên" (hoc-vien/moi) và "Sửa hồ sơ" (hoc-vien/:id/sua).
 * "Chỉ lưu hồ sơ": mục bắt buộc còn thiếu KHÔNG chặn lưu (@BR-f02-ho-so-hoc-vien-004).
 * "Lưu và gửi thông tin đăng nhập": cần đủ mục bắt buộc (@BR-f01-p1-tai-khoan-hoc-vien-002), tạo rồi gửi.
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
  protected readonly message = signal<{ tone: Tone; text: string } | null>(null);
  protected readonly conflict = signal(false);
  protected readonly saving = signal(false);
  protected readonly tried = signal(false);
  protected readonly loaded = signal<StudentDetail | null>(null);
  protected readonly created = signal<StudentDetail | null>(null);
  protected readonly loadFailed = signal(false);
  protected readonly isEdit = computed(() => this.mode() === 'edit');
  protected readonly accountLabel = ACCOUNT_STATUS_LABEL;
  protected readonly emailChangeNote = EMAIL_CHANGE_NOTE;

  /** Sửa hồ sơ: email sửa được khi tài khoản còn Chờ kích hoạt (@BR-f01-p1-tai-khoan-hoc-vien-007). */
  protected readonly emailEditable = computed(() => !this.isEdit() || this.loaded()?.accountStatus === 'PendingActivation');
  /** Ghi chú dưới ô email khi hồ sơ đã từng được gửi thông tin (@BR-f01-p1-tai-khoan-hoc-vien-009). */
  protected readonly showEmailChangeNote = computed(
    () => this.isEdit() && this.emailEditable() && this.loaded()?.account.credentialsSentAt != null);
  /** Aside "Trạng thái tài khoản" sau khi Lưu và gửi (stText/stNote của prototype). */
  protected readonly sentState = computed(() => {
    const c = this.created();
    return c?.account.credentialsSentAt ? { email: c.email, time: formatVnTime(c.account.credentialsSentAt) } : null;
  });

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
      missing: requiredAll - requiredDone,
    };
  });

  ngOnInit(): void {
    if (this.isEdit()) void this.load();
    else this.values.set(readDraft());
  }

  protected isMissing(f: FieldDef): boolean {
    return this.tried() && f.required && (this.values()[f.key] ?? '').trim() === '';
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

  /** Nút chính: tạo → "Lưu và gửi thông tin đăng nhập"; sửa → "Lưu thay đổi". */
  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    if (this.isEdit()) await this.save();
    else await this.saveAndSend();
  }

  /** "Chỉ lưu hồ sơ" (F02): lưu rồi mở hồ sơ. */
  protected async saveOnly(): Promise<void> {
    await this.save();
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
    this.tried.set(false);
    this.created.set(null);
  }

  private async save(): Promise<void> {
    if (this.isEdit() && !this.loaded()) return; // chưa tải xong hồ sơ: chưa có version để gửi
    const payload = this.validPayload();
    if (!payload) return;
    this.begin();
    try {
      const saved = this.isEdit() ? await this.saveEdit(payload) : await this.saveCreate(payload);
      await this.router.navigateByUrl(`/hoc-vien/${saved.id}`);
    } catch (error) {
      // @BR-f02-ho-so-hoc-vien-015: chỉ xung đột phiên bản mới hiện "Tải lại"; 409 khác (email đã dùng/đã khoá) hiện câu của server
      if (this.isEdit() && isVersionConflict(error)) this.conflict.set(true);
      else this.warn(problemTitle(error));
    } finally {
      this.saving.set(false);
    }
  }

  // @BR-f01-p1-tai-khoan-hoc-vien-002: lần gửi đầu cần đủ mục bắt buộc → hồ sơ VẪN được lưu (spec), chưa gửi;
  // sang trang hồ sơ kèm câu báo số mục thiếu (trang hồ sơ có khối "Mục bắt buộc còn thiếu" của F02)
  private async saveAndSend(): Promise<void> {
    const missing = this.progress().missing;
    if (missing > 0) {
      this.tried.set(true);
      const draft = this.validPayload();
      if (!draft) return;
      this.begin();
      try {
        const saved = await this.saveCreate(draft);
        await this.router.navigateByUrl(`/hoc-vien/${saved.id}`, {
          state: { notice: `Đã lưu hồ sơ. Còn ${missing} mục bắt buộc chưa điền nên chưa gửi thông tin đăng nhập.` },
        });
      } catch (error) {
        this.warn(problemTitle(error));
      } finally {
        this.saving.set(false);
      }
      return;
    }
    const payload = this.validPayload();
    if (!payload) return;
    this.begin();
    let created: StudentDetail;
    try {
      created = await this.saveCreate(payload);
    } catch (error) {
      this.warn(problemTitle(error));
      this.saving.set(false);
      return;
    }
    try {
      this.created.set(await firstValueFrom(this.api.sendCredentials(created.id)));
      this.message.set({ tone: 'ok', text: SENT_MESSAGE });
    } catch {
      // @BR-f01-p1-tai-khoan-hoc-vien-006: hồ sơ đã lưu, thông tin chưa gửi → sang trang hồ sơ để coach gửi lại
      await this.router.navigateByUrl(`/hoc-vien/${created.id}`, { state: { notice: SAVED_NOT_SENT_MESSAGE } });
    } finally {
      this.saving.set(false);
    }
  }

  private validPayload(): FormPayload | null {
    const { payload, errors } = toPayload(this.values());
    this.errors.set(errors);
    if (Object.keys(errors).length > 0) {
      this.warn('Có ô số liệu chưa đúng. Sửa các ô được đánh dấu đỏ.');
      return null;
    }
    return payload;
  }

  private begin(): void {
    this.saving.set(true);
    this.message.set(null);
    this.conflict.set(false);
  }

  private warn(text: string): void {
    this.message.set({ tone: 'warn', text });
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
    let current = this.loaded()!;
    // @BR-f01-p1-tai-khoan-hoc-vien-007 @BR-f01-p1-tai-khoan-hoc-vien-009: đổi email đi endpoint riêng trước
    // (đã gửi thông tin → server gửi thông tin mới tới email mới), rồi PUT hồ sơ với version mới
    if (this.emailEditable() && payload.email.toLowerCase() !== current.email.toLowerCase()) {
      current = await firstValueFrom(this.api.changeEmail(current.id, payload.email, current.version));
      this.loaded.set(current);
    }
    const saved = await firstValueFrom(this.api.update(current.id, {
      fullName: payload.fullName, registration: payload.registration, version: current.version,
    }));
    this.loaded.set(saved); // PUT hồ sơ thành công: giữ version mới để thử lại ghi chú không bị 409 giả
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

function isVersionConflict(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === 409 && error.error?.code === 'student.version_conflict';
}

function problemTitle(error: unknown): string {
  if (error instanceof HttpErrorResponse && typeof error.error?.title === 'string') return error.error.title;
  return 'Không lưu được hồ sơ. Vui lòng thử lại.';
}
```

`frontend/projects/admin/src/app/students/student-form.component.html` (thay toàn bộ):
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
                           [class.err]="!!errors()[f.key] || isMissing(f)" [attr.aria-invalid]="!!errors()[f.key] || isMissing(f)"
                           (input)="setValue(f.key, $any($event.target).value)" />
                  }
                  @case ('area') {
                    <textarea class="ctl" [id]="'hs-' + f.key" [rows]="f.rows ?? 3" [placeholder]="f.placeholder ?? ''"
                              [value]="values()[f.key] ?? ''" [class.err]="!!errors()[f.key] || isMissing(f)"
                              [attr.aria-invalid]="!!errors()[f.key] || isMissing(f)"
                              (input)="setValue(f.key, $any($event.target).value)"></textarea>
                  }
                  @case ('select') {
                    <select class="ctl" [id]="'hs-' + f.key" [class.err]="!!errors()[f.key] || isMissing(f)"
                            [attr.aria-invalid]="!!errors()[f.key] || isMissing(f)"
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
                             [readOnly]="f.key === 'email' && !emailEditable()"
                             [class.err]="!!errors()[f.key] || isMissing(f)" [attr.aria-invalid]="!!errors()[f.key] || isMissing(f)"
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
                @if (f.key === 'email' && showEmailChangeNote()) {
                  <span class="note">{{ emailChangeNote }}</span>
                }
                @if (errors()[f.key]; as e) {
                  <span class="field-err">{{ e }}</span>
                } @else if (isMissing(f)) {
                  <span class="field-err">Chưa điền</span>
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
        <p class="msg" [class.ok]="m.tone === 'ok'" role="status">{{ m.text }}</p>
      }
      <div class="actions">
        @if (isEdit()) {
          <button type="submit" class="primary" [disabled]="saving() || !loaded()">Lưu thay đổi</button>
        } @else {
          <button type="submit" class="primary" [disabled]="saving() || !!created()">Lưu và gửi thông tin đăng nhập</button>
          <button type="button" class="save-only" [disabled]="saving() || !!created()" (click)="saveOnly()">Chỉ lưu hồ sơ</button>
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
        @if (isEdit()) {
          @if (loaded(); as d) {
            <span class="acct" [class.pending]="d.accountStatus === 'PendingActivation'">{{ accountLabel[d.accountStatus] }}</span>
          }
        } @else if (sentState(); as s) {
          <span class="acct pending">Đã gửi thông tin, chờ kích hoạt</span>
          <span class="acct-note">Gửi đến {{ s.email }} lúc {{ s.time }}. Mật khẩu tạm hết hạn sau 72 giờ.</span>
        } @else {
          <span class="acct">Chưa tạo</span>
          <span class="acct-note">Hồ sơ ở trạng thái Chờ kích hoạt sau khi gửi.</span>
        }
      </section>
      @if (!isEdit()) {
        <section class="receive">
          <h2>Học viên sẽ nhận gì</h2>
          <ol>
            <li>Email có link đăng nhập, email tài khoản và mật khẩu tạm.</li>
            <li>Lần đầu đăng nhập, học viên đặt mật khẩu riêng.</li>
            <li>Tài khoản chuyển sang Đã kích hoạt và mở trang Hôm nay.</li>
          </ol>
        </section>
      }
    </aside>
  </div>
}
```

`frontend/projects/admin/src/app/students/student-form.component.scss` — thêm sau dòng `.msg { … }`:
```scss
.msg.ok { background: var(--okBg); color: var(--okInk); }
```
thêm sau dòng `.primary:disabled { … }`:
```scss
.save-only { height: 50px; padding: 0 22px; border-radius: 999px; border: 1px solid var(--field); background: var(--surface);
  color: var(--ink); font: inherit; font-weight: 600; font-size: 15px; cursor: pointer; }
.save-only:disabled { opacity: .6; cursor: default; }
```
và thêm trước dòng `@media (max-width: 879.98px) …`:
```scss
.receive { background: var(--soft); border-radius: 14px; padding: 16px 18px; display: flex; flex-direction: column; gap: 8px; }
.receive h2 { margin: 0; font-family: var(--font-head); font-weight: 700; font-size: 20px; color: var(--navInk); }
.receive ol { margin: 0; padding-left: 20px; display: flex; flex-direction: column; gap: 6px; font-size: 14px; line-height: 1.5; }
```

- [ ] **Step 12: Chạy — kỳ vọng PASS**

Run: `cd frontend && npm run test:ci`
Expected: PASS (`StudentFormComponent` 19 test, `StudentProfileComponent` 13 test, `account card` 3 test, `format` 3 test; shared/client không đổi).
Run: `cd frontend && npx ng build admin`
Expected: build thành công.
Kiểm tay (`npx ng serve admin` + API local Task 1–7, `Email:Provider=Capture`): Tạo hồ sơ chỉ họ tên + email → "Lưu và gửi thông tin đăng nhập" → hồ sơ được lưu, chuyển sang trang hồ sơ với câu "Đã lưu hồ sơ. Còn 28 mục bắt buộc chưa điền nên chưa gửi thông tin đăng nhập."; điền đủ → hộp xanh "Đã lưu hồ sơ và gửi thông tin đăng nhập.", aside "Đã gửi thông tin, chờ kích hoạt" + "Gửi đến … lúc HH:mm. Mật khẩu tạm hết hạn sau 72 giờ."; "Chỉ lưu hồ sơ" → `/hoc-vien/<id>`. Hồ sơ: nút "Gửi thông tin đăng nhập" → card "Tài khoản đăng nhập" đổi dòng trạng thái + lịch sử; học viên kích hoạt → header "Kích hoạt dd/MM/yyyy", nút "Gửi link đặt lại mật khẩu". Sửa hồ sơ khi Chờ kích hoạt: ô email sửa được, ghi chú "Đổi email sẽ huỷ…" khi đã gửi. So với `AdTaoHoSo` và `AdHoSo` (tab "Tài khoản") ở 390px và 1280px: nút "Chỉ lưu hồ sơ" 50px viền `--field` 15px 600, card "Học viên sẽ nhận gì" nền `--soft` radius 14 h2 Barlow 700 20px `--navInk`, card "Tài khoản đăng nhập" radius 12 padding 16px 18px h2 Barlow 700 24px, `<ol>` viền trái 2px `--soft`, ghi chú cuối 13px `--muted`, "Kích hoạt dd/MM/yyyy" 13px `--muted`, nút phụ header 44px.

- [ ] **Step 13: Commit**
```bash
git add frontend/projects/admin
git commit -F - <<'EOF'
feat(admin): save-and-send login details, account card with history, pending email edit

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 10: E2E `@web` cho tài khoản học viên + compose (Email Capture, ClientBaseUrl) + bảng route trong CLAUDE.md

**Files:**
- Create: `frontend/e2e/steps/account.steps.ts`
- Modify: `frontend/e2e/support/world.ts`, `frontend/e2e/steps/student-profile.steps.ts`
- Modify: `deploy/docker-compose.yml`
- Modify: `CLAUDE.md` (bảng "Page key → Route gợi ý" + "Lệnh thường dùng")
- Không đổi: `deploy/.env.example` (không thêm biến mới — hai giá trị cố định nằm trong compose), `.github/workflows/ci.yml` (job `e2e` đã chạy `docker compose -f deploy/docker-compose.yml --env-file deploy/.env` với `AUTH_PERMIT_PER_MINUTE=1000` nên tự nhận cấu hình mới).

**Interfaces:**
- Consumes: Task 2 — `Email__Provider=Capture` (chỉ ngoài Production), `GET /api/test/emails?to=` → `[{ to, subject, textBody, sentAt }]`; tiêu đề "Thông tin đăng nhập Duobexiuuu Coaching" / "Đặt lại mật khẩu Duobexiuuu Coaching"; thân thư có `<ClientBaseUrl>/login`, dòng `Mật khẩu tạm: <mật khẩu>` và link `<ClientBaseUrl>/dat-lai-mat-khau#t=<token>`; `App__ClientBaseUrl`. Task 4/5/7 — `POST /api/admin/students/{id}/credentials`, `POST /api/auth/login` (dạng `activationRequired`), `POST /api/auth/activate`. Task 8 — `/login` + "Quên mật khẩu?", `/kich-hoat` (h1 "Đặt mật khẩu riêng", nhãn "Mật khẩu mới", "Nhập lại mật khẩu", nút "Lưu mật khẩu và vào trang Hôm nay"), `/quen-mat-khau` (nhãn "Email", nút "Gửi link đặt lại", `role=status`), `/dat-lai-mat-khau` (h1 "Đặt lại mật khẩu"). Task 9 — `/hoc-vien/:id` (nút "Gửi thông tin đăng nhập" / "Gửi lại thông tin đăng nhập", region "Tài khoản đăng nhập", `.meta`), `/hoc-vien/:id/sua` (ô "Email đăng nhập", nút "Lưu thay đổi"), `/hoc-vien/moi` nút "Chỉ lưu hồ sơ". F00/F02 — `E2EWorld.login/openPage`, `ADMIN`, `ADMIN_URL`, `CLIENT_URL`, step `coach {string} đã đăng nhập`, `học viên vào được khu vực học viên`.
- Produces: step `@web` cho 5 scenario: `thong-tin-dang-nhap.feature` BR-002 "Gửi lần đầu khi hồ sơ đủ mục bắt buộc", BR-007 "Sửa email khi tài khoản chưa kích hoạt"; `kich-hoat.feature` BR-011 "Đăng nhập bằng mật khẩu tạm chỉ dẫn tới việc đặt mật khẩu riêng", BR-016 "Đặt mật khẩu riêng thì tài khoản được kích hoạt"; `dat-lai-mat-khau.feature` BR-022 "Học viên đã kích hoạt nhận được link đặt lại". `E2EWorld.student`, `emailSuffix`, `tempPassword`.

**Owns:** —

- [ ] **Step 1: Compose dùng hộp thư Capture + địa chỉ web học viên; chạy e2e — kỳ vọng FAIL**

`deploy/docker-compose.yml` — trong `api.environment`, thay dòng:
```yaml
      Email__Provider: Log
```
bằng:
```yaml
      # @ADR-f01-p1-tai-khoan-hoc-vien-007: thư gửi đi giữ trong bộ nhớ, e2e đọc qua GET /api/test/emails?to= (API chặn Capture ở Production)
      Email__Provider: Capture
      # @ADR-f01-p1-tai-khoan-hoc-vien-008: địa chỉ đăng nhập / link đặt lại trong email trỏ về web học viên của compose
      App__ClientBaseUrl: http://localhost:4300
```

Run: `cp -n deploy/.env.example deploy/.env; sed -i 's/^AUTH_PERMIT_PER_MINUTE=.*/AUTH_PERMIT_PER_MINUTE=1000/' deploy/.env` rồi `docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build --wait`
Expected: các service `db`, `api`, `web` healthy/running; `curl -s "http://localhost:8080/api/test/emails?to=x@y.vn"` trả `[]`.
Run: `cd frontend && npm run e2e -- --tags "@web and (@BR-f01-p1-tai-khoan-hoc-vien-002 or @BR-f01-p1-tai-khoan-hoc-vien-007 or @BR-f01-p1-tai-khoan-hoc-vien-011 or @BR-f01-p1-tai-khoan-hoc-vien-016 or @BR-f01-p1-tai-khoan-hoc-vien-022)"`
Expected: FAIL — `5 scenarios (5 undefined)` (`một hồ sơ học viên đủ mục bắt buộc, chưa gửi thông tin đăng nhập lần nào`, `một học viên vừa được gửi mật khẩu tạm`, `hôm nay là "13/10/2026"`, `học viên có tài khoản Đã kích hoạt với email "quan@gmail.com"`, …).

- [ ] **Step 2: World giữ học viên của scenario; F02 bấm đúng "Chỉ lưu hồ sơ"; viết step defs `@web`**

`frontend/e2e/support/world.ts` — trong `class E2EWorld`, thêm sau `refreshStatus: number | null = null;`:
```ts
  /** F01-P1: học viên tạo trong scenario (email thật đã có hậu tố +e2e…). */
  student: { id: string; fullName: string; email: string } | null = null;
  /** F01-P1: email viết trong feature (chuẩn hoá) → hậu tố +e2e… để không trùng dữ liệu của lần chạy trước. */
  emailSuffix: Record<string, string> = {};
  tempPassword = '';
```

`frontend/e2e/steps/student-profile.steps.ts` — trong step `coach tạo hồ sơ học viên chỉ với họ tên {string} và email {string}`, thay:
```ts
    await this.page.getByRole('button', { name: 'Lưu hồ sơ' }).click();
```
bằng:
```ts
    await this.page.getByRole('button', { name: 'Chỉ lưu hồ sơ', exact: true }).click(); // F01-P1: nút chính là "Lưu và gửi…"
```

`frontend/e2e/steps/account.steps.ts`:
```ts
import { Given, Then, When } from '@cucumber/cucumber';
import { expect } from '../support/expect';
import { ADMIN, ADMIN_URL, CLIENT_URL } from '../support/env';
import { E2EWorld } from '../support/world';

interface CapturedMail { to: string; subject: string; textBody: string; sentAt: string; }

const CREDENTIALS_SUBJECT = 'Thông tin đăng nhập Duobexiuuu Coaching';
const RESET_SUBJECT = 'Đặt lại mật khẩu Duobexiuuu Coaching';
const FORGOT_MESSAGE = 'Nếu email này thuộc tài khoản học viên đã kích hoạt, link đặt lại mật khẩu đã được gửi tới email đó. Link có hiệu lực trong 30 phút. Không thấy email thì kiểm tra mục Spam.';
const FULL_NAME = 'Phạm Hoàng Quân';
const OWN_PASSWORD = 'Coach2025';

/** Đủ 28 mục bắt buộc của `registration` (+ họ tên, email = 30), giá trị hợp lệ theo RegistrationForm của F02. */
const FULL_REGISTRATION = {
  pack: 'Pack 2', price: 3500000, transferDate: '2026-01-15', address: 'Hà Nội', age: 28, heightCm: 170, weightKg: 72.5,
  trainingYears: '1–2 năm', weeklyTrainingTime: '5 buổi, mỗi buổi 75 phút', weightBeforeLiftingKg: 65, heightThenCm: 168,
  trainingGoal: 'Giảm mỡ, giữ cơ', medicalHistory: 'Không', foodBudgetReadiness: 'Sẵn sàng', activityLevel: 'Vừa',
  stressLevel: 'Vừa', sleepDuration: '7–8 tiếng', currentDiet: 'Ăn 3 bữa chính', mealAndTrainingTimes: 'Ăn 7h, 12h, 19h. Tập 17h30–19h.',
  sessionsPerWeek: '4 buổi', programStyle: 'Upper / Lower', photoFrontUrl: 'https://drive.google.com/front',
  photoBackUrl: 'https://drive.google.com/back', photoLeftUrl: 'https://drive.google.com/left', photoRightUrl: 'https://drive.google.com/right',
  expectations: 'Giáo án rõ ràng', previousCoaching: 'Chưa từng', investmentReason: 'Muốn khỏe hơn',
};

/**
 * DB của compose giữ dữ liệu giữa các lần chạy → mỗi email viết trong feature được gắn một hậu tố +e2e… cố định trong scenario.
 * Giữ nguyên hoa/thường và khoảng trắng người dùng gõ ("QUAN@gmail.com " → "QUAN+e2e…@gmail.com ") để server tự chuẩn hoá.
 */
function realEmail(world: E2EWorld, written: string): string {
  const key = written.trim().toLowerCase();
  world.emailSuffix[key] ??= `+e2e${Date.now()}${Math.floor(Math.random() * 1000)}`;
  return written.replace('@', `${world.emailSuffix[key]}@`);
}

async function adminToken(world: E2EWorld): Promise<string> {
  const res = await world.page.request.post(`${ADMIN_URL}/api/auth/login`,
    { data: { email: ADMIN.email, password: ADMIN.password, app: 'admin' } });
  expect(res.ok()).toBe(true);
  return ((await res.json()) as { accessToken: string }).accessToken;
}

async function createStudent(world: E2EWorld, writtenEmail: string, complete: boolean): Promise<void> {
  const token = await adminToken(world);
  const res = await world.page.request.post(`${ADMIN_URL}/api/admin/students`, {
    data: { fullName: FULL_NAME, email: realEmail(world, writtenEmail), ...(complete ? { registration: FULL_REGISTRATION } : {}) },
    headers: { authorization: `Bearer ${token}` },
  });
  expect(res.status()).toBe(201);
  const body = (await res.json()) as { id: string; email: string };
  world.student = { id: body.id, fullName: FULL_NAME, email: body.email };
}

/** Đợi tới khi hộp thư Capture có `count` thư cùng tiêu đề gửi tới `to`; trả thư mới nhất. */
async function waitForMail(world: E2EWorld, to: string, subject: string, count = 1): Promise<CapturedMail> {
  const deadline = Date.now() + 15_000;
  for (;;) {
    const res = await world.page.request.get(`${ADMIN_URL}/api/test/emails`, { params: { to: to.trim().toLowerCase() } });
    expect(res.ok()).toBe(true);
    const mails = ((await res.json()) as CapturedMail[]).filter((m) => m.subject === subject);
    if (mails.length >= count) return mails.sort((a, b) => Date.parse(b.sentAt) - Date.parse(a.sentAt))[0];
    if (Date.now() > deadline) throw new Error(`Không thấy ${count} thư "${subject}" gửi tới ${to} (có ${mails.length})`);
    await new Promise((r) => setTimeout(r, 250));
  }
}

function tempPasswordOf(mail: CapturedMail): string {
  const match = /Mật khẩu tạm:\s*(\S+)/.exec(mail.textBody);
  if (!match) throw new Error(`Thư thông tin đăng nhập không có dòng "Mật khẩu tạm:"\n${mail.textBody}`);
  return match[1];
}

async function sendCredentialsByApi(world: E2EWorld): Promise<void> {
  const token = await adminToken(world);
  const res = await world.page.request.post(`${ADMIN_URL}/api/admin/students/${world.student!.id}/credentials`,
    { data: {}, headers: { authorization: `Bearer ${token}` } });
  expect(res.status()).toBe(200);
  world.tempPassword = tempPasswordOf(await waitForMail(world, world.student!.email, CREDENTIALS_SUBJECT));
}

async function activateByApi(world: E2EWorld, password: string): Promise<void> {
  const login = await world.page.request.post(`${CLIENT_URL}/api/auth/login`,
    { data: { email: world.student!.email, password: world.tempPassword, app: 'client' } });
  expect(login.status()).toBe(200);
  const { activationToken } = (await login.json()) as { activationToken: string };
  const res = await world.page.request.post(`${CLIENT_URL}/api/auth/activate`,
    { data: { activationToken, password, confirmPassword: password } });
  expect(res.status()).toBe(200);
}

/** Ngày hôm nay theo giờ Việt Nam (UTC+7), dạng dd/MM/yyyy. */
function vnToday(): string {
  const d = new Date(Date.now() + 7 * 60 * 60 * 1000);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${pad(d.getUTCDate())}/${pad(d.getUTCMonth() + 1)}/${d.getUTCFullYear()}`;
}

// ---- features/hoc-vien/tai-khoan/thong-tin-dang-nhap.feature ----

// @BR-f01-p1-tai-khoan-hoc-vien-002
Given('một hồ sơ học viên đủ mục bắt buộc, chưa gửi thông tin đăng nhập lần nào', async function (this: E2EWorld) {
  await createStudent(this, 'quan@gmail.com', true);
});

When('coach gửi thông tin đăng nhập cho học viên đó', async function (this: E2EWorld) {
  await this.page.goto(`${ADMIN_URL}/hoc-vien/${this.student!.id}`);
  await this.page.getByRole('button', { name: 'Gửi thông tin đăng nhập', exact: true }).click();
  await expect(this.page.getByRole('button', { name: 'Gửi lại thông tin đăng nhập', exact: true })).toBeVisible();
});

Then('học viên đó nhận được email thông tin đăng nhập', async function (this: E2EWorld) {
  const mail = await waitForMail(this, this.student!.email, CREDENTIALS_SUBJECT);
  expect(mail.textBody).toContain(`${CLIENT_URL}/login`);
  expect(mail.textBody).toContain(this.student!.email);
  this.tempPassword = tempPasswordOf(mail);
});

Then('coach thấy tài khoản đã được gửi thông tin đăng nhập', async function (this: E2EWorld) {
  const card = this.page.getByRole('region', { name: 'Tài khoản đăng nhập' });
  await expect(card).toContainText(/Đã gửi lúc \d{2}\/\d{2} \d{2}:\d{2}, mật khẩu tạm còn hạn đến \d{2}\/\d{2} \d{2}:\d{2}/);
  await expect(card).toContainText('Gửi thông tin đăng nhập lần đầu.');
  await expect(this.page.locator('body')).not.toContainText(this.tempPassword); // @BR-f01-p1-tai-khoan-hoc-vien-025
});

// @BR-f01-p1-tai-khoan-hoc-vien-007
Given('một hồ sơ học viên với email {string}, tài khoản Chờ kích hoạt', async function (this: E2EWorld, email: string) {
  await createStudent(this, email, false);
});

Given('chưa có tài khoản nào dùng email {string}', function (this: E2EWorld, email: string) {
  realEmail(this, email); // hậu tố mới trong scenario này → chắc chắn chưa ai dùng
});

When('coach sửa email của học viên đó thành {string}', async function (this: E2EWorld, email: string) {
  const id = this.student!.id;
  await this.page.goto(`${ADMIN_URL}/hoc-vien/${id}/sua`);
  const field = this.page.getByLabel('Email đăng nhập');
  await expect(field).toHaveValue(this.student!.email);
  await field.fill(realEmail(this, email));
  await this.page.getByRole('button', { name: 'Lưu thay đổi', exact: true }).click();
  await expect(this.page).toHaveURL(new RegExp(`/hoc-vien/${id}$`));
});

Then('hồ sơ đó có email {string}', async function (this: E2EWorld, email: string) {
  await expect(this.page.locator('.meta')).toContainText(realEmail(this, email).trim().toLowerCase());
});

// ---- features/hoc-vien/tai-khoan/kich-hoat.feature ----

// @BR-f01-p1-tai-khoan-hoc-vien-011
Given('một học viên vừa được gửi mật khẩu tạm', async function (this: E2EWorld) {
  await createStudent(this, 'quan@gmail.com', true);
  await sendCredentialsByApi(this);
});

When('học viên đăng nhập web học viên bằng mật khẩu tạm đó', async function (this: E2EWorld) {
  await this.page.goto(`${CLIENT_URL}/login`);
  await this.login(this.page, this.student!.email, this.tempPassword);
});

Then('học viên được đưa tới bước đặt mật khẩu riêng', async function (this: E2EWorld) {
  await expect(this.page).toHaveURL(/\/kich-hoat$/);
  await expect(this.page.getByRole('heading', { level: 1, name: 'Đặt mật khẩu riêng' })).toBeVisible();
  expect(await this.page.evaluate(() => localStorage.getItem('dbx.client.rt'))).toBeNull(); // chưa có phiên
});

// @BR-f01-p1-tai-khoan-hoc-vien-016
// Compose chạy đồng hồ thật nên không đặt được "hôm nay" của server; ngày cụ thể trong feature được kiểm ở @api
// (Reqnroll, đồng hồ giả). Ở @web, "ngày kích hoạt" phải là hôm nay theo giờ Việt Nam.
Given('hôm nay là {string}', function (this: E2EWorld, _date: string) { /* xem chú thích trên */ });

Given('học viên đang đặt mật khẩu riêng bằng mật khẩu tạm còn hiệu lực', async function (this: E2EWorld) {
  await createStudent(this, 'quan@gmail.com', true);
  await sendCredentialsByApi(this);
  await this.page.goto(`${CLIENT_URL}/login`);
  await this.login(this.page, this.student!.email, this.tempPassword);
  await expect(this.page.getByRole('heading', { level: 1, name: 'Đặt mật khẩu riêng' })).toBeVisible();
});

When('học viên lưu mật khẩu riêng {string}', async function (this: E2EWorld, password: string) {
  await this.page.getByLabel('Mật khẩu mới', { exact: true }).fill(password);
  await this.page.getByLabel('Nhập lại mật khẩu', { exact: true }).fill(password);
  await this.page.getByRole('button', { name: 'Lưu mật khẩu và vào trang Hôm nay' }).click();
});

Then('coach thấy tài khoản của học viên đó Đã kích hoạt ngày {string}', async function (this: E2EWorld, _date: string) {
  const today = vnToday();
  const admin = await this.openPage(`${ADMIN_URL}/login`);
  await this.login(admin, ADMIN.email, ADMIN.password);
  await expect(admin).not.toHaveURL(/\/login/);
  await admin.goto(`${ADMIN_URL}/hoc-vien/${this.student!.id}`);
  await expect(admin.getByRole('region', { name: 'Tài khoản đăng nhập' })).toContainText(`Đã kích hoạt ngày ${today}`);
  await expect(admin.locator('.meta')).toContainText(`Kích hoạt ${today}`);
});

// ---- features/hoc-vien/tai-khoan/dat-lai-mat-khau.feature ----

// @BR-f01-p1-tai-khoan-hoc-vien-022
Given('học viên có tài khoản Đã kích hoạt với email {string}', async function (this: E2EWorld, email: string) {
  await createStudent(this, email, true);
  await sendCredentialsByApi(this);
  await activateByApi(this, OWN_PASSWORD);
});

When('một người xin đặt lại mật khẩu cho email {string}', async function (this: E2EWorld, email: string) {
  await this.page.goto(`${CLIENT_URL}/login`);
  await this.page.getByRole('button', { name: 'Quên mật khẩu?' }).click();
  await expect(this.page).toHaveURL(/\/quen-mat-khau$/);
  await this.page.getByLabel('Email').fill(realEmail(this, email));
  await this.page.getByRole('button', { name: 'Gửi link đặt lại' }).click();
  await expect(this.page.getByRole('status')).toHaveText(FORGOT_MESSAGE);
});

Then('email {string} nhận được email có link đặt lại mật khẩu', async function (this: E2EWorld, email: string) {
  const mail = await waitForMail(this, realEmail(this, email), RESET_SUBJECT);
  const link = /(https?:\/\/\S+\/dat-lai-mat-khau#t=[A-Za-z0-9_-]+)/.exec(mail.textBody)?.[1];
  expect(link, 'thư đặt lại phải có link /dat-lai-mat-khau#t=…').toBeTruthy();
  await this.page.goto(link!);
  await expect(this.page.getByRole('heading', { level: 1, name: 'Đặt lại mật khẩu' })).toBeVisible();
  await expect(this.page.getByRole('button', { name: 'Lưu mật khẩu mới' })).toBeVisible();
});
```

- [ ] **Step 3: Chạy — kỳ vọng PASS**

Run: `docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build --wait` (build lại `web` có Task 8/9) rồi `cd frontend && npm run e2e -- --tags "@web and (@BR-f01-p1-tai-khoan-hoc-vien-002 or @BR-f01-p1-tai-khoan-hoc-vien-007 or @BR-f01-p1-tai-khoan-hoc-vien-011 or @BR-f01-p1-tai-khoan-hoc-vien-016 or @BR-f01-p1-tai-khoan-hoc-vien-022)"`
Expected: `5 scenarios (5 passed)`.
Run: `cd frontend && npm run e2e`
Expected: không scenario nào failed/undefined (gồm `@web` F00 — đăng nhập học viên demo đã kích hoạt ở Task 5, so ảnh `màn đăng nhập` của web quản trị — và F02 BR-003, BR-024).

- [ ] **Step 4: Cập nhật CLAUDE.md**

`CLAUDE.md` — trong bảng "Page key", thay dòng:
```markdown
| `Main` | Đăng nhập, kích hoạt, quên mật khẩu | cả 2 | `/login`, `/activate`, `/forgot-password` |
```
bằng:
```markdown
| `Main` | Đăng nhập, kích hoạt, quên mật khẩu, đặt lại mật khẩu | cả 2 (3 màn sau chỉ client) | `/login`, `/kich-hoat`, `/quen-mat-khau`, `/dat-lai-mat-khau#t=<token>` |
```
và ngay sau đoạn "Route web admin dùng tiếng Việt (ADR-f02-ho-so-hoc-vien-008): …" thêm:
```markdown
Route web học viên dùng tiếng Việt (ADR-f01-p1-tai-khoan-hoc-vien-010): đã có `/login`, `/kich-hoat` (mã kích hoạt chỉ trong bộ nhớ), `/quen-mat-khau`, `/dat-lai-mat-khau#t=<token>` (token sau `#`), `/hom-nay`. Route học viên chưa làm trong bảng vẫn là gợi ý — khi làm, đặt tiếng Việt.
```
Trong khối "Lệnh thường dùng", ngay sau dòng `# admin: http://localhost:4200   client: http://localhost:4300` thêm:
```bash
# compose dùng Email__Provider=Capture: thư gửi đi (mật khẩu tạm, link đặt lại) đọc ở http://localhost:8080/api/test/emails?to=<email>
```

- [ ] **Step 5: Nghiệm thu toàn bộ F01-P1**

Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: không Failed (mọi scenario `@api` của `features/hoc-vien/tai-khoan/*.feature` xanh).
Run: `cd frontend && npm run test:ci && npm run e2e`
Expected: không Failed.
Kiểm tay (ghi kết quả vào mô tả commit/MR, không chặn commit):
- So 4 màn web học viên (Đăng nhập, Kích hoạt, Quên mật khẩu, Đặt lại mật khẩu) với page `Main` và 3 màn admin (Tạo hồ sơ, Hồ sơ, Sửa hồ sơ) với `AdTaoHoSo`/`AdHoSo` ở 390px và 1280px.
- NFR-f01-p1-tai-khoan-hoc-vien-004: đo p95 của `POST /api/auth/login`, `/api/auth/activate`, `/api/auth/forgot-password`, `POST /api/admin/students/{id}/credentials` < 500 ms (không tính thời gian gọi Resend).
- NFR-f01-p1-tai-khoan-hoc-vien-005: gửi coach 2 thư mẫu (đọc ở `/api/test/emails`) để duyệt nội dung trước khi bật Resend.

- [ ] **Step 6: Commit**
```bash
git add frontend/e2e/steps/account.steps.ts frontend/e2e/steps/student-profile.steps.ts frontend/e2e/support/world.ts deploy/docker-compose.yml CLAUDE.md
git commit -F - <<'EOF'
test(e2e): student account web scenarios (BR-f01-p1-002, 007, 011, 016, 022); compose captures email

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```
