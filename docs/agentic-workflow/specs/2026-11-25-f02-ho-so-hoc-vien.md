---
doc-id: 2026-11-25-f02-ho-so-hoc-vien
status: ready
---

# Spec: F02 — Hồ sơ học viên & trạng thái coaching

> Nguồn: `docs/prd/F02-ho-so-hoc-vien.md` + hai rule "email học viên duy nhất" và "hồ sơ mới Chờ kích hoạt" của `docs/prd/F01-tai-khoan-phan-quyen.md` (PO chuyển vào F02). PRD tổng `docs/PRD-coaching-web.md`. Không có Confluence.

## 1. Problem & Why

Coach cần một hồ sơ duy nhất cho mỗi học viên. Hồ sơ gồm câu trả lời form đăng ký, ghi chú nội bộ chỉ coach thấy, và trạng thái coaching (Đang coaching / Tạm dừng / Đã kết thúc) có lịch sử. Hồ sơ là điểm vào của mọi tính năng sau: gửi tài khoản (F01), phase (F05), review (F09), tiến độ (F08).

Spec này là spec đầu tiên của P1. Thứ tự P1 PO đã chốt: F02 → F01-P1 (gửi tài khoản, kích hoạt, quên mật khẩu) → F03 → F04. Dữ liệu hồ sơ bám đúng form đăng ký trong prototype (`AdTaoHoSo` / `AdHoSo`, thêm 2 mục Lịch rảnh và Dụng cụ); coach sẽ sửa Google Form cho khớp. Chi tiết giao diện thuộc tech-design.

## 2. User Stories

| ID | User Story | Related BRs |
|----|------------|-------------|
| US-01 | Là coach, tôi muốn tạo hồ sơ học viên theo câu trả lời form đăng ký, để có một điểm vào duy nhất cho phase, review và tiến độ. | BR-f02-ho-so-hoc-vien-001, BR-f02-ho-so-hoc-vien-002, BR-f02-ho-so-hoc-vien-003, BR-f02-ho-so-hoc-vien-004, BR-f02-ho-so-hoc-vien-005, BR-f02-ho-so-hoc-vien-006, BR-f02-ho-so-hoc-vien-007, BR-f02-ho-so-hoc-vien-008, BR-f02-ho-so-hoc-vien-009, BR-f02-ho-so-hoc-vien-010, BR-f02-ho-so-hoc-vien-011 |
| US-02 | Là coach, tôi muốn sửa thông tin hồ sơ mà không làm mất chỉnh sửa ở nơi khác, để dữ liệu luôn đúng. | BR-f02-ho-so-hoc-vien-012, BR-f02-ho-so-hoc-vien-013, BR-f02-ho-so-hoc-vien-015, BR-f02-ho-so-hoc-vien-022 |
| US-03 | Là coach, tôi muốn ghi chú nội bộ mà học viên không bao giờ thấy, để lưu nhận xét riêng. | BR-f02-ho-so-hoc-vien-014, BR-f02-ho-so-hoc-vien-016 |
| US-04 | Là coach, tôi muốn đổi trạng thái coaching có lý do và lịch sử, để biết vì sao và khi nào học viên dừng hoặc quay lại. | BR-f02-ho-so-hoc-vien-017, BR-f02-ho-so-hoc-vien-018, BR-f02-ho-so-hoc-vien-019, BR-f02-ho-so-hoc-vien-020, BR-f02-ho-so-hoc-vien-021 |
| US-05 | Là coach, tôi muốn xem danh sách tất cả học viên và tìm theo tên hoặc email, để mở nhanh hồ sơ. | BR-f02-ho-so-hoc-vien-023, BR-f02-ho-so-hoc-vien-024 |
| US-06 | Là coach, tôi muốn không bao giờ mất hồ sơ, để giữ lịch sử học viên. | BR-f02-ho-so-hoc-vien-025 |

## 3. Business Rules

Mọi rule là `Thêm mới` (chưa có living doc nào về hồ sơ học viên).

