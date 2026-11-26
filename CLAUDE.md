# CLAUDE.md — Duobexiuuu Coaching

Hệ thống quản lý coaching gym online gồm **2 website dùng chung 1 backend**:
- **Web Admin** (coach): tạo hồ sơ học viên, soạn & phát hành phase giáo án, xử lý review tuần, chuyển phase.
- **Web Học viên** (client, responsive, ưu tiên điện thoại): xem việc hôm nay, ghi set, check-in, nộp review, xem tiến độ.

Tài liệu gốc (source of truth):
- Nghiệp vụ: `docs/Ke-hoach-nghiep-vu-website-coaching.docx`
- Design/prototype: `docs/duobexiuuu-coaching-design.html` — **FE phải bám chính xác** (xem mục Frontend).
- `docs/ke-hoach-trien-khai-chatbot-chung-khoan.md` là của dự án khác — **bỏ qua**.

Lộ trình: **P0** codebase → **P1** Admin hồ sơ & thư viện → **P2** Admin soạn & phát hành giáo án (+ import Excel cuối P2) → **P3** Admin dashboard, review, chuyển phase → **P4** Client nhận giáo án → **P5** Client ghi tập, check-in, review, tiến độ + chạy thử. Làm hết Admin trước rồi mới tới Client.

---

## Tech stack

| Tầng | Công nghệ |
|---|---|
| Backend | .NET 10, ASP.NET Core Web API, Clean Architecture + DDD, EF Core |
| Database | SQL Server 2022 (Express trên production) |
| Auth | ASP.NET Core Identity + JWT (access token ngắn hạn + refresh token), role `Admin` / `Student` |
| Frontend | Angular (1 workspace, 2 app + 1 lib dùng chung) |
| Email | `IEmailSender` → Resend (free tier), dự phòng Amazon SES |
| CI/CD | GitHub Actions (theo mẫu Healthy app): test → đẩy image API lên GHCR → Liquibase update DB staging → gọi deploy hook Render (API = image, 2 app Angular = static site). DB staging: database hosted dùng lại từ Healthy app. Mọi push chạy backend + frontend + kiểm changelog DB; merge vào `main` mới migrate DB staging và deploy |
| Acceptance | Reqnroll (.NET) cho @api/@integration; CucumberJS + Playwright cho @web; so ảnh pixelmatch với prototype |
| Hosting production | Chưa chốt (PO báo sau) — code phải chạy được bằng Docker trên VPS Linux bất kỳ |

## Cấu trúc repo

```
/backend
  Coaching.slnx
  src/
    Coaching.Domain/          # Entity, Aggregate, Value Object, Domain Event, Domain Service — KHÔNG phụ thuộc gì
    Coaching.Application/     # Use case (Command/Query + Handler), DTO, interface port (IEmailSender, IClock, IAppDbContext...)
    Coaching.Infrastructure/  # EF Core DbContext + Configurations + Migrations, Identity, Email, Clock, Audit
    Coaching.Api/             # 1 host duy nhất: Controllers/Endpoints chia /api/admin/* và /api/client/*
  tests/
    Coaching.Domain.Tests/
    Coaching.Application.Tests/
    Coaching.Api.IntegrationTests/
/frontend
  angular.json
  projects/
    admin/                    # Web Admin (desktop-first)
    client/                   # Web Học viên (mobile-first)
    shared/                   # lib: design tokens, UI components, auth, http interceptor, models
/deploy
  docker-compose.yml          # sqlserver, api, nginx (serve admin + client) — dùng cho dev local và production VPS
  nginx/
  render.yaml                 # blueprint môi trường test trên Render
/.github/workflows/           # CI build + test, CD deploy Render
/docs
  PRD-coaching-web.md         # PRD tổng
  prd/F00…F12-*.md            # PRD theo tính năng, business rules BR-xx
```

Business rules nằm trong `docs/prd/*.md` với mã BR-xx cố định. Code và test nên tham chiếu đúng mã BR (ví dụ tên test `BR33_PhaseStart_MustBeAfterPreviousEnd`).

## Backend — kiến trúc & quy ước

### Một backend, hai nhóm route
- **1 API host**, 2 nhóm route: `/api/admin/*` (policy `AdminOnly`) và `/api/client/*` (policy `StudentOnly`).
- Domain/Application/Infrastructure **dùng chung**. Không nhân đôi logic nghiệp vụ cho 2 phía. Khác biệt giữa 2 phía chỉ nằm ở tầng Api (endpoint, DTO trả về, phân quyền).
- Thiết kế sao cho sau này tách được thành `Admin.Api` + `Client.Api` mà không phải sửa Domain/Application.

