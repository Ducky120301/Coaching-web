---
doc-id: 2026-11-26-f02-import-excel
status: completed
---

# Spec: Import hồ sơ học viên từ Excel + form theo template (increment F02)

## 1. Problem & Why
Coach đang có sẵn nhiều câu trả lời Google Form của học viên, xuất ra Excel. Hiện phải gõ lại từng hồ sơ (~45 mục) bằng tay. Coach đã chuẩn hoá file Excel (1 sheet, 40 cột theo câu hỏi Google Form). Coach cần đưa cả file vào hệ thống một lần, biết dòng nào lỗi để sửa và import lại.

Form hồ sơ hiện tại lệch với Google Form: có hai mục Google Form không hỏi (Lịch rảnh, Dụng cụ); có bốn câu hỏi mà Google Form hỏi gộp thành một (mỡ + cách đo; cân nặng + chiều cao trước khi tập; 4 ảnh chỉ 1 link); "Số năm tập luyện" ở Google Form là chữ tự do. PO chốt sửa form theo Excel để dữ liệu import không phải đoán. Quyết định import đã chốt ở spec F02 (OQ-01): dòng có ô sai bị bỏ cả dòng và báo từng ô; trùng email bị bỏ qua và báo lại; hồ sơ import ở Chờ kích hoạt + Đang coaching và không gửi email.

## 2. User Stories
| ID | User Story | Related BRs |
|----|------------|-------------|
| US-01 | Là coach, tôi muốn form hồ sơ hỏi đúng các câu như Google Form, để dữ liệu import và nhập tay khớp nhau. | BR-f02-import-excel-001, BR-f02-import-excel-002, BR-f02-import-excel-003, BR-f02-import-excel-004, BR-f02-import-excel-005, BR-f02-import-excel-006, BR-f02-import-excel-007, BR-f02-import-excel-008, BR-f02-import-excel-009, BR-f02-import-excel-010, BR-f02-import-excel-011 |
| US-02 | Là coach, tôi muốn import file Excel câu trả lời Google Form để tạo nhiều hồ sơ học viên một lần. | BR-f02-import-excel-012, BR-f02-import-excel-013, BR-f02-import-excel-014, BR-f02-import-excel-015, BR-f02-import-excel-016, BR-f02-import-excel-017, BR-f02-import-excel-018, BR-f02-import-excel-019, BR-f02-import-excel-020, BR-f02-import-excel-022, BR-f02-import-excel-027, BR-f02-import-excel-028, BR-f02-import-excel-029, BR-f02-import-excel-030, BR-f02-import-excel-031, BR-f02-import-excel-032, BR-f02-import-excel-033 |
| US-03 | Là coach, tôi muốn biết dòng nào đã tạo, dòng nào bị bỏ qua và lý do từng ô, để sửa file rồi import lại. | BR-f02-import-excel-021, BR-f02-import-excel-023, BR-f02-import-excel-024, BR-f02-import-excel-025, BR-f02-import-excel-026, BR-f02-import-excel-034 |

## 3. Business Rules

### Form hồ sơ theo template Google Form
- `BR-f02-import-excel-001` — Hồ sơ học viên không có mục "Lịch rảnh" và "Dụng cụ". _(Xóa: hai mục đã thêm ở F02)_
  - ✅ Coach tạo hồ sơ mới → hồ sơ không có Lịch rảnh, Dụng cụ
  - ❌ Dữ liệu gửi lên có Lịch rảnh "Tối 2-4-6" → không được ghi nhận vào hồ sơ
- `BR-f02-import-excel-002` — Giá trị Lịch rảnh và Dụng cụ của các hồ sơ đã có bị xoá khi thay đổi form được áp dụng. _(Xóa)_
  - ✅ Hồ sơ có Lịch rảnh "Tối 2-4-6", Dụng cụ "Tạ đơn" → sau thay đổi, hồ sơ không còn hai giá trị này
- `BR-f02-import-excel-003` — "Số năm tập luyện" nhận chữ tự do dài tối đa 200 ký tự và giữ nguyên văn. _(Thay đổi: trước chỉ nhận 1 trong 7 giá trị cho trước, theo rule F02 "mục có danh sách giá trị cho trước")_
  - ✅ "1" → hồ sơ ghi "1"
  - ✅ "Khoảng 2 năm rưỡi" → ghi nguyên văn
  - ✅ Hồ sơ cũ có "1–2 năm" → vẫn là "1–2 năm"
  - ❌ 201 ký tự → không được lưu
