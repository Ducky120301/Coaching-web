# F00 — Dựng codebase Backend + Frontend

> Thuộc [PRD tổng](../PRD-coaching-web.md). Phase: **P0**.

## 1. Metadata
- **Feature Name:** Dựng codebase BE (.NET 10) + FE (Angular) cho Web Admin và Web Học viên
- **Status:** Ready for Dev
- **Product Owner:** Hoangduc1203BK — duc.hm4@kiotviet.com

## 2. Context & Problem Statement
- Repo hiện đang trống. Trước khi làm tính năng, cần một codebase chuẩn: Clean Architecture + DDD, 1 backend dùng chung cho 2 web, Angular workspace gồm 2 app.
- Mục đích là để mọi tính năng từ P1 trở đi chỉ việc thêm use case/màn hình, không phải bàn lại kiến trúc.

## 3. UX/UI
- Tài liệu design: `docs/duobexiuuu-coaching-design.html`. P0 dựng design tokens, font và layout khung (rail admin; rail/tab bar client theo breakpoint 880px) đúng theo prototype.

## 4. Scope Configuration
- **In-Scope:**
  - **Backend:**
    - Solution .NET 10: `Coaching.Domain` / `Coaching.Application` / `Coaching.Infrastructure` / `Coaching.Api` cùng các project test (Domain, Application, Api integration).
    - Khung aggregate dự kiến: Coach, Student, Program, Phase (chứa Week, PlanVersion, Session, Meal), Exercise, Food, WorkoutLog, CheckIn, WeeklyReview.
    - EF Core + SQL Server, migration đầu tiên.
    - ASP.NET Core Identity + JWT (access + refresh token), role `Admin` / `Student`, policy cho `/api/admin` và `/api/client`.
    - Seed 1 tài khoản admin. Student có `CoachId`.
    - Hạ tầng dùng chung: `IEmailSender` (Resend), `IClock` theo giờ Việt Nam, audit log, ProblemDetails cho lỗi.
  - **Frontend:**
    - Angular workspace: `projects/admin`, `projects/client`, `projects/shared`.
    - `shared`: design tokens (CSS variables từ prototype), font (Barlow Condensed, Be Vietnam Pro, Yellowtail), `AuthService`, `authGuard`, HTTP interceptor (đính JWT, refresh token).
    - Layout khung: rail admin; client có rail khi rộng ≥ 880px và tab bar khi hẹp hơn.
    - Màn đăng nhập (`Main`) đúng prototype, dùng chung cho cả 2 app.
  - **DevOps:**
    - `deploy/docker-compose.yml` gồm sqlserver, api, nginx (serve 2 app Angular).
    - CI/CD bằng **GitHub Actions**: build + test mỗi lần push/PR; deploy nhánh chính lên **Render** làm môi trường test (API chạy Docker web service, 2 app Angular là static site).
    - SQL Server cho môi trường test đặt trên máy cá nhân/máy khác do PO cung cấp; API trên Render kết nối qua connection string lưu trong secret.
- **Out-of-Scope:** mọi màn nghiệp vụ (từ P1 trở đi); kích hoạt tài khoản học viên (F01); hosting production (PO đăng ký và báo sau, trước go-live).

## 5. Functional Requirements
P0 không có business rule mới. Tiêu chí nghiệm thu:
- ✅ Admin seed đăng nhập trên môi trường test (Render) → vào được khung web admin, gọi được một API `/api/admin/*` có bảo vệ
- ❌ Gọi `/api/admin/*` không kèm token → 401
- ❌ Tài khoản role Student gọi `/api/admin/*` → 403
- ✅ `docker compose up` trên máy dev → chạy đủ DB, API và 2 web
- ✅ Layout khung ở khung 390px và ≥ 1280px khớp prototype (màu, font, kích thước rail và tab bar)

## 6. Non-functional Requirements & Constraints
- Đúng dependency rule: `Api → Application → Domain`; Infrastructure phụ thuộc vào Application/Domain; Domain không tham chiếu EF Core/ASP.NET.
- Không gọi `DateTime.Now` trong Domain/Application; mọi thời gian đi qua `IClock`.
- Các NFR chung ở PRD tổng.

## 7. Dependencies
- GitHub (repo + Actions), tài khoản Render, SQL Server cho môi trường test (PO cung cấp), tài khoản Resend (dùng chế độ test ở P0).

## 8. Open Questions

| # | Question | Blocking | Answer | PIC |
|---|---|---|---|---|
| 1 | Dùng CI nào? | No | **Đã chốt:** GitHub Actions, deploy test lên Render | Dev |
| 2 | Connection string SQL Server cho môi trường test. Máy chứa DB phải cho Render truy cập được từ internet (IP/port public hoặc tunnel, firewall, bật TLS, user DB riêng không phải `sa`) | No (chặn bước deploy test, không chặn code) | PO cung cấp | Product |
| 3 | Gói Render: free tier tự "ngủ" sau một thời gian không có request, request đầu tiên sau đó chậm vài chục giây. Có chấp nhận cho môi trường test không? | No | Đề xuất: chấp nhận free cho test | Product |
