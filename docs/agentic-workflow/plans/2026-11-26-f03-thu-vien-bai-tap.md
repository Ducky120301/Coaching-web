---
doc-id: 2026-11-26-f03-thu-vien-bai-tap
status: draft
---

# F03 — Thư viện bài tập: Implementation Plan

> Frontmatter `status: draft | ready` là gate (exec-plan yêu cầu spec + tech-design + plan đều `ready`). User mark Ready sau khi review.

> **For agentic workers:** REQUIRED SUB-SKILL: Use `exec-plan` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Coach có thư viện bài tập riêng (thêm, sửa, ẩn, xoá, tìm, lọc; danh mục nhóm cơ/dụng cụ thêm được) để F05 chọn bài khi soạn giáo án (BR-f03-thu-vien-bai-tap-001→021).

**Architecture:** Aggregate `Exercise` + `ExerciseCategory` theo coach, chặn trùng bằng `NameKey` chuẩn hoá + unique index, liều mặc định có cấu trúc, video chỉ nhận link YouTube. Xoá đi qua điểm cắm `IExerciseUsage` (P1 luôn "chưa dùng", F05 nối vào). Web admin thêm màn Thư viện (theo `AdThuVien`) và màn Thêm/Sửa bài; 99 bài của coach được nạp sẵn ở mọi môi trường.

**Tech Stack:** .NET 10, EF Core 10, Liquibase 4.33.0, Reqnroll.xUnit, Testcontainers.MsSql · Angular 22 (zoneless), Vitest · @cucumber/cucumber + Playwright.

## Global Constraints

- Làm trên nhánh `main`, không worktree (quyết định của user). Không push.
- Dependency rule: `Api → Application → Domain`; `Infrastructure → Application, Domain`; Domain không tham chiếu EF/ASP.NET.
- Không gọi `DateTime.Now`/`DateTime.UtcNow` trong Domain/Application; thời gian qua `IClock`.
- Schema chỉ đổi qua Liquibase `database/changelog/v0.5.0/`; KHÔNG dùng `dotnet ef migrations`. Không thêm package mới.
- Giới hạn (nguyên văn spec): tên bài ≤ 100 ký tự; Sets 1–10; Reps 1–100 (số hoặc khoảng thấp–cao, thấp ≤ cao); RIR 0–5 (số hoặc khoảng, thấp ≤ cao); nghỉ 15–600 giây; hướng dẫn kỹ thuật ≤ 2000; lỗi hay gặp ≤ 2000. Mọi liều và ghi chú không bắt buộc.
- Trùng bài theo (tên, dụng cụ) trong thư viện của coach, kể cả bài đang ẩn; bỏ qua hoa/thường, dấu tiếng Việt, khoảng trắng thừa, kiểu dấu gạch (‐ - – —). Danh mục so trùng cùng cách.
- Danh mục ban đầu: nhóm cơ "Ngực, Vai, Tay sau, Tay trước, Lưng, Vai sau, Đùi trước, Đùi sau, Mông, Bắp chân, Bụng"; dụng cụ "Machine, Cable, Dumbbell, Barbell, Smith Machine, Kettlebell, Bodyweight" — có ở mọi môi trường. Coach chỉ thêm được danh mục (không đổi tên/xoá).
- Link video: không bắt buộc; có thì phải là http/https với host youtube.com, youtu.be hoặc *.youtube.com. FE chỉ nhúng qua `youtube-nocookie.com/embed/<id>` khi rút được mã 11 ký tự.
- Bài đã ẩn: không có trong danh sách/tìm mặc định; xem được khi chọn bài đã ẩn. Xoá bài đang được dùng (qua `IExerciseUsage`) → 409 `exercise.in_use`.
- Mã lỗi và câu chữ theo bảng API Contracts của tech-design, nguyên văn.
- 99 bài prototype (gán theo Phụ lục tech-design) là thư viện thật của coach: nạp ở mọi môi trường, chỉ khi thư viện của coach trống (không nạp lại, không ghi đè); host test tắt bằng `Seed:ExerciseLibrary=false`.
- `NFR-f03-thu-vien-bai-tap-001`: danh sách/tìm/lọc p95 < 500 ms với 500 bài (kiểm tay ở Task 8).
- Route admin tiếng Việt: `/thu-vien/bai-tap`, `/thu-vien/bai-tap/moi`, `/thu-vien/bai-tap/:id/sua`; rail có mục "Thư viện bài tập".
- FE bám **chính xác** prototype `docs/duobexiuuu-coaching-design.html` page `AdThuVien` (H1 46px); breakpoint 880px; không hard-code màu; kiểm ở 390px và 1280px. Phần UI suy ra theo mục "Thiết kế giao diện (UI)" của tech-design (PO đã duyệt).
- Living doc `features/thu-vien/bai-tap/thu-vien-bai-tap.feature` đã chốt — KHÔNG sửa. `@api` chạy Reqnroll, `@web` chạy CucumberJS, `@manual` không chạy.
- `reqnroll.json` `missingOrPendingStepsOutcome: Error`: scenario của task sau fail `MissingStepDefinition` cho tới khi được bind; mọi lỗi khác phải sửa.
- Annotation truy vết: `// @BR-f03-thu-vien-bai-tap-<số>` / `// @ADR-f03-thu-vien-bai-tap-<số>` tại code thực thi và trên test (SQL `-- @…`, TS `// @…`).
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
| 1 | Danh mục: schema v0.5.0, danh mục ban đầu, API danh mục | BR-005, 006; ADR-002, 003 |
| 2 | Bài tập: thêm, sửa (version), xem; chặn trùng; tên/nhóm cơ/dụng cụ | BR-001–004, 020, 021; ADR-001 |
| 3 | Video, liều mặc định, hướng dẫn, lỗi hay gặp | BR-007–014; ADR-005, 006 |
| 4 | Ẩn, bỏ ẩn, xoá + điểm cắm "đang được dùng" | BR-015, 017; ADR-004 |
| 5 | Danh sách, tìm, lọc, xem bài đã ẩn + nạp thư viện 99 bài | BR-016, 018, 019; ADR-007 |
| 6 | [UI] Thư viện bài tập | — |
| 7 | [UI] Thêm / Sửa bài | — |
| 8 | e2e `@web` + CLAUDE.md | — |

---

### Task 1: Danh mục — schema Liquibase v0.5.0 (2 bảng), danh mục ban đầu lúc khởi động, API `GET/POST /api/admin/exercise-categories`

**Files:**
- Create: `database/changelog/v0.5.0/changelog.xml`, `database/changelog/v0.5.0/0001-exercise-library.xml`, `database/changelog/v0.5.0/scripts/0001-exercise-library.sql`, `database/changelog/v0.5.0/rollback/0001-exercise-library.rollback.sql`
- Modify: `database/changelog/db.changelog-master.xml`
- Create: `backend/src/Coaching.Domain/Exercises/CategoryKind.cs`, `ExerciseNameKey.cs`, `DefaultCatalog.cs`, `ExerciseCategory.cs`, `IExerciseCategoryRepository.cs`
- Create: `backend/src/Coaching.Application/Common/UniqueConstraintException.cs`
- Create: `backend/src/Coaching.Application/Exercises/ExerciseErrors.cs`, `Exercises/Queries/ListCategories.cs`, `Exercises/Commands/AddCategory.cs`
- Modify: `backend/src/Coaching.Application/DependencyInjection.cs`
- Create: `backend/src/Coaching.Infrastructure/Persistence/Configurations/ExerciseCategoryConfiguration.cs`, `Persistence/Repositories/ExerciseCategoryRepository.cs`, `Seeding/ExerciseCatalogSeeder.cs`
- Modify: `backend/src/Coaching.Infrastructure/Persistence/AppDbContext.cs`, `DependencyInjection.cs`, `Seeding/DatabaseInitializer.cs`
- Create: `backend/src/Coaching.Api/Controllers/Admin/ExerciseCategoriesController.cs`
- Test: `backend/tests/Coaching.Domain.Tests/Exercises/ExerciseCatalogTests.cs` (mới)
- Test: `backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseRequests.cs` (mới, helper dùng chung cho Task 1–5), `Exercises/ExerciseSchemaTests.cs`, `Exercises/ExerciseCategoryTests.cs` (mới)
- Acceptance: `backend/tests/Coaching.Acceptance/Support/ExerciseCatalogApi.cs`, `backend/tests/Coaching.Acceptance/Steps/ExerciseCategorySteps.cs` (mới)
- Living docs bind: `features/thu-vien/bai-tap/thu-vien-bai-tap.feature` — Rule `@BR-f03-thu-vien-bai-tap-005`, `@BR-f03-thu-vien-bai-tap-006`. KHÔNG sửa `.feature`.

**Interfaces:**
- Consumes: `AggregateRoot<Guid>`, `DomainException(code, message)`, `SearchText.Normalize` (ADR-f02-ho-so-hoc-vien-003), `CurrentCoach.GetAsync`, `StudentErrors.CoachNotFound`, `Result<T>`/`Error`/`ErrorKind`, `IUnitOfWork.SaveChangesAsync`, `AdminControllerBase`, `ResultExtensions.ToActionResult/ToProblem`, `DatabaseInitializer.InitializeAsync`, `ApiFactory` (+ `AdminClientAsync`, `LoginAsync`, `CreateStudentAsync`), `StudentRequests.ErrorCodeAsync`, `AcceptanceContext.SendJsonAsync/Client`, `StudentApi.AdminToken/ErrorCodeAsync`, binding có sẵn `StudentCreateSteps.CoachLoggedIn` (Background).
- Produces:
  - Domain `Coaching.Domain.Exercises`: `enum CategoryKind { MuscleGroup, Equipment }`; `ExerciseNameKey.For(string? name)`; `DefaultCatalog.MuscleGroups` (11) / `DefaultCatalog.Equipment` (7); `ExerciseCategory.Create(Guid coachId, CategoryKind kind, string? name, int sortOrder)` (+ `CoachId`, `Kind`, `Name`, `NameKey`, `SortOrder`, `NameMaxLength = 100`); `IExerciseCategoryRepository { ListAsync, GetAsync, ExistsAsync, Add }`.
  - Application: `UniqueConstraintException` (IUnitOfWork ném khi vi phạm chỉ mục duy nhất 2601/2627); `ExerciseErrors.CategoryDuplicate` (409 `category.duplicate` "Đã có mục cùng tên."), `ExerciseErrors.CategoryKindInvalid` (400 `category.kind_invalid`); `CategoryDto(Guid Id, string Name)`, `CategoriesDto(IReadOnlyList<CategoryDto> MuscleGroups, IReadOnlyList<CategoryDto> Equipment)`, `ListCategoriesHandler.HandleAsync(ct)`; `AddCategoryCommand(string? Kind, string? Name)`, `CategoryCreatedDto(Guid Id, string Kind, string Name)`, `AddCategoryHandler.HandleAsync(command, ct)`.
  - Infrastructure: `AppDbContext.ExerciseCategories`; `ExerciseCatalogSeeder.SeedAsync(AppDbContext db, CancellationToken ct = default)` (public — mọi coach chưa có danh mục, mọi môi trường, idempotent).
  - Api: `GET /api/admin/exercise-categories` → 200 `CategoriesDto` (theo `SortOrder`); `POST /api/admin/exercise-categories` `{ kind, name }` → 201 `CategoryCreatedDto` | 400 `category.kind_invalid` / `category.name_required` / `category.name_too_long` | 409 `category.duplicate`.
  - DB: Liquibase v0.5.0 tạo `ExerciseCategories` VÀ `Exercises` (đủ cột cho Task 2–5) + tag `v0.5.0`.
  - Test support: `ExerciseRequests` (integration: `Url`, `CategoriesUrl`, `CategoriesAsync`, `NewCoachAsync(factory, withCatalog)`, `StudentClientAsync`, `AdminClientAsync(factory, host)`, `WithRace<T>`, `Race`); `ExerciseCatalogApi` (acceptance: `GetAsync`, `TryAddAsync`, `MuscleGroupIdAsync`, `EquipmentIdAsync`).

**Owns:** `@BR-f03-thu-vien-bai-tap-005`, `@BR-f03-thu-vien-bai-tap-006` | `@ADR-f03-thu-vien-bai-tap-002`, `@ADR-f03-thu-vien-bai-tap-003`

Ghi chú bind step (đã soát mọi `[Given|When|Then]` trong `backend/tests/Coaching.Acceptance/Steps/*.cs` — không trùng):
| Step trong living doc | Binding |
|---|---|
| `coach "Coach Duobexiuuu" đã đăng nhập` (Background) | có sẵn `StudentCreateSteps.CoachLoggedIn` → `StudentApi.AdminToken` |
| `coach xem danh mục của thư viện bài tập` | **mới** `ExerciseCategorySteps.ViewCatalog` |
| `danh mục nhóm cơ gồm "…"` / `danh mục dụng cụ gồm "…"` | **mới** `MuscleGroupsAre` / `EquipmentIs` |
| `coach đã thêm dụng cụ "…"` | **mới** `EquipmentAdded` |
| `coach thêm nhóm cơ "…"` | **mới** `AddMuscleGroup` |
| `nhóm cơ không được thêm vì đã có "…"` | **mới** `MuscleGroupDuplicate` |
| `coach thêm bài "…" dụng cụ "…" nhóm cơ "…"` / `thư viện có bài "…" dụng cụ "…"` (scenario "Thêm dụng cụ mới rồi dùng ngay cho bài mới") | bind ở **Task 2** (`ExerciseSteps`) — tới lúc đó scenario này fail `No matching step definition found` (interim rule) |

- [ ] **Step 1: Viết Domain test cho danh mục ban đầu, khoá tên và `ExerciseCategory`**

`backend/tests/Coaching.Domain.Tests/Exercises/ExerciseCatalogTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Exercises;

namespace Coaching.Domain.Tests.Exercises;

public sealed class ExerciseCatalogTests
{
    private static readonly Guid CoachId = Guid.NewGuid();

    private static ExerciseCategory Category(string? name, CategoryKind kind = CategoryKind.MuscleGroup) =>
        ExerciseCategory.Create(CoachId, kind, name, 1);

    // @BR-f03-thu-vien-bai-tap-005: 11 nhóm cơ và 7 dụng cụ, đúng thứ tự của spec
    [Fact]
    public void BR005_Default_catalog_has_11_muscle_groups_and_7_equipment_in_spec_order()
    {
        Assert.Equal(
            new[] { "Ngực", "Vai", "Tay sau", "Tay trước", "Lưng", "Vai sau", "Đùi trước", "Đùi sau", "Mông", "Bắp chân", "Bụng" },
            DefaultCatalog.MuscleGroups.ToArray());
        Assert.Equal(
            new[] { "Machine", "Cable", "Dumbbell", "Barbell", "Smith Machine", "Kettlebell", "Bodyweight" },
            DefaultCatalog.Equipment.ToArray());
    }

    // @ADR-f03-thu-vien-bai-tap-002 @BR-f03-thu-vien-bai-tap-006: hoa/thường, dấu, khoảng trắng thừa, kiểu dấu gạch không làm khác khoá
    [Theory]
    [InlineData("Lateral Raise", "lateral raise")]
    [InlineData("  lateral   raise  ", "lateral raise")]
    [InlineData("Ép ngực", "ep nguc")]
    [InlineData("Ep nguc", "ep nguc")]
    [InlineData("Đùi trước", "dui truoc")]
    [InlineData("T–Bar Row", "t-bar row")]
    [InlineData("T—Bar Row", "t-bar row")]
    [InlineData("T‐Bar Row", "t-bar row")]
    [InlineData("T-Bar Row", "t-bar row")]
    [InlineData("Lat\tPulldown", "lat pulldown")]
    [InlineData("45° Incline Chest Fly", "45° incline chest fly")]
    public void ADR002_Name_key_ignores_case_marks_extra_spaces_and_dash_style(string name, string key) =>
        Assert.Equal(key, ExerciseNameKey.For(name));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ADR002_Name_key_of_blank_is_empty(string? name) => Assert.Equal(string.Empty, ExerciseNameKey.For(name));

    // @BR-f03-thu-vien-bai-tap-006 @ADR-f03-thu-vien-bai-tap-003
    [Fact]
    public void BR006_Category_keeps_trimmed_name_kind_order_and_key()
    {
        var category = ExerciseCategory.Create(CoachId, CategoryKind.Equipment, "  Resistance   Band ", 8);

        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Equal(CoachId, category.CoachId);
        Assert.Equal(CategoryKind.Equipment, category.Kind);
        Assert.Equal("Resistance   Band", category.Name);
        Assert.Equal("resistance band", category.NameKey);
        Assert.Equal(8, category.SortOrder);
    }

    // @BR-f03-thu-vien-bai-tap-006: "ngực" trùng "Ngực" (so theo khoá)
    [Fact]
    public void BR006_Names_differing_only_by_case_marks_or_spaces_share_a_key() =>
        Assert.Equal(Category("Ngực").NameKey, Category("  NGUC ").NameKey);

    // @BR-f03-thu-vien-bai-tap-006: tên danh mục trống → không thêm
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BR006_Blank_category_name_is_rejected(string? name) =>
        Assert.Equal("category.name_required", Assert.Throws<DomainException>(() => Category(name)).Code);

    [Fact]
    public void Category_name_is_at_most_100_characters()
    {
        Assert.Equal(100, Category(new string('a', 100)).Name.Length);
        Assert.Equal("category.name_too_long",
            Assert.Throws<DomainException>(() => Category(new string('a', 101))).Code);
    }
}
```

- [ ] **Step 2: Chạy — kỳ vọng FAIL (đỏ)**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Exercises"`
Expected: FAIL — build error `CS0234: The type or namespace name 'Exercises' does not exist in the namespace 'Coaching.Domain'`.

- [ ] **Step 3: Domain — `CategoryKind`, `ExerciseNameKey`, `DefaultCatalog`, `ExerciseCategory`, `IExerciseCategoryRepository`**

`backend/src/Coaching.Domain/Exercises/CategoryKind.cs`:
```csharp
namespace Coaching.Domain.Exercises;

/// <summary>@ADR-f03-thu-vien-bai-tap-003: nhóm cơ và dụng cụ là hai loại của cùng một bảng danh mục.</summary>
public enum CategoryKind
{
    MuscleGroup,
    Equipment,
}
```

`backend/src/Coaching.Domain/Exercises/ExerciseNameKey.cs`:
```csharp
using System.Text;
using Coaching.Domain.Students;

namespace Coaching.Domain.Exercises;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-002: một định nghĩa "trùng" cho bài tập và danh mục — SearchText.Normalize (bỏ dấu, đ → d,
/// lowercase, trim) + đồng nhất mọi kiểu dấu gạch (‐ ‑ ‒ – — ― −) thành "-" + gộp khoảng trắng liên tiếp thành một.
/// Dùng cho Exercise.NameKey/SearchKey và ExerciseCategory.NameKey.
/// </summary>
public static class ExerciseNameKey
{
    public static string For(string? name)
    {
        var normalized = SearchText.Normalize(name);
        var builder = new StringBuilder(normalized.Length);
        var pendingSpace = false;
        foreach (var c in normalized)
        {
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }
            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }
            builder.Append(IsDash(c) ? '-' : c);
        }
        return builder.ToString();
    }

    private static bool IsDash(char c) => c is '‐' or '‑' or '‒' or '–' or '—' or '―' or '−';
}
```

`backend/src/Coaching.Domain/Exercises/DefaultCatalog.cs`:
```csharp
namespace Coaching.Domain.Exercises;

/// <summary>@BR-f03-thu-vien-bai-tap-005: danh mục ban đầu của mọi coach, đúng thứ tự spec (thứ tự hiển thị = SortOrder).</summary>
public static class DefaultCatalog
{
    public static IReadOnlyList<string> MuscleGroups { get; } =
    [
        "Ngực", "Vai", "Tay sau", "Tay trước", "Lưng", "Vai sau", "Đùi trước", "Đùi sau", "Mông", "Bắp chân", "Bụng",
    ];

    public static IReadOnlyList<string> Equipment { get; } =
    [
        "Machine", "Cable", "Dumbbell", "Barbell", "Smith Machine", "Kettlebell", "Bodyweight",
    ];
}
```

`backend/src/Coaching.Domain/Exercises/ExerciseCategory.cs`:
```csharp
using Coaching.Domain.Common;

namespace Coaching.Domain.Exercises;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-003: một mục danh mục (nhóm cơ hoặc dụng cụ) của coach. Duy nhất theo (CoachId, Kind, NameKey).
/// MVP chỉ thêm mới — không đổi tên, không xoá (spec §5).
/// </summary>
public sealed class ExerciseCategory : AggregateRoot<Guid>
{
    public const int NameMaxLength = 100;
    public const int NameKeyMaxLength = 200;

    public Guid CoachId { get; private set; }

    public CategoryKind Kind { get; private set; }

    public string Name { get; private set; } = default!;

    /// <summary>@ADR-f03-thu-vien-bai-tap-002: khoá so trùng (ExerciseNameKey).</summary>
    public string NameKey { get; private set; } = default!;

    public int SortOrder { get; private set; }

    private ExerciseCategory() { }

    private ExerciseCategory(Guid id) : base(id) { }

    // @BR-f03-thu-vien-bai-tap-006: tên bắt buộc (sau trim), tối đa 100 ký tự
    public static ExerciseCategory Create(Guid coachId, CategoryKind kind, string? name, int sortOrder)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new DomainException("category.name_required", "Nhập tên mục.");
        if (trimmed.Length > NameMaxLength)
            throw new DomainException("category.name_too_long", $"Tên mục tối đa {NameMaxLength} ký tự.");

        return new ExerciseCategory(Guid.CreateVersion7())
        {
            CoachId = coachId,
            Kind = kind,
            Name = trimmed,
            NameKey = ExerciseNameKey.For(trimmed),
            SortOrder = sortOrder,
        };
    }
}
```

`backend/src/Coaching.Domain/Exercises/IExerciseCategoryRepository.cs`:
```csharp
namespace Coaching.Domain.Exercises;

public interface IExerciseCategoryRepository
{
    /// <summary>Mọi mục của coach (cả hai loại), theo SortOrder.</summary>
    Task<IReadOnlyList<ExerciseCategory>> ListAsync(Guid coachId, CancellationToken ct = default);

    /// <summary>Mục thuộc coach; mục của coach khác coi như không tồn tại.</summary>
    Task<ExerciseCategory?> GetAsync(Guid id, Guid coachId, CancellationToken ct = default);

    /// <summary>@BR-f03-thu-vien-bai-tap-006: đã có mục cùng loại, cùng khoá tên.</summary>
    Task<bool> ExistsAsync(Guid coachId, CategoryKind kind, string nameKey, CancellationToken ct = default);

    void Add(ExerciseCategory c);
}
```

- [ ] **Step 4: Chạy Domain test — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Exercises"`
Expected: PASS — `ExerciseCatalogTests` `Passed: 21` (1 + 11 + 3 + 1 + 1 + 3 + 1), `Failed: 0`.

- [ ] **Step 5: Liquibase v0.5.0 — 2 bảng thư viện bài tập, rollback, changelog, master**

`database/changelog/v0.5.0/scripts/0001-exercise-library.sql`:
```sql
-- v0.5.0-0001: thư viện bài tập (F03) — danh mục nhóm cơ/dụng cụ + bài tập
-- @ADR-f03-thu-vien-bai-tap-003: nhóm cơ và dụng cụ chung một bảng, phân biệt bằng Kind; chỉ có tên
CREATE TABLE [ExerciseCategories] (
    [Id] uniqueidentifier NOT NULL,
    [CoachId] uniqueidentifier NOT NULL,
    [Kind] nvarchar(20) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [NameKey] nvarchar(200) NOT NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_ExerciseCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExerciseCategories_Coaches_CoachId] FOREIGN KEY ([CoachId]) REFERENCES [Coaches] ([Id]) ON DELETE NO ACTION
);
GO
-- @BR-f03-thu-vien-bai-tap-006 @ADR-f03-thu-vien-bai-tap-002: mục cùng loại không trùng khoá tên (chốt chặn cuối khi hai request đua)
CREATE UNIQUE INDEX [IX_ExerciseCategories_CoachId_Kind_NameKey] ON [ExerciseCategories] ([CoachId], [Kind], [NameKey]);
GO
-- @ADR-f03-thu-vien-bai-tap-001: bài tập; liều mặc định là các cột int NULL (@ADR-f03-thu-vien-bai-tap-006); Version chặn ghi đè
CREATE TABLE [Exercises] (
    [Id] uniqueidentifier NOT NULL,
    [CoachId] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [NameKey] nvarchar(200) NOT NULL,
    [SearchKey] nvarchar(200) NOT NULL,
    [MuscleGroupId] uniqueidentifier NOT NULL,
    [EquipmentId] uniqueidentifier NOT NULL,
    [VideoUrl] nvarchar(2048) NULL,
    [DefaultSets] int NULL,
    [DefaultRepsMin] int NULL,
    [DefaultRepsMax] int NULL,
    [DefaultRirMin] int NULL,
    [DefaultRirMax] int NULL,
    [DefaultRestSeconds] int NULL,
    [TechniqueNotes] nvarchar(2000) NULL,
    [CommonMistakes] nvarchar(2000) NULL,
    [IsHidden] bit NOT NULL CONSTRAINT [DF_Exercises_IsHidden] DEFAULT 0,
    [Version] rowversion NOT NULL,
    CONSTRAINT [PK_Exercises] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Exercises_Coaches_CoachId] FOREIGN KEY ([CoachId]) REFERENCES [Coaches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Exercises_ExerciseCategories_MuscleGroupId] FOREIGN KEY ([MuscleGroupId]) REFERENCES [ExerciseCategories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Exercises_ExerciseCategories_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [ExerciseCategories] ([Id]) ON DELETE NO ACTION
);
GO
-- @BR-f03-thu-vien-bai-tap-001 @ADR-f03-thu-vien-bai-tap-001: duy nhất theo (tên chuẩn hoá, dụng cụ) trong thư viện của coach, kể cả bài đang ẩn
CREATE UNIQUE INDEX [IX_Exercises_CoachId_NameKey_EquipmentId] ON [Exercises] ([CoachId], [NameKey], [EquipmentId]);
GO
-- Danh sách bài: 1 truy vấn theo (CoachId, IsHidden) + LIKE trên SearchKey (NFR-f03-thu-vien-bai-tap-001)
CREATE INDEX [IX_Exercises_CoachId_IsHidden] ON [Exercises] ([CoachId], [IsHidden]);
GO
CREATE INDEX [IX_Exercises_MuscleGroupId] ON [Exercises] ([MuscleGroupId]);
GO
CREATE INDEX [IX_Exercises_EquipmentId] ON [Exercises] ([EquipmentId]);
GO
```

`database/changelog/v0.5.0/rollback/0001-exercise-library.rollback.sql`:
```sql
-- Rollback v0.5.0-0001: xoá bảng theo thứ tự ngược khoá ngoại
DROP TABLE IF EXISTS [Exercises];
GO
DROP TABLE IF EXISTS [ExerciseCategories];
GO
```

`database/changelog/v0.5.0/0001-exercise-library.xml`:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog http://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-4.33.xsd">
  <!-- @ADR-f03-thu-vien-bai-tap-001 @ADR-f03-thu-vien-bai-tap-003: bảng ExerciseCategories + Exercises -->
  <changeSet id="v0.5.0-0001-exercise-library" author="duc.hm4@kiotviet.com">
    <comment>F03: thư viện bài tập — ExerciseCategories (nhóm cơ, dụng cụ) và Exercises (liều mặc định có cấu trúc, rowversion)</comment>
    <sqlFile path="scripts/0001-exercise-library.sql" relativeToChangelogFile="true" splitStatements="true" endDelimiter="\nGO" stripComments="false"/>
    <rollback>
      <sqlFile path="rollback/0001-exercise-library.rollback.sql" relativeToChangelogFile="true" splitStatements="true" endDelimiter="\nGO"/>
    </rollback>
  </changeSet>
</databaseChangeLog>
```

`database/changelog/v0.5.0/changelog.xml`:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!-- v0.5.0 = F03 thư viện bài tập. Thêm changeset mới TRƯỚC changeset tag. -->
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog http://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-4.33.xsd">
  <include file="0001-exercise-library.xml" relativeToChangelogFile="true"/>

  <changeSet id="v0.5.0-tag" author="duc.hm4@kiotviet.com">
    <tagDatabase tag="v0.5.0"/>
  </changeSet>
</databaseChangeLog>
```

`database/changelog/db.changelog-master.xml` — thêm dòng include sau v0.4.0:
```xml
  <include file="v0.4.0/changelog.xml" relativeToChangelogFile="true"/>
  <include file="v0.5.0/changelog.xml" relativeToChangelogFile="true"/>
</databaseChangeLog>
```

- [ ] **Step 6: Helper integration test dùng chung `ExerciseRequests` + test schema/API danh mục**

`backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseRequests.cs`:
```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Auth;
using Coaching.Application.Common;
using Coaching.Application.Exercises.Queries;
using Coaching.Domain.Coaches;
using Coaching.Domain.Common;
using Coaching.Infrastructure.Identity;
using Coaching.Infrastructure.Persistence;
using Coaching.Infrastructure.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Exercises;

internal static class ExerciseRequests
{
    public const string Url = "/api/admin/exercises";
    public const string CategoriesUrl = "/api/admin/exercise-categories";

    public static async Task<CategoriesDto> CategoriesAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<CategoriesDto>(CategoriesUrl))!;

    /// <summary>
    /// Coach mới (tài khoản Admin + hồ sơ coach). <paramref name="withCatalog"/> = true: tạo luôn danh mục ban đầu
    /// (giống lần khởi động kế tiếp); false: coach chưa có danh mục nào.
    /// </summary>
    public static async Task<(Guid CoachId, HttpClient Client)> NewCoachAsync(ApiFactory factory, bool withCatalog = true)
    {
        var email = $"coach-{Guid.NewGuid():N}@test.local";
        const string password = "Coach@12345";
        Guid coachId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var sp = scope.ServiceProvider;
            var users = sp.GetRequiredService<UserManager<AppUser>>();
            var user = new AppUser { UserName = email, Email = email, FullName = "Coach khác", EmailConfirmed = true };
            Assert.True((await users.CreateAsync(user, password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, Roles.Admin)).Succeeded);
            var db = sp.GetRequiredService<AppDbContext>();
            var coach = Coach.Create(user.Id, "Coach khác", Email.Create(email));
            db.Coaches.Add(coach);
            await db.SaveChangesAsync();
            coachId = coach.Id;
            if (withCatalog)
                await ExerciseCatalogSeeder.SeedAsync(db);
        }
        var tokens = await factory.LoginAsync(email, password, ClientApp.Admin);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return (coachId, client);
    }

    public static async Task<HttpClient> StudentClientAsync(ApiFactory factory)
    {
        var (email, password) = await factory.CreateStudentAsync();
        var tokens = await factory.LoginAsync(email, password);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    /// <summary>Client của admin seed gọi vào <paramref name="host"/> (host phụ dựng từ <paramref name="factory"/>).</summary>
    public static async Task<HttpClient> AdminClientAsync(ApiFactory factory, WebApplicationFactory<Program> host)
    {
        var tokens = await factory.LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    /// <summary>
    /// Host phụ: ngay trước khi lưu một entity <typeparamref name="T"/> mới khớp <paramref name="match"/> (bước kiểm trùng
    /// đã qua), một request khác — scope + kết nối riêng, commit ngay — chạy <paramref name="competitor"/> đúng một lần.
    /// Lệnh INSERT của request chính vì vậy đụng chỉ mục duy nhất (2601).
    /// </summary>
    public static WebApplicationFactory<Program> WithRace<T>(ApiFactory factory, Race race, Func<T, bool> match,
        Func<IServiceProvider, T, Task> competitor) where T : class =>
        factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
            services.ConfigureDbContext<AppDbContext>((sp, options) => options.AddInterceptors(
                new RaceInterceptor<T>(race, match, competitor, sp.GetRequiredService<IServiceScopeFactory>())))));

    private sealed class RaceInterceptor<T>(Race race, Func<T, bool> match, Func<IServiceProvider, T, Task> competitor,
        IServiceScopeFactory scopes) : SaveChangesInterceptor where T : class
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var entity = eventData.Context!.ChangeTracker.Entries<T>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .FirstOrDefault(match);
            if (entity is not null && race.TryClaim())
            {
                await using var scope = scopes.CreateAsyncScope();
                await competitor(scope.ServiceProvider, entity);
            }
            return result;
        }
    }
}

/// <summary>Trạng thái cuộc đua dùng chung giữa các DbContext của host phụ: chỉ chạy đối thủ một lần.</summary>
internal sealed class Race
{
    private int _claimed;

    public bool Happened => Volatile.Read(ref _claimed) == 1;

    public bool TryClaim() => Interlocked.Exchange(ref _claimed, 1) == 0;
}
```

`backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseSchemaTests.cs`:
```csharp
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Exercises;

[Collection(ApiCollection.Name)]
public sealed class ExerciseSchemaTests(ApiFactory factory)
{
    // @ADR-f03-thu-vien-bai-tap-003 @ADR-f03-thu-vien-bai-tap-001: changeset v0.5.0 tạo đủ 2 bảng, cột và chỉ mục duy nhất
    [Fact]
    public async Task Changelog_v0_5_0_creates_exercise_library_tables()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(1, await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [DATABASECHANGELOG] WHERE [ID] = {"v0.5.0-0001-exercise-library"}")
            .SingleAsync());

        async Task<string[]> ColumnsOf(string table) => (await db.Database
            .SqlQuery<string>($"SELECT COLUMN_NAME AS [Value] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = {table}")
            .ToListAsync()).Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(new[] { "CoachId", "Id", "Kind", "Name", "NameKey", "SortOrder" }, await ColumnsOf("ExerciseCategories"));
        Assert.Equal(
            new[]
            {
                "CoachId", "CommonMistakes", "DefaultRepsMax", "DefaultRepsMin", "DefaultRestSeconds", "DefaultRirMax",
                "DefaultRirMin", "DefaultSets", "EquipmentId", "Id", "IsHidden", "MuscleGroupId", "Name", "NameKey",
                "SearchKey", "TechniqueNotes", "Version", "VideoUrl",
            },
            await ColumnsOf("Exercises"));

        var unique = (await db.Database.SqlQuery<string>(
                $"SELECT [name] AS [Value] FROM sys.indexes WHERE [is_unique] = 1 AND [is_primary_key] = 0 AND [object_id] IN (OBJECT_ID(N'ExerciseCategories'), OBJECT_ID(N'Exercises'))")
            .ToListAsync()).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "IX_ExerciseCategories_CoachId_Kind_NameKey", "IX_Exercises_CoachId_NameKey_EquipmentId" }, unique);
    }
}
```

`backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseCategoryTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Exercises.Commands;
using Coaching.Domain.Exercises;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Exercises;

[Collection(ApiCollection.Name)]
public sealed class ExerciseCategoryTests(ApiFactory factory)
{
    private static string Tag() => Guid.NewGuid().ToString("N")[..10];

    private async Task<int> CountAsync(Guid coachId, CategoryKind kind)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ExerciseCategories.CountAsync(c => c.CoachId == coachId && c.Kind == kind);
    }

    // @BR-f03-thu-vien-bai-tap-005 @ADR-f03-thu-vien-bai-tap-003: coach seed có sẵn 11 nhóm cơ + 7 dụng cụ, đúng thứ tự
    [Fact]
    public async Task BR005_Seeded_coach_starts_with_default_catalog_in_order()
    {
        var catalog = await ExerciseRequests.CategoriesAsync(await factory.AdminClientAsync());

        Assert.Equal(DefaultCatalog.MuscleGroups.ToArray(), catalog.MuscleGroups.Take(11).Select(c => c.Name).ToArray());
        Assert.Equal(DefaultCatalog.Equipment.ToArray(), catalog.Equipment.Take(7).Select(c => c.Name).ToArray());
    }

    // @BR-f03-thu-vien-bai-tap-005 @ADR-f03-thu-vien-bai-tap-003: coach chưa có danh mục được tạo ở lần khởi động kế tiếp; khởi động lại không tạo thêm
    [Fact]
    public async Task BR005_Coach_without_catalog_gets_it_on_next_startup_once()
    {
        var (coachId, _) = await ExerciseRequests.NewCoachAsync(factory, withCatalog: false);
        Assert.Equal(0, await CountAsync(coachId, CategoryKind.MuscleGroup));

        await using (var first = factory.WithWebHostBuilder(_ => { }))
            _ = first.Services;
        await using (var second = factory.WithWebHostBuilder(_ => { }))
            _ = second.Services;

        Assert.Equal(11, await CountAsync(coachId, CategoryKind.MuscleGroup));
        Assert.Equal(7, await CountAsync(coachId, CategoryKind.Equipment));
    }

    // @BR-f03-thu-vien-bai-tap-006: mục mới (đã trim) đứng sau các mục đã có, dùng được ngay
    [Fact]
    public async Task BR006_New_equipment_is_trimmed_and_listed_last()
    {
        var admin = await factory.AdminClientAsync();
        var name = $"Resistance Band {Tag()}";

        var response = await admin.PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind = "Equipment", name = $"  {name} " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<CategoryCreatedDto>())!;
        Assert.Equal("Equipment", created.Kind);
        Assert.Equal(name, created.Name);
        var catalog = await ExerciseRequests.CategoriesAsync(admin);
        Assert.Equal(created.Id, catalog.Equipment[^1].Id);
        Assert.DoesNotContain(catalog.MuscleGroups, c => c.Id == created.Id);
    }

    // @BR-f03-thu-vien-bai-tap-006: trùng không phân biệt hoa/thường, dấu, khoảng trắng thừa — với mục CÙNG loại
    [Theory]
    [InlineData("MuscleGroup", "ngực")]
    [InlineData("MuscleGroup", "  NGUC  ")]
    [InlineData("musclegroup", "Vai   sau")]
    [InlineData("Equipment", "smith   machine")]
    [InlineData("Equipment", "CABLE")]
    public async Task BR006_Name_equal_to_an_item_of_the_same_kind_is_duplicate(string kind, string name)
    {
        var response = await (await factory.AdminClientAsync())
            .PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind, name });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("category.duplicate", json.RootElement.GetProperty("code").GetString());
        Assert.Equal("Đã có mục cùng tên.", json.RootElement.GetProperty("title").GetString());
    }

    // @BR-f03-thu-vien-bai-tap-006: cùng tên ở loại khác không phải trùng
    [Fact]
    public async Task BR006_Same_name_in_the_other_kind_is_allowed()
    {
        var admin = await factory.AdminClientAsync();
        var name = $"Landmine {Tag()}";

        Assert.Equal(HttpStatusCode.Created,
            (await admin.PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind = "Equipment", name })).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await admin.PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind = "MuscleGroup", name })).StatusCode);
    }

    // @BR-f03-thu-vien-bai-tap-006: tên danh mục trống → không thêm
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task BR006_Blank_category_name_is_rejected(string? name)
    {
        var response = await (await factory.AdminClientAsync())
            .PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind = "MuscleGroup", name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("category.name_required", await StudentRequests.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Category_name_longer_than_100_is_rejected()
    {
        var response = await (await factory.AdminClientAsync())
            .PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind = "MuscleGroup", name = new string('x', 101) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("category.name_too_long", await StudentRequests.ErrorCodeAsync(response));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Muscle")]
    [InlineData("5")]
    public async Task Unknown_category_kind_is_rejected(string? kind)
    {
        var response = await (await factory.AdminClientAsync())
            .PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind, name = $"Mục {Tag()}" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("category.kind_invalid", await StudentRequests.ErrorCodeAsync(response));
    }

    // @BR-f03-thu-vien-bai-tap-006: hai request thêm cùng tên đua nhau qua bước kiểm trùng → chỉ mục duy nhất chặn, trả 409 thay vì 500
    [Fact]
    public async Task BR006_Concurrent_duplicate_category_returns_409()
    {
        var race = new Race();
        await using var host = ExerciseRequests.WithRace<ExerciseCategory>(factory, race,
            c => c.Name.StartsWith("Race ", StringComparison.Ordinal),
            async (sp, c) =>
            {
                var db = sp.GetRequiredService<AppDbContext>();
                db.ExerciseCategories.Add(ExerciseCategory.Create(c.CoachId, c.Kind, c.Name, c.SortOrder + 1000));
                await db.SaveChangesAsync();
            });
        var admin = await ExerciseRequests.AdminClientAsync(factory, host);

        var response = await admin.PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind = "Equipment", name = $"Race {Tag()}" });

        Assert.True(race.Happened, "Giả lập đua không chạy");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("category.duplicate", await StudentRequests.ErrorCodeAsync(response));
    }

    // NFR-f03-thu-vien-bai-tap-002: học viên không truy cập được danh mục
    [Fact]
    public async Task Student_cannot_read_or_add_categories()
    {
        var student = await ExerciseRequests.StudentClientAsync(factory);

        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync(ExerciseRequests.CategoriesUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync(ExerciseRequests.CategoriesUrl,
            new { kind = "Equipment", name = "Band" })).StatusCode);
    }

    // NFR-f03-thu-vien-bai-tap-002 @ADR-f03-thu-vien-bai-tap-003: danh mục thuộc coach — coach khác không thấy mục của nhau
    [Fact]
    public async Task Coach_sees_only_own_categories()
    {
        var (_, other) = await ExerciseRequests.NewCoachAsync(factory);
        var name = $"Riêng {Tag()}";
        Assert.Equal(HttpStatusCode.Created,
            (await other.PostAsJsonAsync(ExerciseRequests.CategoriesUrl, new { kind = "Equipment", name })).StatusCode);

        Assert.Contains((await ExerciseRequests.CategoriesAsync(other)).Equipment, c => c.Name == name);
        Assert.DoesNotContain((await ExerciseRequests.CategoriesAsync(await factory.AdminClientAsync())).Equipment, c => c.Name == name);
    }
}
```

- [ ] **Step 7: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Exercises"`
Expected: FAIL — build error `CS0234: The type or namespace name 'Exercises' does not exist in the namespace 'Coaching.Application'` (và `'ExerciseCatalogSeeder' could not be found`, `'AppDbContext' does not contain a definition for 'ExerciseCategories'`).

- [ ] **Step 8: Application — `UniqueConstraintException`, lỗi danh mục, xem/thêm danh mục**

`backend/src/Coaching.Application/Common/UniqueConstraintException.cs`:
```csharp
namespace Coaching.Application.Common;

/// <summary>
/// Lần lưu vi phạm chỉ mục duy nhất: hai request đua nhau qua bước kiểm trùng của use case. IUnitOfWork ném lỗi này để
/// use case trả lỗi nghiệp vụ (409) mà không phụ thuộc EF/SqlClient.
/// </summary>
public sealed class UniqueConstraintException(Exception innerException)
    : Exception("Dữ liệu trùng với một bản ghi đã có.", innerException);
```

`backend/src/Coaching.Application/Exercises/ExerciseErrors.cs`:
```csharp
using Coaching.Application.Common;

namespace Coaching.Application.Exercises;

/// <summary>Lỗi của thư viện bài tập và danh mục (mã + câu chữ theo bảng API của tech-design F03).</summary>
public static class ExerciseErrors
{
    // @BR-f03-thu-vien-bai-tap-006
    public static readonly Error CategoryDuplicate =
        new("category.duplicate", "Đã có mục cùng tên.", ErrorKind.Conflict);

    public static readonly Error CategoryKindInvalid =
        new("category.kind_invalid", "Loại danh mục phải là MuscleGroup hoặc Equipment.");
}
```

`backend/src/Coaching.Application/Exercises/Queries/ListCategories.cs`:
```csharp
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Queries;

public sealed record CategoryDto(Guid Id, string Name);

public sealed record CategoriesDto(IReadOnlyList<CategoryDto> MuscleGroups, IReadOnlyList<CategoryDto> Equipment)
{
    public static CategoriesDto From(IEnumerable<ExerciseCategory> categories)
    {
        var ordered = categories.OrderBy(c => c.SortOrder).ThenBy(c => c.Name, StringComparer.Ordinal).ToList();
        return new CategoriesDto(Of(ordered, CategoryKind.MuscleGroup), Of(ordered, CategoryKind.Equipment));
    }

    private static List<CategoryDto> Of(IEnumerable<ExerciseCategory> categories, CategoryKind kind) =>
        categories.Where(c => c.Kind == kind).Select(c => new CategoryDto(c.Id, c.Name)).ToList();
}

/// <summary>@BR-f03-thu-vien-bai-tap-005: danh mục của coach đang đăng nhập, mỗi loại theo SortOrder.</summary>
public sealed class ListCategoriesHandler(CurrentCoach currentCoach, IExerciseCategoryRepository categories)
{
    public async Task<Result<CategoriesDto>> HandleAsync(CancellationToken ct = default)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return StudentErrors.CoachNotFound;

        return CategoriesDto.From(await categories.ListAsync(coach.Id, ct));
    }
}
```

`backend/src/Coaching.Application/Exercises/Commands/AddCategory.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Commands;

/// <summary><paramref name="Kind"/>: "MuscleGroup" | "Equipment" (không phân biệt hoa/thường).</summary>
public sealed record AddCategoryCommand(string? Kind, string? Name);

public sealed record CategoryCreatedDto(Guid Id, string Kind, string Name);

public sealed class AddCategoryHandler(
    CurrentCoach currentCoach,
    IExerciseCategoryRepository categories,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<CategoryCreatedDto>> HandleAsync(AddCategoryCommand command, CancellationToken ct = default)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return StudentErrors.CoachNotFound;
        if (!Enum.TryParse<CategoryKind>(command.Kind, ignoreCase: true, out var kind) || !Enum.IsDefined(kind))
            return ExerciseErrors.CategoryKindInvalid;

        // @BR-f03-thu-vien-bai-tap-006: mục mới đứng sau các mục cùng loại đã có
        var existing = await categories.ListAsync(coach.Id, ct);
        var sortOrder = existing.Where(c => c.Kind == kind).Select(c => c.SortOrder).DefaultIfEmpty(0).Max() + 1;
        var category = ExerciseCategory.Create(coach.Id, kind, command.Name, sortOrder); // tên trống → 400 category.name_required

        // @BR-f03-thu-vien-bai-tap-006 @ADR-f03-thu-vien-bai-tap-002: trùng khoá tên với mục cùng loại → 409
        if (await categories.ExistsAsync(coach.Id, kind, category.NameKey, ct))
            return ExerciseErrors.CategoryDuplicate;

        categories.Add(category);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintException)
        {
            return ExerciseErrors.CategoryDuplicate; // @BR-f03-thu-vien-bai-tap-006: hai request đua nhau
        }

        return new CategoryCreatedDto(category.Id, category.Kind.ToString(), category.Name);
    }
}
```

`backend/src/Coaching.Application/DependencyInjection.cs` — thêm using và 2 dòng đăng ký (cuối danh sách, trước `return services;`):
```csharp
using Coaching.Application.Exercises.Commands;
using Coaching.Application.Exercises.Queries;
```
```csharp
        services.AddScoped<ImportStudentsHandler>(); // @ADR-f02-import-excel-007
        services.AddScoped<ListCategoriesHandler>(); // @BR-f03-thu-vien-bai-tap-005
        services.AddScoped<AddCategoryHandler>(); // @BR-f03-thu-vien-bai-tap-006
        return services;
```

- [ ] **Step 9: Infrastructure — cấu hình EF, repository, `UniqueConstraintException`, seeder danh mục, DI, initializer**

`backend/src/Coaching.Infrastructure/Persistence/Configurations/ExerciseCategoryConfiguration.cs`:
```csharp
using Coaching.Domain.Coaches;
using Coaching.Domain.Exercises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Persistence.Configurations;

/// <summary>@ADR-f03-thu-vien-bai-tap-003: bảng ExerciseCategories (Liquibase v0.5.0).</summary>
internal sealed class ExerciseCategoryConfiguration : IEntityTypeConfiguration<ExerciseCategory>
{
    public void Configure(EntityTypeBuilder<ExerciseCategory> builder)
    {
        builder.ToTable("ExerciseCategories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Name).HasMaxLength(ExerciseCategory.NameMaxLength).IsRequired();
        builder.Property(c => c.NameKey).HasMaxLength(ExerciseCategory.NameKeyMaxLength).IsRequired();

        // @BR-f03-thu-vien-bai-tap-006 @ADR-f03-thu-vien-bai-tap-002
        builder.HasIndex(c => new { c.CoachId, c.Kind, c.NameKey }).IsUnique();
        builder.HasOne<Coach>().WithMany().HasForeignKey(c => c.CoachId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(c => c.DomainEvents);
    }
}
```

`backend/src/Coaching.Infrastructure/Persistence/Repositories/ExerciseCategoryRepository.cs`:
```csharp
using Coaching.Domain.Exercises;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Persistence.Repositories;

internal sealed class ExerciseCategoryRepository(AppDbContext db) : IExerciseCategoryRepository
{
    public async Task<IReadOnlyList<ExerciseCategory>> ListAsync(Guid coachId, CancellationToken ct = default) =>
        await db.ExerciseCategories.AsNoTracking()
            .Where(c => c.CoachId == coachId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

    public Task<ExerciseCategory?> GetAsync(Guid id, Guid coachId, CancellationToken ct = default) =>
        db.ExerciseCategories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id && c.CoachId == coachId, ct);

    public Task<bool> ExistsAsync(Guid coachId, CategoryKind kind, string nameKey, CancellationToken ct = default) =>
        db.ExerciseCategories.AnyAsync(c => c.CoachId == coachId && c.Kind == kind && c.NameKey == nameKey, ct);

    public void Add(ExerciseCategory c) => db.ExerciseCategories.Add(c);
}
```

`backend/src/Coaching.Infrastructure/Seeding/ExerciseCatalogSeeder.cs`:
```csharp
using Coaching.Domain.Exercises;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Seeding;

/// <summary>
/// @BR-f03-thu-vien-bai-tap-005 @ADR-f03-thu-vien-bai-tap-003: danh mục ban đầu (11 nhóm cơ, 7 dụng cụ) cho mọi coach chưa có
/// mục nào — chạy lúc khởi động ở MỌI môi trường (quy tắc nghiệp vụ, khác seed bài tập dev). Idempotent.
/// </summary>
public static class ExerciseCatalogSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        var coachIds = await db.Coaches
            .Where(coach => !db.ExerciseCategories.Any(c => c.CoachId == coach.Id))
            .Select(coach => coach.Id)
            .ToListAsync(ct);
        if (coachIds.Count == 0)
            return;

        foreach (var coachId in coachIds)
        {
            db.ExerciseCategories.AddRange(DefaultCatalog.MuscleGroups
                .Select((name, i) => ExerciseCategory.Create(coachId, CategoryKind.MuscleGroup, name, i + 1)));
            db.ExerciseCategories.AddRange(DefaultCatalog.Equipment
                .Select((name, i) => ExerciseCategory.Create(coachId, CategoryKind.Equipment, name, i + 1)));
        }
        await db.SaveChangesAsync(ct);
    }
}
```

`backend/src/Coaching.Infrastructure/Persistence/AppDbContext.cs` — thêm using, DbSet và nhánh bắt vi phạm chỉ mục duy nhất trong `IUnitOfWork.SaveChangesAsync`:
```csharp
using Coaching.Domain.Exercises;
using Microsoft.Data.SqlClient;
```
```csharp
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    public DbSet<ExerciseCategory> ExerciseCategories => Set<ExerciseCategory>();
```
```csharp
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
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // @BR-f03-thu-vien-bai-tap-006: hai request đua nhau qua bước kiểm trùng → use case trả 409 thay vì 500
            throw new UniqueConstraintException(ex);
        }
    }
```

`backend/src/Coaching.Infrastructure/DependencyInjection.cs` — thêm using và đăng ký repository (sau `IStudentRepository`):
```csharp
using Coaching.Domain.Exercises;
```
```csharp
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IExerciseCategoryRepository, ExerciseCategoryRepository>(); // @ADR-f03-thu-vien-bai-tap-003
```

`backend/src/Coaching.Infrastructure/Seeding/DatabaseInitializer.cs` — gọi seeder ngay trước khối "hồ sơ tạo trước khi có SearchKey":
```csharp
        // @BR-f03-thu-vien-bai-tap-005 @ADR-f03-thu-vien-bai-tap-003: danh mục ban đầu cho mọi coach chưa có, ở mọi môi trường
        await ExerciseCatalogSeeder.SeedAsync(db, cancellationToken);

        // @ADR-f02-ho-so-hoc-vien-003: hồ sơ tạo trước khi có SearchKey (cột mặc định '') được tính lại một lần
```

- [ ] **Step 10: Api — `ExerciseCategoriesController`**

`backend/src/Coaching.Api/Controllers/Admin/ExerciseCategoriesController.cs`:
```csharp
using Coaching.Api.Common;
using Coaching.Application.Exercises.Commands;
using Coaching.Application.Exercises.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers.Admin;

/// <summary>Danh mục nhóm cơ/dụng cụ của coach đang đăng nhập. MVP chỉ xem và thêm (spec §5).</summary>
// Route riêng (kebab-case) thay [controller] của AdminControllerBase; AdminOnly vẫn kế thừa (NFR-f03-thu-vien-bai-tap-002)
[Route("api/admin/exercise-categories")]
public sealed class ExerciseCategoriesController : AdminControllerBase
{
    // @BR-f03-thu-vien-bai-tap-005
    [HttpGet]
    [ProducesResponseType<CategoriesDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromServices] ListCategoriesHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(cancellationToken)).ToActionResult(this);

    // @BR-f03-thu-vien-bai-tap-006
    [HttpPost]
    [ProducesResponseType<CategoryCreatedDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Add([FromBody] AddCategoryCommand command, [FromServices] AddCategoryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : result.Error!.ToProblem(this);
    }
}
```

- [ ] **Step 11: Chạy integration test — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Exercises"`
Expected: PASS — `ExerciseSchemaTests` (1) và `ExerciseCategoryTests` (1 + 1 + 1 + 5 + 1 + 2 + 1 + 3 + 1 + 1 + 1 = 18) `Passed`, `Failed: 0`. (Nếu `InitializeAsync` lỗi `Liquibase update lỗi (exit …)`: đọc log trong exception, sửa SQL v0.5.0 rồi chạy lại.)

- [ ] **Step 12: Acceptance — `ExerciseCatalogApi` + bind Rule BR-005, BR-006**

`backend/tests/Coaching.Acceptance/Support/ExerciseCatalogApi.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Exercises.Queries;

namespace Coaching.Acceptance.Support;

/// <summary>Gọi /api/admin/exercise-categories bằng phiên coach của StudentApi. Đọc danh mục KHÔNG ghi đè ctx.LastResponse.</summary>
public sealed class ExerciseCatalogApi(AcceptanceContext ctx, StudentApi coach)
{
    public const string Url = "/api/admin/exercise-categories";

    public async Task<CategoriesDto> GetAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", coach.AdminToken);
        using var response = await ctx.Client().SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CategoriesDto>())!;
    }

    public Task<HttpResponseMessage> TryAddAsync(string kind, string? name) =>
        ctx.SendJsonAsync(HttpMethod.Post, Url, new { kind, name }, coach.AdminToken);

    public async Task<Guid> MuscleGroupIdAsync(string name) => (await GetAsync()).MuscleGroups.Single(c => c.Name == name).Id;

    public async Task<Guid> EquipmentIdAsync(string name) => (await GetAsync()).Equipment.Single(c => c.Name == name).Id;
}
```

`backend/tests/Coaching.Acceptance/Steps/ExerciseCategorySteps.cs`:
```csharp
using System.Net;
using Coaching.Acceptance.Support;
using Coaching.Application.Exercises.Queries;
using Coaching.Domain.Exercises;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class ExerciseCategorySteps(AcceptanceContext ctx, ExerciseCatalogApi catalog)
{
    private CategoriesDto? _seen;

    private static string[] Split(string names) => names.Split(", ", StringSplitOptions.TrimEntries);

    // @BR-f03-thu-vien-bai-tap-005
    [When(@"^coach xem danh mục của thư viện bài tập$")]
    public async Task ViewCatalog() => _seen = await catalog.GetAsync();

    // @BR-f03-thu-vien-bai-tap-005 @ADR-f03-thu-vien-bai-tap-003
    [Then(@"^danh mục nhóm cơ gồm ""([^""]*)""$")]
    public void MuscleGroupsAre(string names) =>
        Assert.Equal(Split(names), _seen!.MuscleGroups.Select(c => c.Name).ToArray());

    // @BR-f03-thu-vien-bai-tap-005 @ADR-f03-thu-vien-bai-tap-003
    [Then(@"^danh mục dụng cụ gồm ""([^""]*)""$")]
    public void EquipmentIs(string names) =>
        Assert.Equal(Split(names), _seen!.Equipment.Select(c => c.Name).ToArray());

    // @BR-f03-thu-vien-bai-tap-006
    [Given(@"^coach đã thêm dụng cụ ""([^""]*)""$")]
    public async Task EquipmentAdded(string name) =>
        Assert.Equal(HttpStatusCode.Created, (await catalog.TryAddAsync("Equipment", name)).StatusCode);

    // @BR-f03-thu-vien-bai-tap-006
    [When(@"^coach thêm nhóm cơ ""([^""]*)""$")]
    public Task AddMuscleGroup(string name) => catalog.TryAddAsync("MuscleGroup", name);

    // @BR-f03-thu-vien-bai-tap-006 @ADR-f03-thu-vien-bai-tap-002: báo trùng, danh mục vẫn chỉ có một mục cùng khoá
    [Then(@"^nhóm cơ không được thêm vì đã có ""([^""]*)""$")]
    public async Task MuscleGroupDuplicate(string existing)
    {
        Assert.Equal(HttpStatusCode.Conflict, ctx.LastResponse!.StatusCode);
        Assert.Equal("category.duplicate", await StudentApi.ErrorCodeAsync(ctx.LastResponse));
        var sameKey = (await catalog.GetAsync()).MuscleGroups
            .Where(c => ExerciseNameKey.For(c.Name) == ExerciseNameKey.For(existing))
            .Select(c => c.Name)
            .ToList();
        Assert.Equal(new[] { existing }, sameKey);
    }
}
```

- [ ] **Step 13: Chạy acceptance Rule BR-005, BR-006 — kỳ vọng 2 PASS, 1 chờ Task 2**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f03-thu-vien-bai-tap-005|Category=BR-f03-thu-vien-bai-tap-006)&Category!=web&Category!=manual"`
Expected: `Passed: 2` (`Coach có sẵn danh mục ban đầu`, `Nhóm cơ trùng tên không được thêm`), `Failed: 1` — `Thêm dụng cụ mới rồi dùng ngay cho bài mới` với `No matching step definition found` cho `coach thêm bài "Band Pull Apart" dụng cụ "Resistance Band" nhóm cơ "Vai sau"` (interim rule — bind ở Task 2). Không có lỗi nào khác.

- [ ] **Step 14: Verify tổng backend**

Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: Build thành công (0 warning, TreatWarningsAsErrors). Mọi test Domain/Application/Integration (gồm toàn bộ test F00/F01-P1/F02 — `IUnitOfWork` mới chỉ thêm nhánh 2601/2627) và acceptance F00/F01-P1/F02 `Passed`. Chỉ được phép fail các scenario `@api` của `features/thu-vien/bai-tap/thu-vien-bai-tap.feature` thuộc BR-f03-thu-vien-bai-tap-001…004, 006 (scenario "Thêm dụng cụ mới…"), 007…021 với `No matching step definition found` (interim rule). Không có lỗi nào khác.

- [ ] **Step 15: Commit**

```bash
git add database/changelog/db.changelog-master.xml database/changelog/v0.5.0 backend/src/Coaching.Domain/Exercises backend/src/Coaching.Application/Common/UniqueConstraintException.cs backend/src/Coaching.Application/Exercises backend/src/Coaching.Application/DependencyInjection.cs backend/src/Coaching.Infrastructure/Persistence/Configurations/ExerciseCategoryConfiguration.cs backend/src/Coaching.Infrastructure/Persistence/Repositories/ExerciseCategoryRepository.cs backend/src/Coaching.Infrastructure/Seeding/ExerciseCatalogSeeder.cs backend/src/Coaching.Infrastructure/Persistence/AppDbContext.cs backend/src/Coaching.Infrastructure/DependencyInjection.cs backend/src/Coaching.Infrastructure/Seeding/DatabaseInitializer.cs backend/src/Coaching.Api/Controllers/Admin/ExerciseCategoriesController.cs backend/tests/Coaching.Domain.Tests/Exercises/ExerciseCatalogTests.cs backend/tests/Coaching.Api.IntegrationTests/Exercises backend/tests/Coaching.Acceptance/Support/ExerciseCatalogApi.cs backend/tests/Coaching.Acceptance/Steps/ExerciseCategorySteps.cs
git commit -m "$(cat <<'EOF'
feat(exercises): exercise library schema v0.5.0, default catalog seeded for every coach, categories API (BR-f03-thu-vien-bai-tap-005, 006)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
)"
```

---

### Task 2: Bài tập — thêm, xem, sửa (có version); chặn trùng (tên chuẩn hoá, dụng cụ) kể cả bài đang ẩn; tên/nhóm cơ/dụng cụ

**Files:**
- Create: `backend/src/Coaching.Domain/Exercises/Exercise.cs`, `ExerciseDose.cs`, `IExerciseRepository.cs`
- Modify: `backend/src/Coaching.Application/Common/Result.cs` (`Error.Extensions`), `backend/src/Coaching.Api/Common/ResultExtensions.cs`
- Modify: `backend/src/Coaching.Application/Exercises/ExerciseErrors.cs`
- Create: `backend/src/Coaching.Application/Exercises/ExerciseDtos.cs`, `Exercises/ExerciseCategoryCheck.cs`, `Exercises/Commands/SaveExerciseCommand.cs`, `Exercises/Commands/CreateExercise.cs`, `Exercises/Commands/UpdateExercise.cs`, `Exercises/Queries/GetExercise.cs`
- Modify: `backend/src/Coaching.Application/DependencyInjection.cs`
- Create: `backend/src/Coaching.Infrastructure/Persistence/Configurations/ExerciseConfiguration.cs`, `Persistence/Repositories/ExerciseRepository.cs`
- Modify: `backend/src/Coaching.Infrastructure/Persistence/AppDbContext.cs`, `backend/src/Coaching.Infrastructure/DependencyInjection.cs`
- Create: `backend/src/Coaching.Api/Controllers/Admin/ExercisesController.cs`
- Test: `backend/tests/Coaching.Domain.Tests/Exercises/ExerciseTests.cs` (mới)
- Test: `backend/tests/Coaching.Api.IntegrationTests/Common/ResultExtensionsTests.cs` (thêm 1 test), `Exercises/ExerciseRequests.cs` (thêm method), `Exercises/ExerciseCrudTests.cs` (mới)
- Acceptance: `backend/tests/Coaching.Acceptance/Support/ExerciseApi.cs`, `backend/tests/Coaching.Acceptance/Steps/ExerciseSteps.cs` (mới)
- Living docs bind: Rule `@BR-f03-thu-vien-bai-tap-001`…`004`, `@BR-f03-thu-vien-bai-tap-020` + scenario còn lại của `@BR-f03-thu-vien-bai-tap-006`. KHÔNG sửa `.feature`.

**Interfaces:**
- Consumes (Task 1): `ExerciseNameKey.For`, `ExerciseCategory` (+ `Kind`, `Name`), `IExerciseCategoryRepository.GetAsync/ListAsync`, `UniqueConstraintException`, `ExerciseErrors` (file), `AppDbContext.ExerciseCategories`, `ExerciseRequests` (`CategoriesAsync`, `NewCoachAsync`, `StudentClientAsync`, `AdminClientAsync(factory, host)`, `WithRace<T>`, `Race`), `ExerciseCatalogApi`. F02: `StudentVersion.Parse` (internal, cùng assembly — ADR-f02-ho-so-hoc-vien-004), `ConcurrencyConflictException`, `StudentErrors.CoachNotFound`, `StudentApi.AdminToken/AssertErrorAsync/ErrorCodeAsync`.
- Produces:
  - Domain: `Exercise` (`CoachId`, `Name`, `NameKey`, `SearchKey`, `MuscleGroupId`, `EquipmentId`, `VideoUrl`, `HasVideo`, `Dose`, `TechniqueNotes`, `CommonMistakes`, `IsHidden`, `Version`; `NameMaxLength = 100`, `NotesMaxLength = 2000`, `KeyMaxLength = 200`, `VideoUrlMaxLength = 2048`; `Create(Guid coachId, string? name, Guid muscleGroupId, Guid equipmentId)`, `Rename(string? name)`, `Categorise(Guid muscleGroupId, Guid equipmentId)`); `ExerciseDose` record (+ `Empty`; `Create` ở Task 3); `IExerciseRepository { GetAsync, GetForUpdateAsync, FindByKeyAsync, Add, Remove }` (`ListAsync` thêm ở Task 5).
  - Application: `Error(string Code, string Message, ErrorKind Kind = Validation, IReadOnlyDictionary<string, object?>? Extensions = null)`; `ExerciseErrors.NotFound/Duplicate(Guid?)/VersionRequired/VersionConflict/CategoryNotFound/MuscleGroupRequired/EquipmentRequired`; `ExerciseDetailDto` (đúng skeleton) + `From(Exercise, string muscleGroup, string equipment)`; `SaveExerciseCommand` (đúng skeleton); `CreateExerciseHandler.HandleAsync(SaveExerciseCommand, ct)`, `UpdateExerciseHandler.HandleAsync(Guid id, SaveExerciseCommand, ct)`, `GetExerciseHandler.HandleAsync(Guid id, ct)` → `Result<ExerciseDetailDto>`; `ExerciseCategoryCheck.ResolveAsync(...)` / `ExerciseCategoryCheck.DetailAsync(...)` (internal, Task 4 dùng lại).
  - Api: `POST /api/admin/exercises` → 201 (+ Location) | 400 | 409 `exercise.duplicate` (+ `hiddenExerciseId` khi bài trùng đang ẩn); `GET /api/admin/exercises/{id}` → 200 | 404; `PUT /api/admin/exercises/{id}` → 200 | 400 `exercise.version_required` | 404 | 409 `exercise.duplicate` / `exercise.version_conflict`.
  - Test support: `ExerciseRequests.UniqueName/CommandAsync/CreateAsync/GetAsync/ToCommand/UpdateAsync/HideInDatabaseAsync`; acceptance `ExerciseApi` (`Ids`, `LastCreatedName`, `IdOf`, `CommandAsync`, `TryCreateAsync`, `CreateOkAsync`, `TryGetAsync`, `GetAsync`, `ToCommand`, `TryUpdateAsync`, `HideAsync` (SQL — Task 4 đổi sang API), `AssertErrorAsync`).

Quyết định (ghi lại cho Task 3/5): API nhận `repsMin/repsMax`, `rirMin/rirMax`; một số đơn lẻ = `min = max = n` (ExerciseDose.Create ở Task 3 gộp phía thiếu về phía có). Task 2 chưa có danh sách: bước kiểm "thư viện có bài …" đọc `GET /api/admin/exercises/{id}` theo Id nhớ trong scenario; `GET /api/admin/exercises` (danh sách) làm ở Task 5.

**Owns:** `@BR-f03-thu-vien-bai-tap-001`, `@BR-f03-thu-vien-bai-tap-002`, `@BR-f03-thu-vien-bai-tap-003`, `@BR-f03-thu-vien-bai-tap-004`, `@BR-f03-thu-vien-bai-tap-020`, `@BR-f03-thu-vien-bai-tap-021` | `@ADR-f03-thu-vien-bai-tap-001`

Ghi chú bind step (đã soát `Steps/*.cs` gồm `ExerciseCategorySteps` của Task 1 — không trùng; Given/Then cùng chữ là hai binding khác loại, Reqnroll cho phép):
| Step trong living doc | Binding |
|---|---|
| `thư viện có bài "…" dụng cụ "…"` (Given) | **mới** `ExerciseSteps.LibraryHas` — nhóm cơ mặc định "Ngực" |
| `thư viện có bài "…" dụng cụ "…" và bài "…" dụng cụ "…"` | **mới** `LibraryHasTwo` |
| `thư viện có bài "…" dụng cụ "…" đang ẩn` | **mới** `LibraryHasHidden` (đặt ẩn bằng SQL; Task 4 đổi `ExerciseApi.HideAsync` sang API) |
| `coach thêm bài "…" dụng cụ "…" nhóm cơ "…"` | **mới** `Add` (dùng chung cho BR-006, BR-017) |
| `coach thêm một bài không có tên, dụng cụ "…" nhóm cơ "…"` / `coach thêm bài "…" dụng cụ "…", không có nhóm cơ` / `coach thêm bài "…" nhóm cơ "…", không có dụng cụ` | **mới** `AddWithoutName` / `AddWithoutMuscleGroup` / `AddWithoutEquipment` |
| `coach đổi tên bài đó thành "…"` / `coach đổi tên bài "…" thành "…"` | **mới** `RenameLast` / `Rename` |
| `coach đang sửa bài "…" và bài đó vừa được sửa ở nơi khác` / `coach cập nhật bài "…" dựa trên phiên bản đang sửa` | **mới** `EditingWhileChangedElsewhere` / `UpdateFromStaleVersion` |
| `thư viện có bài "…" dụng cụ "…"` (Then) | **mới** `LibraryContains` |
| `bài không được (thêm\|lưu) vì đã có bài cùng tên và dụng cụ` | **mới** `Duplicate` |
| `coach được gợi ý bỏ ẩn bài "…" dụng cụ "…"` | **mới** `SuggestedToUnhide` |
| `bài "…" giữ nguyên` / `bài không được thêm vì thiếu (tên\|nhóm cơ\|dụng cụ)` / `bài "…" thuộc nhóm cơ "…"` / `bài "…" dùng dụng cụ "…"` | **mới** `Unchanged` / `Missing` / `HasMuscleGroup` / `UsesEquipment` |
| `bài không được lưu vì vừa được sửa ở nơi khác` / `bài "…" giữ nội dung của lần lưu trước` | **mới** `VersionConflict` / `KeepsPreviousSave` |
| `@web` scenario "Cùng tên khác dụng cụ là hai bài khác nhau" (step `thư viện có cả …`) | không chạy ở Reqnroll (`Category!=web`) — bind Playwright ở Task 8 |
| Rule BR-021 scenario `@api` (hướng dẫn kỹ thuật "Cũ"/"Mới") | step bind ở **Task 3** (cần hướng dẫn kỹ thuật — BR-013); tới lúc đó fail `No matching step definition found` (interim rule) |

- [ ] **Step 1: Viết Domain test cho `Exercise`**

`backend/tests/Coaching.Domain.Tests/Exercises/ExerciseTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Exercises;

namespace Coaching.Domain.Tests.Exercises;

public sealed class ExerciseTests
{
    private static readonly Guid CoachId = Guid.NewGuid();
    private static readonly Guid Chest = Guid.NewGuid();
    private static readonly Guid Shoulder = Guid.NewGuid();
    private static readonly Guid Cable = Guid.NewGuid();
    private static readonly Guid Dumbbell = Guid.NewGuid();

    private static Exercise Create(string? name = "Lateral Raise") => Exercise.Create(CoachId, name, Chest, Cable);

    private static string ErrorOf(Action action) => Assert.Throws<DomainException>(action).Code;

    // @ADR-f03-thu-vien-bai-tap-001 @BR-f03-thu-vien-bai-tap-002: tên đã trim; NameKey/SearchKey theo ExerciseNameKey; bài mới chưa ẩn, chưa có video/liều/hướng dẫn
    [Fact]
    public void ADR001_Create_keeps_trimmed_name_categories_and_keys()
    {
        var exercise = Create("  Lateral   Raise ");

        Assert.NotEqual(Guid.Empty, exercise.Id);
        Assert.Equal(CoachId, exercise.CoachId);
        Assert.Equal("Lateral   Raise", exercise.Name);
        Assert.Equal("lateral raise", exercise.NameKey);
        Assert.Equal(exercise.NameKey, exercise.SearchKey);
        Assert.Equal(Chest, exercise.MuscleGroupId);
        Assert.Equal(Cable, exercise.EquipmentId);
        Assert.False(exercise.IsHidden);
        Assert.Null(exercise.VideoUrl);
        Assert.False(exercise.HasVideo);
        Assert.Equal(ExerciseDose.Empty, exercise.Dose);
        Assert.Null(exercise.TechniqueNotes);
        Assert.Null(exercise.CommonMistakes);
    }

    // @BR-f03-thu-vien-bai-tap-002: tên trống hoặc chỉ có khoảng trắng → báo thiếu tên
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BR002_Name_is_required(string? name) => Assert.Equal("exercise.name_required", ErrorOf(() => Create(name)));

    // @BR-f03-thu-vien-bai-tap-002: tối đa 100 ký tự sau trim; 101 → không lưu
    [Fact]
    public void BR002_Name_is_at_most_100_characters_after_trim()
    {
        Assert.Equal(100, Create($"  {new string('a', 100)}  ").Name.Length);
        Assert.Equal("exercise.name_too_long", ErrorOf(() => Create(new string('a', 101))));
    }

    // @BR-f03-thu-vien-bai-tap-003: không có nhóm cơ → báo thiếu nhóm cơ
    [Fact]
    public void BR003_Muscle_group_is_required() =>
        Assert.Equal("exercise.muscle_group_required", ErrorOf(() => Exercise.Create(CoachId, "Chest Press", Guid.Empty, Cable)));

    // @BR-f03-thu-vien-bai-tap-004: không có dụng cụ → báo thiếu dụng cụ
    [Fact]
    public void BR004_Equipment_is_required() =>
        Assert.Equal("exercise.equipment_required", ErrorOf(() => Exercise.Create(CoachId, "Cable Crossover", Chest, Guid.Empty)));

    // @BR-f03-thu-vien-bai-tap-001 @ADR-f03-thu-vien-bai-tap-002: hoa/thường, dấu, khoảng trắng thừa, kiểu dấu gạch → cùng khoá
    [Theory]
    [InlineData("Lateral Raise", "  lateral   raise ")]
    [InlineData("Ép ngực", "Ep nguc")]
    [InlineData("T–Bar Row", "T-Bar Row")]
    public void BR001_Names_written_differently_share_a_key(string existing, string added) =>
        Assert.Equal(Create(existing).NameKey, Create(added).NameKey);

    // @BR-f03-thu-vien-bai-tap-001: sửa chữ hoa/thường tên của chính bài đó — khoá không đổi
    [Fact]
    public void BR001_Rename_updates_name_and_keeps_key_when_only_case_changes()
    {
        var exercise = Create("lateral raise");

        exercise.Rename("Lateral Raise");

        Assert.Equal("Lateral Raise", exercise.Name);
        Assert.Equal("lateral raise", exercise.NameKey);
        Assert.Equal("lateral raise", exercise.SearchKey);
    }

    // @BR-f03-thu-vien-bai-tap-002: đổi sang tên trống bị từ chối, bài giữ tên cũ
    [Fact]
    public void BR002_Rename_to_blank_is_rejected_and_keeps_old_name()
    {
        var exercise = Create("Chest Press");

        Assert.Equal("exercise.name_required", ErrorOf(() => exercise.Rename("  ")));

        Assert.Equal("Chest Press", exercise.Name);
        Assert.Equal("chest press", exercise.NameKey);
    }

    // @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004
    [Fact]
    public void Categorise_changes_both_categories_or_nothing()
    {
        var exercise = Create();

        exercise.Categorise(Shoulder, Dumbbell);
        Assert.Equal((Shoulder, Dumbbell), (exercise.MuscleGroupId, exercise.EquipmentId));

        Assert.Equal("exercise.equipment_required", ErrorOf(() => exercise.Categorise(Chest, Guid.Empty)));
        Assert.Equal((Shoulder, Dumbbell), (exercise.MuscleGroupId, exercise.EquipmentId));
    }

    // @BR-f03-thu-vien-bai-tap-021: sửa bài chỉ đổi bản trong thư viện (giáo án F05 lưu bản chụp, không tham chiếu sống)
    [Fact]
    public void BR021_Editing_changes_the_library_copy()
    {
        var exercise = Create("Lateral Raise");

        exercise.Rename("Cable Lateral Raise");

        Assert.Equal("Cable Lateral Raise", exercise.Name);
        Assert.Equal("cable lateral raise", exercise.NameKey);
    }
}
```

- [ ] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Exercises.ExerciseTests"`
Expected: FAIL — build error `CS0246: The type or namespace name 'Exercise' could not be found` và `'ExerciseDose' could not be found`.

- [ ] **Step 3: Domain — `ExerciseDose` (khung), `Exercise`, `IExerciseRepository`**

`backend/src/Coaching.Domain/Exercises/ExerciseDose.cs`:
```csharp
namespace Coaching.Domain.Exercises;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-006: liều mặc định có cấu trúc (Sets, Reps thấp–cao, RIR thấp–cao, nghỉ giây); mọi phần không
/// bắt buộc. Một số đơn lẻ lưu là thấp = cao. Kiểm khoảng ở <c>Create</c> (Task 3).
/// </summary>
public sealed record ExerciseDose(int? Sets, int? RepsMin, int? RepsMax, int? RirMin, int? RirMax, int? RestSeconds)
{
    public static readonly ExerciseDose Empty = new(null, null, null, null, null, null);
}
```

`backend/src/Coaching.Domain/Exercises/Exercise.cs`:
```csharp
using Coaching.Domain.Common;

namespace Coaching.Domain.Exercises;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-001: một bài trong thư viện của coach. Duy nhất theo (CoachId, NameKey, EquipmentId), kể cả
/// bài đang ẩn (chỉ mục duy nhất trong DB). Tham chiếu danh mục chỉ bằng Id.
/// @BR-f03-thu-vien-bai-tap-021: giáo án (F05) lưu bản chụp bài lúc thêm — sửa ở đây chỉ đổi bản trong thư viện.
/// </summary>
public sealed class Exercise : AggregateRoot<Guid>
{
    public const int NameMaxLength = 100;
    public const int NotesMaxLength = 2000;
    public const int KeyMaxLength = 200;
    public const int VideoUrlMaxLength = 2048;

    public Guid CoachId { get; private set; }

    public string Name { get; private set; } = default!;

    /// <summary>@ADR-f03-thu-vien-bai-tap-002: khoá so trùng — chỉ aggregate ghi.</summary>
    public string NameKey { get; private set; } = default!;

    /// <summary>@ADR-f03-thu-vien-bai-tap-002: khoá tìm kiếm, cùng chuẩn hoá với NameKey.</summary>
    public string SearchKey { get; private set; } = default!;

    public Guid MuscleGroupId { get; private set; }

    public Guid EquipmentId { get; private set; }

    public string? VideoUrl { get; private set; }

    public bool HasVideo => VideoUrl is not null;

    public ExerciseDose Dose { get; private set; } = ExerciseDose.Empty;

    public string? TechniqueNotes { get; private set; }

    public string? CommonMistakes { get; private set; }

    public bool IsHidden { get; private set; }

    /// <summary>@BR-f03-thu-vien-bai-tap-020: rowversion do SQL Server sinh, dùng chặn ghi đè.</summary>
    public byte[] Version { get; private set; } = [];

    private Exercise() { }

    private Exercise(Guid id) : base(id) { }

    public static Exercise Create(Guid coachId, string? name, Guid muscleGroupId, Guid equipmentId)
    {
        if (coachId == Guid.Empty)
            throw new DomainException("exercise.coach_required", "Bài tập phải thuộc một coach.");

        var exercise = new Exercise(Guid.CreateVersion7()) { CoachId = coachId };
        exercise.Rename(name);
        exercise.Categorise(muscleGroupId, equipmentId);
        return exercise;
    }

    // @BR-f03-thu-vien-bai-tap-002: bắt buộc, ≤ 100 ký tự sau trim
    // @BR-f03-thu-vien-bai-tap-001 @ADR-f03-thu-vien-bai-tap-002: khoá trùng/tìm kiếm luôn đi theo tên
    public void Rename(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new DomainException("exercise.name_required", "Nhập tên bài.");
        if (trimmed.Length > NameMaxLength)
            throw new DomainException("exercise.name_too_long", $"Tên bài tối đa {NameMaxLength} ký tự.");

        Name = trimmed;
        NameKey = ExerciseNameKey.For(trimmed);
        SearchKey = NameKey;
    }

    // @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004: đúng một nhóm cơ, đúng một dụng cụ (thuộc danh mục: kiểm ở Application)
    public void Categorise(Guid muscleGroupId, Guid equipmentId)
    {
        if (muscleGroupId == Guid.Empty)
            throw new DomainException("exercise.muscle_group_required", "Chọn nhóm cơ.");
        if (equipmentId == Guid.Empty)
            throw new DomainException("exercise.equipment_required", "Chọn dụng cụ.");

        MuscleGroupId = muscleGroupId;
        EquipmentId = equipmentId;
    }
}
```

`backend/src/Coaching.Domain/Exercises/IExerciseRepository.cs`:
```csharp
namespace Coaching.Domain.Exercises;

public interface IExerciseRepository
{
    /// <summary>Bài thuộc coach (được theo dõi để sửa/xoá); bài của coach khác coi như không tồn tại.</summary>
    Task<Exercise?> GetAsync(Guid id, Guid coachId, CancellationToken ct = default);

    /// <summary>@BR-f03-thu-vien-bai-tap-020: bài để sửa; khi lưu so Version trong DB với <paramref name="expectedVersion"/>.</summary>
    Task<Exercise?> GetForUpdateAsync(Guid id, Guid coachId, byte[] expectedVersion, CancellationToken ct = default);

    /// <summary>@BR-f03-thu-vien-bai-tap-001: bài cùng khoá tên + dụng cụ (kể cả đang ẩn), không theo dõi.</summary>
    Task<Exercise?> FindByKeyAsync(Guid coachId, string nameKey, Guid equipmentId, CancellationToken ct = default);

    void Add(Exercise e);

    void Remove(Exercise e);
}
```

- [ ] **Step 4: Chạy Domain test — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Exercises"`
Expected: PASS — `ExerciseTests` 14 test (1 + 3 + 1 + 1 + 1 + 3 + 1 + 1 + 1 + 1) và `ExerciseCatalogTests` 21 test `Passed`, `Failed: 0`.

- [ ] **Step 5: Viết integration test — lỗi mang `hiddenExerciseId`, thêm/xem/sửa, trùng, version, phân quyền**

`backend/tests/Coaching.Api.IntegrationTests/Common/ResultExtensionsTests.cs` — thêm `using Coaching.Application.Exercises;` và test sau vào class (trước `StubController`):
```csharp
    // @BR-f03-thu-vien-bai-tap-001: bài trùng đang ẩn → ProblemDetails mang hiddenExerciseId để gợi ý bỏ ẩn
    [Fact]
    public void Error_extensions_are_copied_to_problem()
    {
        var hiddenId = Guid.NewGuid();

        var result = Assert.IsType<ObjectResult>(ExerciseErrors.Duplicate(hiddenId).ToProblem(new StubController()));

        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal("exercise.duplicate", problem.Extensions["code"]);
        Assert.Equal("Đã có bài cùng tên và dụng cụ.", problem.Title);
        Assert.Equal(hiddenId, problem.Extensions["hiddenExerciseId"]);
        Assert.False(Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(
            ExerciseErrors.Duplicate(null).ToProblem(new StubController())).Value).Extensions.ContainsKey("hiddenExerciseId"));
    }
```

`backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseRequests.cs` — thêm using và các method sau vào class `ExerciseRequests`:
```csharp
using System.Net;
using Coaching.Application.Exercises;
using Coaching.Application.Exercises.Commands;
```
```csharp
    /// <summary>Tên duy nhất trong DB dùng chung của collection (các test cùng coach admin).</summary>
    public static string UniqueName(string label) => $"{label} {Guid.NewGuid():N}";

    public static async Task<SaveExerciseCommand> CommandAsync(HttpClient client, string? name,
        string muscleGroup = "Ngực", string equipment = "Machine")
    {
        var catalog = await CategoriesAsync(client);
        return new SaveExerciseCommand(name,
            catalog.MuscleGroups.Single(c => c.Name == muscleGroup).Id,
            catalog.Equipment.Single(c => c.Name == equipment).Id,
            null, null, null, null, null, null, null, null, null, null);
    }

    public static async Task<ExerciseDetailDto> CreateAsync(HttpClient client, string name, string muscleGroup = "Ngực",
        string equipment = "Machine", Func<SaveExerciseCommand, SaveExerciseCommand>? change = null)
    {
        var command = await CommandAsync(client, name, muscleGroup, equipment);
        var response = await client.PostAsJsonAsync(Url, change is null ? command : change(command));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ExerciseDetailDto>())!;
    }

    public static async Task<ExerciseDetailDto> GetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<ExerciseDetailDto>($"{Url}/{id}"))!;

    public static SaveExerciseCommand ToCommand(ExerciseDetailDto d) =>
        new(d.Name, d.MuscleGroupId, d.EquipmentId, d.VideoUrl, d.Sets, d.RepsMin, d.RepsMax, d.RirMin, d.RirMax,
            d.RestSeconds, d.TechniqueNotes, d.CommonMistakes, d.Version);

    public static Task<HttpResponseMessage> UpdateAsync(HttpClient client, ExerciseDetailDto current,
        Func<SaveExerciseCommand, SaveExerciseCommand> change) =>
        client.PutAsJsonAsync($"{Url}/{current.Id}", change(ToCommand(current)));

    /// <summary>Đặt bài ở trạng thái ẩn thẳng trong DB (API ẩn bài có ở Task 4).</summary>
    public static async Task HideInDatabaseAsync(ApiFactory factory, Guid id)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Exercises] SET [IsHidden] = 1 WHERE [Id] = {id}");
    }
```

`backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseCrudTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Exercises;
using Coaching.Domain.Exercises;
using Coaching.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Exercises;

[Collection(ApiCollection.Name)]
public sealed class ExerciseCrudTests(ApiFactory factory)
{
    private static string Name(string label) => ExerciseRequests.UniqueName(label);

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, await StudentRequests.ErrorCodeAsync(response));
    }

    // @ADR-f03-thu-vien-bai-tap-001 @BR-f03-thu-vien-bai-tap-002 @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004
    [Fact]
    public async Task ADR001_Created_exercise_round_trips_with_category_names_and_version()
    {
        var admin = await factory.AdminClientAsync();
        var name = Name("Chest Press");

        var response = await admin.PostAsJsonAsync(ExerciseRequests.Url,
            await ExerciseRequests.CommandAsync(admin, $"  {name} ", "Ngực", "Machine"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ExerciseDetailDto>())!;
        Assert.Equal($"{ExerciseRequests.Url}/{created.Id}", response.Headers.Location!.AbsolutePath, ignoreCase: true);
        Assert.Equal(name, created.Name);
        Assert.Equal("Ngực", created.MuscleGroup);
        Assert.Equal("Machine", created.Equipment);
        Assert.False(created.IsHidden);
        Assert.False(created.HasVideo);
        Assert.NotEmpty(created.Version);
        Assert.Equal(created, await ExerciseRequests.GetAsync(admin, created.Id));
    }

    // @BR-f03-thu-vien-bai-tap-002: 100 ký tự lưu được; 101 ký tự → không lưu (seed ❌)
    [Fact]
    public async Task BR002_Name_of_101_characters_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var name100 = $"{Guid.NewGuid():N}{new string('x', 68)}";

        Assert.Equal(100, (await ExerciseRequests.CreateAsync(admin, name100)).Name.Length);
        var response = await admin.PostAsJsonAsync(ExerciseRequests.Url,
            await ExerciseRequests.CommandAsync(admin, $"{Guid.NewGuid():N}{new string('x', 69)}"));
        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "exercise.name_too_long");
    }

    // @BR-f03-thu-vien-bai-tap-002: tên trống hoặc chỉ khoảng trắng → báo thiếu tên
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task BR002_Blank_name_is_rejected(string? name)
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PostAsJsonAsync(ExerciseRequests.Url, await ExerciseRequests.CommandAsync(admin, name));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "exercise.name_required");
    }

    // @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004: danh mục trống (seed ❌)
    [Fact]
    public async Task BR003_BR004_Missing_muscle_group_or_equipment_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var command = await ExerciseRequests.CommandAsync(admin, Name("Chest Press"));

        await AssertProblemAsync(await admin.PostAsJsonAsync(ExerciseRequests.Url, command with { MuscleGroupId = null }),
            HttpStatusCode.BadRequest, "exercise.muscle_group_required");
        await AssertProblemAsync(await admin.PostAsJsonAsync(ExerciseRequests.Url, command with { EquipmentId = null }),
            HttpStatusCode.BadRequest, "exercise.equipment_required");
        await AssertProblemAsync(await admin.PostAsJsonAsync(ExerciseRequests.Url, command with { MuscleGroupId = Guid.Empty }),
            HttpStatusCode.BadRequest, "exercise.muscle_group_required");
    }

    // @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004: nhóm cơ/dụng cụ không thuộc danh mục của coach (seed ❌) — gồm sai loại và mục của coach khác
    [Fact]
    public async Task BR003_BR004_Category_outside_the_coachs_catalog_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var (_, other) = await ExerciseRequests.NewCoachAsync(factory);
        var command = await ExerciseRequests.CommandAsync(admin, Name("Chest Press"));
        var theirs = await ExerciseRequests.CategoriesAsync(other);

        foreach (var wrong in new[]
                 {
                     command with { MuscleGroupId = Guid.NewGuid() },
                     command with { EquipmentId = Guid.NewGuid() },
                     command with { MuscleGroupId = command.EquipmentId },
                     command with { EquipmentId = command.MuscleGroupId },
                     command with { MuscleGroupId = theirs.MuscleGroups[0].Id },
                     command with { EquipmentId = theirs.Equipment[0].Id },
                 })
        {
            await AssertProblemAsync(await admin.PostAsJsonAsync(ExerciseRequests.Url, wrong),
                HttpStatusCode.BadRequest, "exercise.category_not_found");
        }
    }

    // @BR-f03-thu-vien-bai-tap-001: cùng tên chuẩn hoá + cùng dụng cụ → 409 (không gợi ý bỏ ẩn); khác dụng cụ → tạo được
    [Fact]
    public async Task BR001_Same_name_written_differently_with_same_equipment_is_duplicate()
    {
        var admin = await factory.AdminClientAsync();
        var tag = Guid.NewGuid().ToString("N");
        await ExerciseRequests.CreateAsync(admin, $"Ép ngực {tag}", "Ngực", "Cable");

        var duplicate = await admin.PostAsJsonAsync(ExerciseRequests.Url,
            await ExerciseRequests.CommandAsync(admin, $"  ep   NGUC {tag} ", "Ngực", "Cable"));

        await AssertProblemAsync(duplicate, HttpStatusCode.Conflict, "exercise.duplicate");
        using var json = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.TryGetProperty("hiddenExerciseId", out _));
        Assert.Equal("Dumbbell", (await ExerciseRequests.CreateAsync(admin, $"Ép ngực {tag}", "Ngực", "Dumbbell")).Equipment);
    }

    // @BR-f03-thu-vien-bai-tap-001: trùng một bài đang ẩn → 409 kèm Id bài đó để gợi ý bỏ ẩn
    [Fact]
    public async Task BR001_Duplicate_of_a_hidden_exercise_points_to_it()
    {
        var admin = await factory.AdminClientAsync();
        var hidden = await ExerciseRequests.CreateAsync(admin, Name("Face Pull"), "Vai sau", "Cable");
        await ExerciseRequests.HideInDatabaseAsync(factory, hidden.Id);

        var response = await admin.PostAsJsonAsync(ExerciseRequests.Url,
            await ExerciseRequests.CommandAsync(admin, hidden.Name, "Vai sau", "Cable"));

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "exercise.duplicate");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(hidden.Id, json.RootElement.GetProperty("hiddenExerciseId").GetGuid());
    }

    // @BR-f03-thu-vien-bai-tap-001: sửa A thành (tên, dụng cụ) của B → 409, A giữ nguyên; sửa hoa/thường tên của chính nó → lưu được
    [Fact]
    public async Task BR001_Renaming_onto_another_exercise_is_rejected_but_own_case_change_is_kept()
    {
        var admin = await factory.AdminClientAsync();
        var legPress = await ExerciseRequests.CreateAsync(admin, Name("leg press"), "Đùi trước", "Machine");
        var hackSquat = await ExerciseRequests.CreateAsync(admin, Name("Hack Squat"), "Đùi trước", "Machine");

        var clash = await ExerciseRequests.UpdateAsync(admin, hackSquat, c => c with { Name = legPress.Name.ToUpperInvariant() });
        await AssertProblemAsync(clash, HttpStatusCode.Conflict, "exercise.duplicate");
        Assert.Equal(hackSquat.Name, (await ExerciseRequests.GetAsync(admin, hackSquat.Id)).Name);

        var own = await ExerciseRequests.UpdateAsync(admin, legPress, c => c with { Name = legPress.Name.Replace("leg press", "Leg Press") });
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.StartsWith("Leg Press ", (await own.Content.ReadFromJsonAsync<ExerciseDetailDto>())!.Name);
    }

    // @BR-f03-thu-vien-bai-tap-001 @ADR-f03-thu-vien-bai-tap-001: hai request cùng (tên, dụng cụ) đua nhau qua bước kiểm trùng → chỉ mục duy nhất chặn, 409 thay vì 500
    [Fact]
    public async Task BR001_Create_losing_the_unique_race_returns_409()
    {
        var race = new Race();
        await using var host = ExerciseRequests.WithRace<Exercise>(factory, race,
            e => e.Name.StartsWith("Race ", StringComparison.Ordinal),
            async (sp, e) =>
            {
                var db = sp.GetRequiredService<AppDbContext>();
                db.Exercises.Add(Exercise.Create(e.CoachId, e.Name, e.MuscleGroupId, e.EquipmentId));
                await db.SaveChangesAsync();
            });
        var admin = await ExerciseRequests.AdminClientAsync(factory, host);

        var response = await admin.PostAsJsonAsync(ExerciseRequests.Url,
            await ExerciseRequests.CommandAsync(admin, Name("Race"), "Lưng", "Cable"));

        Assert.True(race.Happened, "Giả lập đua không chạy");
        await AssertProblemAsync(response, HttpStatusCode.Conflict, "exercise.duplicate");
    }

    // @BR-f03-thu-vien-bai-tap-001: 2 request song song → 1 tạo, 1 bị báo trùng
    [Fact]
    public async Task BR001_Two_parallel_creates_produce_one_exercise()
    {
        var admin = await factory.AdminClientAsync();
        var command = await ExerciseRequests.CommandAsync(admin, Name("Parallel Row"), "Lưng", "Cable");

        var responses = await Task.WhenAll(
            admin.PostAsJsonAsync(ExerciseRequests.Url, command),
            admin.PostAsJsonAsync(ExerciseRequests.Url, command));

        Assert.Equal(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict },
            responses.Select(r => r.StatusCode).OrderBy(s => (int)s).ToArray());
    }

    // @BR-f03-thu-vien-bai-tap-020: lưu dựa trên phiên bản cũ → 409, không ghi đè lần lưu trước
    [Fact]
    public async Task BR020_Stale_version_is_rejected_and_keeps_the_previous_save()
    {
        var admin = await factory.AdminClientAsync();
        var original = await ExerciseRequests.CreateAsync(admin, Name("Chest Press"));
        var catalog = await ExerciseRequests.CategoriesAsync(admin);
        var vai = catalog.MuscleGroups.Single(c => c.Name == "Vai").Id;
        var lung = catalog.MuscleGroups.Single(c => c.Name == "Lưng").Id;

        var first = await ExerciseRequests.UpdateAsync(admin, original, c => c with { MuscleGroupId = vai });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.NotEqual(original.Version, (await first.Content.ReadFromJsonAsync<ExerciseDetailDto>())!.Version);

        var stale = await ExerciseRequests.UpdateAsync(admin, original, c => c with { MuscleGroupId = lung });

        await AssertProblemAsync(stale, HttpStatusCode.Conflict, "exercise.version_conflict");
        Assert.Equal("Vai", (await ExerciseRequests.GetAsync(admin, original.Id)).MuscleGroup);
    }

    [Fact]
    public async Task BR020_Update_without_version_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var exercise = await ExerciseRequests.CreateAsync(admin, Name("Chest Press"));

        var response = await ExerciseRequests.UpdateAsync(admin, exercise, c => c with { Version = null });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "exercise.version_required");
    }

    [Fact]
    public async Task Unknown_exercise_is_404()
    {
        var response = await (await factory.AdminClientAsync()).GetAsync($"{ExerciseRequests.Url}/{Guid.NewGuid()}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "exercise.not_found");
    }

    // NFR-f03-thu-vien-bai-tap-002: học viên không gọi được API thư viện
    [Fact]
    public async Task Student_cannot_use_the_exercise_api()
    {
        var admin = await factory.AdminClientAsync();
        var exercise = await ExerciseRequests.CreateAsync(admin, Name("Chest Press"));
        var student = await ExerciseRequests.StudentClientAsync(factory);

        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync($"{ExerciseRequests.Url}/{exercise.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync(ExerciseRequests.Url, ExerciseRequests.ToCommand(exercise))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.PutAsJsonAsync($"{ExerciseRequests.Url}/{exercise.Id}", ExerciseRequests.ToCommand(exercise))).StatusCode);
    }

    // NFR-f03-thu-vien-bai-tap-002: bài của coach khác → 404, không bị đổi
    [Fact]
    public async Task Coach_cannot_read_or_change_another_coachs_exercise()
    {
        var (_, other) = await ExerciseRequests.NewCoachAsync(factory);
        var theirs = await ExerciseRequests.CreateAsync(other, Name("Riêng"));
        var admin = await factory.AdminClientAsync();
        var adminCommand = await ExerciseRequests.CommandAsync(admin, "Bị sửa");

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{ExerciseRequests.Url}/{theirs.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"{ExerciseRequests.Url}/{theirs.Id}",
            adminCommand with { Version = theirs.Version })).StatusCode);
        Assert.Equal(theirs.Name, (await ExerciseRequests.GetAsync(other, theirs.Id)).Name);
    }
}
```

- [ ] **Step 6: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Exercises|FullyQualifiedName~ResultExtensionsTests"`
Expected: FAIL — build error `CS0117: 'ExerciseErrors' does not contain a definition for 'Duplicate'`, `CS0246: The type or namespace name 'SaveExerciseCommand' could not be found`, `'ExerciseDetailDto' could not be found`, `'AppDbContext' does not contain a definition for 'Exercises'`.

- [ ] **Step 7: Application — `Error.Extensions`, lỗi bài tập, DTO, lệnh lưu, kiểm danh mục**

`backend/src/Coaching.Application/Common/Result.cs` — thay khai báo `Error`:
```csharp
/// <summary><paramref name="Extensions"/>: dữ liệu thêm vào ProblemDetails (vd. hiddenExerciseId — BR-f03-thu-vien-bai-tap-001).</summary>
public sealed record Error(
    string Code,
    string Message,
    ErrorKind Kind = ErrorKind.Validation,
    IReadOnlyDictionary<string, object?>? Extensions = null);
```

`backend/src/Coaching.Application/Exercises/ExerciseErrors.cs` — thêm vào class (sau `CategoryKindInvalid`):
```csharp
    public static readonly Error NotFound =
        new("exercise.not_found", "Không tìm thấy bài tập.", ErrorKind.NotFound);

    // @BR-f03-thu-vien-bai-tap-001: bài trùng đang ẩn → kèm Id để web admin gợi ý "Bỏ ẩn bài đó"
    public static Error Duplicate(Guid? hiddenExerciseId) =>
        new("exercise.duplicate", "Đã có bài cùng tên và dụng cụ.", ErrorKind.Conflict,
            hiddenExerciseId is { } id ? new Dictionary<string, object?> { ["hiddenExerciseId"] = id } : null);

    public static readonly Error VersionRequired =
        new("exercise.version_required", "Thiếu phiên bản bài tập. Tải lại bài rồi thử lại.");

    // @BR-f03-thu-vien-bai-tap-020
    public static readonly Error VersionConflict =
        new("exercise.version_conflict", "Bài vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.", ErrorKind.Conflict);

    // @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004
    public static readonly Error CategoryNotFound =
        new("exercise.category_not_found", "Nhóm cơ hoặc dụng cụ không có trong danh mục.");

    // @BR-f03-thu-vien-bai-tap-003
    public static readonly Error MuscleGroupRequired = new("exercise.muscle_group_required", "Chọn nhóm cơ.");

    // @BR-f03-thu-vien-bai-tap-004
    public static readonly Error EquipmentRequired = new("exercise.equipment_required", "Chọn dụng cụ.");
```

`backend/src/Coaching.Application/Exercises/ExerciseDtos.cs`:
```csharp
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises;

/// <summary>Chi tiết bài trong thư viện. <see cref="Version"/>: base64 của rowversion (BR-f03-thu-vien-bai-tap-020).</summary>
public sealed record ExerciseDetailDto(
    Guid Id,
    string Name,
    Guid MuscleGroupId,
    string MuscleGroup,
    Guid EquipmentId,
    string Equipment,
    string? VideoUrl,
    bool HasVideo,
    int? Sets,
    int? RepsMin,
    int? RepsMax,
    int? RirMin,
    int? RirMax,
    int? RestSeconds,
    string? TechniqueNotes,
    string? CommonMistakes,
    bool IsHidden,
    string Version)
{
    public static ExerciseDetailDto From(Exercise e, string muscleGroup, string equipment) => new(
        e.Id, e.Name, e.MuscleGroupId, muscleGroup, e.EquipmentId, equipment, e.VideoUrl, e.HasVideo,
        e.Dose.Sets, e.Dose.RepsMin, e.Dose.RepsMax, e.Dose.RirMin, e.Dose.RirMax, e.Dose.RestSeconds,
        e.TechniqueNotes, e.CommonMistakes, e.IsHidden,
        StudentVersion.Format(e.Version)); // cùng định dạng version base64 với hồ sơ (ADR-f02-ho-so-hoc-vien-004)
}
```

`backend/src/Coaching.Application/Exercises/Commands/SaveExerciseCommand.cs`:
```csharp
namespace Coaching.Application.Exercises.Commands;

/// <summary>
/// Thân POST/PUT bài tập (thay toàn bộ nội dung bài). @ADR-f03-thu-vien-bai-tap-006: liều có cấu trúc — một số đơn lẻ
/// gửi min = max. <paramref name="Version"/> bắt buộc khi sửa, bị bỏ qua khi thêm.
/// </summary>
public sealed record SaveExerciseCommand(
    string? Name,
    Guid? MuscleGroupId,
    Guid? EquipmentId,
    string? VideoUrl,
    int? Sets,
    int? RepsMin,
    int? RepsMax,
    int? RirMin,
    int? RirMax,
    int? RestSeconds,
    string? TechniqueNotes,
    string? CommonMistakes,
    string? Version);
```

`backend/src/Coaching.Application/Exercises/ExerciseCategoryCheck.cs`:
```csharp
using Coaching.Application.Common;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises;

internal static class ExerciseCategoryCheck
{
    /// <summary>
    /// @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004: thiếu nhóm cơ/dụng cụ → 400 riêng; mục không thuộc danh mục
    /// của coach (không có, của coach khác, hoặc sai loại) → 400 exercise.category_not_found.
    /// </summary>
    public static async Task<Result<(ExerciseCategory MuscleGroup, ExerciseCategory Equipment)>> ResolveAsync(
        IExerciseCategoryRepository categories, Guid coachId, Guid? muscleGroupId, Guid? equipmentId, CancellationToken ct)
    {
        if (muscleGroupId is not { } muscleId || muscleId == Guid.Empty)
            return ExerciseErrors.MuscleGroupRequired;
        if (equipmentId is not { } equipmentIdValue || equipmentIdValue == Guid.Empty)
            return ExerciseErrors.EquipmentRequired;

        var muscleGroup = await categories.GetAsync(muscleId, coachId, ct);
        var equipment = await categories.GetAsync(equipmentIdValue, coachId, ct);
        if (muscleGroup is not { Kind: CategoryKind.MuscleGroup } || equipment is not { Kind: CategoryKind.Equipment })
            return ExerciseErrors.CategoryNotFound;

        return Result<(ExerciseCategory MuscleGroup, ExerciseCategory Equipment)>.Success((muscleGroup, equipment));
    }

    /// <summary>Chi tiết bài kèm tên nhóm cơ/dụng cụ (mục danh mục không xoá được nên luôn có).</summary>
    public static async Task<ExerciseDetailDto> DetailAsync(IExerciseCategoryRepository categories, Exercise exercise,
        CancellationToken ct)
    {
        var muscleGroup = await categories.GetAsync(exercise.MuscleGroupId, exercise.CoachId, ct);
        var equipment = await categories.GetAsync(exercise.EquipmentId, exercise.CoachId, ct);
        return ExerciseDetailDto.From(exercise, muscleGroup!.Name, equipment!.Name);
    }
}
```

- [ ] **Step 8: Application — thêm, sửa, xem bài + DI**

`backend/src/Coaching.Application/Exercises/Commands/CreateExercise.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Commands;

public sealed class CreateExerciseHandler(
    CurrentCoach currentCoach,
    IExerciseCategoryRepository categories,
    IExerciseRepository exercises,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<ExerciseDetailDto>> HandleAsync(SaveExerciseCommand command, CancellationToken ct = default)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return StudentErrors.CoachNotFound;

        var resolved = await ExerciseCategoryCheck.ResolveAsync(categories, coach.Id, command.MuscleGroupId, command.EquipmentId, ct);
        if (!resolved.IsSuccess)
            return resolved.Error!;
        var (muscleGroup, equipment) = resolved.Value;

        // @BR-f03-thu-vien-bai-tap-002 → 400 exercise.name_required / exercise.name_too_long
        var exercise = Exercise.Create(coach.Id, command.Name, muscleGroup.Id, equipment.Id);

        // @BR-f03-thu-vien-bai-tap-001: trùng (tên chuẩn hoá, dụng cụ) kể cả bài đang ẩn → 409; bài trùng đang ẩn → gợi ý bỏ ẩn
        if (await exercises.FindByKeyAsync(coach.Id, exercise.NameKey, equipment.Id, ct) is { } existing)
            return ExerciseErrors.Duplicate(existing.IsHidden ? existing.Id : null);

        exercises.Add(exercise);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintException)
        {
            return ExerciseErrors.Duplicate(null); // @BR-f03-thu-vien-bai-tap-001 @ADR-f03-thu-vien-bai-tap-001: hai request đua nhau
        }

        return ExerciseDetailDto.From(exercise, muscleGroup.Name, equipment.Name);
    }
}
```

`backend/src/Coaching.Application/Exercises/Commands/UpdateExercise.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Commands;

public sealed class UpdateExerciseHandler(
    CurrentCoach currentCoach,
    IExerciseCategoryRepository categories,
    IExerciseRepository exercises,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<ExerciseDetailDto>> HandleAsync(Guid id, SaveExerciseCommand command, CancellationToken ct = default)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return StudentErrors.CoachNotFound;
        if (StudentVersion.Parse(command.Version) is not { } version)
            return ExerciseErrors.VersionRequired;

        // NFR-f03-thu-vien-bai-tap-002: bài không tồn tại hoặc của coach khác → 404
        if (await exercises.GetForUpdateAsync(id, coach.Id, version, ct) is not { } exercise)
            return ExerciseErrors.NotFound;

        var resolved = await ExerciseCategoryCheck.ResolveAsync(categories, coach.Id, command.MuscleGroupId, command.EquipmentId, ct);
        if (!resolved.IsSuccess)
            return resolved.Error!;
        var (muscleGroup, equipment) = resolved.Value;

        exercise.Rename(command.Name); // @BR-f03-thu-vien-bai-tap-002
        exercise.Categorise(muscleGroup.Id, equipment.Id);

        // @BR-f03-thu-vien-bai-tap-001: sửa thành (tên, dụng cụ) của bài KHÁC → 409; trùng chính nó (đổi hoa/thường) → lưu được
        if (await exercises.FindByKeyAsync(coach.Id, exercise.NameKey, exercise.EquipmentId, ct) is { } other && other.Id != exercise.Id)
            return ExerciseErrors.Duplicate(other.IsHidden ? other.Id : null);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            return ExerciseErrors.VersionConflict; // @BR-f03-thu-vien-bai-tap-020
        }
        catch (UniqueConstraintException)
        {
            return ExerciseErrors.Duplicate(null); // @BR-f03-thu-vien-bai-tap-001: hai request đua nhau
        }

        // @BR-f03-thu-vien-bai-tap-021: chỉ bản trong thư viện đổi; giáo án (F05) giữ bản chụp
        return ExerciseDetailDto.From(exercise, muscleGroup.Name, equipment.Name);
    }
}
```

`backend/src/Coaching.Application/Exercises/Queries/GetExercise.cs`:
```csharp
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Queries;

public sealed class GetExerciseHandler(
    CurrentCoach currentCoach,
    IExerciseRepository exercises,
    IExerciseCategoryRepository categories)
{
    public async Task<Result<ExerciseDetailDto>> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return StudentErrors.CoachNotFound;

        // NFR-f03-thu-vien-bai-tap-002: bài của coach khác coi như không tồn tại
        if (await exercises.GetAsync(id, coach.Id, ct) is not { } exercise)
            return ExerciseErrors.NotFound;

        return await ExerciseCategoryCheck.DetailAsync(categories, exercise, ct);
    }
}
```

`backend/src/Coaching.Application/DependencyInjection.cs` — đăng ký thêm (sau `AddCategoryHandler`):
```csharp
        services.AddScoped<CreateExerciseHandler>(); // @ADR-f03-thu-vien-bai-tap-001
        services.AddScoped<UpdateExerciseHandler>(); // @BR-f03-thu-vien-bai-tap-020
        services.AddScoped<GetExerciseHandler>();
```

- [ ] **Step 9: Infrastructure — cấu hình EF `Exercise`, repository, DbSet, DI**

`backend/src/Coaching.Infrastructure/Persistence/Configurations/ExerciseConfiguration.cs`:
```csharp
using Coaching.Domain.Coaches;
using Coaching.Domain.Exercises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Persistence.Configurations;

/// <summary>@ADR-f03-thu-vien-bai-tap-001: bảng Exercises (Liquibase v0.5.0).</summary>
internal sealed class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("Exercises");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Name).HasMaxLength(Exercise.NameMaxLength).IsRequired();
        builder.Property(e => e.NameKey).HasMaxLength(Exercise.KeyMaxLength).IsRequired();
        builder.Property(e => e.SearchKey).HasMaxLength(Exercise.KeyMaxLength).IsRequired();
        builder.Property(e => e.VideoUrl).HasMaxLength(Exercise.VideoUrlMaxLength);
        builder.Property(e => e.TechniqueNotes).HasMaxLength(Exercise.NotesMaxLength);
        builder.Property(e => e.CommonMistakes).HasMaxLength(Exercise.NotesMaxLength);
        builder.Ignore(e => e.HasVideo);

        // @ADR-f03-thu-vien-bai-tap-006: liều mặc định là các cột int NULL của chính bảng Exercises
        builder.ComplexProperty(e => e.Dose, dose =>
        {
            dose.Property(d => d.Sets).HasColumnName("DefaultSets");
            dose.Property(d => d.RepsMin).HasColumnName("DefaultRepsMin");
            dose.Property(d => d.RepsMax).HasColumnName("DefaultRepsMax");
            dose.Property(d => d.RirMin).HasColumnName("DefaultRirMin");
            dose.Property(d => d.RirMax).HasColumnName("DefaultRirMax");
            dose.Property(d => d.RestSeconds).HasColumnName("DefaultRestSeconds");
        });

        // @BR-f03-thu-vien-bai-tap-001 @ADR-f03-thu-vien-bai-tap-001: chốt chặn cuối khi hai request đua
        builder.HasIndex(e => new { e.CoachId, e.NameKey, e.EquipmentId }).IsUnique();
        builder.HasIndex(e => new { e.CoachId, e.IsHidden });
        builder.HasOne<Coach>().WithMany().HasForeignKey(e => e.CoachId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExerciseCategory>().WithMany().HasForeignKey(e => e.MuscleGroupId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExerciseCategory>().WithMany().HasForeignKey(e => e.EquipmentId).OnDelete(DeleteBehavior.Restrict);

        // @BR-f03-thu-vien-bai-tap-020: rowversion là concurrency token của mọi lệnh sửa
        builder.Property(e => e.Version).IsRowVersion();
        builder.Ignore(e => e.DomainEvents);
    }
}
```

`backend/src/Coaching.Infrastructure/Persistence/Repositories/ExerciseRepository.cs`:
```csharp
using Coaching.Domain.Exercises;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Persistence.Repositories;

internal sealed class ExerciseRepository(AppDbContext db) : IExerciseRepository
{
    public Task<Exercise?> GetAsync(Guid id, Guid coachId, CancellationToken ct = default) =>
        db.Exercises.SingleOrDefaultAsync(e => e.Id == id && e.CoachId == coachId, ct);

    public async Task<Exercise?> GetForUpdateAsync(Guid id, Guid coachId, byte[] expectedVersion, CancellationToken ct = default)
    {
        var exercise = await GetAsync(id, coachId, ct);
        if (exercise is not null)
            db.Entry(exercise).Property(e => e.Version).OriginalValue = expectedVersion; // @BR-f03-thu-vien-bai-tap-020
        return exercise;
    }

    // @BR-f03-thu-vien-bai-tap-001: theo chỉ mục duy nhất (CoachId, NameKey, EquipmentId) — gồm cả bài đang ẩn
    public Task<Exercise?> FindByKeyAsync(Guid coachId, string nameKey, Guid equipmentId, CancellationToken ct = default) =>
        db.Exercises.AsNoTracking()
            .SingleOrDefaultAsync(e => e.CoachId == coachId && e.NameKey == nameKey && e.EquipmentId == equipmentId, ct);

    public void Add(Exercise e) => db.Exercises.Add(e);

    public void Remove(Exercise e) => db.Exercises.Remove(e);
}
```

`backend/src/Coaching.Infrastructure/Persistence/AppDbContext.cs` — thêm DbSet (sau `ExerciseCategories`):
```csharp
    public DbSet<Exercise> Exercises => Set<Exercise>();
```

`backend/src/Coaching.Infrastructure/DependencyInjection.cs` — đăng ký (sau `IExerciseCategoryRepository`):
```csharp
        services.AddScoped<IExerciseRepository, ExerciseRepository>(); // @ADR-f03-thu-vien-bai-tap-001
```

- [ ] **Step 10: Api — `ProblemDetails` mang extension + `ExercisesController`**

`backend/src/Coaching.Api/Common/ResultExtensions.cs` — trong `ToProblem`, sau `problem.Extensions["code"] = error.Code;`:
```csharp
        // vd. hiddenExerciseId của exercise.duplicate (BR-f03-thu-vien-bai-tap-001)
        if (error.Extensions is not null)
        {
            foreach (var (key, value) in error.Extensions)
                problem.Extensions[key] = value;
        }
```

`backend/src/Coaching.Api/Controllers/Admin/ExercisesController.cs`:
```csharp
using Coaching.Api.Common;
using Coaching.Application.Exercises;
using Coaching.Application.Exercises.Commands;
using Coaching.Application.Exercises.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers.Admin;

/// <summary>Thư viện bài tập của coach đang đăng nhập (AdminOnly — NFR-f03-thu-vien-bai-tap-002).</summary>
public sealed class ExercisesController : AdminControllerBase
{
    // @ADR-f03-thu-vien-bai-tap-001 @BR-f03-thu-vien-bai-tap-001
    [HttpPost]
    [ProducesResponseType<ExerciseDetailDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] SaveExerciseCommand command, [FromServices] CreateExerciseHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : result.Error!.ToProblem(this);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ExerciseDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, [FromServices] GetExerciseHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, cancellationToken)).ToActionResult(this);

    // @BR-f03-thu-vien-bai-tap-020: bắt buộc version
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ExerciseDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveExerciseCommand command,
        [FromServices] UpdateExerciseHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, command, cancellationToken)).ToActionResult(this);
}
```

- [ ] **Step 11: Chạy integration test — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Exercises|FullyQualifiedName~ResultExtensionsTests"`
Expected: PASS — `ExerciseCrudTests` 16 test (gồm `BR001_Create_losing_the_unique_race_returns_409`, `BR020_Stale_version_is_rejected_and_keeps_the_previous_save`, `Coach_cannot_read_or_change_another_coachs_exercise`), `ExerciseCategoryTests` 18, `ExerciseSchemaTests` 1, `ResultExtensionsTests` 2 `Passed`, `Failed: 0`.

- [ ] **Step 12: Acceptance — `ExerciseApi` + bind Rule BR-001…004, BR-020 và scenario còn lại của BR-006**

`backend/tests/Coaching.Acceptance/Support/ExerciseApi.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coaching.Application.Exercises;
using Coaching.Application.Exercises.Commands;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Acceptance.Support;

/// <summary>Gọi /api/admin/exercises bằng phiên coach; nhớ Id bài theo tên trong scenario. Đọc bài KHÔNG ghi đè ctx.LastResponse.</summary>
public sealed class ExerciseApi(AcceptanceContext ctx, StudentApi coach, ExerciseCatalogApi catalog)
{
    public const string Url = "/api/admin/exercises";

    public Dictionary<string, Guid> Ids { get; } = new();

    public string? LastCreatedName { get; private set; }

    public Guid IdOf(string name) =>
        Ids.TryGetValue(name, out var id) ? id : throw new InvalidOperationException($"Scenario chưa tạo bài \"{name}\".");

    public async Task<SaveExerciseCommand> CommandAsync(string? name, string? muscleGroup, string? equipment) => new(
        name,
        muscleGroup is null ? null : await catalog.MuscleGroupIdAsync(muscleGroup),
        equipment is null ? null : await catalog.EquipmentIdAsync(equipment),
        null, null, null, null, null, null, null, null, null, null);

    public async Task<HttpResponseMessage> TryCreateAsync(SaveExerciseCommand command)
    {
        var response = await ctx.SendJsonAsync(HttpMethod.Post, Url, command, coach.AdminToken);
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var created = (await response.Content.ReadFromJsonAsync<ExerciseDetailDto>())!;
            Ids[created.Name] = created.Id;
            LastCreatedName = created.Name;
        }
        return response;
    }

    public async Task<HttpResponseMessage> TryCreateAsync(string? name, string? muscleGroup, string? equipment) =>
        await TryCreateAsync(await CommandAsync(name, muscleGroup, equipment));

    public async Task<ExerciseDetailDto> CreateOkAsync(string name, string muscleGroup, string equipment,
        Func<SaveExerciseCommand, SaveExerciseCommand>? change = null)
    {
        var command = await CommandAsync(name, muscleGroup, equipment);
        var response = await TryCreateAsync(change is null ? command : change(command));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await GetAsync(name);
    }

    public async Task<HttpResponseMessage> TryGetAsync(Guid id)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{Url}/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", coach.AdminToken);
        return await ctx.Client().SendAsync(request);
    }

    public async Task<ExerciseDetailDto> GetAsync(string name)
    {
        using var response = await TryGetAsync(IdOf(name));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ExerciseDetailDto>())!;
    }

    public static SaveExerciseCommand ToCommand(ExerciseDetailDto d) =>
        new(d.Name, d.MuscleGroupId, d.EquipmentId, d.VideoUrl, d.Sets, d.RepsMin, d.RepsMax, d.RirMin, d.RirMax,
            d.RestSeconds, d.TechniqueNotes, d.CommonMistakes, d.Version);

    /// <summary>PUT bài: đọc bản hiện tại, áp <paramref name="change"/>, gửi kèm version (mặc định: bản vừa đọc).</summary>
    public async Task<HttpResponseMessage> TryUpdateAsync(string name, Func<SaveExerciseCommand, SaveExerciseCommand> change,
        string? version = null)
    {
        var current = await GetAsync(name);
        var command = change(ToCommand(current)) with { Version = version ?? current.Version };
        var response = await ctx.SendJsonAsync(HttpMethod.Put, $"{Url}/{current.Id}", command, coach.AdminToken);
        if (response.IsSuccessStatusCode)
        {
            var saved = (await response.Content.ReadFromJsonAsync<ExerciseDetailDto>())!;
            Ids[saved.Name] = saved.Id;
        }
        return response;
    }

    /// <summary>Đặt bài ở trạng thái đã ẩn (SQL — Task 4 đổi sang gọi API ẩn bài).</summary>
    public async Task HideAsync(string name)
    {
        var id = IdOf(name);
        await using var scope = ctx.Host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Exercises] SET [IsHidden] = 1 WHERE [Id] = {id}");
    }

    public Task AssertErrorAsync(HttpStatusCode status, string code) => coach.AssertErrorAsync(status, code);
}
```

`backend/tests/Coaching.Acceptance/Steps/ExerciseSteps.cs`:
```csharp
using System.Net;
using System.Text.Json;
using Coaching.Acceptance.Support;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class ExerciseSteps(AcceptanceContext ctx, ExerciseApi exercises, ExerciseCatalogApi catalog)
{
    private const string DefaultMuscleGroup = "Ngực";
    private string? _staleVersion;

    // @BR-f03-thu-vien-bai-tap-001
    [Given(@"^thư viện có bài ""([^""]*)"" dụng cụ ""([^""]*)""$")]
    public Task LibraryHas(string name, string equipment) => exercises.CreateOkAsync(name, DefaultMuscleGroup, equipment);

    // @BR-f03-thu-vien-bai-tap-001
    [Given(@"^thư viện có bài ""([^""]*)"" dụng cụ ""([^""]*)"" và bài ""([^""]*)"" dụng cụ ""([^""]*)""$")]
    public async Task LibraryHasTwo(string first, string firstEquipment, string second, string secondEquipment)
    {
        await exercises.CreateOkAsync(first, DefaultMuscleGroup, firstEquipment);
        await exercises.CreateOkAsync(second, DefaultMuscleGroup, secondEquipment);
    }

    // @BR-f03-thu-vien-bai-tap-001 (dùng lại ở BR-015)
    [Given(@"^thư viện có bài ""([^""]*)"" dụng cụ ""([^""]*)"" đang ẩn$")]
    public async Task LibraryHasHidden(string name, string equipment)
    {
        await exercises.CreateOkAsync(name, DefaultMuscleGroup, equipment);
        await exercises.HideAsync(name);
    }

    // @BR-f03-thu-vien-bai-tap-001 @BR-f03-thu-vien-bai-tap-002 @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004
    [When(@"^coach thêm bài ""([^""]*)"" dụng cụ ""([^""]*)"" nhóm cơ ""([^""]*)""$")]
    public Task Add(string name, string equipment, string muscleGroup) => exercises.TryCreateAsync(name, muscleGroup, equipment);

    // @BR-f03-thu-vien-bai-tap-002
    [When(@"^coach thêm một bài không có tên, dụng cụ ""([^""]*)"" nhóm cơ ""([^""]*)""$")]
    public Task AddWithoutName(string equipment, string muscleGroup) => exercises.TryCreateAsync(null, muscleGroup, equipment);

    // @BR-f03-thu-vien-bai-tap-003
    [When(@"^coach thêm bài ""([^""]*)"" dụng cụ ""([^""]*)"", không có nhóm cơ$")]
    public Task AddWithoutMuscleGroup(string name, string equipment) => exercises.TryCreateAsync(name, null, equipment);

    // @BR-f03-thu-vien-bai-tap-004
    [When(@"^coach thêm bài ""([^""]*)"" nhóm cơ ""([^""]*)"", không có dụng cụ$")]
    public Task AddWithoutEquipment(string name, string muscleGroup) => exercises.TryCreateAsync(name, muscleGroup, null);

    // @BR-f03-thu-vien-bai-tap-001: "bài đó" = bài vừa tạo ở Given
    [When(@"^coach đổi tên bài đó thành ""([^""]*)""$")]
    public Task RenameLast(string newName) => exercises.TryUpdateAsync(exercises.LastCreatedName!, c => c with { Name = newName });

    // @BR-f03-thu-vien-bai-tap-001
    [When(@"^coach đổi tên bài ""([^""]*)"" thành ""([^""]*)""$")]
    public Task Rename(string name, string newName) => exercises.TryUpdateAsync(name, c => c with { Name = newName });

    // @BR-f03-thu-vien-bai-tap-020: giữ version lúc mở sửa, rồi "nơi khác" lưu đổi nhóm cơ sang Vai
    [Given(@"^coach đang sửa bài ""([^""]*)"" và bài đó vừa được sửa ở nơi khác$")]
    public async Task EditingWhileChangedElsewhere(string name)
    {
        _staleVersion = (await exercises.CreateOkAsync(name, DefaultMuscleGroup, "Machine")).Version;
        var vai = await catalog.MuscleGroupIdAsync("Vai");
        Assert.Equal(HttpStatusCode.OK, (await exercises.TryUpdateAsync(name, c => c with { MuscleGroupId = vai })).StatusCode);
    }

    // @BR-f03-thu-vien-bai-tap-020
    [When(@"^coach cập nhật bài ""([^""]*)"" dựa trên phiên bản đang sửa$")]
    public async Task UpdateFromStaleVersion(string name)
    {
        var lung = await catalog.MuscleGroupIdAsync("Lưng");
        await exercises.TryUpdateAsync(name, c => c with { MuscleGroupId = lung }, _staleVersion);
    }

    // @BR-f03-thu-vien-bai-tap-001 @BR-f03-thu-vien-bai-tap-002 @BR-f03-thu-vien-bai-tap-006 @BR-f03-thu-vien-bai-tap-017
    [Then(@"^thư viện có bài ""([^""]*)"" dụng cụ ""([^""]*)""$")]
    public async Task LibraryContains(string name, string equipment)
    {
        var exercise = await exercises.GetAsync(name);
        Assert.Equal(name, exercise.Name);
        Assert.Equal(equipment, exercise.Equipment);
    }

    // @BR-f03-thu-vien-bai-tap-001
    [Then(@"^bài không được (?:thêm|lưu) vì đã có bài cùng tên và dụng cụ$")]
    public Task Duplicate() => exercises.AssertErrorAsync(HttpStatusCode.Conflict, "exercise.duplicate");

    // @BR-f03-thu-vien-bai-tap-001: lỗi trùng chỉ tới bài đang ẩn
    [Then(@"^coach được gợi ý bỏ ẩn bài ""([^""]*)"" dụng cụ ""([^""]*)""$")]
    public async Task SuggestedToUnhide(string name, string equipment)
    {
        using var json = JsonDocument.Parse(await ctx.LastResponse!.Content.ReadAsStringAsync());
        Assert.Equal(exercises.IdOf(name), json.RootElement.GetProperty("hiddenExerciseId").GetGuid());
        var hidden = await exercises.GetAsync(name);
        Assert.True(hidden.IsHidden);
        Assert.Equal(equipment, hidden.Equipment);
    }

    // @BR-f03-thu-vien-bai-tap-001
    [Then(@"^bài ""([^""]*)"" giữ nguyên$")]
    public async Task Unchanged(string name) => Assert.Equal(name, (await exercises.GetAsync(name)).Name);

    // @BR-f03-thu-vien-bai-tap-002 @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004
    [Then(@"^bài không được thêm vì thiếu (tên|nhóm cơ|dụng cụ)$")]
    public Task Missing(string what) => exercises.AssertErrorAsync(HttpStatusCode.BadRequest, what switch
    {
        "tên" => "exercise.name_required",
        "nhóm cơ" => "exercise.muscle_group_required",
        _ => "exercise.equipment_required",
    });

    // @BR-f03-thu-vien-bai-tap-003
    [Then(@"^bài ""([^""]*)"" thuộc nhóm cơ ""([^""]*)""$")]
    public async Task HasMuscleGroup(string name, string muscleGroup) =>
        Assert.Equal(muscleGroup, (await exercises.GetAsync(name)).MuscleGroup);

    // @BR-f03-thu-vien-bai-tap-004
    [Then(@"^bài ""([^""]*)"" dùng dụng cụ ""([^""]*)""$")]
    public async Task UsesEquipment(string name, string equipment) =>
        Assert.Equal(equipment, (await exercises.GetAsync(name)).Equipment);

    // @BR-f03-thu-vien-bai-tap-020
    [Then(@"^bài không được lưu vì vừa được sửa ở nơi khác$")]
    public Task VersionConflict() => exercises.AssertErrorAsync(HttpStatusCode.Conflict, "exercise.version_conflict");

    // @BR-f03-thu-vien-bai-tap-020: lần lưu ở nơi khác (nhóm cơ Vai) không bị ghi đè
    [Then(@"^bài ""([^""]*)"" giữ nội dung của lần lưu trước$")]
    public async Task KeepsPreviousSave(string name) => Assert.Equal("Vai", (await exercises.GetAsync(name)).MuscleGroup);
}
```

- [ ] **Step 13: Chạy acceptance — kỳ vọng PASS cho Rule BR-001…004, 006, 020**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f03-thu-vien-bai-tap-001|Category=BR-f03-thu-vien-bai-tap-002|Category=BR-f03-thu-vien-bai-tap-003|Category=BR-f03-thu-vien-bai-tap-004|Category=BR-f03-thu-vien-bai-tap-005|Category=BR-f03-thu-vien-bai-tap-006|Category=BR-f03-thu-vien-bai-tap-020)&Category!=web&Category!=manual"`
Expected: PASS — 16 scenario/example `Passed` (BR-001 ×6, BR-002 ×2, BR-003 ×2, BR-004 ×2, BR-005 ×1, BR-006 ×2, BR-020 ×1), `Failed: 0`. Không có `No matching step definition found`.

- [ ] **Step 14: Verify tổng backend**

Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: Build thành công (0 warning). Mọi test `Passed` trừ scenario `@api` của `thu-vien-bai-tap.feature` thuộc BR-f03-thu-vien-bai-tap-007…019 và BR-021 fail `No matching step definition found` (interim rule — bind ở Task 3/4/5). `ResultExtensionsTests.Unavailable_error_maps_to_503_problem_with_code` vẫn pass (`Error` thêm tham số tuỳ chọn). Không lỗi khác.

- [ ] **Step 15: Commit**

```bash
git add backend/src/Coaching.Domain/Exercises/Exercise.cs backend/src/Coaching.Domain/Exercises/ExerciseDose.cs backend/src/Coaching.Domain/Exercises/IExerciseRepository.cs backend/src/Coaching.Application/Common/Result.cs backend/src/Coaching.Application/Exercises backend/src/Coaching.Application/DependencyInjection.cs backend/src/Coaching.Infrastructure/Persistence/Configurations/ExerciseConfiguration.cs backend/src/Coaching.Infrastructure/Persistence/Repositories/ExerciseRepository.cs backend/src/Coaching.Infrastructure/Persistence/AppDbContext.cs backend/src/Coaching.Infrastructure/DependencyInjection.cs backend/src/Coaching.Api/Common/ResultExtensions.cs backend/src/Coaching.Api/Controllers/Admin/ExercisesController.cs backend/tests/Coaching.Domain.Tests/Exercises/ExerciseTests.cs backend/tests/Coaching.Api.IntegrationTests/Common/ResultExtensionsTests.cs backend/tests/Coaching.Api.IntegrationTests/Exercises backend/tests/Coaching.Acceptance/Support/ExerciseApi.cs backend/tests/Coaching.Acceptance/Steps/ExerciseSteps.cs
git commit -m "$(cat <<'EOF'
feat(exercises): create, read and versioned update of library exercises; duplicate (name, equipment) incl. hidden (BR-f03-thu-vien-bai-tap-001..004, 020, 021)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
)"
```

---

### Task 3: Video mẫu YouTube, liều mặc định (Sets/Reps/RIR/nghỉ), hướng dẫn kỹ thuật, lỗi hay gặp

**Files:**
- Create: `backend/src/Coaching.Domain/Exercises/YouTubeUrl.cs`
- Modify: `backend/src/Coaching.Domain/Exercises/ExerciseDose.cs` (`Create` + giới hạn), `backend/src/Coaching.Domain/Exercises/Exercise.cs` (`SetVideo`, `SetDose`, `SetNotes`)
- Create: `backend/src/Coaching.Application/Exercises/ExerciseDetails.cs`
- Modify: `backend/src/Coaching.Application/Exercises/Commands/CreateExercise.cs`, `Commands/UpdateExercise.cs`
- Test: `backend/tests/Coaching.Domain.Tests/Exercises/ExerciseDoseTests.cs`, `YouTubeUrlTests.cs`, `ExerciseDetailTests.cs` (mới)
- Test: `backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseDetailApiTests.cs` (mới)
- Acceptance: `backend/tests/Coaching.Acceptance/Support/DoseText.cs`, `backend/tests/Coaching.Acceptance/Steps/ExerciseDetailSteps.cs` (mới)
- Living docs bind: Rule `@BR-f03-thu-vien-bai-tap-007`…`014` + scenario `@api` của `@BR-f03-thu-vien-bai-tap-021` (chờ từ Task 2). KHÔNG sửa `.feature`.

**Interfaces:**
- Consumes (Task 2): `Exercise` (+ `VideoUrlMaxLength`, `NotesMaxLength`, `HasVideo`, `Dose`), `ExerciseDose`, `SaveExerciseCommand`, `CreateExerciseHandler`, `UpdateExerciseHandler`, `ExerciseDetailDto`, `ExerciseRequests.CommandAsync/CreateAsync/UpdateAsync/GetAsync/UniqueName`, `ExerciseApi.CommandAsync/TryCreateAsync/CreateOkAsync/TryUpdateAsync/GetAsync/AssertErrorAsync`.
- Produces:
  - Domain: `YouTubeUrl.Normalize(string? raw)` (`MaxLength = 2048`; lỗi `exercise.video_not_youtube`); `ExerciseDose.Create(int? sets, int? repsMin, int? repsMax, int? rirMin, int? rirMax, int? restSeconds)` (lỗi `exercise.sets_out_of_range`, `exercise.reps_invalid`, `exercise.rir_invalid`, `exercise.rest_out_of_range`; một phía trống → gộp về phía có: min = max); `Exercise.SetVideo(string?)`, `SetDose(ExerciseDose)`, `SetNotes(string? technique, string? mistakes)` (lỗi `exercise.notes_too_long`).
  - Application: `ExerciseDetails.Apply(Exercise, SaveExerciseCommand)` (internal) — POST/PUT ghi video, liều, hướng dẫn, lỗi hay gặp.
  - Acceptance: `DoseText.ParseRange/FormatRange/FormatRest`.

**Owns:** `@BR-f03-thu-vien-bai-tap-007`, `@BR-f03-thu-vien-bai-tap-008`, `@BR-f03-thu-vien-bai-tap-009`, `@BR-f03-thu-vien-bai-tap-010`, `@BR-f03-thu-vien-bai-tap-011`, `@BR-f03-thu-vien-bai-tap-012`, `@BR-f03-thu-vien-bai-tap-013`, `@BR-f03-thu-vien-bai-tap-014` | `@ADR-f03-thu-vien-bai-tap-005`, `@ADR-f03-thu-vien-bai-tap-006`

Ghi chú bind step (đã soát `Steps/*.cs` gồm `ExerciseSteps`/`ExerciseCategorySteps` — không trùng; mọi regex neo `^…$`):
| Step trong living doc | Binding |
|---|---|
| `thư viện có bài "Chest Press" dụng cụ "Machine"` / `"Lateral Raise" dụng cụ "Cable"` | có sẵn `ExerciseSteps.LibraryHas` (Task 2) |
| `coach thêm bài "…" dụng cụ "…" nhóm cơ "…" với video "…"` / `… không có video` | **mới** `ExerciseDetailSteps.AddWithVideo` / `AddWithoutVideo` |
| `bài "…" có video "…"` / `bài không được thêm vì link video không phải YouTube` | **mới** `HasVideo` / `VideoNotYouTube` |
| `bài "…" bị đánh dấu thiếu video` / `bài "…" không bị đánh dấu thiếu video` | **mới** `MarkedMissingVideo` / `NotMarkedMissingVideo` |
| `coach cập nhật bài "…" với Sets mặc định N` / `… không có Sets mặc định` | **mới** `UpdateSets` / `UpdateWithoutSets` |
| `bài "…" có Sets mặc định N` / `bài "…" được lưu, chưa có Sets mặc định` / `bài không được lưu vì Sets phải từ 1 đến 10` | **mới** `HasSets` / `SavedWithoutSets` / `SetsOutOfRange` |
| `coach cập nhật bài "…" với Reps mặc định "…"` / `bài "…" có Reps mặc định "…"` / `bài không được lưu vì Reps không hợp lệ` | **mới** `UpdateReps` / `HasReps` / `RepsInvalid` |
| `coach cập nhật bài "…" với RIR mặc định "…"` / `bài "…" có RIR mặc định "…"` / `bài không được lưu vì RIR không hợp lệ` | **mới** `UpdateRir` / `HasRir` / `RirInvalid` |
| `coach cập nhật bài "…" với thời gian nghỉ mặc định N giây` / `bài "…" có thời gian nghỉ mặc định "…"` / `bài không được lưu vì thời gian nghỉ phải từ 15 đến 600 giây` | **mới** `UpdateRest` / `HasRest` / `RestOutOfRange` |
| `coach cập nhật bài "…" với hướng dẫn kỹ thuật "…"` / `bài "…" có hướng dẫn kỹ thuật "…"` | **mới** `UpdateTechnique` / `HasTechnique` |
| `coach cập nhật bài "…" với lỗi hay gặp "…"` / `bài "…" có lỗi hay gặp "…"` | **mới** `UpdateMistakes` / `HasMistakes` |
| `thư viện có bài "…" với hướng dẫn kỹ thuật "…"` (BR-021) | **mới** `LibraryHasWithTechnique` |

- [ ] **Step 1: Viết Domain test cho liều, link YouTube, video/hướng dẫn của bài**

`backend/tests/Coaching.Domain.Tests/Exercises/ExerciseDoseTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Exercises;

namespace Coaching.Domain.Tests.Exercises;

public sealed class ExerciseDoseTests
{
    private static string ErrorOf(Action action) => Assert.Throws<DomainException>(action).Code;

    // @ADR-f03-thu-vien-bai-tap-006: mọi phần của liều mặc định không bắt buộc
    [Fact]
    public void ADR006_Dose_without_any_value_is_empty() =>
        Assert.Equal(ExerciseDose.Empty, ExerciseDose.Create(null, null, null, null, null, null));

    // @BR-f03-thu-vien-bai-tap-009
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void BR009_Sets_from_1_to_10_are_kept(int sets) =>
        Assert.Equal(sets, ExerciseDose.Create(sets, null, null, null, null, null).Sets);

    // @BR-f03-thu-vien-bai-tap-009: 0, 11 → không lưu
    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void BR009_Sets_outside_1_to_10_are_rejected(int sets) =>
        Assert.Equal("exercise.sets_out_of_range", ErrorOf(() => ExerciseDose.Create(sets, null, null, null, null, null)));

    // @BR-f03-thu-vien-bai-tap-010: một số hoặc khoảng thấp–cao trong 1–100
    [Theory]
    [InlineData(10, 10)]
    [InlineData(10, 12)]
    [InlineData(1, 100)]
    public void BR010_Reps_number_or_range_within_1_to_100_is_kept(int min, int max)
    {
        var dose = ExerciseDose.Create(null, min, max, null, null, null);
        Assert.Equal((min, max), (dose.RepsMin!.Value, dose.RepsMax!.Value));
    }

    // @BR-f03-thu-vien-bai-tap-010 @ADR-f03-thu-vien-bai-tap-006: một số đơn lẻ lưu là thấp = cao
    [Fact]
    public void BR010_Single_reps_value_is_stored_as_min_equal_max()
    {
        var fromMin = ExerciseDose.Create(null, 10, null, null, null, null);
        var fromMax = ExerciseDose.Create(null, null, 12, null, null, null);

        Assert.Equal((10, 10), (fromMin.RepsMin!.Value, fromMin.RepsMax!.Value));
        Assert.Equal((12, 12), (fromMax.RepsMin!.Value, fromMax.RepsMax!.Value));
    }

    // @BR-f03-thu-vien-bai-tap-010: "12–10", "0", "101" → không lưu
    [Theory]
    [InlineData(12, 10)]
    [InlineData(0, 0)]
    [InlineData(101, 101)]
    [InlineData(0, 5)]
    [InlineData(5, 101)]
    public void BR010_Invalid_reps_are_rejected(int min, int max) =>
        Assert.Equal("exercise.reps_invalid", ErrorOf(() => ExerciseDose.Create(null, min, max, null, null, null)));

    // @BR-f03-thu-vien-bai-tap-011: một số hoặc khoảng thấp–cao trong 0–5
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(0, 1)]
    [InlineData(0, 5)]
    public void BR011_Rir_number_or_range_within_0_to_5_is_kept(int min, int max)
    {
        var dose = ExerciseDose.Create(null, null, null, min, max, null);
        Assert.Equal((min, max), (dose.RirMin!.Value, dose.RirMax!.Value));
    }

    // @BR-f03-thu-vien-bai-tap-011: "6", "2–1" → không lưu
    [Theory]
    [InlineData(6, 6)]
    [InlineData(2, 1)]
    [InlineData(-1, 0)]
    public void BR011_Invalid_rir_is_rejected(int min, int max) =>
        Assert.Equal("exercise.rir_invalid", ErrorOf(() => ExerciseDose.Create(null, null, null, min, max, null)));

    // @BR-f03-thu-vien-bai-tap-012: 15–600 giây
    [Theory]
    [InlineData(15)]
    [InlineData(90)]
    [InlineData(600)]
    public void BR012_Rest_from_15_to_600_seconds_is_kept(int rest) =>
        Assert.Equal(rest, ExerciseDose.Create(null, null, null, null, null, rest).RestSeconds);

    // @BR-f03-thu-vien-bai-tap-012: 10, 601 → không lưu
    [Theory]
    [InlineData(10)]
    [InlineData(14)]
    [InlineData(601)]
    public void BR012_Rest_outside_15_to_600_is_rejected(int rest) =>
        Assert.Equal("exercise.rest_out_of_range", ErrorOf(() => ExerciseDose.Create(null, null, null, null, null, rest)));
}
```

`backend/tests/Coaching.Domain.Tests/Exercises/YouTubeUrlTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Exercises;

namespace Coaching.Domain.Tests.Exercises;

public sealed class YouTubeUrlTests
{
    // @BR-f03-thu-vien-bai-tap-007 @ADR-f03-thu-vien-bai-tap-005: mọi link http/https của youtube.com, *.youtube.com, youtu.be — lưu nguyên văn (đã trim)
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=vcBig73ojpE")]
    [InlineData("https://youtube.com/shorts/PfmfAeMpym4")]
    [InlineData("https://youtu.be/abc123XYZ09")]
    [InlineData("https://www.youtube.com/playlist?list=PL123abc")]
    [InlineData("https://www.youtube.com/@duobexiuuu")]
    [InlineData("http://m.youtube.com/watch?v=vcBig73ojpE")]
    [InlineData("HTTPS://WWW.YOUTUBE.COM/watch?v=vcBig73ojpE")]
    public void BR007_YouTube_links_are_kept_verbatim(string link) => Assert.Equal(link, YouTubeUrl.Normalize(link));

    [Fact]
    public void BR007_Link_is_trimmed() =>
        Assert.Equal("https://youtu.be/abc123XYZ09", YouTubeUrl.Normalize("  https://youtu.be/abc123XYZ09 "));

    // @BR-f03-thu-vien-bai-tap-007: không có link → lưu được (không video)
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BR007_No_link_means_no_video(string? link) => Assert.Null(YouTubeUrl.Normalize(link));

    // @BR-f03-thu-vien-bai-tap-007 @ADR-f03-thu-vien-bai-tap-005: không phải YouTube, host giả mạo, scheme lạ, không phải URL tuyệt đối
    [Theory]
    [InlineData("https://vimeo.com/123")]
    [InlineData("https://youtube.com.evil.com/watch?v=x")]
    [InlineData("https://notyoutube.com/watch?v=x")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://youtube.com/video")]
    [InlineData("abc")]
    [InlineData("/watch?v=vcBig73ojpE")]
    public void BR007_Non_YouTube_links_are_rejected(string link) =>
        Assert.Equal("exercise.video_not_youtube", Assert.Throws<DomainException>(() => YouTubeUrl.Normalize(link)).Code);

    [Fact]
    public void BR007_Link_longer_than_2048_is_rejected() =>
        Assert.Equal("exercise.video_not_youtube", Assert.Throws<DomainException>(() =>
            YouTubeUrl.Normalize($"https://www.youtube.com/watch?v={new string('a', 2048)}")).Code);
}
```

`backend/tests/Coaching.Domain.Tests/Exercises/ExerciseDetailTests.cs`:
```csharp
using Coaching.Domain.Common;
using Coaching.Domain.Exercises;

namespace Coaching.Domain.Tests.Exercises;

public sealed class ExerciseDetailTests
{
    private static Exercise Create() => Exercise.Create(Guid.NewGuid(), "Lateral Raise", Guid.NewGuid(), Guid.NewGuid());

    private static string ErrorOf(Action action) => Assert.Throws<DomainException>(action).Code;

    // @BR-f03-thu-vien-bai-tap-008: có link → không thiếu video; bỏ link → thiếu video
    [Fact]
    public void BR008_Exercise_without_link_is_missing_video()
    {
        var exercise = Create();
        Assert.False(exercise.HasVideo);

        exercise.SetVideo("https://www.youtube.com/watch?v=Nhvz9EzdJ4U");
        Assert.True(exercise.HasVideo);
        Assert.Equal("https://www.youtube.com/watch?v=Nhvz9EzdJ4U", exercise.VideoUrl);

        exercise.SetVideo("  ");
        Assert.False(exercise.HasVideo);
        Assert.Null(exercise.VideoUrl);
    }

    // @BR-f03-thu-vien-bai-tap-007: link sai bị từ chối, video cũ giữ nguyên
    [Fact]
    public void BR007_Invalid_link_keeps_the_previous_video()
    {
        var exercise = Create();
        exercise.SetVideo("https://youtu.be/abc123XYZ09");

        Assert.Equal("exercise.video_not_youtube", ErrorOf(() => exercise.SetVideo("https://vimeo.com/123")));

        Assert.Equal("https://youtu.be/abc123XYZ09", exercise.VideoUrl);
    }

    // @ADR-f03-thu-vien-bai-tap-006
    [Fact]
    public void ADR006_SetDose_replaces_the_default_dose()
    {
        var exercise = Create();
        var dose = ExerciseDose.Create(3, 10, 12, 1, 2, 90);

        exercise.SetDose(dose);

        Assert.Equal(dose, exercise.Dose);
    }

    // @BR-f03-thu-vien-bai-tap-013 @BR-f03-thu-vien-bai-tap-014: không bắt buộc (trống → null), lưu đã trim, tối đa 2000 ký tự
    [Fact]
    public void BR013_BR014_Notes_are_optional_trimmed_and_at_most_2000_characters()
    {
        var exercise = Create();

        exercise.SetNotes("  Giữ khuỷu hơi cong, kéo bằng vai ", "Nhún vai khi kéo");
        Assert.Equal("Giữ khuỷu hơi cong, kéo bằng vai", exercise.TechniqueNotes);
        Assert.Equal("Nhún vai khi kéo", exercise.CommonMistakes);

        exercise.SetNotes(new string('a', 2000), "   ");
        Assert.Equal(2000, exercise.TechniqueNotes!.Length);
        Assert.Null(exercise.CommonMistakes);
    }

    // @BR-f03-thu-vien-bai-tap-013: 2001 ký tự → không lưu
    [Fact]
    public void BR013_Technique_notes_of_2001_characters_are_rejected() =>
        Assert.Equal("exercise.notes_too_long", ErrorOf(() => Create().SetNotes(new string('a', 2001), null)));

    // @BR-f03-thu-vien-bai-tap-014: 2001 ký tự → không lưu
    [Fact]
    public void BR014_Common_mistakes_of_2001_characters_are_rejected() =>
        Assert.Equal("exercise.notes_too_long", ErrorOf(() => Create().SetNotes(null, new string('a', 2001))));
}
```

- [ ] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Exercises"`
Expected: FAIL — build error `CS0117: 'ExerciseDose' does not contain a definition for 'Create'`, `CS0103: The name 'YouTubeUrl' does not exist in the current context`, `CS1061: 'Exercise' does not contain a definition for 'SetVideo'` (và `SetDose`, `SetNotes`).

- [ ] **Step 3: Domain — `YouTubeUrl`, `ExerciseDose.Create`, `Exercise.SetVideo/SetDose/SetNotes`**

`backend/src/Coaching.Domain/Exercises/YouTubeUrl.cs`:
```csharp
using Coaching.Domain.Common;

namespace Coaching.Domain.Exercises;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-005 @BR-f03-thu-vien-bai-tap-007: link video mẫu là URI tuyệt đối http/https, host youtube.com,
/// youtu.be hoặc kết thúc bằng ".youtube.com"; lưu nguyên văn (đã trim). Không kiểm video có tồn tại (spec §5).
/// FE chỉ nhúng khi rút được mã video 11 ký tự (youtube-nocookie.com/embed/&lt;id&gt;).
/// </summary>
public static class YouTubeUrl
{
    public const int MaxLength = Exercise.VideoUrlMaxLength;

    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var url = raw.Trim();
        if (url.Length <= MaxLength
            && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            && IsYouTubeHost(uri.Host))
            return url;

        throw new DomainException("exercise.video_not_youtube", "Link video phải là link YouTube (youtube.com hoặc youtu.be).");
    }

    private static bool IsYouTubeHost(string host)
    {
        var h = host.ToLowerInvariant();
        return h is "youtube.com" or "youtu.be" || h.EndsWith(".youtube.com", StringComparison.Ordinal);
    }
}
```

`backend/src/Coaching.Domain/Exercises/ExerciseDose.cs` — thay toàn bộ file:
```csharp
using Coaching.Domain.Common;

namespace Coaching.Domain.Exercises;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-006: liều mặc định có cấu trúc (Sets, Reps thấp–cao, RIR thấp–cao, nghỉ giây); mọi phần không
/// bắt buộc. Một số đơn lẻ lưu là thấp = cao (chỉ gửi một phía → gộp về phía có). F05 dùng lại số khi soạn giáo án.
/// </summary>
public sealed record ExerciseDose(int? Sets, int? RepsMin, int? RepsMax, int? RirMin, int? RirMax, int? RestSeconds)
{
    public const int SetsMin = 1;
    public const int SetsMax = 10;
    public const int RepsLowest = 1;
    public const int RepsHighest = 100;
    public const int RirLowest = 0;
    public const int RirHighest = 5;
    public const int RestMinSeconds = 15;
    public const int RestMaxSeconds = 600;

    public static readonly ExerciseDose Empty = new(null, null, null, null, null, null);

    public static ExerciseDose Create(int? sets, int? repsMin, int? repsMax, int? rirMin, int? rirMax, int? restSeconds)
    {
        // @BR-f03-thu-vien-bai-tap-009
        if (sets is < SetsMin or > SetsMax)
            throw new DomainException("exercise.sets_out_of_range", "Sets phải từ 1 đến 10.");

        // @BR-f03-thu-vien-bai-tap-010
        var (repsLow, repsHigh) = Range(repsMin, repsMax, RepsLowest, RepsHighest, "exercise.reps_invalid",
            "Reps phải là một số hoặc khoảng thấp–cao trong 1–100, số thấp không lớn hơn số cao.");

        // @BR-f03-thu-vien-bai-tap-011
        var (rirLow, rirHigh) = Range(rirMin, rirMax, RirLowest, RirHighest, "exercise.rir_invalid",
            "RIR phải là một số hoặc khoảng thấp–cao trong 0–5, số thấp không lớn hơn số cao.");

        // @BR-f03-thu-vien-bai-tap-012
        if (restSeconds is < RestMinSeconds or > RestMaxSeconds)
            throw new DomainException("exercise.rest_out_of_range", "Thời gian nghỉ phải từ 15 đến 600 giây.");

        return new ExerciseDose(sets, repsLow, repsHigh, rirLow, rirHigh, restSeconds);
    }

    private static (int? Low, int? High) Range(int? min, int? max, int lowest, int highest, string code, string message)
    {
        if (min is null && max is null)
            return (null, null);

        var low = min ?? max!.Value;
        var high = max ?? min!.Value;
        if (low < lowest || high > highest || low > high)
            throw new DomainException(code, message);
        return (low, high);
    }
}
```

`backend/src/Coaching.Domain/Exercises/Exercise.cs` — thêm các method sau vào cuối class (sau `Categorise`):
```csharp
    // @BR-f03-thu-vien-bai-tap-007 @ADR-f03-thu-vien-bai-tap-005: chỉ link YouTube; trống → không có video
    // @BR-f03-thu-vien-bai-tap-008: không có link = thiếu video (HasVideo)
    public void SetVideo(string? url) => VideoUrl = YouTubeUrl.Normalize(url);

    // @ADR-f03-thu-vien-bai-tap-006: liều đã kiểm bởi ExerciseDose.Create (BR-f03-thu-vien-bai-tap-009…012)
    public void SetDose(ExerciseDose dose) => Dose = dose ?? throw new ArgumentNullException(nameof(dose));

    // @BR-f03-thu-vien-bai-tap-013 @BR-f03-thu-vien-bai-tap-014: không bắt buộc, ≤ 2000 ký tự; kiểm cả hai trước khi ghi
    public void SetNotes(string? technique, string? mistakes)
    {
        var techniqueNotes = Notes(technique, "Hướng dẫn kỹ thuật");
        var commonMistakes = Notes(mistakes, "Lỗi hay gặp");
        TechniqueNotes = techniqueNotes;
        CommonMistakes = commonMistakes;
    }

    private static string? Notes(string? text, string label)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var trimmed = text.Trim();
        if (trimmed.Length > NotesMaxLength)
            throw new DomainException("exercise.notes_too_long", $"{label} tối đa {NotesMaxLength} ký tự.");
        return trimmed;
    }
```

- [ ] **Step 4: Chạy Domain test — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Exercises"`
Expected: PASS — `ExerciseDoseTests` 29, `YouTubeUrlTests` 19, `ExerciseDetailTests` 6, cùng `ExerciseTests` 14 và `ExerciseCatalogTests` 21 `Passed`, `Failed: 0`.

- [ ] **Step 5: Viết integration test — POST/PUT ghi đủ chi tiết, seed LOẠI qua API**

`backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseDetailApiTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Exercises;
using Coaching.Application.Exercises.Commands;

namespace Coaching.Api.IntegrationTests.Exercises;

[Collection(ApiCollection.Name)]
public sealed class ExerciseDetailApiTests(ApiFactory factory)
{
    private static string Name(string label) => ExerciseRequests.UniqueName(label);

    private async Task<HttpResponseMessage> PostAsync(Func<SaveExerciseCommand, SaveExerciseCommand> change)
    {
        var admin = await factory.AdminClientAsync();
        return await admin.PostAsJsonAsync(ExerciseRequests.Url,
            change(await ExerciseRequests.CommandAsync(admin, Name("Chest Dip"), "Ngực", "Bodyweight")));
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, string code)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(code, await StudentRequests.ErrorCodeAsync(response));
    }

    // @BR-f03-thu-vien-bai-tap-007 @BR-f03-thu-vien-bai-tap-008 @ADR-f03-thu-vien-bai-tap-006 @BR-f03-thu-vien-bai-tap-013 @BR-f03-thu-vien-bai-tap-014
    [Fact]
    public async Task Create_and_update_store_video_dose_and_notes()
    {
        var admin = await factory.AdminClientAsync();
        var created = await ExerciseRequests.CreateAsync(admin, Name("Lateral Raise"), "Vai", "Cable", c => c with
        {
            VideoUrl = " https://www.youtube.com/shorts/lMJUXEvcMkQ ",
            Sets = 3, RepsMin = 12, RepsMax = 15, RirMin = 0, RirMax = 1, RestSeconds = 60,
            TechniqueNotes = "Giữ khuỷu hơi cong, kéo bằng vai",
            CommonMistakes = "Nhún vai khi kéo",
        });

        Assert.Equal("https://www.youtube.com/shorts/lMJUXEvcMkQ", created.VideoUrl);
        Assert.True(created.HasVideo);
        Assert.Equal(new int?[] { 3, 12, 15, 0, 1, 60 },
            new[] { created.Sets, created.RepsMin, created.RepsMax, created.RirMin, created.RirMax, created.RestSeconds });
        Assert.Equal("Giữ khuỷu hơi cong, kéo bằng vai", created.TechniqueNotes);
        Assert.Equal("Nhún vai khi kéo", created.CommonMistakes);

        var response = await ExerciseRequests.UpdateAsync(admin, created, c => c with
        {
            VideoUrl = null, Sets = null, RepsMin = 10, RepsMax = null, RirMin = null, RirMax = null, RestSeconds = null,
            TechniqueNotes = "Mới", CommonMistakes = "  ",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await ExerciseRequests.GetAsync(admin, created.Id);
        Assert.False(updated.HasVideo);
        Assert.Null(updated.VideoUrl);
        Assert.Equal(new int?[] { null, 10, 10, null, null, null },
            new[] { updated.Sets, updated.RepsMin, updated.RepsMax, updated.RirMin, updated.RirMax, updated.RestSeconds });
        Assert.Equal("Mới", updated.TechniqueNotes);
        Assert.Null(updated.CommonMistakes);
    }

    // @BR-f03-thu-vien-bai-tap-007: link không phải YouTube → 400, bài không được thêm
    [Theory]
    [InlineData("https://vimeo.com/123")]
    [InlineData("https://youtube.com.evil.com/watch?v=x")]
    [InlineData("javascript:alert(1)")]
    [InlineData("abc")]
    public async Task BR007_Non_YouTube_link_is_rejected(string link) =>
        await AssertProblemAsync(await PostAsync(c => c with { VideoUrl = link }), "exercise.video_not_youtube");

    // @BR-f03-thu-vien-bai-tap-009: Sets "abc" (seed ❌) — không phải số → lỗi đọc body 400 request.invalid
    [Fact]
    public async Task BR009_Sets_that_is_not_a_number_is_rejected()
    {
        var admin = await factory.AdminClientAsync();
        var command = await ExerciseRequests.CommandAsync(admin, Name("Chest Press"));

        var response = await admin.PostAsJsonAsync(ExerciseRequests.Url, new
        {
            name = command.Name,
            muscleGroupId = command.MuscleGroupId,
            equipmentId = command.EquipmentId,
            sets = "abc",
        });

        await AssertProblemAsync(response, "request.invalid");
    }

    // @BR-f03-thu-vien-bai-tap-009 @BR-f03-thu-vien-bai-tap-010 @BR-f03-thu-vien-bai-tap-011 @BR-f03-thu-vien-bai-tap-012: ngoài khoảng → 400 đúng mã
    [Fact]
    public async Task BR009_BR012_Dose_out_of_range_is_rejected()
    {
        await AssertProblemAsync(await PostAsync(c => c with { Sets = 11 }), "exercise.sets_out_of_range");
        await AssertProblemAsync(await PostAsync(c => c with { RepsMin = 12, RepsMax = 10 }), "exercise.reps_invalid");
        await AssertProblemAsync(await PostAsync(c => c with { RepsMin = 101 }), "exercise.reps_invalid");
        await AssertProblemAsync(await PostAsync(c => c with { RirMin = 2, RirMax = 1 }), "exercise.rir_invalid");
        await AssertProblemAsync(await PostAsync(c => c with { RirMin = 6 }), "exercise.rir_invalid");
        await AssertProblemAsync(await PostAsync(c => c with { RestSeconds = 10 }), "exercise.rest_out_of_range");
        await AssertProblemAsync(await PostAsync(c => c with { RestSeconds = 601 }), "exercise.rest_out_of_range");
    }

    // @BR-f03-thu-vien-bai-tap-013 @BR-f03-thu-vien-bai-tap-014: ghi chú 2001 ký tự (seed ❌) → 400; 2000 lưu được
    [Fact]
    public async Task BR013_BR014_Notes_of_2001_characters_are_rejected()
    {
        await AssertProblemAsync(await PostAsync(c => c with { TechniqueNotes = new string('a', 2001) }), "exercise.notes_too_long");
        await AssertProblemAsync(await PostAsync(c => c with { CommonMistakes = new string('a', 2001) }), "exercise.notes_too_long");

        var ok = await PostAsync(c => c with { TechniqueNotes = new string('a', 2000), CommonMistakes = new string('b', 2000) });
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        Assert.Equal(2000, (await ok.Content.ReadFromJsonAsync<ExerciseDetailDto>())!.CommonMistakes!.Length);
    }
}
```

- [ ] **Step 6: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~ExerciseDetailApiTests"`
Expected: FAIL — `Create_and_update_store_video_dose_and_notes` (`Assert.Equal() Failure` — `VideoUrl` null vì handler chưa ghi), `BR007_Non_YouTube_link_is_rejected` ×4, `BR009_BR012_Dose_out_of_range_is_rejected`, `BR013_BR014_Notes_of_2001_characters_are_rejected` (`Expected: BadRequest, Actual: Created`). `BR009_Sets_that_is_not_a_number_is_rejected` PASS (model binding có sẵn).

- [ ] **Step 7: Application — `ExerciseDetails.Apply` và gọi ở thêm/sửa**

`backend/src/Coaching.Application/Exercises/ExerciseDetails.cs`:
```csharp
using Coaching.Application.Exercises.Commands;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises;

internal static class ExerciseDetails
{
    /// <summary>
    /// Ghi phần chi tiết của bài từ thân POST/PUT (thay toàn bộ): video (@BR-f03-thu-vien-bai-tap-007), liều mặc định
    /// (@ADR-f03-thu-vien-bai-tap-006, BR-f03-thu-vien-bai-tap-009…012), hướng dẫn kỹ thuật/lỗi hay gặp (BR-f03-thu-vien-bai-tap-013, 014).
    /// Sai → DomainException → 400 kèm mã.
    /// </summary>
    public static void Apply(Exercise exercise, SaveExerciseCommand command)
    {
        exercise.SetVideo(command.VideoUrl);
        exercise.SetDose(ExerciseDose.Create(command.Sets, command.RepsMin, command.RepsMax, command.RirMin, command.RirMax,
            command.RestSeconds));
        exercise.SetNotes(command.TechniqueNotes, command.CommonMistakes);
    }
}
```

`backend/src/Coaching.Application/Exercises/Commands/CreateExercise.cs` — ngay sau dòng `var exercise = Exercise.Create(...)`:
```csharp
        var exercise = Exercise.Create(coach.Id, command.Name, muscleGroup.Id, equipment.Id);
        ExerciseDetails.Apply(exercise, command); // @BR-f03-thu-vien-bai-tap-007…014
```

`backend/src/Coaching.Application/Exercises/Commands/UpdateExercise.cs` — ngay sau `exercise.Categorise(...)`:
```csharp
        exercise.Categorise(muscleGroup.Id, equipment.Id);
        ExerciseDetails.Apply(exercise, command); // @BR-f03-thu-vien-bai-tap-007…014
```

- [ ] **Step 8: Chạy integration test — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Exercises"`
Expected: PASS — `ExerciseDetailApiTests` 8 test, `ExerciseCrudTests` 16, `ExerciseCategoryTests` 18, `ExerciseSchemaTests` 1 `Passed`, `Failed: 0`.

- [ ] **Step 9: Acceptance — `DoseText` + bind Rule BR-007…014 và BR-021**

`backend/tests/Coaching.Acceptance/Support/DoseText.cs`:
```csharp
using System.Globalization;

namespace Coaching.Acceptance.Support;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-006: chữ liều như coach gõ ("10", "10–12", "10-12") ↔ số có cấu trúc của API;
/// hiển thị lại "10–12" (dấu gạch ngang en), "10" khi thấp = cao, "90s", trống là "—" (FE làm cùng quy tắc).
/// </summary>
public static class DoseText
{
    public static (int Min, int Max) ParseRange(string text)
    {
        var parts = text.Split(new[] { '–', '-' }, StringSplitOptions.TrimEntries);
        var min = int.Parse(parts[0], CultureInfo.InvariantCulture);
        return (min, parts.Length > 1 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : min);
    }

    public static string FormatRange(int? min, int? max) =>
        min is null ? "—" : min == max ? $"{min}" : $"{min}–{max}";

    public static string FormatRest(int? seconds) => seconds is null ? "—" : $"{seconds}s";
}
```

`backend/tests/Coaching.Acceptance/Steps/ExerciseDetailSteps.cs`:
```csharp
using System.Net;
using Coaching.Acceptance.Support;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class ExerciseDetailSteps(AcceptanceContext ctx, ExerciseApi exercises)
{
    // @BR-f03-thu-vien-bai-tap-007
    [When(@"^coach thêm bài ""([^""]*)"" dụng cụ ""([^""]*)"" nhóm cơ ""([^""]*)"" với video ""([^""]*)""$")]
    public async Task AddWithVideo(string name, string equipment, string muscleGroup, string link) =>
        await exercises.TryCreateAsync((await exercises.CommandAsync(name, muscleGroup, equipment)) with { VideoUrl = link });

    // @BR-f03-thu-vien-bai-tap-008
    [When(@"^coach thêm bài ""([^""]*)"" dụng cụ ""([^""]*)"" nhóm cơ ""([^""]*)"" không có video$")]
    public Task AddWithoutVideo(string name, string equipment, string muscleGroup) =>
        exercises.TryCreateAsync(name, muscleGroup, equipment);

    // @BR-f03-thu-vien-bai-tap-007 @ADR-f03-thu-vien-bai-tap-005: lưu nguyên văn
    [Then(@"^bài ""([^""]*)"" có video ""([^""]*)""$")]
    public async Task HasVideo(string name, string link)
    {
        Assert.Equal(HttpStatusCode.Created, ctx.LastResponse!.StatusCode);
        Assert.Equal(link, (await exercises.GetAsync(name)).VideoUrl);
    }

    // @BR-f03-thu-vien-bai-tap-007
    [Then(@"^bài không được thêm vì link video không phải YouTube$")]
    public Task VideoNotYouTube() => exercises.AssertErrorAsync(HttpStatusCode.BadRequest, "exercise.video_not_youtube");

    // @BR-f03-thu-vien-bai-tap-008
    [Then(@"^bài ""([^""]*)"" bị đánh dấu thiếu video$")]
    public async Task MarkedMissingVideo(string name) => Assert.False((await exercises.GetAsync(name)).HasVideo);

    // @BR-f03-thu-vien-bai-tap-008
    [Then(@"^bài ""([^""]*)"" không bị đánh dấu thiếu video$")]
    public async Task NotMarkedMissingVideo(string name) => Assert.True((await exercises.GetAsync(name)).HasVideo);

    // @BR-f03-thu-vien-bai-tap-009
    [When(@"^coach cập nhật bài ""([^""]*)"" với Sets mặc định (\d+)$")]
    public Task UpdateSets(string name, int sets) => exercises.TryUpdateAsync(name, c => c with { Sets = sets });

    // @BR-f03-thu-vien-bai-tap-009
    [When(@"^coach cập nhật bài ""([^""]*)"" không có Sets mặc định$")]
    public Task UpdateWithoutSets(string name) => exercises.TryUpdateAsync(name, c => c with { Sets = null });

    // @BR-f03-thu-vien-bai-tap-009
    [Then(@"^bài ""([^""]*)"" có Sets mặc định (\d+)$")]
    public async Task HasSets(string name, int sets) => Assert.Equal(sets, (await exercises.GetAsync(name)).Sets);

    // @BR-f03-thu-vien-bai-tap-009
    [Then(@"^bài ""([^""]*)"" được lưu, chưa có Sets mặc định$")]
    public async Task SavedWithoutSets(string name)
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        Assert.Null((await exercises.GetAsync(name)).Sets);
    }

    // @BR-f03-thu-vien-bai-tap-009
    [Then(@"^bài không được lưu vì Sets phải từ 1 đến 10$")]
    public Task SetsOutOfRange() => exercises.AssertErrorAsync(HttpStatusCode.BadRequest, "exercise.sets_out_of_range");

    // @BR-f03-thu-vien-bai-tap-010 @ADR-f03-thu-vien-bai-tap-006: "10" → 10/10; "10–12", "10-12" → 10/12
    [When(@"^coach cập nhật bài ""([^""]*)"" với Reps mặc định ""([^""]*)""$")]
    public Task UpdateReps(string name, string reps)
    {
        var (min, max) = DoseText.ParseRange(reps);
        return exercises.TryUpdateAsync(name, c => c with { RepsMin = min, RepsMax = max });
    }

    // @BR-f03-thu-vien-bai-tap-010
    [Then(@"^bài ""([^""]*)"" có Reps mặc định ""([^""]*)""$")]
    public async Task HasReps(string name, string reps)
    {
        var exercise = await exercises.GetAsync(name);
        Assert.Equal(reps, DoseText.FormatRange(exercise.RepsMin, exercise.RepsMax));
    }

    // @BR-f03-thu-vien-bai-tap-010
    [Then(@"^bài không được lưu vì Reps không hợp lệ$")]
    public Task RepsInvalid() => exercises.AssertErrorAsync(HttpStatusCode.BadRequest, "exercise.reps_invalid");

    // @BR-f03-thu-vien-bai-tap-011
    [When(@"^coach cập nhật bài ""([^""]*)"" với RIR mặc định ""([^""]*)""$")]
    public Task UpdateRir(string name, string rir)
    {
        var (min, max) = DoseText.ParseRange(rir);
        return exercises.TryUpdateAsync(name, c => c with { RirMin = min, RirMax = max });
    }

    // @BR-f03-thu-vien-bai-tap-011
    [Then(@"^bài ""([^""]*)"" có RIR mặc định ""([^""]*)""$")]
    public async Task HasRir(string name, string rir)
    {
        var exercise = await exercises.GetAsync(name);
        Assert.Equal(rir, DoseText.FormatRange(exercise.RirMin, exercise.RirMax));
    }

    // @BR-f03-thu-vien-bai-tap-011
    [Then(@"^bài không được lưu vì RIR không hợp lệ$")]
    public Task RirInvalid() => exercises.AssertErrorAsync(HttpStatusCode.BadRequest, "exercise.rir_invalid");

    // @BR-f03-thu-vien-bai-tap-012
    [When(@"^coach cập nhật bài ""([^""]*)"" với thời gian nghỉ mặc định (\d+) giây$")]
    public Task UpdateRest(string name, int seconds) => exercises.TryUpdateAsync(name, c => c with { RestSeconds = seconds });

    // @BR-f03-thu-vien-bai-tap-012: đọc là "90s"
    [Then(@"^bài ""([^""]*)"" có thời gian nghỉ mặc định ""([^""]*)""$")]
    public async Task HasRest(string name, string rest) =>
        Assert.Equal(rest, DoseText.FormatRest((await exercises.GetAsync(name)).RestSeconds));

    // @BR-f03-thu-vien-bai-tap-012
    [Then(@"^bài không được lưu vì thời gian nghỉ phải từ 15 đến 600 giây$")]
    public Task RestOutOfRange() => exercises.AssertErrorAsync(HttpStatusCode.BadRequest, "exercise.rest_out_of_range");

    // @BR-f03-thu-vien-bai-tap-013 @BR-f03-thu-vien-bai-tap-021
    [When(@"^coach cập nhật bài ""([^""]*)"" với hướng dẫn kỹ thuật ""([^""]*)""$")]
    public Task UpdateTechnique(string name, string text) => exercises.TryUpdateAsync(name, c => c with { TechniqueNotes = text });

    // @BR-f03-thu-vien-bai-tap-013 @BR-f03-thu-vien-bai-tap-021
    [Then(@"^bài ""([^""]*)"" có hướng dẫn kỹ thuật ""([^""]*)""$")]
    public async Task HasTechnique(string name, string text) =>
        Assert.Equal(text, (await exercises.GetAsync(name)).TechniqueNotes);

    // @BR-f03-thu-vien-bai-tap-014
    [When(@"^coach cập nhật bài ""([^""]*)"" với lỗi hay gặp ""([^""]*)""$")]
    public Task UpdateMistakes(string name, string text) => exercises.TryUpdateAsync(name, c => c with { CommonMistakes = text });

    // @BR-f03-thu-vien-bai-tap-014
    [Then(@"^bài ""([^""]*)"" có lỗi hay gặp ""([^""]*)""$")]
    public async Task HasMistakes(string name, string text) =>
        Assert.Equal(text, (await exercises.GetAsync(name)).CommonMistakes);

    // @BR-f03-thu-vien-bai-tap-021
    [Given(@"^thư viện có bài ""([^""]*)"" với hướng dẫn kỹ thuật ""([^""]*)""$")]
    public Task LibraryHasWithTechnique(string name, string text) =>
        exercises.CreateOkAsync(name, "Vai", "Cable", c => c with { TechniqueNotes = text });
}
```

- [ ] **Step 10: Chạy acceptance Rule BR-007…014, BR-021 — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f03-thu-vien-bai-tap-007|Category=BR-f03-thu-vien-bai-tap-008|Category=BR-f03-thu-vien-bai-tap-009|Category=BR-f03-thu-vien-bai-tap-010|Category=BR-f03-thu-vien-bai-tap-011|Category=BR-f03-thu-vien-bai-tap-012|Category=BR-f03-thu-vien-bai-tap-013|Category=BR-f03-thu-vien-bai-tap-014|Category=BR-f03-thu-vien-bai-tap-021)&Category!=web&Category!=manual"`
Expected: PASS — 31 scenario/example `Passed` (BR-007 ×9, BR-008 ×2, BR-009 ×4, BR-010 ×6, BR-011 ×4, BR-012 ×3, BR-013 ×1, BR-014 ×1, BR-021 ×1), `Failed: 0`.

- [ ] **Step 11: Verify tổng backend**

Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: Build thành công (0 warning). Mọi test `Passed` trừ scenario `@api` của `thu-vien-bai-tap.feature` thuộc BR-f03-thu-vien-bai-tap-015…019 fail `No matching step definition found` (interim rule — Task 4/5). Không lỗi khác.

- [ ] **Step 12: Commit**

```bash
git add backend/src/Coaching.Domain/Exercises/YouTubeUrl.cs backend/src/Coaching.Domain/Exercises/ExerciseDose.cs backend/src/Coaching.Domain/Exercises/Exercise.cs backend/src/Coaching.Application/Exercises/ExerciseDetails.cs backend/src/Coaching.Application/Exercises/Commands/CreateExercise.cs backend/src/Coaching.Application/Exercises/Commands/UpdateExercise.cs backend/tests/Coaching.Domain.Tests/Exercises/ExerciseDoseTests.cs backend/tests/Coaching.Domain.Tests/Exercises/YouTubeUrlTests.cs backend/tests/Coaching.Domain.Tests/Exercises/ExerciseDetailTests.cs backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseDetailApiTests.cs backend/tests/Coaching.Acceptance/Support/DoseText.cs backend/tests/Coaching.Acceptance/Steps/ExerciseDetailSteps.cs
git commit -m "$(cat <<'EOF'
feat(exercises): YouTube video link, structured default dose, technique notes and common mistakes (BR-f03-thu-vien-bai-tap-007..014)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
)"
```

---

### Task 4: Ẩn, bỏ ẩn, xoá bài + điểm cắm "bài đang được dùng" (`IExerciseUsage`)

**Files:**
- Modify: `backend/src/Coaching.Domain/Exercises/Exercise.cs` (`Hide`, `Unhide`)
- Create: `backend/src/Coaching.Application/Exercises/IExerciseUsage.cs`, `Exercises/Commands/HideExercise.cs`, `Exercises/Commands/DeleteExercise.cs`
- Modify: `backend/src/Coaching.Application/Exercises/ExerciseErrors.cs` (`InUse`), `backend/src/Coaching.Application/DependencyInjection.cs`
- Modify: `backend/src/Coaching.Api/Controllers/Admin/ExercisesController.cs` (`POST {id}/hide`, `POST {id}/unhide`, `DELETE {id}`)
- Test: `backend/tests/Coaching.Domain.Tests/Exercises/ExerciseVisibilityTests.cs` (mới)
- Test: `backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseHideDeleteTests.cs` (mới)
- Acceptance: Modify `backend/tests/Coaching.Acceptance/Support/ExerciseApi.cs` (ẩn bằng API thay SQL; `TryHideAsync/TryUnhideAsync/TryDeleteAsync`); Create `backend/tests/Coaching.Acceptance/Steps/ExerciseVisibilitySteps.cs`
- Living docs bind: Rule `@BR-f03-thu-vien-bai-tap-015`, `@BR-f03-thu-vien-bai-tap-017` (2 scenario `@manual` của BR-017/BR-021 bị loại bằng `Category!=manual`, chờ F05). KHÔNG sửa `.feature`.

**Interfaces:**
- Consumes (Task 2/3): `Exercise.IsHidden`, `IExerciseRepository.GetAsync/Remove`, `ExerciseCategoryCheck.DetailAsync`, `ExerciseErrors.NotFound/VersionConflict`, `ConcurrencyConflictException`, `ExerciseRequests.*`, `ExerciseApi.*`, step có sẵn `ExerciseSteps.LibraryHas/LibraryHasHidden/Add/LibraryContains`.
- Produces:
  - Domain: `Exercise.Hide()`, `Exercise.Unhide()` (lặp lại không lỗi).
  - Application: `IExerciseUsage { Task<bool> IsUsedAsync(Guid exerciseId, CancellationToken ct = default); }`, `NoExerciseUsage` (luôn `false`, đăng ký mặc định — F05 thay bằng truy vấn giáo án); `ExerciseErrors.InUse` (409 `exercise.in_use` "Bài đang có trong giáo án, chỉ ẩn được."); `HideExerciseHandler` / `UnhideExerciseHandler` `.HandleAsync(Guid id, ct)` → `Result<ExerciseDetailDto>`; `DeleteExerciseHandler.HandleAsync(Guid id, ct)` → `Result`.
  - Api: `POST /api/admin/exercises/{id}/hide`, `POST /api/admin/exercises/{id}/unhide` → 200 `ExerciseDetailDto` | 404; `DELETE /api/admin/exercises/{id}` → 204 | 404 | 409 `exercise.in_use`.
  - Acceptance: `ExerciseApi.TryHideAsync/TryUnhideAsync/TryDeleteAsync`, `ExerciseApi.HideAsync` gọi API.

**Owns:** `@BR-f03-thu-vien-bai-tap-015`, `@BR-f03-thu-vien-bai-tap-017` | `@ADR-f03-thu-vien-bai-tap-004`

Ghi chú bind step (đã soát `Steps/*.cs` gồm `Exercise*Steps` của Task 1–3 — không trùng):
| Step trong living doc | Binding |
|---|---|
| `thư viện có bài "Chest Dip" dụng cụ "Bodyweight"` / `… đang ẩn` | có sẵn `ExerciseSteps.LibraryHas` / `LibraryHasHidden` (Task 2; nay ẩn qua API) |
| `coach ẩn bài "…"` / `coach bỏ ẩn bài "…"` | **mới** `ExerciseVisibilitySteps.Hide` / `Unhide` |
| `bài "…" ở trạng thái đã ẩn` / `bài "…" không còn ở trạng thái đã ẩn` | **mới** `IsHidden` / `IsVisible` |
| `thư viện có bài "…" dụng cụ "…" chưa có giáo án nào dùng` | **mới** `LibraryHasUnused` |
| `coach xoá bài "…"` (When) / `coach đã xoá bài "…"` (Given) | **mới** `Delete` / `Deleted` |
| `thư viện không còn bài "…" dụng cụ "…"` | **mới** `LibraryNoLongerHas` |
| `coach thêm bài "Chest Dip" dụng cụ "Bodyweight" nhóm cơ "Ngực"` / `thư viện có bài "…" dụng cụ "…"` (Then) | có sẵn `ExerciseSteps.Add` / `LibraryContains` |

- [ ] **Step 1: Viết Domain test cho ẩn/bỏ ẩn**

`backend/tests/Coaching.Domain.Tests/Exercises/ExerciseVisibilityTests.cs`:
```csharp
using Coaching.Domain.Exercises;

namespace Coaching.Domain.Tests.Exercises;

public sealed class ExerciseVisibilityTests
{
    // @BR-f03-thu-vien-bai-tap-015: ẩn và bỏ ẩn được bất kỳ bài nào; lặp lại không lỗi; nội dung bài không đổi
    [Fact]
    public void BR015_Hide_and_unhide_any_exercise()
    {
        var exercise = Exercise.Create(Guid.NewGuid(), "Chest Dip", Guid.NewGuid(), Guid.NewGuid());

        exercise.Hide();
        exercise.Hide();
        Assert.True(exercise.IsHidden);
        Assert.Equal("Chest Dip", exercise.Name);

        exercise.Unhide();
        exercise.Unhide();
        Assert.False(exercise.IsHidden);
    }
}
```

- [ ] **Step 2: Viết integration test — ẩn/bỏ ẩn/xoá qua API, điểm cắm "đang được dùng", phân quyền**

`backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseHideDeleteTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coaching.Api.IntegrationTests.Students;
using Coaching.Application.Exercises;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Coaching.Api.IntegrationTests.Exercises;

[Collection(ApiCollection.Name)]
public sealed class ExerciseHideDeleteTests(ApiFactory factory)
{
    private static string Name(string label) => ExerciseRequests.UniqueName(label);

    private static string Url(Guid id, string? action = null) =>
        action is null ? $"{ExerciseRequests.Url}/{id}" : $"{ExerciseRequests.Url}/{id}/{action}";

    // @BR-f03-thu-vien-bai-tap-015: ẩn → đã ẩn; bỏ ẩn → hết ẩn; gọi lại không lỗi
    [Fact]
    public async Task BR015_Hide_and_unhide_through_the_api()
    {
        var admin = await factory.AdminClientAsync();
        var exercise = await ExerciseRequests.CreateAsync(admin, Name("Chest Dip"), "Ngực", "Bodyweight");

        var hidden = await admin.PostAsync(Url(exercise.Id, "hide"), null);
        Assert.Equal(HttpStatusCode.OK, hidden.StatusCode);
        Assert.True((await hidden.Content.ReadFromJsonAsync<ExerciseDetailDto>())!.IsHidden);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync(Url(exercise.Id, "hide"), null)).StatusCode);
        Assert.True((await ExerciseRequests.GetAsync(admin, exercise.Id)).IsHidden);

        var shown = await admin.PostAsync(Url(exercise.Id, "unhide"), null);
        Assert.Equal(HttpStatusCode.OK, shown.StatusCode);
        Assert.False((await shown.Content.ReadFromJsonAsync<ExerciseDetailDto>())!.IsHidden);
        Assert.False((await ExerciseRequests.GetAsync(admin, exercise.Id)).IsHidden);
    }

    // @BR-f03-thu-vien-bai-tap-017: bài chưa được dùng xoá được; cặp (tên, dụng cụ) dùng lại được
    [Fact]
    public async Task BR017_Unused_exercise_is_deleted_and_its_name_can_be_reused()
    {
        var admin = await factory.AdminClientAsync();
        var exercise = await ExerciseRequests.CreateAsync(admin, Name("Chest Dip"), "Ngực", "Bodyweight");

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync(Url(exercise.Id))).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(Url(exercise.Id))).StatusCode);
        var again = await ExerciseRequests.CreateAsync(admin, exercise.Name, "Ngực", "Bodyweight");
        Assert.NotEqual(exercise.Id, again.Id);
    }

    // @BR-f03-thu-vien-bai-tap-017 @ADR-f03-thu-vien-bai-tap-004: bài đang dùng (bản giả trả true) → 409, bài vẫn còn, ẩn vẫn được
    [Fact]
    public async Task BR017_Exercise_in_use_cannot_be_deleted_only_hidden()
    {
        await using var host = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            services.RemoveAll<IExerciseUsage>();
            services.AddScoped<IExerciseUsage, AlwaysUsed>();
        }));
        var admin = await ExerciseRequests.AdminClientAsync(factory, host);
        var exercise = await ExerciseRequests.CreateAsync(admin, Name("Chest Press"));

        var response = await admin.DeleteAsync(Url(exercise.Id));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using (var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
        {
            Assert.Equal("exercise.in_use", json.RootElement.GetProperty("code").GetString());
            Assert.Equal("Bài đang có trong giáo án, chỉ ẩn được.", json.RootElement.GetProperty("title").GetString());
        }
        Assert.Equal(exercise.Name, (await ExerciseRequests.GetAsync(admin, exercise.Id)).Name);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync(Url(exercise.Id, "hide"), null)).StatusCode);
    }

    // @ADR-f03-thu-vien-bai-tap-004: P1 chưa có giáo án — cài đặt mặc định luôn "chưa được dùng"
    [Fact]
    public async Task ADR004_Default_usage_is_never_used()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var usage = scope.ServiceProvider.GetRequiredService<IExerciseUsage>();

        Assert.IsType<NoExerciseUsage>(usage);
        Assert.False(await usage.IsUsedAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Unknown_exercise_cannot_be_hidden_or_deleted()
    {
        var admin = await factory.AdminClientAsync();
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync(Url(id, "hide"), null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync(Url(id, "unhide"), null)).StatusCode);
        var deleted = await admin.DeleteAsync(Url(id));
        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
        Assert.Equal("exercise.not_found", await StudentRequests.ErrorCodeAsync(deleted));
    }

    // NFR-f03-thu-vien-bai-tap-002: bài của coach khác → 404, không bị ẩn/xoá
    [Fact]
    public async Task Coach_cannot_hide_or_delete_another_coachs_exercise()
    {
        var (_, other) = await ExerciseRequests.NewCoachAsync(factory);
        var theirs = await ExerciseRequests.CreateAsync(other, Name("Riêng"));
        var admin = await factory.AdminClientAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync(Url(theirs.Id, "hide"), null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync(Url(theirs.Id, "unhide"), null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync(Url(theirs.Id))).StatusCode);

        var intact = await ExerciseRequests.GetAsync(other, theirs.Id);
        Assert.False(intact.IsHidden);
    }

    // NFR-f03-thu-vien-bai-tap-002
    [Fact]
    public async Task Student_cannot_hide_or_delete()
    {
        var exercise = await ExerciseRequests.CreateAsync(await factory.AdminClientAsync(), Name("Chest Press"));
        var student = await ExerciseRequests.StudentClientAsync(factory);

        Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsync(Url(exercise.Id, "hide"), null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.DeleteAsync(Url(exercise.Id))).StatusCode);
    }

    private sealed class AlwaysUsed : IExerciseUsage
    {
        public Task<bool> IsUsedAsync(Guid exerciseId, CancellationToken ct = default) => Task.FromResult(true);
    }
}
```

- [ ] **Step 3: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~ExerciseVisibilityTests"` và `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~ExerciseHideDeleteTests"`
Expected: FAIL — build error `CS1061: 'Exercise' does not contain a definition for 'Hide'` (và `'Unhide'`), `CS0246: The type or namespace name 'IExerciseUsage' could not be found`, `'NoExerciseUsage' could not be found`.

- [ ] **Step 4: Domain — `Hide`, `Unhide`**

`backend/src/Coaching.Domain/Exercises/Exercise.cs` — thêm vào cuối class:
```csharp
    // @BR-f03-thu-vien-bai-tap-015: coach ẩn/bỏ ẩn được bất kỳ bài nào của mình; lặp lại không lỗi
    public void Hide() => IsHidden = true;

    // @BR-f03-thu-vien-bai-tap-015
    public void Unhide() => IsHidden = false;
```

- [ ] **Step 5: Application — `IExerciseUsage`, `InUse`, ẩn/bỏ ẩn/xoá, DI**

`backend/src/Coaching.Application/Exercises/IExerciseUsage.cs`:
```csharp
namespace Coaching.Application.Exercises;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-004: điểm cắm "bài đang được dùng trong giáo án". F05 thay đăng ký bằng truy vấn giáo án thật;
/// luồng xoá không phải sửa.
/// </summary>
public interface IExerciseUsage
{
    Task<bool> IsUsedAsync(Guid exerciseId, CancellationToken ct = default);
}

/// <summary>@ADR-f03-thu-vien-bai-tap-004: P1 chưa có giáo án → không bài nào đang được dùng.</summary>
public sealed class NoExerciseUsage : IExerciseUsage
{
    public Task<bool> IsUsedAsync(Guid exerciseId, CancellationToken ct = default) => Task.FromResult(false);
}
```

`backend/src/Coaching.Application/Exercises/ExerciseErrors.cs` — thêm vào class:
```csharp
    // @BR-f03-thu-vien-bai-tap-017
    public static readonly Error InUse =
        new("exercise.in_use", "Bài đang có trong giáo án, chỉ ẩn được.", ErrorKind.Conflict);
```

`backend/src/Coaching.Application/Exercises/Commands/HideExercise.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Commands;

/// <summary>@BR-f03-thu-vien-bai-tap-015: ẩn bài (không cần version — chỉ đổi cờ ẩn).</summary>
public sealed class HideExerciseHandler(
    CurrentCoach currentCoach,
    IExerciseRepository exercises,
    IExerciseCategoryRepository categories,
    IUnitOfWork unitOfWork)
{
    public Task<Result<ExerciseDetailDto>> HandleAsync(Guid id, CancellationToken ct = default) =>
        ExerciseVisibility.ChangeAsync(currentCoach, exercises, categories, unitOfWork, id, e => e.Hide(), ct);
}

/// <summary>@BR-f03-thu-vien-bai-tap-015: bỏ ẩn bài.</summary>
public sealed class UnhideExerciseHandler(
    CurrentCoach currentCoach,
    IExerciseRepository exercises,
    IExerciseCategoryRepository categories,
    IUnitOfWork unitOfWork)
{
    public Task<Result<ExerciseDetailDto>> HandleAsync(Guid id, CancellationToken ct = default) =>
        ExerciseVisibility.ChangeAsync(currentCoach, exercises, categories, unitOfWork, id, e => e.Unhide(), ct);
}

internal static class ExerciseVisibility
{
    public static async Task<Result<ExerciseDetailDto>> ChangeAsync(CurrentCoach currentCoach, IExerciseRepository exercises,
        IExerciseCategoryRepository categories, IUnitOfWork unitOfWork, Guid id, Action<Exercise> change, CancellationToken ct)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return StudentErrors.CoachNotFound;
        // NFR-f03-thu-vien-bai-tap-002: bài của coach khác → 404
        if (await exercises.GetAsync(id, coach.Id, ct) is not { } exercise)
            return ExerciseErrors.NotFound;

        change(exercise);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            return ExerciseErrors.VersionConflict; // bài bị sửa ở nơi khác giữa lúc đọc và lúc lưu
        }

        return await ExerciseCategoryCheck.DetailAsync(categories, exercise, ct);
    }
}
```

`backend/src/Coaching.Application/Exercises/Commands/DeleteExercise.cs`:
```csharp
using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Commands;

public sealed class DeleteExerciseHandler(
    CurrentCoach currentCoach,
    IExerciseRepository exercises,
    IExerciseUsage usage,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return Result.Failure(StudentErrors.CoachNotFound);
        if (await exercises.GetAsync(id, coach.Id, ct) is not { } exercise)
            return Result.Failure(ExerciseErrors.NotFound); // NFR-f03-thu-vien-bai-tap-002

        // @BR-f03-thu-vien-bai-tap-017 @ADR-f03-thu-vien-bai-tap-004: bài đang có trong giáo án chỉ ẩn được
        if (await usage.IsUsedAsync(exercise.Id, ct))
            return Result.Failure(ExerciseErrors.InUse);

        // @BR-f03-thu-vien-bai-tap-017: xoá thật → cặp (tên, dụng cụ) rời chỉ mục duy nhất, thêm lại được
        exercises.Remove(exercise);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure(ExerciseErrors.VersionConflict);
        }
        return Result.Success();
    }
}
```

`backend/src/Coaching.Application/DependencyInjection.cs` — đăng ký thêm (sau `GetExerciseHandler`):
```csharp
        services.AddScoped<HideExerciseHandler>(); // @BR-f03-thu-vien-bai-tap-015
        services.AddScoped<UnhideExerciseHandler>();
        services.AddScoped<DeleteExerciseHandler>(); // @BR-f03-thu-vien-bai-tap-017
        // @ADR-f03-thu-vien-bai-tap-004: F05 đăng ký lại IExerciseUsage bằng truy vấn giáo án (đăng ký sau thắng)
        services.AddScoped<IExerciseUsage, NoExerciseUsage>();
```
và thêm `using Coaching.Application.Exercises;` ở đầu file.

- [ ] **Step 6: Api — hide, unhide, delete**

`backend/src/Coaching.Api/Controllers/Admin/ExercisesController.cs` — thêm vào class (sau `Update`):
```csharp
    // @BR-f03-thu-vien-bai-tap-015
    [HttpPost("{id:guid}/hide")]
    [ProducesResponseType<ExerciseDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Hide(Guid id, [FromServices] HideExerciseHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, cancellationToken)).ToActionResult(this);

    // @BR-f03-thu-vien-bai-tap-015
    [HttpPost("{id:guid}/unhide")]
    [ProducesResponseType<ExerciseDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Unhide(Guid id, [FromServices] UnhideExerciseHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, cancellationToken)).ToActionResult(this);

    // @BR-f03-thu-vien-bai-tap-017 @ADR-f03-thu-vien-bai-tap-004
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, [FromServices] DeleteExerciseHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : result.Error!.ToProblem(this);
    }
```

- [ ] **Step 7: Chạy Domain + integration — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Domain.Tests --filter "FullyQualifiedName~Coaching.Domain.Tests.Exercises"` và `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~Coaching.Api.IntegrationTests.Exercises"`
Expected: PASS — Domain: `ExerciseVisibilityTests` 1 + toàn bộ test Exercises trước đó, `Failed: 0`. Integration: `ExerciseHideDeleteTests` 7 (gồm `BR017_Exercise_in_use_cannot_be_deleted_only_hidden`, `ADR004_Default_usage_is_never_used`) + `ExerciseDetailApiTests` 8 + `ExerciseCrudTests` 16 + `ExerciseCategoryTests` 18 + `ExerciseSchemaTests` 1 `Passed`, `Failed: 0`.

- [ ] **Step 8: Acceptance — `ExerciseApi` ẩn/xoá qua API + bind Rule BR-015, BR-017**

`backend/tests/Coaching.Acceptance/Support/ExerciseApi.cs` — thay method `HideAsync` (SQL) bằng các method gọi API, và xoá 3 dòng using không còn dùng (`Coaching.Infrastructure.Persistence`, `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.DependencyInjection`):
```csharp
    // @BR-f03-thu-vien-bai-tap-015
    public Task<HttpResponseMessage> TryHideAsync(string name) =>
        ctx.SendJsonAsync(HttpMethod.Post, $"{Url}/{IdOf(name)}/hide", null, coach.AdminToken);

    // @BR-f03-thu-vien-bai-tap-015
    public Task<HttpResponseMessage> TryUnhideAsync(string name) =>
        ctx.SendJsonAsync(HttpMethod.Post, $"{Url}/{IdOf(name)}/unhide", null, coach.AdminToken);

    // @BR-f03-thu-vien-bai-tap-017
    public Task<HttpResponseMessage> TryDeleteAsync(string name) =>
        ctx.SendJsonAsync(HttpMethod.Delete, $"{Url}/{IdOf(name)}", null, coach.AdminToken);

    /// <summary>Đặt bài ở trạng thái đã ẩn qua API ẩn bài.</summary>
    public async Task HideAsync(string name) =>
        Assert.Equal(HttpStatusCode.OK, (await TryHideAsync(name)).StatusCode);
```

`backend/tests/Coaching.Acceptance/Steps/ExerciseVisibilitySteps.cs`:
```csharp
using System.Net;
using Coaching.Acceptance.Support;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class ExerciseVisibilitySteps(AcceptanceContext ctx, ExerciseApi exercises)
{
    private readonly Dictionary<string, Guid> _deleted = new();

    // @BR-f03-thu-vien-bai-tap-015
    [When(@"^coach ẩn bài ""([^""]*)""$")]
    public Task Hide(string name) => exercises.TryHideAsync(name);

    // @BR-f03-thu-vien-bai-tap-015
    [When(@"^coach bỏ ẩn bài ""([^""]*)""$")]
    public Task Unhide(string name) => exercises.TryUnhideAsync(name);

    // @BR-f03-thu-vien-bai-tap-015
    [Then(@"^bài ""([^""]*)"" ở trạng thái đã ẩn$")]
    public async Task IsHidden(string name)
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        Assert.True((await exercises.GetAsync(name)).IsHidden);
    }

    // @BR-f03-thu-vien-bai-tap-015
    [Then(@"^bài ""([^""]*)"" không còn ở trạng thái đã ẩn$")]
    public async Task IsVisible(string name)
    {
        Assert.Equal(HttpStatusCode.OK, ctx.LastResponse!.StatusCode);
        Assert.False((await exercises.GetAsync(name)).IsHidden);
    }

    // @BR-f03-thu-vien-bai-tap-017 @ADR-f03-thu-vien-bai-tap-004: P1 chưa có giáo án — NoExerciseUsage
    [Given(@"^thư viện có bài ""([^""]*)"" dụng cụ ""([^""]*)"" chưa có giáo án nào dùng$")]
    public Task LibraryHasUnused(string name, string equipment) => exercises.CreateOkAsync(name, "Ngực", equipment);

    // @BR-f03-thu-vien-bai-tap-017
    [When(@"^coach xoá bài ""([^""]*)""$")]
    public async Task Delete(string name)
    {
        _deleted[name] = exercises.IdOf(name);
        await exercises.TryDeleteAsync(name);
    }

    // @BR-f03-thu-vien-bai-tap-017
    [Given(@"^coach đã xoá bài ""([^""]*)""$")]
    public async Task Deleted(string name)
    {
        await Delete(name);
        Assert.Equal(HttpStatusCode.NoContent, ctx.LastResponse!.StatusCode);
    }

    // @BR-f03-thu-vien-bai-tap-017: bài đã xoá (Id lúc xoá) không còn — dụng cụ chỉ để đọc cho rõ
    [Then(@"^thư viện không còn bài ""([^""]*)"" dụng cụ ""([^""]*)""$")]
    public async Task LibraryNoLongerHas(string name, string equipment)
    {
        Assert.Equal(HttpStatusCode.NoContent, ctx.LastResponse!.StatusCode);
        using var response = await exercises.TryGetAsync(_deleted[name]);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("exercise.not_found", await StudentApi.ErrorCodeAsync(response));
    }
}
```

- [ ] **Step 9: Chạy acceptance — kỳ vọng PASS (gồm BR-001 vì "đang ẩn" nay đi qua API)**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f03-thu-vien-bai-tap-001|Category=BR-f03-thu-vien-bai-tap-015|Category=BR-f03-thu-vien-bai-tap-017)&Category!=web&Category!=manual"`
Expected: PASS — 10 scenario/example `Passed` (BR-001 ×6, BR-015 ×2, BR-017 ×2), `Failed: 0`. Scenario `@manual` "Bài đang có trong một phase không xoá được" không chạy.

- [ ] **Step 10: Verify tổng backend**

Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: Build thành công (0 warning). Mọi test `Passed` trừ scenario `@api` của BR-f03-thu-vien-bai-tap-016, 018, 019 fail `No matching step definition found` (interim rule — Task 5). Không lỗi khác.

- [ ] **Step 11: Commit**

```bash
git add backend/src/Coaching.Domain/Exercises/Exercise.cs backend/src/Coaching.Application/Exercises/IExerciseUsage.cs backend/src/Coaching.Application/Exercises/ExerciseErrors.cs backend/src/Coaching.Application/Exercises/Commands/HideExercise.cs backend/src/Coaching.Application/Exercises/Commands/DeleteExercise.cs backend/src/Coaching.Application/DependencyInjection.cs backend/src/Coaching.Api/Controllers/Admin/ExercisesController.cs backend/tests/Coaching.Domain.Tests/Exercises/ExerciseVisibilityTests.cs backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseHideDeleteTests.cs backend/tests/Coaching.Acceptance/Support/ExerciseApi.cs backend/tests/Coaching.Acceptance/Steps/ExerciseVisibilitySteps.cs
git commit -m "$(cat <<'EOF'
feat(exercises): hide, unhide and delete exercises with IExerciseUsage hook for F05 (BR-f03-thu-vien-bai-tap-015, 017)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
)"
```

---

### Task 5: Danh sách, tìm, lọc, xem bài đã ẩn + nạp thư viện ban đầu 99 bài cho coach (mọi môi trường, một lần)

> **Thay đổi yêu cầu của PO (ràng buộc, thay skeleton/tech-design cho task này):** 99 bài của prototype là thư viện THẬT của coach, không phải dữ liệu dev. Nạp ở MỌI môi trường (Development, Local, staging, Production), mỗi coach đúng một lần và chỉ khi thư viện của coach đó đang trống — không nạp lại, không ghi đè, không nhân đôi sau khi coach sửa/xoá. Vì vậy: `DevExerciseSeeder` → `ExerciseLibrarySeeder`, `exercises.dev.json` → `exercises.seed.json`, bỏ kiểm môi trường; "một lần" được ghi bằng bảng đánh dấu `ExerciseLibrarySeeds` (changeset `v0.5.0-0002`), nên coach đã xoá hết bài cũng KHÔNG được nạp lại. Host test (ApiFactory, AcceptanceContext) đặt cờ cấu hình `Seed:ExerciseLibrary=false` (mặc định `true`) để scenario BDD/integration bắt đầu với thư viện trống. Ownership vẫn là `ADR-f03-thu-vien-bai-tap-007` (coordinator cập nhật câu chữ ADR/NFR-003).

**Files:**
- Create: `backend/src/Coaching.Domain/Exercises/ExerciseFilter.cs` (`ExerciseFilter`, `ExerciseListRow`)
- Modify: `backend/src/Coaching.Domain/Exercises/IExerciseRepository.cs` (`ListAsync`)
- Create: `backend/src/Coaching.Application/Exercises/Queries/ListExercises.cs`
- Modify: `backend/src/Coaching.Application/DependencyInjection.cs`
- Modify: `backend/src/Coaching.Infrastructure/Persistence/Repositories/ExerciseRepository.cs` (`ListAsync`)
- Create: `backend/src/Coaching.Infrastructure/Seeding/ExerciseLibrarySeeder.cs`, `Seeding/ExerciseLibrarySeed.cs`, `Seeding/exercises.seed.json`, `Persistence/Configurations/ExerciseLibrarySeedConfiguration.cs`
- Modify: `backend/src/Coaching.Infrastructure/Coaching.Infrastructure.csproj` (EmbeddedResource), `Seeding/DatabaseInitializer.cs`
- Modify: `backend/src/Coaching.Api/Controllers/Admin/ExercisesController.cs` (`GET` danh sách), `backend/src/Coaching.Api/appsettings.json` (`Seed:ExerciseLibrary`)
- Create: `database/changelog/v0.5.0/0002-exercise-library-seeds.xml`, `scripts/0002-exercise-library-seeds.sql`, `rollback/0002-exercise-library-seeds.rollback.sql`; Modify: `database/changelog/v0.5.0/changelog.xml`
- Modify: `backend/tests/Coaching.Api.IntegrationTests/ApiFactory.cs`, `backend/tests/Coaching.Acceptance/Support/AcceptanceContext.cs` (`Seed:ExerciseLibrary=false`)
- Test: `backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseListTests.cs`, `Exercises/ExerciseLibrarySeedTests.cs` (mới)
- Acceptance: Modify `backend/tests/Coaching.Acceptance/Support/ExerciseApi.cs` (`ListAsync`); Create `backend/tests/Coaching.Acceptance/Steps/ExerciseListSteps.cs`
- Living docs bind: Rule `@BR-f03-thu-vien-bai-tap-016`, `@BR-f03-thu-vien-bai-tap-018` (scenario `@api` "Từ khoá không khớp bài nào"; outline `@web` bind ở Task 8), `@BR-f03-thu-vien-bai-tap-019`. KHÔNG sửa `.feature`.

**Interfaces:**
- Consumes (Task 1–4): `ExerciseNameKey.For`, `DefaultCatalog`, `CategoryKind`, `Exercise.Create/SetVideo/SetDose`, `ExerciseDose.Create`, `IExerciseCategoryRepository.ListAsync`, `AppDbContext.Exercises/ExerciseCategories/Coaches`, `ExerciseCatalogSeeder` (chạy trước), `IClock.UtcNow`, `ExerciseRequests.*`, `ExerciseApi.CreateOkAsync/HideAsync`, `ExerciseCatalogApi.MuscleGroupIdAsync/EquipmentIdAsync`.
- Produces:
  - Domain: `ExerciseFilter(string NormalizedQuery, Guid? MuscleGroupId, Guid? EquipmentId, bool Hidden)`, `ExerciseListRow(Guid Id, string Name, Guid MuscleGroupId, Guid EquipmentId, bool HasVideo, bool IsHidden)`, `IExerciseRepository.ListAsync(Guid coachId, ExerciseFilter filter, CancellationToken ct = default)` (interface nay đúng skeleton).
  - Application: `ListExercisesQuery(string? Q, Guid? MuscleGroupId, Guid? EquipmentId, bool Hidden)`, `ExerciseListItemDto(Guid Id, string Name, string MuscleGroup, string Equipment, bool HasVideo, bool IsHidden)`, `ListExercisesHandler.HandleAsync(ListExercisesQuery, ct)` → `Result<IReadOnlyList<ExerciseListItemDto>>` sắp theo tên.
  - Api: `GET /api/admin/exercises?q=&muscleGroupId=&equipmentId=&hidden=` (hidden mặc định false) → 200 `[ExerciseListItemDto]`.
  - Infrastructure: `ExerciseLibrarySeeder.EnabledKey = "Seed:ExerciseLibrary"`, `ExerciseLibrarySeeder.SeedAsync(AppDbContext db, DateTimeOffset now, CancellationToken ct = default)`; internal `Load()`, `SeedCoachAsync(...)`, `SeedExercise`; bảng `ExerciseLibrarySeeds(CoachId PK, SeededAt, ExercisesAdded)`.
  - Acceptance: `ExerciseApi.ListAsync(string? q, string? muscleGroup, string? equipment, bool hidden)`.

**Owns:** `@BR-f03-thu-vien-bai-tap-016`, `@BR-f03-thu-vien-bai-tap-018`, `@BR-f03-thu-vien-bai-tap-019` | `@ADR-f03-thu-vien-bai-tap-007`

Ghi chú bind step (đã soát `Steps/*.cs` gồm `Exercise*Steps` của Task 1–4 — không trùng; `^thư viện có bài "…"$` không khớp các step có thêm "dụng cụ …"/"đang ẩn"/"với hướng dẫn …" vì neo `$`; step liệt kê nhiều bài bắt buộc dấu `"` ngay sau "thư viện có "):
| Step trong living doc | Binding |
|---|---|
| `thư viện có bài "…"` | **mới** `ExerciseListSteps.Has` (Ngực, Machine) |
| `thư viện có bài "…" đang ẩn` / `thư viện có bài "…" đang ẩn và bài "…" đang hiện` | **mới** `HasHidden` / `HasHiddenAndVisible` |
| `thư viện có "…" (nhóm, dụng cụ), "…" (…) và "…" (…)` (2 biến thể "," / "và") | **mới** `HasMany` |
| `coach xem danh sách bài tập` / `coach xem các bài đã ẩn` | **mới** `ViewList` / `ViewHidden` |
| `coach tìm bài với từ khoá "…"` / `coach lọc nhóm cơ "…", dụng cụ "…" và tìm "…"` / `coach lọc dụng cụ "…"` | **mới** `Search` / `FilterAll` / `FilterEquipment` |
| `danh sách có "…" và không có "…"` / `danh sách có "…"` | **mới** `ListHasNot` / `ListHas` |
| `kết quả tìm rỗng` / `kết quả chỉ có "…"` / `kết quả có "…" và "…", không có "…"` | **mới** `ResultsEmpty` / `ResultsOnly` / `ResultsHaveNot` |
| `@web` outline "Tìm theo một phần tên, không dấu" (`kết quả có "…"`) | không chạy ở Reqnroll (`Category!=web`) — Task 8 |

- [ ] **Step 1: Viết integration test danh sách/tìm/lọc/bài đã ẩn**

`backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseListTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Coaching.Application.Exercises.Queries;

namespace Coaching.Api.IntegrationTests.Exercises;

[Collection(ApiCollection.Name)]
public sealed class ExerciseListTests(ApiFactory factory)
{
    private static string Tag() => Guid.NewGuid().ToString("N");

    private static async Task<List<ExerciseListItemDto>> ListAsync(HttpClient client, string query) =>
        (await client.GetFromJsonAsync<List<ExerciseListItemDto>>($"{ExerciseRequests.Url}?{query}"))!;

    private static string Q(string text) => $"q={Uri.EscapeDataString(text)}";

    // @BR-f03-thu-vien-bai-tap-016: mặc định không có bài đã ẩn; chọn xem bài đã ẩn mới thấy
    [Fact]
    public async Task BR016_Hidden_exercises_are_listed_only_when_asked()
    {
        var admin = await factory.AdminClientAsync();
        var tag = Tag();
        var visible = await ExerciseRequests.CreateAsync(admin, $"Chest Press {tag}");
        var hidden = await ExerciseRequests.CreateAsync(admin, $"Chest Dip {tag}", "Ngực", "Bodyweight");
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"{ExerciseRequests.Url}/{hidden.Id}/hide", null)).StatusCode);

        Assert.Equal(new[] { visible.Name }, (await ListAsync(admin, Q(tag))).Select(r => r.Name).ToArray());
        var onlyHidden = await ListAsync(admin, $"{Q(tag)}&hidden=true");
        Assert.Equal(new[] { hidden.Name }, onlyHidden.Select(r => r.Name).ToArray());
        Assert.True(onlyHidden[0].IsHidden);
    }

    // @BR-f03-thu-vien-bai-tap-018 @ADR-f03-thu-vien-bai-tap-002: khớp một phần, không phân biệt hoa/thường, dấu, kiểu dấu gạch
    [Fact]
    public async Task BR018_Search_matches_part_of_the_name_ignoring_case_marks_and_dashes()
    {
        var admin = await factory.AdminClientAsync();
        var tag = Tag();
        var fly = await ExerciseRequests.CreateAsync(admin, $"45° Incline Chest Fly {tag}", "Ngực", "Cable");
        var ep = await ExerciseRequests.CreateAsync(admin, $"Ép ngực {tag}", "Ngực", "Machine");
        var row = await ExerciseRequests.CreateAsync(admin, $"T–Bar Row {tag}", "Lưng", "Barbell");

        Assert.Equal(fly.Id, Assert.Single(await ListAsync(admin, Q($"INCLINE chest fly {tag}"))).Id);
        Assert.Equal(ep.Id, Assert.Single(await ListAsync(admin, Q($"ep nguc {tag}"))).Id);
        Assert.Equal(row.Id, Assert.Single(await ListAsync(admin, Q($"t-bar row {tag}"))).Id);
        Assert.Equal(3, (await ListAsync(admin, Q(tag))).Count);
        Assert.Empty(await ListAsync(admin, Q($"zzz {tag}")));
    }

    // @BR-f03-thu-vien-bai-tap-019: nhóm cơ, dụng cụ và từ khoá kết hợp — bài phải thoả mọi điều kiện đã chọn
    [Fact]
    public async Task BR019_Filters_and_keyword_combine()
    {
        var admin = await factory.AdminClientAsync();
        var tag = Tag();
        var catalog = await ExerciseRequests.CategoriesAsync(admin);
        Guid Muscle(string name) => catalog.MuscleGroups.Single(c => c.Name == name).Id;
        Guid Equipment(string name) => catalog.Equipment.Single(c => c.Name == name).Id;
        await ExerciseRequests.CreateAsync(admin, $"Dumbbell Fly {tag}", "Ngực", "Dumbbell");
        await ExerciseRequests.CreateAsync(admin, $"Cable Fly {tag}", "Ngực", "Cable");
        await ExerciseRequests.CreateAsync(admin, $"Rear Delt Fly {tag}", "Vai sau", "Dumbbell");
        await ExerciseRequests.CreateAsync(admin, $"Face Pull {tag}", "Vai sau", "Cable");

        var all = await ListAsync(admin, $"{Q($"fly {tag}")}&muscleGroupId={Muscle("Ngực")}&equipmentId={Equipment("Dumbbell")}");
        Assert.Equal(new[] { $"Dumbbell Fly {tag}" }, all.Select(r => r.Name).ToArray());

        var cable = await ListAsync(admin, $"{Q(tag)}&equipmentId={Equipment("Cable")}");
        Assert.Equal(new[] { $"Cable Fly {tag}", $"Face Pull {tag}" }, cable.Select(r => r.Name).ToArray());

        var rear = await ListAsync(admin, $"{Q(tag)}&muscleGroupId={Muscle("Vai sau")}");
        Assert.Equal(new[] { $"Face Pull {tag}", $"Rear Delt Fly {tag}" }, rear.Select(r => r.Name).ToArray());
    }

    // Danh sách sắp theo tên; mỗi dòng có tên nhóm cơ, dụng cụ, có/thiếu video
    [Fact]
    public async Task List_is_sorted_by_name_with_category_names_and_video_flag()
    {
        var admin = await factory.AdminClientAsync();
        var tag = Tag();
        await ExerciseRequests.CreateAsync(admin, $"{tag} C", "Vai", "Cable",
            c => c with { VideoUrl = "https://www.youtube.com/shorts/lMJUXEvcMkQ" });
        await ExerciseRequests.CreateAsync(admin, $"{tag} A");
        await ExerciseRequests.CreateAsync(admin, $"{tag} B");

        var rows = await ListAsync(admin, Q(tag));

        Assert.Equal(new[] { $"{tag} A", $"{tag} B", $"{tag} C" }, rows.Select(r => r.Name).ToArray());
        Assert.Equal(("Vai", "Cable", true, false), (rows[2].MuscleGroup, rows[2].Equipment, rows[2].HasVideo, rows[2].IsHidden));
        Assert.False(rows[0].HasVideo);
    }

    // NFR-f03-thu-vien-bai-tap-002: không thấy bài của coach khác; học viên không gọi được
    [Fact]
    public async Task List_contains_only_the_coachs_own_exercises()
    {
        var tag = Tag();
        var (_, other) = await ExerciseRequests.NewCoachAsync(factory);
        await ExerciseRequests.CreateAsync(other, $"Riêng {tag}");

        Assert.Empty(await ListAsync(await factory.AdminClientAsync(), Q(tag)));
        Assert.Single(await ListAsync(other, Q(tag)));
        var student = await ExerciseRequests.StudentClientAsync(factory);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync(ExerciseRequests.Url)).StatusCode);
    }
}
```

- [ ] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~ExerciseListTests"`
Expected: FAIL — build error `CS0246: The type or namespace name 'ExerciseListItemDto' could not be found`.

- [ ] **Step 3: Domain + Application + Infrastructure + Api — danh sách có lọc**

`backend/src/Coaching.Domain/Exercises/ExerciseFilter.cs`:
```csharp
namespace Coaching.Domain.Exercises;

/// <summary>
/// @BR-f03-thu-vien-bai-tap-016 @BR-f03-thu-vien-bai-tap-018 @BR-f03-thu-vien-bai-tap-019: điều kiện danh sách bài.
/// <paramref name="NormalizedQuery"/> đã qua ExerciseNameKey.For (rỗng = không lọc tên); <paramref name="Hidden"/> = true chỉ bài đã ẩn.
/// </summary>
public sealed record ExerciseFilter(string NormalizedQuery, Guid? MuscleGroupId, Guid? EquipmentId, bool Hidden);

public sealed record ExerciseListRow(Guid Id, string Name, Guid MuscleGroupId, Guid EquipmentId, bool HasVideo, bool IsHidden);
```

`backend/src/Coaching.Domain/Exercises/IExerciseRepository.cs` — thêm vào interface (sau `FindByKeyAsync`):
```csharp
    /// <summary>
    /// @BR-f03-thu-vien-bai-tap-016 @BR-f03-thu-vien-bai-tap-018 @BR-f03-thu-vien-bai-tap-019: bài của coach thoả MỌI điều kiện
    /// của <paramref name="filter"/>, sắp theo tên.
    /// </summary>
    Task<IReadOnlyList<ExerciseListRow>> ListAsync(Guid coachId, ExerciseFilter filter, CancellationToken ct = default);
```

`backend/src/Coaching.Infrastructure/Persistence/Repositories/ExerciseRepository.cs` — thêm method:
```csharp
    // @BR-f03-thu-vien-bai-tap-016 @BR-f03-thu-vien-bai-tap-018 @BR-f03-thu-vien-bai-tap-019 @ADR-f03-thu-vien-bai-tap-002:
    // 1 truy vấn theo index (CoachId, IsHidden) + LIKE '%q%' trên SearchKey (EF escape ký tự đặc biệt của LIKE) — NFR-f03-thu-vien-bai-tap-001
    public async Task<IReadOnlyList<ExerciseListRow>> ListAsync(Guid coachId, ExerciseFilter filter, CancellationToken ct = default)
    {
        var query = db.Exercises.AsNoTracking().Where(e => e.CoachId == coachId && e.IsHidden == filter.Hidden);
        if (filter.NormalizedQuery.Length > 0)
            query = query.Where(e => e.SearchKey.Contains(filter.NormalizedQuery));
        if (filter.MuscleGroupId is { } muscleGroupId)
            query = query.Where(e => e.MuscleGroupId == muscleGroupId);
        if (filter.EquipmentId is { } equipmentId)
            query = query.Where(e => e.EquipmentId == equipmentId);

        return await query
            .OrderBy(e => e.Name)
            .ThenBy(e => e.Id)
            .Select(e => new ExerciseListRow(e.Id, e.Name, e.MuscleGroupId, e.EquipmentId, e.VideoUrl != null, e.IsHidden))
            .ToListAsync(ct);
    }
```

`backend/src/Coaching.Application/Exercises/Queries/ListExercises.cs`:
```csharp
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises.Queries;

/// <summary>@BR-f03-thu-vien-bai-tap-016: <paramref name="Hidden"/> mặc định false — danh sách không gồm bài đã ẩn.</summary>
public sealed record ListExercisesQuery(string? Q, Guid? MuscleGroupId, Guid? EquipmentId, bool Hidden);

public sealed record ExerciseListItemDto(Guid Id, string Name, string MuscleGroup, string Equipment, bool HasVideo, bool IsHidden);

public sealed class ListExercisesHandler(
    CurrentCoach currentCoach,
    IExerciseRepository exercises,
    IExerciseCategoryRepository categories)
{
    public async Task<Result<IReadOnlyList<ExerciseListItemDto>>> HandleAsync(ListExercisesQuery query, CancellationToken ct = default)
    {
        if (await currentCoach.GetAsync(ct) is not { } coach)
            return StudentErrors.CoachNotFound;

        // @BR-f03-thu-vien-bai-tap-018 @ADR-f03-thu-vien-bai-tap-002: từ khoá chuẩn hoá giống SearchKey
        // @BR-f03-thu-vien-bai-tap-019: nhóm cơ + dụng cụ + từ khoá kết hợp (AND)
        var filter = new ExerciseFilter(ExerciseNameKey.For(query.Q), query.MuscleGroupId, query.EquipmentId, query.Hidden);
        var rows = await exercises.ListAsync(coach.Id, filter, ct);
        var names = (await categories.ListAsync(coach.Id, ct)).ToDictionary(c => c.Id, c => c.Name);

        IReadOnlyList<ExerciseListItemDto> items = rows
            .Select(r => new ExerciseListItemDto(r.Id, r.Name, names[r.MuscleGroupId], names[r.EquipmentId], r.HasVideo, r.IsHidden))
            .ToList();
        return Result<IReadOnlyList<ExerciseListItemDto>>.Success(items);
    }
}
```

`backend/src/Coaching.Application/DependencyInjection.cs` — đăng ký thêm (sau `DeleteExerciseHandler`):
```csharp
        services.AddScoped<ListExercisesHandler>(); // @BR-f03-thu-vien-bai-tap-016 @BR-f03-thu-vien-bai-tap-018 @BR-f03-thu-vien-bai-tap-019
```

`backend/src/Coaching.Api/Controllers/Admin/ExercisesController.cs` — thêm action đầu class:
```csharp
    // @BR-f03-thu-vien-bai-tap-016 @BR-f03-thu-vien-bai-tap-018 @BR-f03-thu-vien-bai-tap-019
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ExerciseListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] Guid? muscleGroupId, [FromQuery] Guid? equipmentId,
        [FromQuery] bool hidden, [FromServices] ListExercisesHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListExercisesQuery(q, muscleGroupId, equipmentId, hidden), cancellationToken)).ToActionResult(this);
```

- [ ] **Step 4: Chạy — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~ExerciseListTests"`
Expected: PASS — 5 test `Passed`, `Failed: 0`.

- [ ] **Step 5: Tắt nạp thư viện ban đầu ở host test + viết integration test nạp 99 bài**

`backend/tests/Coaching.Api.IntegrationTests/ApiFactory.cs` — trong `ConfigureWebHost`, sau dòng `RateLimiting:AuthPermitPerMinute`:
```csharp
        // @ADR-f03-thu-vien-bai-tap-007: host test bắt đầu với thư viện bài tập trống (test nạp 99 bài tự bật lại)
        builder.UseSetting("Seed:ExerciseLibrary", "false");
```

`backend/tests/Coaching.Acceptance/Support/AcceptanceContext.cs` — thêm vào `Settings` (sau `RateLimiting:AuthPermitPerMinute`):
```csharp
        // @ADR-f03-thu-vien-bai-tap-007: scenario BDD bắt đầu với thư viện bài tập trống
        ["Seed:ExerciseLibrary"] = "false",
```

`backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseLibrarySeedTests.cs`:
```csharp
using System.Net.Http.Json;
using Coaching.Application.Exercises;
using Coaching.Application.Exercises.Queries;
using Coaching.Domain.Exercises;
using Coaching.Infrastructure.Persistence;
using Coaching.Infrastructure.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Exercises;

[Collection(ApiCollection.Name)]
public sealed class ExerciseLibrarySeedTests(ApiFactory factory)
{
    /// <summary>Khởi động (rồi tắt) một host trên cùng DB → DatabaseInitializer chạy lại.</summary>
    private async Task StartAsync(string environment, bool seedLibrary)
    {
        await using var host = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment(environment);
            b.UseSetting("Email:Provider", "Log"); // Capture bị cấm ngoài dev/e2e (ADR-f01-p1-tai-khoan-hoc-vien-007)
            b.UseSetting(ExerciseLibrarySeeder.EnabledKey, seedLibrary ? "true" : "false");
        });
        _ = host.Services;
    }

    private async Task<int> CountAsync(Guid coachId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Exercises.CountAsync(e => e.CoachId == coachId);
    }

    private static async Task<ExerciseListItemDto> FindAsync(HttpClient client, string name) =>
        Assert.Single((await client.GetFromJsonAsync<List<ExerciseListItemDto>>(
                $"{ExerciseRequests.Url}?q={Uri.EscapeDataString(name)}"))!, r => r.Name == name);

    // @ADR-f03-thu-vien-bai-tap-007: file nhúng có đủ 99 bài, đã gán theo danh mục ban đầu (Phụ lục), không trùng (tên, dụng cụ), đều có video
    [Fact]
    public void ADR007_Seed_file_has_99_exercises_mapped_to_the_default_catalog()
    {
        var seeds = ExerciseLibrarySeeder.Load();

        Assert.Equal(99, seeds.Count);
        Assert.All(seeds, s =>
        {
            Assert.Contains(s.MuscleGroup, DefaultCatalog.MuscleGroups);
            Assert.Contains(s.Equipment, DefaultCatalog.Equipment);
            Assert.NotNull(s.VideoUrl);
        });
        Assert.Equal(99, seeds.Select(s => (ExerciseNameKey.For(s.Name), s.Equipment)).Distinct().Count());
        Assert.Equal(27, seeds.Count(s => s.Sets is not null));
    }

    // @ADR-f03-thu-vien-bai-tap-007: (a) coach thư viện trống nhận 99 bài ở host Production; (b) khởi động lại không nhân đôi;
    // (c) coach đã có ≥ 1 bài không được nạp; coach đã xoá hết bài sau khi nạp cũng không được nạp lại
    [Fact]
    public async Task ADR007_Empty_library_gets_99_exercises_once_in_every_environment()
    {
        var admin = await factory.AdminClientAsync();
        await ExerciseRequests.CreateAsync(admin, ExerciseRequests.UniqueName("Giữ thư viện admin")); // admin không trống → không bị nạp
        var (emptyCoach, emptyClient) = await ExerciseRequests.NewCoachAsync(factory, withCatalog: false);
        var (busyCoach, busyClient) = await ExerciseRequests.NewCoachAsync(factory);
        await ExerciseRequests.CreateAsync(busyClient, "Bài riêng của coach");

        await StartAsync("Production", seedLibrary: true);

        Assert.Equal(99, await CountAsync(emptyCoach)); // (a) — danh mục ban đầu được tạo trước ở cùng lần khởi động
        Assert.Equal(1, await CountAsync(busyCoach)); // (c)
        var hip = await FindAsync(emptyClient, "Hip Adduction");
        Assert.Equal(("Đùi trước", "Machine", true), (hip.MuscleGroup, hip.Equipment, hip.HasVideo));
        var crunch = await FindAsync(emptyClient, "Decline Crunch");
        Assert.Equal(("Bụng", "Bodyweight"), (crunch.MuscleGroup, crunch.Equipment));
        var incline = await ExerciseRequests.GetAsync(emptyClient, (await FindAsync(emptyClient, "45° Incline Chest Fly")).Id);
        Assert.Equal(new int?[] { 3, 10, 12, 1, 1, 90 },
            new[] { incline.Sets, incline.RepsMin, incline.RepsMax, incline.RirMin, incline.RirMax, incline.RestSeconds });
        Assert.Equal("https://www.youtube.com/shorts/PfmfAeMpym4", incline.VideoUrl);
        var facepull = await ExerciseRequests.GetAsync(emptyClient, (await FindAsync(emptyClient, "Facepull with External Rotation")).Id);
        Assert.Equal(("Vai sau", 0, 1), (facepull.MuscleGroup, facepull.RirMin!.Value, facepull.RirMax!.Value));
        Assert.Null((await ExerciseRequests.GetAsync(emptyClient, (await FindAsync(emptyClient, "Barbell Bench Press")).Id)).Sets);

        await StartAsync("Production", seedLibrary: true);
        Assert.Equal(99, await CountAsync(emptyCoach)); // (b)

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Exercises] WHERE [CoachId] = {emptyCoach}");
        }
        await StartAsync("Staging", seedLibrary: true); // không dùng Development: appsettings.Development.json bật DemoSeed
        Assert.Equal(0, await CountAsync(emptyCoach)); // đã nạp một lần → không bao giờ nạp lại
        Assert.Equal(1, await CountAsync(busyCoach));
    }

    // @ADR-f03-thu-vien-bai-tap-007: Seed:ExerciseLibrary=false (host test) → không nạp, không đánh dấu
    [Fact]
    public async Task ADR007_Seed_is_skipped_when_disabled_by_configuration()
    {
        var (coachId, _) = await ExerciseRequests.NewCoachAsync(factory);

        await StartAsync("Production", seedLibrary: false);

        Assert.Equal(0, await CountAsync(coachId));
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Set<ExerciseLibrarySeed>().AnyAsync(s => s.CoachId == coachId));
    }
}
```

- [ ] **Step 6: Chạy — kỳ vọng FAIL**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~ExerciseLibrarySeedTests"`
Expected: FAIL — build error `CS0103: The name 'ExerciseLibrarySeeder' does not exist in the current context`, `CS0246: The type or namespace name 'ExerciseLibrarySeed' could not be found`.

- [ ] **Step 7: Liquibase `v0.5.0-0002` — bảng đánh dấu đã nạp thư viện ban đầu**

`database/changelog/v0.5.0/scripts/0002-exercise-library-seeds.sql`:
```sql
-- v0.5.0-0002: đánh dấu coach đã được nạp thư viện bài tập ban đầu
-- @ADR-f03-thu-vien-bai-tap-007: mỗi coach chỉ nạp một lần — coach đã sửa/xoá bài không bao giờ bị nạp lại hay nhân đôi
CREATE TABLE [ExerciseLibrarySeeds] (
    [CoachId] uniqueidentifier NOT NULL,
    [SeededAt] datetimeoffset NOT NULL,
    [ExercisesAdded] int NOT NULL,
    CONSTRAINT [PK_ExerciseLibrarySeeds] PRIMARY KEY ([CoachId]),
    CONSTRAINT [FK_ExerciseLibrarySeeds_Coaches_CoachId] FOREIGN KEY ([CoachId]) REFERENCES [Coaches] ([Id]) ON DELETE NO ACTION
);
GO
```

`database/changelog/v0.5.0/rollback/0002-exercise-library-seeds.rollback.sql`:
```sql
-- Rollback v0.5.0-0002
DROP TABLE IF EXISTS [ExerciseLibrarySeeds];
GO
```

`database/changelog/v0.5.0/0002-exercise-library-seeds.xml`:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog http://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-4.33.xsd">
  <!-- @ADR-f03-thu-vien-bai-tap-007: thư viện ban đầu nạp một lần mỗi coach -->
  <changeSet id="v0.5.0-0002-exercise-library-seeds" author="duc.hm4@kiotviet.com">
    <comment>F03: ExerciseLibrarySeeds — đánh dấu coach đã được nạp 99 bài ban đầu</comment>
    <sqlFile path="scripts/0002-exercise-library-seeds.sql" relativeToChangelogFile="true" splitStatements="true" endDelimiter="\nGO" stripComments="false"/>
    <rollback>
      <sqlFile path="rollback/0002-exercise-library-seeds.rollback.sql" relativeToChangelogFile="true" splitStatements="true" endDelimiter="\nGO"/>
    </rollback>
  </changeSet>
</databaseChangeLog>
```

`database/changelog/v0.5.0/changelog.xml` — include TRƯỚC changeset tag:
```xml
  <include file="0001-exercise-library.xml" relativeToChangelogFile="true"/>
  <include file="0002-exercise-library-seeds.xml" relativeToChangelogFile="true"/>
```

- [ ] **Step 8: Infrastructure — `ExerciseLibrarySeed`, `ExerciseLibrarySeeder`, file `exercises.seed.json` nhúng, initializer, cấu hình**

`backend/src/Coaching.Infrastructure/Seeding/ExerciseLibrarySeed.cs`:
```csharp
namespace Coaching.Infrastructure.Seeding;

/// <summary>@ADR-f03-thu-vien-bai-tap-007: coach đã được xét nạp thư viện ban đầu (ExercisesAdded = 0 khi thư viện đã có bài).</summary>
internal sealed class ExerciseLibrarySeed(Guid coachId, DateTimeOffset seededAt, int exercisesAdded)
{
    public Guid CoachId { get; private set; } = coachId;

    public DateTimeOffset SeededAt { get; private set; } = seededAt;

    public int ExercisesAdded { get; private set; } = exercisesAdded;
}
```

`backend/src/Coaching.Infrastructure/Persistence/Configurations/ExerciseLibrarySeedConfiguration.cs`:
```csharp
using Coaching.Domain.Coaches;
using Coaching.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Persistence.Configurations;

/// <summary>@ADR-f03-thu-vien-bai-tap-007: bảng ExerciseLibrarySeeds (Liquibase v0.5.0-0002).</summary>
internal sealed class ExerciseLibrarySeedConfiguration : IEntityTypeConfiguration<ExerciseLibrarySeed>
{
    public void Configure(EntityTypeBuilder<ExerciseLibrarySeed> builder)
    {
        builder.ToTable("ExerciseLibrarySeeds");
        builder.HasKey(s => s.CoachId);
        builder.Property(s => s.CoachId).ValueGeneratedNever();
        builder.HasOne<Coach>().WithOne().HasForeignKey<ExerciseLibrarySeed>(s => s.CoachId).OnDelete(DeleteBehavior.Restrict);
    }
}
```

`backend/src/Coaching.Infrastructure/Seeding/ExerciseLibrarySeeder.cs`:
```csharp
using System.Text.Json;
using Coaching.Domain.Exercises;
using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coaching.Infrastructure.Seeding;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-007: thư viện bài tập ban đầu của coach — 99 bài prototype đã gán theo danh mục ban đầu (Phụ lục
/// tech-design). Chạy lúc khởi động ở MỌI môi trường; mỗi coach được xét đúng một lần (bảng ExerciseLibrarySeeds): thư viện trống
/// → nạp 99 bài; đã có bài → chỉ đánh dấu. Không bao giờ nạp lại, ghi đè hay nhân đôi. Host test tắt bằng Seed:ExerciseLibrary=false.
/// Chạy SAU ExerciseCatalogSeeder (cần danh mục ban đầu).
/// </summary>
public static class ExerciseLibrarySeeder
{
    public const string EnabledKey = "Seed:ExerciseLibrary";
    internal const string ResourceName = "Coaching.Infrastructure.Seeding.exercises.seed.json";

    public static async Task SeedAsync(AppDbContext db, DateTimeOffset now, CancellationToken ct = default)
    {
        var coachIds = await db.Coaches
            .Where(coach => !db.Set<ExerciseLibrarySeed>().Any(s => s.CoachId == coach.Id))
            .Select(coach => coach.Id)
            .ToListAsync(ct);
        if (coachIds.Count == 0)
            return;

        var seeds = Load();
        foreach (var coachId in coachIds)
            await SeedCoachAsync(db, coachId, seeds, now, ct);
    }

    internal static async Task<int> SeedCoachAsync(AppDbContext db, Guid coachId, IReadOnlyList<SeedExercise> seeds,
        DateTimeOffset now, CancellationToken ct)
    {
        var added = 0;
        if (!await db.Exercises.AnyAsync(e => e.CoachId == coachId, ct))
        {
            var categories = await db.ExerciseCategories.AsNoTracking().Where(c => c.CoachId == coachId).ToListAsync(ct);
            var muscleGroups = categories.Where(c => c.Kind == CategoryKind.MuscleGroup).ToDictionary(c => c.Name, c => c.Id);
            var equipment = categories.Where(c => c.Kind == CategoryKind.Equipment).ToDictionary(c => c.Name, c => c.Id);
            foreach (var seed in seeds)
            {
                // cùng luật với coach nhập tay (BR-f03-thu-vien-bai-tap-002, 007, 009…012)
                var exercise = Exercise.Create(coachId, seed.Name, muscleGroups[seed.MuscleGroup], equipment[seed.Equipment]);
                exercise.SetVideo(seed.VideoUrl);
                exercise.SetDose(ExerciseDose.Create(seed.Sets, seed.RepsMin, seed.RepsMax, seed.RirMin, seed.RirMax, seed.RestSeconds));
                db.Exercises.Add(exercise);
            }
            added = seeds.Count;
        }

        db.Set<ExerciseLibrarySeed>().Add(new ExerciseLibrarySeed(coachId, now, added));
        await db.SaveChangesAsync(ct);
        return added;
    }

    internal static IReadOnlyList<SeedExercise> Load()
    {
        using var stream = typeof(ExerciseLibrarySeeder).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Thiếu resource nhúng {ResourceName}.");
        return JsonSerializer.Deserialize<List<SeedExercise>>(stream, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException($"{ResourceName} rỗng.");
    }

    /// <summary>Một dòng của exercises.seed.json: liều "3|10–12|1|90s" đã tách thành số; "—" là null.</summary>
    internal sealed record SeedExercise(
        string Name, string MuscleGroup, string Equipment, string? VideoUrl,
        int? Sets, int? RepsMin, int? RepsMax, int? RirMin, int? RirMax, int? RestSeconds);
}
```

`backend/src/Coaching.Infrastructure/Coaching.Infrastructure.csproj` — thêm ItemGroup:
```xml
  <ItemGroup>
    <!-- @ADR-f03-thu-vien-bai-tap-007: thư viện bài tập ban đầu, nạp ở mọi môi trường -->
    <EmbeddedResource Include="Seeding\exercises.seed.json" LogicalName="Coaching.Infrastructure.Seeding.exercises.seed.json" />
  </ItemGroup>
```

`backend/src/Coaching.Infrastructure/Seeding/DatabaseInitializer.cs` — thêm `using Microsoft.Extensions.Configuration;` và ngay sau dòng gọi `ExerciseCatalogSeeder.SeedAsync`:
```csharp
        // @ADR-f03-thu-vien-bai-tap-007: thư viện ban đầu 99 bài — mọi môi trường, một lần mỗi coach (sau danh mục ban đầu);
        // host test đặt Seed:ExerciseLibrary=false để bắt đầu với thư viện trống
        if (sp.GetRequiredService<IConfiguration>().GetValue(ExerciseLibrarySeeder.EnabledKey, true))
            await ExerciseLibrarySeeder.SeedAsync(db, clock.UtcNow, cancellationToken);
```

`backend/src/Coaching.Api/appsettings.json` — thêm mục (sau `"Database"`):
```json
  "Seed": {
    "ExerciseLibrary": true
  },
```

`backend/src/Coaching.Infrastructure/Seeding/exercises.seed.json` (99 bài — đã áp Phụ lục: "Tay" → Tay trước/Tay sau, "Đùi" → Đùi trước/Đùi sau, "Bắp chuối" → Bắp chân, "Vai" (4 bài) → Vai sau, "Weighted" → Bodyweight; liều "3|10–12|1|90s" → `sets 3, repsMin 10, repsMax 12, rirMin 1, rirMax 1, restSeconds 90`, "—" → `null`):
```json
[
  {"name": "45° Incline Chest Fly", "muscleGroup": "Ngực", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/PfmfAeMpym4", "sets": 3, "repsMin": 10, "repsMax": 12, "rirMin": 1, "rirMax": 1, "restSeconds": 90},
  {"name": "Barbell Bench Press", "muscleGroup": "Ngực", "equipment": "Barbell", "videoUrl": "https://www.youtube.com/watch?v=vcBig73ojpE", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Cable Crossover (High-to-Low)", "muscleGroup": "Ngực", "equipment": "Cable", "videoUrl": "https://www.youtube.com/watch?v=JUDTGZh4rhg", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Chest Dip", "muscleGroup": "Ngực", "equipment": "Bodyweight", "videoUrl": "https://www.youtube.com/watch?v=yN6Q1UI_xkE", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Chest Press", "muscleGroup": "Ngực", "equipment": "Machine", "videoUrl": "https://www.youtube.com/shorts/2awX3rTGa1k", "sets": 3, "repsMin": 8, "repsMax": 10, "rirMin": 1, "rirMax": 2, "restSeconds": 120},
  {"name": "Dumbbell Fly", "muscleGroup": "Ngực", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=Nhvz9EzdJ4U", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Incline Chest Press", "muscleGroup": "Ngực", "equipment": "Smith Machine", "videoUrl": "https://www.youtube.com/shorts/ohRa_YRmVCk", "sets": 3, "repsMin": 6, "repsMax": 8, "rirMin": 1, "rirMax": 2, "restSeconds": 150},
  {"name": "Incline Dumbbell Press", "muscleGroup": "Ngực", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=awEEyL5zGvU", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Incline Machine Chest Press", "muscleGroup": "Ngực", "equipment": "Machine", "videoUrl": "https://www.youtube.com/watch?v=LiDArz1R2NU", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Pec Deck", "muscleGroup": "Ngực", "equipment": "Machine", "videoUrl": "https://www.youtube.com/shorts/fgXSA2-o0NM", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Push Up", "muscleGroup": "Ngực", "equipment": "Bodyweight", "videoUrl": "https://www.youtube.com/shorts/pKZ-lkKKMws", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "75° Shoulder Press", "muscleGroup": "Vai", "equipment": "Smith Machine", "videoUrl": "https://www.youtube.com/watch?v=evtRb6cPF-Q", "sets": 3, "repsMin": 6, "repsMax": 8, "rirMin": 1, "rirMax": 2, "restSeconds": 150},
  {"name": "Arnold Press", "muscleGroup": "Vai", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=ris9tKqMwgU", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Barbell Overhead Press", "muscleGroup": "Vai", "equipment": "Barbell", "videoUrl": "https://www.youtube.com/watch?v=bMksDb5a3P0", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Behind the body Lateral Raise", "muscleGroup": "Vai", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/YfI31W4n7mY", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Butterfly Raise", "muscleGroup": "Vai sau", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/IkhOnVLxD5Q", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Cable Upright Row", "muscleGroup": "Vai", "equipment": "Cable", "videoUrl": "https://www.youtube.com/watch?v=GXkY1ZlI7cs", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Dumbbell Lateral Raise", "muscleGroup": "Vai", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=XPPfnSEATJA", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Facepull with External Rotation", "muscleGroup": "Vai sau", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/oWncQ0QzVBY", "sets": 3, "repsMin": 15, "repsMax": 20, "rirMin": 0, "rirMax": 1, "restSeconds": 60},
  {"name": "Lateral Raise", "muscleGroup": "Vai", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/lMJUXEvcMkQ", "sets": 3, "repsMin": 12, "repsMax": 15, "rirMin": 0, "rirMax": 1, "restSeconds": 60},
  {"name": "Machine Lateral Raise", "muscleGroup": "Vai", "equipment": "Machine", "videoUrl": "https://www.youtube.com/watch?v=IropE3iOk2c", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Parallel Shoulder Press", "muscleGroup": "Vai", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/ERgUy2_D_aM", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Reardelt Fly", "muscleGroup": "Vai sau", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/FeERX9UwspY", "sets": 3, "repsMin": 12, "repsMax": 15, "rirMin": 0, "rirMax": 1, "restSeconds": 60},
  {"name": "Reverse Pec Deck", "muscleGroup": "Vai sau", "equipment": "Machine", "videoUrl": "https://www.youtube.com/watch?v=5OI1CFf1oc8", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Shoulder Press", "muscleGroup": "Vai", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/shorts/k6tzKisR3NY", "sets": 3, "repsMin": 8, "repsMax": 10, "rirMin": 1, "rirMax": 2, "restSeconds": 120},
  {"name": "Y-Raise", "muscleGroup": "Vai", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/shorts/M5DeSgxNyKQ", "sets": 3, "repsMin": 12, "repsMax": 15, "rirMin": 0, "rirMax": 1, "restSeconds": 60},
  {"name": "Barbell Bent-over Row", "muscleGroup": "Lưng", "equipment": "Barbell", "videoUrl": "https://www.youtube.com/watch?v=qXrTDQG1oUQ", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Chest-supported Row", "muscleGroup": "Lưng", "equipment": "Machine", "videoUrl": "https://www.youtube.com/shorts/FVyVvApcyZY", "sets": 3, "repsMin": 8, "repsMax": 10, "rirMin": 1, "rirMax": 2, "restSeconds": 120},
  {"name": "Dumbbell Shrug", "muscleGroup": "Lưng", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=zrs4FLeF_-4", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Iliac Pulldown", "muscleGroup": "Lưng", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/uZbBCEOPkgY", "sets": 3, "repsMin": 10, "repsMax": 12, "rirMin": 1, "rirMax": 1, "restSeconds": 90},
  {"name": "Keenan Flap", "muscleGroup": "Lưng", "equipment": "Cable", "videoUrl": "https://www.youtube.com/watch?v=11MaNZ8acE4", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Lat Pulldown - Close-grip", "muscleGroup": "Lưng", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/K2fvbRmi1xI", "sets": 3, "repsMin": 8, "repsMax": 10, "rirMin": 1, "rirMax": 2, "restSeconds": 120},
  {"name": "Meadows Row", "muscleGroup": "Lưng", "equipment": "Barbell", "videoUrl": "https://www.youtube.com/watch?v=G-jU1aPVhnY", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "One-arm Row", "muscleGroup": "Lưng", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/shorts/jpi4reqwiKY", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "One-arm Row (Cable)", "muscleGroup": "Lưng", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/9TWiV80cUYs", "sets": 3, "repsMin": 10, "repsMax": 12, "rirMin": 1, "rirMax": 1, "restSeconds": 90},
  {"name": "Pendlay Row", "muscleGroup": "Lưng", "equipment": "Barbell", "videoUrl": "https://www.youtube.com/watch?v=axoeDmW0oAY", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Pull Over", "muscleGroup": "Lưng", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/shorts/Datv2L6t3-4", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Pull Up", "muscleGroup": "Lưng", "equipment": "Bodyweight", "videoUrl": "https://www.youtube.com/watch?v=ui5rcJx-_hY", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Rack Pull", "muscleGroup": "Lưng", "equipment": "Barbell", "videoUrl": "https://www.youtube.com/watch?v=4nm5ObSUtt0", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Seal Row", "muscleGroup": "Lưng", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=49Hbk_5fhoI", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Seated Cable Row - Neutral grip", "muscleGroup": "Lưng", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/ZyZ4QEBqrcQ", "sets": 3, "repsMin": 10, "repsMax": 12, "rirMin": 1, "rirMax": 1, "restSeconds": 90},
  {"name": "Single-arm Lat Pulldown", "muscleGroup": "Lưng", "equipment": "Cable", "videoUrl": "https://www.youtube.com/watch?v=fHQ5Yrc5Sds", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Straight-arm Pulldown", "muscleGroup": "Lưng", "equipment": "Cable", "videoUrl": "https://www.youtube.com/watch?v=pp_n3XTFEOQ", "sets": 2, "repsMin": 12, "repsMax": 15, "rirMin": 1, "rirMax": 1, "restSeconds": 60},
  {"name": "T-Bar Row", "muscleGroup": "Lưng", "equipment": "Machine", "videoUrl": "https://www.youtube.com/watch?v=yPis7nlbqdY", "sets": 3, "repsMin": 8, "repsMax": 10, "rirMin": 1, "rirMax": 2, "restSeconds": 120},
  {"name": "15° Incline Curl", "muscleGroup": "Tay trước", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/shorts/fXFN8_1Bh6k", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Barbell Curl", "muscleGroup": "Tay trước", "equipment": "Barbell", "videoUrl": "https://www.youtube.com/watch?v=bV54Vl--I50", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Bayesian Curl", "muscleGroup": "Tay trước", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/BaSd7C58L3o", "sets": 3, "repsMin": 10, "repsMax": 12, "rirMin": 1, "rirMax": 1, "restSeconds": 60},
  {"name": "Cable Curl (Straight Bar)", "muscleGroup": "Tay trước", "equipment": "Cable", "videoUrl": "https://www.youtube.com/watch?v=d7friQusjF8", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Close-grip Bench Press", "muscleGroup": "Tay sau", "equipment": "Barbell", "videoUrl": "https://www.youtube.com/watch?v=q88BGHUlOaU", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "EZ Bar Skull Crusher", "muscleGroup": "Tay sau", "equipment": "Barbell", "videoUrl": "https://www.youtube.com/watch?v=xB5ssQ5UT-o", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Hammer Curl", "muscleGroup": "Tay trước", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=3nqezx8BEv0", "sets": 2, "repsMin": 10, "repsMax": 12, "rirMin": 0, "rirMax": 1, "restSeconds": 60},
  {"name": "JM Press", "muscleGroup": "Tay sau", "equipment": "Smith Machine", "videoUrl": "https://www.youtube.com/watch?v=sQJzzZVqV3k", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "One-arm Triceps Extension", "muscleGroup": "Tay sau", "equipment": "Cable", "videoUrl": "https://www.youtube.com/watch?v=8rl4ioij6lc", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Overhead Triceps Extension", "muscleGroup": "Tay sau", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/x14KzQCXD3A", "sets": 2, "repsMin": 12, "repsMax": 15, "rirMin": 0, "rirMax": 1, "restSeconds": 60},
  {"name": "Preacher Curl", "muscleGroup": "Tay trước", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=BPmUhDtdQfw", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Recline Curl", "muscleGroup": "Tay trước", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=-YcyYvmXZeM", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Seated Dumbbell Overhead Triceps Extension", "muscleGroup": "Tay sau", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=Ml9QzVI-pBQ", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Spider Curl", "muscleGroup": "Tay trước", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=z1uuyKW5PRk", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Triceps Pushdown", "muscleGroup": "Tay sau", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/BQ3VcGleQI0", "sets": 3, "repsMin": 10, "repsMax": 12, "rirMin": 1, "rirMax": 1, "restSeconds": 60},
  {"name": "Barbell Back Squat", "muscleGroup": "Đùi trước", "equipment": "Barbell", "videoUrl": "https://www.youtube.com/watch?v=QLbMsabhBqY", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Bulgarian Split Squat", "muscleGroup": "Đùi trước", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=hiLF_pF3EJM", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Conventional Deadlift", "muscleGroup": "Đùi sau", "equipment": "Barbell", "videoUrl": "https://www.youtube.com/watch?v=VL5Ab0T07e4", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Dumbbell Walking Lunge", "muscleGroup": "Đùi trước", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=I34ysEkPK7w", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Front Squat", "muscleGroup": "Đùi trước", "equipment": "Barbell", "videoUrl": "https://www.youtube.com/watch?v=v-mQm_droHg", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Goblet Squat", "muscleGroup": "Đùi trước", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=xIU3-8WqasQ", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Hack Squat", "muscleGroup": "Đùi trước", "equipment": "Machine", "videoUrl": "https://www.youtube.com/watch?v=hglQExHCM9Q", "sets": 3, "repsMin": 6, "repsMax": 8, "rirMin": 1, "rirMax": 2, "restSeconds": 180},
  {"name": "Hip Adduction", "muscleGroup": "Đùi trước", "equipment": "Machine", "videoUrl": "https://www.youtube.com/shorts/BmMmt-c9aNM", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Leg Curl", "muscleGroup": "Đùi sau", "equipment": "Machine", "videoUrl": "https://www.youtube.com/shorts/qYuwgGnPBZw", "sets": 3, "repsMin": 10, "repsMax": 12, "rirMin": 1, "rirMax": 1, "restSeconds": 90},
  {"name": "Leg Extension", "muscleGroup": "Đùi trước", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/uM86QE59Tgc", "sets": 3, "repsMin": 12, "repsMax": 15, "rirMin": 0, "rirMax": 1, "restSeconds": 60},
  {"name": "Leg Press", "muscleGroup": "Đùi trước", "equipment": "Machine", "videoUrl": "https://www.youtube.com/watch?v=cDGOn-yfKJA", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Nordic Hamstring Curl", "muscleGroup": "Đùi sau", "equipment": "Bodyweight", "videoUrl": "https://www.youtube.com/watch?v=_e9vFU9-tkc", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Pendulum Squat", "muscleGroup": "Đùi trước", "equipment": "Machine", "videoUrl": "https://www.youtube.com/watch?v=cLMpfZX8bQ0", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Romanian Deadlift", "muscleGroup": "Đùi sau", "equipment": "Smith Machine", "videoUrl": "https://www.youtube.com/shorts/SO3dakJKNss", "sets": 3, "repsMin": 8, "repsMax": 10, "rirMin": 2, "rirMax": 2, "restSeconds": 150},
  {"name": "Seated Leg Curl", "muscleGroup": "Đùi sau", "equipment": "Machine", "videoUrl": "https://www.youtube.com/watch?v=G5iP_YcDQdE", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Static Lunges - Front Feet-elevated", "muscleGroup": "Đùi trước", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/shorts/TQAcW-hiLss", "sets": 2, "repsMin": 8, "repsMax": 10, "rirMin": 1, "rirMax": 2, "restSeconds": 120},
  {"name": "Barbell Hip Thrust", "muscleGroup": "Mông", "equipment": "Barbell", "videoUrl": "https://www.youtube.com/watch?v=Zp26q4BY5HE", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Cable Glute Kickback", "muscleGroup": "Mông", "equipment": "Cable", "videoUrl": "https://www.youtube.com/watch?v=5jJNfIlKTmg", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Cable Pull Through", "muscleGroup": "Mông", "equipment": "Cable", "videoUrl": "https://www.youtube.com/watch?v=yXopOhzEoeo", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Dumbbell Step Up", "muscleGroup": "Mông", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=X_oNQX4srHc", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Glute Bridge", "muscleGroup": "Mông", "equipment": "Barbell", "videoUrl": "https://www.youtube.com/watch?v=bcnun1NjZ1E", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Hip Abduction", "muscleGroup": "Mông", "equipment": "Machine", "videoUrl": "https://www.youtube.com/watch?v=cja4D-SoPjE", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Hip Thrust", "muscleGroup": "Mông", "equipment": "Machine", "videoUrl": "https://www.youtube.com/shorts/sxOrVuCvWq4", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Hyperextension (45°)", "muscleGroup": "Mông", "equipment": "Bodyweight", "videoUrl": "https://www.youtube.com/watch?v=d6151fdHDUY", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Ab Wheel Rollout", "muscleGroup": "Bụng", "equipment": "Bodyweight", "videoUrl": "https://www.youtube.com/watch?v=OJIWMlLa38Q", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Cable Crunch", "muscleGroup": "Bụng", "equipment": "Cable", "videoUrl": "https://www.youtube.com/shorts/dkGwcfo9zto", "sets": 3, "repsMin": 12, "repsMax": 15, "rirMin": 1, "rirMax": 1, "restSeconds": 60},
  {"name": "Dead Bug", "muscleGroup": "Bụng", "equipment": "Bodyweight", "videoUrl": "https://www.youtube.com/watch?v=-gRN_JyVTLY", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Decline Crunch", "muscleGroup": "Bụng", "equipment": "Bodyweight", "videoUrl": "https://www.youtube.com/shorts/xfNAzPwNRqw", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Hanging Leg Raise", "muscleGroup": "Bụng", "equipment": "Bodyweight", "videoUrl": "https://www.youtube.com/watch?v=JXztA3fLp50", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Pallof Press", "muscleGroup": "Bụng", "equipment": "Cable", "videoUrl": "https://www.youtube.com/watch?v=ehOUKeo_BDk", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Plank", "muscleGroup": "Bụng", "equipment": "Bodyweight", "videoUrl": "https://www.youtube.com/watch?v=6LqqeBtFn9M", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Reverse Crunch", "muscleGroup": "Bụng", "equipment": "Bodyweight", "videoUrl": "https://www.youtube.com/watch?v=U01aKpf68T8", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Russian Twist", "muscleGroup": "Bụng", "equipment": "Bodyweight", "videoUrl": "https://www.youtube.com/watch?v=jjyR6Z1U4dA", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Calves Raise", "muscleGroup": "Bắp chân", "equipment": "Smith Machine", "videoUrl": "https://www.youtube.com/shorts/wlqTemUXPXY", "sets": 3, "repsMin": 12, "repsMax": 15, "rirMin": 0, "rirMax": 1, "restSeconds": 60},
  {"name": "Donkey Calf Raise", "muscleGroup": "Bắp chân", "equipment": "Machine", "videoUrl": "https://www.youtube.com/watch?v=EQyCroWhIPs", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Leg Press Calf Raise", "muscleGroup": "Bắp chân", "equipment": "Machine", "videoUrl": "https://www.youtube.com/watch?v=sluiSitVePs", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Seated Calf Raise", "muscleGroup": "Bắp chân", "equipment": "Machine", "videoUrl": "https://www.youtube.com/watch?v=smyXy_dhXo8", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Single-leg Dumbbell Calf Raise", "muscleGroup": "Bắp chân", "equipment": "Dumbbell", "videoUrl": "https://www.youtube.com/watch?v=KcTEGifMTDY", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Standing Calf Raise", "muscleGroup": "Bắp chân", "equipment": "Machine", "videoUrl": "https://www.youtube.com/watch?v=SVtg-1loH4c", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null},
  {"name": "Tibialis Raise", "muscleGroup": "Bắp chân", "equipment": "Bodyweight", "videoUrl": "https://www.youtube.com/watch?v=S-8UOBHYc9w", "sets": null, "repsMin": null, "repsMax": null, "rirMin": null, "rirMax": null, "restSeconds": null}
]
```

- [ ] **Step 9: Chạy integration test nạp thư viện — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Api.IntegrationTests --filter "FullyQualifiedName~ExerciseLibrarySeedTests|FullyQualifiedName~ExerciseSchemaTests"`
Expected: PASS — `ADR007_Seed_file_has_99_exercises_mapped_to_the_default_catalog`, `ADR007_Empty_library_gets_99_exercises_once_in_every_environment`, `ADR007_Seed_is_skipped_when_disabled_by_configuration`, `Changelog_v0_5_0_creates_exercise_library_tables` `Passed`, `Failed: 0`. (Lỗi `KeyNotFoundException` ở `SeedCoachAsync` = tên nhóm cơ/dụng cụ trong JSON lệch `DefaultCatalog` — sửa JSON.)

- [ ] **Step 10: Acceptance — `ExerciseApi.ListAsync` + bind Rule BR-016, 018, 019**

`backend/tests/Coaching.Acceptance/Support/ExerciseApi.cs` — thêm `using Coaching.Application.Exercises.Queries;` và method:
```csharp
    // @BR-f03-thu-vien-bai-tap-016 @BR-f03-thu-vien-bai-tap-018 @BR-f03-thu-vien-bai-tap-019
    public async Task<IReadOnlyList<ExerciseListItemDto>> ListAsync(string? q = null, string? muscleGroup = null,
        string? equipment = null, bool hidden = false)
    {
        var query = new List<string>();
        if (q is not null)
            query.Add($"q={Uri.EscapeDataString(q)}");
        if (muscleGroup is not null)
            query.Add($"muscleGroupId={await catalog.MuscleGroupIdAsync(muscleGroup)}");
        if (equipment is not null)
            query.Add($"equipmentId={await catalog.EquipmentIdAsync(equipment)}");
        if (hidden)
            query.Add("hidden=true");

        var response = await ctx.GetAsync(query.Count == 0 ? Url : $"{Url}?{string.Join('&', query)}", coach.AdminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<ExerciseListItemDto>>())!;
    }
```

`backend/tests/Coaching.Acceptance/Steps/ExerciseListSteps.cs`:
```csharp
using System.Text.RegularExpressions;
using Coaching.Acceptance.Support;
using Coaching.Application.Exercises.Queries;
using Reqnroll;

namespace Coaching.Acceptance.Steps;

[Binding]
public sealed class ExerciseListSteps(ExerciseApi exercises)
{
    private IReadOnlyList<ExerciseListItemDto> _results = [];

    private string[] Names => _results.Select(r => r.Name).ToArray();

    // @BR-f03-thu-vien-bai-tap-018
    [Given(@"^thư viện có bài ""([^""]*)""$")]
    public Task Has(string name) => exercises.CreateOkAsync(name, "Ngực", "Machine");

    // @BR-f03-thu-vien-bai-tap-016
    [Given(@"^thư viện có bài ""([^""]*)"" đang ẩn$")]
    public async Task HasHidden(string name)
    {
        await exercises.CreateOkAsync(name, "Ngực", "Bodyweight");
        await exercises.HideAsync(name);
    }

    // @BR-f03-thu-vien-bai-tap-016
    [Given(@"^thư viện có bài ""([^""]*)"" đang ẩn và bài ""([^""]*)"" đang hiện$")]
    public async Task HasHiddenAndVisible(string hidden, string visible)
    {
        await HasHidden(hidden);
        await exercises.CreateOkAsync(visible, "Ngực", "Machine");
    }

    // @BR-f03-thu-vien-bai-tap-019: "Tên" (Nhóm cơ, Dụng cụ), … và "Tên" (…)
    [Given(@"^thư viện có ((?:""[^""]+"" \([^)]+\)(?:, | và )?)+)$")]
    public async Task HasMany(string list)
    {
        foreach (Match m in Regex.Matches(list, "\"([^\"]+)\" \\(([^,]+), ([^)]+)\\)"))
            await exercises.CreateOkAsync(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value);
    }

    // @BR-f03-thu-vien-bai-tap-016
    [When(@"^coach xem danh sách bài tập$")]
    public async Task ViewList() => _results = await exercises.ListAsync();

    // @BR-f03-thu-vien-bai-tap-016
    [When(@"^coach xem các bài đã ẩn$")]
    public async Task ViewHidden() => _results = await exercises.ListAsync(hidden: true);

    // @BR-f03-thu-vien-bai-tap-018
    [When(@"^coach tìm bài với từ khoá ""([^""]*)""$")]
    public async Task Search(string keyword) => _results = await exercises.ListAsync(q: keyword);

    // @BR-f03-thu-vien-bai-tap-019
    [When(@"^coach lọc nhóm cơ ""([^""]*)"", dụng cụ ""([^""]*)"" và tìm ""([^""]*)""$")]
    public async Task FilterAll(string muscleGroup, string equipment, string keyword) =>
        _results = await exercises.ListAsync(keyword, muscleGroup, equipment);

    // @BR-f03-thu-vien-bai-tap-019
    [When(@"^coach lọc dụng cụ ""([^""]*)""$")]
    public async Task FilterEquipment(string equipment) => _results = await exercises.ListAsync(equipment: equipment);

    // @BR-f03-thu-vien-bai-tap-016
    [Then(@"^danh sách có ""([^""]*)"" và không có ""([^""]*)""$")]
    public void ListHasNot(string present, string absent)
    {
        Assert.Contains(present, Names);
        Assert.DoesNotContain(absent, Names);
    }

    // @BR-f03-thu-vien-bai-tap-016
    [Then(@"^danh sách có ""([^""]*)""$")]
    public void ListHas(string name) => Assert.Contains(_results, r => r.Name == name && r.IsHidden);

    // @BR-f03-thu-vien-bai-tap-018
    [Then(@"^kết quả tìm rỗng$")]
    public void ResultsEmpty() => Assert.Empty(_results);

    // @BR-f03-thu-vien-bai-tap-019
    [Then(@"^kết quả chỉ có ""([^""]*)""$")]
    public void ResultsOnly(string name) => Assert.Equal(new[] { name }, Names);

    // @BR-f03-thu-vien-bai-tap-019
    [Then(@"^kết quả có ""([^""]*)"" và ""([^""]*)"", không có ""([^""]*)""$")]
    public void ResultsHaveNot(string first, string second, string absent)
    {
        Assert.Contains(first, Names);
        Assert.Contains(second, Names);
        Assert.DoesNotContain(absent, Names);
    }
}
```

- [ ] **Step 11: Chạy acceptance Rule BR-016, 018, 019 — kỳ vọng PASS**

Run: `cd backend && dotnet test tests/Coaching.Acceptance --filter "(Category=BR-f03-thu-vien-bai-tap-016|Category=BR-f03-thu-vien-bai-tap-018|Category=BR-f03-thu-vien-bai-tap-019)&Category!=web&Category!=manual"`
Expected: PASS — 5 scenario `Passed` (BR-016 ×2, BR-018 ×1 "Từ khoá không khớp bài nào", BR-019 ×2), `Failed: 0`. Thư viện mỗi scenario bắt đầu trống (`Seed:ExerciseLibrary=false`) nên "kết quả chỉ có "Dumbbell Fly"" không lẫn bài nạp sẵn.

- [ ] **Step 12: Verify tổng backend — toàn bộ living doc F03 `@api` xanh**

Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: Build thành công (0 warning, TreatWarningsAsErrors). MỌI test `Passed`, `Failed: 0` — gồm 56 scenario/example `@api` của `features/thu-vien/bai-tap/thu-vien-bai-tap.feature` (BR-001…021, không còn `No matching step definition found`), Domain `Coaching.Domain.Tests.Exercises.*`, integration `Coaching.Api.IntegrationTests.Exercises.*` và toàn bộ test F00/F01-P1/F02.

Run (thử nhanh local, tuỳ chọn): `docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build --wait` rồi đăng nhập admin, `GET http://localhost:8080/api/admin/exercises` (Bearer token admin)
Expected: 99 bài (khi DB local mới; thư viện admin trống lần khởi động đầu). Khởi động lại container API → vẫn 99.

- [ ] **Step 13: Commit**

```bash
git add backend/src/Coaching.Domain/Exercises/ExerciseFilter.cs backend/src/Coaching.Domain/Exercises/IExerciseRepository.cs backend/src/Coaching.Application/Exercises/Queries/ListExercises.cs backend/src/Coaching.Application/DependencyInjection.cs backend/src/Coaching.Infrastructure/Persistence/Repositories/ExerciseRepository.cs backend/src/Coaching.Infrastructure/Seeding/ExerciseLibrarySeeder.cs backend/src/Coaching.Infrastructure/Seeding/ExerciseLibrarySeed.cs backend/src/Coaching.Infrastructure/Seeding/exercises.seed.json backend/src/Coaching.Infrastructure/Persistence/Configurations/ExerciseLibrarySeedConfiguration.cs backend/src/Coaching.Infrastructure/Coaching.Infrastructure.csproj backend/src/Coaching.Infrastructure/Seeding/DatabaseInitializer.cs backend/src/Coaching.Api/Controllers/Admin/ExercisesController.cs backend/src/Coaching.Api/appsettings.json database/changelog/v0.5.0 backend/tests/Coaching.Api.IntegrationTests/ApiFactory.cs backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseListTests.cs backend/tests/Coaching.Api.IntegrationTests/Exercises/ExerciseLibrarySeedTests.cs backend/tests/Coaching.Acceptance/Support/AcceptanceContext.cs backend/tests/Coaching.Acceptance/Support/ExerciseApi.cs backend/tests/Coaching.Acceptance/Steps/ExerciseListSteps.cs
git commit -m "$(cat <<'EOF'
feat(exercises): list, search and filter exercises incl. hidden view; seed the 99-exercise starting library once per coach in every environment (BR-f03-thu-vien-bai-tap-016, 018, 019)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
)"
```

---

### Task 6: [UI] Thư viện bài tập

Màn hình (tech-design §3 "Thiết kế giao diện (UI)", page `AdThuVien`, extract `.kv/sdd/proto/AdThuVien.md`): **Thư viện bài tập** `/thu-vien/bai-tap` + mục rail "Thư viện bài tập" (icon tạ của prototype). Style chép nguyên văn inline style của `AdThuVien`: main `padding: 28px clamp(16px, 3vw, 44px) 48px; gap: 22px`; H1 Barlow 800 46px (`< 880px` 38px) + dòng tóm tắt 14px `--ink2`; nút chính "Thêm bài mới" 50px Barlow 700 20px; ô tìm 48px radius 14; chip nhóm cơ 40px (`role=tablist` "Nhóm cơ"), chip dụng cụ nhỏ 34px/13px (`role=group` "Dụng cụ", nhãn "Dụng cụ" 13px `--muted`, `margin-top: -10px`); bảng `min-width: 600px` (header 13px 500 `--muted` padding `12px 14px`, ô `11px 14px` viền `--rowLine`, dòng chọn `--soft`); nhãn video `--okBg/--okInk` "Xem video" / `--warnBg/--warn` "Thiếu video"; aside `flex: 1 1 320px; max-width: 420px; padding: 20px; gap: 14px` (meta 13px `--primary` 600, H2 Barlow 800 31px, khung video 16:9 hoặc 9:16 `max-width: 260px` cho shorts, `dl` 4 ô Sets/Reps/RIR/Nghỉ nền `--bg` radius 12, hộp ghi chú 13px nền `--bg` radius 10). Bỏ cột/khối "Đang dùng trong" và nút "Thêm vào giáo án" (F05).

*Suy ra* (tech-design đã ghi, PO duyệt 2026-11-26): chip "Đã ẩn" cuối hàng chip nhóm cơ (BR-f03-thu-vien-bai-tap-016); trong aside nút phụ "Ẩn bài"/"Bỏ ẩn" và nút chữ "Xoá bài" (hỏi xác nhận); "Sửa bài" mở `/thu-vien/bai-tap/:id/sua`. Câu chữ không có trong prototype/tech-design (đặt theo giọng prototype, PO xem khi duyệt màn): dòng tóm tắt "{n} bài, {m} bài thiếu video mẫu." / ", bài nào cũng có video mẫu." + " Đang hiện {k} bài." khi đang lọc (prototype nói về "giáo án hồng/bài mới thêm" — không có trong dữ liệu thật); hộp "Chưa có hướng dẫn kỹ thuật và lỗi hay gặp cho bài này. Bấm Sửa bài để thêm." (bỏ "bài thay thế" — non-goal); "Chưa có liều mặc định."; câu báo ẩn/bỏ ẩn/xoá; câu xác nhận xoá; câu rỗng/lỗi tải. Nhãn "Xem video" trong bảng là nhãn (span), không phải link — `GET /api/admin/exercises` không trả `videoUrl`; link YouTube nằm ở aside "Mở trên YouTube".

Tạo luôn các helper dùng chung cho Task 7: `exercise.models.ts`, `exercises.api.ts` (+ `problemCode`/`problemTitle`), `dose-format.ts`, `youtube.ts`.

**Files:**
- Create: `frontend/projects/admin/src/app/exercises/exercise.models.ts`
- Create: `frontend/projects/admin/src/app/exercises/exercises.api.ts`, `exercises.api.spec.ts`
- Create: `frontend/projects/admin/src/app/exercises/dose-format.ts`, `dose-format.spec.ts`
- Create: `frontend/projects/admin/src/app/exercises/youtube.ts`, `youtube.spec.ts`
- Create: `frontend/projects/admin/src/app/exercises/exercise-library.component.ts`, `exercise-library.component.spec.ts`
- Modify: `frontend/projects/shared/src/lib/ui/icons.ts` (thêm `dumbbell`, path nguyên văn rail `AdThuVien`)
- Modify: `frontend/projects/admin/src/app/shell/admin-shell.component.ts`, `admin-shell.component.spec.ts`
- Modify: `frontend/projects/admin/src/app/app.routes.ts`

**Interfaces:**
- Consumes: Task 1–5 — `GET /api/admin/exercise-categories` → `200 { muscleGroups: [{id,name}], equipment: [{id,name}] }` theo `SortOrder` (11 nhóm cơ, 7 dụng cụ ban đầu); `GET /api/admin/exercises?q&muscleGroupId&equipmentId&hidden` → `200 [{ id, name, muscleGroup, equipment, hasVideo, isHidden }]` sắp theo tên; `GET /api/admin/exercises/{id}` → `ExerciseDetailDto { id, name, muscleGroupId, muscleGroup, equipmentId, equipment, videoUrl, hasVideo, sets, repsMin, repsMax, rirMin, rirMax, restSeconds, techniqueNotes, commonMistakes, isHidden, version }`; `POST /api/admin/exercises/{id}/hide|unhide` → `200 ExerciseDetailDto`; `DELETE /api/admin/exercises/{id}` → `204`, `409 { code: "exercise.in_use", title: "Bài đang có trong giáo án, chỉ ẩn được." }`. ProblemDetails có `code` + `title` (`ResultExtensions.ToProblem`). Shell rail hiện có (`AdminShellComponent`), `ICONS`, `StatusBadgeComponent`, `AuthService.apiUrl`.
- Produces: `ExercisesApi` (`list(filter)`, `get`, `create`, `update`, `hide`, `unhide`, `remove`, `categories`, `addCategory(kind, name)`), `problemCode(error)`, `problemTitle(error, fallback)`; models `CategoryKind`, `Category`, `Categories`, `CategoryCreated`, `ExerciseListItem`, `ExerciseDetail`, `ExerciseFilter`, `SaveExerciseBody`; `parseRange`, `parseWhole`, `formatRange`, `formatCount`, `formatRest`; `youTubeId`, `isShort`, `embedUrl`; `ExerciseLibraryComponent` (input `bai` = query `?bai=<id>` chọn sẵn bài), route `/thu-vien/bai-tap`; selector cho Task 8: H1 "Thư viện bài tập", ô tìm nhãn "Tìm bài tập" (Enter để tìm), link "Thêm bài mới", bảng `tbody tr` với `th[scope=row]` (rowheader = tên bài) + `td` Nhóm cơ / Dụng cụ / Video mẫu.

**Owns:** —

- [ ] **Step 1: Viết spec cho helper (liều, YouTube, API)**

`frontend/projects/admin/src/app/exercises/dose-format.spec.ts`:
```ts
import { describe, expect, it } from 'vitest';
import { formatCount, formatRange, formatRest, parseRange, parseWhole } from './dose-format';

// @ADR-f03-thu-vien-bai-tap-006: FE nhận chữ "10–12"/"10-12"/"10", gửi min/max; hiển thị "10–12", "90s", trống "—"
describe('parseRange', () => {
  it.each([
    ['10', { min: 10, max: 10 }],
    ['10–12', { min: 10, max: 12 }],
    ['10-12', { min: 10, max: 12 }],
    [' 10 — 12 ', { min: 10, max: 12 }],
    ['0–1', { min: 0, max: 1 }],
    ['', { min: null, max: null }],
    ['   ', { min: null, max: null }],
  ])('reads "%s"', (text, expected) => {
    expect(parseRange(text)).toEqual(expected);
  });

  it.each(['abc', '10–', '–12', '1.5', '10–12–14', '1000'])('rejects "%s" as invalid', (text) => {
    expect(parseRange(text)).toBe('invalid');
  });

  it('leaves the range rule to the API (BR-f03-thu-vien-bai-tap-010): "12–10" is parsed as typed', () => {
    expect(parseRange('12–10')).toEqual({ min: 12, max: 10 });
  });
});

describe('parseWhole', () => {
  it.each([['3', 3], [' 90 ', 90], ['', null], ['  ', null]])('reads "%s"', (text, expected) => {
    expect(parseWhole(text)).toBe(expected);
  });

  it.each(['abc', '1.5', '-3', '3 hiệp'])('rejects "%s" as invalid', (text) => {
    expect(parseWhole(text)).toBe('invalid');
  });
});

describe('format', () => {
  it('formats a range with an en dash, a single value without, empty as —', () => {
    expect(formatRange(10, 12)).toBe('10–12');
    expect(formatRange(10, 10)).toBe('10');
    expect(formatRange(1, null)).toBe('1');
    expect(formatRange(null, null)).toBe('—');
    expect(formatRange(null, null, '')).toBe('');
  });

  it('formats rest as seconds and sets as a plain number', () => {
    expect(formatRest(90)).toBe('90s');
    expect(formatRest(null)).toBe('—');
    expect(formatCount(3)).toBe('3');
    expect(formatCount(null)).toBe('—');
  });
});
```

`frontend/projects/admin/src/app/exercises/youtube.spec.ts`:
```ts
import { describe, expect, it } from 'vitest';
import { embedUrl, isShort, youTubeId } from './youtube';

// @ADR-f03-thu-vien-bai-tap-005: chỉ nhúng khi rút được mã video 11 ký tự; còn lại chỉ hiện link
describe('youtube', () => {
  it.each([
    ['https://www.youtube.com/watch?v=vcBig73ojpE', 'vcBig73ojpE'],
    ['https://youtube.com/shorts/PfmfAeMpym4', 'PfmfAeMpym4'],
    ['https://www.youtube.com/shorts/PfmfAeMpym4?feature=share', 'PfmfAeMpym4'],
    ['https://youtu.be/abc123XYZ09', 'abc123XYZ09'],
    ['https://www.youtube.com/embed/vcBig73ojpE', 'vcBig73ojpE'],
    ['https://www.youtube.com/watch?feature=share&v=-YcyYvmXZeM', '-YcyYvmXZeM'],
  ])('extracts the video id from %s', (url, id) => {
    expect(youTubeId(url)).toBe(id);
  });

  it('has no id for a playlist, a channel or no link', () => {
    expect(youTubeId('https://www.youtube.com/playlist?list=PL1234567890')).toBeNull();
    expect(youTubeId('https://www.youtube.com/@duobexiuuu')).toBeNull();
    expect(youTubeId(null)).toBeNull();
    expect(youTubeId('')).toBeNull();
  });

  it('tells shorts apart (9:16 frame)', () => {
    expect(isShort('https://youtube.com/shorts/PfmfAeMpym4')).toBe(true);
    expect(isShort('https://www.youtube.com/watch?v=vcBig73ojpE')).toBe(false);
    expect(isShort(null)).toBe(false);
  });

  it('embeds through youtube-nocookie with the prototype parameters', () => {
    expect(embedUrl('PfmfAeMpym4')).toBe('https://www.youtube-nocookie.com/embed/PfmfAeMpym4?rel=0&modestbranding=1&playsinline=1');
  });
});
```

`frontend/projects/admin/src/app/exercises/exercises.api.spec.ts`:
```ts
import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { describe, expect, it } from 'vitest';
import { ExercisesApi, problemCode, problemTitle } from './exercises.api';

describe('ExercisesApi', () => {
  function setup() {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    return { api: TestBed.inject(ExercisesApi), http: TestBed.inject(HttpTestingController) };
  }

  it('lists without parameters by default and sends only the filters that are set', () => {
    const { api, http } = setup();
    api.list().subscribe();
    http.expectOne((r) => r.url === '/api/admin/exercises' && r.params.keys().length === 0).flush([]);

    api.list({ q: '  fly ', muscleGroupId: 'g0', equipmentId: null, hidden: true }).subscribe();
    const req = http.expectOne((r) => r.url === '/api/admin/exercises' && r.params.keys().length > 0);
    expect(req.request.params.get('q')).toBe('fly');
    expect(req.request.params.get('muscleGroupId')).toBe('g0');
    expect(req.request.params.has('equipmentId')).toBe(false);
    expect(req.request.params.get('hidden')).toBe('true');
    req.flush([]);
  });

  it('calls the contract routes for hide, unhide, delete and categories', () => {
    const { api, http } = setup();
    api.hide('x1').subscribe();
    expect(http.expectOne({ method: 'POST', url: '/api/admin/exercises/x1/hide' }).request.body).toEqual({});
    api.unhide('x1').subscribe();
    http.expectOne({ method: 'POST', url: '/api/admin/exercises/x1/unhide' });
    api.remove('x1').subscribe();
    http.expectOne({ method: 'DELETE', url: '/api/admin/exercises/x1' });
    api.categories().subscribe();
    http.expectOne({ method: 'GET', url: '/api/admin/exercise-categories' });
    api.addCategory('Equipment', 'Resistance Band').subscribe();
    expect(http.expectOne({ method: 'POST', url: '/api/admin/exercise-categories' }).request.body)
      .toEqual({ kind: 'Equipment', name: 'Resistance Band' });
  });

  it('reads code and title from ProblemDetails, with a fallback title', () => {
    const problem = new HttpErrorResponse({ status: 409, error: { code: 'exercise.in_use', title: 'Bài đang có trong giáo án, chỉ ẩn được.' } });
    expect(problemCode(problem)).toBe('exercise.in_use');
    expect(problemTitle(problem, 'x')).toBe('Bài đang có trong giáo án, chỉ ẩn được.');
    expect(problemCode(new Error('boom'))).toBeNull();
    expect(problemTitle(new HttpErrorResponse({ status: 0 }), 'Thử lại.')).toBe('Thử lại.');
  });
});
```

- [ ] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: FAIL — build lỗi `Cannot find module './dose-format'`, `'./youtube'`, `'./exercises.api'`.

- [ ] **Step 3: Viết models, API, helper liều và YouTube**

`frontend/projects/admin/src/app/exercises/exercise.models.ts`:
```ts
/** GET/POST /api/admin/exercise-categories — @ADR-f03-thu-vien-bai-tap-003. */
export type CategoryKind = 'MuscleGroup' | 'Equipment';

export interface Category {
  id: string;
  name: string;
}

/** Theo SortOrder: 11 nhóm cơ + 7 dụng cụ ban đầu (@BR-f03-thu-vien-bai-tap-005), mục coach thêm nằm cuối. */
export interface Categories {
  muscleGroups: Category[];
  equipment: Category[];
}

export interface CategoryCreated {
  id: string;
  kind: CategoryKind;
  name: string;
}

/** GET /api/admin/exercises — sắp theo tên. */
export interface ExerciseListItem {
  id: string;
  name: string;
  muscleGroup: string;
  equipment: string;
  hasVideo: boolean;
  isHidden: boolean;
}

/** GET /api/admin/exercises/{id} (ExerciseDetailDto) — liều có cấu trúc (@ADR-f03-thu-vien-bai-tap-006). */
export interface ExerciseDetail {
  id: string;
  name: string;
  muscleGroupId: string;
  muscleGroup: string;
  equipmentId: string;
  equipment: string;
  videoUrl: string | null;
  hasVideo: boolean;
  sets: number | null;
  repsMin: number | null;
  repsMax: number | null;
  rirMin: number | null;
  rirMax: number | null;
  restSeconds: number | null;
  techniqueNotes: string | null;
  commonMistakes: string | null;
  isHidden: boolean;
  version: string;
}

export interface ExerciseFilter {
  q?: string;
  muscleGroupId?: string | null;
  equipmentId?: string | null;
  /** true = chỉ bài đã ẩn (@BR-f03-thu-vien-bai-tap-016). */
  hidden?: boolean;
}

/** Body POST /api/admin/exercises; PUT thêm `version`. */
export interface SaveExerciseBody {
  name: string;
  muscleGroupId: string;
  equipmentId: string;
  videoUrl: string | null;
  sets: number | null;
  repsMin: number | null;
  repsMax: number | null;
  rirMin: number | null;
  rirMax: number | null;
  restSeconds: number | null;
  techniqueNotes: string | null;
  commonMistakes: string | null;
  version?: string;
}
```

`frontend/projects/admin/src/app/exercises/exercises.api.ts`:
```ts
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AuthService } from '@coaching/shared';
import { Observable } from 'rxjs';
import {
  Categories, CategoryCreated, CategoryKind, ExerciseDetail, ExerciseFilter, ExerciseListItem, SaveExerciseBody,
} from './exercise.models';

@Injectable({ providedIn: 'root' })
export class ExercisesApi {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  private url(path = ''): string {
    return this.auth.apiUrl(`/api/admin/exercises${path}`);
  }

  /** Tìm không dấu ở server (@BR-f03-thu-vien-bai-tap-018); lọc kết hợp (@BR-f03-thu-vien-bai-tap-019). */
  list(filter: ExerciseFilter = {}): Observable<ExerciseListItem[]> {
    let params = new HttpParams();
    const q = filter.q?.trim();
    if (q) params = params.set('q', q);
    if (filter.muscleGroupId) params = params.set('muscleGroupId', filter.muscleGroupId);
    if (filter.equipmentId) params = params.set('equipmentId', filter.equipmentId);
    if (filter.hidden) params = params.set('hidden', 'true');
    return this.http.get<ExerciseListItem[]>(this.url(), { params });
  }

  get(id: string): Observable<ExerciseDetail> {
    return this.http.get<ExerciseDetail>(this.url(`/${id}`));
  }

  create(body: SaveExerciseBody): Observable<ExerciseDetail> {
    return this.http.post<ExerciseDetail>(this.url(), body);
  }

  update(id: string, body: SaveExerciseBody): Observable<ExerciseDetail> {
    return this.http.put<ExerciseDetail>(this.url(`/${id}`), body);
  }

  hide(id: string): Observable<ExerciseDetail> {
    return this.http.post<ExerciseDetail>(this.url(`/${id}/hide`), {});
  }

  unhide(id: string): Observable<ExerciseDetail> {
    return this.http.post<ExerciseDetail>(this.url(`/${id}/unhide`), {});
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(this.url(`/${id}`));
  }

  categories(): Observable<Categories> {
    return this.http.get<Categories>(this.auth.apiUrl('/api/admin/exercise-categories'));
  }

  addCategory(kind: CategoryKind, name: string): Observable<CategoryCreated> {
    return this.http.post<CategoryCreated>(this.auth.apiUrl('/api/admin/exercise-categories'), { kind, name });
  }
}

/** `code` của ProblemDetails (ResultExtensions.ToProblem), null khi không phải lỗi API. */
export function problemCode(error: unknown): string | null {
  return error instanceof HttpErrorResponse && typeof error.error?.code === 'string' ? error.error.code : null;
}

/** `title` tiếng Việt của ProblemDetails, hoặc câu dự phòng (mất mạng, 5xx không body). */
export function problemTitle(error: unknown, fallback: string): string {
  return error instanceof HttpErrorResponse && typeof error.error?.title === 'string' ? error.error.title : fallback;
}
```

`frontend/projects/admin/src/app/exercises/dose-format.ts`:
```ts
/** @ADR-f03-thu-vien-bai-tap-006: liều nhập dạng chữ ở FE, gửi API dạng số; khoảng hợp lệ do Domain kiểm (BR-009–012). */
export interface DoseRange {
  min: number | null;
  max: number | null;
}

const RANGE = /^(\d{1,3})(?:\s*[-‐–—]\s*(\d{1,3}))?$/;
const WHOLE = /^\d{1,4}$/;

/** "10" → 10–10; "10–12" / "10-12" → 10–12; trống → {null, null}; sai định dạng → 'invalid'. */
export function parseRange(text: string): DoseRange | 'invalid' {
  const t = text.trim();
  if (!t) return { min: null, max: null };
  const m = RANGE.exec(t);
  if (!m) return 'invalid';
  const min = Number(m[1]);
  return { min, max: m[2] === undefined ? min : Number(m[2]) };
}

/** Số nguyên không dấu (Sets, Nghỉ giây); trống → null; khác → 'invalid'. */
export function parseWhole(text: string): number | null | 'invalid' {
  const t = text.trim();
  if (!t) return null;
  return WHOLE.test(t) ? Number(t) : 'invalid';
}

/** 10–12 → "10–12"; 10–10 → "10"; trống → `empty` ("—" khi hiển thị, "" khi đổ vào ô nhập). */
export function formatRange(min: number | null, max: number | null, empty = '—'): string {
  if (min === null && max === null) return empty;
  if (min === null) return `${max}`;
  if (max === null || max === min) return `${min}`;
  return `${min}–${max}`;
}

export function formatCount(value: number | null): string {
  return value === null ? '—' : `${value}`;
}

/** 90 → "90s" như prototype. */
export function formatRest(seconds: number | null): string {
  return seconds === null ? '—' : `${seconds}s`;
}
```

`frontend/projects/admin/src/app/exercises/youtube.ts`:
```ts
/** Regex nguyên văn prototype AdThuVien (`yid`). @ADR-f03-thu-vien-bai-tap-005 */
const VIDEO_ID = /(?:shorts\/|v=|youtu\.be\/|embed\/)([\w-]{11})/;

export function youTubeId(url: string | null | undefined): string | null {
  const m = VIDEO_ID.exec(url ?? '');
  return m ? m[1] : null;
}

export function isShort(url: string | null | undefined): boolean {
  return /\/shorts\//.test(url ?? '');
}

/** Chỉ nhúng qua youtube-nocookie với mã đã rút ([\w-]{11}) — không bao giờ nhúng URL coach nhập. */
export function embedUrl(id: string): string {
  return `https://www.youtube-nocookie.com/embed/${id}?rel=0&modestbranding=1&playsinline=1`;
}
```

- [ ] **Step 4: Chạy — kỳ vọng PASS (helper)**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: PASS — `dose-format`, `youtube`, `ExercisesApi` xanh; các spec cũ của admin vẫn xanh.

- [ ] **Step 5: Viết spec màn Thư viện và mục rail**

`frontend/projects/admin/src/app/exercises/exercise-library.component.spec.ts`:
```ts
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ExerciseLibraryComponent, NO_NOTES, NO_VIDEO } from './exercise-library.component';
import { Categories, ExerciseDetail, ExerciseListItem } from './exercise.models';

const settle = () => new Promise((resolve) => setTimeout(resolve, 0));

const CATS: Categories = {
  muscleGroups: ['Ngực', 'Vai', 'Tay sau', 'Tay trước', 'Lưng', 'Vai sau', 'Đùi trước', 'Đùi sau', 'Mông', 'Bắp chân', 'Bụng']
    .map((name, i) => ({ id: `g${i}`, name })),
  equipment: ['Machine', 'Cable', 'Dumbbell', 'Barbell', 'Smith Machine', 'Kettlebell', 'Bodyweight']
    .map((name, i) => ({ id: `e${i}`, name })),
};

const ROWS: ExerciseListItem[] = [
  { id: 'x1', name: '45° Incline Chest Fly', muscleGroup: 'Ngực', equipment: 'Cable', hasVideo: true, isHidden: false },
  { id: 'x2', name: 'Barbell Bench Press', muscleGroup: 'Ngực', equipment: 'Barbell', hasVideo: true, isHidden: false },
  { id: 'x3', name: 'Lateral Raise', muscleGroup: 'Vai', equipment: 'Cable', hasVideo: false, isHidden: false },
];

function detail(overrides: Partial<ExerciseDetail> = {}): ExerciseDetail {
  return {
    id: 'x1', name: '45° Incline Chest Fly', muscleGroupId: 'g0', muscleGroup: 'Ngực', equipmentId: 'e1', equipment: 'Cable',
    videoUrl: 'https://www.youtube.com/shorts/PfmfAeMpym4', hasVideo: true, sets: 3, repsMin: 10, repsMax: 12, rirMin: 1, rirMax: 2,
    restSeconds: 90, techniqueNotes: null, commonMistakes: null, isHidden: false, version: 'AAAAAAAAB9E=',
    ...overrides,
  };
}

describe('ExerciseLibraryComponent', () => {
  afterEach(() => vi.restoreAllMocks());

  function mount(bai?: string) {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '' } },
        { provide: CLIENT_APP, useValue: 'admin' },
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    const fixture = TestBed.createComponent(ExerciseLibraryComponent);
    if (bai) fixture.componentRef.setInput('bai', bai);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const render = async () => {
      await settle();
      fixture.detectChanges();
    };
    const chip = (label: string) =>
      [...el.querySelectorAll('button.chip')].find((b) => b.textContent?.trim() === label) as HTMLButtonElement;
    const button = (label: string) =>
      [...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === label) as HTMLButtonElement | undefined;
    return { fixture, el, http: TestBed.inject(HttpTestingController), render, chip, button };
  }

  /** Danh mục → danh sách mặc định (không lọc); chưa trả chi tiết bài đầu. */
  async function setup(bai?: string) {
    const m = mount(bai);
    m.http.expectOne('/api/admin/exercise-categories').flush(CATS);
    await settle();
    m.http.expectOne('/api/admin/exercises').flush(ROWS);
    await settle();
    return m;
  }

  it('shows the title, one chip per muscle group with its count, the hidden chip, equipment chips and the table', async () => {
    const { el, http, render } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();

    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Thư viện bài tập');
    expect(el.querySelector('.summary')?.textContent?.trim()).toBe('3 bài, 1 bài thiếu video mẫu.');
    const groups = [...el.querySelectorAll('[role=tablist][aria-label="Nhóm cơ"] button')].map((b) => b.textContent?.trim());
    expect(groups).toEqual([
      'Tất cả (3)', 'Ngực (2)', 'Vai (1)', 'Tay sau (0)', 'Tay trước (0)', 'Lưng (0)', 'Vai sau (0)',
      'Đùi trước (0)', 'Đùi sau (0)', 'Mông (0)', 'Bắp chân (0)', 'Bụng (0)', 'Đã ẩn',
    ]);
    expect(el.querySelector('[role=tab][aria-selected=true]')?.textContent?.trim()).toBe('Tất cả (3)');
    const equips = [...el.querySelectorAll('[role=group][aria-label="Dụng cụ"] button')].map((b) => b.textContent?.trim());
    expect(equips).toEqual(['Tất cả', 'Machine', 'Cable', 'Dumbbell', 'Barbell', 'Smith Machine', 'Kettlebell', 'Bodyweight']);
    expect(el.querySelector('label[for=q]')?.textContent?.trim()).toBe('Tìm bài tập');
    expect((el.querySelector('#q') as HTMLInputElement).placeholder).toBe('Tìm theo tên bài, gõ xong bấm Enter');

    expect([...el.querySelectorAll('thead th')].map((th) => th.textContent?.trim())).toEqual(['Bài tập', 'Nhóm cơ', 'Dụng cụ', 'Video mẫu']);
    const rows = [...el.querySelectorAll('tbody tr')];
    expect(rows).toHaveLength(3);
    expect(rows[0].querySelector('th[scope=row]')?.textContent?.trim()).toBe('45° Incline Chest Fly');
    expect(rows[0].querySelector('.video')?.textContent?.trim()).toBe('Xem video');
    expect(rows[2].querySelector('.video')?.textContent?.trim()).toBe('Thiếu video');
    expect(rows[2].querySelector('.video')?.classList.contains('missing')).toBe(true);
    expect(el.querySelector('a.create')?.getAttribute('href')).toBe('/thu-vien/bai-tap/moi');
    expect(el.textContent).not.toContain('Đang dùng trong');
    expect(el.textContent).not.toContain('Thêm vào giáo án');
  });

  // @ADR-f03-thu-vien-bai-tap-005 @ADR-f03-thu-vien-bai-tap-006
  it('shows the first exercise in the aside with a nocookie 9:16 embed for shorts and the formatted dose', async () => {
    const { el, http, render } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();

    expect(el.querySelector('tbody tr.on th')?.textContent?.trim()).toBe('45° Incline Chest Fly');
    expect(el.querySelector('tbody tr.on button.pick')?.getAttribute('aria-pressed')).toBe('true');
    const aside = el.querySelector('aside')!;
    expect(aside.querySelector('.meta')?.textContent?.trim()).toBe('Ngực, Cable');
    expect(aside.querySelector('h2')?.textContent?.trim()).toBe('45° Incline Chest Fly');
    const iframe = aside.querySelector('iframe')!;
    expect(iframe.getAttribute('src')).toBe('https://www.youtube-nocookie.com/embed/PfmfAeMpym4?rel=0&modestbranding=1&playsinline=1');
    expect(iframe.getAttribute('title')).toBe('Video mẫu 45° Incline Chest Fly');
    expect(aside.querySelector('.frame')?.classList.contains('short')).toBe(true);
    expect(aside.querySelector('a.yt')?.getAttribute('href')).toBe('https://www.youtube.com/shorts/PfmfAeMpym4');
    expect(aside.querySelector('a.yt')?.textContent?.trim()).toBe('Mở trên YouTube');
    const dose = [...aside.querySelectorAll('dl > div')].map((d) => [d.querySelector('dt')?.textContent?.trim(), d.querySelector('dd')?.textContent?.trim()]);
    expect(dose).toEqual([['Sets', '3'], ['Reps', '10–12'], ['RIR', '1–2'], ['Nghỉ', '90s']]);
    expect(aside.querySelector('.box')?.textContent?.trim()).toBe(NO_NOTES);
    expect(aside.querySelector('a.edit')?.getAttribute('href')).toBe('/thu-vien/bai-tap/x1/sua');
    expect([...aside.querySelectorAll('.actions button')].map((b) => b.textContent?.trim())).toEqual(['Ẩn bài', 'Xoá bài']);
  });

  it('shows a watch video in a 16:9 frame and the technique notes and common mistakes', async () => {
    const { el, http, render } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();

    (el.querySelectorAll('tbody button.pick')[1] as HTMLButtonElement).click();
    http.expectOne('/api/admin/exercises/x2').flush(detail({
      id: 'x2', name: 'Barbell Bench Press', equipmentId: 'e3', equipment: 'Barbell', videoUrl: 'https://www.youtube.com/watch?v=vcBig73ojpE',
      techniqueNotes: 'Siết bả vai, hạ thanh chạm ngực dưới', commonMistakes: 'Nảy thanh trên ngực',
    }));
    await render();

    const aside = el.querySelector('aside')!;
    expect(el.querySelector('tbody tr.on th')?.textContent?.trim()).toBe('Barbell Bench Press');
    expect(aside.querySelector('iframe')?.getAttribute('src')).toBe('https://www.youtube-nocookie.com/embed/vcBig73ojpE?rel=0&modestbranding=1&playsinline=1');
    expect(aside.querySelector('.frame')?.classList.contains('short')).toBe(false);
    const notes = [...aside.querySelectorAll('section.notes')].map((s) => [s.querySelector('h3')?.textContent?.trim(), s.querySelector('p')?.textContent?.trim()]);
    expect(notes).toEqual([['Hướng dẫn kỹ thuật', 'Siết bả vai, hạ thanh chạm ngực dưới'], ['Lỗi hay gặp', 'Nảy thanh trên ngực']]);
    expect(aside.querySelector('.box')).toBeNull();
  });

  // @BR-f03-thu-vien-bai-tap-008
  it('says the video is missing and shows — for an empty dose', async () => {
    const { el, http, render } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();

    (el.querySelectorAll('tbody button.pick')[2] as HTMLButtonElement).click();
    http.expectOne('/api/admin/exercises/x3').flush(detail({
      id: 'x3', name: 'Lateral Raise', muscleGroupId: 'g1', muscleGroup: 'Vai', videoUrl: null, hasVideo: false,
      sets: null, repsMin: null, repsMax: null, rirMin: null, rirMax: null, restSeconds: null,
    }));
    await render();

    const aside = el.querySelector('aside')!;
    expect(aside.querySelector('iframe')).toBeNull();
    expect(aside.querySelector('a.yt')).toBeNull();
    expect(aside.textContent).toContain(NO_VIDEO);
    expect([...aside.querySelectorAll('dd')].map((d) => d.textContent?.trim())).toEqual(['—', '—', '—', '—']);
    expect(aside.querySelector('.rx')?.textContent?.trim()).toBe('Chưa có liều mặc định.');
  });

  // @BR-f03-thu-vien-bai-tap-016 @BR-f03-thu-vien-bai-tap-018 @BR-f03-thu-vien-bai-tap-019
  it('combines muscle group, equipment and keyword, and lists hidden exercises under "Đã ẩn"', async () => {
    const { el, http, render, chip } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();

    chip('Ngực (2)').click();
    http.expectOne((r) => r.url === '/api/admin/exercises' && r.params.get('muscleGroupId') === 'g0' && !r.params.has('hidden'))
      .flush([ROWS[0], ROWS[1]]);
    await render();
    expect(chip('Ngực (2)').getAttribute('aria-selected')).toBe('true');
    expect(chip('Ngực (2)').classList.contains('on')).toBe(true);

    chip('Cable').click();
    http.expectOne((r) => r.params.get('muscleGroupId') === 'g0' && r.params.get('equipmentId') === 'e1').flush([ROWS[0]]);
    await render();
    expect(chip('Cable').getAttribute('aria-pressed')).toBe('true');

    const q = el.querySelector('#q') as HTMLInputElement;
    q.value = 'fly';
    q.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    http.expectOne((r) => r.params.get('q') === 'fly' && r.params.get('muscleGroupId') === 'g0' && r.params.get('equipmentId') === 'e1')
      .flush([ROWS[0]]);
    await render();
    expect(el.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(el.querySelector('.summary')?.textContent?.trim()).toBe('3 bài, 1 bài thiếu video mẫu. Đang hiện 1 bài.');

    chip('Đã ẩn').click();
    http.expectOne((r) => r.params.get('hidden') === 'true' && !r.params.has('muscleGroupId')
      && r.params.get('equipmentId') === 'e1' && r.params.get('q') === 'fly').flush([]);
    await render();
    expect(el.querySelector('table')).toBeNull();
    expect(el.querySelector('aside')).toBeNull();
    expect(el.querySelector('.empty')?.textContent?.trim()).toBe('Không tìm thấy bài phù hợp.');
  });

  // @BR-f03-thu-vien-bai-tap-015
  it('hides the selected exercise, keeps it in the aside with "Bỏ ẩn", and unhides it again', async () => {
    const { el, http, render, button } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();

    button('Ẩn bài')!.click();
    http.expectOne({ method: 'POST', url: '/api/admin/exercises/x1/hide' }).flush(detail({ isHidden: true }));
    await settle();
    http.expectOne('/api/admin/exercises').flush([ROWS[1], ROWS[2]]);
    await render();

    expect(el.querySelectorAll('tbody tr')).toHaveLength(2);
    expect(el.querySelector('aside h2')?.textContent?.trim()).toBe('45° Incline Chest Fly');
    expect(el.querySelector('aside dbx-status-badge')?.textContent?.trim()).toBe('Đã ẩn');
    expect(button('Bỏ ẩn')).toBeDefined();
    expect(el.querySelector('.msg')?.textContent?.trim()).toBe('Đã ẩn bài "45° Incline Chest Fly". Xem lại ở mục Đã ẩn.');
    expect(el.querySelector('[role=tab][aria-selected=true]')?.textContent?.trim()).toBe('Tất cả (2)');

    button('Bỏ ẩn')!.click();
    http.expectOne({ method: 'POST', url: '/api/admin/exercises/x1/unhide' }).flush(detail());
    await settle();
    http.expectOne('/api/admin/exercises').flush(ROWS);
    await render();

    expect(el.querySelectorAll('tbody tr')).toHaveLength(3);
    expect(button('Ẩn bài')).toBeDefined();
    expect(el.querySelector('aside dbx-status-badge')).toBeNull();
    expect(el.querySelector('.msg')?.textContent?.trim()).toBe('Đã bỏ ẩn bài "45° Incline Chest Fly".');
  });

  // @BR-f03-thu-vien-bai-tap-017
  it('deletes after confirmation and selects the first remaining exercise', async () => {
    const { el, http, render, button } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true);

    button('Xoá bài')!.click();
    expect(confirm).toHaveBeenCalledWith('Xoá bài "45° Incline Chest Fly" (Cable) khỏi thư viện? Không hoàn tác được.');
    http.expectOne({ method: 'DELETE', url: '/api/admin/exercises/x1' }).flush(null, { status: 204, statusText: 'No Content' });
    await settle();
    http.expectOne('/api/admin/exercises').flush([ROWS[1], ROWS[2]]);
    await settle();
    http.expectOne('/api/admin/exercises/x2').flush(detail({ id: 'x2', name: 'Barbell Bench Press', equipment: 'Barbell' }));
    await render();

    expect(el.querySelector('.msg')?.textContent?.trim()).toBe('Đã xoá bài "45° Incline Chest Fly".');
    expect(el.querySelector('.msg')?.classList.contains('ok')).toBe(true);
    expect(el.querySelector('aside h2')?.textContent?.trim()).toBe('Barbell Bench Press');
  });

  // @BR-f03-thu-vien-bai-tap-017
  it('keeps the exercise when deletion is cancelled or refused because it is in use', async () => {
    const { el, http, render, button } = await setup();
    http.expectOne('/api/admin/exercises/x1').flush(detail());
    await render();
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false);

    button('Xoá bài')!.click();
    http.expectNone({ method: 'DELETE', url: '/api/admin/exercises/x1' });

    confirm.mockReturnValue(true);
    button('Xoá bài')!.click();
    http.expectOne({ method: 'DELETE', url: '/api/admin/exercises/x1' })
      .flush({ code: 'exercise.in_use', title: 'Bài đang có trong giáo án, chỉ ẩn được.' }, { status: 409, statusText: 'Conflict' });
    await render();

    expect(el.querySelector('.msg')?.textContent?.trim()).toBe('Bài đang có trong giáo án, chỉ ẩn được.');
    expect(el.querySelector('.msg')?.classList.contains('ok')).toBe(false);
    expect(el.querySelector('aside h2')?.textContent?.trim()).toBe('45° Incline Chest Fly');
    expect(el.querySelectorAll('tbody tr')).toHaveLength(3);
  });

  it('preselects the exercise given by ?bai=', async () => {
    const { el, http, render } = await setup('x3');
    http.expectNone('/api/admin/exercises/x1');
    http.expectOne('/api/admin/exercises/x3').flush(detail({ id: 'x3', name: 'Lateral Raise' }));
    await render();

    expect(el.querySelector('tbody tr.on th')?.textContent?.trim()).toBe('Lateral Raise');
    expect(el.querySelector('aside h2')?.textContent?.trim()).toBe('Lateral Raise');
  });

  it('shows an error line when the library cannot be loaded', async () => {
    const { el, http, render } = mount();
    http.expectOne('/api/admin/exercise-categories').flush('boom', { status: 500, statusText: 'Server Error' });
    await render();

    expect(el.querySelector('[role=alert]')?.textContent?.trim()).toBe('Không tải được thư viện bài tập. Vui lòng tải lại trang.');
  });
});
```

`frontend/projects/admin/src/app/shell/admin-shell.component.spec.ts` — thêm test cuối `describe`:
```ts
  it('lists the library in the rail after the student items', () => {
    const { el } = setup();

    const links = [...el.querySelectorAll('nav[aria-label="Admin"] a')].map((a) => [a.textContent?.trim(), a.getAttribute('href')]);
    expect(links).toEqual([
      ['Dashboard', '/dashboard'],
      ['Tạo hồ sơ học viên', '/hoc-vien/moi'],
      ['Thư viện bài tập', '/thu-vien/bai-tap'],
    ]);
  });
```

- [ ] **Step 6: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: FAIL — `Cannot find module './exercise-library.component'`; sau khi tạm bỏ file spec đó, `AdminShellComponent > lists the library in the rail…` fail (chỉ có 2 link).

- [ ] **Step 7: Viết màn Thư viện, icon, rail, route**

`frontend/projects/shared/src/lib/ui/icons.ts` — thêm khoá cuối object `ICONS` (path nguyên văn rail `AdThuVien`):
```ts
  dumbbell: '<path d="M6 8v8M18 8v8M3 10v4M21 10v4M6 12h12"></path>',
```

`frontend/projects/admin/src/app/shell/admin-shell.component.ts` — thêm link thứ ba trong `<nav aria-label="Admin">`, ngay sau link "Tạo hồ sơ học viên":
```html
          <a routerLink="/thu-vien/bai-tap" routerLinkActive #library="routerLinkActive" [attr.aria-current]="library.isActive ? 'page' : null">
            <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" stroke-linejoin="round" [innerHTML]="dumbbellIcon"></svg>Thư viện bài tập</a>
```
và thêm field sau `userPlusIcon`:
```ts
  protected readonly dumbbellIcon = this.sanitizer.bypassSecurityTrustHtml(ICONS.dumbbell);
```

`frontend/projects/admin/src/app/app.routes.ts` — thêm sau route `hoc-vien/:id/sua` (trong `children`):
```ts
      // F03: route tiếng Việt theo @ADR-f02-ho-so-hoc-vien-008
      {
        path: 'thu-vien/bai-tap',
        loadComponent: () => import('./exercises/exercise-library.component').then((m) => m.ExerciseLibraryComponent),
        title: 'Thư viện bài tập — Khu vực coach',
      },
```

`frontend/projects/admin/src/app/exercises/exercise-library.component.ts`:
```ts
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { ICONS, StatusBadgeComponent } from '@coaching/shared';
import { firstValueFrom } from 'rxjs';
import { formatCount, formatRange, formatRest } from './dose-format';
import { Categories, ExerciseDetail, ExerciseListItem } from './exercise.models';
import { ExercisesApi, problemTitle } from './exercises.api';
import { embedUrl, isShort, youTubeId } from './youtube';

const ALL = 'all';
const HIDDEN = 'hidden';
type Tone = 'ok' | 'warn';

/** Câu `video` nguyên văn prototype AdThuVien. */
export const NO_VIDEO = 'Chưa có video. Thêm link để học viên xem.';
/** Câu prototype bỏ "bài thay thế" (non-goal F03). */
export const NO_NOTES = 'Chưa có hướng dẫn kỹ thuật và lỗi hay gặp cho bài này. Bấm Sửa bài để thêm.';

/**
 * Thư viện bài tập (page AdThuVien) — danh sách + chi tiết; bỏ "Đang dùng trong" và "Thêm vào giáo án" (F05).
 * Chip "Đã ẩn" xem bài đã ẩn (@BR-f03-thu-vien-bai-tap-016); tìm/lọc ở server (@BR-f03-thu-vien-bai-tap-018, 019).
 */
@Component({
  selector: 'app-exercise-library',
  imports: [RouterLink, StatusBadgeComponent],
  template: `
    <header class="head">
      <div>
        <h1>Thư viện bài tập</h1>
        <p class="summary">{{ summary() }}</p>
      </div>
      <a class="create" routerLink="/thu-vien/bai-tap/moi">
        <svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
             stroke-linecap="round" [innerHTML]="plusIcon"></svg>Thêm bài mới</a>
    </header>

    <div class="search-row">
      <div class="search">
        <label for="q" class="sr-only">Tìm bài tập</label>
        <input id="q" type="search" placeholder="Tìm theo tên bài, gõ xong bấm Enter"
               (keydown.enter)="onSearch($any($event.target).value)" (change)="onSearch($any($event.target).value)" />
      </div>
    </div>

    <div role="tablist" aria-label="Nhóm cơ" class="groups">
      @for (g of groupChips(); track g.id) {
        <button type="button" role="tab" class="chip" [class.on]="group() === g.id" [attr.aria-selected]="group() === g.id"
                (click)="pickGroup(g.id)">{{ g.label }}</button>
      }
    </div>
    <div role="group" aria-label="Dụng cụ" class="equips">
      <span class="equips-label">Dụng cụ</span>
      @for (q of equipChips(); track q.id) {
        <button type="button" class="chip small" [class.on]="equipment() === q.id" [attr.aria-pressed]="equipment() === q.id"
                (click)="pickEquipment(q.id)">{{ q.label }}</button>
      }
    </div>

    @if (message(); as m) {
      <p class="msg" [class.ok]="m.tone === 'ok'" role="status">{{ m.text }}</p>
    }

    <div class="body">
      <div class="table-wrap">
        @if (rows(); as list) {
          @if (list.length > 0) {
            <table>
              <thead>
                <tr>
                  <th scope="col">Bài tập</th>
                  <th scope="col">Nhóm cơ</th>
                  <th scope="col">Dụng cụ</th>
                  <th scope="col">Video mẫu</th>
                </tr>
              </thead>
              <tbody>
                @for (r of list; track r.id) {
                  <tr [class.on]="selectedId() === r.id">
                    <th scope="row">
                      <button type="button" class="pick" [attr.aria-pressed]="selectedId() === r.id" (click)="pick(r.id)">{{ r.name }}</button>
                    </th>
                    <td class="group">{{ r.muscleGroup }}</td>
                    <td class="equip">{{ r.equipment }}</td>
                    <td><span class="video" [class.missing]="!r.hasVideo">{{ r.hasVideo ? 'Xem video' : 'Thiếu video' }}</span></td>
                  </tr>
                }
              </tbody>
            </table>
          } @else {
            <p class="empty">{{ emptyText() }}</p>
          }
        } @else if (failed()) {
          <p class="empty" role="alert">Không tải được thư viện bài tập. Vui lòng tải lại trang.</p>
        }
      </div>

      @if (detail(); as d) {
        <aside aria-labelledby="h-ct">
          <div>
            <p class="meta">{{ d.muscleGroup }}, {{ d.equipment }}</p>
            <h2 id="h-ct">{{ d.name }}</h2>
            @if (d.isHidden) {
              <dbx-status-badge class="hidden-badge" tone="paused" [dense]="true">Đã ẩn</dbx-status-badge>
            }
          </div>

          @if (video(); as v) {
            <div class="video-block">
              @if (v.embed) {
                <div class="frame" [class.short]="v.short">
                  <iframe [src]="v.embed" [title]="'Video mẫu ' + d.name" loading="lazy"
                          allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
                          referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe>
                </div>
              }
              <a class="yt" [href]="v.url" target="_blank" rel="noopener">Mở trên YouTube</a>
            </div>
          } @else {
            <p class="box">{{ noVideo }}</p>
          }

          <dl>
            <div><dt>Sets</dt><dd>{{ dose().sets }}</dd></div>
            <div><dt>Reps</dt><dd>{{ dose().reps }}</dd></div>
            <div><dt>RIR</dt><dd>{{ dose().rir }}</dd></div>
            <div><dt>Nghỉ</dt><dd>{{ dose().rest }}</dd></div>
          </dl>
          @if (dose().empty) {
            <p class="rx">Chưa có liều mặc định.</p>
          }

          @if (d.techniqueNotes || d.commonMistakes) {
            @if (d.techniqueNotes) {
              <section class="notes"><h3>Hướng dẫn kỹ thuật</h3><p>{{ d.techniqueNotes }}</p></section>
            }
            @if (d.commonMistakes) {
              <section class="notes"><h3>Lỗi hay gặp</h3><p>{{ d.commonMistakes }}</p></section>
            }
          } @else {
            <p class="box">{{ noNotes }}</p>
          }

          <div class="actions">
            <a class="secondary edit" [routerLink]="['/thu-vien/bai-tap', d.id, 'sua']">Sửa bài</a>
            <button type="button" class="secondary" [disabled]="busy()" (click)="toggleHidden(d)">{{ d.isHidden ? 'Bỏ ẩn' : 'Ẩn bài' }}</button>
            <button type="button" class="danger" [disabled]="busy()" (click)="remove(d)">Xoá bài</button>
          </div>
        </aside>
      }
    </div>
  `,
  styles: `
    /* AdThuVien main: padding 28px clamp(16px, 3vw, 44px) 48px, gap 22px */
    :host { display: flex; flex-direction: column; gap: 22px; box-sizing: border-box; padding: 28px clamp(16px, 3vw, 44px) 48px; }
    .head { display: flex; flex-wrap: wrap; justify-content: space-between; gap: 16px; align-items: flex-end; }
    h1 { margin: 0; font-family: var(--font-head); font-weight: 800; font-size: 46px; line-height: 1.05; }
    .summary { margin: 6px 0 0; font-size: 14px; color: var(--ink2); }
    .create { height: 50px; padding: 0 26px; border-radius: 999px; border: 0; background: var(--primary); color: var(--onPrimary);
      font-family: var(--font-head); font-weight: 700; font-size: 20px; cursor: pointer; display: inline-flex; align-items: center; gap: 8px;
      text-decoration: none; box-sizing: border-box; }
    .create:hover { color: var(--onPrimary); }
    .search-row { display: flex; flex-wrap: wrap; gap: 10px; align-items: center; }
    .search { flex: 1 1 280px; }
    .sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
    #q { box-sizing: border-box; width: 100%; height: 48px; border-radius: 14px; border: 1px solid var(--field); background: var(--surface);
      color: var(--ink); padding: 0 14px; font: inherit; font-size: 15px; }
    .groups { display: flex; flex-wrap: wrap; gap: 8px; }
    .chip { height: 40px; padding: 0 16px; border-radius: 999px; font: inherit; font-size: 14px; cursor: pointer;
      background: var(--surface); color: var(--ink); border: 1px solid var(--field); }
    .chip.on { background: var(--primary); color: var(--onPrimary); border: 0; font-weight: 600; }
    .chip.small { height: 34px; padding: 0 12px; font-size: 13px; }
    .equips { display: flex; flex-wrap: wrap; gap: 6px; align-items: center; margin-top: -10px; }
    .equips-label { font-size: 13px; color: var(--muted); margin-right: 4px; }
    .msg { margin: 0; border-radius: 12px; padding: 10px 14px; font-size: 14px; font-weight: 600; background: var(--warnBg); color: var(--warn); }
    .msg.ok { background: var(--okBg); color: var(--okInk); }
    .body { display: flex; flex-wrap: wrap; gap: 22px; align-items: flex-start; }
    .table-wrap { flex: 999 1 540px; min-width: 0; overflow-x: auto; border: 1px solid var(--line); border-radius: 12px; background: var(--tableBg); }
    table { width: 100%; min-width: 600px; border-collapse: collapse; font-size: 14px; }
    thead th { padding: 12px 14px; font-weight: 500; font-size: 13px; color: var(--muted); text-align: left; border-bottom: 1px solid var(--line); }
    tbody th, tbody td { padding: 11px 14px; border-bottom: 1px solid var(--rowLine); }
    tbody th { text-align: left; }
    tbody tr:hover { background: var(--rowHover); }
    tbody tr.on { background: var(--soft); }
    td.group { color: var(--ink2); white-space: nowrap; }
    td.equip { color: var(--ink2); }
    .pick { background: none; border: 0; padding: 4px 0; font: inherit; font-weight: 600; color: var(--ink); cursor: pointer; text-align: left; }
    .video { display: inline-block; white-space: nowrap; border-radius: 999px; padding: 3px 10px; font-size: 12px; font-weight: 600;
      background: var(--okBg); color: var(--okInk); }
    .video.missing { background: var(--warnBg); color: var(--warn); }
    .empty { margin: 0; padding: 32px; text-align: center; color: var(--muted); }
    aside { flex: 1 1 320px; max-width: 420px; box-sizing: border-box; background: var(--surface); border: 1px solid var(--line); border-radius: 12px;
      padding: 20px; display: flex; flex-direction: column; gap: 14px; }
    .meta { margin: 0; font-size: 13px; color: var(--primary); font-weight: 600; }
    h2 { margin: 2px 0 0; font-family: var(--font-head); font-weight: 800; font-size: 31px; line-height: 1.1; }
    .hidden-badge { display: inline-block; margin-top: 6px; }
    .video-block { display: flex; flex-direction: column; gap: 6px; }
    /* Nền khung video #000 nguyên văn prototype (letterbox của YouTube), không phải màu thương hiệu */
    .frame { position: relative; width: 100%; border-radius: 12px; overflow: hidden; background: #000; aspect-ratio: 16 / 9; }
    .frame.short { aspect-ratio: 9 / 16; max-width: 260px; align-self: center; margin: 0 auto; }
    iframe { position: absolute; inset: 0; width: 100%; height: 100%; border: 0; }
    .yt { align-self: flex-start; font-size: 13px; font-weight: 600; }
    dl { margin: 0; display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 8px; font-size: 12px; }
    dl > div { background: var(--bg); border-radius: 12px; padding: 8px; }
    dt { color: var(--muted); }
    dd { margin: 2px 0 0; font-weight: 600; font-size: 15px; }
    .rx { margin: -6px 0 0; font-size: 12px; color: var(--muted); }
    .notes { display: flex; flex-direction: column; gap: 6px; }
    .notes h3 { margin: 0; font-size: 15px; font-weight: 600; }
    .notes p { margin: 0; font-size: 14px; line-height: 1.6; color: var(--ink2); white-space: pre-line; }
    .box { margin: 0; font-size: 13px; line-height: 1.55; color: var(--muted); background: var(--bg); border-radius: 10px; padding: 10px 12px; }
    .actions { display: flex; flex-wrap: wrap; gap: 8px; }
    .secondary { box-sizing: border-box; display: inline-flex; align-items: center; height: 44px; padding: 0 22px; border-radius: 999px;
      border: 1px solid var(--field); background: var(--surface); color: var(--ink); font: inherit; font-weight: 600; font-size: 14px;
      cursor: pointer; text-decoration: none; }
    .secondary:hover { color: var(--ink); }
    .danger { height: 44px; padding: 0 12px; border: 0; background: transparent; color: var(--alert); font: inherit; font-weight: 600;
      font-size: 14px; text-decoration: underline; cursor: pointer; }
    .secondary:disabled, .danger:disabled { opacity: .6; cursor: default; }
    @media (max-width: 879.98px) { h1 { font-size: 38px; } }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExerciseLibraryComponent implements OnInit {
  /** Query `?bai=<id>`: chọn sẵn bài (sau khi lưu ở form, bỏ ẩn bài trùng, hoặc Huỷ khi sửa). */
  readonly bai = input<string>();

  private readonly api = inject(ExercisesApi);
  private readonly sanitizer = inject(DomSanitizer);
  /** Chống trả lời đến trễ khi coach bấm chip/tìm liên tiếp. */
  private listSeq = 0;
  private detailSeq = 0;

  protected readonly plusIcon = this.sanitizer.bypassSecurityTrustHtml(ICONS.plus);
  protected readonly noVideo = NO_VIDEO;
  protected readonly noNotes = NO_NOTES;

  protected readonly categories = signal<Categories | null>(null);
  /** Bài không ẩn, không lọc — để đếm theo nhóm cơ và dòng tóm tắt. */
  protected readonly all = signal<ExerciseListItem[]>([]);
  protected readonly rows = signal<ExerciseListItem[] | null>(null);
  protected readonly failed = signal(false);
  protected readonly group = signal(ALL);
  protected readonly equipment = signal(ALL);
  protected readonly query = signal('');
  protected readonly selectedId = signal<string | null>(null);
  protected readonly detail = signal<ExerciseDetail | null>(null);
  protected readonly message = signal<{ tone: Tone; text: string } | null>(null);
  protected readonly busy = signal(false);

  protected readonly filtered = computed(() => this.group() !== ALL || this.equipment() !== ALL || this.query() !== '');

  protected readonly groupChips = computed(() => {
    const cats = this.categories();
    if (!cats) return [];
    const all = this.all();
    return [
      { id: ALL, label: `Tất cả (${all.length})` },
      ...cats.muscleGroups.map((g) => ({ id: g.id, label: `${g.name} (${all.filter((x) => x.muscleGroup === g.name).length})` })),
      { id: HIDDEN, label: 'Đã ẩn' },
    ];
  });

  protected readonly equipChips = computed(() => {
    const cats = this.categories();
    return cats ? [{ id: ALL, label: 'Tất cả' }, ...cats.equipment.map((e) => ({ id: e.id, label: e.name }))] : [];
  });

  protected readonly summary = computed(() => {
    const all = this.all();
    const missing = all.filter((x) => !x.hasVideo).length;
    let text = `${all.length} bài`
      + (all.length === 0 ? '.' : missing ? `, ${missing} bài thiếu video mẫu.` : ', bài nào cũng có video mẫu.');
    const rows = this.rows();
    if (this.filtered() && rows) text += ` Đang hiện ${rows.length} bài.`;
    return text;
  });

  protected readonly emptyText = computed(() => {
    if (this.group() === HIDDEN && this.equipment() === ALL && !this.query()) return 'Chưa có bài nào đang ẩn.';
    return this.filtered() ? 'Không tìm thấy bài phù hợp.' : 'Chưa có bài nào trong thư viện. Bấm Thêm bài mới để thêm bài đầu tiên.';
  });

  /** @ADR-f03-thu-vien-bai-tap-005: chỉ nhúng youtube-nocookie khi rút được mã 11 ký tự; còn lại chỉ link. */
  protected readonly video = computed(() => {
    const url = this.detail()?.videoUrl;
    if (!url) return null;
    const id = youTubeId(url);
    return { url, short: isShort(url), embed: id ? this.sanitizer.bypassSecurityTrustResourceUrl(embedUrl(id)) : null };
  });

  /** @ADR-f03-thu-vien-bai-tap-006: hiển thị "3", "10–12", "1–2", "90s"; trống "—". */
  protected readonly dose = computed(() => {
    const d = this.detail();
    if (!d) return { sets: '—', reps: '—', rir: '—', rest: '—', empty: true };
    return {
      sets: formatCount(d.sets),
      reps: formatRange(d.repsMin, d.repsMax),
      rir: formatRange(d.rirMin, d.rirMax),
      rest: formatRest(d.restSeconds),
      empty: [d.sets, d.repsMin, d.repsMax, d.rirMin, d.rirMax, d.restSeconds].every((v) => v === null),
    };
  });

  async ngOnInit(): Promise<void> {
    try {
      this.categories.set(await firstValueFrom(this.api.categories()));
    } catch {
      this.failed.set(true);
      return;
    }
    await this.reload(false, this.bai() ?? null);
  }

  protected pickGroup(id: string): void {
    this.group.set(id);
    void this.reload();
  }

  protected pickEquipment(id: string): void {
    this.equipment.set(id);
    void this.reload();
  }

  protected onSearch(value: string): void {
    const q = value.trim();
    if (q === this.query()) return; // Enter rồi blur cùng từ khoá: không tìm lại
    this.query.set(q);
    void this.reload();
  }

  protected pick(id: string): void {
    this.message.set(null);
    void this.select(id);
  }

  // @BR-f03-thu-vien-bai-tap-015: ẩn / bỏ ẩn; bài vẫn ở aside để coach bỏ ẩn lại ngay
  protected async toggleHidden(d: ExerciseDetail): Promise<void> {
    this.busy.set(true);
    this.message.set(null);
    try {
      const updated = await firstValueFrom(d.isHidden ? this.api.unhide(d.id) : this.api.hide(d.id));
      this.detail.set(updated);
      this.message.set({
        tone: 'ok',
        text: updated.isHidden ? `Đã ẩn bài "${updated.name}". Xem lại ở mục Đã ẩn.` : `Đã bỏ ẩn bài "${updated.name}".`,
      });
      await this.refresh(true);
    } catch (error) {
      this.message.set({ tone: 'warn', text: problemTitle(error, 'Không lưu được. Vui lòng thử lại.') });
    } finally {
      this.busy.set(false);
    }
  }

  // @BR-f03-thu-vien-bai-tap-017: hỏi xác nhận; 409 exercise.in_use → câu của server (gợi ý ẩn)
  protected async remove(d: ExerciseDetail): Promise<void> {
    if (!window.confirm(`Xoá bài "${d.name}" (${d.equipment}) khỏi thư viện? Không hoàn tác được.`)) return;
    this.busy.set(true);
    this.message.set(null);
    try {
      await firstValueFrom(this.api.remove(d.id));
      this.selectedId.set(null);
      this.detail.set(null);
      this.message.set({ tone: 'ok', text: `Đã xoá bài "${d.name}".` });
      await this.refresh(false);
    } catch (error) {
      this.message.set({ tone: 'warn', text: problemTitle(error, 'Không xoá được bài. Vui lòng thử lại.') });
    } finally {
      this.busy.set(false);
    }
  }

  /** Tải lại danh sách đang lọc; khi đang lọc thì tải thêm danh sách đầy đủ để đếm. */
  private async refresh(keepSelection: boolean): Promise<void> {
    await Promise.all([this.reload(keepSelection), this.filtered() ? this.reloadCounts() : Promise.resolve()]);
  }

  private async reloadCounts(): Promise<void> {
    try {
      this.all.set(await firstValueFrom(this.api.list()));
    } catch {
      /* giữ số đếm cũ */
    }
  }

  private async reload(keepSelection = false, prefer: string | null = null): Promise<void> {
    const seq = ++this.listSeq;
    const group = this.group();
    let rows: ExerciseListItem[];
    try {
      rows = await firstValueFrom(this.api.list({
        q: this.query(),
        muscleGroupId: group === ALL || group === HIDDEN ? null : group,
        equipmentId: this.equipment() === ALL ? null : this.equipment(),
        hidden: group === HIDDEN,
      }));
    } catch {
      if (seq === this.listSeq) {
        this.rows.set(null);
        this.failed.set(true);
      }
      return;
    }
    if (seq !== this.listSeq) return;
    this.failed.set(false);
    this.rows.set(rows);
    if (!this.filtered()) this.all.set(rows);
    if (prefer) return this.select(prefer);
    if (keepSelection) return;
    const current = this.selectedId();
    if (current && rows.some((r) => r.id === current)) return;
    await this.select(rows[0]?.id ?? null);
  }

  private async select(id: string | null): Promise<void> {
    this.selectedId.set(id);
    const seq = ++this.detailSeq;
    if (!id) {
      this.detail.set(null);
      return;
    }
    try {
      const d = await firstValueFrom(this.api.get(id));
      if (seq === this.detailSeq) this.detail.set(d);
    } catch {
      if (seq === this.detailSeq) this.detail.set(null);
    }
  }
}
```

- [ ] **Step 8: Chạy — kỳ vọng PASS**

Run: `cd frontend && npm run test:ci`
Expected: PASS — `ExerciseLibraryComponent` 10 test, `AdminShellComponent` 3 test, `dose-format`/`youtube`/`ExercisesApi` xanh; shared/client không đổi; không test nào failed.
Run: `cd frontend && npx ng build admin`
Expected: build thành công, có lazy chunk `exercise-library-component`.

Kiểm tay (compose chạy Task 1–5 → thư viện coach có sẵn 99 bài nạp lúc khởi động, `Seed:ExerciseLibrary` mặc định true): `docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build --wait`, mở `http://localhost:4200/thu-vien/bai-tap` ở **390px** và **1280px**, đặt cạnh `docs/duobexiuuu-coaching-design.html#AdThuVien` cùng độ rộng:
- Rail: mục "Thư viện bài tập" (icon tạ 18px) dưới "Tạo hồ sơ học viên", active nền `rgba(255,92,157,.16)` + vạch trái `#FF5C9D`, `aria-current="page"`.
- 1280px: H1 46px Barlow 800, dòng tóm tắt 14px `--ink2`; nút "Thêm bài mới" 50px góc phải, chữ Barlow 700 20px; ô tìm 48px radius 14 full width; chip nhóm cơ 40px (bật: nền `--primary` chữ trắng 600, không viền) với 11 nhóm theo thứ tự Ngực, Vai, Tay sau, Tay trước, Lưng, Vai sau, Đùi trước, Đùi sau, Mông, Bắp chân, Bụng + "Đã ẩn" cuối; hàng "Dụng cụ" chip 34px/13px sát dưới (margin -10px); bảng và aside nằm cạnh nhau, gap 22px, aside rộng tối đa 420px; dòng chọn nền `--soft`; nhãn "Xem video"/"Thiếu video" 12px 600 bo 999.
- Aside: bài shorts (vd "45° Incline Chest Fly") khung 9:16 rộng 260px giữa; bài watch (vd "Barbell Bench Press") khung 16:9 full width; 4 ô Sets/Reps/RIR/Nghỉ nền `--bg` (vd "3", "10–12", "1", "90s"); không có khối "Đang dùng trong" hay nút "Thêm vào giáo án"; nút "Sửa bài", "Ẩn bài" 44px viền `--field`, "Xoá bài" chữ `--alert` gạch chân.
- 390px: H1 38px; nút "Thêm bài mới" xuống dòng dưới tiêu đề; chip xuống nhiều dòng; bảng cuộn ngang trong khung radius 12 (không tràn ngang trang); aside xuống dưới bảng, full width; 4 ô liều vẫn 1 hàng.
- Gõ "ep nguc"/"incline" + Enter → lọc từ server; chọn "Ngực" + "Dumbbell" → chỉ bài khớp cả hai, dòng tóm tắt thêm "Đang hiện {k} bài."; "Ẩn bài" → bài biến khỏi danh sách, aside giữ bài với nhãn "Đã ẩn" + nút "Bỏ ẩn"; chip "Đã ẩn" thấy bài đó; "Xoá bài" → hộp xác nhận, xoá xong chọn bài đầu.
- Tab qua chip/nút: viền focus 2px `--primary` offset 2px.

- [ ] **Step 9: Commit**
```bash
git add frontend/projects/admin/src/app/exercises frontend/projects/admin/src/app/shell frontend/projects/admin/src/app/app.routes.ts frontend/projects/shared/src/lib/ui/icons.ts
git commit -F - <<'EOF'
feat(admin): exercise library screen with muscle group/equipment chips, search, hidden view and detail aside

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 7: [UI] Thêm / Sửa bài

Màn hình (tech-design §3 "Thiết kế giao diện (UI)"): **Thêm / Sửa bài** `/thu-vien/bai-tap/moi`, `/thu-vien/bai-tap/:id/sua`. Prototype chưa có màn này (*Suy ra*, PO duyệt 2026-11-26) — kiểu form `AdTaoHoSo`, giá trị chép nguyên văn từ extract `.kv/sdd/proto/AdTaoHoSo.md` như `student-form.component.scss` đang dùng: main `padding: 28px clamp(16px, 3vw, 44px) 48px; gap: 22px`, đường dẫn 14px `--muted`, H1 Barlow 800 46px (`< 880px` 38px), card `--surface` viền `--line` radius 12 padding `20px 22px` gap 14, h2 Barlow 700 26px, lưới `repeat(auto-fill, minmax(220px, 1fr))` gap `14px 16px`, nhãn 14px 600 line-height 1.35, ô 46px viền `--field` **radius 14px** (giá trị `FIELD` của prototype; tech-design ghi "radius 10" — prototype là nguồn chuẩn), lỗi ô viền `--alert` + chữ 12px 600 `--alert`, nút chính 50px Barlow 700 20px `--primary`, nút phụ 50px viền `--field` 15px 600, hộp báo `--warnBg`/`--warn` 14px 600 radius 12 padding `10px 14px` + nút "Tải lại" 36px.

Ô theo tech-design: Tên bài *, Nhóm cơ * (chọn), Dụng cụ * (chọn), Link video YouTube, Sets, Reps ("10–12"), RIR ("1–2"), Nghỉ (giây), Hướng dẫn kỹ thuật, Lỗi hay gặp; dưới mỗi ô chọn link "+ Thêm nhóm cơ" / "+ Thêm dụng cụ" mở ô nhập nhỏ tại chỗ (BR-f03-thu-vien-bai-tap-006). Nút "Lưu bài" và "Huỷ". Trùng với bài đang ẩn → câu của server + "Bài đó đang ẩn." + nút "Bỏ ẩn bài đó". 409 phiên bản → "Bài vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất." + "Tải lại". Câu chữ suy ra (PO xem khi duyệt màn): 3 tiêu đề card "Thông tin bài", "Liều mặc định", "Hướng dẫn"; câu dẫn, gợi ý dưới ô, placeholder; lỗi phía FE "Nhập tên bài.", "Chọn nhóm cơ.", "Chọn dụng cụ.", "Nhập một số nguyên, VD: 3.", "Nhập một số hoặc khoảng, VD: 10–12.", "Nhập một số hoặc khoảng, VD: 1–2.", "Nhập số giây, VD: 90.", "Nhập tên mục."; câu chung "Có ô chưa đúng. Sửa các ô được đánh dấu đỏ.". FE chỉ kiểm định dạng (số / khoảng); khoảng hợp lệ (Sets 1–10, Reps 1–100, RIR 0–5, Nghỉ 15–600, ghi chú ≤ 2000, tên ≤ 100, link YouTube) do API kiểm — lỗi 400 gắn vào đúng ô theo `code`, chữ là `title` của server.

Lưu xong (tạo/sửa/bỏ ẩn bài trùng) → `/thu-vien/bai-tap?bai=<id>` (Task 6 chọn sẵn bài đó). "Huỷ": tạo → `/thu-vien/bai-tap`; sửa → `/thu-vien/bai-tap?bai=<id>`.

**Files:**
- Create: `frontend/projects/admin/src/app/exercises/exercise-form.component.ts`, `exercise-form.component.html`, `exercise-form.component.scss`, `exercise-form.component.spec.ts`
- Modify: `frontend/projects/admin/src/app/app.routes.ts`

**Interfaces:**
- Consumes: Task 6 — `ExercisesApi` (`categories`, `get`, `create`, `update`, `unhide`, `addCategory`), `problemCode`, `problemTitle`, `parseRange`, `parseWhole`, `formatRange`, models; `ExerciseLibraryComponent` input `bai`. Task 1–5 — `POST /api/admin/exercises` → `201 ExerciseDetailDto`; `PUT /api/admin/exercises/{id}` (+ `version`) → `200`; `409 { code: "exercise.duplicate", title: "Đã có bài cùng tên và dụng cụ.", hiddenExerciseId?: "<guid>" }` (extension ở gốc ProblemDetails); `409 { code: "exercise.version_conflict", title: "Bài vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất." }`; `400` với `code` `exercise.name_required|name_too_long|muscle_group_required|equipment_required|category_not_found|video_not_youtube|sets_out_of_range|reps_invalid|rir_invalid|rest_out_of_range|notes_too_long`; `POST /api/admin/exercise-categories { kind, name }` → `201 { id, kind, name }`, `409 { code: "category.duplicate", title: "Đã có mục cùng tên." }`, `400 category.name_required`.
- Produces: `ExerciseFormComponent` (inputs `mode: 'create' | 'edit'`, `id`), hằng `VERSION_CONFLICT`, `FIX_FIELDS`; route `thu-vien/bai-tap/moi` (`data.mode = 'create'`), `thu-vien/bai-tap/:id/sua` (`data.mode = 'edit'`); selector cho Task 8: H1 "Thêm bài mới"/"Sửa bài", nhãn "Tên bài", "Nhóm cơ", "Dụng cụ" (select theo nhãn mục), nút "Lưu bài"; sau lưu URL `/thu-vien/bai-tap?bai=<guid>`.

**Owns:** —

- [ ] **Step 1: Viết spec form**

`frontend/projects/admin/src/app/exercises/exercise-form.component.spec.ts`:
```ts
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { APP_CONFIG, CLIENT_APP } from '@coaching/shared';
import { describe, expect, it, vi } from 'vitest';
import { ExerciseFormComponent, FIX_FIELDS, VERSION_CONFLICT } from './exercise-form.component';
import { Categories, ExerciseDetail } from './exercise.models';

const settle = () => new Promise((resolve) => setTimeout(resolve, 0));

const CATS: Categories = {
  muscleGroups: ['Ngực', 'Vai', 'Tay sau', 'Tay trước', 'Lưng', 'Vai sau', 'Đùi trước', 'Đùi sau', 'Mông', 'Bắp chân', 'Bụng']
    .map((name, i) => ({ id: `g${i}`, name })),
  equipment: ['Machine', 'Cable', 'Dumbbell', 'Barbell', 'Smith Machine', 'Kettlebell', 'Bodyweight']
    .map((name, i) => ({ id: `e${i}`, name })),
};

function detail(overrides: Partial<ExerciseDetail> = {}): ExerciseDetail {
  return {
    id: 'x1', name: 'Lateral Raise', muscleGroupId: 'g1', muscleGroup: 'Vai', equipmentId: 'e1', equipment: 'Cable',
    videoUrl: 'https://youtube.com/shorts/lMJUXEvcMkQ', hasVideo: true, sets: 3, repsMin: 10, repsMax: 12, rirMin: 1, rirMax: 2,
    restSeconds: 90, techniqueNotes: 'Giữ khuỷu hơi cong', commonMistakes: null, isHidden: false, version: 'AAAAAAAAB9E=',
    ...overrides,
  };
}

describe('ExerciseFormComponent', () => {
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
    const fixture = TestBed.createComponent(ExerciseFormComponent);
    fixture.componentRef.setInput('mode', mode);
    if (id) fixture.componentRef.setInput('id', id);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const http = TestBed.inject(HttpTestingController);
    const render = async () => {
      await settle();
      fixture.detectChanges();
    };
    const fill = (selector: string, value: string) => {
      const c = el.querySelector(selector) as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;
      c.value = value;
      c.dispatchEvent(new Event(c instanceof HTMLSelectElement ? 'change' : 'input'));
    };
    const value = (selector: string) => (el.querySelector(selector) as HTMLInputElement).value;
    const button = (text: string) => [...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === text) as HTMLButtonElement | undefined;
    const save = () => {
      el.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
      fixture.detectChanges();
    };
    const fillValid = () => {
      fill('#ex-name', 'Lateral Raise');
      fill('#ex-muscle', 'g1');
      fill('#ex-equipment', 'e2');
    };
    return { fixture, el, http, render, fill, value, button, save, fillValid, navigate };
  }

  async function created() {
    const s = setup('create');
    s.http.expectOne('/api/admin/exercise-categories').flush(CATS);
    await s.render();
    return s;
  }

  async function edited() {
    const s = setup('edit', 'x1');
    s.http.expectOne('/api/admin/exercise-categories').flush(CATS);
    s.http.expectOne('/api/admin/exercises/x1').flush(detail());
    await s.render();
    return s;
  }

  // @ADR-f03-thu-vien-bai-tap-006 @BR-f03-thu-vien-bai-tap-001
  it('creates an exercise from the typed dose text and opens it in the library', async () => {
    const { el, http, render, fill, save, navigate } = await created();

    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Thêm bài mới');
    expect(el.querySelector('.crumbs a')?.getAttribute('href')).toBe('/thu-vien/bai-tap');
    expect([...el.querySelectorAll('#ex-muscle option')].map((o) => o.textContent?.trim()))
      .toEqual(['Chọn…', ...CATS.muscleGroups.map((g) => g.name)]);
    expect([...el.querySelectorAll('#ex-equipment option')].map((o) => o.textContent?.trim()))
      .toEqual(['Chọn…', ...CATS.equipment.map((e) => e.name)]);
    expect([...el.querySelectorAll('label')].map((l) => l.textContent?.replace(' *', '').trim())).toEqual([
      'Tên bài', 'Nhóm cơ', 'Dụng cụ', 'Link video YouTube', 'Sets', 'Reps', 'RIR', 'Nghỉ', 'Hướng dẫn kỹ thuật', 'Lỗi hay gặp',
    ]);

    fill('#ex-name', '  Lateral Raise ');
    fill('#ex-muscle', 'g1');
    fill('#ex-equipment', 'e2');
    fill('#ex-video', 'https://youtube.com/shorts/lMJUXEvcMkQ');
    fill('#ex-sets', '3');
    fill('#ex-reps', '12-15');
    fill('#ex-rir', '0–1');
    fill('#ex-rest', '60');
    fill('#ex-technique', 'Giữ khuỷu hơi cong, kéo bằng vai');
    save();

    const req = http.expectOne({ method: 'POST', url: '/api/admin/exercises' });
    expect(req.request.body).toEqual({
      name: 'Lateral Raise', muscleGroupId: 'g1', equipmentId: 'e2', videoUrl: 'https://youtube.com/shorts/lMJUXEvcMkQ',
      sets: 3, repsMin: 12, repsMax: 15, rirMin: 0, rirMax: 1, restSeconds: 60,
      techniqueNotes: 'Giữ khuỷu hơi cong, kéo bằng vai', commonMistakes: null,
    });
    req.flush(detail({ id: 'new1' }), { status: 201, statusText: 'Created' });
    await render();

    expect(navigate).toHaveBeenCalledWith('/thu-vien/bai-tap?bai=new1');
  });

  // @BR-f03-thu-vien-bai-tap-002 @BR-f03-thu-vien-bai-tap-003 @BR-f03-thu-vien-bai-tap-004
  it('marks missing name, muscle group, equipment and an unreadable dose without calling the API', async () => {
    const { el, http, fill, save } = await created();

    fill('#ex-name', '   ');
    fill('#ex-reps', 'abc');
    fill('#ex-rest', '90 giây');
    save();

    http.expectNone({ method: 'POST', url: '/api/admin/exercises' });
    expect([...el.querySelectorAll('.field-err')].map((e) => e.textContent?.trim())).toEqual([
      'Nhập tên bài.', 'Chọn nhóm cơ.', 'Chọn dụng cụ.', 'Nhập một số hoặc khoảng, VD: 10–12.', 'Nhập số giây, VD: 90.',
    ]);
    expect(el.querySelector('#ex-name')?.getAttribute('aria-invalid')).toBe('true');
    expect(el.querySelector('#ex-name')?.classList.contains('err')).toBe(true);
    expect(el.querySelector('.msg')?.textContent?.trim()).toBe(FIX_FIELDS);

    fill('#ex-name', 'Lateral Raise');
    expect(el.querySelector('#ex-name')?.classList.contains('err')).toBe(false);
  });

  // @BR-f03-thu-vien-bai-tap-010: khoảng do API kiểm, lỗi gắn vào đúng ô
  it('puts the server message under the field named by the error code', async () => {
    const { el, http, render, fill, save, fillValid } = await created();
    fillValid();
    fill('#ex-reps', '12–10');
    save();

    const req = http.expectOne({ method: 'POST', url: '/api/admin/exercises' });
    expect(req.request.body.repsMin).toBe(12);
    expect(req.request.body.repsMax).toBe(10);
    req.flush({ code: 'exercise.reps_invalid', title: 'Reps là một số hoặc khoảng thấp–cao trong 1–100.' },
      { status: 400, statusText: 'Bad Request' });
    await render();

    const reps = el.querySelector('#ex-reps')!.closest('.field')!;
    expect(reps.querySelector('.field-err')?.textContent?.trim()).toBe('Reps là một số hoặc khoảng thấp–cao trong 1–100.');
    expect(el.querySelector('#ex-reps')?.getAttribute('aria-invalid')).toBe('true');
    expect(el.querySelector('.msg')?.textContent?.trim()).toBe(FIX_FIELDS);
  });

  // @BR-f03-thu-vien-bai-tap-001: trùng bài đang ẩn → gợi ý bỏ ẩn bài đó
  it('offers to unhide the hidden duplicate and opens it after unhiding', async () => {
    const { el, http, render, save, fillValid, button, navigate } = await created();
    fillValid();
    save();
    http.expectOne({ method: 'POST', url: '/api/admin/exercises' }).flush(
      { code: 'exercise.duplicate', title: 'Đã có bài cùng tên và dụng cụ.', hiddenExerciseId: 'h1' },
      { status: 409, statusText: 'Conflict' });
    await render();

    expect(el.querySelector('.msg')?.textContent).toContain('Đã có bài cùng tên và dụng cụ. Bài đó đang ẩn.');
    button('Bỏ ẩn bài đó')!.click();
    http.expectOne({ method: 'POST', url: '/api/admin/exercises/h1/unhide' }).flush(detail({ id: 'h1' }));
    await render();

    expect(navigate).toHaveBeenCalledWith('/thu-vien/bai-tap?bai=h1');
  });

  // @BR-f03-thu-vien-bai-tap-001
  it('shows the duplicate message without an unhide action when the duplicate is visible', async () => {
    const { el, http, render, save, fillValid, button } = await created();
    fillValid();
    save();
    http.expectOne({ method: 'POST', url: '/api/admin/exercises' }).flush(
      { code: 'exercise.duplicate', title: 'Đã có bài cùng tên và dụng cụ.' }, { status: 409, statusText: 'Conflict' });
    await render();

    expect(el.querySelector('.msg')?.textContent?.trim()).toBe('Đã có bài cùng tên và dụng cụ.');
    expect(button('Bỏ ẩn bài đó')).toBeUndefined();
  });

  // @BR-f03-thu-vien-bai-tap-020
  it('loads the exercise into the form and saves it with its version', async () => {
    const { el, http, render, fill, value, save, navigate } = await edited();

    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Sửa bài');
    expect(value('#ex-name')).toBe('Lateral Raise');
    expect(value('#ex-muscle')).toBe('g1');
    expect(value('#ex-equipment')).toBe('e1');
    expect(value('#ex-video')).toBe('https://youtube.com/shorts/lMJUXEvcMkQ');
    expect(value('#ex-sets')).toBe('3');
    expect(value('#ex-reps')).toBe('10–12');
    expect(value('#ex-rir')).toBe('1–2');
    expect(value('#ex-rest')).toBe('90');
    expect(value('#ex-technique')).toBe('Giữ khuỷu hơi cong');
    expect(value('#ex-mistakes')).toBe('');
    expect(el.querySelector('a.cancel')?.getAttribute('href')).toBe('/thu-vien/bai-tap?bai=x1');

    fill('#ex-mistakes', 'Nhún vai khi kéo');
    save();
    const req = http.expectOne({ method: 'PUT', url: '/api/admin/exercises/x1' });
    expect(req.request.body).toEqual({
      name: 'Lateral Raise', muscleGroupId: 'g1', equipmentId: 'e1', videoUrl: 'https://youtube.com/shorts/lMJUXEvcMkQ',
      sets: 3, repsMin: 10, repsMax: 12, rirMin: 1, rirMax: 2, restSeconds: 90,
      techniqueNotes: 'Giữ khuỷu hơi cong', commonMistakes: 'Nhún vai khi kéo', version: 'AAAAAAAAB9E=',
    });
    req.flush(detail({ commonMistakes: 'Nhún vai khi kéo', version: 'AAAAAAAAB9I=' }));
    await render();

    expect(navigate).toHaveBeenCalledWith('/thu-vien/bai-tap?bai=x1');
  });

  // @BR-f03-thu-vien-bai-tap-020
  it('reports a version conflict and reloads the latest exercise on "Tải lại"', async () => {
    const { el, http, render, fill, value, save, button } = await edited();
    fill('#ex-name', 'Lateral Raise (bản của tôi)');
    save();
    http.expectOne({ method: 'PUT', url: '/api/admin/exercises/x1' }).flush(
      { code: 'exercise.version_conflict', title: VERSION_CONFLICT }, { status: 409, statusText: 'Conflict' });
    await render();

    expect(el.querySelector('.msg')?.textContent).toContain('Bài vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.');
    button('Tải lại')!.click();
    http.expectOne('/api/admin/exercises/x1').flush(detail({ name: 'Cable Lateral Raise', version: 'AAAAAAAAB9I=' }));
    await render();

    expect(value('#ex-name')).toBe('Cable Lateral Raise');
    expect(el.querySelector('.msg')).toBeNull();
  });

  // @BR-f03-thu-vien-bai-tap-006
  it('adds a muscle group inline and selects it', async () => {
    const { el, http, render, fill, button } = await created();

    button('+ Thêm nhóm cơ')!.click();
    await render();
    expect(el.querySelector('label[for="new-MuscleGroup"]')?.textContent?.trim()).toBe('Tên nhóm cơ mới');
    fill('#new-MuscleGroup', 'Cẳng tay');
    button('Thêm')!.click();

    const req = http.expectOne({ method: 'POST', url: '/api/admin/exercise-categories' });
    expect(req.request.body).toEqual({ kind: 'MuscleGroup', name: 'Cẳng tay' });
    req.flush({ id: 'g11', kind: 'MuscleGroup', name: 'Cẳng tay' }, { status: 201, statusText: 'Created' });
    await render();

    expect(el.querySelector('#new-MuscleGroup')).toBeNull();
    const select = el.querySelector('#ex-muscle') as HTMLSelectElement;
    expect(select.value).toBe('g11');
    expect(select.selectedOptions[0].textContent?.trim()).toBe('Cẳng tay');
    expect(button('+ Thêm nhóm cơ')).toBeDefined();
  });

  // @BR-f03-thu-vien-bai-tap-006
  it('keeps the inline box open with the server message when the category already exists', async () => {
    const { el, http, render, fill, button } = await created();

    button('+ Thêm dụng cụ')!.click();
    await render();
    button('Thêm')!.click();
    await render();
    expect(el.querySelector('#new-Equipment')!.closest('.field')!.textContent).toContain('Nhập tên mục.');
    http.expectNone({ method: 'POST', url: '/api/admin/exercise-categories' });

    fill('#new-Equipment', 'cable');
    button('Thêm')!.click();
    http.expectOne({ method: 'POST', url: '/api/admin/exercise-categories' }).flush(
      { code: 'category.duplicate', title: 'Đã có mục cùng tên.' }, { status: 409, statusText: 'Conflict' });
    await render();

    expect(el.querySelector('#new-Equipment')).not.toBeNull();
    expect(el.querySelector('#new-Equipment')!.closest('.field')!.textContent).toContain('Đã có mục cùng tên.');
  });

  it('shows a load error instead of the form when the exercise cannot be loaded', async () => {
    const { el, http, render } = setup('edit', 'zz');
    http.expectOne('/api/admin/exercise-categories').flush(CATS);
    http.expectOne('/api/admin/exercises/zz').flush({ code: 'exercise.not_found', title: 'Không tìm thấy bài.' }, { status: 404, statusText: 'Not Found' });
    await render();

    expect(el.querySelector('form')).toBeNull();
    expect(el.querySelector('.msg')?.textContent?.trim()).toBe('Không tải được bài tập. Vui lòng tải lại trang.');
  });
});
```

- [ ] **Step 2: Chạy — kỳ vọng FAIL**

Run: `cd frontend && npx ng test admin --watch=false`
Expected: FAIL — `Cannot find module './exercise-form.component'`.

- [ ] **Step 3: Viết form + route**

`frontend/projects/admin/src/app/exercises/exercise-form.component.ts`:
```ts
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { formatRange, parseRange, parseWhole } from './dose-format';
import { Categories, CategoryKind, ExerciseDetail, SaveExerciseBody } from './exercise.models';
import { ExercisesApi, problemCode, problemTitle } from './exercises.api';

/** Câu 409 phiên bản nguyên văn tech-design (@BR-f03-thu-vien-bai-tap-020). */
export const VERSION_CONFLICT = 'Bài vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.';
export const FIX_FIELDS = 'Có ô chưa đúng. Sửa các ô được đánh dấu đỏ.';

type FieldKey = 'name' | 'muscleGroupId' | 'equipmentId' | 'videoUrl' | 'sets' | 'reps' | 'rir' | 'rest' | 'techniqueNotes' | 'commonMistakes';
type Values = Record<FieldKey, string>;
type Errors = Partial<Record<FieldKey, string>>;

const EMPTY: Values = {
  name: '', muscleGroupId: '', equipmentId: '', videoUrl: '', sets: '', reps: '', rir: '', rest: '', techniqueNotes: '', commonMistakes: '',
};

/** Lỗi 400 của API → ô hiển thị `title` (BR-002…004, 007, 009…014). */
const FIELD_BY_CODE: Record<string, FieldKey> = {
  'exercise.name_required': 'name',
  'exercise.name_too_long': 'name',
  'exercise.muscle_group_required': 'muscleGroupId',
  'exercise.equipment_required': 'equipmentId',
  'exercise.video_not_youtube': 'videoUrl',
  'exercise.sets_out_of_range': 'sets',
  'exercise.reps_invalid': 'reps',
  'exercise.rir_invalid': 'rir',
  'exercise.rest_out_of_range': 'rest',
};

/**
 * Thêm bài (thu-vien/bai-tap/moi) / Sửa bài (thu-vien/bai-tap/:id/sua) — kiểu form AdTaoHoSo (prototype chưa có màn này).
 * @ADR-f03-thu-vien-bai-tap-006: liều nhập chữ "10–12", gửi API min/max.
 */
@Component({
  selector: 'app-exercise-form',
  imports: [RouterLink],
  templateUrl: './exercise-form.component.html',
  styleUrl: './exercise-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExerciseFormComponent implements OnInit {
  /** Route data: 'create' | 'edit'. */
  readonly mode = input<'create' | 'edit'>('create');
  /** Route param `:id` (chế độ sửa). */
  readonly id = input<string>();

  private readonly api = inject(ExercisesApi);
  private readonly router = inject(Router);

  protected readonly values = signal<Values>({ ...EMPTY });
  protected readonly errors = signal<Errors>({});
  protected readonly categories = signal<Categories | null>(null);
  protected readonly loaded = signal<ExerciseDetail | null>(null);
  protected readonly loadFailed = signal(false);
  protected readonly saving = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly conflict = signal(false);
  protected readonly hiddenDuplicateId = signal<string | null>(null);
  protected readonly adding = signal<CategoryKind | null>(null);
  protected readonly newCategory = signal('');
  protected readonly categoryError = signal<string | null>(null);
  protected readonly versionConflict = VERSION_CONFLICT;

  protected readonly isEdit = computed(() => this.mode() === 'edit');
  protected readonly title = computed(() => (this.isEdit() ? 'Sửa bài' : 'Thêm bài mới'));
  protected readonly cancelQuery = computed(() => (this.isEdit() && this.id() ? { bai: this.id() } : {}));

  async ngOnInit(): Promise<void> {
    try {
      const [cats, current] = await Promise.all([
        firstValueFrom(this.api.categories()),
        this.isEdit() ? firstValueFrom(this.api.get(this.id()!)) : Promise.resolve(null),
      ]);
      this.categories.set(cats);
      if (current) this.fill(current);
    } catch {
      this.loadFailed.set(true);
    }
  }

  protected setValue(key: FieldKey, value: string): void {
    this.values.set({ ...this.values(), [key]: value });
    if (this.errors()[key]) {
      const rest = { ...this.errors() };
      delete rest[key];
      this.errors.set(rest);
    }
  }

  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    if (this.saving() || (this.isEdit() && !this.loaded())) return;
    const body = this.toBody();
    if (!body) return;
    this.saving.set(true);
    this.message.set(null);
    this.conflict.set(false);
    this.hiddenDuplicateId.set(null);
    try {
      const current = this.loaded();
      const saved = await firstValueFrom(current
        ? this.api.update(current.id, { ...body, version: current.version })
        : this.api.create(body));
      await this.router.navigateByUrl(`/thu-vien/bai-tap?bai=${saved.id}`);
    } catch (error) {
      this.showError(error);
    } finally {
      this.saving.set(false);
    }
  }

  // @BR-f03-thu-vien-bai-tap-001: trùng bài đang ẩn → bỏ ẩn bài đó rồi mở nó trong thư viện
  protected async unhideDuplicate(): Promise<void> {
    const id = this.hiddenDuplicateId();
    if (!id || this.saving()) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.unhide(id));
      await this.router.navigateByUrl(`/thu-vien/bai-tap?bai=${id}`);
    } catch (error) {
      this.hiddenDuplicateId.set(null);
      this.message.set(problemTitle(error, 'Không bỏ ẩn được bài. Vui lòng thử lại.'));
    } finally {
      this.saving.set(false);
    }
  }

  // @BR-f03-thu-vien-bai-tap-020: tải bản mới nhất (bỏ phần đang sửa)
  protected async reload(): Promise<void> {
    try {
      this.fill(await firstValueFrom(this.api.get(this.id()!)));
      this.conflict.set(false);
      this.message.set(null);
    } catch {
      this.message.set('Không tải được bài. Vui lòng thử lại.');
    }
  }

  protected openAdd(kind: CategoryKind): void {
    this.adding.set(kind);
    this.newCategory.set('');
    this.categoryError.set(null);
  }

  protected cancelAdd(): void {
    this.adding.set(null);
    this.categoryError.set(null);
  }

  // @BR-f03-thu-vien-bai-tap-006: thêm mục mới tại chỗ, chọn luôn cho bài đang soạn
  protected async addCategory(): Promise<void> {
    const kind = this.adding();
    const name = this.newCategory().trim();
    if (!kind) return;
    if (!name) {
      this.categoryError.set('Nhập tên mục.');
      return;
    }
    this.categoryError.set(null);
    try {
      const created = await firstValueFrom(this.api.addCategory(kind, name));
      const cats = this.categories() ?? { muscleGroups: [], equipment: [] };
      const item = { id: created.id, name: created.name };
      this.categories.set(kind === 'MuscleGroup'
        ? { ...cats, muscleGroups: [...cats.muscleGroups, item] }
        : { ...cats, equipment: [...cats.equipment, item] });
      this.setValue(kind === 'MuscleGroup' ? 'muscleGroupId' : 'equipmentId', created.id);
      this.adding.set(null);
    } catch (error) {
      this.categoryError.set(problemTitle(error, 'Không thêm được mục. Vui lòng thử lại.'));
    }
  }

  private fill(d: ExerciseDetail): void {
    this.loaded.set(d);
    this.values.set({
      name: d.name,
      muscleGroupId: d.muscleGroupId,
      equipmentId: d.equipmentId,
      videoUrl: d.videoUrl ?? '',
      sets: d.sets === null ? '' : `${d.sets}`,
      reps: formatRange(d.repsMin, d.repsMax, ''),
      rir: formatRange(d.rirMin, d.rirMax, ''),
      rest: d.restSeconds === null ? '' : `${d.restSeconds}`,
      techniqueNotes: d.techniqueNotes ?? '',
      commonMistakes: d.commonMistakes ?? '',
    });
    this.errors.set({});
  }

  /** FE chỉ kiểm bắt buộc + định dạng; khoảng hợp lệ do Domain kiểm (BR-009…014). */
  private toBody(): SaveExerciseBody | null {
    const v = this.values();
    const errors: Errors = {};
    const name = v.name.trim();
    if (!name) errors.name = 'Nhập tên bài.';
    if (!v.muscleGroupId) errors.muscleGroupId = 'Chọn nhóm cơ.';
    if (!v.equipmentId) errors.equipmentId = 'Chọn dụng cụ.';
    const sets = parseWhole(v.sets);
    if (sets === 'invalid') errors.sets = 'Nhập một số nguyên, VD: 3.';
    const reps = parseRange(v.reps);
    if (reps === 'invalid') errors.reps = 'Nhập một số hoặc khoảng, VD: 10–12.';
    const rir = parseRange(v.rir);
    if (rir === 'invalid') errors.rir = 'Nhập một số hoặc khoảng, VD: 1–2.';
    const rest = parseWhole(v.rest);
    if (rest === 'invalid') errors.rest = 'Nhập số giây, VD: 90.';
    this.errors.set(errors);
    if (sets === 'invalid' || reps === 'invalid' || rir === 'invalid' || rest === 'invalid' || Object.keys(errors).length > 0) {
      this.message.set(FIX_FIELDS);
      this.hiddenDuplicateId.set(null);
      this.conflict.set(false);
      return null;
    }
    return {
      name,
      muscleGroupId: v.muscleGroupId,
      equipmentId: v.equipmentId,
      videoUrl: v.videoUrl.trim() || null,
      sets,
      repsMin: reps.min,
      repsMax: reps.max,
      rirMin: rir.min,
      rirMax: rir.max,
      restSeconds: rest,
      techniqueNotes: v.techniqueNotes.trim() || null,
      commonMistakes: v.commonMistakes.trim() || null,
    };
  }

  private showError(error: unknown): void {
    const code = problemCode(error);
    if (code === 'exercise.version_conflict') {
      this.conflict.set(true);
      return;
    }
    const title = problemTitle(error, 'Không lưu được bài. Vui lòng thử lại.');
    if (code === 'exercise.duplicate') {
      const hidden = (error as { error?: { hiddenExerciseId?: unknown } }).error?.hiddenExerciseId;
      this.hiddenDuplicateId.set(typeof hidden === 'string' ? hidden : null);
      this.message.set(typeof hidden === 'string' ? `${title} Bài đó đang ẩn.` : title);
      return;
    }
    const field = code ? FIELD_BY_CODE[code] : undefined;
    if (field) {
      this.errors.set({ ...this.errors(), [field]: title });
      this.message.set(FIX_FIELDS);
      return;
    }
    this.message.set(title);
  }
}
```

`frontend/projects/admin/src/app/exercises/exercise-form.component.html`:
```html
<nav aria-label="Đường dẫn" class="crumbs">
  <a routerLink="/thu-vien/bai-tap">Thư viện bài tập</a><span aria-hidden="true">/</span>
  <span class="here">{{ title() }}</span>
</nav>
<h1>{{ title() }}</h1>

@if (loadFailed()) {
  <p class="msg" role="status">Không tải được bài tập. Vui lòng tải lại trang.</p>
} @else {
  <form novalidate (submit)="submit($event)">
    <p class="intro">Mục có dấu <span class="star">*</span> là bắt buộc. Mỗi bài là duy nhất theo tên và dụng cụ.</p>

    <section class="card" aria-labelledby="sec-info">
      <h2 id="sec-info">Thông tin bài</h2>
      <div class="grid">
        <div class="field wide">
          <label for="ex-name">Tên bài<span aria-hidden="true" class="req"> *</span></label>
          <input class="ctl" id="ex-name" type="text" placeholder="VD: Lateral Raise" [value]="values().name"
                 [class.err]="!!errors().name" [attr.aria-invalid]="!!errors().name"
                 (input)="setValue('name', $any($event.target).value)" />
          @if (errors().name; as e) {
            <span class="field-err">{{ e }}</span>
          }
        </div>

        <div class="field">
          <label for="ex-muscle">Nhóm cơ<span aria-hidden="true" class="req"> *</span></label>
          <select class="ctl" id="ex-muscle" [class.err]="!!errors().muscleGroupId" [attr.aria-invalid]="!!errors().muscleGroupId"
                  (change)="setValue('muscleGroupId', $any($event.target).value)">
            <option value="">Chọn…</option>
            @for (g of categories()?.muscleGroups ?? []; track g.id) {
              <option [value]="g.id" [selected]="values().muscleGroupId === g.id">{{ g.name }}</option>
            }
          </select>
          @if (errors().muscleGroupId; as e) {
            <span class="field-err">{{ e }}</span>
          }
          @if (adding() === 'MuscleGroup') {
            <div class="add-row">
              <label for="new-MuscleGroup" class="sr-only">Tên nhóm cơ mới</label>
              <input class="ctl" id="new-MuscleGroup" type="text" placeholder="VD: Cẳng tay" [value]="newCategory()"
                     (input)="newCategory.set($any($event.target).value)" (keydown.enter)="$event.preventDefault(); addCategory()" />
              <button type="button" class="small-btn" (click)="addCategory()">Thêm</button>
              <button type="button" class="link" (click)="cancelAdd()">Huỷ</button>
            </div>
            @if (categoryError(); as e) {
              <span class="field-err">{{ e }}</span>
            }
          } @else {
            <button type="button" class="add-link" (click)="openAdd('MuscleGroup')">+ Thêm nhóm cơ</button>
          }
        </div>

        <div class="field">
          <label for="ex-equipment">Dụng cụ<span aria-hidden="true" class="req"> *</span></label>
          <select class="ctl" id="ex-equipment" [class.err]="!!errors().equipmentId" [attr.aria-invalid]="!!errors().equipmentId"
                  (change)="setValue('equipmentId', $any($event.target).value)">
            <option value="">Chọn…</option>
            @for (q of categories()?.equipment ?? []; track q.id) {
              <option [value]="q.id" [selected]="values().equipmentId === q.id">{{ q.name }}</option>
            }
          </select>
          @if (errors().equipmentId; as e) {
            <span class="field-err">{{ e }}</span>
          }
          @if (adding() === 'Equipment') {
            <div class="add-row">
              <label for="new-Equipment" class="sr-only">Tên dụng cụ mới</label>
              <input class="ctl" id="new-Equipment" type="text" placeholder="VD: Resistance Band" [value]="newCategory()"
                     (input)="newCategory.set($any($event.target).value)" (keydown.enter)="$event.preventDefault(); addCategory()" />
              <button type="button" class="small-btn" (click)="addCategory()">Thêm</button>
              <button type="button" class="link" (click)="cancelAdd()">Huỷ</button>
            </div>
            @if (categoryError(); as e) {
              <span class="field-err">{{ e }}</span>
            }
          } @else {
            <button type="button" class="add-link" (click)="openAdd('Equipment')">+ Thêm dụng cụ</button>
          }
        </div>

        <div class="field wide">
          <label for="ex-video">Link video YouTube</label>
          <input class="ctl" id="ex-video" type="text" inputmode="url" placeholder="https://www.youtube.com/watch?v=…" [value]="values().videoUrl"
                 [class.err]="!!errors().videoUrl" [attr.aria-invalid]="!!errors().videoUrl"
                 (input)="setValue('videoUrl', $any($event.target).value)" />
          <span class="note">Link youtube.com hoặc youtu.be. Để trống nếu chưa có video.</span>
          @if (errors().videoUrl; as e) {
            <span class="field-err">{{ e }}</span>
          }
        </div>
      </div>
    </section>

    <section class="card" aria-labelledby="sec-dose">
      <h2 id="sec-dose">Liều mặc định</h2>
      <p class="hint">Gợi ý khi thêm bài vào giáo án, sửa được theo từng học viên. Để trống nếu chưa có.</p>
      <div class="grid">
        <div class="field">
          <label for="ex-sets">Sets</label>
          <input class="ctl" id="ex-sets" type="text" inputmode="numeric" placeholder="VD: 3" [value]="values().sets"
                 [class.err]="!!errors().sets" [attr.aria-invalid]="!!errors().sets"
                 (input)="setValue('sets', $any($event.target).value)" />
          @if (errors().sets; as e) {
            <span class="field-err">{{ e }}</span>
          }
        </div>
        <div class="field">
          <label for="ex-reps">Reps</label>
          <input class="ctl" id="ex-reps" type="text" placeholder="VD: 10–12" [value]="values().reps"
                 [class.err]="!!errors().reps" [attr.aria-invalid]="!!errors().reps"
                 (input)="setValue('reps', $any($event.target).value)" />
          @if (errors().reps; as e) {
            <span class="field-err">{{ e }}</span>
          }
        </div>
        <div class="field">
          <label for="ex-rir">RIR</label>
          <input class="ctl" id="ex-rir" type="text" placeholder="VD: 1–2" [value]="values().rir"
                 [class.err]="!!errors().rir" [attr.aria-invalid]="!!errors().rir"
                 (input)="setValue('rir', $any($event.target).value)" />
          @if (errors().rir; as e) {
            <span class="field-err">{{ e }}</span>
          }
        </div>
        <div class="field">
          <label for="ex-rest">Nghỉ</label>
          <div class="with-unit">
            <input class="ctl" id="ex-rest" type="text" inputmode="numeric" placeholder="VD: 90" [value]="values().rest"
                   [class.err]="!!errors().rest" [attr.aria-invalid]="!!errors().rest"
                   (input)="setValue('rest', $any($event.target).value)" />
            <span class="unit">giây</span>
          </div>
          @if (errors().rest; as e) {
            <span class="field-err">{{ e }}</span>
          }
        </div>
      </div>
    </section>

    <section class="card" aria-labelledby="sec-guide">
      <h2 id="sec-guide">Hướng dẫn</h2>
      <div class="grid">
        <div class="field wide">
          <label for="ex-technique">Hướng dẫn kỹ thuật</label>
          <textarea class="ctl" id="ex-technique" rows="4" placeholder="VD: Giữ khuỷu hơi cong, kéo bằng vai" [value]="values().techniqueNotes"
                    (input)="setValue('techniqueNotes', $any($event.target).value)"></textarea>
        </div>
        <div class="field wide">
          <label for="ex-mistakes">Lỗi hay gặp</label>
          <textarea class="ctl" id="ex-mistakes" rows="3" placeholder="VD: Nhún vai khi kéo" [value]="values().commonMistakes"
                    (input)="setValue('commonMistakes', $any($event.target).value)"></textarea>
        </div>
      </div>
    </section>

    @if (conflict()) {
      <p class="msg" role="status">{{ versionConflict }}<button type="button" class="reload" (click)="reload()">Tải lại</button></p>
    } @else if (message(); as m) {
      <p class="msg" role="status">{{ m }}@if (hiddenDuplicateId()) {<button type="button" class="reload" [disabled]="saving()" (click)="unhideDuplicate()">Bỏ ẩn bài đó</button>}</p>
    }

    <div class="actions">
      <button type="submit" class="primary" [disabled]="saving()">Lưu bài</button>
      <a class="cancel" routerLink="/thu-vien/bai-tap" [queryParams]="cancelQuery()">Huỷ</a>
    </div>
  </form>
}
```

`frontend/projects/admin/src/app/exercises/exercise-form.component.scss`:
```scss
// Kiểu form AdTaoHoSo (giá trị nguyên văn như student-form.component.scss); prototype chưa có màn Thêm/Sửa bài
:host { display: flex; flex-direction: column; gap: 22px; box-sizing: border-box; padding: 28px clamp(16px, 3vw, 44px) 48px; }
.crumbs { font-size: 14px; color: var(--muted); display: flex; gap: 8px; }
.crumbs a { color: var(--muted); }
.crumbs .here { color: var(--ink); }
h1 { margin: 0; font-family: var(--font-head); font-weight: 800; font-size: 46px; line-height: 1.05; }
form { max-width: 760px; min-width: 0; display: flex; flex-direction: column; gap: 16px; }
.intro { margin: 0; font-size: 14px; color: var(--muted); }
.star { color: var(--primary); font-weight: 700; }
.card { background: var(--surface); border: 1px solid var(--line); border-radius: 12px; padding: 20px 22px; display: flex; flex-direction: column; gap: 14px; }
.card h2 { margin: 0; font-family: var(--font-head); font-weight: 700; font-size: 26px; line-height: 1.1; }
.hint { margin: -6px 0 0; font-size: 13px; color: var(--muted); }
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 14px 16px; }
.field { display: flex; flex-direction: column; gap: 6px; min-width: 0; }
.field.wide { grid-column: 1 / -1; }
label { font-size: 14px; font-weight: 600; line-height: 1.35; }
.req { color: var(--primary); }
.sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
.ctl { box-sizing: border-box; width: 100%; border-radius: 14px; background: var(--surface); color: var(--ink); font: inherit; font-size: 15px; border: 1px solid var(--field); }
.ctl.err { border-color: var(--alert); }
input.ctl, select.ctl { height: 46px; padding: 0 12px; }
input.ctl { padding: 0 14px; }
textarea.ctl { padding: 12px 14px; line-height: 1.5; resize: vertical; }
.with-unit { display: flex; align-items: center; gap: 8px; }
.unit { font-size: 14px; color: var(--muted); white-space: nowrap; }
.note { font-size: 12px; color: var(--muted); }
.field-err { font-size: 12px; color: var(--alert); font-weight: 600; }
// Link "+ Thêm nhóm cơ" / "+ Thêm dụng cụ" (suy ra): chữ link của prototype ("Mở trên YouTube": 13px 600 --primary)
.add-link { align-self: flex-start; background: none; border: 0; padding: 2px 0; font: inherit; font-size: 13px; font-weight: 600; color: var(--primary); cursor: pointer; }
.add-link:hover { color: var(--navInk); }
.add-row { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
.add-row .ctl { flex: 1 1 160px; width: auto; }
.small-btn, .reload { height: 36px; padding: 0 14px; border-radius: 999px; border: 1px solid var(--field); background: var(--surface);
  color: var(--ink); font: inherit; font-weight: 600; font-size: 14px; cursor: pointer; }
.reload { margin-left: 8px; }
.link { height: 36px; padding: 0 6px; border: 0; background: transparent; color: var(--muted); font: inherit; font-size: 14px; text-decoration: underline; cursor: pointer; }
.msg { margin: 0; border-radius: 12px; padding: 10px 14px; font-size: 14px; font-weight: 600; background: var(--warnBg); color: var(--warn); }
.actions { display: flex; flex-wrap: wrap; gap: 10px; }
.primary, .cancel { box-sizing: border-box; height: 50px; border-radius: 999px; cursor: pointer; }
.primary:disabled { opacity: .6; cursor: default; }
.primary { padding: 0 26px; border: 0; background: var(--primary); color: var(--onPrimary); font-family: var(--font-head); font-weight: 700; font-size: 20px; }
.cancel { display: inline-flex; align-items: center; padding: 0 22px; border: 1px solid var(--field); background: var(--surface); color: var(--ink);
  font-weight: 600; font-size: 15px; text-decoration: none; }
.cancel:hover { color: var(--ink); }
@media (max-width: 879.98px) { h1 { font-size: 38px; } }
```

`frontend/projects/admin/src/app/app.routes.ts` — thêm ngay sau route `thu-vien/bai-tap` (Task 6):
```ts
      {
        path: 'thu-vien/bai-tap/moi',
        loadComponent: () => import('./exercises/exercise-form.component').then((m) => m.ExerciseFormComponent),
        data: { mode: 'create' },
        title: 'Thêm bài mới — Khu vực coach',
      },
      {
        path: 'thu-vien/bai-tap/:id/sua',
        loadComponent: () => import('./exercises/exercise-form.component').then((m) => m.ExerciseFormComponent),
        data: { mode: 'edit' },
        title: 'Sửa bài — Khu vực coach',
      },
```

- [ ] **Step 4: Chạy — kỳ vọng PASS**

Run: `cd frontend && npm run test:ci`
Expected: PASS — `ExerciseFormComponent` 10 test; các spec Task 6 và spec cũ vẫn xanh; không test nào failed.
Run: `cd frontend && npx ng build admin`
Expected: build thành công, có lazy chunk `exercise-form-component`.

Kiểm tay (compose chạy Task 1–6): `docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build --wait`, mở `http://localhost:4200/thu-vien/bai-tap/moi` và `/thu-vien/bai-tap/<id>/sua` ở **390px** và **1280px**, đối chiếu `docs/duobexiuuu-coaching-design.html#AdTaoHoSo` (card, nhãn, ô, nút) cùng độ rộng:
- Đường dẫn "Thư viện bài tập / Thêm bài mới" 14px `--muted`; H1 46px (390px: 38px); 3 card trắng viền `--line` radius 12 padding `20px 22px`, h2 Barlow 700 26px; ô 46px radius 14 viền `--field`, nhãn 14px 600, sao `--primary`; 1280px: form rộng tối đa 760px, "Nhóm cơ" và "Dụng cụ" 2 cột, 4 ô liều chia cột theo lưới 220px; 390px: mọi ô 1 cột full width, không tràn ngang.
- "+ Thêm nhóm cơ" 13px 600 `--primary`; bấm → ô nhập + "Thêm" 36px + "Huỷ" gạch chân; Enter trong ô thêm mục KHÔNG gửi form; thêm "Resistance Band" ở dụng cụ → ô chọn chuyển sang mục mới; thêm "ngực" ở nhóm cơ → "Đã có mục cùng tên." đỏ 12px dưới ô.
- Lưu trống → viền `--alert` ở Tên bài/Nhóm cơ/Dụng cụ + chữ đỏ dưới ô + hộp vàng "Có ô chưa đúng…"; Reps "12–10" → lỗi server dưới ô Reps.
- Thêm "Face Pull" + Cable khi bài đó đang ẩn → hộp vàng "Đã có bài cùng tên và dụng cụ. Bài đó đang ẩn." + nút "Bỏ ẩn bài đó" 36px → về thư viện, bài được chọn sẵn.
- Mở trang sửa ở 2 tab, lưu tab 1 rồi lưu tab 2 → hộp vàng "Bài vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất." + "Tải lại" → form nhận bản mới.
- Lưu thành công → `/thu-vien/bai-tap?bai=<id>`, aside hiện bài vừa lưu với liều "10–12", "90s"; "Huỷ" ở trang sửa quay về đúng bài đó.
- Tab qua ô/nút: viền focus 2px `--primary`; placeholder màu `--faint`.

- [ ] **Step 5: Commit**
```bash
git add frontend/projects/admin/src/app/exercises frontend/projects/admin/src/app/app.routes.ts
git commit -F - <<'EOF'
feat(admin): add/edit exercise form with inline categories, hidden-duplicate unhide and version reload

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```

---

### Task 8: E2E `@web` thư viện bài tập (BR-001, BR-018) + route admin trong CLAUDE.md

Bind 2 scenario `@web` của `features/thu-vien/bai-tap/thu-vien-bai-tap.feature`: **BR-f03-thu-vien-bai-tap-001** "Cùng tên khác dụng cụ là hai bài khác nhau" và outline **BR-f03-thu-vien-bai-tap-018** "Tìm theo một phần tên, không dấu" (2 dòng Examples). Background `coach "Coach Duobexiuuu" đã đăng nhập` đã có ở `student-profile.steps.ts` — dùng lại, không định nghĩa lại. Step texts mới không trùng step cũ (`kết quả có {string}` không khớp `kết quả có hồ sơ {string}` vì Cucumber Expression neo đầu/cuối; `coach tìm bài với từ khoá {string}` ≠ `coach tìm học viên với từ khóa {string}`). Các step cùng chữ trong scenario `@api` được Reqnroll bind ở Task 2/5 — CucumberJS chỉ chạy `@web and not @manual` nên không đụng.

DB của compose giữ dữ liệu giữa các lần chạy và API nạp sẵn 99 bài cho coach ở mọi môi trường (Task 5, `Seed:ExerciseLibrary` mặc định true — e2e KHÔNG tắt cờ này) (có sẵn "Lateral Raise" + Cable, "45° Incline Chest Fly") → mỗi tên bài viết trong feature được gắn hậu tố ` e2e<timestamp><rand>` cố định trong scenario (cùng cách hậu tố email của F01/F02), để BR-f03-thu-vien-bai-tap-001 không chặn lần chạy sau. Hậu tố không đổi ý nghĩa: "incline"/"ep nguc" vẫn là một phần tên. Bài tạo sẵn (Given) đi qua API; bước coach thao tác (When/Then) đi qua UI. Mỗi lần chạy e2e để lại 4 bài có hậu tố trong thư viện coach của compose (giống email hậu tố của F01/F02).

Cập nhật `CLAUDE.md`: bảng page key `AdThuVien` → route thật `/thu-vien/bai-tap`, câu route web admin liệt kê thêm 3 route thư viện.

**Files:**
- Create: `frontend/e2e/steps/exercise-library.steps.ts`
- Modify: `frontend/e2e/support/world.ts`
- Modify: `CLAUDE.md`

**Interfaces:**
- Consumes: Task 6 — `/thu-vien/bai-tap`: H1 "Thư viện bài tập", ô "Tìm bài tập" (Enter), link "Thêm bài mới", `tbody tr` có rowheader = tên bài, ô "Dụng cụ". Task 7 — H1 "Thêm bài mới", nhãn "Tên bài"/"Nhóm cơ"/"Dụng cụ", nút "Lưu bài", URL sau lưu `/thu-vien/bai-tap?bai=<guid>`. Task 1–2 — `GET /api/admin/exercise-categories`, `POST /api/admin/exercises` → `201`. F02 — step `coach {string} đã đăng nhập`; `POST /api/auth/login` (`app: 'admin'`), `ADMIN`, `ADMIN_URL`.
- Produces: `E2EWorld.exerciseSuffix`; step defs `@web`: `thư viện có bài {string} dụng cụ {string}`, `coach thêm bài {string} dụng cụ {string} nhóm cơ {string}`, `thư viện có cả {string} dụng cụ {string} và {string} dụng cụ {string}`, `thư viện có bài {string}`, `coach tìm bài với từ khoá {string}`, `kết quả có {string}`.

**Owns:** —

- [ ] **Step 1: Chạy e2e thư viện — kỳ vọng FAIL (undefined)**

Run (compose chạy Task 1–7; `deploy/.env` có `AUTH_PERMIT_PER_MINUTE=1000`):
```bash
docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build --wait
cd frontend && npm run e2e -- ../features/thu-vien/bai-tap/thu-vien-bai-tap.feature
```
Expected: FAIL — `3 scenarios (3 undefined)`; Background `coach "Coach Duobexiuuu" đã đăng nhập` đã bind, các step `thư viện có bài …`, `coach thêm bài …`, `coach tìm bài với từ khoá …` báo `Undefined`.

- [ ] **Step 2: Viết step definitions**

`frontend/e2e/support/world.ts` — thêm field trong `E2EWorld`, sau `importResult`:
```ts
  /** F03: hậu tố " e2e…" gắn vào mọi tên bài trong scenario (DB compose giữ dữ liệu + có 99 bài nạp sẵn). */
  exerciseSuffix = '';
```

`frontend/e2e/steps/exercise-library.steps.ts`:
```ts
import { Given, Then, When } from '@cucumber/cucumber';
import { expect } from '../support/expect';
import { ADMIN, ADMIN_URL } from '../support/env';
import { E2EWorld } from '../support/world';

interface Category { id: string; name: string; }
interface Categories { muscleGroups: Category[]; equipment: Category[]; }

/**
 * DB của compose giữ dữ liệu giữa các lần chạy và có 99 bài nạp sẵn ("Lateral Raise" + Cable, "45° Incline Chest Fly"…)
 * → mỗi tên bài viết trong feature được gắn một hậu tố cố định trong scenario để không bị chặn trùng (BR-f03-thu-vien-bai-tap-001).
 */
function realName(world: E2EWorld, written: string): string {
  world.exerciseSuffix ||= ` e2e${Date.now()}${Math.floor(Math.random() * 1000)}`;
  return `${written}${world.exerciseSuffix}`;
}

async function authHeaders(world: E2EWorld): Promise<Record<string, string>> {
  const res = await world.page.request.post(`${ADMIN_URL}/api/auth/login`,
    { data: { email: ADMIN.email, password: ADMIN.password, app: 'admin' } });
  expect(res.ok()).toBe(true);
  const { accessToken } = (await res.json()) as { accessToken: string };
  return { authorization: `Bearer ${accessToken}` };
}

function categoryId(list: Category[], name: string): string {
  const found = list.find((c) => c.name === name);
  if (!found) throw new Error(`Danh mục không có "${name}"`);
  return found.id;
}

/** Tạo bài qua API (dữ liệu có sẵn của Given). */
async function createExercise(world: E2EWorld, name: string, muscleGroup: string, equipment: string): Promise<void> {
  const headers = await authHeaders(world);
  const cats = await world.page.request.get(`${ADMIN_URL}/api/admin/exercise-categories`, { headers });
  expect(cats.ok()).toBe(true);
  const { muscleGroups, equipment: equipmentList } = (await cats.json()) as Categories;
  const res = await world.page.request.post(`${ADMIN_URL}/api/admin/exercises`, {
    headers,
    data: { name, muscleGroupId: categoryId(muscleGroups, muscleGroup), equipmentId: categoryId(equipmentList, equipment) },
  });
  expect(res.status()).toBe(201);
}

/** Mở Thư viện bài tập, gõ từ khoá rồi Enter (ô tìm của AdThuVien tìm khi gõ xong bấm Enter). */
async function searchLibrary(world: E2EWorld, keyword: string): Promise<void> {
  await world.page.goto(`${ADMIN_URL}/thu-vien/bai-tap`);
  await expect(world.page.getByRole('heading', { level: 1, name: 'Thư viện bài tập' })).toBeVisible();
  const box = world.page.getByLabel('Tìm bài tập');
  await box.fill(keyword);
  await box.press('Enter');
}

/** Dòng bảng có đúng tên bài (rowheader) và đúng dụng cụ (ô). */
function libraryRow(world: E2EWorld, name: string, equipment: string) {
  return world.page.locator('tbody tr')
    .filter({ has: world.page.getByRole('rowheader', { name, exact: true }) })
    .filter({ has: world.page.getByRole('cell', { name: equipment, exact: true }) });
}

// @BR-f03-thu-vien-bai-tap-001 — nhóm cơ của bài có sẵn không quan trọng với rule, dùng "Vai" như bài seed
Given('thư viện có bài {string} dụng cụ {string}', async function (this: E2EWorld, name: string, equipment: string) {
  await createExercise(this, realName(this, name), 'Vai', equipment);
});

// @BR-f03-thu-vien-bai-tap-001
When('coach thêm bài {string} dụng cụ {string} nhóm cơ {string}',
  async function (this: E2EWorld, name: string, equipment: string, muscleGroup: string) {
    await this.page.goto(`${ADMIN_URL}/thu-vien/bai-tap`);
    await this.page.getByRole('link', { name: 'Thêm bài mới' }).click();
    await expect(this.page.getByRole('heading', { level: 1, name: 'Thêm bài mới' })).toBeVisible();
    await this.page.getByLabel('Tên bài').fill(realName(this, name));
    await this.page.getByLabel('Nhóm cơ').selectOption({ label: muscleGroup });
    await this.page.getByLabel('Dụng cụ').selectOption({ label: equipment });
    await this.page.getByRole('button', { name: 'Lưu bài' }).click();
    await expect(this.page).toHaveURL(/\/thu-vien\/bai-tap\?bai=[0-9a-f-]{36}$/);
  });

// @BR-f03-thu-vien-bai-tap-001
Then('thư viện có cả {string} dụng cụ {string} và {string} dụng cụ {string}',
  async function (this: E2EWorld, name1: string, equipment1: string, name2: string, equipment2: string) {
    await searchLibrary(this, realName(this, name1));
    await expect(libraryRow(this, realName(this, name1), equipment1)).toHaveCount(1);
    await expect(libraryRow(this, realName(this, name2), equipment2)).toHaveCount(1);
  });

// @BR-f03-thu-vien-bai-tap-018
Given('thư viện có bài {string}', async function (this: E2EWorld, name: string) {
  await createExercise(this, realName(this, name), 'Ngực', 'Machine');
});

// @BR-f03-thu-vien-bai-tap-018 — tìm ở server (bỏ dấu, không phân biệt hoa/thường)
When('coach tìm bài với từ khoá {string}', async function (this: E2EWorld, keyword: string) {
  await searchLibrary(this, keyword);
});

Then('kết quả có {string}', async function (this: E2EWorld, name: string) {
  await expect(this.page.getByRole('rowheader', { name: realName(this, name), exact: true })).toBeVisible();
});
```

`CLAUDE.md` — sửa dòng bảng page key:
```diff
-| `AdThuVien` | Thư viện bài tập | admin | `/library/exercises` |
+| `AdThuVien` | Thư viện bài tập | admin | `/thu-vien/bai-tap` (thêm: `/thu-vien/bai-tap/moi`, sửa: `/thu-vien/bai-tap/:id/sua`) |
```
và câu route ngay dưới bảng:
```diff
-Route web admin dùng tiếng Việt (ADR-f02-ho-so-hoc-vien-008): đã có `/dashboard` (trang đích), `/hoc-vien/moi`, `/hoc-vien/:id`, `/hoc-vien/:id/sua`. Route của màn chưa làm trong bảng vẫn là gợi ý — khi làm, đặt tiếng Việt dưới tiền tố `/hoc-vien/:id/…`.
+Route web admin dùng tiếng Việt (ADR-f02-ho-so-hoc-vien-008): đã có `/dashboard` (trang đích), `/hoc-vien/moi`, `/hoc-vien/:id`, `/hoc-vien/:id/sua`, `/thu-vien/bai-tap` (`?bai=<id>` chọn sẵn bài), `/thu-vien/bai-tap/moi`, `/thu-vien/bai-tap/:id/sua`. Route của màn chưa làm trong bảng vẫn là gợi ý — khi làm, đặt tiếng Việt dưới tiền tố `/hoc-vien/:id/…` (màn theo học viên) hoặc `/thu-vien/…` (thư viện dùng chung).
```

- [ ] **Step 3: Chạy — kỳ vọng PASS**

Run:
```bash
docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build --wait
cd frontend && npm run e2e -- ../features/thu-vien/bai-tap/thu-vien-bai-tap.feature
```
Expected: PASS — `3 scenarios (3 passed)` (BR-001 + 2 dòng Examples của BR-018), không `undefined`/`ambiguous`; `@manual` và scenario chỉ `@api` không được chạy.

Run (chạy lại ngay, kiểm hậu tố chống trùng): `cd frontend && npm run e2e -- ../features/thu-vien/bai-tap/thu-vien-bai-tap.feature`
Expected: `3 scenarios (3 passed)`.

Run (toàn bộ @web — không phá F00/F01/F02): `cd frontend && npm run e2e`
Expected: mọi scenario `@web` passed, không step `ambiguous`.

Run: `cd frontend && npm run test:ci && npx ng build admin`
Expected: PASS; build thành công.

Run: `cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"`
Expected: PASS (Task 8 không đổi backend; 2 scenario `@web` bị lọc khỏi Reqnroll).

Kiểm tay lần cuối (manual verification của tech-design §6): so màn Thư viện và Thêm/Sửa bài với prototype ở **390px** và **1280px** (`AdThuVien`, kiểu form `AdTaoHoSo`) theo checklist Step 8 Task 6 và Step 4 Task 7; rail active đúng mục ở cả 3 route thư viện. Đo p95 (NFR-f03-thu-vien-bai-tap-001): thêm bài qua API cho đủ 500 bài trong thư viện coach của compose, gọi `GET /api/admin/exercises` (không lọc, có `q`, có `muscleGroupId`) mỗi kiểu 50 lần bằng `curl -w '%{time_total}'`; p95 phải < 0,5 s.

- [ ] **Step 4: Commit**
```bash
git add frontend/e2e/steps/exercise-library.steps.ts frontend/e2e/support/world.ts CLAUDE.md
git commit -F - <<'EOF'
test(e2e): exercise library @web scenarios (same name different equipment, accent-insensitive search); document library routes

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_017b7Z6NqFqiU4t4EbdWs9by
EOF
```