- `BR-f02-import-excel-004` — "Mỡ cơ thể và cách đo" là một mục chữ tự do dài tối đa 200 ký tự. _(Thay đổi: trước là mỡ % trong khoảng 3–70 cộng cách đo chọn theo danh sách, rule F02 về khoảng số và danh sách giá trị)_
  - ✅ "34" → ghi "34"
  - ✅ "18% đo InBody" → ghi nguyên văn
  - ❌ 201 ký tự → không được lưu
- `BR-f02-import-excel-005` — "Cân nặng và chiều cao trước khi tập tạ" là một mục chữ tự do dài tối đa 2000 ký tự. _(Thay đổi: trước là cân nặng 20–300 kg và chiều cao 100–250 cm, hai mục số riêng, rule F02 về khoảng số)_
  - ✅ "60kg, 1m70" → ghi nguyên văn
  - ❌ 2001 ký tự → không được lưu
- `BR-f02-import-excel-006` — Hồ sơ có một "Link ảnh toàn thân" là địa chỉ web http hoặc https, dài tối đa 2048 ký tự. _(Thay đổi: trước là 4 link ảnh trước, sau, trái, phải, rule F02 về link ảnh)_
  - ✅ "https://drive.google.com/open?id=abc" → ghi nhận
  - ❌ "drive.google.com/abc" → không được lưu, link không hợp lệ
  - ❌ "ftp://x.vn/a.jpg" → không được lưu
- `BR-f02-import-excel-007` — Mục bắt buộc của hồ sơ gồm "Số năm tập luyện", "Cân nặng và chiều cao trước khi tập tạ" và "Link ảnh toàn thân"; "Mỡ cơ thể và cách đo" không bắt buộc. _(Thay đổi danh sách mục bắt buộc của F02: bỏ 4 ảnh và 2 mục số, thêm Link ảnh toàn thân và mục gộp)_
  - ✅ Hồ sơ để trống Link ảnh toàn thân → danh sách còn thiếu có "Link ảnh toàn thân"
  - ✅ Hồ sơ để trống Mỡ cơ thể và cách đo → danh sách còn thiếu không có mục này
- `BR-f02-import-excel-008` — Với hồ sơ đã có, mỡ % và cách đo cũ được gộp vào "Mỡ cơ thể và cách đo" dạng "<mỡ> % – <cách đo>", bỏ phần trống.
  - ✅ Mỡ 18, cách đo InBody → "18 % – InBody"
  - ✅ Chỉ có mỡ 18 → "18 %"
  - ✅ Chỉ có cách đo InBody → "InBody"
  - ✅ Không có cả hai → mục để trống
- `BR-f02-import-excel-009` — Với hồ sơ đã có, cân nặng và chiều cao trước khi tập cũ được gộp vào "Cân nặng và chiều cao trước khi tập tạ" dạng "<cân nặng> kg, <chiều cao> cm", bỏ phần trống.
  - ✅ 60 và 170 → "60 kg, 170 cm"
  - ✅ Chỉ có 60 → "60 kg"
  - ✅ Không có cả hai → mục để trống
- `BR-f02-import-excel-010` — Với hồ sơ đã có, "Link ảnh toàn thân" lấy link ảnh cũ đầu tiên có giá trị theo thứ tự trước, sau, trái, phải.
  - ✅ Có đủ 4 link → lấy link ảnh trước
  - ✅ Chỉ có link ảnh bên trái → lấy link đó
  - ✅ Không có link nào → mục để trống
- `BR-f02-import-excel-011` — Với hồ sơ đã có, các link ảnh cũ không được chọn làm "Link ảnh toàn thân" được nối vào cuối ghi chú nội bộ, mỗi link một dòng ghi rõ tên ảnh.
  - ✅ Có 4 link → ghi chú nội bộ thêm "Ảnh mặt sau: …", "Ảnh bên trái: …", "Ảnh bên phải: …"
  - ✅ Ghi chú đang có "Hay đau gối" → các dòng ảnh được thêm sau "Hay đau gối"
  - ✅ Chỉ có 1 link → ghi chú không đổi

