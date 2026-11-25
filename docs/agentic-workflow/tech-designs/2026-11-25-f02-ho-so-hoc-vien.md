---
doc-id: 2026-11-25-f02-ho-so-hoc-vien
status: ready
---

# Technical Design: F02 — Hồ sơ học viên & trạng thái coaching

## Meta

| Field | Value |
|-------|-------|
| Author | Hoangduc1203BK (soạn cùng Claude) |
| Date | 2026-11-25 |
| Spec | `docs/agentic-workflow/specs/2026-11-25-f02-ho-so-hoc-vien.md` |
| BDD | `@BR-f02-ho-so-hoc-vien-*` trong `features/hoc-vien/ho-so/ho-so-hoc-vien.feature` |
| JIRA | N/A — local-only |

---

## 1. Tổng quan

### Vấn đề
Chưa có dữ liệu học viên: học viên hiện chỉ là user Identity tạo trong test. Coach cần tạo/sửa hồ sơ theo form đăng ký (~45 mục), ghi chú nội bộ, đổi trạng thái coaching có lịch sử, và danh sách có tìm kiếm.

### Goals
- Aggregate `Student` + tài khoản đăng nhập "Chờ kích hoạt" tạo cùng lúc (nền cho F01-P1 gửi mật khẩu tạm).
- API `/api/admin/students/*` cho 25 BR; `/api/client/me` trả dữ liệu học viên không có ghi chú nội bộ.
- 4 màn admin: Dashboard (danh sách tạm), Tạo hồ sơ, Hồ sơ, Sửa hồ sơ — bám prototype.

### Non-Goals
Gửi email/mật khẩu tạm/kích hoạt (F01-P1), import Excel (increment sau), dashboard đầy đủ (F08), phase (F05).

---

## 2. Bối cảnh & Kiến trúc tổng quan

### Hiện trạng
- Domain có `Coach`, value object `Email` (trim + lowercase, `backend/src/Coaching.Domain/Common/Email.cs`). Use case dạng `XxxHandler.HandleAsync` trả `Result<T>`, controller inject handler (`Coaching.Api/Controllers/Admin/ProfileController.cs`).
- `DomainException` → 400 (`GlobalExceptionHandler`); `Error` có `ErrorKind.NotFound/Conflict` → 404/409 (`ResultExtensions`).
- `IAuditLog.Record(...)` thêm bản ghi vào unit of work hiện tại. `IClock.Today` = ngày giờ VN.
- Schema do Liquibase quản lý (`database/changelog/v0.1.0`). Identity bật `RequireUniqueEmail`.
- FE admin: route `trang-chu` trong `AdminShellComponent`; rail chỉ hiện mục đã có trang (quyết định F00).

### Kiến trúc tổng quan
N/A — gói trong 1 API + 1 web admin hiện có, không thêm service hay integration ngoài.

---

## 3. Giải pháp đề xuất

### Quyết định kiến trúc

- `ADR-f02-ho-so-hoc-vien-001` — Tạo `Student` và tài khoản Identity "Chờ kích hoạt" trong cùng một transaction
  - **Quyết định**: `CreateStudentHandler` gọi port `IStudentAccounts.CreatePendingAsync(email, fullName)` (tạo `AppUser` role Student, **không mật khẩu**, chưa đăng nhập được) rồi `Student.Create(...)` giữ `UserId`; cả hai bọc trong `IUnitOfWork.ExecuteInTransactionAsync`. Trạng thái tài khoản (`PendingActivation`) nằm ở `Student`.
  - **Lý do**: email duy nhất toàn hệ thống (gồm cả tài khoản coach) có sẵn ở Identity `NormalizedEmail` + unique index, và F01-P1 chỉ cần đặt mật khẩu tạm cho user đã có.
  - **Trade-off chấp nhận**: `UserManager.CreateAsync` tự `SaveChanges`, nên cần transaction tường minh thay vì một `SaveChanges`.
