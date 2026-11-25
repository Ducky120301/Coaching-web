---
doc-id: 2026-11-25-f01-p1-tai-khoan-hoc-vien
status: draft
---

# Spec: F01-P1 Tài khoản học viên — gửi thông tin đăng nhập, kích hoạt, đặt lại mật khẩu

## 1. Problem & Why
Học viên chỉ có tài khoản theo lời mời của coach. Sau F02, mỗi hồ sơ đã có sẵn một tài khoản Chờ kích hoạt nhưng chưa có mật khẩu, nên học viên chưa vào được hệ thống. Cần một quy trình đơn giản: coach gửi email có mật khẩu tạm → học viên đăng nhập và đặt mật khẩu riêng → tài khoản Đã kích hoạt. Khi quên mật khẩu, học viên (hoặc coach thay mặt) nhận link đặt lại. Coach phải thấy trạng thái gửi và kích hoạt nhưng không bao giờ biết mật khẩu riêng của học viên.

PRD gốc: `docs/prd/F01-tai-khoan-phan-quyen.md` (phần P1, cộng phần kích hoạt và quên mật khẩu PO kéo từ P4 lên). Thứ tự P1 PO đã chốt: F02 → F01-P1 → F03 → F04.

## 2. User Stories
| ID | User Story | Related BRs |
|----|------------|-------------|
| US-01 | Là coach, tôi muốn gửi và gửi lại thông tin đăng nhập cho học viên, để học viên vào được hệ thống mà tôi không cần biết mật khẩu riêng. | BR-f01-p1-tai-khoan-hoc-vien-001, BR-f01-p1-tai-khoan-hoc-vien-002, BR-f01-p1-tai-khoan-hoc-vien-003, BR-f01-p1-tai-khoan-hoc-vien-004, BR-f01-p1-tai-khoan-hoc-vien-005, BR-f01-p1-tai-khoan-hoc-vien-006 |
| US-02 | Là coach, tôi muốn sửa email của học viên chưa kích hoạt, để sửa sai chính tả mà học viên vẫn nhận được thông tin đăng nhập. | BR-f01-p1-tai-khoan-hoc-vien-007, BR-f01-p1-tai-khoan-hoc-vien-008, BR-f01-p1-tai-khoan-hoc-vien-009, BR-f01-p1-tai-khoan-hoc-vien-010 |
| US-03 | Là học viên, tôi muốn kích hoạt tài khoản bằng mật khẩu tạm và đặt mật khẩu riêng, để tự quản lý mật khẩu của mình. | BR-f01-p1-tai-khoan-hoc-vien-011, BR-f01-p1-tai-khoan-hoc-vien-012, BR-f01-p1-tai-khoan-hoc-vien-013, BR-f01-p1-tai-khoan-hoc-vien-014, BR-f01-p1-tai-khoan-hoc-vien-015, BR-f01-p1-tai-khoan-hoc-vien-016 |
| US-04 | Là học viên đã kích hoạt, tôi muốn đặt lại mật khẩu qua link (tự xin hoặc coach gửi), để vào lại được khi quên mật khẩu. | BR-f01-p1-tai-khoan-hoc-vien-017, BR-f01-p1-tai-khoan-hoc-vien-018, BR-f01-p1-tai-khoan-hoc-vien-019, BR-f01-p1-tai-khoan-hoc-vien-020, BR-f01-p1-tai-khoan-hoc-vien-021, BR-f01-p1-tai-khoan-hoc-vien-022, BR-f01-p1-tai-khoan-hoc-vien-023, BR-f01-p1-tai-khoan-hoc-vien-024 |
| US-05 | Là coach, tôi muốn thấy trạng thái tài khoản và lịch sử gửi, kích hoạt của học viên, để biết khi nào cần gửi lại. | BR-f01-p1-tai-khoan-hoc-vien-025, BR-f01-p1-tai-khoan-hoc-vien-026, BR-f01-p1-tai-khoan-hoc-vien-027 |

## 3. Business Rules

### Gửi thông tin đăng nhập
- `BR-f01-p1-tai-khoan-hoc-vien-001` — Thông tin đăng nhập chỉ gửi được cho tài khoản học viên đang Chờ kích hoạt, ở mọi trạng thái coaching. _(Thêm mới)_
  - ✅ Hồ sơ Chờ kích hoạt, coach gửi thông tin đăng nhập → email được gửi tới học viên
  - ✅ Hồ sơ Chờ kích hoạt, coaching Tạm dừng hoặc Đã kết thúc → vẫn gửi được
  - ❌ Tài khoản Đã kích hoạt → từ chối, báo chỉ gửi được link đặt lại mật khẩu