### Import Excel
- `BR-f02-import-excel-012` — Chỉ coach đã đăng nhập mới import được hồ sơ.
  - ✅ Coach import file hợp lệ → hồ sơ được tạo
  - ❌ Học viên import → bị từ chối, không hồ sơ nào được tạo
  - ❌ Người chưa đăng nhập import → bị từ chối
- `BR-f02-import-excel-013` — Hồ sơ tạo từ import thuộc coach đã import.
  - ✅ Coach A import → hồ sơ nằm trong danh sách học viên của coach A
- `BR-f02-import-excel-014` — File import phải là file Excel .xlsx đọc được.
  - ✅ File .xlsx từ Google Sheets → được xử lý
  - ❌ File .csv, .xls, file hỏng hoặc có mật khẩu → từ chối cả file, nêu lý do, không tạo gì
- `BR-f02-import-excel-015` — File import tối đa 5 MB.
  - ✅ File 50 KB → được xử lý
  - ❌ File 6 MB → từ chối cả file, nêu giới hạn 5 MB
- `BR-f02-import-excel-016` — File import có tối đa 500 dòng dữ liệu.
  - ✅ File 500 dòng → được xử lý
  - ❌ File 501 dòng → từ chối cả file, nêu giới hạn 500 dòng
- `BR-f02-import-excel-017` — Chỉ sheet đầu tiên của file được đọc.
  - ✅ File có 2 sheet → chỉ dữ liệu sheet đầu được xử lý
- `BR-f02-import-excel-018` — Cột được nhận ra theo tiêu đề tiếng Việt của câu hỏi, không phụ thuộc thứ tự cột.
  - ✅ Cột "Họ Và Tên" đứng trước cột "Địa chỉ email" → vẫn đọc đúng
  - ✅ Tiêu đề song ngữ "Số đo Vòng 1 / Chest measurement" → nhận là Vòng 1
- `BR-f02-import-excel-019` — File thiếu cột email hoặc cột họ tên bị từ chối cả file.
  - ✅ File đủ 2 cột này, thiếu cột Nghề nghiệp → vẫn được xử lý
  - ❌ File không có cột "Địa chỉ email" → từ chối, nêu tên cột thiếu
- `BR-f02-import-excel-020` — Cột không thuộc form hồ sơ bị bỏ qua.
  - ✅ Cột "Dấu thời gian" → không ghi vào hồ sơ, không gây lỗi
- `BR-f02-import-excel-021` — File không có dòng dữ liệu nào thì không tạo hồ sơ và báo không có dòng dữ liệu.
  - ✅ File chỉ có dòng tiêu đề → báo không có dòng dữ liệu
- `BR-f02-import-excel-022` — Dòng hoàn toàn trống bị bỏ qua và không tính là lỗi.
  - ✅ Dòng trống giữa hai dòng hợp lệ → hai dòng kia được tạo, dòng trống không được báo
- `BR-f02-import-excel-023` — Dòng có ô mang giá trị không hợp lệ theo luật của hồ sơ bị bỏ qua cả dòng, và mỗi ô sai được báo kèm số dòng Excel, tên cột và lý do.
  - ✅ Dòng 4 có Tuổi 5 → dòng 4 bỏ qua, báo "dòng 4, Tuổi: phải từ 10 đến 100"; các dòng khác vẫn tạo
  - ❌ Ngày chuyển khoản là ngày mai theo giờ Việt Nam → dòng bỏ qua, báo ô Ngày chuyển khoản
  - ❌ Link ảnh toàn thân "drive.google.com/abc" → dòng bỏ qua
  - ❌ Email "abc" → dòng bỏ qua, báo email không hợp lệ
  - ❌ Dòng có 2 ô sai → báo cả 2 ô
- `BR-f02-import-excel-024` — Dòng để trống email hoặc họ tên bị bỏ qua và báo ô thiếu.
  - ✅ Dòng đủ email và họ tên → được xét tiếp
  - ❌ Dòng có email nhưng trống họ tên → bỏ qua, báo "Họ Và Tên: bắt buộc"
