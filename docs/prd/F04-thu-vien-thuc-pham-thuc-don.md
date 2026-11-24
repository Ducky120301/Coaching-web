# F04 — Thư viện thực phẩm & thực đơn

> Thuộc [PRD tổng](../PRD-coaching-web.md). Phase: **P1** (thư viện thực phẩm) · **P2** (thực đơn trong giáo án).

## 1. Metadata
- **Feature Name:** Thư viện thực phẩm có macro; thực đơn theo bữa tự tính kcal/macro; đổi món giữ macro chính
- **Status:** Ready for Dev
- **Product Owner:** Hoangduc1203BK — duc.hm4@kiotviet.com

## 2. Context & Problem Statement
- Thực đơn trong template hiện tính kcal/macro thủ công nên dễ sai.
- Coach nhập mỗi thực phẩm một lần (C/F/P/kcal trên 100g, nhóm macro chính). Khi soạn thực đơn chỉ cần chọn thực phẩm và nhập gram; hệ thống tự cộng tổng và tự quy đổi lượng khi đổi món.

## 3. UX/UI
- Prototype: `AdSoanGiaoAn`, khối "Thực đơn theo bữa" (cột Lượng, Thực phẩm, Carb, Fat, Pro, Calo, Giờ ăn; "Thêm món"; "Về thực đơn gốc"; dòng tô hồng là món đã đổi hoặc thêm). Phía học viên xem ở `HvDinhDuong` (F11).

## 4. Scope Configuration
- **In-Scope:**
  - Coach thêm/sửa thực phẩm.
  - Thực đơn theo bữa (tên bữa, giờ ăn, món, lượng).
  - Tổng kcal/macro theo bữa và theo ngày.
  - Đổi món, đánh dấu món đã đổi, khôi phục thực đơn gốc.
  - Thực phẩm bổ sung theo nhóm (Pre-workout, Post-workout, Vitamin, Vi chất), có thời điểm dùng.
- **Out-of-Scope:**
  - Seed sẵn thư viện thực phẩm (coach tự nhập hoặc import qua F07).
  - Thực đơn khác nhau cho ngày tập và ngày nghỉ: mỗi phase chỉ có 1 thực đơn. Riêng thực phẩm bổ sung pre/post-workout gắn "chỉ ngày tập".

## 5. Functional Requirements

**BR-23 — Mỗi thực phẩm có kcal, Carb, Fat, Protein tính trên 100g và thuộc đúng một nhóm macro chính (Đạm / Tinh bột / Chất béo / Rau–khác).**
- ✅ Tạo "Ức gà (sống)": 120 kcal, P 23, F 2,6, C 0, nhóm Đạm → được
- ❌ Tạo thực phẩm chưa chọn nhóm macro chính → từ chối

**BR-24 — Macro của một món trong bữa = giá trị trên 100g × số gram ÷ 100. Tổng bữa và tổng ngày tự cộng từ các món.**
- ✅ Ức gà 200g với P 23/100g → món đó có 46g P
- ❌ Coach sửa tay tổng kcal của bữa → không cho sửa, tổng luôn được tính lại

**BR-25 — Khi đổi sang món cùng nhóm macro chính, lượng món mới được tự tính để lượng macro chính bằng món cũ.**
- ✅ Ức gà 200g (46g P) đổi sang cá trắng (P 20/100g) → cá trắng 230g
- ❌ Món mới giữ nguyên 200g → sai

**BR-86 — Lượng món được hệ thống tự quy đổi làm tròn tới 1g.**
- ✅ Tính ra 233,4g → hiển thị và lưu 233g
- ❌ Hiển thị 233,4g → sai

**BR-26 — Khi đổi sang món khác nhóm macro chính, hệ thống không tự quy đổi; coach tự nhập lượng.**
- ✅ Đổi ức gà sang cơm trắng → hệ thống yêu cầu nhập gram, tổng tính lại sau khi nhập
- ❌ Hệ thống tự quy đổi cơm theo lượng đạm của ức gà → sai

**BR-27 — Món đã đổi hoặc thêm so với thực đơn gốc được đánh dấu, và coach khôi phục được về thực đơn gốc.**
- ✅ Bấm "Về thực đơn gốc" → các món quay về như bản gốc, không còn dấu đánh dấu
- ❌ Món đã đổi hiển thị giống món gốc → sai

## 6. Non-functional Requirements & Constraints
- Macro hiển thị 1 chữ số thập phân, dùng dấu phẩy (46,0g); khi tính tổng giữ độ chính xác đầy đủ. Lượng món tính bằng gram nguyên (BR-86).
- Thực đơn trong giáo án lưu bản chụp giá trị macro tại thời điểm thêm món (giống BR-22 của bài tập).

## 7. Dependencies
- F07 (import thực phẩm từ Excel của coach).

## 8. Open Questions

| # | Question | Blocking | Answer | PIC |
|---|---|---|---|---|
| 1 | Có cần thực đơn riêng cho ngày tập và ngày nghỉ không? | No | **Đã chốt:** không, 1 thực đơn/phase | Product |
| 2 | Lượng tự quy đổi có làm tròn không? | No | **Đã chốt:** làm tròn tới 1g (BR-86) | Product |
