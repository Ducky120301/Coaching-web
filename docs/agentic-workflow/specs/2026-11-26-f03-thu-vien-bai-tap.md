---
doc-id: 2026-11-26-f03-thu-vien-bai-tap
status: ready
---

# Spec: F03 — Thư viện bài tập

## 1. Problem & Why
Khi soạn giáo án (F05), coach phải gõ lại tên bài, video mẫu, liều tập và hướng dẫn cho từng học viên. Thư viện bài tập cho coach lưu mỗi bài một lần (tên, nhóm cơ, dụng cụ, video YouTube, liều mặc định, hướng dẫn kỹ thuật, lỗi hay gặp) để dùng lại khi soạn giáo án. PRD: `docs/prd/F03-thu-vien-bai-tap.md`.

Giáo án chưa có ở P1, nên quy tắc "chỉ xoá bài chưa được dùng" và "giáo án giữ bản chụp bài tập" được chốt ngay bây giờ nhưng chỉ kiểm đầy đủ được khi F05 làm xong. Import bài tập từ file làm ở một increment sau, khi coach gửi template.

## 2. User Stories
| ID | User Story | Related BRs |
|----|------------|-------------|
| US-01 | Là coach, tôi muốn thêm và sửa bài tập với tên, nhóm cơ, dụng cụ, video mẫu, liều mặc định, hướng dẫn kỹ thuật và lỗi hay gặp, để dùng lại khi soạn giáo án. | BR-f03-thu-vien-bai-tap-001, BR-f03-thu-vien-bai-tap-002, BR-f03-thu-vien-bai-tap-003, BR-f03-thu-vien-bai-tap-004, BR-f03-thu-vien-bai-tap-007, BR-f03-thu-vien-bai-tap-008, BR-f03-thu-vien-bai-tap-009, BR-f03-thu-vien-bai-tap-010, BR-f03-thu-vien-bai-tap-011, BR-f03-thu-vien-bai-tap-012, BR-f03-thu-vien-bai-tap-013, BR-f03-thu-vien-bai-tap-014, BR-f03-thu-vien-bai-tap-020, BR-f03-thu-vien-bai-tap-021 |
| US-02 | Là coach, tôi muốn tìm và lọc bài theo tên, nhóm cơ, dụng cụ, để chọn bài nhanh. | BR-f03-thu-vien-bai-tap-016, BR-f03-thu-vien-bai-tap-018, BR-f03-thu-vien-bai-tap-019 |
| US-03 | Là coach, tôi muốn ẩn, bỏ ẩn và xoá bài, để thư viện gọn mà không làm hỏng giáo án đã có. | BR-f03-thu-vien-bai-tap-015, BR-f03-thu-vien-bai-tap-017 |
| US-04 | Là coach, tôi muốn có sẵn danh mục nhóm cơ, dụng cụ và thêm được mục mới khi chưa đủ. | BR-f03-thu-vien-bai-tap-005, BR-f03-thu-vien-bai-tap-006 |

## 3. Business Rules

### Bài tập
- `BR-f03-thu-vien-bai-tap-001` — Mỗi bài trong thư viện của coach là duy nhất theo cặp (tên bài, dụng cụ), kể cả bài đang ẩn; hai tên chỉ khác chữ hoa/thường, dấu tiếng Việt, khoảng trắng thừa hoặc kiểu dấu gạch (-, –, —) được coi là trùng.
  - ✅ "Lateral Raise" + Cable và "Lateral Raise" + Dumbbell → tạo được cả hai
  - ✅ Sửa bài "lateral raise" + Cable thành "Lateral Raise" + Cable (chính nó) → lưu được
  - ❌ Thêm "  lateral   raise " + Cable khi đã có "Lateral Raise" + Cable → báo trùng, không tạo
  - ❌ Thêm "Ep nguc" khi đã có "Ép ngực" cùng dụng cụ → báo trùng
  - ❌ Thêm "T-Bar Row" khi đã có "T–Bar Row" cùng dụng cụ → báo trùng
  - ❌ Thêm bài trùng một bài đang ẩn → báo trùng, gợi ý bỏ ẩn bài đó
  - ❌ Sửa bài A thành (tên, dụng cụ) của bài B → báo trùng, A giữ nguyên
