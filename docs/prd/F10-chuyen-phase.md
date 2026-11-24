# F10 — Tổng kết & chuyển phase

> Thuộc [PRD tổng](../PRD-coaching-web.md). Phase: **P3**. Màn AD07.

## 1. Metadata
- **Feature Name:** Tổng kết phase, mục tiêu phase tiếp theo, kết thúc / gia hạn / tạm dừng có lịch sử trạng thái
- **Status:** Ready for Dev
- **Product Owner:** Hoangduc1203BK — duc.hm4@kiotviet.com

## 2. Context & Problem Statement
- Cuối mỗi phase, coach cần tổng kết kết quả so với chỉ tiêu, ghi kết luận cho học viên, đặt lý do và mục tiêu cho phase tiếp theo, và chuẩn bị phase kế tiếp trước khi phase hiện tại kết thúc.
- Coach cũng cần gia hạn hoặc tạm dừng phase khi học viên có việc riêng hoặc chấn thương.

## 3. UX/UI
- Prototype `AdChuyenPhase`: kết quả so với chỉ tiêu (chỉ tiêu, đầu phase, cuối phase, đánh giá); kết luận phase; lý do và mục tiêu phase tiếp theo; phase kế tiếp; xử lý phase hiện tại; lịch sử trạng thái.

## 4. Scope Configuration
- **In-Scope:**
  - Bấm Kết thúc phase (phase không tự kết thúc, xem BR-39).
  - Tổng kết và kết luận.
  - Lý do và mục tiêu phase mới.
  - Tạo phase kế tiếp khi phase hiện tại còn chạy (soạn ở F05, phát hành ở F06).
  - Gia hạn, tạm dừng, tiếp tục.
  - Lịch sử trạng thái.
- **Out-of-Scope:** gợi ý tự động phase tiếp theo.

## 5. Functional Requirements

**BR-66 — Tổng kết phase bắt buộc có kết luận. Kết luận hiển thị cho học viên ở trang Tiến độ.**
- ✅ Lưu kết luận → học viên thấy trên trang Tiến độ
- ❌ Đóng tổng kết mà không có kết luận → từ chối

**BR-67 — Gia hạn (thêm tuần) chỉ áp dụng cho phase đang chạy, và ngày kết thúc mới không được trùng với phase kế tiếp đã lên lịch.**
- ✅ Gia hạn 1 tuần khi chưa có phase sau → được
- ❌ Gia hạn chồng lên phase 3 đã lên lịch → từ chối

**BR-68 — Tạm dừng phase thì phase dừng lại. Khi tiếp tục, coach xác nhận lịch các buổi còn lại; ngày kết thúc lùi thêm đúng số ngày đã tạm dừng.**
- ✅ Tạm dừng 5 ngày rồi tiếp tục → ngày kết thúc lùi 5 ngày
- ❌ Tiếp tục mà coach chưa xác nhận lịch → từ chối

**BR-83 — Khi tiếp tục phase sau tạm dừng mà ngày kết thúc mới chồng lên phase kế tiếp đã lên lịch, hệ thống đề xuất lùi phase kế tiếp đúng số ngày bị chồng; chỉ áp dụng sau khi coach xác nhận.**
- ✅ Tạm dừng 5 ngày, phase 3 hẹn 08/12 → đề xuất lùi phase 3 sang 13/12, coach xác nhận → phase 3 bắt đầu 13/12
- ❌ Hệ thống tự lùi phase 3 mà không hỏi coach → sai

*Kết thúc phase do coach bấm (BR-39, BR-43)* và audit (BR-42) nằm ở [F06](F06-phat-hanh-phien-ban.md). Phase mới không chồng ngày (BR-33) nằm ở [F05](F05-soan-giao-an.md).*

## 6. Non-functional Requirements & Constraints
- Mọi chuyển trạng thái đều ghi audit (BR-42).

## 7. Dependencies
- F05, F06, F08.

## 8. Open Questions

| # | Question | Blocking | Answer | PIC |
|---|---|---|---|---|
| 1 | Khi tạm dừng, nếu ngày kết thúc lùi lại chồng lên phase kế tiếp đã lên lịch thì xử lý thế nào? | No | **Đã chốt:** lùi phase kế tiếp, coach xác nhận (BR-83) | Product |
