import { Given, Then, When } from '@cucumber/cucumber';
import { expect } from '../support/expect';
import { ADMIN, ADMIN_URL, CLIENT_URL, STUDENT } from '../support/env';
import { E2EWorld } from '../support/world';

async function loginAdmin(world: E2EWorld, page = world.page) {
  await page.goto(`${ADMIN_URL}/login`);
  await world.login(page, ADMIN.email, ADMIN.password);
  await expect(page.getByRole('heading', { name: /^Xin chào,/ })).toBeVisible();
}

async function logoutVia(page: import('playwright').Page) {
  await page.getByRole('button', { name: /Coach|Quản trị viên/ }).click();
  await page.getByRole('menuitem', { name: 'Đăng xuất' }).click();
}

// @BR-f00-codebase-010: server trả 401 thật cho request đầu (token không hợp lệ) → web tự gia hạn
Given('admin đang làm việc và lượt truy cập vừa hết hạn', async function (this: E2EWorld) {
  await loginAdmin(this);
  this.rejectedOnce = false;
  this.refreshStatus = null;
  await this.page.route('**/api/admin/profile', async (route, request) => {
    if (!this.rejectedOnce) {
      this.rejectedOnce = true;
      await route.continue({ headers: { ...request.headers(), authorization: 'Bearer het-han' } });
    } else {
      await route.continue();
    }
  });
});

When('admin xem hồ sơ coach của mình', async function (this: E2EWorld) {
  const rejected = this.page.waitForResponse((r) => r.url().includes('/api/admin/profile') && r.status() === 401);
  const refreshed = this.page.waitForResponse((r) => r.url().includes('/api/auth/refresh'));
  await Promise.all([rejected, refreshed, this.page.goto(`${ADMIN_URL}/trang-chu`)]);
  this.refreshStatus = (await refreshed).status();
});

Then('admin thấy hồ sơ coach mà không phải đăng nhập lại', async function (this: E2EWorld) {
  expect(this.rejectedOnce, 'request đầu bị trả 401').toBe(true);
  expect(this.refreshStatus, '/api/auth/refresh trả 200').toBe(200);
  await expect(this.page.getByRole('heading', { name: /^Xin chào,/ })).toBeVisible();
  await expect(this.page).not.toHaveURL(/\/login/);
});

// @BR-f00-codebase-013
Given('admin đã đăng nhập web quản trị', async function (this: E2EWorld) { await loginAdmin(this); });
When('admin đăng xuất', async function (this: E2EWorld) { await logoutVia(this.page); });
Then('admin ở trạng thái chưa đăng nhập web quản trị', async function (this: E2EWorld) {
  await expect(this.page).toHaveURL(/\/login/);
  expect(await this.page.evaluate(() => localStorage.getItem('dbx.admin.rt'))).toBeNull();
});

// @BR-f00-codebase-014
Given('admin đang mở web quản trị ở hai tab của cùng trình duyệt', async function (this: E2EWorld) {
  await loginAdmin(this);
  const second = await this.openPage(`${ADMIN_URL}/trang-chu`);
  await expect(second.getByRole('heading', { name: /^Xin chào,/ })).toBeVisible();
});
When('admin đăng xuất ở tab thứ nhất', async function (this: E2EWorld) { await logoutVia(this.pages[0]); });
Then('tab thứ hai cũng ở trạng thái chưa đăng nhập', async function (this: E2EWorld) {
  await expect(this.pages[1]).toHaveURL(/\/login/);
});

Given('trên cùng trình duyệt có một phiên ở web quản trị và một phiên ở web học viên', async function (this: E2EWorld) {
  await loginAdmin(this);
  const client = await this.openPage(`${CLIENT_URL}/login`);
  await this.login(client, STUDENT.email, STUDENT.password);
  await expect(client.getByRole('heading', { name: /^Chào / })).toBeVisible();
});
When('người dùng đăng xuất web quản trị', async function (this: E2EWorld) { await logoutVia(this.pages[0]); });
Then('phiên ở web học viên vẫn còn đăng nhập', async function (this: E2EWorld) {
  const client = this.pages[1];
  await client.reload();
  await expect(client.getByRole('heading', { name: /^Chào / })).toBeVisible();
});

// @BR-f00-codebase-015
Given('admin chưa đăng nhập và mở một trang con của khu vực coach', async function (this: E2EWorld) {
  this.expectedUrl = `${ADMIN_URL}/trang-chu?tu=e2e`;
  await this.page.goto(this.expectedUrl);
  await expect(this.page).toHaveURL(/\/login\?returnUrl=/);
});
Given('admin mở một đường dẫn đăng nhập web quản trị yêu cầu quay về một trang ngoài web quản trị', async function (this: E2EWorld) {
  await this.page.goto(`${ADMIN_URL}/login?returnUrl=${encodeURIComponent('https://evil.example.com/')}`);
});
When('admin đăng nhập web quản trị thành công', async function (this: E2EWorld) {
  await this.login(this.page, ADMIN.email, ADMIN.password);
  await expect(this.page.getByRole('heading', { name: /^Xin chào,/ })).toBeVisible();
});
Then('admin được đưa về đúng trang con đó', async function (this: E2EWorld) {
  await expect(this.page).toHaveURL(this.expectedUrl);
});
Then('admin được đưa về trang chủ của web quản trị', async function (this: E2EWorld) {
  await expect(this.page).toHaveURL(`${ADMIN_URL}/trang-chu`);
});