- `BR-f03-thu-vien-bai-tap-002` — Tên bài là bắt buộc và dài tối đa 100 ký tự sau khi bỏ khoảng trắng đầu/cuối.
  - ✅ "Barbell Bench Press" → lưu được
  - ❌ Tên trống hoặc chỉ có khoảng trắng → báo thiếu tên
  - ❌ Tên 101 ký tự → không lưu
- `BR-f03-thu-vien-bai-tap-003` — Mỗi bài có đúng một nhóm cơ thuộc danh mục nhóm cơ của coach.
  - ✅ Nhóm cơ "Ngực" → lưu được
  - ❌ Không có nhóm cơ → báo thiếu nhóm cơ
  - ❌ Nhóm cơ không thuộc danh mục → không lưu
- `BR-f03-thu-vien-bai-tap-004` — Mỗi bài có đúng một dụng cụ thuộc danh mục dụng cụ của coach.
  - ✅ Dụng cụ "Cable" → lưu được
  - ❌ Không có dụng cụ → báo thiếu dụng cụ
  - ❌ Dụng cụ không thuộc danh mục → không lưu
- `BR-f03-thu-vien-bai-tap-007` — Link video mẫu, khi có, phải là địa chỉ web http hoặc https của YouTube (tên miền youtube.com hoặc youtu.be).
  - ✅ https://www.youtube.com/watch?v=vcBig73ojpE → lưu được
  - ✅ https://youtube.com/shorts/PfmfAeMpym4, https://youtu.be/abc123XYZ09, link playlist hoặc kênh YouTube → lưu được
  - ✅ Không có link → lưu được
  - ❌ https://vimeo.com/123 → không lưu
  - ❌ https://youtube.com.evil.com/watch?v=x → không lưu
  - ❌ "javascript:alert(1)" hoặc "abc" → không lưu
- `BR-f03-thu-vien-bai-tap-008` — Bài không có link video được đánh dấu là thiếu video.
  - ✅ Bài không có link → ở trạng thái thiếu video
  - ✅ Bài có link → không ở trạng thái thiếu video
- `BR-f03-thu-vien-bai-tap-009` — Số hiệp (Sets) mặc định, khi có, là số nguyên từ 1 đến 10.
  - ✅ 3 → lưu được
  - ✅ Không có → lưu được
  - ❌ 0, 11 hoặc "abc" → không lưu
- `BR-f03-thu-vien-bai-tap-010` — Số lần (Reps) mặc định, khi có, là một số hoặc một khoảng thấp–cao trong 1–100, số thấp không lớn hơn số cao.
  - ✅ "10" → lưu được
  - ✅ "10–12" hoặc "10-12" → lưu là 10–12
  - ❌ "12–10" → không lưu
  - ❌ "0" hoặc "101" → không lưu
- `BR-f03-thu-vien-bai-tap-011` — RIR mặc định, khi có, là một số hoặc một khoảng thấp–cao trong 0–5, số thấp không lớn hơn số cao.
  - ✅ "1" → lưu được
  - ✅ "1–2" → lưu được
  - ❌ "6" → không lưu
  - ❌ "2–1" → không lưu
- `BR-f03-thu-vien-bai-tap-012` — Thời gian nghỉ mặc định, khi có, tính bằng giây từ 15 đến 600.
  - ✅ 90 → lưu được, đọc là "90s"
  - ❌ 10 hoặc 601 → không lưu