**Danh sách mục hồ sơ** (theo form đăng ký; `*` = bắt buộc trước khi gửi tài khoản, F01-P1 kiểm):
1. Tài khoản và gói: Họ và tên\*, Email đăng nhập\*, Gói đã mua\* (Pack 1 / Pack 2 / Pack 3 / Khác), Giá\* (đồng), Ngày chuyển khoản\*, Địa chỉ\*, Tuổi\*, Chiều cao\* (cm).
2. Số đo cơ thể hiện tại: Cân nặng\* (kg); Vòng 1 ngực, Vòng 2 bụng, Vòng eo (trên rốn 4 cm), Vòng 3 mông, Tay phải, Tay trái, Đùi phải, Đùi trái (cm); Mỡ cơ thể (%); Cách đo mỡ (Ước lượng bằng mắt / Cân điện tử / InBody / Thước kẹp mỡ / DEXA / Khác).
3. Tập luyện hiện tại: Số năm tập luyện\* (Chưa tập / Dưới 6 tháng / 6–12 tháng / 1–2 năm / 2–3 năm / 3–5 năm / Trên 5 năm), Thời gian dành cho tập mỗi tuần\*, Cân nặng trước khi tập tạ\* (kg), Chiều cao khi đó\* (cm), Mục tiêu tập luyện\*, Chế độ tập hiện tại, Tiền sử bệnh lý và chấn thương\*, Lịch rảnh, Dụng cụ.
4. Ăn uống, sinh hoạt: Sẵn sàng đầu tư thực phẩm\* (Sẵn sàng / Đang cân nhắc / Chưa sẵn sàng), Nghề nghiệp, Mức độ hoạt động\* (Ít vận động (ngồi nhiều) / Nhẹ / Vừa / Nhiều / Rất nhiều (lao động chân tay)), Mức độ căng thẳng\* (Thấp / Vừa / Cao / Rất cao), Thời gian ngủ\* (Dưới 5 tiếng / 5–6 tiếng / 6–7 tiếng / 7–8 tiếng / Trên 8 tiếng), Caffeine, Thực phẩm bổ sung, Chế độ dinh dưỡng hiện tại\*, Giờ có thể ăn và giờ đi tập\*.
5. Chương trình tập sắp tới: Số buổi tập mỗi tuần\* (2 buổi … 7 buổi), Phong cách giáo án\* (Push / Pull / Legs; Upper / Lower; Full body; Mỗi buổi một nhóm cơ; Để coach chọn; Khác), Ảnh mặt trước\*, Ảnh mặt sau\*, Ảnh bên trái\*, Ảnh bên phải\* (link).
6. Kỳ vọng: Mong đợi gì từ coach\*, Từng theo coach chưa\*, Lý do đầu tư lúc này\*.
7. Chỉ coach thấy: Ghi chú nội bộ.

### Tạo hồ sơ

- `BR-f02-ho-so-hoc-vien-001` — Email học viên là duy nhất trong toàn hệ thống; hai email chỉ khác nhau về chữ hoa/thường hoặc khoảng trắng đầu/cuối được coi là cùng một email.
  - ✅ Tạo hồ sơ với " An@Mail.com " khi chưa có an@mail.com → hồ sơ được tạo, email ghi nhận là "an@mail.com"
  - ❌ Tạo hồ sơ "AN@mail.com" khi đã có hồ sơ "an@mail.com" → từ chối, báo email đã được dùng
  - ❌ Tạo hồ sơ với email của tài khoản coach → từ chối, báo email đã được dùng
  - ❌ Tạo hồ sơ trùng email của một hồ sơ Đã kết thúc → từ chối, báo email đã được dùng
- `BR-f02-ho-so-hoc-vien-002` — Hồ sơ chỉ được lưu khi có email đúng định dạng.
  - ✅ Email "lan@gmail.com" → hồ sơ được lưu
  - ❌ Thiếu email → từ chối, báo thiếu email
  - ❌ Email "an@mail" → từ chối, báo email không hợp lệ
- `BR-f02-ho-so-hoc-vien-003` — Hồ sơ chỉ được lưu khi có họ tên không rỗng (sau khi bỏ khoảng trắng đầu/cuối) và dài tối đa 100 ký tự.
  - ✅ Chỉ có họ tên "Lan" và email hợp lệ, mọi mục khác trống → hồ sơ được lưu
  - ❌ Họ tên chỉ toàn khoảng trắng → từ chối, báo thiếu họ tên
  - ❌ Họ tên 101 ký tự → từ chối