### Dependency rule (bắt buộc)
`Api → Application → Domain`; `Infrastructure → Application, Domain`. Domain **không** tham chiếu EF Core, ASP.NET hay bất kỳ package hạ tầng nào.

### DDD
- Aggregate dự kiến: `Coach`, `Student` (hồ sơ + trạng thái tài khoản + trạng thái coaching), `Program`, `Phase` (chứa `Week`, `PlanVersion`, `Session`, `ExerciseItem`, `Meal`, `Supplement`, chỉ tiêu check-in), `Exercise` (thư viện), `Food` (thư viện), `WorkoutLog`, `CheckIn`, `WeeklyReview` (chứa các bản nộp).
- Invariant nằm **trong aggregate** (method có tên nghiệp vụ: `Publish()`, `Pause()`, `CreateVersion(effectiveDate, reason)`…). Không set property trực tiếp từ bên ngoài. Không dùng anemic model.
- Chỉ tham chiếu aggregate khác bằng Id.
- Value Object cho: `Email`, `DateRange`, `RepRange`, `Rir`/`Rpe` (RIR + RPE = 10), `Macro` (C/F/P/kcal), `Weight`…
- Trạng thái dùng enum + chuyển trạng thái có kiểm tra:
  - Phase: `Draft → Scheduled → Running → Ended`, cộng thêm `Paused` ↔ `Running`. Mỗi chương trình có tối đa 1 phase `Running`. Phase **không tự kết thúc**: coach bấm Kết thúc. Phase `Scheduled` chỉ tự chạy khi không còn phase `Running`/`Paused` (BR-37, BR-39, BR-43).
  - Review: `Draft → Submitted → Viewed → Responded`, cộng thêm `NeedsMoreInfo`. Mỗi lần nộp lại tạo một bản mới.
  - Tài khoản: `PendingActivation → Activated`. Coaching: `Active / Paused / Ended`.
- **Phiên bản giáo án**: chỉnh phase đang chạy thì tạo `PlanVersion` mới (ngày hiệu lực ≥ hôm nay, lý do, người sửa). Buổi đã ghi giữ chỉ tiêu của phiên bản lúc tập. Giáo án lưu **bản chụp (snapshot)** bài tập/thực phẩm, không tham chiếu sống tới thư viện.
- Review đã nộp lưu **bản chụp số liệu** tại thời điểm nộp.

### Thời gian
- Mọi phép tính ngày/tuần dùng `IClock` theo **giờ Việt Nam (UTC+7)**. DB lưu `DateTimeOffset`/UTC; ngày nghiệp vụ dùng `DateOnly`.
- Tuần thứ n = 7 ngày bắt đầu từ `phaseStart + 7*(n-1)`.
- Tuyệt đối không gọi `DateTime.Now` trong Domain/Application.

### Bảo mật & truy vết
- Học viên chỉ truy cập dữ liệu của mình. Kiểm tra ở tầng Application/query (lọc theo `StudentId` từ token), **không** chỉ ẩn trên UI.
- Ghi chú nội bộ của coach không bao giờ có trong DTO của `/api/client`.
- Mật khẩu tạm (hết hạn sau 72h) và token đặt lại mật khẩu (30 phút, dùng 1 lần) chỉ lưu dưới dạng hash.
- Audit log (người thực hiện + thời điểm) cho: phát hành, đổi trạng thái phase, sửa chỉ tiêu, gửi phản hồi, đổi trạng thái coaching.
- Mỗi Student có `CoachId` (MVP chỉ có 1 admin được seed) để sau này mở nhiều coach.

### Quy ước code
- Use case theo CQRS nhẹ: `Commands/` và `Queries/`, mỗi use case gồm 1 handler + 1 validator.
- Lỗi nghiệp vụ → `DomainException`/Result, map ra `ProblemDetails` (400/404/409). Không trả stack trace.
- EF Core: mỗi entity 1 `IEntityTypeConfiguration`, migration đặt tên có nghĩa. Không dùng lazy loading.
- Test: Domain test cho mọi invariant/business rule; integration test cho phân quyền (học viên A không đọc được dữ liệu học viên B, học viên không gọi được `/api/admin`).
- Target hiệu năng: API p95 < 500ms.