- `BR-f03-thu-vien-bai-tap-013` — Hướng dẫn kỹ thuật không bắt buộc, dài tối đa 2000 ký tự.
  - ✅ "Giữ khuỷu hơi cong, kéo bằng vai" → lưu được
  - ❌ 2001 ký tự → không lưu
- `BR-f03-thu-vien-bai-tap-014` — Lỗi hay gặp không bắt buộc, dài tối đa 2000 ký tự.
  - ✅ "Nhún vai khi kéo" → lưu được
  - ❌ 2001 ký tự → không lưu
- `BR-f03-thu-vien-bai-tap-020` — Lần lưu bài dựa trên một phiên bản đã cũ bị từ chối và không ghi đè lần lưu trước.
  - ✅ Lưu dựa trên phiên bản mới nhất → lưu được
  - ❌ Hai lần sửa cùng lúc, lần sau dựa trên phiên bản cũ → báo bài vừa được sửa ở nơi khác
- `BR-f03-thu-vien-bai-tap-021` — Sửa bài trong thư viện không làm thay đổi bài đó trong giáo án đã có; giáo án giữ bản chụp lúc thêm bài.
  - ✅ Sửa hướng dẫn kỹ thuật của bài → bài trong thư viện mang nội dung mới
  - ❌ Giáo án đã phát hành tự đổi theo thư viện → sai (kiểm khi có F05)

### Danh mục
- `BR-f03-thu-vien-bai-tap-005` — Danh mục ban đầu của coach gồm 11 nhóm cơ (Ngực, Vai, Tay sau, Tay trước, Lưng, Vai sau, Đùi trước, Đùi sau, Mông, Bắp chân, Bụng) và 7 dụng cụ (Machine, Cable, Dumbbell, Barbell, Smith Machine, Kettlebell, Bodyweight).
  - ✅ Coach bắt đầu dùng thư viện → có sẵn 11 nhóm cơ và 7 dụng cụ trên
- `BR-f03-thu-vien-bai-tap-006` — Coach thêm được nhóm cơ hoặc dụng cụ mới có tên không trùng (không phân biệt hoa/thường, dấu, khoảng trắng thừa) với mục cùng loại đã có.
  - ✅ Thêm dụng cụ "Resistance Band" → dùng được ngay cho bài mới
  - ❌ Thêm nhóm cơ "ngực" khi đã có "Ngực" → báo trùng
  - ❌ Tên danh mục trống → không thêm

### Ẩn, xoá, tìm
- `BR-f03-thu-vien-bai-tap-015` — Coach ẩn và bỏ ẩn được bất kỳ bài nào trong thư viện của mình.
  - ✅ Ẩn bài → bài ở trạng thái đã ẩn
  - ✅ Bỏ ẩn bài đã ẩn → bài hết ẩn
- `BR-f03-thu-vien-bai-tap-016` — Danh sách và kết quả tìm bài mặc định không gồm bài đã ẩn; coach chỉ thấy bài đã ẩn khi chọn xem bài đã ẩn.
  - ✅ Có 1 bài ẩn → không có trong danh sách mặc định
  - ✅ Chọn xem bài đã ẩn → thấy bài đó
- `BR-f03-thu-vien-bai-tap-017` — Bài đang được dùng trong giáo án không xoá được, chỉ ẩn được; bài chưa được dùng xoá được và cặp (tên, dụng cụ) của nó được dùng lại.
  - ✅ Xoá bài chưa có giáo án nào dùng → bài không còn trong thư viện, thêm lại cùng (tên, dụng cụ) được
  - ❌ Xoá bài đang có trong một phase → từ chối, gợi ý ẩn
- `BR-f03-thu-vien-bai-tap-018` — Tìm bài theo tên khớp một phần, không phân biệt hoa/thường và dấu tiếng Việt.
  - ✅ "incline" → thấy "45° Incline Chest Fly"
  - ✅ "ep nguc" → thấy "Ép ngực"
  - ✅ Từ khoá không khớp bài nào → kết quả rỗng
