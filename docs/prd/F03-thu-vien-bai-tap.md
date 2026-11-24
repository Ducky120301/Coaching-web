# F03 — Thư viện bài tập

> Thuộc [PRD tổng](../PRD-coaching-web.md). Phase: **P1**.

## 1. Metadata
- **Feature Name:** Thư viện bài tập (tên, dụng cụ, nhóm cơ, video mẫu, hướng dẫn kỹ thuật, chỉ tiêu mặc định)
- **Status:** Ready for Dev
- **Product Owner:** Hoangduc1203BK — duc.hm4@kiotviet.com

## 2. Context & Problem Statement
- Khi soạn giáo án, coach dùng lại bài tập, video và hướng dẫn thay vì gõ lại cho từng học viên.
- Thư viện là nguồn để chọn bài ở màn Soạn giáo án và Thêm buổi.

## 3. UX/UI
- Prototype: `AdThuVien` (tìm bài; cột Dụng cụ, Bài tập, Nhóm cơ, Video mẫu, Đang dùng trong; chi tiết bài gồm Sets/Reps/RIR/Nghỉ mặc định, hướng dẫn kỹ thuật, lỗi hay gặp, bài thay thế; nút "Thêm vào giáo án", "Sửa bài").

## 4. Scope Configuration
- **In-Scope:** thêm, sửa, tìm kiếm, ẩn/xóa bài; danh mục nhóm cơ và dụng cụ (seed sẵn, coach thêm được); video mẫu (link YouTube); chỉ tiêu mặc định khi thêm vào giáo án; hiển thị bài đang được dùng ở những phase nào.
- **Out-of-Scope:** upload video; import bài tập hàng loạt (nếu cần thì bổ sung vào F07).

## 5. Functional Requirements

**BR-20 — Mỗi bài tập trong thư viện là duy nhất theo cặp (tên bài, dụng cụ).**
- ✅ Thêm "Lateral Raise – Cable" và "Lateral Raise – Dumbbell" → được cả hai
- ❌ Thêm "Lateral Raise – Cable" lần thứ hai → báo trùng

**BR-21 — Chỉ xóa được bài tập chưa được dùng trong giáo án nào. Bài đã dùng thì chỉ ẩn được.**
- ✅ Xóa bài chưa dùng → xóa được
- ✅ Ẩn bài đã dùng → bài không còn xuất hiện khi chọn bài mới, giáo án cũ vẫn hiện bài đó
- ❌ Xóa bài đang có trong một phase → từ chối

**BR-22 — Giáo án lưu bản chụp nội dung bài tập tại thời điểm thêm vào. Sửa bài trong thư viện không làm thay đổi giáo án đã có.**
- ✅ Sửa ghi chú kỹ thuật của bài trong thư viện → phase đang chạy vẫn giữ ghi chú cũ
- ❌ Sửa thư viện mà giáo án đã phát hành tự đổi theo → sai

## 6. Non-functional Requirements & Constraints
- Tìm bài tập trả kết quả < 500ms (p95).
- Seed sẵn các bài có trong `window.__CLIENT__` của prototype để làm dữ liệu dev.

## 7. Dependencies
- YouTube (chỉ lưu link).

## 8. Open Questions

| # | Question | Blocking | Answer | PIC |
|---|---|---|---|---|
| 1 | Danh mục nhóm cơ và dụng cụ cố định hay coach tự thêm? | No | **Đã chốt:** seed sẵn theo prototype, coach thêm được. Nhóm cơ: Ngực, Vai, Tay sau, Tay trước, Lưng, Vai sau, Đùi trước, Đùi sau, Mông, Bắp chân, Bụng. Dụng cụ: Machine, Cable, Dumbbell, Barbell, Smith Machine, Kettlebell, Bodyweight | Product |