- `BR-f02-ho-so-hoc-vien-004` — Hồ sơ luôn cho biết danh sách mục bắt buộc còn thiếu; mục chỉ chứa khoảng trắng tính là thiếu.
  - ✅ Hồ sơ thiếu 3 mục bắt buộc → vẫn lưu được, cho biết còn thiếu đúng 3 mục đó
  - ✅ Hồ sơ đủ mọi mục bắt buộc → không còn mục thiếu
  - ❌ Mục tiêu tập luyện chỉ toàn khoảng trắng → vẫn tính là thiếu
- `BR-f02-ho-so-hoc-vien-005` — Hồ sơ mới tạo có trạng thái tài khoản "Chờ kích hoạt".
  - ✅ Coach tạo hồ sơ → tài khoản ở trạng thái Chờ kích hoạt
  - ❌ Yêu cầu tạo hồ sơ kèm sẵn trạng thái "Đã kích hoạt" → hồ sơ vẫn ở Chờ kích hoạt
- `BR-f02-ho-so-hoc-vien-006` — Hồ sơ mới tạo có trạng thái coaching "Đang coaching".
  - ✅ Coach tạo hồ sơ → trạng thái coaching là Đang coaching
  - ❌ Yêu cầu tạo hồ sơ kèm sẵn trạng thái "Đã kết thúc" → hồ sơ vẫn ở Đang coaching
- `BR-f02-ho-so-hoc-vien-007` — Hồ sơ thuộc coach đã tạo ra nó.
  - ✅ Coach Duobexiuuu tạo hồ sơ → hồ sơ thuộc coach Duobexiuuu
- `BR-f02-ho-so-hoc-vien-008` — Số liệu của hồ sơ, khi có nhập, phải nằm trong khoảng: tuổi là số nguyên 10–100; chiều cao 100–250 cm; cân nặng 20–300 kg; mỡ cơ thể 3–70 %; mỗi vòng đo 10–250 cm; giá là số nguyên từ 0 đồng.
  - ✅ Cân nặng 72,5 kg → hợp lệ
  - ✅ Mỡ cơ thể 3 % và 70 % (biên) → hợp lệ
  - ✅ Giá 0 đồng (gói tặng) → hợp lệ
  - ❌ Cân nặng 720 kg → từ chối
  - ❌ Tuổi 9 hoặc 101 → từ chối
  - ❌ Chiều cao 250,1 cm → từ chối
  - ❌ Giá -1 đồng → từ chối
  - ❌ Cân nặng "abc" → từ chối
- `BR-f02-ho-so-hoc-vien-009` — Mục có danh sách giá trị cho trước chỉ nhận một giá trị trong danh sách của mục đó.
  - ✅ Số buổi tập mỗi tuần "4 buổi" → hợp lệ
  - ❌ Số buổi tập mỗi tuần "8 buổi" → từ chối
  - ❌ Mức độ căng thẳng "Stress-free" → từ chối
- `BR-f02-ho-so-hoc-vien-010` — Ngày chuyển khoản không được sau ngày hôm nay theo giờ Việt Nam.
  - ✅ Hôm nay 25/11 (giờ VN), ngày chuyển khoản 25/11 → hợp lệ
  - ✅ Lúc 23h30 UTC ngày 24/11 (đã là 25/11 giờ VN), ngày chuyển khoản 25/11 → hợp lệ
  - ❌ Hôm nay 25/11, ngày chuyển khoản 26/11 → từ chối
- `BR-f02-ho-so-hoc-vien-011` — Link ảnh của hồ sơ phải là địa chỉ web http hoặc https.
  - ✅ "https://drive.google.com/open?id=abc" → hợp lệ
  - ❌ "javascript:alert(1)" → từ chối
  - ❌ "drive.google.com/abc" (không có http/https) → từ chối

### Sửa hồ sơ

- `BR-f02-ho-so-hoc-vien-012` — Coach sửa được mọi mục của hồ sơ trừ email, kể cả số đo và ảnh ban đầu.
  - ✅ Coach sửa cân nặng ban đầu từ 72 thành 70 → hồ sơ ghi nhận 70
  - ❌ Sửa hồ sơ không tồn tại → báo không tìm thấy
- `BR-f02-ho-so-hoc-vien-013` — Hồ sơ ghi nhận người sửa và thời điểm sửa gần nhất (không lưu giá trị cũ).
  - ✅ Coach sửa Mục tiêu lúc 10:00 ngày 23/11 → hồ sơ cho biết sửa lần cuối 23/11 bởi coach đó
