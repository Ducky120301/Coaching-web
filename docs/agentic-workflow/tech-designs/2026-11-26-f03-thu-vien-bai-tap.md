---
doc-id: 2026-11-26-f03-thu-vien-bai-tap
status: ready
---

# Technical Design: F03 — Thư viện bài tập

## Meta

| Field | Value |
|-------|-------|
| Author | Hoangduc1203BK (soạn cùng Claude) |
| Date | 2026-11-26 |
| Spec | `docs/agentic-workflow/specs/2026-11-26-f03-thu-vien-bai-tap.md` |
| BDD | `@BR-f03-thu-vien-bai-tap-*` trong `features/thu-vien/bai-tap/thu-vien-bai-tap.feature` |
| JIRA | N/A — local-only |

---

## 1. Tổng quan

### Vấn đề
Chưa có thư viện bài tập; F05 (Soạn giáo án) cần nguồn bài để chọn. Prototype `AdThuVien` có danh sách + chi tiết nhưng chưa có form thêm/sửa và quản lý danh mục.

### Goals
- Aggregate `Exercise` + danh mục nhóm cơ/dụng cụ theo coach; API `/api/admin/exercises*`, `/api/admin/exercise-categories` cho BR-f03-thu-vien-bai-tap-001…021.
- Màn Thư viện (danh sách + chi tiết), Thêm/Sửa bài, thêm danh mục; seed ~99 bài prototype ở môi trường dev.
- Điểm cắm "bài đang được dùng" để F05 nối vào.

### Non-Goals
Theo spec §5 (thêm vào giáo án, "Đang dùng trong", bài thay thế, đổi tên/xoá danh mục, import, audit).

---

## 2. Bối cảnh & Kiến trúc tổng quan

### Hiện trạng
- Mẫu aggregate + repository + handler `XxxHandler.HandleAsync → Result<T>` của F02 (`backend/src/Coaching.Domain/Students`, `Coaching.Application/Students`, `Coaching.Api/Controllers/Admin/StudentsController.cs` kế thừa `AdminControllerBase`, policy AdminOnly).
- Tìm không dấu: `SearchText.Normalize` + cột `SearchKey` + `LIKE` (ADR-f02-ho-so-hoc-vien-003). Chặn ghi đè bằng `rowversion` + `version` base64 (ADR-f02-ho-so-hoc-vien-004).
- `CurrentCoach` lấy coach của người đăng nhập; dữ liệu lọc theo `CoachId`.
- Seed lúc khởi động: `Coaching.Infrastructure/Seeding/DatabaseInitializer.cs`. Schema qua Liquibase `database/changelog/vX`.
- FE admin: shell có rail (`frontend/projects/admin/src/app/shell/admin-shell.component.ts`, hiện có Dashboard, Tạo hồ sơ học viên); route tiếng Việt (ADR-f02-ho-so-hoc-vien-008).

### Kiến trúc tổng quan
N/A — gói trong API + web admin hiện có; YouTube chỉ là link, nhúng ở FE.

---

## 3. Giải pháp đề xuất

### Quyết định kiến trúc

- `ADR-f03-thu-vien-bai-tap-001` — Aggregate `Exercise` (Domain/Exercises): `CoachId`, `Name`, `NameKey`, `SearchKey`, `MuscleGroupId`, `EquipmentId`, `VideoUrl?`, `DefaultDose` (value object `Sets?`, `RepsMin?/RepsMax?`, `RirMin?/RirMax?`, `RestSeconds?`), `TechniqueNotes?`, `CommonMistakes?`, `IsHidden`, `Version` (rowversion). Unique index `(CoachId, NameKey, EquipmentId)`.
  - **Lý do:** Quy tắc tên/liều/video (BR-f03-thu-vien-bai-tap-002, 007, 009–014) nằm trong aggregate như F02; index unique chặn trùng kể cả khi hai request đua (BR-f03-thu-vien-bai-tap-001).
  - **Trade-off:** `NameKey` là cột dẫn xuất, chỉ aggregate được ghi.