- `BR-f01-p1-tai-khoan-hoc-vien-002` — Lần gửi thông tin đăng nhập đầu tiên của một hồ sơ chỉ được khi hồ sơ đã điền đủ các mục bắt buộc. _(Thêm mới)_
  - ✅ Hồ sơ đủ mục bắt buộc, chưa gửi lần nào → gửi được
  - ❌ Hồ sơ còn 3 mục bắt buộc trống, chưa gửi lần nào → từ chối, báo còn 3 mục bắt buộc chưa điền; hồ sơ vẫn được lưu
  - ✅ Hồ sơ đã gửi lần đầu, sau đó coach xóa trống một mục bắt buộc rồi gửi lại → gửi được
- `BR-f01-p1-tai-khoan-hoc-vien-003` — Mỗi lần gửi tạo một mật khẩu tạm mới, và email gửi học viên gồm địa chỉ đăng nhập web học viên, email tài khoản và mật khẩu tạm; địa chỉ đăng nhập không chứa mật khẩu hay mã bí mật nào. _(Thêm mới)_
  - ✅ Coach gửi cho quan@gmail.com → email có địa chỉ đăng nhập web học viên, "quan@gmail.com", mật khẩu tạm và hạn 72 giờ
  - ❌ Địa chỉ đăng nhập trong email chứa mật khẩu tạm hoặc mã đăng nhập → sai
- `BR-f01-p1-tai-khoan-hoc-vien-004` — Mật khẩu tạm hết hiệu lực khi đủ 72 giờ kể từ lúc gửi. _(Thêm mới)_
  - ✅ Học viên dùng mật khẩu tạm sau 48 giờ → vào được bước đặt mật khẩu riêng
  - ✅ Dùng ở 71 giờ 59 phút → vào được bước đặt mật khẩu riêng
  - ❌ Dùng ở đúng 72 giờ → từ chối
  - ❌ Dùng sau 73 giờ → từ chối
- `BR-f01-p1-tai-khoan-hoc-vien-005` — Mỗi lần gửi lại, mật khẩu tạm trước đó mất hiệu lực, kể cả khi học viên đang ở bước đặt mật khẩu riêng bằng mật khẩu tạm cũ. _(Thêm mới)_
  - ✅ Học viên dùng mật khẩu trong email mới nhất → vào được bước đặt mật khẩu riêng
  - ❌ Học viên dùng mật khẩu trong email trước đó → từ chối
  - ❌ Học viên đã đăng nhập bằng mật khẩu tạm A, coach gửi lại mật khẩu B, học viên lưu mật khẩu riêng → từ chối, phải đăng nhập lại bằng B
  - ✅ Coach gửi hai lần liên tiếp → chỉ mật khẩu của lần gửi sau cùng dùng được
- `BR-f01-p1-tai-khoan-hoc-vien-006` — Khi email thông tin đăng nhập không gửi được, tài khoản và lịch sử không thay đổi gì và coach được báo là chưa gửi được. _(Thêm mới)_
  - ✅ Dịch vụ email lỗi lúc coach gửi lại → coach được báo chưa gửi được; mật khẩu tạm cũ vẫn dùng được; lịch sử không thêm dòng
  - ✅ Lần gửi đầu bị lỗi → hồ sơ vẫn ở tình trạng chưa gửi lần nào
  - ❌ Dịch vụ email lỗi nhưng hồ sơ ghi là đã gửi → sai

### Sửa email
- `BR-f01-p1-tai-khoan-hoc-vien-007` — Email của học viên chỉ sửa được khi tài khoản đang Chờ kích hoạt. _(Thay đổi: rule F02 "Coach sửa được mọi mục của hồ sơ trừ email" trước đây khóa email ở mọi trạng thái → nay chỉ khóa khi Đã kích hoạt)_
  - ✅ Sửa "quan@gmial.com" thành "quan@gmail.com" khi Chờ kích hoạt → email mới được lưu
  - ❌ Sửa email khi tài khoản Đã kích hoạt → từ chối