- `ADR-f02-ho-so-hoc-vien-002` — Câu trả lời form lưu dạng JSON, chỉ các cột cần truy vấn là cột riêng
  - **Quyết định**: bảng `Students` có cột: `Id, CoachId, UserId, FullName, Email, SearchKey, AccountStatus, CoachingStatus, InternalNote, InternalNoteUpdatedAt, CreatedAt, UpdatedAt, UpdatedByUserId, Version (rowversion)`. Toàn bộ mục form còn lại là owned value object `RegistrationForm` map `ToJson()` vào cột `Registration nvarchar(max)`.
  - **Lý do**: ~45 mục chỉ đọc/ghi cùng hồ sơ, không lọc theo chúng; JSON tránh 45 cột và đổi form không cần migration. F10 đọc số đo qua chính value object.
  - **Trade-off chấp nhận**: không có ràng buộc DB cho từng mục — kiểm hoàn toàn ở Domain.
- `ADR-f02-ho-so-hoc-vien-003` — Tìm kiếm qua cột `SearchKey` tính ở Domain
  - **Quyết định**: `SearchKey` = bỏ dấu tiếng Việt (`đ/Đ → d`, `NormalizationForm.FormD` bỏ dấu kết hợp) + lowercase của `FullName + " " + Email`; truy vấn `SearchKey LIKE '%' + Normalize(q) + '%'`.
  - **Lý do**: collation `*_CI_AI` của SQL Server không coi `đ` = `d`, nên "duc" không ra "Đức".
  - **Trade-off chấp nhận**: thêm một cột phải cập nhật mỗi lần đổi tên; LIKE `%x%` không dùng index (chấp nhận với ≤ 500 hồ sơ, NFR-f02-ho-so-hoc-vien-005).
- `ADR-f02-ho-so-hoc-vien-004` — Chặn ghi đè bằng `rowversion`
  - **Quyết định**: DTO chi tiết trả `version` (base64 của `rowversion`); mọi lệnh sửa (`PUT` hồ sơ, `PUT` ghi chú, `POST` đổi trạng thái) bắt buộc gửi `version`, EF đặt `OriginalValue` → `DbUpdateConcurrencyException` → 409 `student.version_conflict`.
  - **Lý do**: optimistic concurrency là cách rẻ nhất cho 1 coach nhiều tab, không cần khóa.
  - **Trade-off chấp nhận**: FE phải tải lại khi 409; sửa ghi chú cũng tăng version của cả hồ sơ.
- `ADR-f02-ho-so-hoc-vien-005` — Lịch sử trạng thái là entity con của `Student`, chỉ thêm
  - **Quyết định**: `Student.ChangeCoachingStatus(newStatus, reason, actorUserId, at)` kiểm khác trạng thái hiện tại + lý do 1–500 ký tự, đổi trạng thái và thêm `CoachingStatusChange(From, To, ActorUserId, At, Reason)`; handler ghi `IAuditLog.Record("coaching_status.changed", "Student", id, "From→To")` (không ghi lý do, không ghi ghi chú). Không có API sửa/xóa lịch sử.
  - **Lý do**: invariant "đổi trạng thái luôn kèm đúng một dòng lịch sử" nằm trong aggregate, lưu cùng `SaveChanges`.
  - **Trade-off chấp nhận**: lịch sử nằm trong aggregate nên load hồ sơ kéo theo lịch sử (ít dòng, chấp nhận).
- `ADR-f02-ho-so-hoc-vien-006` — Kiểm dữ liệu ở Domain, hằng số danh sách lựa chọn và giới hạn dùng chung
  - **Quyết định**: `RegistrationForm.Create(RegistrationInput, DateOnly today)` kiểm khoảng số (BR-f02-ho-so-hoc-vien-008), danh sách lựa chọn (`RegistrationOptions`, BR-f02-ho-so-hoc-vien-009), ngày chuyển khoản ≤ `today` (BR-f02-ho-so-hoc-vien-010), URL http/https (BR-f02-ho-so-hoc-vien-011), độ dài văn bản; lỗi → `DomainException("student.<field>.<lỗi>")` → 400. `RegistrationForm.MissingRequired()` trả mã các mục `*` còn thiếu (trim rỗng = thiếu). Không thêm FluentValidation.
  - **Lý do**: một nguồn luật cho tạo, sửa và import sau này; FE có bản sao danh sách lựa chọn chỉ để hiển thị.
  - **Trade-off chấp nhận**: lỗi trả từng lỗi một (lỗi đầu tiên) thay vì gom; FE đã kiểm trước nên hiếm gặp.