- `ADR-f03-thu-vien-bai-tap-002` — `NameKey` = `SearchText.Normalize(name)` + đồng nhất các dấu gạch (‐ - – —) thành "-" + gộp khoảng trắng; `SearchKey` dùng cùng chuẩn hoá; danh mục dùng cùng hàm.
  - **Lý do:** Một định nghĩa "trùng" cho bài và danh mục (BR-f03-thu-vien-bai-tap-001, 006), dùng lại hàm bỏ dấu đã có.
  - **Trade-off:** Đổi quy tắc chuẩn hoá sau này cần backfill cột.
- `ADR-f03-thu-vien-bai-tap-003` — Danh mục là bảng `ExerciseCategories(Id, CoachId, Kind: MuscleGroup|Equipment, Name, NameKey, SortOrder)`, unique `(CoachId, Kind, NameKey)`. Danh mục ban đầu (BR-f03-thu-vien-bai-tap-005) được `DatabaseInitializer` tạo lúc khởi động cho mọi coach chưa có danh mục, ở mọi môi trường, idempotent.
  - **Lý do:** Một bảng đủ cho 2 loại chỉ có tên; danh mục ban đầu là quy tắc nghiệp vụ nên có cả ở production (khác seed bài tập).
  - **Trade-off:** Coach tạo sau lúc khởi động có danh mục ở lần khởi động kế tiếp — MVP chỉ 1 coach seed sẵn.
- `ADR-f03-thu-vien-bai-tap-004` — Điểm cắm `IExerciseUsage.IsUsedAsync(exerciseId, ct)` (Application). P1 đăng ký `NoExerciseUsage` (luôn `false`); `DeleteExerciseHandler` gọi nó, `true` → 409 `exercise.in_use`. F05 thay đăng ký bằng truy vấn giáo án thật.
  - **Lý do:** BR-f03-thu-vien-bai-tap-017 kiểm được ngay bằng test với bản giả trả `true`; F05 không phải sửa luồng xoá.
  - **Trade-off:** Interface chỉ có 1 cài đặt thật ở P1 — chấp nhận vì F05 chắc chắn thêm cài đặt thứ hai.
- `ADR-f03-thu-vien-bai-tap-005` — Link video kiểm ở Domain: URI tuyệt đối, scheme http/https, host là `youtube.com`, `youtu.be` hoặc kết thúc bằng `.youtube.com`; lưu nguyên văn. FE chỉ nhúng (`youtube-nocookie.com/embed/<id>`) khi rút được mã video 11 ký tự, còn lại chỉ hiện link mở YouTube.
  - **Lý do:** Đáp ứng BR-f03-thu-vien-bai-tap-007 (mọi link YouTube) mà không nhúng URL tuỳ ý vào iframe.
  - **Trade-off:** Link playlist/kênh không xem tại chỗ được.
- `ADR-f03-thu-vien-bai-tap-006` — API nhận liều mặc định có cấu trúc (`sets`, `repsMin`, `repsMax`, `rirMin`, `rirMax`, `restSeconds`); FE nhận chữ "10–12"/"10-12"/"10", tách thành min/max và hiển thị lại "10–12", "90s", trống là "—".
  - **Lý do:** F05 dùng lại được số (spec OQ-03); kiểm khoảng ở Domain (BR-f03-thu-vien-bai-tap-009–012).
  - **Trade-off:** Phân tích chữ nằm ở FE (và step test), không ở API.
- `ADR-f03-thu-vien-bai-tap-007` — 99 bài prototype (đã gán theo Phụ lục) là file JSON nhúng trong Infrastructure (`Seeding/exercises.seed.json`); `ExerciseLibrarySeeder` được `DatabaseInitializer` gọi ở mọi môi trường và chỉ nạp cho coach có thư viện trống. Cấu hình `Seed:ExerciseLibrary=false` tắt bước này cho host test.
  - **Lý do:** NFR-f03-thu-vien-bai-tap-003 (thư viện thật của coach, PO đổi 2026-11-27); điều kiện "thư viện trống" đảm bảo không nạp lại, không ghi đè.
  - **Trade-off:** Coach xoá hết bài thì lần khởi động sau sẽ nạp lại 99 bài — chấp nhận vì coach không có lý do xoá toàn bộ thư viện.