---

## Frontend — Angular

### Workspace
- `projects/admin`: desktop-first, layout có rail bên trái.
- `projects/client`: mobile-first. Dưới 880px dùng tab bar dưới đáy; từ 880px trở lên dùng rail bên trái.
- `projects/shared`: design tokens (CSS variables), component UI dùng chung, `AuthService`, `authGuard`, HTTP interceptor (đính JWT, refresh token), model/DTO.
- Standalone components, Signals, lazy-load theo feature route.

### ⚠️ BẮT BUỘC: bám chính xác design prototype
`docs/duobexiuuu-coaching-design.html` là **nguồn chuẩn duy nhất** về giao diện. Kích thước, màu sắc, font, cỡ chữ, khoảng cách, bo góc, chiều cao control, layout và câu chữ phải **giống prototype**. Không tự "cải tiến", không dùng giá trị mặc định của thư viện UI (Material/Bootstrap…) nếu nó làm lệch design.

**Cách làm khi dựng một màn hình:**
1. Mở prototype, tìm page key tương ứng trong `window.__PAGES__` (bảng dưới). Mỗi page có `css`, `tpl` (HTML có inline style) và `factory` (logic + object style `S`).
2. Sao chép **đúng** giá trị inline style (px, màu `var(--…)`, font-weight, gap, padding, radius) sang SCSS của component.
3. Giữ nguyên text tiếng Việt, thứ tự field, trạng thái rỗng/lỗi/thành công như prototype.
4. So sánh trực quan với prototype ở 2 độ rộng **390px** và **≥ 1280px** trước khi coi là xong.

| Page key | Màn hình | App | Route gợi ý |
|---|---|---|---|
| `Main` | Đăng nhập, kích hoạt, quên mật khẩu, đặt lại mật khẩu | cả 2 (3 màn sau chỉ client) | `/login`, `/kich-hoat`, `/quen-mat-khau`, `/dat-lai-mat-khau#t=<token>` |
| `HvHomNay` | HV01 Hôm nay | client | `/today` |
| `HvLich` | HV02 Lịch và giáo án | client | `/schedule` |
| `HvNhatKy` | HV03 Nhật ký buổi tập | client | `/sessions/:id/log` |
| `HvCheckIn` | HV04 Check-in ngày | client | `/check-in` |
| `HvReview` | HV05 Review tuần | client | `/reviews/:week` |
| `HvDinhDuong` | HV06 Dinh dưỡng | client | `/nutrition` |
| `HvThuatNgu` | Thuật ngữ | client | `/glossary` |
| `HvTienDo` | HV07 Tiến độ | client | `/progress` |
| `Admin-Dashboard` | AD02 Dashboard | admin | `/dashboard` |
| `Admin-Review` | AD06 Xử lý review tuần | admin | `/students/:id/reviews/:week` |
| `AdTaoHoSo` | AD01 Tạo hồ sơ học viên | admin | `/hoc-vien/moi` |
| `AdHoSo` | AD01/AD03 Hồ sơ học viên | admin | `/hoc-vien/:id` (sửa: `/hoc-vien/:id/sua`) |
| `AdSoanGiaoAn` | AD04 Soạn giáo án | admin | `/students/:id/phases/:phaseId/edit` |
| `AdThemBuoi` | AD04 Thêm buổi tập | admin | `/students/:id/phases/:phaseId/sessions/new` |
| `AdThuVien` | Thư viện bài tập | admin | `/thu-vien/bai-tap` (thêm: `/thu-vien/bai-tap/moi`, sửa: `/thu-vien/bai-tap/:id/sua`) |
| `AdPhatHanh` | AD05 Phát hành phase | admin | `/students/:id/phases/:phaseId/publish` |
| `AdChuyenPhase` | AD07 Tổng kết & chuyển phase | admin | `/students/:id/phases/:phaseId/close` |

Route web admin dùng tiếng Việt (ADR-f02-ho-so-hoc-vien-008): đã có `/dashboard` (trang đích), `/hoc-vien/moi`, `/hoc-vien/:id`, `/hoc-vien/:id/sua`, `/thu-vien/bai-tap` (`?bai=<id>` chọn sẵn bài), `/thu-vien/bai-tap/moi`, `/thu-vien/bai-tap/:id/sua`. Route của màn chưa làm trong bảng vẫn là gợi ý — khi làm, đặt tiếng Việt dưới tiền tố `/hoc-vien/:id/…` (màn theo học viên) hoặc `/thu-vien/…` (thư viện dùng chung).

