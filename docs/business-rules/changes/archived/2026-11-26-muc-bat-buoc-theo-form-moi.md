---
state: Archived
type: change
date: 2026-11-26
feature: hoc-vien/ho-so/ho-so-hoc-vien.feature
br: BR-f02-ho-so-hoc-vien-004
source: to-spec
---

# Rule change — Ví dụ mục còn thiếu theo form mới

> **Feature:** features/hoc-vien/ho-so/ho-so-hoc-vien.feature · **Rule:** @BR-f02-ho-so-hoc-vien-004 · **Type:** Thay đổi

## What changed
- **Was (living doc):** Ví dụ thiếu "Ảnh mặt sau".
- **Now (spec):** Rule giữ nguyên; ví dụ đổi mục ảnh thành "Link ảnh toàn thân" (danh sách mục bắt buộc mới). Evidence: `docs/agentic-workflow/specs/2026-11-26-f02-import-excel.md` (BR-f02-import-excel-007).
- **Examples:** ✅ Thiếu "Gói đã mua", "Mục tiêu tập luyện", "Link ảnh toàn thân" → hồ sơ lưu, báo đúng 3 mục

## Attribution
- **Commit:** f98c00e — docs(bdd): F02 hồ sơ học viên living doc (25 rules)   **Author:** Hoangduc1203BK · **When:** 2026-11-25
- **Why:** PO chuẩn hoá form theo file Excel/Google Form để import (2026-11-26).

## Living doc reference
- `features/hoc-vien/ho-so/ho-so-hoc-vien.feature:54:64` — Rule @BR-f02-ho-so-hoc-vien-004
