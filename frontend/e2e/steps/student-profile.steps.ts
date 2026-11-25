import { Given, Then, When } from '@cucumber/cucumber';
import { expect } from '../support/expect';
import { ADMIN, ADMIN_URL } from '../support/env';
import { E2EWorld } from '../support/world';

/** DB của compose giữ dữ liệu giữa các lần chạy → thêm hậu tố vào email để không trùng (BR-f02-ho-so-hoc-vien-001). */
const uniqueEmail = (email: string) =>
  email.replace('@', `+e2e${Date.now()}${Math.floor(Math.random() * 1000)}@`);

// Background của features/hoc-vien/ho-so/ho-so-hoc-vien.feature (admin seed của compose là coach duy nhất)
Given('coach {string} đã đăng nhập', async function (this: E2EWorld, _coachName: string) {
  await this.page.goto(`${ADMIN_URL}/login`);
  await this.login(this.page, ADMIN.email, ADMIN.password);
  await expect(this.page.getByRole('heading', { name: 'Học viên', exact: true })).toBeVisible();
});

// @BR-f02-ho-so-hoc-vien-003
When('coach tạo hồ sơ học viên chỉ với họ tên {string} và email {string}',
  async function (this: E2EWorld, fullName: string, email: string) {
    await this.page.getByRole('link', { name: 'Tạo hồ sơ học viên' }).first().click();
    await expect(this.page.getByRole('heading', { name: 'Tạo hồ sơ học viên', level: 1 })).toBeVisible();
    await this.page.getByLabel('Họ và tên').fill(fullName);
    await this.page.getByLabel('Email đăng nhập').fill(uniqueEmail(email));
    await this.page.getByRole('button', { name: 'Lưu hồ sơ' }).click();
  });

Then('hồ sơ của {string} được lưu', async function (this: E2EWorld, fullName: string) {
  await expect(this.page).toHaveURL(/\/hoc-vien\/[0-9a-f-]{36}$/);
  await expect(this.page.getByRole('heading', { level: 1, name: fullName, exact: true })).toBeVisible();
  expect(await this.page.evaluate(() => localStorage.getItem('dbx.admin.student-draft'))).toBeNull();
});

// @BR-f02-ho-so-hoc-vien-024
Given('có hồ sơ {string} với email {string}', async function (this: E2EWorld, fullName: string, email: string) {
  const login = await this.page.request.post(`${ADMIN_URL}/api/auth/login`,
    { data: { email: ADMIN.email, password: ADMIN.password, app: 'admin' } });
  expect(login.ok()).toBe(true);
  const { accessToken } = (await login.json()) as { accessToken: string };
  const created = await this.page.request.post(`${ADMIN_URL}/api/admin/students`, {
    data: { fullName, email: uniqueEmail(email) },
    headers: { authorization: `Bearer ${accessToken}` },
  });
  expect(created.status()).toBe(201);
});

When('coach tìm học viên với từ khóa {string}', async function (this: E2EWorld, keyword: string) {
  await this.page.goto(`${ADMIN_URL}/dashboard`);
  await expect(this.page.getByRole('heading', { name: 'Học viên', exact: true })).toBeVisible();
  await this.page.getByLabel('Tìm học viên').fill(keyword);
});

Then('kết quả có hồ sơ {string}', async function (this: E2EWorld, fullName: string) {
  await expect(this.page.getByRole('link', { name: fullName, exact: true }).first()).toBeVisible();
});