- `ADR-f02-ho-so-hoc-vien-007` — Dữ liệu phía học viên là DTO riêng không có ghi chú
  - **Quyết định**: `/api/client/me` trả `StudentSelfDto(FullName, Email, CoachingStatus, AccountStatus)` đọc từ `Student` theo `UserId` của token. Test kiến trúc quét mọi type trả về từ `Controllers/Client/**` không có property chứa `Note`.
  - **Lý do**: tách type DTO theo phía là cách chắc nhất để ghi chú không lọt (NFR-f02-ho-so-hoc-vien-001).
  - **Trade-off chấp nhận**: thêm một truy vấn ở `/me`.
- `ADR-f02-ho-so-hoc-vien-008` — FE admin: route tiếng Việt, một form dùng cho tạo + sửa
  - **Quyết định**: route `dashboard` (danh sách tạm, trang đích sau đăng nhập thay `trang-chu`), `hoc-vien/moi`, `hoc-vien/:id`, `hoc-vien/:id/sua`. `StudentFormComponent` dùng chung tạo/sửa; bản nháp `localStorage['dbx.admin.student-draft']` chỉ ở chế độ tạo, xóa khi lưu thành công hoặc "Xoá trắng hồ sơ" (NFR-f02-ho-so-hoc-vien-004).
  - **Lý do**: giữ quy ước route tiếng Việt của F00; một form tránh lệch giữa tạo và sửa.
  - **Trade-off chấp nhận**: lệch gợi ý route tiếng Anh trong CLAUDE.md — sẽ cập nhật bảng route CLAUDE.md.
- `ADR-f02-ho-so-hoc-vien-009` — Giới hạn độ dài văn bản
  - **Quyết định**: họ tên ≤ 100; văn bản một dòng ≤ 200; đoạn văn ≤ 2000; ghi chú nội bộ ≤ 5000; lý do đổi trạng thái ≤ 500; URL ≤ 2048 (NFR-f02-ho-so-hoc-vien-003).
  - **Lý do**: đủ cho câu trả lời form thật (mẫu Excel dài nhất ~600 ký tự) và chặn payload lạm dụng.
  - **Trade-off chấp nhận**: câu trả lời rất dài phải rút gọn.

### Component Design

**Domain** (`backend/src/Coaching.Domain/Students/`)
- `Student` (aggregate): `Create(coachId, userId, fullName, Email, RegistrationForm, at)`, `UpdateRegistration(fullName, RegistrationForm, actorUserId, at)`, `UpdateInternalNote(note, at)`, `ChangeCoachingStatus(...)`, `MissingRequiredFields`. Không có method xóa (BR-f02-ho-so-hoc-vien-025).
- `AccountStatus { PendingActivation, Activated }`, `CoachingStatus { Active, Paused, Ended }`.
- `RegistrationForm` (value object, toàn bộ mục form trừ họ tên/email/ghi chú), `RegistrationOptions` (danh sách lựa chọn), `CoachingStatusChange` (entity con), `SearchText.Normalize(string)`.
- `IStudentRepository`: `GetAsync(id)`, `Add`, `ListAsync(coachId, normalizedQuery)`, `GetByUserIdAsync(userId)`.

