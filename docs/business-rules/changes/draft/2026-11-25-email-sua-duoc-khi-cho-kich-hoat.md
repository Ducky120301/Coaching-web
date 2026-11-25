---
state: Draft
type: change
date: 2026-11-25
feature: hoc-vien/ho-so/ho-so-hoc-vien.feature
br: BR-f02-ho-so-hoc-vien-012
source: to-spec
---

# Rule change — Email học viên sửa được khi tài khoản còn Chờ kích hoạt

> **Feature:** features/hoc-vien/ho-so/ho-so-hoc-vien.feature · **Rule:** @BR-f02-ho-so-hoc-vien-012 · **Type:** Thay đổi

## What changed
- **Was (living doc):** "Coach sửa được mọi mục của hồ sơ trừ email, kể cả số đo và ảnh ban đầu" — email bị khoá ở mọi trạng thái tài khoản.
- **Now (spec):** Coach sửa được mọi mục của hồ sơ trừ email, kể cả số đo và ảnh ban đầu; riêng email được quản lý bởi rule mới @BR-f01-p1-tai-khoan-hoc-vien-007 — "Email của học viên chỉ sửa được khi tài khoản đang Chờ kích hoạt". Evidence: `docs/agentic-workflow/specs/2026-11-25-f01-p1-tai-khoan-hoc-vien.md` (BR-f01-p1-tai-khoan-hoc-vien-007).
- **Examples:** ✅ Sửa "quan@gmial.com" thành "quan@gmail.com" khi Chờ kích hoạt → email mới được lưu  ❌ Sửa email khi Đã kích hoạt → từ chối

## Attribution
- **Commit:** f98c00e — docs(bdd): F02 hồ sơ học viên living doc (25 rules)   **Author:** Hoangduc1203BK · **When:** 2026-11-25
- **Why:** F01-P1 cần cho coach sửa email gõ sai trước khi học viên kích hoạt (PRD F01, quyết định OQ trong discovery F01-P1).
