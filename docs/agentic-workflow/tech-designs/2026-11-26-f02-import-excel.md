---
doc-id: 2026-11-26-f02-import-excel
status: ready
---

# Technical Design: Import hồ sơ học viên từ Excel + form theo Google Form (increment F02)

## Meta

| Field | Value |
|-------|-------|
| Author | Hoangduc1203BK (soạn cùng Claude) |
| Date | 2026-11-26 |
| Spec | `docs/agentic-workflow/specs/2026-11-26-f02-import-excel.md` |
| BDD | `@BR-f02-import-excel-*` trong `features/hoc-vien/ho-so/form-dang-ky.feature`, `features/hoc-vien/ho-so/import-excel.feature`; ví dụ F02 đã sửa trong `features/hoc-vien/ho-so/ho-so-hoc-vien.feature` |
| JIRA | N/A — local-only |

---

## 1. Tổng quan

### Vấn đề
Form hồ sơ (F02) lệch câu hỏi Google Form: có Lịch rảnh/Dụng cụ mà Google Form không hỏi; Google Form hỏi gộp mỡ + cách đo, cân nặng + chiều cao trước khi tập, 4 ảnh bằng 1 link; Số năm tập là chữ tự do. Chưa có import; coach gõ tay ~45 mục mỗi hồ sơ.

### Goals
- Form + dữ liệu hồ sơ khớp Google Form (BR-f02-import-excel-001…011), kể cả chuyển dữ liệu hồ sơ cũ.
- `POST /api/admin/students/import` nhận file .xlsx, tạo hồ sơ từng dòng, trả kết quả theo dòng (BR-f02-import-excel-012…034).
- Mục "Import từ Excel" ở đầu trang Tạo hồ sơ; prototype cập nhật theo form mới.

### Non-Goals
Theo spec §5 (cập nhật hồ sơ có sẵn, tải ảnh Drive, Google Sheets trực tiếp, nhiều sheet, gửi email, hoàn tác lô).

---

## 2. Bối cảnh & Kiến trúc tổng quan

### Hiện trạng
- `RegistrationForm` (`backend/src/Coaching.Domain/Students/RegistrationForm.cs`) là owned value object lưu một cột JSON `Students.Registration` (`StudentConfiguration`: `OwnsOne(...).ToJson("Registration")`). `Create(input, today)` kiểm từng mục bằng `Range/Option/Line/Paragraph/Url` và ném `DomainException` ở lỗi đầu tiên (ADR-f02-ho-so-hoc-vien-006).
- Danh sách mục và mục bắt buộc ở `RegistrationFields.cs`; danh sách chọn ở `RegistrationOptions.cs`; FE có bản sao ở `frontend/projects/admin/src/app/students/registration-fields.ts`.
- Tạo hồ sơ: `CreateStudentHandler` tạo `Student` rồi trong `ExecuteInTransactionAsync` gọi `IStudentAccounts.CreatePendingAsync` + lưu. Trùng email (kể cả coach) → `StudentErrors.EmailTaken`.
- Backfill lúc khởi động đã có tiền lệ (`DatabaseInitializer`, SearchKey). Schema/dữ liệu đổi qua Liquibase `database/changelog/vX/`.
- Backend chưa có thư viện đọc Excel (`backend/Directory.Packages.props`).

### Kiến trúc tổng quan
N/A — gói trong API + web admin hiện có; không thêm service hay integration ngoài (thư viện đọc file chạy trong process).

---

## 3. Giải pháp đề xuất

### Quyết định kiến trúc

- `ADR-f02-import-excel-001` — Đổi `RegistrationForm` theo Google Form: bỏ `Availability`, `Equipment`, `BodyFatPercent`, `BodyFatMethod`, `WeightBeforeLiftingKg`, `HeightThenCm`, `PhotoFront/Back/Left/RightUrl`; thêm `BodyFatAndMethod` (dòng ≤ 200), `WeightHeightBeforeLifting` (đoạn ≤ 2000), `BodyPhotoUrl` (URL http/https ≤ 2048); `TrainingYears` đổi từ `Option` sang `Line`. Mã mục mới: `bodyFatAndMethod`, `weightHeightBeforeLifting`, `bodyPhotoUrl`.
  - **Lý do:** Một nguồn luật cho nhập tay và import (BR-f02-import-excel-001, 003–007); lưu JSON nên không đổi cột DB.
  - **Trade-off:** `RegistrationInput`/DTO đổi khoá JSON — admin FE và mọi test dùng mã cũ phải sửa cùng lúc.