**Application** (`backend/src/Coaching.Application/Students/`)
- `Commands/CreateStudent`, `Commands/UpdateStudent`, `Commands/UpdateInternalNote`, `Commands/ChangeCoachingStatus`, `Queries/GetStudent`, `Queries/ListStudents`, `Queries/GetStudentSelf`.
- Port `IStudentAccounts.CreatePendingAsync(Email, fullName) → Result<Guid>` (`email_taken` → 409).
- `IUnitOfWork.ExecuteInTransactionAsync(Func<CancellationToken, Task>, CancellationToken)`.
- Coach hiện tại: lấy `Coach` theo `ICurrentUser.UserId` (`ICoachRepository.GetByUserIdAsync`).

**Infrastructure**: `StudentConfiguration` (+ `OwnsOne(Registration).ToJson()`, `OwnsMany(StatusHistory)` bảng riêng, `Version` `IsRowVersion`), `StudentRepository`, `StudentAccounts` (UserManager), `AppDbContext.ExecuteInTransactionAsync` (execution strategy + transaction).

**Api**: `Controllers/Admin/StudentsController`, sửa `Controllers/Client/MeController`.

**FE** (`frontend/projects/admin/src/app/students/`): `StudentListComponent`, `StudentFormComponent`, `StudentProfileComponent`, `CoachingStatusPanelComponent`, `students.api.ts`, `registration-options.ts`; shared: thêm `StatusBadge` nếu chưa có.

### API Contracts

Mọi route dưới `/api/admin/students` (policy `AdminOnly`). Lỗi theo ProblemDetails với `code`.

| Method + route | Body | Trả về | Lỗi |
|---|---|---|---|
| `GET /api/admin/students?q=` | — | `StudentListItemDto[]` (`id, fullName, email, coachingStatus, accountStatus, createdAt`), sắp `CreatedAt` giảm dần | — |
| `POST /api/admin/students` | `{ fullName, email, registration, internalNote? }` | 201 + `StudentDetailDto` | 400 `student.*`/`email.*`, 409 `student.email_taken` |
| `GET /api/admin/students/{id}` | — | `StudentDetailDto` (`…, registration, missingRequired[], internalNote, internalNoteUpdatedAt, updatedAt, updatedBy, statusHistory[], version`) | 404 `student.not_found` |
| `PUT /api/admin/students/{id}` | `{ fullName, registration, version }` (không có email) | `StudentDetailDto` | 400, 404, 409 `student.version_conflict` |
| `PUT /api/admin/students/{id}/internal-note` | `{ note, version }` | `StudentDetailDto` | 400, 404, 409 |
| `POST /api/admin/students/{id}/coaching-status` | `{ status, reason, version }` | `StudentDetailDto` | 400 `student.status_unchanged`, `student.reason_required`, `student.reason_too_long`; 404; 409 |
| `GET /api/client/me` | — | `StudentSelfDto` | 404 nếu user chưa có `Student` |

`registration` là object phẳng theo mã mục (`pack`, `price`, `transferDate`, `address`, `age`, `heightCm`, `weightKg`, `chestCm`… `photoFrontUrl`…); số thập phân gửi dạng JSON number (FE đổi "72,5" → 72.5). Trạng thái gửi/nhận dạng `"Active" | "Paused" | "Ended"`; FE hiển thị tiếng Việt. Không endpoint DELETE/sửa lịch sử.

### Data Model / Database Changes

Liquibase `database/changelog/v0.2.0/` (include sau `v0.1.0` trong master, tag `v0.2.0`):
- `0001-students.xml` → `scripts/0001-students.sql` + `rollback/0001-students.rollback.sql`:
  - `Students` (cột như ADR-f02-ho-so-hoc-vien-002; `Email nvarchar(256)` unique; `SearchKey nvarchar(400)`; `AccountStatus/CoachingStatus nvarchar(20)`; `InternalNote nvarchar(max)`; `Registration nvarchar(max) not null default '{}'`; FK `CoachId → Coaches`, FK `UserId → AspNetUsers` unique).
  - `StudentCoachingStatusChanges` (`Id, StudentId FK, FromStatus, ToStatus, ActorUserId, At datetimeoffset, Reason nvarchar(500)`; index `StudentId, At`).
  - Index `IX_Students_CoachId_CreatedAt`.

