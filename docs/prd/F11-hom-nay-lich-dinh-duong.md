# F11 — Hôm nay, Lịch & giáo án, Dinh dưỡng, Thuật ngữ (Web học viên)

> Thuộc [PRD tổng](../PRD-coaching-web.md). Phase: **P4**. Màn HV01, HV02, HV06.

## 1. Metadata
- **Feature Name:** Học viên xem việc hôm nay, lịch tuần và giáo án, dời buổi, xem dinh dưỡng và thuật ngữ
- **Status:** Ready for Dev
- **Product Owner:** Hoangduc1203BK — duc.hm4@kiotviet.com

## 2. Context & Problem Statement
- Mở web trên điện thoại, học viên phải thấy ngay: đang ở phase và tuần nào, hôm nay tập buổi gì, chỉ tiêu hôm nay, việc còn thiếu (check-in, review đến hạn).
- Học viên xem được toàn bộ giáo án đang hiệu lực, thực đơn, thực phẩm bổ sung và tra cứu thuật ngữ (RIR, RPE, tempo…).

## 3. UX/UI
- Prototype `HvHomNay`: lời chào, phase/tuần, lịch tuần, buổi hôm nay, chỉ tiêu (bước, nước, muối), nút Check-in.
- Prototype `HvLich`: tuần, ngày nghỉ; buổi gồm danh sách bài, dụng cụ, sets, reps, RIR, nghỉ, ghi chú, video mẫu.
- Prototype `HvDinhDuong`: kcal, macro, thực đơn theo bữa, thực phẩm bổ sung theo nhóm.
- Prototype `HvThuatNgu`: thuật ngữ theo nhóm.
- Layout: dưới 880px dùng tab bar, từ 880px dùng rail; khung kiểm tra 390px.

## 4. Scope Configuration
- **In-Scope:**
  - Trang Hôm nay.
  - Lịch tuần, chi tiết buổi, video mẫu.
  - Dời buổi trong tuần.
  - Dinh dưỡng và bổ sung.
  - Thuật ngữ (nội dung tĩnh, seed từ prototype).
- **Out-of-Scope:** học viên tự sửa giáo án; dời buổi sang tuần khác.

## 5. Functional Requirements

**BR-38 — Học viên chỉ thấy nội dung của phase Đang chạy, Tạm dừng hoặc Kết thúc. Phase Nháp hoặc Đã lên lịch chỉ hiển thị ngày bắt đầu đã hẹn.**
- ✅ Phase 3 Đã lên lịch từ 08/12 → trang Hôm nay hiện "Phase 3 bắt đầu 08/12"
- ❌ Học viên xem được bài tập của phase Nháp → sai

**BR-70 — Trang Hôm nay lấy buổi tập và chỉ tiêu theo phiên bản giáo án đang hiệu lực vào hôm nay (giờ VN).**
- ✅ Hôm nay 24/11, bản 2 hiệu lực từ 24/11 → hiện chỉ tiêu bản 2

**BR-71 — Học viên dời được buổi chưa hoàn thành sang một ngày khác trong cùng tuần phase mà không cần coach duyệt; việc dời không tạo thêm buổi.**
- ✅ Dời Push từ thứ Hai sang thứ Ba cùng tuần → tuần vẫn có đúng 1 buổi Push
- ❌ Dời sang tuần sau → từ chối, báo cần liên hệ coach
- ❌ Dời buổi đã hoàn thành → từ chối

**BR-82 — Chỉ dời buổi được vào ngày chưa có buổi tập nào.**
- ✅ Dời Push sang thứ Tư (ngày nghỉ) → được
- ❌ Dời Pull vào thứ Năm đã có Legs → từ chối, báo ngày đó đã có buổi

## 6. Non-functional Requirements & Constraints
- Trang Hôm nay tải < 1s trên mạng 4G.
- UI bám chính xác prototype (`CLAUDE.md`).

## 7. Dependencies
- F06 (phase đã phát hành), F04 (thực đơn), F01 (đăng nhập).

## 8. Open Questions

| # | Question | Blocking | Answer | PIC |
|---|---|---|---|---|
| 1 | Có cho dời buổi sang ngày đã có buổi khác (2 buổi trong 1 ngày) không? | No | **Đã chốt:** không cho (BR-82) | Product |
