# F07 — Import Excel (hồ sơ, giáo án, thực phẩm)

> Thuộc [PRD tổng](../PRD-coaching-web.md). Phase: **cuối P2**.

## 1. Metadata
- **Feature Name:** Import Excel hồ sơ học viên, giáo án và thư viện thực phẩm theo template của coach
- **Status:** Draft *(OQ #1 đang Blocking: chưa có template Excel)*
- **Product Owner:** Hoangduc1203BK — duc.hm4@kiotviet.com

## 2. Context & Problem Statement
- Coach đang có học viên chạy trên Excel/template. Để chuyển sang web mà không phải nhập tay toàn bộ, coach cần import hồ sơ, phase đang chạy (kèm tuần hiện tại và các buổi còn lại) và thư viện thực phẩm.
- Nhập tay vẫn luôn làm được (F02, F04, F05). Import chỉ để tăng tốc.

## 3. UX/UI
- Prototype không có màn import. Đề xuất: một màn upload file → xem trước kết quả kiểm tra → xác nhận nhập. Giao diện theo design tokens trong `CLAUDE.md`.

## 4. Scope Configuration
- **In-Scope:**
  - Import theo template Excel coach cung cấp: (a) thư viện thực phẩm, (b) hồ sơ học viên, (c) giáo án/phase.
  - Kiểm tra toàn bộ file và báo lỗi theo từng dòng.
- **Out-of-Scope:** import nhật ký set và check-in lịch sử; tự nhận diện các định dạng Excel khác ngoài template.

## 5. Functional Requirements

**BR-28 — Import Excel kiểm tra toàn bộ file trước khi nhập. Nếu có dòng lỗi thì không nhập dòng nào và trả báo cáo lỗi theo dòng.**
- ✅ File 50 dòng hợp lệ → nhập đủ 50
- ❌ File có dòng 12 thiếu email → không nhập gì, báo "dòng 12: thiếu email"

*Các dòng import vẫn phải thỏa các rule nghiệp vụ gốc: email duy nhất (BR-01), thực phẩm có nhóm macro (BR-23), phase không chồng ngày (BR-33).*

## 6. Non-functional Requirements & Constraints
- File tối đa 5MB, chỉ nhận định dạng `.xlsx`.
- Import chạy trong một transaction: hoặc nhập hết, hoặc không nhập gì.

## 7. Dependencies
- Template Excel của coach.
- Thư viện đọc Excel phía .NET (ví dụ ClosedXML/MiniExcel), chọn ở bước tech design.

## 8. Open Questions

| # | Question | Blocking | Answer | PIC |
|---|---|---|---|---|
| 1 | Template Excel để import hồ sơ, giáo án và thực phẩm (mẫu cột) | **Yes** | Coach gửi sau | Product |
| 2 | Bỏ cả file khi có lỗi (BR-28) hay nhập các dòng hợp lệ? | No | **Đã chốt:** bỏ cả file | Product |