- `ADR-f02-import-excel-002` — Chuyển dữ liệu hồ sơ cũ bằng changeset Liquibase `v0.4.0` (SQL `JSON_VALUE`/`JSON_MODIFY` trên `Students.Registration`, nối link ảnh thừa vào `InternalNote` và đặt `InternalNoteUpdatedAt` khi ghi chú đổi), chỉ chạm dòng còn khoá cũ.
  - **Lý do:** Sau ADR-001 entity không còn đọc được khoá cũ, nên phải chuyển ở tầng SQL; điều kiện "còn khoá cũ" làm việc chạy lại không đổi gì (NFR-f02-import-excel-004, BR-f02-import-excel-002, 008–011).
  - **Trade-off:** Rollback không khôi phục được dữ liệu cũ (rollback script chỉ ghi chú, không làm gì); staging cần backup trước khi migrate.
- `ADR-f02-import-excel-003` — `RegistrationForm.Validate(input, today)` gom **mọi** lỗi thành `IReadOnlyList<RegistrationFieldError(Code, Message)>`; `Create` gọi `Validate` và ném lỗi đầu tiên như cũ.
  - **Lý do:** Import phải báo từng ô sai (BR-f02-import-excel-023) mà vẫn dùng đúng luật F02 (ADR-f02-ho-so-hoc-vien-006).
  - **Trade-off:** Refactor hàm kiểm của form đang dùng; test F02 phải giữ xanh.
- `ADR-f02-import-excel-004` — Đọc .xlsx bằng **ClosedXML** (MIT) sau port `IWorkbookReader` (Application) → `WorkbookSheet(Headers, Rows)` với `WorkbookCell(string? Text, double? Number, DateTime? Date)`; chỉ sheet đầu; file mật khẩu/hỏng/không phải xlsx → `import.file_invalid`.
  - **Lý do:** ClosedXML cho kiểu ô (ngày/số/chữ) cần cho BR-f02-import-excel-031/032, giấy phép MIT đạt NFR-f02-import-excel-003; port giữ Application không phụ thuộc thư viện.
  - **Trade-off:** Thêm 1 package (cùng phụ thuộc DocumentFormat.OpenXml); nặng hơn tự đọc XML nhưng ít mã hơn nhiều.
- `ADR-f02-import-excel-005` — Nhận cột theo tiêu đề: chuẩn hoá tiêu đề (bỏ phần sau xuống dòng, trim, chữ thường, bỏ dấu tiếng Việt, gộp khoảng trắng) rồi so **tiền tố** với bảng `StudentImportColumns` (40 tiêu đề trong file mẫu → mã mục; "Dấu thời gian" và cột lạ bị bỏ qua).
  - **Lý do:** Đổi thứ tự cột vẫn đọc đúng, tiêu đề song ngữ vẫn khớp (BR-f02-import-excel-018–020).
  - **Trade-off:** Coach sửa chữ đầu tiêu đề trong Google Form thì phải cập nhật bảng.
- `ADR-f02-import-excel-006` — Chuẩn hoá giá trị chọn ở Domain: `RegistrationOptions.Match(fieldCode, raw)` so dạng chuẩn hoá (chữ thường, bỏ khoảng trắng, đồng nhất `-–—`, bỏ `/`) với giá trị chuẩn và bảng `KnownSpellings`; không khớp → ô không hợp lệ. Chỉ dùng cho import.
  - **Lý do:** Đáp ứng BR-f02-import-excel-030 mà không đổi hành vi form nhập tay (danh sách chọn vẫn là select).
  - **Trade-off:** Bảng cách viết đã biết phải bảo trì tay; bản đầu ở §3 "Bảng cách viết đã biết", PO duyệt (OQ-01).
- `ADR-f02-import-excel-007` — Import chạy **đồng bộ** trong một request: kiểm file (≤ 5 MB qua `RequestSizeLimit` + kiểm độ dài, ≤ 500 dòng sau khi đọc) → duyệt dòng; mỗi dòng hợp lệ tạo hồ sơ bằng `StudentRegistrar` (tách từ `CreateStudentHandler`) trong **transaction riêng**; trùng trong file dùng `HashSet` email đã chuẩn hoá; file chỉ nằm trong bộ nhớ, không ghi đĩa.
  - **Lý do:** 500 dòng × ~20 ms ≈ 10 s, dưới NFR-f02-import-excel-001; transaction từng dòng giữ hồ sơ đã tạo khi dừng giữa chừng (BR-f02-import-excel-033); không lưu file (NFR-f02-import-excel-002).
  - **Trade-off:** Request dài (vài giây) giữ một kết nối; nếu file lớn hơn nhiều sẽ cần job nền.

