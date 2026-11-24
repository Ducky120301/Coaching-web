# PRD tổng — Duobexiuuu Coaching: Web Admin (Coach) & Web Học viên

> File tổng quan. Business rules chi tiết nằm trong các **PRD theo tính năng** ở `docs/prd/` (bảng ở mục 4).
> Mã BR-xx được giữ cố định trên toàn bộ tài liệu để truy vết.

## 1. Metadata
- **Feature Name:** Hệ thống quản lý coaching online: Web Admin + Web Học viên
- **Status:** Ready for Dev *(không có Open Question Blocking ở cấp tổng; riêng F07 Import Excel đang là Draft)*
- **Product Owner:** Hoangduc1203BK — duc.hm4@kiotviet.com
- **Ngày chốt:** 24/11/2026

## 2. Context & Problem Statement
- **Vấn đề:** coach giao giáo án bằng template/Excel. Học viên ghi nhật ký lưu cục bộ trên trình duyệt, sao lưu thủ công và gửi báo cáo qua Zalo/chat. Hệ quả:
  - Học viên không chắc mình đang xem phiên bản giáo án nào.
  - Dữ liệu dễ mất khi đổi thiết bị.
  - Coach không có chỗ tập trung để biết ai đang chờ phản hồi, ai thiếu dữ liệu, ai sắp hết phase.
  - Lịch sử nhiều phase bị rải rác nên khó so sánh xu hướng.
- **Giải pháp:** hai website dùng chung một hồ sơ coaching.
  - **Web Admin:** coach tạo hồ sơ, soạn và phát hành phase, xử lý review, chuyển phase.
  - **Web Học viên** (responsive, ưu tiên điện thoại): xem việc hôm nay, ghi set, check-in, nộp review tuần, xem tiến độ.
  - Mọi quyết định ảnh hưởng tới giáo án, tiến độ hoặc review đều được ghi trên web. Zalo và mạng xã hội chỉ là kênh trao đổi ngoài hệ thống.
- **Giá trị kinh doanh:**
  - Giao giáo án nhất quán: nhân bản phase cũ, cá nhân hóa từng học viên.
  - Một coach quản lý được nhiều học viên (mục tiêu khoảng 1000 user).
  - Có số liệu vận hành: thời gian từ lúc tạo hồ sơ tới khi phát hành giáo án, tỷ lệ nộp review đúng hạn, số review đang chờ, tỷ lệ học viên đã có phase kế tiếp trước khi phase hiện tại kết thúc.
- **Kiến trúc đã chốt:**
  - **Một backend chung** (.NET 10, Clean Architecture + DDD, SQL Server). Domain, Application và Infrastructure dùng chung. Một API host chia hai nhóm route `/api/admin/*` và `/api/client/*`, phân quyền theo role.
  - Lý do: hai web dùng gần như toàn bộ domain chung, chỉ một coach, quy mô nhỏ. Sau này vẫn tách được thành 2 host mà không phải sửa Domain.
  - Frontend là một Angular workspace gồm 2 app (`admin`, `client`) và 1 thư viện `shared`.
  - Quy ước chi tiết ở `CLAUDE.md`.

## 3. UX/UI
- **Tài liệu design:** `docs/duobexiuuu-coaching-design.html` (prototype gồm 18 màn: Đăng nhập, 8 màn học viên HV, 9 màn admin AD). FE phải bám chính xác prototype (xem `CLAUDE.md`).
- **Tài liệu nghiệp vụ gốc:** `docs/Ke-hoach-nghiep-vu-website-coaching.docx` (bản đề xuất ngày 20.11.2026).

## 4. Scope Configuration

### Lộ trình: làm hết Admin trước, rồi tới Học viên

| Phase | Nội dung | Điều kiện hoàn thành |
|---|---|---|
| **P0** | Dựng codebase BE + FE + DevOps (GitHub Actions, deploy test lên Render) | Admin seed đăng nhập được trên môi trường test, gọi được API được bảo vệ. Học viên giả gọi API admin thì bị từ chối. |
| **P1** | Admin: đăng nhập, hồ sơ học viên + gửi mời, thư viện bài tập, thư viện thực phẩm | Học viên nhận được email mời. Coach thấy trạng thái gửi và kích hoạt. |
| **P2** | Admin: soạn giáo án, phát hành và phiên bản; import Excel ở cuối phase | Phase 4 tuần bắt đầu thứ Ba hiển thị đúng dải ngày. Nhân bản cho 2 học viên rồi sửa một bản thì bản kia không đổi. |
| **P3** | Admin: dashboard, hồ sơ tiến độ, hàng chờ review, chuyển phase (kiểm thử bằng seed data) | Với seed data, coach xử lý trọn một review và chuyển được sang phase mới. |
| **P4** | Client: kích hoạt/đăng nhập, Hôm nay, Lịch, Dinh dưỡng, Thuật ngữ | Học viên đặt mật khẩu riêng và mở đúng giáo án trên điện thoại. |
| **P5** | Client: nhật ký set, check-in, review tuần, tiến độ + chạy thử | Toàn bộ 7 tình huống nghiệm thu ở mục 05 của tài liệu nghiệp vụ đều đạt trên hai website. |

### PRD theo tính năng