### Component Design
- **Domain** `Coaching.Domain/Exercises/`: `Exercise`, `ExerciseDose`, `ExerciseCategory`, `CategoryKind`, `ExerciseNameKey`, `YouTubeUrl`, `IExerciseRepository`, `IExerciseCategoryRepository`.
- **Application** `Coaching.Application/Exercises/`: `Commands/{CreateExercise, UpdateExercise, HideExercise, UnhideExercise, DeleteExercise, AddCategory}`, `Queries/{ListExercises, GetExercise, ListCategories}`, `ExerciseDtos`, `ExerciseErrors`, `IExerciseUsage` + `NoExerciseUsage`.
- **Infrastructure**: configuration + repository (mẫu `StudentRepository`), `Seeding/ExerciseCatalogSeeder` (danh mục mọi môi trường), `Seeding/DevExerciseSeeder` + `Seeding/exercises.dev.json`.
- **Api**: `Controllers/Admin/ExercisesController.cs`, `ExerciseCategoriesController.cs` (AdminOnly; học viên 403 — NFR-f03-thu-vien-bai-tap-002).

### API Contracts
| Method + route | Body / query | Thành công | Lỗi |
|---|---|---|---|
| `GET /api/admin/exercises` | `q`, `muscleGroupId`, `equipmentId`, `hidden` (mặc định false) | `200 [{ id, name, muscleGroup, equipment, hasVideo, isHidden }]` sắp theo tên | — |
| `GET /api/admin/exercises/{id}` | — | `200 ExerciseDetailDto` (mọi trường + `version`) | 404 `exercise.not_found` |
| `POST /api/admin/exercises` | `{ name, muscleGroupId, equipmentId, videoUrl?, sets?, repsMin?, repsMax?, rirMin?, rirMax?, restSeconds?, techniqueNotes?, commonMistakes? }` | `201 ExerciseDetailDto` | 409 `exercise.duplicate` "Đã có bài cùng tên và dụng cụ." (+ `hiddenExerciseId` khi bài trùng đang ẩn); 400 `exercise.name_required`, `exercise.name_too_long`, `exercise.muscle_group_required`, `exercise.equipment_required`, `exercise.category_not_found`, `exercise.video_not_youtube`, `exercise.sets_out_of_range`, `exercise.reps_invalid`, `exercise.rir_invalid`, `exercise.rest_out_of_range`, `exercise.notes_too_long` |
| `PUT /api/admin/exercises/{id}` | như POST + `version` | `200 ExerciseDetailDto` | như POST; 409 `exercise.version_conflict` "Bài vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất." |
| `POST /api/admin/exercises/{id}/hide`, `/unhide` | — | `200 ExerciseDetailDto` | 404 |
| `DELETE /api/admin/exercises/{id}` | — | `204` | 409 `exercise.in_use` "Bài đang có trong giáo án, chỉ ẩn được."; 404 |
| `GET /api/admin/exercise-categories` | — | `200 { muscleGroups: [{id,name}], equipment: [{id,name}] }` theo `SortOrder` | — |
| `POST /api/admin/exercise-categories` | `{ kind: "MuscleGroup"\|"Equipment", name }` | `201 {id, kind, name}` | 409 `category.duplicate` "Đã có mục cùng tên."; 400 `category.name_required` |

### Data Model / Database Changes
Liquibase `database/changelog/v0.5.0/`: bảng `ExerciseCategories` và `Exercises` (cột theo ADR-001/003; `Name nvarchar(100)`, `NameKey/SearchKey nvarchar(200)`, `VideoUrl nvarchar(2048)`, `TechniqueNotes/CommonMistakes nvarchar(2000)`, liều là cột `int NULL`; FK `MuscleGroupId/EquipmentId → ExerciseCategories`, `CoachId → Coaches`; index `(CoachId, IsHidden)`). Rollback xoá 2 bảng.