- `BR-f01-p1-tai-khoan-hoc-vien-008` — Email mới khi sửa phải chưa thuộc tài khoản nào khác, không phân biệt hoa/thường và bỏ khoảng trắng đầu/cuối. _(Thêm mới)_
  - ✅ Sửa sang email chưa ai dùng → lưu được
  - ❌ Sửa sang "QUAN@Gmail.com " khi "quan@gmail.com" đã thuộc hồ sơ khác → từ chối, báo email đã được dùng; hồ sơ giữ email cũ và mật khẩu tạm cũ vẫn dùng được
  - ❌ Sửa sang email của tài khoản coach → từ chối, báo email đã được dùng
  - ❌ Sửa sang "quan@" → từ chối, báo email không hợp lệ
- `BR-f01-p1-tai-khoan-hoc-vien-009` — Sửa email của hồ sơ đã từng được gửi thông tin đăng nhập thì mật khẩu tạm cũ mất hiệu lực và thông tin đăng nhập mới được gửi tới email mới. _(Thêm mới)_
  - ✅ Sửa sai chính tả email → email mới nhận được thông tin đăng nhập mới
  - ❌ Đăng nhập bằng email cũ kèm mật khẩu tạm cũ → từ chối
  - ❌ Đăng nhập bằng email mới kèm mật khẩu tạm cũ → từ chối
  - ✅ Gửi tới email mới bị lỗi → hồ sơ giữ email cũ và mật khẩu tạm cũ, coach được báo chưa gửi được
- `BR-f01-p1-tai-khoan-hoc-vien-010` — Sửa email của hồ sơ chưa từng được gửi thông tin đăng nhập thì chỉ đổi email, không gửi email nào. _(Thêm mới)_
  - ✅ Hồ sơ chưa gửi lần nào, sửa email → email đổi, không có email nào được gửi
  - ✅ Sau đó coach gửi lần đầu → thông tin đăng nhập tới email mới

### Kích hoạt
- `BR-f01-p1-tai-khoan-hoc-vien-011` — Đăng nhập bằng mật khẩu tạm còn hiệu lực chỉ cho phép học viên đặt mật khẩu riêng, chưa xem hay ghi được dữ liệu nào khác. _(Thêm mới)_
  - ✅ Đăng nhập lần đầu bằng mật khẩu tạm → được đưa thẳng tới bước đặt mật khẩu riêng
  - ❌ Chưa đặt mật khẩu riêng mà xem lịch, nhật ký hay hồ sơ → từ chối
  - ❌ Dùng mật khẩu tạm đăng nhập web quản trị → từ chối với thông báo đăng nhập thất bại chung
- `BR-f01-p1-tai-khoan-hoc-vien-012` — Đăng nhập bằng mật khẩu tạm không còn hiệu lực bị từ chối với đúng thông báo chung của mọi lần đăng nhập thất bại, không nói lý do. _(Thêm mới)_
  - ✅ Mật khẩu tạm còn hạn của email mới nhất → đăng nhập được
  - ❌ Mật khẩu tạm quá 72 giờ → thông báo đăng nhập thất bại chung, không nói là hết hạn
  - ❌ Mật khẩu tạm của email trước đó → thông báo đăng nhập thất bại chung
  - ❌ Mật khẩu tạm sau khi tài khoản đã kích hoạt → thông báo đăng nhập thất bại chung
- `BR-f01-p1-tai-khoan-hoc-vien-013` — Mật khẩu riêng, dù đặt lúc kích hoạt hay đặt lại qua link, dài từ 8 đến 128 ký tự. _(Thêm mới)_
  - ✅ "Coach2026" → lưu được
  - ✅ "abcdefgh" (đúng 8 ký tự) → lưu được
  - ❌ "abc123" → từ chối, báo cần ít nhất 8 ký tự
  - ❌ "abcdefg" (7 ký tự) → từ chối
  - ❌ Mật khẩu 129 ký tự → từ chối
- `BR-f01-p1-tai-khoan-hoc-vien-014` — Mật khẩu riêng, dù đặt lúc kích hoạt hay đặt lại qua link, phải được nhập giống nhau ở hai lần. _(Thêm mới)_
  - ✅ Nhập "Coach2026" hai lần → lưu được
  - ❌ "Coach2026" và "Coach2027" → từ chối, tài khoản giữ nguyên trạng thái
