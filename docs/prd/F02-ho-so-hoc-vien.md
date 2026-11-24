# F02 — Hồ sơ học viên & trạng thái coaching

> Thuộc [PRD tổng](../PRD-coaching-web.md). Phase: **P1**. Màn AD01.

## 1. Metadata
- **Feature Name:** Tạo, sửa hồ sơ học viên; ghi chú nội bộ; trạng thái coaching
- **Status:** Ready for Dev
- **Product Owner:** Hoangduc1203BK — duc.hm4@kiotviet.com

## 2. Context & Problem Statement
- Coach cần một hồ sơ duy nhất cho mỗi học viên: thông tin đăng ký (mục tiêu, lịch rảnh, dụng cụ…), ghi chú nội bộ chỉ coach thấy, và trạng thái coaching (đang coaching / tạm dừng / đã kết thúc) có lịch sử.
- Hồ sơ là điểm vào của mọi tính năng khác: phase, review, tiến độ.

## 3. UX/UI
- Prototype: `AdTaoHoSo` (form tạo hồ sơ, các nút "Lưu và gửi thông tin đăng nhập" / "Chỉ lưu hồ sơ"), `AdHoSo` (khối Thông tin học viên, Ghi chú nội bộ, Chương trình và phase).

## 4. Scope Configuration
- **In-Scope:**
  - Tạo hồ sơ bằng tên + email, kèm các field theo form đăng ký.
  - Sửa thông tin hồ sơ.
  - Ghi chú nội bộ.
  - Đổi trạng thái coaching (Đang coaching / Tạm dừng / Đã kết thúc) có lịch sử.
  - Danh sách học viên (bản đơn giản; bản đầy đủ có bộ lọc làm ở F08).
- **Out-of-Scope:** xóa cứng hồ sơ; gán học viên cho coach khác.

## 5. Functional Requirements

**BR-14 — Ghi chú nội bộ của coach chỉ hiển thị trên web admin.**
- ✅ Coach xem hồ sơ → thấy ghi chú nội bộ
- ❌ Học viên xem dữ liệu của mình → không có ghi chú nội bộ trong dữ liệu trả về

**BR-16 — Mỗi lần đổi trạng thái coaching (Đang coaching / Tạm dừng / Đã kết thúc) đều được ghi lịch sử gồm người thực hiện, thời điểm và lý do.**
- ✅ Tạm dừng một học viên, có ghi lý do → lịch sử có dòng mới
- ❌ Đổi trạng thái mà không ghi lý do → từ chối

*Quy tắc về email duy nhất và trạng thái tài khoản khi tạo hồ sơ (BR-01, BR-02) nằm ở [F01](F01-tai-khoan-phan-quyen.md).*

## 6. Non-functional Requirements & Constraints
- DTO của `/api/client` không bao giờ chứa field ghi chú nội bộ (có test).
- NFR chung ở PRD tổng.

## 7. Dependencies
- F01 (gửi thông tin đăng nhập từ form tạo hồ sơ).

## 8. Open Questions

| # | Question | Blocking | Answer | PIC |
|---|---|---|---|---|
| 1 | Danh sách field của form "Tạo hồ sơ học viên" (mục tiêu, lịch rảnh, dụng cụ… theo form đăng ký) và field nào bắt buộc | No (P1) | Lấy theo prototype AdTaoHoSo, cần coach duyệt | Product |
