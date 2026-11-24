# F08 — Dashboard, hồ sơ tiến độ & chỉ số

> Thuộc [PRD tổng](../PRD-coaching-web.md). Phase: **P3** (AD02 Dashboard, AD03 Hồ sơ tiến độ) · **P5** (HV07 Tiến độ phía học viên).

## 1. Metadata
- **Feature Name:** Dashboard việc cần xử lý, timeline và so sánh tuần/phase, cách tính chỉ số
- **Status:** Ready for Dev
- **Product Owner:** Hoangduc1203BK — duc.hm4@kiotviet.com

## 2. Context & Problem Statement
- Một coach quản lý nhiều học viên cần biết ngay ai chờ phản hồi, ai thiếu dữ liệu, ai sắp hết phase, ai chưa có phase kế tiếp, và đi thẳng tới hồ sơ hoặc tuần cần xử lý.
- Số liệu (số buổi, trung bình check-in, volume) phải được tính nhất quán ở mọi màn.

## 3. UX/UI
- Prototype `Admin-Dashboard`: tìm học viên; lọc theo trạng thái và phase; nhóm việc (Review chờ xem, Thiếu log/check-in, Sắp hết phase, Chưa có phase sau, Chờ kích hoạt); bảng Học viên / Trạng thái / Phase và tuần / Hoạt động gần nhất / Thao tác.
- Prototype `AdHoSo`: Chương trình và phase, chỉ tiêu và thực tế, so sánh phase 1 với phase 2, hoạt động gần đây.
- Prototype `HvTienDo`: phía học viên.

## 4. Scope Configuration
- **In-Scope:**
  - Dashboard có tìm kiếm, lọc và các nhóm việc (thêm nhóm "Phase quá hạn chưa kết thúc" ngoài các nhóm trong prototype).
  - Hồ sơ tiến độ: timeline; chỉ tiêu so với thực tế; so sánh tuần và phase; mở từng set, check-in, review, đính kèm.
  - Trang Tiến độ của học viên: lịch sử phase, biểu đồ tuần, kết luận cuối phase, phản hồi của coach, mốc hiệu lực khi giáo án thay đổi.
- **Out-of-Scope:** xuất báo cáo; thông báo đẩy.

## 5. Functional Requirements

**BR-59 — Số buổi hoàn thành chỉ đếm buổi đã được đánh dấu hoàn thành.**
- ✅ 3 buổi hoàn thành, 1 buổi lưu dở → 3
- ❌ Đếm cả buổi lưu dở → sai

**BR-60 — Trung bình của mỗi chỉ số check-in chỉ tính trên các ngày có giá trị, kèm số ngày đã nhập.**
- ✅ Cân nặng nhập 5/7 ngày → trung bình 5 ngày, hiển thị "5/7 ngày"
- ❌ Ngày không nhập bị tính là 0 → sai

**BR-61 — Volume = tổng kg × reps của các set có đủ cả kg và reps.**
- ✅ 3 set 26kg × 10 reps → 780
- ❌ Set thiếu reps mà vẫn tính vào volume → sai

**BR-62 — Chỉ so sánh tiến triển giữa các lần tập cùng một bài và cùng cách ghi tải.**
- ✅ Incline DB press tuần 1 so với tuần 2 → hiện thay đổi
- ❌ Incline DB press so với Smith incline → không so sánh

**BR-63 — Dashboard "Sắp hết phase": phase đang chạy còn từ 0 đến 7 ngày tới ngày kết thúc dự kiến.**
- ✅ Còn 3 ngày → hiện trong nhóm
- ❌ Còn 10 ngày → không hiện
- ❌ Đã qua ngày kết thúc dự kiến → không hiện ở nhóm này

**BR-80 — Dashboard "Phase quá hạn chưa kết thúc": phase đang chạy đã qua ngày kết thúc dự kiến mà coach chưa bấm Kết thúc; hiển thị kèm phase kế tiếp đang chờ (nếu có).**
- ✅ Phase 2 hết 07/12, hôm nay 09/12 chưa kết thúc, phase 3 Đã lên lịch → hiện "quá 2 ngày, phase 3 đang chờ"
- ❌ Phase 2 đã được coach kết thúc → không hiện

**BR-64 — Dashboard "Chưa có phase sau": phase đang chạy chưa có phase kế tiếp (Nháp hoặc Đã lên lịch).**
- ✅ Phase 2 đang chạy, phase 3 còn Nháp → không hiện trong nhóm
- ❌ Phase 2 đang chạy và chưa có phase 3 → hiện

**BR-65 — Dashboard "Thiếu log, check-in": trong 2 ngày gần nhất, học viên thiếu check-in hoặc có buổi dự kiến đã qua mà chưa ghi.**
- ✅ Không check-in thứ Năm và thứ Sáu → hiện trong nhóm
- ❌ Đủ check-in và đã ghi mọi buổi → không hiện

*Nhóm "Review chờ xem" lấy theo trạng thái review ở [F09](F09-review-tuan.md); nhóm "Chờ kích hoạt" lấy theo trạng thái tài khoản ở [F01](F01-tai-khoan-phan-quyen.md).*

## 6. Non-functional Requirements & Constraints
- Dashboard tải < 1s với 1000 học viên. Các nhóm việc tính bằng truy vấn tổng hợp, không lặp từng học viên ở tầng ứng dụng.
- Biểu đồ theo design tokens; chọn thư viện chart ở bước tech design.

## 7. Dependencies
- F09 (review), F12 (nhật ký, check-in), F06 (phase).

## 8. Open Questions

| # | Question | Blocking | Answer | PIC |
|---|---|---|---|---|
| 1 | Các định nghĩa nhóm việc (BR-63/64/65: ngưỡng 7 ngày, 2 ngày) có đúng cách coach đang vận hành không? | No | **Đã chốt:** như đề xuất, ngưỡng cố định; thêm nhóm "Phase quá hạn chưa kết thúc" (BR-80) | Product |