### Data Flow — tạo hồ sơ
1. FE `POST /students` → `CreateStudentHandler`.
2. Trong transaction: kiểm form (`RegistrationForm.Create`) → `IStudentAccounts.CreatePendingAsync` (Identity báo trùng email → 409) → `Student.Create` → `SaveChanges` → commit.
3. Trả `StudentDetailDto` (kèm `missingRequired`); FE xóa bản nháp, chuyển `hoc-vien/:id`.

### Thiết kế giao diện (UI)

- **Design link**: `docs/duobexiuuu-coaching-design.html` (prototype, không có Figma/Code Connect). Token, font, kích thước theo CLAUDE.md; breakpoint 880 px; kiểm ở 390 px và 1280 px.
- **Màn hình**:
  - **Dashboard (danh sách tạm)** — `/dashboard`, mục rail "Dashboard". *Suy ra, PO duyệt 2026-11-25*: dùng style bảng của page `Admin-Dashboard` (dòng `--rowLine`, hover `--rowHover`, cell `14px 12px` như prototype), tiêu đề "Học viên", cột Họ tên / Email / Trạng thái coaching / Tài khoản, ô tìm kiếm phía trên (input 48 px, radius 14 như prototype), nút "Tạo hồ sơ học viên". Không có các bộ lọc con (F08). Mỗi dòng mở `hoc-vien/:id`.
  - **Tạo hồ sơ học viên** — page `AdTaoHoSo`, `/hoc-vien/moi`: đúng 7 nhóm mục + thêm "Lịch rảnh", "Dụng cụ" (đoạn văn, không bắt buộc) vào nhóm "Tập luyện hiện tại"; bỏ mục "Gán giáo án" (F05). Nút "Lưu hồ sơ" (thay "Chỉ lưu hồ sơ"; nút "Lưu và gửi thông tin đăng nhập" thuộc F01-P1, chưa hiện) và "Xoá trắng hồ sơ". Thanh tiến độ "x/y mục bắt buộc" như prototype nhưng **không chặn lưu**. Aside "Trạng thái tài khoản": "Chờ kích hoạt" sau khi lưu.
  - **Hồ sơ học viên** — page `AdHoSo`, `/hoc-vien/:id`: header tên, email, chip trạng thái coaching, chip tài khoản; nút "Tạm dừng" / "Kết thúc" / "Tiếp tục coaching" (theo trạng thái hiện tại), "Sửa thông tin". Khối "Thông tin học viên" (Mục tiêu, Lịch rảnh, Dụng cụ + nút mở xem đủ form), khối "Ghi chú nội bộ" (sửa tại chỗ, "Chỉ coach thấy. Sửa lần cuối dd/MM."), khối "Mục bắt buộc còn thiếu". *Suy ra, PO duyệt 2026-11-25*: **khung lý do** mở ngay dưới header khi bấm đổi trạng thái (textarea + "Xác nhận" / "Huỷ", lỗi hiển thị như `msgStyle` warn của prototype); **card "Lịch sử coaching"** ở cột phải theo kiểu card "Tài khoản đăng nhập" (dòng: ngày giờ, Từ → Sang, lý do, người). Ẩn các khối thuộc spec khác (Chương trình và phase, Chỉ tiêu, Hoạt động gần đây, So sánh, Tài khoản đăng nhập, nút "Gửi link đặt lại mật khẩu", "Soạn phase").
  - **Sửa hồ sơ** — `/hoc-vien/:id/sua`, *suy ra, PO duyệt 2026-11-25*: dùng lại form `AdTaoHoSo`, điền sẵn, email chỉ xem, nút "Lưu thay đổi"; 409 → thông báo "Hồ sơ vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất." kèm nút "Tải lại".
- Số liệu nhập dấu phẩy thập phân, hiển thị `0,3`; ngày `dd/MM/yyyy`.

---

## 4. Các phương án đã cân nhắc

