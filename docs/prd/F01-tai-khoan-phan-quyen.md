# F01 — Tài khoản & phân quyền

> Thuộc [PRD tổng](../PRD-coaching-web.md). Phase: **P1** (phía admin: gửi mời, gửi lại, theo dõi kích hoạt) · **P4** (phía học viên: kích hoạt, đăng nhập, quên mật khẩu).

## 1. Metadata
- **Feature Name:** Mời, kích hoạt, đăng nhập, đặt lại mật khẩu và phân quyền
- **Status:** Ready for Dev
- **Product Owner:** Hoangduc1203BK — duc.hm4@kiotviet.com

## 2. Context & Problem Statement
- Tài khoản học viên chỉ được tạo theo lời mời của coach. Học viên cần vào được hệ thống bằng một quy trình đơn giản: nhận email → mật khẩu tạm → đặt mật khẩu riêng.
- Coach cần thấy trạng thái gửi và kích hoạt, nhưng không được biết mật khẩu riêng của học viên.
- Dữ liệu của học viên này tuyệt đối không được lộ sang học viên khác.

## 3. UX/UI
- Prototype: page `Main` (đăng nhập, kích hoạt, quên mật khẩu); `AdTaoHoSo` và `AdHoSo` (khối "Trạng thái tài khoản", "Tài khoản đăng nhập").

## 4. Scope Configuration
- **In-Scope:**
  - Đăng nhập admin (P1).
  - Gửi thông tin đăng nhập, gửi lại, sửa email trước khi kích hoạt (P1).
  - Coach gửi link đặt lại mật khẩu (P1).
  - Học viên kích hoạt bằng mật khẩu tạm, đăng nhập, quên mật khẩu (P4).
  - Phân quyền theo role, chặn truy cập chéo giữa học viên.
- **Out-of-Scope:** đăng nhập bằng Google/Facebook; 2FA; tài khoản cho nhiều coach.

## 5. Functional Requirements

**BR-01 — Email học viên là duy nhất trong toàn hệ thống.**
- ✅ Tạo hồ sơ với email chưa tồn tại → hồ sơ được tạo
- ❌ Tạo hồ sơ với email đã thuộc hồ sơ khác → từ chối, báo email đã được dùng

**BR-02 — Hồ sơ mới tạo có trạng thái tài khoản "Chờ kích hoạt" và được gán phase ngay mà không cần chờ kích hoạt.**
- ✅ Tạo hồ sơ rồi soạn phase nháp trước khi học viên kích hoạt → được
- ❌ Hồ sơ mới tạo hiện trạng thái "Đã kích hoạt" → sai

**BR-03 — Gửi thông tin đăng nhập: hệ thống tạo mật khẩu tạm mới và gửi email gồm link trang đăng nhập, email tài khoản và mật khẩu tạm. Link chỉ chứa địa chỉ trang, không kèm mật khẩu hay token.**
- ✅ Coach gửi thông tin cho hồ sơ Chờ kích hoạt → học viên nhận email đủ 3 thông tin
- ❌ Coach gửi thông tin đăng nhập cho hồ sơ đã kích hoạt → từ chối, chỉ cho gửi link đặt lại mật khẩu

**BR-04 — Mật khẩu tạm hết hiệu lực sau 72 giờ kể từ lúc gửi.**
- ✅ Học viên kích hoạt sau 48 giờ → kích hoạt thành công
- ❌ Học viên dùng mật khẩu tạm sau 73 giờ → từ chối, hướng dẫn liên hệ coach để gửi lại

**BR-05 — Mỗi lần gửi lại thông tin đăng nhập, mật khẩu tạm cũ mất hiệu lực.**
- ✅ Học viên dùng mật khẩu trong email mới nhất → đăng nhập được
- ❌ Học viên dùng mật khẩu trong email trước đó → từ chối

**BR-06 — Khi sửa email của hồ sơ Chờ kích hoạt, mật khẩu tạm cũ mất hiệu lực và hệ thống gửi thông tin mới tới email mới.**
- ✅ Sửa sai chính tả email → email mới nhận được thông tin đăng nhập
- ❌ Đăng nhập bằng email cũ kèm mật khẩu tạm cũ → từ chối