### Thiết kế giao diện (UI)
- **Design link**: `docs/duobexiuuu-coaching-design.html`, page `AdThuVien` (extract `.kv/sdd/proto/AdThuVien.md`). Token, font theo CLAUDE.md; 880 px; kiểm 390/1280 px.
- **Màn hình**:
  - **Thư viện bài tập** — `/thu-vien/bai-tap`, mục rail "Thư viện bài tập" (icon như prototype). Theo `AdThuVien`: H1 46 px + dòng tóm tắt; chip nhóm cơ (40 px) cho 11 nhóm kèm số bài; chip dụng cụ nhỏ (34 px); ô tìm; bảng cột Bài tập, Nhóm cơ, Dụng cụ, Video mẫu (nhãn "Xem video" `--okBg` / "Thiếu video" `--warnBg`); aside chi tiết bài (H2 31 px, video nhúng 16:9 hoặc 9:16 cho shorts, Sets/Reps/RIR/Nghỉ, hướng dẫn kỹ thuật, lỗi hay gặp). Bỏ cột "Đang dùng trong" và nút "Thêm vào giáo án" (F05). *Suy ra*: chip "Đã ẩn" ở cuối hàng chip nhóm cơ để xem bài đã ẩn (BR-f03-thu-vien-bai-tap-016); trong aside thêm nút phụ "Ẩn bài"/"Bỏ ẩn" và nút chữ "Xoá bài" (hỏi xác nhận); nút chính "Thêm bài mới" cạnh H1; "Sửa bài" mở trang sửa.
  - **Thêm / Sửa bài** — `/thu-vien/bai-tap/moi`, `/thu-vien/bai-tap/:id/sua`. *Suy ra* (prototype chưa có): kiểu form `AdTaoHoSo` (nhãn 14 px 600, ô 46 px radius 10, viền `--field`); ô: Tên bài, Nhóm cơ (chọn), Dụng cụ (chọn), Link video YouTube, Sets, Reps ("10–12"), RIR ("1–2"), Nghỉ (giây), Hướng dẫn kỹ thuật, Lỗi hay gặp; dưới mỗi ô chọn có link "+ Thêm nhóm cơ" / "+ Thêm dụng cụ" mở ô nhập nhỏ tại chỗ (BR-f03-thu-vien-bai-tap-006). Nút "Lưu bài" và "Huỷ". Trùng với bài đang ẩn → thông báo kèm nút "Bỏ ẩn bài đó". 409 phiên bản → "Bài vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất." + "Tải lại".

---

## 4. Các phương án đã cân nhắc
- **Danh mục là enum cố định trong code** — loại: coach phải thêm được mục mới (BR-f03-thu-vien-bai-tap-006).
- **Chỉ ẩn ở P1, xoá thật để F05** — loại theo PO (spec OQ-02); điểm cắm ADR-004 giữ an toàn khi F05 tới.
- **Lưu liều dạng chữ** — loại: F05 cần số (spec OQ-03).

---

## 5. Implementation Map
| # | File (C/M) | Nội dung | BR / ADR |
|---|---|---|---|
| 1 | C `database/changelog/v0.5.0/**`; M master | 2 bảng | ADR-001, 003 |
| 2 | C `Coaching.Domain/Exercises/**` + tests | Aggregate, liều, tên, video, danh mục | BR-001–004, 007–014, 021; ADR-001, 002, 005 |
| 3 | C `Coaching.Infrastructure/Persistence/Configurations/{Exercise,ExerciseCategory}Configuration.cs`, `Repositories/Exercise*Repository.cs`, `Seeding/ExerciseCatalogSeeder.cs`; M `AppDbContext`, DI, `DatabaseInitializer` | Persist + danh mục ban đầu | BR-005; ADR-003 |
| 4 | C `Coaching.Application/Exercises/**`, `Coaching.Api/Controllers/Admin/Exercise*Controller.cs` | Use case + API | BR-001, 006, 015–020; ADR-004, 006 |
| 5 | C `Seeding/DevExerciseSeeder.cs`, `Seeding/exercises.dev.json` | Seed dev | NFR-003; ADR-007 |
| 6 | C `frontend/projects/admin/src/app/exercises/**`; M `app.routes.ts`, `admin-shell.component.ts` | 2 màn + rail | ADR-005, 006 (FE) |
| 7 | C `frontend/e2e/steps/exercise-library.steps.ts`; M `CLAUDE.md` (route) | `@web` | — |