### Component Design

- **Domain** (`Coaching.Domain/Students/`): `RegistrationForm` (ADR-001, 003), `RegistrationFields` (danh sách + mục bắt buộc mới, BR-f02-import-excel-007), `RegistrationOptions` (bỏ `TrainingYears`, `BodyFatMethods`; thêm `Match` + `KnownSpellings`, ADR-006), `RegistrationInput` (khoá mới), `RegistrationFieldError`.
- **Application** (`Coaching.Application/Students/`):
  - `StudentRegistrar` — tạo `Student` + tài khoản Chờ kích hoạt trong transaction; `CreateStudentHandler` và import cùng dùng (BR-f02-import-excel-028, 029: không gửi email).
  - `Import/IWorkbookReader`, `Import/StudentImportColumns`, `Import/ImportCellParser` (số/ngày/giá theo BR-f02-import-excel-031, 032), `Import/ImportStudentsHandler` → `ImportResultDto`.
- **Infrastructure**: `Import/ClosedXmlWorkbookReader : IWorkbookReader`.
- **Api**: `StudentsController.Import` — `[HttpPost("import")]`, `[RequestSizeLimit(5 * 1024 * 1024)]`, `IFormFile file`; AdminOnly như controller hiện có (BR-f02-import-excel-012, 013).

### API Contracts

| Method + route | Body | Thành công | Lỗi |
|---|---|---|---|
| `POST /api/admin/students/import` | `multipart/form-data`, field `file` | `200 { created, skipped, rows: [{ row, email, status: "Created"\|"Skipped", studentId?, errors: [{ column, reason }] }] }` (chỉ trả dòng có dữ liệu) | `400 import.file_invalid` "File phải là Excel .xlsx đọc được."; `400 import.file_too_large` "File vượt giới hạn 5 MB." (và 413 của Kestrel → map cùng code); `400 import.too_many_rows` "File vượt giới hạn 500 dòng."; `400 import.missing_column` "Thiếu cột \"{tên}\"."; `400 import.no_rows` "File không có dòng dữ liệu."; 401/403 như mọi route admin |
| `POST/PUT /api/admin/students…` (có sẵn) | `registration` dùng khoá mới (ADR-001) | như F02 | như F02 |

Câu lý do theo ô (tiếng Việt, cột = tiêu đề trong file): lấy từ `RegistrationFieldError.Message` (vd "phải từ 10 đến 100", "link ảnh không hợp lệ", "không được sau hôm nay"); trùng: "email đã có hồ sơ", "trùng email với dòng {n}"; thiếu: "bắt buộc".

### Data Model / Database Changes
Liquibase `database/changelog/v0.4.0/` (`0001-registration-google-form.xml`, `scripts/…sql`, `rollback/…sql`), không đổi cột. Với mỗi `Students` có khoá cũ trong `Registration`:
- `$.bodyFatAndMethod` = ghép `bodyFatPercent` + " %" và `bodyFatMethod` bằng " – " (BR-f02-import-excel-008).
- `$.weightHeightBeforeLifting` = ghép `weightBeforeLiftingKg` + " kg" và `heightThenCm` + " cm" bằng ", " (BR-f02-import-excel-009).
- `$.bodyPhotoUrl` = link đầu tiên có giá trị theo thứ tự front, back, left, right (BR-f02-import-excel-010); các link còn lại nối vào `InternalNote` dạng `Ảnh mặt sau: …` / `Ảnh bên trái: …` / `Ảnh bên phải: …` (BR-f02-import-excel-011), `InternalNoteUpdatedAt = SYSDATETIMEOFFSET()` khi ghi chú đổi.
- Xoá `$.availability`, `$.equipment` (BR-f02-import-excel-002) và các khoá cũ.
- Số thập phân hiển thị kiểu VN (72,5) khi ghép.

### Thiết kế giao diện (UI)

