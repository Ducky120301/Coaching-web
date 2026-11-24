# F12 — Nhật ký tập & Check-in ngày (Web học viên)

> Thuộc [PRD tổng](../PRD-coaching-web.md). Phase: **P5**. Màn HV03, HV04.

## 1. Metadata
- **Feature Name:** Ghi kg/reps theo set, RIR thực tế, ghi chú; trạng thái buổi; check-in ngày (cân, bước, ngủ, tuân thủ)
- **Status:** Ready for Dev
- **Product Owner:** Hoangduc1203BK — duc.hm4@kiotviet.com

## 2. Context & Problem Statement
- Nhật ký hiện lưu cục bộ trên trình duyệt nên mất khi đổi thiết bị và coach không xem được.
- Trên web: học viên ghi từng set trên điện thoại, mở máy khác vẫn thấy đúng dữ liệu, xem được lần tập trước và volume để tăng tải.
- Check-in ngày là nguồn số liệu cho review tuần và dashboard.

## 3. UX/UI
- Prototype `HvNhatKy`: từng bài với các set (kg, reps), RIR thực tế, ghi chú, lần tập trước, volume; lưu dở / hoàn thành / bỏ buổi.
- Prototype `HvCheckIn`: cân nặng, bước, ngủ, tuân thủ tập/ăn (1–10), ghi chú, buổi thực tế.

## 4. Scope Configuration
- **In-Scope:**
  - Ghi set và RIR thực tế theo bài; ghi chú.
  - Lưu dở, hoàn thành, bỏ buổi.
  - Xem lần tập trước và volume.
  - Check-in ngày.
  - Đồng bộ đa thiết bị (dữ liệu lưu trên server).
- **Out-of-Scope:** chế độ offline / hàng chờ đồng bộ; đồng hồ đếm giờ nghỉ (nếu prototype không có).

## 5. Functional Requirements

**BR-72 — Ghi set: kg là số ≥ 0 và reps là số nguyên ≥ 0.**
- ✅ 26 kg × 10 reps → lưu
- ❌ Nhập reps −1 → từ chối

**BR-73 — Mỗi buổi tập có một trạng thái nhật ký: Lưu dở, Hoàn thành hoặc Bỏ buổi.**
- ✅ Ghi 5/7 bài rồi bấm lưu → Lưu dở; mở lại trên máy khác thấy đủ 5 bài

**BR-74 — Không ghi nhật ký hay check-in cho ngày trong tương lai.**
- ❌ Hôm nay 23/11, ghi check-in cho 25/11 → từ chối

**BR-75 — Mỗi học viên có tối đa 1 check-in cho mỗi ngày; ghi lại cùng ngày thì cập nhật bản đã có.**
- ✅ Sửa cân nặng của check-in hôm nay → cập nhật, không tạo bản thứ hai

**BR-76 — Điểm tuân thủ tập và tuân thủ ăn nằm trong khoảng 1–10.**
- ✅ Nhập 8 → lưu
- ❌ Nhập 11 → từ chối

**BR-77 — Check-in tự lấy buổi thực tế từ nhật ký của cùng ngày nếu đã có.**
- ✅ Đã ghi Push hôm nay → check-in hiện "Buổi: Push"

**BR-81 — Học viên sửa được nhật ký và check-in của các ngày đã qua, không giới hạn thời gian. Review đã nộp vẫn giữ số liệu lúc nộp (BR-53).**
- ✅ Sửa kg của một set tuần trước → lưu, các chỉ số và tiến độ tính lại
- ✅ Sửa check-in ngày 20/11 sau khi đã nộp review tuần 2 → lưu; review tuần 2 vẫn hiện số cũ

**BR-79 — Khi phase đang Tạm dừng, học viên không ghi được nhật ký buổi mới.**
- ✅ Phase Tạm dừng → trang Hôm nay hiện trạng thái Tạm dừng
- ❌ Ghi set khi phase đang Tạm dừng → từ chối

*Chỉ tiêu buổi đã ghi được giữ theo phiên bản lúc tập (BR-41, [F06](F06-phat-hanh-phien-ban.md)). Cách tính volume và trung bình nằm ở [F08](F08-dashboard-tien-do.md).*

## 6. Non-functional Requirements & Constraints
- Lưu một set phản hồi < 300ms (p95) để thao tác giữa các set không bị chậm.
- Mỗi set được lưu ngay khi nhập (auto-save), không đợi tới lúc bấm hoàn thành buổi.

## 7. Dependencies
- F06/F11 (buổi theo phiên bản hiệu lực).

## 8. Open Questions

| # | Question | Blocking | Answer | PIC |
|---|---|---|---|---|
| 1 | Học viên được sửa nhật ký/check-in của ngày cũ tới khi nào? | No | **Đã chốt:** không giới hạn (BR-81) | Product |