- `BR-f02-import-excel-025` — Dòng có email đã thuộc một hồ sơ khác hoặc tài khoản coach, không phân biệt hoa/thường và bỏ khoảng trắng đầu/cuối, bị bỏ qua và báo trùng; hồ sơ có sẵn không bị thay đổi.
  - ✅ Email chưa có trong hệ thống → được tạo
  - ❌ "QUAN@gmail.com " khi đã có hồ sơ quan@gmail.com → bỏ qua, báo email đã có hồ sơ
  - ❌ Email của coach → bỏ qua, báo trùng
  - ❌ Import lại đúng file vừa import xong → mọi dòng báo trùng, không tạo thêm hồ sơ
- `BR-f02-import-excel-026` — Khi nhiều dòng trong cùng file có cùng email (không phân biệt hoa/thường, bỏ khoảng trắng), chỉ dòng xuất hiện trước được xét; các dòng sau bị bỏ qua và báo trùng với dòng trước.
  - ✅ Dòng 3 và dòng 7 cùng email → dòng 3 xét bình thường
  - ❌ Dòng 7 → bỏ qua, báo "trùng email với dòng 3"
- `BR-f02-import-excel-027` — Dòng có email và họ tên hợp lệ, các ô có giá trị đều hợp lệ, được tạo hồ sơ dù còn để trống mục bắt buộc khác.
  - ✅ Dòng chỉ có email và họ tên → tạo hồ sơ, hồ sơ cho biết còn thiếu các mục bắt buộc
- `BR-f02-import-excel-028` — Hồ sơ tạo từ import có tài khoản Chờ kích hoạt và coaching Đang coaching.
  - ✅ Import 3 dòng hợp lệ → 3 hồ sơ Chờ kích hoạt, Đang coaching
- `BR-f02-import-excel-029` — Import không gửi email nào cho học viên.
  - ✅ Import 3 dòng hợp lệ → không học viên nào nhận email
- `BR-f02-import-excel-030` — Giá trị của mục có danh sách cho trước được đưa về đúng giá trị trong danh sách khi chỉ khác chữ hoa/thường, khoảng trắng, kiểu dấu gạch hoặc dấu "/", hoặc khi là một cách viết đã biết của giá trị đó; giá trị không đưa về được là ô không hợp lệ.
  - ✅ "Ít vận động (Ngồi nhiều)" → "Ít vận động (ngồi nhiều)"
  - ✅ "5-6 tiếng" → "5–6 tiếng"
  - ✅ "pack 1" → "Pack 1"
  - ✅ "PUSH PULL LEG" → "Push / Pull / Legs"
  - ❌ Mức độ căng thẳng "Cao quá" → ô không hợp lệ
- `BR-f02-import-excel-031` — Ngày chuyển khoản trong file được nhận khi là ô ngày của Excel hoặc chữ dạng ngày/tháng/năm (dd/MM/yyyy).
  - ✅ Ô ngày Excel 12/11/2026 → ngày chuyển khoản 12/11/2026
  - ✅ Chữ "25/11/2026" → ngày chuyển khoản 25/11/2026
  - ❌ "11/25/2026" → ô không hợp lệ
  - ❌ "hôm qua" → ô không hợp lệ
- `BR-f02-import-excel-032` — Giá trong file được nhận khi là ô số nguyên hoặc chữ gồm số với dấu chấm phân cách nghìn, có thể kèm đuôi "đ" hoặc "VNĐ".
  - ✅ Ô số 10000000 → giá 10.000.000 đồng
  - ✅ "10.000.000đ" → giá 10.000.000 đồng
  - ❌ "10tr" → ô không hợp lệ
  - ❌ "10000000,5" → ô không hợp lệ
  - ❌ "-100" → ô không hợp lệ
- `BR-f02-import-excel-033` — Mỗi dòng được xử lý độc lập; hồ sơ đã tạo được giữ lại dù dòng sau bị bỏ qua hoặc quá trình dừng giữa chừng.
  - ✅ 10 dòng, 8 hợp lệ → 8 hồ sơ được tạo
  - ✅ Quá trình dừng ở dòng 30/100 → 29 hồ sơ đầu đã tạo vẫn còn