Route web học viên dùng tiếng Việt (ADR-f01-p1-tai-khoan-hoc-vien-010): đã có `/login`, `/kich-hoat` (mã kích hoạt chỉ trong bộ nhớ), `/quen-mat-khau`, `/dat-lai-mat-khau#t=<token>` (token sau `#`), `/hom-nay`. Route học viên chưa làm trong bảng vẫn là gợi ý — khi làm, đặt tiếng Việt.

Dữ liệu mẫu (giáo án, dinh dưỡng, thuật ngữ) có trong `window.__CLIENT__` của prototype, dùng làm seed/dev data.

### Design tokens (lấy nguyên văn từ prototype — đặt trong `shared/styles/_tokens.scss` dưới dạng CSS variables)

```css
:root {
  --bg:#F7F1F4; --surface:#FFFFFF; --line:#EBDDE4; --field:#CDB6C1; --soft:#FDE3EE;
  --primary:#C4145E; --onPrimary:#FFFFFF; --ink:#24121D; --muted:#6D5662; --ink2:#4B3340;
  --faint:#8E7682; --chrome:#FFFFFF; --tableBg:#FFFFFF; --rowLine:#F1E5EB;
  --ok:#1F7A4D; --okBg:#E1F2E7; --okInk:#155A37; --onOk:#FFFFFF;
  --warn:#9A5608; --warnBg:#FBF0DC; --alert:#C0364F;
  --navInk:#8C0E45; --linkHover:#8C0E45; --dashed:#E2A9C2; --pausedBg:#EEE8EB;
  --coverBg:#A07892; --coverInk:#FCEFF4; --coverMuted:#C9A9B8; --sig:#FF5C9D; --rowHover:#FCEFF4;
}
```
Không hard-code mã màu trong component. Luôn dùng `var(--…)`.

**Brand override (áp dụng toàn cục trong prototype, bắt buộc giữ):**
- `body { background:#F7F1F4 }`
- Rail (sidebar) **nền tối** `#24121D`, chữ `#FCEFF4`, link `#D9C2CD`; hover: chữ `#fff`, nền `rgba(255,255,255,.06)`.
- Link active trong rail: nền `rgba(255,92,157,.16)`, chữ `#fff`, `box-shadow: inset 3px 0 0 #FF5C9D`. Chữ phụ trong rail `#B79AA8`. Focus outline `#FF5C9D`.
- Số liệu dùng font Barlow Condensed với `font-variant-numeric: tabular-nums`.

**Typography** (Google Fonts: `Barlow Condensed` 600/700/800, `Be Vietnam Pro` 400/500/600, `Yellowtail`):
- Body / UI: `'Be Vietnam Pro', sans-serif`.
- Tiêu đề & số liệu lớn: `'Barlow Condensed', 'Be Vietnam Pro', sans-serif`, weight 800, `line-height:1`, `letter-spacing:-.01em`.
  - H1 trang: ≥ 880px lấy **đúng cỡ của từng page trong prototype** (vd. Admin-Dashboard 48px, AdTaoHoSo 46px, AdHoSo 43px); < 880px dùng **38px**.
- Logo chữ ký "duobexiuuu": `'Yellowtail', cursive`, 34px, màu `var(--sig)`, `line-height:1`.
- Thang cỡ chữ đang dùng: 11 · 11.5 · 12 · 13 · 13.5 · **14 (mặc định)** · 15 · 16 · 20 · 22 · 24 · 26 · 34 · 48px. Không thêm cỡ ngoài thang này nếu prototype không có.
- Font-weight: 400 / 500 / **600 (nhấn chính)** / 700 / 800 (heading). Line-height đoạn văn 1.5–1.6.
- Link: `color: var(--primary)`, hover `var(--navInk)`.