**BR-07 — Mật khẩu riêng có ít nhất 8 ký tự và phải nhập giống nhau ở 2 lần.**
- ✅ Nhập "Coach2026" hai lần → lưu được
- ❌ Nhập "abc123" → từ chối, báo cần ít nhất 8 ký tự
- ❌ Hai lần nhập khác nhau → từ chối

**BR-08 — Đặt mật khẩu riêng xong thì tài khoản chuyển sang "Đã kích hoạt" và mật khẩu tạm mất hiệu lực ngay.**
- ✅ Lưu mật khẩu riêng → vào trang Hôm nay, coach thấy thời điểm kích hoạt
- ❌ Đăng nhập lại bằng mật khẩu tạm sau khi đã kích hoạt → từ chối

**BR-09 — Học viên đăng nhập bằng mật khẩu tạm chỉ được vào bước đặt mật khẩu riêng, chưa xem được dữ liệu nào khác.**
- ✅ Đăng nhập lần đầu → được đưa thẳng tới bước đặt mật khẩu
- ❌ Chưa đặt mật khẩu riêng mà mở trang Lịch → từ chối

**BR-10 — Link đặt lại mật khẩu có hiệu lực 30 phút và chỉ dùng được một lần.**
- ✅ Mở link sau 20 phút → đặt lại được
- ❌ Mở link sau 31 phút → từ chối
- ❌ Mở lại link đã dùng → từ chối

**BR-11 — Chức năng Quên mật khẩu chỉ gửi link cho tài khoản Đã kích hoạt. Thông báo hiển thị giống nhau trong mọi trường hợp để không lộ email nào có tài khoản.**
- ✅ Email đã kích hoạt → nhận được link đặt lại
- ❌ Email chưa có tài khoản hoặc còn Chờ kích hoạt → không gửi link, màn hình vẫn hiện thông báo chung

**BR-12 — Coach không xem và không đặt được mật khẩu riêng của học viên, chỉ gửi được link đặt lại mật khẩu.**
- ✅ Coach bấm "Gửi link đặt lại mật khẩu" → học viên nhận email
- ❌ Coach tìm cách xem mật khẩu học viên → hệ thống không cung cấp

**BR-13 — Học viên chỉ truy cập được dữ liệu của chính mình.**
- ✅ Học viên A xem nhật ký của A → được
- ❌ Học viên A mở đường dẫn hồ sơ, review hoặc đính kèm của học viên B → từ chối

**BR-84 — Học viên có trạng thái coaching "Đã kết thúc" vẫn đăng nhập được nhưng chỉ xem, không ghi dữ liệu mới.**
- ✅ Đăng nhập, xem lịch sử phase, tiến độ, phản hồi → được
- ❌ Ghi nhật ký, check-in hoặc nộp review → từ chối

**BR-15 — API admin chỉ phục vụ tài khoản role Admin.**
- ✅ Admin gọi API phát hành phase → được
- ❌ Học viên đã đăng nhập gọi API admin → từ chối

## 6. Non-functional Requirements & Constraints
- Mật khẩu tạm và token đặt lại mật khẩu chỉ lưu dưới dạng hash.
- Thông báo của Quên mật khẩu không được tiết lộ email nào tồn tại (BR-11).
- Kiểm tra BR-13 ở tầng API/query (lọc theo `StudentId` lấy từ token), có integration test.
- Nên giới hạn số lần thử ở các endpoint đăng nhập và quên mật khẩu (rate limit).

## 7. Dependencies
- Dịch vụ email **Resend** đã verify domain gửi.

## 8. Open Questions

| # | Question | Blocking | Answer | PIC |
|---|---|---|---|---|
| 1 | Học viên có trạng thái coaching "Đã kết thúc" có còn đăng nhập xem lịch sử không? | No (P4) | **Đã chốt:** được, chỉ xem (BR-84) | Product |
| 2 | Nội dung và tiêu đề email mời / email đặt lại mật khẩu | No | Dev soạn bản nháp theo giọng văn trong prototype, coach duyệt | Product |