- `BR-f02-ho-so-hoc-vien-014` — Ghi chú nội bộ có thời điểm sửa gần nhất riêng, chỉ đổi khi ghi chú đổi.
  - ✅ Coach sửa ghi chú ngày 23/11 → ghi chú cho biết sửa lần cuối 23/11
  - ✅ Coach chỉ sửa Mục tiêu ngày 24/11 → thời điểm sửa ghi chú vẫn là 23/11
- `BR-f02-ho-so-hoc-vien-015` — Lần lưu dựa trên một phiên bản hồ sơ đã cũ bị từ chối, không ghi đè lần lưu trước.
  - ✅ Sửa lần lượt, lần sau dựa trên phiên bản mới nhất → lưu được
  - ❌ A và B cùng mở phiên bản 3; A lưu thành phiên bản 4; B lưu dựa trên phiên bản 3 → B bị từ chối, được yêu cầu tải lại
  - ❌ Hai yêu cầu đổi trạng thái coaching cùng lúc từ cùng một phiên bản → chỉ một yêu cầu thành công
- `BR-f02-ho-so-hoc-vien-016` — Ghi chú nội bộ không có trong bất kỳ dữ liệu nào học viên nhận được.
  - ✅ Coach xem hồ sơ → thấy ghi chú nội bộ
  - ❌ Học viên lấy dữ liệu của chính mình khi hồ sơ có ghi chú nội bộ → dữ liệu không chứa ghi chú
  - ❌ Học viên lấy dữ liệu của chính mình khi ghi chú nội bộ trống → dữ liệu vẫn không có mục ghi chú

### Trạng thái coaching

- `BR-f02-ho-so-hoc-vien-017` — Trạng thái coaching chỉ đổi sang một trạng thái khác trạng thái hiện tại.
  - ✅ Đang coaching → Tạm dừng → đổi được
  - ❌ Đang coaching → Đang coaching → từ chối
- `BR-f02-ho-so-hoc-vien-018` — Đổi trạng thái coaching cần lý do không rỗng (sau khi bỏ khoảng trắng đầu/cuối), dài tối đa 500 ký tự.
  - ✅ Tạm dừng với lý do "Đi công tác 2 tuần" → đổi được
  - ❌ Lý do chỉ toàn khoảng trắng → từ chối, trạng thái không đổi
  - ❌ Lý do 501 ký tự → từ chối
- `BR-f02-ho-so-hoc-vien-019` — Trạng thái coaching đổi được giữa Đang coaching, Tạm dừng và Đã kết thúc theo mọi chiều, kể cả mở lại hồ sơ Đã kết thúc.
  - ✅ Tạm dừng → Đang coaching → đổi được
  - ✅ Đang coaching → Đã kết thúc → đổi được
  - ✅ Đã kết thúc → Đang coaching với lý do "Mua gói mới" → mở lại được, dữ liệu cũ giữ nguyên
- `BR-f02-ho-so-hoc-vien-020` — Mỗi lần trạng thái coaching đổi thành công đều có đúng một dòng lịch sử gồm trạng thái cũ, trạng thái mới, người thực hiện, thời điểm (giờ VN) và lý do.
  - ✅ Tạm dừng lúc 09:00 ngày 25/11 (giờ VN) với lý do "Đi công tác" → lịch sử thêm đúng một dòng Đang coaching → Tạm dừng, 09:00 25/11, coach, "Đi công tác"
  - ❌ Đổi trạng thái bị từ chối → lịch sử không thêm dòng nào
- `BR-f02-ho-so-hoc-vien-021` — Lịch sử trạng thái coaching không sửa và không xóa được.
  - ✅ Coach xem lịch sử → thấy đủ các dòng, mới nhất trước
  - ❌ Yêu cầu sửa hoặc xóa một dòng lịch sử → không được chấp nhận
- `BR-f02-ho-so-hoc-vien-022` — Hồ sơ đang Chờ kích hoạt vẫn được coach sửa, ghi chú và đổi trạng thái coaching như hồ sơ đã kích hoạt.
  - ✅ Tạm dừng hồ sơ còn Chờ kích hoạt → đổi được
  - ✅ Sửa Mục tiêu của hồ sơ còn Chờ kích hoạt → lưu được

### Danh sách và lưu giữ