**Kích thước & hình khối:**
- Bo góc: `999px` (nút/chip/pill) · `14px` (card lớn) · `12px` (card, nav item) · `10px` (input, ô nhỏ) · `50%` (avatar).
- Chiều cao control: **44px** (nút chính, chip, input mặc định) · 48/50px (input/nút form lớn, login) · 36–40px (nút phụ, control trong bảng) · avatar 36px.
- Nút chính: `height:44px; border-radius:999px; background:var(--primary); color:var(--onPrimary); font-weight:600; font-size:14px`.
- Chip: 44px, `padding:0 16px`, radius 999px. Bật: nền primary + chữ trắng, không viền. Tắt: nền `var(--surface)`, `border:1px solid var(--field)`.
- Nav item rail: `padding:12px; border-radius:12px; gap:12px; font-size:15px`, icon 20×20 stroke 2.
- Tab bar mobile: item dạng cột, `gap:4px; padding:8px 0; font-size:12px`. Active: `var(--primary)` + 600; còn lại `var(--muted)`.
- Viền/khung: `1px solid var(--line)`; field: `var(--field)`; dòng bảng: `var(--rowLine)`, hover `var(--rowHover)`.
- Focus: `outline:2px solid var(--primary); outline-offset:2px` cho a/button/input/select/textarea/summary.
- Placeholder: `color: var(--faint)`.
- Khoảng cách thường dùng: 4 · 6 · 8 · 10 · 12 · 16 · 20 · 22 · 24 · 28px; padding cell `7px 10px`, card `16px 18px`, input `11px 12px`/`11px 14px`.

**Layout & responsive:**
- **Breakpoint duy nhất: 880px** (`wide = innerWidth >= 880`). Dùng đúng mốc này, không dùng breakpoint mặc định của framework.
- Client, rộng (≥ 880px): rail `flex: 0 0 240px; padding: 28px 16px; gap: 28px`, main `padding: 40px 48px 56px`.
- Client, hẹp (< 880px): layout cột, main `padding: 16px 20px 24px`, tab bar dưới đáy.
  - Thanh hành động sticky đáy: `margin: 0 -20px -24px; padding: 12px 20px 20px; background: var(--chrome); border-top: 1px solid var(--line)`.
- Admin rail: `flex: 1 1 220px; min-width: 220px; max-width: 260px; padding: 24px 16px; gap: 28px`, dưới logo có dòng "Khu vực coach" (13px, `var(--muted)`).
- Main: `max-width: 1160px; display:flex; flex-direction:column; gap:20px`.
- Lưới 2 cột (rộng): `grid-template-columns: minmax(0,1.25fr) minmax(0,1fr); gap: 28px`. Hẹp: 1 cột, `gap: 20px`.
- Lưới thẻ số liệu: 3 cột (rộng) / 2 cột (hẹp), `gap: 10px`.
- Mobile chuẩn kiểm tra: khung **390px**.
- Animation chỉ bật trong `@media (prefers-reduced-motion: no-preference)`.

### Quy ước Angular
- Component UI dùng chung (Button, Chip, Card, Input, Table, StatusBadge, Avatar, Rail, TabBar, PageHeader) đặt trong `shared`, style theo token ở trên. Tạo component mới chỉ khi prototype có pattern lặp lại.
- Tiếng Việt là ngôn ngữ UI duy nhất ở MVP. Định dạng ngày `dd/MM`, tuần `dd/MM–dd/MM`, số thập phân dùng dấu phẩy (0,3 kg).
- Accessibility giữ như prototype: `aria-current="page"` cho nav active, `aria-label` cho nav, `aria-hidden` cho icon trang trí.

---

## Lệnh thường dùng
```bash
# Backend (unit + integration + acceptance @api/@integration)
dotnet test backend/Coaching.slnx --filter "Category!=web&Category!=manual"
# Schema: thêm changeset vào database/changelog/<version>/ (xml + scripts/*.sql + rollback/*.sql), KHÔNG dùng dotnet ef migrations

# Toàn hệ thống local (db + api + 2 web)
cp deploy/.env.example deploy/.env
docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build --wait
# admin: http://localhost:4200   client: http://localhost:4300
# .env.example đặt EMAIL_PROVIDER=Capture (chỉ dev/e2e): thư gửi đi (mật khẩu tạm, link đặt lại) đọc ở http://localhost:8080/api/test/emails?to=<email>
# e2e cần Capture. Trên VPS production: EMAIL_PROVIDER=Resend (bỏ trống → Log), KHÔNG dùng Capture — API chỉ cho Capture ở Development/Local/Testing

# Frontend
cd frontend && npm ci
npm run start:admin      # dev, proxy /api → localhost:8080
npm run start:client
npm run test:ci
npm run e2e              # @web trên compose (đặt AUTH_PERMIT_PER_MINUTE=1000 trong deploy/.env)
```
