import { Given, Then, When } from '@cucumber/cucumber';
import { expect } from '../support/expect';
import { ADMIN, ADMIN_URL } from '../support/env';
import { buildXlsx } from '../support/xlsx';
import { E2EWorld } from '../support/world';

/** Tiêu đề nguyên văn file câu trả lời Google Form của PO (docs/FORM hồ sơ excel để import.xlsx). */
const COL_TIME = 'Dấu thời gian';
const COL_EMAIL = 'Địa chỉ email';
const COL_NAME = 'Họ Và Tên (Name)';
const COL_AGE = 'Tuổi (Age)';
const HEADERS = [COL_TIME, COL_EMAIL, COL_NAME, COL_AGE];
const XLSX = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';

export interface ImportedLearner { fullName: string; email: string; age: number; }
export interface ImportFile { name: string; mimeType: string; buffer: Buffer; }
export interface SkippedLine { row: number; email: string; column: string; reason: string; }
export interface ImportResultBody {
  created: number;
  skipped: number;
  rows: { row: number; email: string | null; status: 'Created' | 'Skipped'; studentId: string | null; errors: { column: string; reason: string }[] }[];
}

const NAMES = ['Nguyễn Thị Lan', 'Phạm Hoàng Quân', 'Trần Minh An', 'Lê Thu Hà', 'Võ Đức Huy'];

/** DB của compose giữ dữ liệu giữa các lần chạy → email duy nhất cho mỗi dòng. */
const uniqueEmail = (local: string) => `${local}+e2e${Date.now()}${Math.floor(Math.random() * 100000)}@gmail.com`;

function learner(i: number, age = 28): ImportedLearner {
  return { fullName: NAMES[i % NAMES.length], email: uniqueEmail(`import${i}`), age };
}

function useFile(world: E2EWorld, learners: ImportedLearner[]): void {
  world.importRows = learners;
  world.importFile = {
    name: 'cau-tra-loi-google-form.xlsx',
    mimeType: XLSX,
    buffer: buildXlsx([HEADERS, ...learners.map((l) => ['26/11/2026 09:00:00', l.email, l.fullName, l.age])]),
  };
}

async function adminToken(world: E2EWorld): Promise<string> {
  const res = await world.page.request.post(`${ADMIN_URL}/api/auth/login`,
    { data: { email: ADMIN.email, password: ADMIN.password, app: 'admin' } });
  expect(res.ok()).toBe(true);
  return ((await res.json()) as { accessToken: string }).accessToken;
}

const importCard = (world: E2EWorld) => world.page.getByRole('region', { name: 'Import từ Excel' });

// @BR-f02-import-excel-012
Given('một file Excel theo mẫu Google Form có {int} dòng học viên hợp lệ', function (this: E2EWorld, count: number) {
  useFile(this, Array.from({ length: count }, (_, i) => learner(i)));
});

// @BR-f02-import-excel-034: dòng Excel 2.. theo thứ tự: hợp lệ, rồi trùng email đã có, rồi tuổi sai (5 — ngoài 10–100)
Given('một file Excel có {int} dòng học viên: {int} dòng hợp lệ, {int} dòng trùng email đã có, {int} dòng tuổi sai',
  async function (this: E2EWorld, total: number, valid: number, duplicate: number, badAge: number) {
    expect(valid + duplicate + badAge).toBe(total);
    const token = await adminToken(this);
    const learners: ImportedLearner[] = [];
    for (let i = 0; i < valid; i++) learners.push(learner(i));
    for (let i = 0; i < duplicate; i++) {
      const existing = learner(valid + i);
      const res = await this.page.request.post(`${ADMIN_URL}/api/admin/students`, {
        data: { fullName: 'Học viên có sẵn', email: existing.email },
        headers: { authorization: `Bearer ${token}` },
      });
      expect(res.status()).toBe(201);
      learners.push(existing);
      this.importSkipped.push({ row: learners.length + 1, email: existing.email, column: COL_EMAIL, reason: 'email đã có hồ sơ' });
    }
    for (let i = 0; i < badAge; i++) {
      const wrong = learner(valid + duplicate + i, 5);
      learners.push(wrong);
      this.importSkipped.push({ row: learners.length + 1, email: wrong.email, column: COL_AGE, reason: 'phải từ 10 đến 100' });
    }
    useFile(this, learners);
  });

When('coach import file đó', async function (this: E2EWorld) {
  await this.page.getByRole('link', { name: 'Tạo hồ sơ học viên' }).first().click();
  await expect(this.page.getByRole('heading', { name: 'Tạo hồ sơ học viên', level: 1 })).toBeVisible();
  const card = importCard(this);
  await card.getByLabel('Chọn file .xlsx').setInputFiles(this.importFile!);
  await expect(card.getByText(this.importFile!.name, { exact: true })).toBeVisible();
  const [response] = await Promise.all([
    this.page.waitForResponse((r) => r.url().endsWith('/api/admin/students/import') && r.request().method() === 'POST'),
    card.getByRole('button', { name: 'Import', exact: true }).click(),
  ]);
  expect(response.status()).toBe(200);
  this.importResult = (await response.json()) as ImportResultBody;
  await expect(card.getByRole('status')).toBeVisible();
});

// @BR-f02-import-excel-012: coach đã đăng nhập import được — hồ sơ tạo đúng họ tên, email
Then('{int} hồ sơ học viên mới được tạo, có đúng họ tên và email trong file', async function (this: E2EWorld, count: number) {
  await expect(importCard(this).getByRole('status')).toHaveText(`Đã tạo ${count}, bỏ qua 0`);
  const created = this.importResult!.rows.filter((r) => r.status === 'Created');
  expect(created).toHaveLength(count);
  const token = await adminToken(this);
  for (const l of this.importRows) {
    const row = created.find((r) => r.email?.trim().toLowerCase() === l.email.toLowerCase());
    expect(row?.studentId, `không thấy hồ sơ tạo cho ${l.email}`).toBeTruthy();
    const res = await this.page.request.get(`${ADMIN_URL}/api/admin/students/${row!.studentId}`,
      { headers: { authorization: `Bearer ${token}` } });
    expect(res.ok()).toBe(true);
    const detail = (await res.json()) as { fullName: string; email: string };
    expect(detail.fullName).toBe(l.fullName);
    expect(detail.email.toLowerCase()).toBe(l.email.toLowerCase());
  }
});

// @BR-f02-import-excel-034
Then('coach thấy đã tạo {int}, bỏ qua {int}', async function (this: E2EWorld, created: number, skipped: number) {
  await expect(importCard(this).getByRole('status')).toHaveText(`Đã tạo ${created}, bỏ qua ${skipped}`);
});

Then('coach thấy lý do của từng dòng bị bỏ qua kèm số dòng Excel', async function (this: E2EWorld) {
  const card = importCard(this);
  await expect(card.getByRole('columnheader')).toHaveText(['Dòng', 'Email', 'Cột', 'Lý do']);
  const rows = card.locator('tbody tr');
  await expect(rows).toHaveCount(this.importSkipped.length);
  for (const [i, expected] of this.importSkipped.entries()) {
    const cells = (await rows.nth(i).locator('td').allInnerTexts()).map((t) => t.trim());
    expect([cells[0], cells[1].toLowerCase(), cells[2], cells[3]])
      .toEqual([String(expected.row), expected.email.toLowerCase(), expected.column, expected.reason]);
  }
});