- `BR-f01-p1-tai-khoan-hoc-vien-015` — Mật khẩu riêng được giữ đúng từng ký tự như học viên nhập, kể cả chữ có dấu và khoảng trắng đầu/cuối. _(Thêm mới)_
  - ✅ "Mậtkhẩu1" → lưu được, đăng nhập lại bằng đúng "Mậtkhẩu1" được
  - ✅ " Coach2026 " → đăng nhập bằng " Coach2026 " được
  - ❌ Đã đặt " Coach2026 ", đăng nhập bằng "Coach2026" → từ chối
- `BR-f01-p1-tai-khoan-hoc-vien-016` — Đặt mật khẩu riêng bằng mật khẩu tạm còn hiệu lực thì tài khoản chuyển sang Đã kích hoạt kèm thời điểm kích hoạt, mật khẩu tạm mất hiệu lực ngay và học viên dùng được dữ liệu của mình. _(Thêm mới)_
  - ✅ Lưu mật khẩu riêng lúc 13/10/2026 → học viên vào được dữ liệu của mình; coach thấy kích hoạt ngày 13/10/2026
  - ✅ Đăng nhập lần sau bằng email và mật khẩu riêng → vào được
  - ❌ Đăng nhập lại bằng mật khẩu tạm sau khi đã kích hoạt → từ chối

### Đặt lại mật khẩu
- `BR-f01-p1-tai-khoan-hoc-vien-017` — Coach chỉ gửi được link đặt lại mật khẩu cho tài khoản Đã kích hoạt, ở mọi trạng thái coaching. _(Thêm mới)_
  - ✅ Học viên Đã kích hoạt, coaching Tạm dừng → học viên nhận email có link đặt lại
  - ✅ Học viên Đã kích hoạt, coaching Đã kết thúc → gửi được
  - ❌ Hồ sơ Chờ kích hoạt → từ chối, báo dùng gửi thông tin đăng nhập
- `BR-f01-p1-tai-khoan-hoc-vien-018` — Link đặt lại mật khẩu hết hiệu lực khi đủ 30 phút kể từ lúc tạo. _(Thêm mới)_
  - ✅ Dùng link sau 20 phút, mật khẩu mới hợp lệ → đặt lại được
  - ❌ Dùng link ở đúng 30 phút → từ chối
  - ❌ Dùng link sau 31 phút → từ chối
- `BR-f01-p1-tai-khoan-hoc-vien-019` — Link đặt lại mật khẩu chỉ đặt lại thành công được một lần. _(Thêm mới)_
  - ✅ Dùng link lần đầu → đặt lại được
  - ❌ Dùng lại link đã đặt lại thành công → từ chối
  - ✅ Lần đầu nhập "abc123" bị từ chối, link còn hạn → dùng tiếp link đó đặt "Coach2026" được
- `BR-f01-p1-tai-khoan-hoc-vien-020` — Khi tài khoản có link đặt lại mới, mọi link đặt lại trước đó của tài khoản mất hiệu lực. _(Thêm mới)_
  - ✅ Xin link lần hai, dùng link mới → đặt lại được
  - ❌ Dùng link lần một sau khi đã có link lần hai → từ chối
- `BR-f01-p1-tai-khoan-hoc-vien-021` — Đặt lại mật khẩu xong, mọi phiên đăng nhập đang có của học viên kết thúc và chỉ mật khẩu mới đăng nhập được. _(Thêm mới)_
  - ✅ Học viên đặt lại mật khẩu trên máy tính, điện thoại đang đăng nhập → điện thoại phải đăng nhập lại
  - ✅ Đăng nhập bằng mật khẩu mới → vào được
  - ❌ Đăng nhập bằng mật khẩu cũ → từ chối
- `BR-f01-p1-tai-khoan-hoc-vien-022` — Quên mật khẩu chỉ gửi link đặt lại cho tài khoản học viên Đã kích hoạt. _(Thêm mới)_
  - ✅ Email Đã kích hoạt, gõ "QUAN@gmail.com " khác hoa/thường và có khoảng trắng → nhận được link đặt lại
  - ❌ Email chưa có tài khoản → không gửi gì
  - ❌ Email còn Chờ kích hoạt → không gửi gì
  - ❌ Email của coach → không gửi gì
- `BR-f01-p1-tai-khoan-hoc-vien-023` — Kết quả trả về của Quên mật khẩu giống hệt nhau trong mọi trường hợp. _(Thêm mới)_
  - ✅ Email Đã kích hoạt, email lạ, email Chờ kích hoạt, email coach → cùng một thông báo, không phân biệt được trường hợp nào
  - ❌ Thông báo hoặc kết quả khác nhau giữa email có và không có tài khoản → sai
