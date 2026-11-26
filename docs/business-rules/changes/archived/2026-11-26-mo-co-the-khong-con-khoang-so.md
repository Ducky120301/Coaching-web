---
state: Archived
type: change
date: 2026-11-26
feature: hoc-vien/ho-so/ho-so-hoc-vien.feature
br: BR-f02-ho-so-hoc-vien-008
source: to-spec
---

# Rule change — Mỡ cơ thể không còn là số trong khoảng 3–70 %

> **Feature:** features/hoc-vien/ho-so/ho-so-hoc-vien.feature · **Rule:** @BR-f02-ho-so-hoc-vien-008 · **Type:** Thay đổi

## What changed
- **Was (living doc):** Rule liệt kê "mỡ cơ thể 3–70 %" trong các khoảng số; ví dụ Mỡ cơ thể 3 và 70.
- **Now (spec):** Bỏ "mỡ cơ thể 3–70 %" khỏi rule khoảng số; mỡ cơ thể nằm trong mục chữ tự do "Mỡ cơ thể và cách đo". Evidence: `docs/agentic-workflow/specs/2026-11-26-f02-import-excel.md` (BR-f02-import-excel-004).
- **Examples:** ✅ Cân nặng 72,5 vẫn ghi nhận  ❌ Cân nặng 720 vẫn bị từ chối (ví dụ Mỡ cơ thể bị bỏ)

## Attribution
- **Commit:** f98c00e — docs(bdd): F02 hồ sơ học viên living doc (25 rules)   **Author:** Hoangduc1203BK · **When:** 2026-11-25
- **Why:** PO chuẩn hoá form theo file Excel/Google Form để import (2026-11-26).

## Living doc reference
- `features/hoc-vien/ho-so/ho-so-hoc-vien.feature:87:109` — Rule @BR-f02-ho-so-hoc-vien-008