- **Design link**: `docs/duobexiuuu-coaching-design.html` (prototype; không Figma). PO yêu cầu **sửa cả file prototype**: dữ liệu `SECS` của page `AdTaoHoSo` (bỏ Lịch rảnh/Dụng cụ, Số năm tập thành ô chữ, 3 mục gộp) và khối "Thông tin học viên" của `AdHoSo`.
- **Màn hình**:
  - **Tạo hồ sơ** — `/hoc-vien/moi`, page `AdTaoHoSo`. *Suy ra, PO chốt vị trí 2026-11-26*: thêm card **"Import từ Excel"** ở đầu trang, trên form (card trắng `--surface`, viền `--line`, radius 12, padding `16px 18px`, h2 Barlow 700 22px như card aside), gồm: mô tả "Tải file .xlsx câu trả lời Google Form (tối đa 5 MB, 500 dòng). Hồ sơ được tạo ở Chờ kích hoạt, chưa gửi email.", nút phụ chọn file (44 px, viền `--field`), nút chính "Import" (44 px, `--primary`). Sau khi import: dòng tóm tắt "Đã tạo {x}, bỏ qua {y}" (hộp `--okBg` nếu y = 0, `--warnBg` nếu y > 0) và bảng lỗi kiểu bảng Dashboard (cột Dòng, Email, Cột, Lý do; dòng `--rowLine`). Lỗi cả file hiện `title` của API trong hộp `--warnBg`.
  - **Form Tạo/Sửa hồ sơ** — các mục theo ADR-001: "Số năm tập luyện" ô chữ 1 dòng; "Mỡ cơ thể và cách đo" ô chữ 1 dòng (không bắt buộc); "Cân nặng và chiều cao trước khi tập tạ" ô đoạn văn; "Link ảnh toàn thân" 1 ô link (thay 4 ô); bỏ Lịch rảnh, Dụng cụ.
  - **Hồ sơ** — `/hoc-vien/:id`, khối "Thông tin học viên": Mục tiêu tập luyện, Số buổi tập mỗi tuần, Giờ có thể ăn và giờ đi tập (PO chốt 2026-11-26).

### Bảng cách viết đã biết (bản đầu, PO duyệt)
| Mục | Cách viết trong file | Giá trị chuẩn |
|---|---|---|
| Phong cách giáo án muốn theo | PUSH PULL LEG, PUSH PULL LEGS, PPL | Push / Pull / Legs |
| Phong cách giáo án muốn theo | UPPER LOWER | Upper / Lower |
| Phong cách giáo án muốn theo | FULLBODY | Full body |
| Thời gian ngủ mỗi ngày | < 5 tiếng | Dưới 5 tiếng |
| Thời gian ngủ mỗi ngày | > 8 tiếng | Trên 8 tiếng |

---

## 4. Các phương án đã cân nhắc

- **Backfill lúc khởi động bằng C#** — loại: entity mới không còn khoá cũ, phải đọc JSON thô ngoài EF; chạy ở mọi lần khởi động thay vì một lần qua pipeline migrate.
- **Import chạy nền (job + trạng thái)** — loại: 500 dòng chỉ vài giây, thêm bảng job + polling là thừa.
- **MiniExcel / tự đọc OpenXML** — loại: MiniExcel trả giá trị đã ép kiểu, khó phân biệt ô ngày với ô số; tự đọc tốn mã cho shared strings/style ngày.

---

## 5. Implementation Map

| # | File (C/M) | Nội dung | BR / ADR |
|---|---|---|---|
| 1 | C `database/changelog/v0.4.0/**`; M `db.changelog-master.xml` | Chuyển dữ liệu hồ sơ cũ | BR-f02-import-excel-002, 008–011; ADR-f02-import-excel-002 |
| 2 | M `Coaching.Domain/Students/{RegistrationForm,RegistrationInput,RegistrationFields,RegistrationOptions}.cs`; C `RegistrationFieldError.cs`; tests `Coaching.Domain.Tests/Students/**`, fixtures `Coaching.TestSupport/RegistrationSamples.cs`, F02 acceptance steps | Form mới + Validate gom lỗi + Match | BR-f02-import-excel-001, 003–007, 030; ADR-f02-import-excel-001, 003, 006 |
| 3 | C `Coaching.Application/Students/StudentRegistrar.cs`; M `Commands/CreateStudent.cs` | Tách luồng tạo hồ sơ | ADR-f02-import-excel-007 |
| 4 | C `Coaching.Application/Students/Import/**`, `Coaching.Infrastructure/Import/ClosedXmlWorkbookReader.cs`; M `Directory.Packages.props`, Infrastructure csproj, `DependencyInjection.cs`, `StudentsController.cs` | Import API | BR-f02-import-excel-012–029, 031–034; ADR-f02-import-excel-004, 005, 007 |
| 5 | M `frontend/projects/admin/src/app/students/{registration-fields.ts,student-form.*,student-profile.*,students.api.ts,student.models.ts}`; C `import-card.component.ts` | Form mới, khối thông tin, card import | UI |
| 6 | M `docs/duobexiuuu-coaching-design.html`, `.kv/sdd/proto/AdTaoHoSo.md`, `AdHoSo.md` (trích lại) | Prototype theo form mới | UI |
| 7 | C `frontend/e2e/steps/import.steps.ts` + file mẫu `frontend/e2e/fixtures/*.xlsx` (sinh trong test); M e2e F02 steps | `@web` | — |