- **Mỗi mục form một cột** — loại: 45 cột, mỗi lần coach sửa Google Form phải migration; không có nhu cầu truy vấn theo từng mục.
- **Chỉ tạo `Student`, để F01-P1 tạo user Identity** — loại: phải tự kiểm trùng email với bảng user (coach) ở hai nơi và F01 phải ghép hai bản ghi về sau.
- **Collation `Vietnamese_CI_AI` thay `SearchKey`** — loại: không khớp "đ" với "d" (BR-f02-ho-so-hoc-vien-024).

---

## 5. Implementation Map

| Thứ tự | File (Create/Modify) | Nội dung | BR / ADR |
|---|---|---|---|
| 1 | C `database/changelog/v0.2.0/**`, M `db.changelog-master.xml` | Bảng `Students`, `StudentCoachingStatusChanges` | ADR-f02-ho-so-hoc-vien-002 |
| 2 | C `backend/src/Coaching.Domain/Students/**` + `Coaching.Domain.Tests/Students/**` | Aggregate, value object, kiểm dữ liệu, trạng thái | BR-f02-ho-so-hoc-vien-001…022, 025; ADR-f02-ho-so-hoc-vien-003, 005, 006, 009 |
| 3 | C `Coaching.Infrastructure/Persistence/Configurations/StudentConfiguration.cs`, `Repositories/StudentRepository.cs`, `Identity/StudentAccounts.cs`; M `AppDbContext.cs`, `IUnitOfWork.cs`, `DependencyInjection.cs` | Persist + tài khoản Chờ kích hoạt + transaction | ADR-f02-ho-so-hoc-vien-001, 004 |
| 4 | C `Coaching.Application/Students/**`, M `Application/DependencyInjection.cs` | 7 handler | BR-f02-ho-so-hoc-vien-001…025 |
| 5 | C `Coaching.Api/Controllers/Admin/StudentsController.cs`; M `Controllers/Client/MeController.cs` | API | ADR-f02-ho-so-hoc-vien-007 |
| 6 | M `Coaching.Infrastructure/Seeding/DatabaseInitializer.cs`; M `tests/**/ApiFactory.cs`, `Coaching.TestSupport/**` | Seed demo tạo kèm `Student` (idempotent: user demo đã có mà thiếu `Student` thì tạo bổ sung); helper test `CreateStudentAsync` tạo `Student` | ADR-f02-ho-so-hoc-vien-001 |
| 7 | C `backend/tests/Coaching.Acceptance/Steps/StudentProfileSteps.cs`; M IntegrationTests | Step `@api` + test kiến trúc DTO client | toàn bộ @BR-f02 |
| 8 | C `frontend/projects/admin/src/app/students/**`; M `app.routes.ts`, `admin-shell.component.ts` | 4 màn + rail Dashboard | ADR-f02-ho-so-hoc-vien-008 |
| 9 | C `frontend/e2e/steps/student-profile.steps.ts`, M `CLAUDE.md` (bảng route) | `@web` + route | BR-f02-ho-so-hoc-vien-003, 024 |

---

## 6. Test Strategy

### Acceptance / E2E
Theo scenario `@BR-f02-ho-so-hoc-vien-*` trong `features/hoc-vien/ho-so/ho-so-hoc-vien.feature`: `@api` chạy Reqnroll (`Coaching.Acceptance`, FakeTimeProvider cho BR-f02-ho-so-hoc-vien-010/013/014/020), `@web` chạy CucumberJS + Playwright trên compose.

### Unit / Integration
Seed bị LOẠI khỏi living doc được kiểm tại đây:

