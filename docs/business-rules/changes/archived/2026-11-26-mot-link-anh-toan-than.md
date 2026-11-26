---
state: Archived
type: change
date: 2026-11-26
feature: hoc-vien/ho-so/ho-so-hoc-vien.feature
br: BR-f02-ho-so-hoc-vien-011
source: to-spec
---

# Rule change — Một link ảnh toàn thân thay 4 link ảnh

> **Feature:** features/hoc-vien/ho-so/ho-so-hoc-vien.feature · **Rule:** @BR-f02-ho-so-hoc-vien-011 · **Type:** Thay đổi

## What changed
- **Was (living doc):** Ví dụ dùng "ảnh mặt trước".
- **Now (spec):** Rule giữ nguyên (link phải http/https); ví dụ đổi sang "Link ảnh toàn thân". Evidence: `docs/agentic-workflow/specs/2026-11-26-f02-import-excel.md` (BR-f02-import-excel-006).
- **Examples:** ✅ Link ảnh toàn thân https://drive.google.com/open?id=abc ghi nhận  ❌ drive.google.com/abc bị từ chối

## Attribution
- **Commit:** f98c00e — docs(bdd): F02 hồ sơ học viên living doc (25 rules)   **Author:** Hoangduc1203BK · **When:** 2026-11-25
- **Why:** PO chuẩn hoá form theo file Excel/Google Form để import (2026-11-26).

## Living doc reference
- `features/hoc-vien/ho-so/ho-so-hoc-vien.feature:136:145` — Rule @BR-f02-ho-so-hoc-vien-011