Thứ tự: 1 → 2 → 3 → 4 → 5 → 6 → 7.

---

## 6. Test Strategy
### Acceptance / E2E
Scenario `@BR-f03-thu-vien-bai-tap-*`: `@api` Reqnroll; `@web` CucumberJS + Playwright; 2 scenario `@manual` chờ F05.
### Unit / Integration
| Kiểm | Ở đâu |
|---|---|
| Seed LOẠI: tên 101, ghi chú 2001 ký tự, Sets "abc", danh mục trống, nhóm cơ/dụng cụ không thuộc danh mục | Domain test + integration 400 |
| BR-f03-thu-vien-bai-tap-017 bài đang dùng | Integration với `IExerciseUsage` giả trả `true` → 409 |
| Đua tạo trùng | Integration: 2 request song song → 1 tạo, 1 409 |
| NFR-f03-thu-vien-bai-tap-002 | Integration: học viên 403; coach khác 404 |
| NFR-f03-thu-vien-bai-tap-003 | Integration: host Production với thư viện trống được nạp 99 bài; khởi động lại không nhân đôi; coach đã có bài thì không nạp |
| NFR-f03-thu-vien-bai-tap-001 | Manual: 500 bài, đo p95 danh sách/tìm |
### Automated verification
```bash
cd backend && dotnet test Coaching.slnx --filter "Category!=web&Category!=manual"
cd frontend && npm run test:ci && npx ng build admin && npm run e2e
```
### Manual verification
So 2 màn với prototype ở 390/1280 px; đo p95 với 500 bài.

---

## 7. Risks & Mitigations
| Risk | Khả năng | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| Gán nhóm cơ cho bài seed sai ý coach | Trung bình | Trung bình | PO duyệt bảng Phụ lục; coach sửa được từng bài sau khi nạp |
| F05 quên nối `IExerciseUsage` | Thấp | Cao | Dependencies trong spec F03 + test hợp đồng ở F05 |

---

## 8. Cross-Cutting Concerns
### Security
AdminOnly; lọc `CoachId`; video chỉ nhúng qua `youtube-nocookie.com/embed/<id>` đã kiểm.
### Performance
Danh sách 1 truy vấn theo `(CoachId, IsHidden)` + LIKE `SearchKey`; đủ cho 500 bài.
### Monitoring & Observability
N/A — log mặc định.
### Backward Compatibility
N/A — API mới.
### Migration & Rollout
Liquibase v0.5.0 khi merge `main`; danh mục ban đầu và 99 bài của coach được nạp lúc khởi động ở mọi môi trường (chỉ khi còn trống).

---

## 9. Open Questions
| # | Câu hỏi | Chặn? | Owner | Trạng thái |
|---|---|---|---|---|
| OQ-01 | Duyệt bảng gán nhóm cơ cho bài seed (Phụ lục), nhất là "Butterfly Raise" → Vai sau, "Hip Adduction" → Đùi trước | ❌ | PO | PO duyệt toàn bộ bảng gán và các đề xuất UI (2026-11-26) |

---

## Phụ lục — Gán bài seed sang danh mục mới
99 bài trong prototype; 44 bài đổi nhóm cơ/dụng cụ như bảng dưới, 55 bài giữ nguyên (Ngực, Vai, Lưng, Mông, Bụng). Liều mặc định của prototype ("3", "10–12", "1–2", "90s") nạp đúng dạng có cấu trúc; bài có "—" để trống.