- `BR-f02-import-excel-034` — Kết quả import cho coach biết số dòng đã tạo, số dòng bị bỏ qua và từng lý do kèm số dòng Excel.
  - ✅ 5 dòng: 3 hợp lệ, 1 trùng email, 1 tuổi sai → "Đã tạo 3, bỏ qua 2" kèm 2 lý do
  - ✅ 2 dòng hợp lệ → "Đã tạo 2, bỏ qua 0"

## 4. Design Requirements / NFR + Compliance/Security
- `NFR-f02-import-excel-001` (Performance) — Import file 500 dòng hoàn tất dưới 60 giây trên môi trường staging, và các thao tác khác của hệ thống vẫn phản hồi trong lúc import.
- `NFR-f02-import-excel-002` (Security) — File upload không được lưu lại trên server sau khi xử lý xong; kết quả import chỉ gồm số dòng, email của dòng, mã hồ sơ đã tạo, tên cột và lý do (PO chốt 2026-11-26).
- `NFR-f02-import-excel-003` (Compliance) — Thư viện đọc file Excel (nếu dùng) phải có giấy phép tự do (MIT hoặc Apache 2.0).
- `NFR-f02-import-excel-004` (Data) — Việc chuyển dữ liệu hồ sơ cũ sang mục mới chạy một lần khi triển khai và chạy lại không làm sai dữ liệu đã chuyển.

## 5. Out of Scope
- Import cập nhật hồ sơ đã có (trùng email luôn bị bỏ qua).
- Tải ảnh từ link Drive về hệ thống.
- Import trực tiếp từ Google Sheets, import nhiều sheet, file mẫu tải về.
- Gửi email thông tin đăng nhập khi import (coach gửi sau, từng hồ sơ).
- Hoàn tác cả lô import.
- Lưu "Dấu thời gian" của Google Form.
- Danh sách cụ thể các cách viết đã biết ngoài ví dụ trong spec — dev đề xuất ở tech-design theo file mẫu, PO duyệt.

## 6. Open Questions
| # | Question | Type | Blocking? | Owner | Resolution |
|---|----------|------|-----------|-------|------------|
| OQ-01 | Cột gộp S (mỡ + cách đo), W (cân nặng + chiều cao trước khi tập), AK (4 ảnh, 1 link) xử lý thế nào? | Business | ✅ | PO | Sửa form theo Excel: 3 mục chữ/link gộp (PO 2026-11-26) |
| OQ-02 | Dòng để trống mục bắt buộc có bị bỏ không? | Business | ✅ | PO | Không, vẫn tạo hồ sơ (PO 2026-11-26) |
| OQ-03 | Chấp nhận giá trị lệch danh sách đến mức nào? | Business | ✅ | PO | Chuẩn hoá về giá trị design + bảng cách viết đã biết (PO 2026-11-26) |
| OQ-04 | Dữ liệu Lịch rảnh/Dụng cụ cũ? | Business | ✅ | PO | Xoá (PO 2026-11-26) |
| OQ-05 | Mục gộp có bắt buộc không? | Business | ✅ | PO | Số năm tập, Cân nặng và chiều cao trước khi tập, Link ảnh bắt buộc; Mỡ + cách đo không (PO 2026-11-26) |
| OQ-06 | Ngày/Giá dạng chữ? | Business | ✅ | PO | Nhận dd/MM/yyyy và số có dấu chấm nghìn + đ/VNĐ (PO 2026-11-26) |
| OQ-07 | Giới hạn file và cách nhận cột? | Business | ✅ | PO | 5 MB, 500 dòng, theo tiêu đề, sheet đầu (PO 2026-11-26) |
| OQ-08 | Dữ liệu cũ của mục bị gộp? | Business | ✅ | PO | Gộp sang mục mới; link ảnh thừa vào ghi chú nội bộ (PO 2026-11-26) |

## 7. Dependencies
| Dependency | Type | Status |
|------------|------|--------|
| File Excel chuẩn hoá của coach (`Copy of GỬI ĐỨC (Câu trả lời)-2.xlsx`) | team | available |
| Thiết kế prototype AdTaoHoSo/AdHoSo cần sửa theo form mới (bỏ 2 mục, gộp mục) | feature | pending — to-tech-design |
| F02 hồ sơ học viên, F01-P1 tài khoản | feature | available |