Thứ tự: 2 → 1 → 3 → 4 → 5, 6 → 7.

---

## 6. Test Strategy

### Acceptance / E2E
Scenario `@BR-f02-import-excel-*` (`form-dang-ky.feature`, `import-excel.feature`) và ví dụ F02 đã sửa: `@api` Reqnroll (file .xlsx sinh trong test bằng ClosedXML; `FakeTimeProvider` cho "hôm nay"), `@web` CucumberJS + Playwright upload file.

### Unit / Integration
| Kiểm | Ở đâu |
|---|---|
| BR-f02-import-excel-002, 008–011, NFR-f02-import-excel-004 (chuyển dữ liệu, chạy lại không đổi) | Integration: seed JSON cũ → chạy changeset → so kết quả; chạy lại → không đổi |
| Độ dài 201/2001, `availability` gửi lên bị bỏ qua (seed LOẠI) | Domain test + integration |
| Học viên / chưa đăng nhập gọi import (seed LOẠI) | Integration 403/401 |
| Dừng giữa chừng giữ hồ sơ đã tạo (seed LOẠI) | Integration: reader giả ném lỗi ở dòng 3 → 2 hồ sơ còn |
| `Validate` gom đủ lỗi, `Match` + bảng cách viết | Domain test |
| `ImportCellParser` (ngày Excel/chữ, giá, số thập phân) | Unit test |
| NFR-f02-import-excel-002 (không ghi file) | Review code: chỉ `IFormFile.OpenReadStream()`, không `Path`/`File` |

### Automated verification
```bash
cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"
cd frontend && npm run test:ci && npx ng build admin && npm run e2e
```

### Manual verification
- Import đúng file mẫu của PO trên local và staging; xem kết quả từng dòng.
- NFR-f02-import-excel-001: import 500 dòng trên staging, đo thời gian < 60 s.
- So màn Tạo hồ sơ (card import + form mới) và Hồ sơ với prototype đã sửa ở 390 px và 1280 px.

---

## 7. Risks & Mitigations

| Risk | Khả năng | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| Migration JSON sai trên staging, không rollback được | Thấp | Cao | Integration test với JSON thật; backup DB staging trước khi push (PO) |
| Đổi khoá JSON làm vỡ nhiều test F02/F01 | Cao | Trung bình | Task 2 sửa fixtures `RegistrationSamples` + steps F02 cùng lúc |
| Render cắt request dài | Thấp | Trung bình | 500 dòng ~10 s; đo ở manual verification |
| Coach đổi chữ tiêu đề Google Form | Trung bình | Thấp | Thiếu cột email/họ tên → lỗi rõ; cột khác bị bỏ qua và mục để trống |

---

## 8. Cross-Cutting Concerns

### Security
Route import thuộc `AdminOnly`, hồ sơ gắn `CoachId` coach hiện tại. File chỉ đọc trong bộ nhớ, giới hạn 5 MB trước khi đọc. Kết quả chỉ chứa số dòng, email dòng đó, tên cột, lý do.

### Performance
Mỗi dòng 1 transaction ngắn (tạo user không mật khẩu + `Student`); đọc file ~ vài trăm ms.

### Monitoring & Observability
Log Information: số dòng tạo/bỏ qua của mỗi lần import (không log nội dung ô).

### Backward Compatibility
`registration` trong DTO admin/client đổi khoá (ADR-001); web admin sửa cùng release; web học viên chưa đọc các mục này.

### Migration & Rollout
Liquibase `v0.4.0` chạy ở job `db-migrate` khi merge `main`. Dữ liệu staging hiện là dữ liệu thử; vẫn khuyến nghị backup.

---

## 9. Open Questions

| # | Câu hỏi | Chặn? | Owner | Trạng thái |
|---|---|---|---|---|
| OQ-01 | Duyệt "Bảng cách viết đã biết" (§3) | ❌ | PO | Bản đầu trong doc; bổ sung khi gặp file thật |