- `BR-f02-ho-so-hoc-vien-023` — Danh sách học viên của coach gồm mọi hồ sơ ở mọi trạng thái, hồ sơ tạo sau đứng trước.
  - ✅ Có 3 hồ sơ, 1 hồ sơ Đã kết thúc → danh sách có đủ 3, hồ sơ tạo gần nhất đứng đầu
  - ✅ Mỗi dòng cho biết tên, email, trạng thái coaching, trạng thái tài khoản
- `BR-f02-ho-so-hoc-vien-024` — Tìm học viên khớp khi từ khóa nằm trong họ tên hoặc email, không phân biệt hoa/thường và không phân biệt dấu tiếng Việt.
  - ✅ Tìm "quan" → có hồ sơ "Phạm Hoàng Quân"
  - ✅ Tìm "Quân" → có hồ sơ "Phạm Hoàng Quân"
  - ✅ Tìm "hquan" → có hồ sơ email "hquanbilly@gmail.com"
  - ❌ Tìm "zzz" → danh sách rỗng
- `BR-f02-ho-so-hoc-vien-025` — Hồ sơ học viên không bị xóa; ngừng coaching bằng cách chuyển sang Đã kết thúc.
  - ✅ Chuyển hồ sơ sang Đã kết thúc → hồ sơ vẫn còn trong danh sách
  - ❌ Yêu cầu xóa hồ sơ → không được chấp nhận

## 4. Design Requirements / NFR + Compliance/Security

- `NFR-f02-ho-so-hoc-vien-001` (Security) — Không dữ liệu nào trả cho học viên chứa ghi chú nội bộ; có test tự động quét dữ liệu trả cho học viên.
- `NFR-f02-ho-so-hoc-vien-002` (Audit) — Mỗi lần đổi trạng thái coaching có audit log (người thực hiện + thời điểm). Audit không lưu nội dung ghi chú nội bộ.
- `NFR-f02-ho-so-hoc-vien-003` (Security) — Văn bản tự do có giới hạn độ dài để chống lạm dụng; ngưỡng cụ thể chốt ở tech-design.
- `NFR-f02-ho-so-hoc-vien-004` (Privacy) — Bản nháp khi coach đang nhập hồ sơ mới chỉ nằm trên trình duyệt của coach, không gửi lên hệ thống, và bị xóa khi hồ sơ lưu thành công hoặc coach chủ động xóa.
- `NFR-f02-ho-so-hoc-vien-005` (Performance) — Danh sách học viên và hồ sơ trả về p95 < 500 ms với 500 hồ sơ.

## 5. Out of Scope

- Gửi thông tin đăng nhập, gửi lại, sửa email, kích hoạt, link đặt lại mật khẩu, khối "Tài khoản đăng nhập" trên trang hồ sơ → F01-P1.
- Import hồ sơ từ Excel → increment sau của F02, khi coach gửi file Excel mới (quyết định đã chốt ở OQ-01).
- Hạn review → F09. Gán giáo án / template, nút soạn phase → F05. Khối "Chương trình và phase", "Hoạt động gần đây" → F05/F08.
- Bộ lọc danh sách đầy đủ → F08.
- Học viên xem hồ sơ của chính mình, chặn học viên xem hồ sơ người khác → F01-P1/P4 (rule "học viên chỉ truy cập dữ liệu của mình" trong PRD F01).
- Xóa cứng hồ sơ; gán học viên cho coach khác; lưu giá trị cũ khi sửa; gộp email dạng alias (an+1@gmail.com là email khác an@gmail.com).

## 6. Open Questions

| # | Question | Type | Blocking? | Owner | Resolution |
|---|----------|------|-----------|-------|------------|
| OQ-01 | Import hồ sơ từ Excel (Google Form) | Business | ❌ | PO | Chờ file Excel mới khớp form prototype. Đã chốt: dòng có ô sai bị bỏ qua cả dòng và báo từng ô; dòng trùng email bị bỏ qua và báo lại; hồ sơ import ở Chờ kích hoạt + Đang coaching và không gửi email. Sẽ thành increment của F02. |
| OQ-02 | Danh sách lựa chọn và mục bắt buộc của form có cần coach duyệt lại không? | Business | ❌ | PO | Lấy đúng theo prototype + Lịch rảnh, Dụng cụ (không bắt buộc). PO đã chốt 2026-11-25. |

## 7. Dependencies

| Dependency | Type | Status |
|------------|------|--------|
| F00 nền tảng (đăng nhập admin, phân quyền, audit, IClock) | feature | available |
| File Excel mới từ coach (cho import) | team | pending — không chặn spec này |