| Mã | Tính năng | Phase | BR | Status |
|---|---|---|---|---|
| [F00](prd/F00-codebase.md) | Dựng codebase | P0 | — | Ready for Dev |
| [F01](prd/F01-tai-khoan-phan-quyen.md) | Tài khoản & phân quyền | P1 (admin), P4 (client) | BR-01→13, 15, 84 | Ready for Dev |
| [F02](prd/F02-ho-so-hoc-vien.md) | Hồ sơ học viên & trạng thái coaching | P1 | BR-14, BR-16 | Ready for Dev |
| [F03](prd/F03-thu-vien-bai-tap.md) | Thư viện bài tập | P1 | BR-20→22 | Ready for Dev |
| [F04](prd/F04-thu-vien-thuc-pham-thuc-don.md) | Thư viện thực phẩm & thực đơn | P1, P2 | BR-23→27, 86 | Ready for Dev |
| [F05](prd/F05-soan-giao-an.md) | Soạn giáo án (nhân bản phase cũ, không có thư viện template) | P2 | BR-30→35 | Ready for Dev |
| [F06](prd/F06-phat-hanh-phien-ban.md) | Phát hành phase & phiên bản giáo án | P2 | BR-36, 37, 39→43 | Ready for Dev |
| [F07](prd/F07-import-excel.md) | Import Excel | Cuối P2 | BR-28 | **Draft** (chờ template) |
| [F08](prd/F08-dashboard-tien-do.md) | Dashboard, hồ sơ tiến độ & chỉ số | P3 (admin), P5 (client) | BR-59→65, 80 | Ready for Dev |
| [F09](prd/F09-review-tuan.md) | Review tuần | P3 (admin), P5 (client) | BR-50→58, 78, 85 | Ready for Dev |
| [F10](prd/F10-chuyen-phase.md) | Tổng kết & chuyển phase | P3 | BR-66→68, 83 | Ready for Dev |
| [F11](prd/F11-hom-nay-lich-dinh-duong.md) | Hôm nay, Lịch, Dinh dưỡng, Thuật ngữ | P4 | BR-38, 70, 71, 82 | Ready for Dev |
| [F12](prd/F12-nhat-ky-checkin.md) | Nhật ký tập & Check-in | P5 | BR-72→77, 79, 81 | Ready for Dev |

### Out-of-Scope (KHÔNG LÀM ở phiên bản này, áp dụng cho mọi feature)
- Upload ảnh/video trực tiếp (MVP dùng link Google Drive).
- Nhiều coach, màn quản lý coach, phân quyền theo danh sách học viên được phân công (chỉ chuẩn bị sẵn `CoachId` trong DB).
- Chat nội bộ, đồng bộ Zalo/mạng xã hội, thông báo đẩy.
- Thanh toán, thiết bị đeo.
- Xuất báo cáo.
- Chế độ offline / chờ đồng bộ khi mất mạng (chỉ làm responsive).
- Import nhật ký cũ từ trình duyệt.

## 5. Functional Requirements
Xem từng PRD tính năng ở bảng trên. Mỗi business rule có ví dụ ✅/❌.

## 6. Non-functional Requirements & Constraints (chung)
- **Hiệu năng:** API p95 < 500ms ở quy mô khoảng 1000 học viên, trên 1 VPS (cấu hình tham khảo 4 vCPU/8GB, ưu tiên đặt ở Việt Nam hoặc Singapore).
- **Giao diện:** web học viên responsive, ưu tiên điện thoại (khung 390px theo prototype). Web admin tối ưu cho desktop. Breakpoint duy nhất là 880px.
- **Bảo mật:**
  - Mật khẩu hash bằng ASP.NET Core Identity.
  - JWT access token ngắn hạn kèm refresh token.
  - Mật khẩu tạm và link đặt lại mật khẩu chỉ lưu dưới dạng hash.
  - Chặn truy cập chéo giữa học viên ngay ở tầng API, không chỉ ẩn trên giao diện.
- **Thời gian:** mọi tính toán ngày/tuần theo giờ Việt Nam (UTC+7) thông qua `IClock`; DB lưu thời điểm ở UTC.
- **Dữ liệu:** SQL Server 2022 bản Express (giới hạn 10GB, đủ dùng vì ảnh/video là link Drive). Production cần backup DB hằng ngày, giữ tối thiểu 7 bản (cách làm cụ thể tùy hosting).
- **Khả năng mở rộng:** có `CoachId` trên học viên; tách được 2 API host mà không sửa Domain; đổi nhà cung cấp email qua `IEmailSender`.
- **Email:** gửi từ domain riêng có SPF/DKIM để tránh vào Spam.

## 7. Dependencies (chung)
- **Email giao dịch:** **Resend** (gói miễn phí), dự phòng Amazon SES.
- **CI/CD & môi trường test:** GitHub Actions; deploy test lên Render; SQL Server test do PO cung cấp.
- **Hosting production:** PO đăng ký và báo sau. Đã khảo sát: Contabo Cloud VPS 4, Bizfly VPS, Hostinger VPS KVM 2, Vinahost Windows Hosting (giới hạn DB 1GB). Gói web hosting của iNET và Hostinger không chạy được .NET.
- **Domain + DNS:** cho subdomain api / admin / app và cho SPF/DKIM của email.
- **Google Drive:** học viên tự cấp quyền xem cho coach. Hệ thống chỉ lưu link, không gọi API của Drive.
- **YouTube:** video mẫu là link nhúng hoặc link mở, không gọi API.

## 8. Open Questions (chung — câu hỏi riêng từng tính năng nằm trong PRD tính năng)

| # | Question | Blocking | Answer | PIC |
|---|---|---|---|---|
| 1 | Hosting production | No (cần trước go-live) | PO đăng ký và báo sau | Product |
| 2 | Domain chính và domain gửi email (cần SPF/DKIM trước khi gửi email thật ở P1) | No | — | Product |
| 3 | Chọn Resend hay Brevo | No | **Đã chốt:** Resend | Dev |
| 4 | SQL Server cho môi trường test trên Render (xem F00 OQ #2) | No | PO cung cấp | Product |
