# Agentic workflow (kv-engineering)

Mỗi PRD tính năng trong `docs/prd/` đi theo pipeline:

```
discovery → to-spec → to-bdd → to-tech-design → to-plan → exec-plan → gen-steps → exec-review → verify-done → ship
```

## Quy ước

- **`<doc-id>` = `yyyy-MM-dd-<feature-slug>`**: ngày là ngày khởi tạo feature (đặt ở `to-spec`), thuần local, KHÔNG chứa JIRA id.
  `to-tech-design` / `to-plan` dùng lại đúng `<doc-id>` của spec.
- **Thư mục:**
  - `specs/<doc-id>.md` (to-spec): artifact đầu tiên, gán BR-ID / NFR-ID.
  - `tech-designs/<doc-id>.md` (to-tech-design): thiết kế kỹ thuật; tính năng có UI thì phần UI design nằm trong file này.
  - `plans/<doc-id>.md` (to-plan): implementation plan chia task.
- **Vòng đời status:** frontmatter `status: draft | ready` (spec thêm `completed`). Chỉ người dùng đánh dấu `ready`.
- **Readiness gate:** feature chỉ được `exec-plan` khi đủ 3 file (`specs/` + `tech-designs/` + `plans/` cùng `<doc-id>`) **và cả 3 đều `status: ready`**.
- **Living docs `.feature` (Cucumber):** nằm dưới `featuresRoot` trong `.kv/living-docs.json` (mặc định `features/`). Không thuộc `docs/agentic-workflow/` và không tính vào gate. Mỗi `Rule:` gắn BR-ID lấy từ spec.

Chi tiết: skill `using-kv-engineering`.