- `BR-f03-thu-vien-bai-tap-019` — Lọc theo nhóm cơ, lọc theo dụng cụ và từ khoá tên kết hợp với nhau; bài phải thoả mọi điều kiện đã chọn.
  - ✅ Ngực + Dumbbell + "fly" → chỉ các bài thoả cả ba
  - ✅ Chỉ chọn Cable → mọi bài dụng cụ Cable

## 4. Design Requirements / NFR + Compliance/Security
- `NFR-f03-thu-vien-bai-tap-001` (Performance) — Tìm và lọc bài có p95 < 500 ms với 500 bài trong thư viện.
- `NFR-f03-thu-vien-bai-tap-002` (Security) — Thư viện và danh mục thuộc coach: coach chỉ đọc/sửa dữ liệu của mình, học viên không truy cập được bất kỳ dữ liệu thư viện nào.
- `NFR-f03-thu-vien-bai-tap-003` (Data) — 99 bài trong prototype là thư viện thật của coach, đã gán theo danh mục ban đầu (bảng gán ở tech-design, PO duyệt). Thư viện của coach được nạp sẵn 99 bài này ở mọi môi trường, một lần duy nhất khi thư viện còn trống; không nạp lại, không ghi đè bài coach đã sửa hay đã xoá.

## 5. Out of Scope
- "Thêm vào giáo án" và thông tin "Đang dùng trong" (F05).
- Bài thay thế.
- Đổi tên, xoá danh mục nhóm cơ/dụng cụ; một bài nhiều nhóm cơ.
- Upload video; kiểm video YouTube có tồn tại.
- Import bài tập từ file — increment sau, khi coach gửi template.
- Audit log thêm/sửa/ẩn/xoá bài.

## 6. Open Questions
| # | Question | Type | Blocking? | Owner | Resolution |
|---|----------|------|-----------|-------|------------|
| OQ-01 | Danh mục theo PRD hay prototype? | Business | ✅ | PO | PRD: 11 nhóm cơ, 7 dụng cụ; seed prototype gán lại, bảng gán duyệt ở tech-design (2026-11-26) |
| OQ-02 | P1 có xoá bài không khi chưa có giáo án? | Business | ✅ | PO | Có; kiểm "đang dùng" là điểm cắm F05 nối vào (2026-11-26) |
| OQ-03 | Liều mặc định dạng nào? | Business | ✅ | PO | Có cấu trúc, không bắt buộc: Sets 1–10, Reps 1–100 số/khoảng, RIR 0–5 số/khoảng, nghỉ 15–600 giây (2026-11-26) |
| OQ-04 | Quản lý danh mục tới đâu? | Business | ✅ | PO | Chỉ thêm mới (2026-11-26) |
| OQ-05 | Hướng dẫn kỹ thuật / lỗi hay gặp / bài thay thế? | Business | ✅ | PO | 2 trường chữ ≤ 2000; bài thay thế để sau (2026-11-26) |
| OQ-06 | Bài đã ẩn hiện thế nào? | Business | ✅ | PO | Mặc định không hiện, chọn xem bài đã ẩn để bỏ ẩn (2026-11-26) |
| OQ-07 | Seed và import? | Business | ✅ | PO | 99 bài prototype là thư viện thật, nạp ở mọi môi trường khi thư viện trống (PO đổi 2026-11-27); import bài tập làm sau khi coach gửi template (2026-11-26) |
| OQ-08 | Link YouTube dạng nào? | Business | ✅ | PO | Mọi link YouTube (youtube.com, youtu.be) (2026-11-26) |

## 7. Dependencies
| Dependency | Type | Status |
|------------|------|--------|
| F05 Soạn giáo án — cung cấp thông tin "bài đang được dùng" và bản chụp bài khi thêm vào giáo án | feature | pending (P2) |
| YouTube — chỉ lưu link | service | available |
