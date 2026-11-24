# F05 — Soạn giáo án

> Thuộc [PRD tổng](../PRD-coaching-web.md). Phase: **P2**. Màn AD04.

## 1. Metadata
- **Feature Name:** Soạn phase giáo án: thiết lập phase, lịch tuần, buổi tập, bài tập, chỉ tiêu, thực đơn, check-in
- **Status:** Ready for Dev
- **Product Owner:** Hoangduc1203BK — duc.hm4@kiotviet.com

## 2. Context & Problem Statement
- Coach cần soạn giáo án nhanh: tạo từ đầu hoặc nhân bản một phase cũ (của bất kỳ học viên nào), rồi cá nhân hóa cho từng học viên mà không ảnh hưởng tới người khác.
- Một chương trình có nhiều phase. Mỗi phase có các tuần, lịch buổi dự kiến và nội dung từng buổi.

## 3. UX/UI
- Prototype: `AdSoanGiaoAn` gồm:
  - Thiết lập phase: tên, ngày bắt đầu, thời lượng, mục tiêu, số tuần, số buổi/tuần, ghi chú lịch tuần.
  - Xếp buổi vào ngày.
  - Bảng bài: Sets, Reps, RIR, RPE, Nghỉ, Ghi chú.
  - Thực đơn (F04).
  - Chỉ tiêu: bước, ngủ, cân nặng, check-in hằng ngày, vòng eo mỗi tuần.
- Prototype: `AdThemBuoi` (tên buổi, thời lượng dự kiến, ngày trong tuần, nhóm cơ chính, chọn bài từ thư viện, ghi chú cho học viên).

## 4. Scope Configuration
- **In-Scope:**
  - Tạo phase từ đầu; nhân bản phase cũ của bất kỳ học viên nào (tìm theo tên học viên / tên phase).
  - Thêm, sửa, xóa buổi; xếp buổi vào ngày trong tuần.
  - Bài tập trong buổi: chọn từ thư viện; sets, khoảng reps, RIR/RPE, thời gian nghỉ, ghi chú.
  - Chỉ tiêu hoạt động: bước, nước, muối, ngủ, cân nặng; các chỉ số check-in.
  - Lưu nháp.
- **Out-of-Scope:** thư viện template riêng (chỉ nhân bản phase cũ); periodization tự động (tự tăng tạ hoặc volume theo tuần); bài tập kiểu superset/giant-set có cấu trúc riêng (MVP ghi vào phần ghi chú).

## 5. Functional Requirements

**BR-30 — RIR + RPE = 10: nhập một chỉ số thì chỉ số còn lại tự tính, áp dụng cả với dạng khoảng.**
- ✅ Nhập RIR 2 → RPE 8
- ✅ Nhập RIR 1–2 → RPE 8–9
- ❌ Lưu RIR 2 cùng RPE 9 → không lưu được

**BR-31 — Nhân bản một phase cho học viên tạo ra bản riêng của học viên đó. Sửa bản riêng không ảnh hưởng tới phase gốc hay học viên khác.**
- ✅ Nhân bản phase của C cho A và B, sửa bản của A → bản của B và phase gốc của C không đổi
- ❌ Sửa bản của A mà bản của B đổi theo → sai

**BR-32 — Tuần thứ n của phase là khối 7 ngày bắt đầu từ ngày (ngày bắt đầu phase + 7×(n−1)), tính theo giờ Việt Nam.**
- ✅ Phase bắt đầu thứ Ba 10/11 → tuần 1 là 10–16/11, tuần 2 là 17–23/11
- ❌ Tuần 1 tính từ thứ Hai 9/11 → sai

**BR-33 — Ngày bắt đầu của phase mới phải sau ngày kết thúc của phase đang chạy hoặc đã lên lịch trong cùng chương trình.**
- ✅ Phase 2 kết thúc 07/12, phase 3 bắt đầu 08/12 → được
- ❌ Phase 3 bắt đầu 05/12 → từ chối, báo trùng với phase 2

**BR-34 — Chỉ phát hành được phase khi mỗi tuần có ít nhất 1 buổi và mỗi buổi có ít nhất 1 bài.**
- ✅ Phase 4 tuần, mỗi tuần 5 buổi đủ bài → phát hành được
- ❌ Phase có một buổi chưa có bài → từ chối, chỉ ra buổi bị thiếu

**BR-35 — Bài tập chưa có video mẫu chỉ tạo cảnh báo, không chặn phát hành.**
- ✅ Landmine press chưa có video → hiện cảnh báo, coach vẫn phát hành được

## 6. Non-functional Requirements & Constraints
- Lưu nháp không làm mất dữ liệu khi coach rời trang (hỏi xác nhận nếu còn thay đổi chưa lưu).
- NFR chung ở PRD tổng.

## 7. Dependencies
- F03 (thư viện bài tập), F04 (thực đơn), F02 (hồ sơ).

## 8. Open Questions

| # | Question | Blocking | Answer | PIC |
|---|---|---|---|---|
| 1 | Template là thực thể riêng (thư viện template) hay chỉ nhân bản phase cũ? | No | **Đã chốt:** chỉ nhân bản phase cũ | Product |