| Bài | Prototype (nhóm / dụng cụ) | Gán mới | Ghi chú |
|---|---|---|---|
| Butterfly Raise | Vai / Cable | Vai sau / Cable |  |
| Facepull with External Rotation | Vai / Cable | Vai sau / Cable |  |
| Reardelt Fly | Vai / Cable | Vai sau / Cable |  |
| Reverse Pec Deck | Vai / Machine | Vai sau / Machine |  |
| 15° Incline Curl | Tay / Dumbbell | Tay trước / Dumbbell |  |
| Barbell Curl | Tay / Barbell | Tay trước / Barbell |  |
| Bayesian Curl | Tay / Cable | Tay trước / Cable |  |
| Cable Curl (Straight Bar) | Tay / Cable | Tay trước / Cable |  |
| Close-grip Bench Press | Tay / Barbell | Tay sau / Barbell |  |
| EZ Bar Skull Crusher | Tay / Barbell | Tay sau / Barbell |  |
| Hammer Curl | Tay / Dumbbell | Tay trước / Dumbbell |  |
| JM Press | Tay / Smith Machine | Tay sau / Smith Machine |  |
| One-arm Triceps Extension | Tay / Cable | Tay sau / Cable |  |
| Overhead Triceps Extension | Tay / Cable | Tay sau / Cable |  |
| Preacher Curl | Tay / Dumbbell | Tay trước / Dumbbell |  |
| Recline Curl | Tay / Dumbbell | Tay trước / Dumbbell |  |
| Seated Dumbbell Overhead Triceps Extension | Tay / Dumbbell | Tay sau / Dumbbell |  |
| Spider Curl | Tay / Dumbbell | Tay trước / Dumbbell |  |
| Triceps Pushdown | Tay / Cable | Tay sau / Cable |  |
| Barbell Back Squat | Đùi / Barbell | Đùi trước / Barbell |  |
| Bulgarian Split Squat | Đùi / Dumbbell | Đùi trước / Dumbbell |  |
| Conventional Deadlift | Đùi / Barbell | Đùi sau / Barbell |  |
| Dumbbell Walking Lunge | Đùi / Dumbbell | Đùi trước / Dumbbell |  |
| Front Squat | Đùi / Barbell | Đùi trước / Barbell |  |
| Goblet Squat | Đùi / Dumbbell | Đùi trước / Dumbbell |  |
| Hack Squat | Đùi / Machine | Đùi trước / Machine |  |
| Hip Adduction | Đùi / Machine | Đùi trước / Machine | đùi trong → Đùi trước |
| Leg Curl | Đùi / Machine | Đùi sau / Machine |  |
| Leg Extension | Đùi / Cable | Đùi trước / Cable |  |
| Leg Press | Đùi / Machine | Đùi trước / Machine |  |
| Nordic Hamstring Curl | Đùi / Bodyweight | Đùi sau / Bodyweight |  |
| Pendulum Squat | Đùi / Machine | Đùi trước / Machine |  |
| Romanian Deadlift | Đùi / Smith Machine | Đùi sau / Smith Machine |  |
| Seated Leg Curl | Đùi / Machine | Đùi sau / Machine |  |
| Static Lunges - Front Feet-elevated | Đùi / Dumbbell | Đùi trước / Dumbbell |  |
| Decline Crunch | Bụng / Weighted | Bụng / Bodyweight | Weighted → Bodyweight |
| Russian Twist | Bụng / Weighted | Bụng / Bodyweight | Weighted → Bodyweight |
| Calves Raise | Bắp chuối / Smith Machine | Bắp chân / Smith Machine |  |
| Donkey Calf Raise | Bắp chuối / Machine | Bắp chân / Machine |  |
| Leg Press Calf Raise | Bắp chuối / Machine | Bắp chân / Machine |  |
| Seated Calf Raise | Bắp chuối / Machine | Bắp chân / Machine |  |
| Single-leg Dumbbell Calf Raise | Bắp chuối / Dumbbell | Bắp chân / Dumbbell |  |
| Standing Calf Raise | Bắp chuối / Machine | Bắp chân / Machine |  |
| Tibialis Raise | Bắp chuối / Bodyweight | Bắp chân / Bodyweight |  |