import { Given, Then, When } from '@cucumber/cucumber';
import { expect } from '../support/expect';
import { ADMIN, ADMIN_URL } from '../support/env';
import { E2EWorld } from '../support/world';

interface Category { id: string; name: string; }
interface Categories { muscleGroups: Category[]; equipment: Category[]; }

/**
 * DB của compose giữ dữ liệu giữa các lần chạy và có 99 bài nạp sẵn ("Lateral Raise" + Cable, "45° Incline Chest Fly"…)
 * → mỗi tên bài viết trong feature được gắn một hậu tố cố định trong scenario để không bị chặn trùng (BR-f03-thu-vien-bai-tap-001).
 */
function realName(world: E2EWorld, written: string): string {
  world.exerciseSuffix ||= ` e2e${Date.now()}${Math.floor(Math.random() * 1000)}`;
  return `${written}${world.exerciseSuffix}`;
}

async function authHeaders(world: E2EWorld): Promise<Record<string, string>> {
  const res = await world.page.request.post(`${ADMIN_URL}/api/auth/login`,
    { data: { email: ADMIN.email, password: ADMIN.password, app: 'admin' } });
  expect(res.ok()).toBe(true);
  const { accessToken } = (await res.json()) as { accessToken: string };
  return { authorization: `Bearer ${accessToken}` };
}

function categoryId(list: Category[], name: string): string {
  const found = list.find((c) => c.name === name);
  if (!found) throw new Error(`Danh mục không có "${name}"`);
  return found.id;
}

/** Tạo bài qua API (dữ liệu có sẵn của Given). */
async function createExercise(world: E2EWorld, name: string, muscleGroup: string, equipment: string): Promise<void> {
  const headers = await authHeaders(world);
  const cats = await world.page.request.get(`${ADMIN_URL}/api/admin/exercise-categories`, { headers });
  expect(cats.ok()).toBe(true);
  const { muscleGroups, equipment: equipmentList } = (await cats.json()) as Categories;
  const res = await world.page.request.post(`${ADMIN_URL}/api/admin/exercises`, {
    headers,
    data: { name, muscleGroupId: categoryId(muscleGroups, muscleGroup), equipmentId: categoryId(equipmentList, equipment) },
  });
  expect(res.status()).toBe(201);
}

/** Mở Thư viện bài tập, gõ từ khoá rồi Enter (ô tìm của AdThuVien tìm khi gõ xong bấm Enter). */
async function searchLibrary(world: E2EWorld, keyword: string): Promise<void> {
  await world.page.goto(`${ADMIN_URL}/thu-vien/bai-tap`);
  await expect(world.page.getByRole('heading', { level: 1, name: 'Thư viện bài tập' })).toBeVisible();
  const box = world.page.getByLabel('Tìm bài tập');
  await box.fill(keyword);
  await box.press('Enter');
}

/** Dòng bảng có đúng tên bài (rowheader) và đúng dụng cụ (ô). */
function libraryRow(world: E2EWorld, name: string, equipment: string) {
  return world.page.locator('tbody tr')
    .filter({ has: world.page.getByRole('rowheader', { name, exact: true }) })
    .filter({ has: world.page.getByRole('cell', { name: equipment, exact: true }) });
}

// @BR-f03-thu-vien-bai-tap-001 — nhóm cơ của bài có sẵn không quan trọng với rule, dùng "Vai" như bài seed
Given('thư viện có bài {string} dụng cụ {string}', async function (this: E2EWorld, name: string, equipment: string) {
  await createExercise(this, realName(this, name), 'Vai', equipment);
});

// @BR-f03-thu-vien-bai-tap-001
When('coach thêm bài {string} dụng cụ {string} nhóm cơ {string}',
  async function (this: E2EWorld, name: string, equipment: string, muscleGroup: string) {
    await this.page.goto(`${ADMIN_URL}/thu-vien/bai-tap`);
    await this.page.getByRole('link', { name: 'Thêm bài mới' }).click();
    await expect(this.page.getByRole('heading', { level: 1, name: 'Thêm bài mới' })).toBeVisible();
    await this.page.getByLabel('Tên bài').fill(realName(this, name));
    await this.page.getByLabel('Nhóm cơ').selectOption({ label: muscleGroup });
    await this.page.getByLabel('Dụng cụ').selectOption({ label: equipment });
    await this.page.getByRole('button', { name: 'Lưu bài' }).click();
    await expect(this.page).toHaveURL(/\/thu-vien\/bai-tap\?bai=[0-9a-f-]{36}$/);
  });

// @BR-f03-thu-vien-bai-tap-001
Then('thư viện có cả {string} dụng cụ {string} và {string} dụng cụ {string}',
  async function (this: E2EWorld, name1: string, equipment1: string, name2: string, equipment2: string) {
    await searchLibrary(this, realName(this, name1));
    await expect(libraryRow(this, realName(this, name1), equipment1)).toHaveCount(1);
    await expect(libraryRow(this, realName(this, name2), equipment2)).toHaveCount(1);
  });

// @BR-f03-thu-vien-bai-tap-018
Given('thư viện có bài {string}', async function (this: E2EWorld, name: string) {
  await createExercise(this, realName(this, name), 'Ngực', 'Machine');
});

// @BR-f03-thu-vien-bai-tap-018 — tìm ở server (bỏ dấu, không phân biệt hoa/thường)
When('coach tìm bài với từ khoá {string}', async function (this: E2EWorld, keyword: string) {
  await searchLibrary(this, keyword);
});

// @BR-f03-thu-vien-bai-tap-018
Then('kết quả có {string}', async function (this: E2EWorld, name: string) {
  await expect(this.page.getByRole('rowheader', { name: realName(this, name), exact: true })).toBeVisible();
});
