# F09 — Review tuần

> Thuộc [PRD tổng](../PRD-coaching-web.md). Phase: **P3** (AD06 Hàng chờ review phía admin) · **P5** (HV05 nộp review phía học viên).

## 1. Metadata
- **Feature Name:** Học viên nộp review tuần (số liệu + link Drive); coach xem, phản hồi theo tuần/bài, yêu cầu bổ sung
- **Status:** Ready for Dev
- **Product Owner:** Hoangduc1203BK — duc.hm4@kiotviet.com

## 2. Context & Problem Statement
- Báo cáo tuần hiện gửi qua chat nên dễ thất lạc, không đối chiếu được với dữ liệu gốc.
- Trên web: học viên nộp review gắn đúng tuần. Coach mở đúng tuần cần review, đối chiếu chỉ tiêu với thực tế, phản hồi hoặc yêu cầu bổ sung. Mọi lần nộp lại đều được giữ để đối chiếu.

## 3. UX/UI
- Prototype `HvReview`: tổng kết tuần (số buổi, cân nặng và thay đổi, bước/ngủ trung bình, điểm tuân thủ, tiến triển bài), stress, ghi chú, link ảnh bốn góc, link video form theo bài.
- Prototype `Admin-Review`:
  - Chỉ tiêu và thực tế; check-in và buổi tập theo ngày; tiến triển bài chính.
  - Học viên ghi chú; ảnh và video kèm trạng thái "Mở được / Chưa cấp quyền".
  - Phản hồi theo tuần / theo bài; ghi chú nội bộ.
  - "Cần học viên bổ sung" với các mục chọn sẵn; nút "Gửi phản hồi" / "Yêu cầu bổ sung".

## 4. Scope Configuration
- **In-Scope:**
  - Hạn review mặc định và hạn riêng theo học viên.
  - Nộp review, nộp lại.
  - Trạng thái review.
  - Phản hồi theo tuần và theo bài.
  - Yêu cầu bổ sung.
  - Coach đánh dấu link "Mở được / Chưa cấp quyền".
- **Out-of-Scope:** upload ảnh/video; chat qua lại trong review.

## 5. Functional Requirements

**BR-50 — Hạn review mặc định là 23:59 ngày cuối của mỗi tuần phase; coach đặt được hạn khác cho từng học viên.**
- ✅ Tuần 2 là 17–23/11 → hạn 23/11 23:59
- ✅ Coach đặt hạn "thứ Hai 23:59" cho học viên Đức → hạn theo cài đặt riêng

**BR-51 — Review nộp sau hạn vẫn được nhận nhưng bị đánh dấu "Trễ".**
- ✅ Nộp 24/11 08:00 cho hạn 23/11 23:59 → nhận, gắn nhãn Trễ

**BR-52 — Học viên nộp được review tuần kể từ ngày cuối của tuần đó.**
- ✅ Tuần 17–23/11, nộp ngày 23/11 → được
- ❌ Nộp ngày 20/11 → từ chối

**BR-53 — Review đã nộp lưu bản chụp số liệu tại thời điểm nộp.**
- ✅ Học viên sửa check-in ngày 20/11 sau khi đã nộp → review vẫn hiện số cũ
- ❌ Review tự cập nhật theo số liệu mới → sai

**BR-54 — Coach mở một review "Đã nộp" thì review tự chuyển sang "Đã xem".**
- ✅ Coach mở review tuần 2 → trạng thái Đã xem, học viên thấy coach đã xem

**BR-55 — Gửi phản hồi cần ít nhất một nhận xét, theo tuần hoặc theo bài. Gửi xong review chuyển sang "Đã phản hồi" và học viên thấy được phản hồi.**
- ✅ Gửi nhận xét tuần và 1 nhận xét bài → Đã phản hồi
- ❌ Gửi phản hồi trống → từ chối

**BR-56 — Yêu cầu bổ sung phải chọn ít nhất một mục cần bổ sung. Review chuyển sang "Cần bổ sung" và học viên sửa được.**
- ✅ Chọn "Cấp quyền xem video form" → học viên thấy yêu cầu và sửa link
- ❌ Gửi yêu cầu bổ sung mà không chọn mục nào → từ chối

**BR-85 — Khi yêu cầu bổ sung, hệ thống gợi ý sẵn các mục dựa trên dữ liệu đang thiếu của tuần đó (ngày thiếu check-in, buổi chưa ghi đủ bài, link coach đã đánh dấu "Chưa cấp quyền"); coach bỏ chọn gợi ý hoặc gõ thêm mục khác được.**
- ✅ Thiếu check-in thứ Sáu 20/11 → gợi ý "Thêm check-in thứ Sáu 20/11"
- ✅ Coach gõ thêm "Quay lại video góc nghiêng" → được gửi kèm
- ❌ Tuần đủ dữ liệu → không hiện gợi ý nào, coach vẫn gõ tay được

**BR-57 — Mỗi lần nộp lại tạo một bản review mới; các bản trước giữ nguyên để coach đối chiếu.**
- ✅ Nộp lại sau khi bị yêu cầu bổ sung → coach thấy bản 1 và bản 2

**BR-58 — Coach không sửa được số liệu học viên đã ghi (set, check-in).**
- ❌ Coach sửa kg của một set học viên đã ghi → không có chức năng này

**BR-78 — Link ảnh/video trong review phải là link Google Drive (drive.google.com hoặc docs.google.com).**
- ✅ Dán link drive.google.com/… → lưu
- ❌ Dán link facebook.com/… → từ chối

## 6. Non-functional Requirements & Constraints
- Ghi chú nội bộ trong màn review tuân theo BR-14 (chỉ admin thấy).
- Bản chụp review lưu đủ để hiển thị lại mà không cần đọc dữ liệu gốc.

## 7. Dependencies
- F12 (nhật ký, check-in làm nguồn số liệu), F08 (cách tính chỉ số), Google Drive (học viên tự cấp quyền).

## 8. Open Questions

| # | Question | Blocking | Answer | PIC |
|---|---|---|---|---|
| 1 | Danh sách mục "Cần học viên bổ sung" cố định hay coach tự gõ thêm? | No | **Đã chốt:** gợi ý theo dữ liệu thiếu và cho gõ thêm (BR-85) | Product |