- `BR-f01-p1-tai-khoan-hoc-vien-024` — Mỗi tài khoản nhận tối đa một email đặt lại từ Quên mật khẩu trong mỗi 60 giây. _(Thêm mới)_
  - ✅ Xin link, 2 phút sau xin lại → nhận email thứ hai
  - ❌ Xin 5 lần trong 30 giây → chỉ nhận 1 email; mỗi lần vẫn nhận cùng thông báo chung

### Coach theo dõi và bảo mật
- `BR-f01-p1-tai-khoan-hoc-vien-025` — Không dữ liệu nào hệ thống trả ra, cho coach hay học viên, chứa mật khẩu tạm, mật khẩu riêng hoặc mã đặt lại mật khẩu. _(Thêm mới)_
  - ✅ Coach xem hồ sơ → thấy trạng thái tài khoản, không thấy mật khẩu
  - ❌ Dữ liệu hồ sơ hoặc kết quả gửi thông tin trả về có mật khẩu tạm → sai
- `BR-f01-p1-tai-khoan-hoc-vien-026` — Coach thấy được trạng thái tài khoản của học viên gồm: chưa gửi, đã gửi kèm thời điểm gửi gần nhất và hạn mật khẩu tạm, mật khẩu tạm đã hết hạn, hoặc đã kích hoạt kèm thời điểm kích hoạt. _(Thêm mới)_
  - ✅ Chưa gửi lần nào → coach thấy chưa gửi
  - ✅ Gửi lúc 01/11 09:00 → coach thấy đã gửi lúc 01/11 09:00, mật khẩu tạm còn hạn đến 04/11 09:00
  - ✅ Quá 72 giờ chưa kích hoạt → coach thấy mật khẩu tạm đã hết hạn
  - ✅ Kích hoạt ngày 13/10/2026 → coach thấy kích hoạt ngày 13/10/2026
- `BR-f01-p1-tai-khoan-hoc-vien-027` — Mỗi lần coach gửi thông tin lần đầu, gửi lại, sửa email, gửi link đặt lại và mỗi lần học viên kích hoạt được ghi thêm đúng một dòng lịch sử tài khoản kèm thời điểm; lịch sử chỉ thêm, không sửa, không xóa. _(Thêm mới)_
  - ✅ Gửi lần đầu, gửi lại, học viên kích hoạt → lịch sử 3 dòng theo thứ tự thời gian: gửi lần đầu, gửi lại kèm ghi chú mật khẩu tạm cũ bị hủy, học viên đặt mật khẩu riêng
  - ✅ Sửa email của hồ sơ đã gửi → thêm dòng sửa email từ email cũ sang email mới
  - ✅ Học viên tự xin link Quên mật khẩu → lịch sử không thêm dòng
  - ❌ Gửi bị lỗi → lịch sử không thêm dòng

## 4. Design Requirements / NFR + Compliance/Security
- `NFR-f01-p1-tai-khoan-hoc-vien-001` (Security) — Mật khẩu tạm và mã đặt lại mật khẩu chỉ được lưu dạng băm; không ghi dạng rõ vào log ở môi trường production (môi trường thử chỉ-ghi-log được phép để kiểm tra).
- `NFR-f01-p1-tai-khoan-hoc-vien-002` (Security) — Quên mật khẩu không để lộ email nào có tài khoản qua nội dung trả về, mã trạng thái hay thời gian phản hồi chênh lệch rõ.
- `NFR-f01-p1-tai-khoan-hoc-vien-003` (Configuration) — Địa chỉ trong email lấy từ cấu hình môi trường (địa chỉ public của web học viên); thiếu cấu hình thì không gửi email và báo lỗi rõ, không gửi địa chỉ hỏng.
- `NFR-f01-p1-tai-khoan-hoc-vien-004` (Performance) — Các thao tác gửi thông tin, sửa email, kích hoạt, đặt lại mật khẩu có p95 < 500 ms, không tính thời gian chờ dịch vụ email.
- `NFR-f01-p1-tai-khoan-hoc-vien-005` (Content) — Tiêu đề và nội dung email mời và email đặt lại do dev soạn theo giọng văn prototype, coach duyệt trước khi bật gửi thật trên staging; tiếng Việt, ghi rõ hạn 72 giờ hoặc 30 phút.