| Seed | Kiểm ở |
|---|---|
| BR-f02-ho-so-hoc-vien-003 tên 101 ký tự; BR-f02-ho-so-hoc-vien-018 lý do 501 ký tự | Domain test + integration 400 |
| BR-f02-ho-so-hoc-vien-004 mục toàn khoảng trắng tính là thiếu | Domain test `MissingRequired` |
| BR-f02-ho-so-hoc-vien-005/006 body gửi kèm trạng thái | Integration: field lạ bị bỏ qua, trạng thái vẫn mặc định |
| BR-f02-ho-so-hoc-vien-008 "abc"; BR-f02-ho-so-hoc-vien-009 giá trị ngoài danh sách | Integration 400 + FE spec (ô số, select) |
| BR-f02-ho-so-hoc-vien-011 `javascript:` | Domain test |
| BR-f02-ho-so-hoc-vien-012 hồ sơ không tồn tại | Integration 404 |
| BR-f02-ho-so-hoc-vien-015 hai lệnh đổi trạng thái cùng version | Integration: một 200, một 409, lịch sử 1 dòng |
| BR-f02-ho-so-hoc-vien-020 đổi bị từ chối không thêm lịch sử | Domain test + integration |
| BR-f02-ho-so-hoc-vien-021/025 không có API sửa/xóa | Integration: `DELETE` → 405/404 |
| NFR-f02-ho-so-hoc-vien-001 | Test kiến trúc quét DTO client (ADR-f02-ho-so-hoc-vien-007) |
| NFR-f02-ho-so-hoc-vien-002 | Integration: đổi trạng thái có 1 dòng audit, không chứa lý do/ghi chú |
| NFR-f02-ho-so-hoc-vien-004 | FE spec: lưu thành công → key nháp bị xóa |

### Automated verification
```bash
dotnet test backend/Coaching.slnx --filter "Category!=web&Category!=manual"
cd frontend && npm run test:ci && npm run e2e
```

### Manual verification
- So giao diện 4 màn với prototype ở 390 px và 1280 px (NFR thiết kế ở CLAUDE.md).
- NFR-f02-ho-so-hoc-vien-005: seed 500 hồ sơ trên môi trường test, đo `GET /students` và `GET /students/{id}` p95 < 500 ms.

---

## 7. Risks & Mitigations

| Risk | Khả năng | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| `UserManager` lưu ngoài transaction khi lỗi giữa chừng → user mồ côi | Thấp | Trung bình | Bọc trong `ExecuteInTransactionAsync`; integration test lỗi sau khi tạo user → không còn user |
| EF `ToJson` đọc JSON cũ khi đổi form | Trung bình | Thấp | Mọi mục nullable; thêm mục mới không cần migration |
| User Student có sẵn từ F00 (demo, test) chưa có `Student` → `/api/client/me` 404 | Cao | Trung bình | Seed bổ sung + helper test tạo kèm `Student` (Implementation Map bước 6) |
| Prototype không có 3 màn (danh sách, khung lý do, sửa) | — | Thấp | Đã chốt với PO 2026-11-25, dùng token/component của prototype |

---

## 8. Cross-Cutting Concerns

### Security
Chỉ `AdminOnly` gọi `/api/admin/students`; hồ sơ lọc theo `CoachId` của coach đang đăng nhập (404 nếu khác coach). Ghi chú chỉ trong DTO admin (ADR-f02-ho-so-hoc-vien-007). URL ảnh chỉ http/https, FE render bằng `href` có `rel="noopener noreferrer"`.

### Performance
Danh sách 1 truy vấn theo `CoachId` (index) + LIKE trên `SearchKey`; chi tiết 1 truy vấn + lịch sử. Đủ cho NFR-f02-ho-so-hoc-vien-005.

### Monitoring & Observability
N/A — dùng log mặc định của F00.

### Backward Compatibility
`/api/client/me` đổi body (thêm `coachingStatus`, `accountStatus`, bỏ `userId`) — client web hiện chỉ đọc `fullName`, `email`; giữ hai trường này.

### Migration & Rollout
Liquibase `v0.2.0` chạy qua job `db-migrate` khi merge `main`; rollback script xóa 2 bảng mới. Học viên demo đã seed ở F00 (local và Render) chưa có `Student` → seed lúc khởi động tạo bổ sung `Student` cho user demo nếu thiếu, nên `/api/client/me` không trả 404. Helper test F00 (`CreateStudentAsync`) đổi sang tạo kèm `Student`.

---

## 9. Open Questions

Không còn câu hỏi chặn. Import Excel chờ file mới của coach (spec OQ-01).
