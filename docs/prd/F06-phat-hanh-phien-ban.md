# F06 — Phát hành phase & phiên bản giáo án

> Thuộc [PRD tổng](../PRD-coaching-web.md). Phase: **P2**. Màn AD05.

## 1. Metadata
- **Feature Name:** Xem trước, phát hành ngay hoặc theo lịch, vòng đời phase, phiên bản giáo án có ngày hiệu lực, audit
- **Status:** Ready for Dev
- **Product Owner:** Hoangduc1203BK — duc.hm4@kiotviet.com

## 2. Context & Problem Statement
- Học viên phải luôn thấy đúng phiên bản giáo án đang có hiệu lực.
- Khi coach chỉnh giáo án giữa phase, dữ liệu đã tập vẫn phải giữ chỉ tiêu lúc tập, và phải có vết ai sửa, khi nào, vì sao.

## 3. UX/UI
- Prototype: `AdPhatHanh` gồm:
  - Xem trước như học viên trên điện thoại.
  - "Kiểm tra trước khi phát hành" (lịch đủ buổi, dinh dưỡng, chỉ tiêu check-in, cảnh báo thiếu video).
  - "Khi nào phát hành".
  - Danh sách phiên bản giáo án (Bản 1, Bản 2, ngày hiệu lực, lý do, người sửa).

## 4. Scope Configuration
- **In-Scope:**
  - Xem trước, phát hành ngay, đặt ngày bắt đầu.
  - Tự chuyển trạng thái theo ngày.
  - Tạo phiên bản mới cho phase đang chạy.
  - Lịch sử phiên bản, audit.
- **Out-of-Scope:** gửi email báo học viên khi có phiên bản mới (MVP: học viên thấy mốc thay đổi trên web).

## 5. Functional Requirements

**BR-36 — Phát hành với ngày bắt đầu từ hôm nay trở về trước thì phase chuyển ngay sang "Đang chạy".**
- ✅ Phát hành hôm nay, bắt đầu hôm nay → Đang chạy, học viên thấy giáo án ngay
- ✅ Nhập học viên cũ từ Excel, ngày bắt đầu 10/11 (quá khứ) → Đang chạy, đang ở tuần hiện tại

**BR-37 — Phát hành với ngày bắt đầu trong tương lai thì phase là "Đã lên lịch". Phase tự chuyển sang "Đang chạy" lúc 00:00 (giờ VN) của ngày bắt đầu nếu lúc đó chương trình không còn phase nào Đang chạy hoặc Tạm dừng.**
- ✅ Phát hành ngày 25/10 với ngày bắt đầu 29/10, phase trước đã Kết thúc → 00:00 ngày 29/10 chuyển Đang chạy
- ✅ Tới 08/12 mà phase 2 vẫn Đang chạy → phase 3 giữ trạng thái Đã lên lịch (chờ)
- ❌ Phase chuyển Đang chạy trước ngày bắt đầu → sai

**BR-39 — Phase Đang chạy chỉ chuyển sang "Kết thúc" khi coach bấm Kết thúc; qua ngày cuối của tuần cuối phase vẫn Đang chạy.**
- ✅ Phase hết 07/12, coach bấm Kết thúc ngày 09/12 → phase Kết thúc ngày 09/12
- ❌ Hệ thống tự kết thúc phase ngày 08/12 → sai

**BR-43 — Khi coach kết thúc một phase mà phase kế tiếp đã quá ngày bắt đầu hẹn trước, coach chọn ngày bắt đầu cho phase kế tiếp: mặc định là hôm nay, hoặc giữ ngày đã hẹn. Phase kế tiếp chuyển ngay sang Đang chạy.**
- ✅ Phase 3 hẹn 08/12, coach kết thúc phase 2 ngày 10/12, giữ mặc định → phase 3 bắt đầu 10/12, tuần 1 là 10–16/12
- ✅ Coach chọn giữ 08/12 → tuần 1 là 08–14/12
- ❌ Phase 3 tự lấy ngày 08/12 mà không hỏi coach → sai

**BR-40 — Muốn chỉnh phase đang chạy thì phải tạo phiên bản mới, bắt buộc có ngày hiệu lực không sớm hơn hôm nay và có lý do. Hệ thống tự ghi người sửa và thời điểm.**
- ✅ Bản 2 hiệu lực 24/11, lý do "vai trái mỏi" → lưu, học viên thấy mốc thay đổi
- ❌ Ngày hiệu lực 20/11 trong khi hôm nay là 23/11 → từ chối
- ❌ Không ghi lý do → từ chối

**BR-41 — Buổi đã ghi nhật ký giữ nguyên chỉ tiêu của phiên bản đang hiệu lực vào ngày tập.**
- ✅ Bản 2 đổi từ 3 set lên 4 set từ 24/11 → buổi đã ghi ngày 20/11 vẫn hiện 3 set
- ❌ Buổi ngày 20/11 hiện 4 set → sai

**BR-42 — Các thao tác phát hành, đổi trạng thái phase, sửa chỉ tiêu và gửi phản hồi đều được ghi người thực hiện và thời điểm.**
- ✅ Coach phát hành phase 3 → lịch sử có dòng "Đã lên lịch, coach phát hành, 27/10 15:20"

*Điều kiện để được phát hành (BR-34, BR-35) nằm ở [F05](F05-soan-giao-an.md). Phần học viên thấy gì theo trạng thái phase (BR-38) nằm ở [F11](F11-hom-nay-lich-dinh-duong.md).*

## 6. Non-functional Requirements & Constraints
- Job chuyển trạng thái Đã lên lịch → Đang chạy chạy lúc 00:00 giờ VN và idempotent. Ngoài ra trạng thái cũng được tính lại khi đọc, để không phụ thuộc hoàn toàn vào job.
- NFR chung ở PRD tổng.

## 7. Dependencies
- F05 (nội dung phase).

## 8. Open Questions

| # | Question | Blocking | Answer | PIC |
|---|---|---|---|---|
| 1 | Phase tự Kết thúc sau ngày cuối hay chờ coach bấm Kết thúc? | No | **Đã chốt:** chờ coach bấm (BR-39); phase kế tiếp chờ, không tự chạy (BR-37); coach chọn ngày bắt đầu phase kế tiếp, mặc định hôm nay (BR-43) | Product |