## 5. Out of Scope
- Sửa email sau khi đã kích hoạt; đổi mật khẩu khi đang đăng nhập; khóa hoặc hủy tài khoản thủ công.
- Hàng đợi hoặc tự gửi lại email khi lỗi; theo dõi email bị trả về; thông báo cho coach khi học viên kích hoạt; nhắc tự động khi mật khẩu tạm sắp hết hạn.
- Học viên Đã kết thúc chỉ xem (rule cuối của PRD F01) — P4. Chặn chéo dữ liệu giữa học viên cho dữ liệu P4 — P4.
- Email duy nhất khi tạo hồ sơ, hồ sơ mới Chờ kích hoạt (hai rule đầu của PRD F01) — đã làm ở F02. Đăng nhập admin, phân quyền vai trò, khóa 5 phút sau 5 lần sai — đã có ở F00 và áp dụng nguyên cho đăng nhập bằng mật khẩu tạm.
- Đăng nhập Google/Facebook, 2FA, nhiều coach.
- Phần giao diện (gợi ý "liên hệ coach" cố định trên màn đăng nhập, màn đặt mật khẩu mới từ link mà prototype chưa vẽ, nút "Lưu và gửi thông tin đăng nhập", khối "Tài khoản đăng nhập") → to-tech-design.

## 6. Open Questions
| # | Question | Type | Blocking? | Owner | Resolution |
|---|----------|------|-----------|-------|------------|
| OQ-01 | Mật khẩu tạm hết hạn có báo riêng cho học viên không (PRD muốn hướng dẫn liên hệ coach, F00 cấm lộ lý do)? | Business | ✅ | PO | Thông báo đăng nhập thất bại chung; luôn có gợi ý cố định liên hệ coach; coach thấy hết hạn trên hồ sơ (BR-f01-p1-tai-khoan-hoc-vien-012, BR-f01-p1-tai-khoan-hoc-vien-026). |
| OQ-02 | Kiểm mục bắt buộc ở những lần gửi nào? | Business | ✅ | PO | Chỉ lần gửi đầu (BR-f01-p1-tai-khoan-hoc-vien-002). |
| OQ-03 | Email gửi lỗi thì sao? | Business | ✅ | PO | Hoàn tác, báo coach, không hàng đợi (BR-f01-p1-tai-khoan-hoc-vien-006). |
| OQ-04 | Có lịch sử tài khoản không? | Business | ✅ | PO | Có, danh sách sự kiện (BR-f01-p1-tai-khoan-hoc-vien-027). |
| OQ-05 | Gửi lại khi đang kích hoạt, nhiều link đặt lại, phiên sau khi đặt lại? | Business | ✅ | PO | Hủy cái cũ, đăng xuất mọi thiết bị (BR-f01-p1-tai-khoan-hoc-vien-005, BR-f01-p1-tai-khoan-hoc-vien-020, BR-f01-p1-tai-khoan-hoc-vien-021). |
| OQ-06 | Quy tắc mật khẩu ngoài 8 ký tự? | Business | ✅ | PO | Giữ nguyên ký tự, tối đa 128, không chặn thêm (BR-f01-p1-tai-khoan-hoc-vien-013, BR-f01-p1-tai-khoan-hoc-vien-015). |
| OQ-07 | Chống spam Quên mật khẩu? | Business | ✅ | PO | 1 email mỗi 60 giây mỗi tài khoản (BR-f01-p1-tai-khoan-hoc-vien-024). |
| OQ-08 | Sửa email hồ sơ chưa từng gửi? | Business | ✅ | PO | Chỉ đổi email (BR-f01-p1-tai-khoan-hoc-vien-010). |
| OQ-09 | Nội dung email mời và email đặt lại | Business | ❌ | PO / coach | Dev soạn nháp theo giọng prototype, coach duyệt (NFR-f01-p1-tai-khoan-hoc-vien-005). |

## 7. Dependencies
| Dependency | Type | Status |
|------------|------|--------|
| Resend đã verify domain gửi; khóa Resend + địa chỉ gửi trên staging (Render) | service | pending — PO cấu hình |
| Địa chỉ public của web học viên trên từng môi trường | infra | available (staging: https://coaching-client.onrender.com) — cần đưa vào cấu hình API |
| F02 hồ sơ học viên (tài khoản Chờ kích hoạt, danh sách mục bắt buộc còn thiếu) | feature | available (completed) |
| F00 đăng nhập, phiên, khóa tài khoản | feature | available (completed) |
