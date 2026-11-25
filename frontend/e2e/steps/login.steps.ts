import { Given, Then, When } from '@cucumber/cucumber';
import { expect } from 'playwright/test';
import { ADMIN, STUDENT } from '../support/env';
import { E2EWorld, WebApp, urlOf } from '../support/world';

// @BR-f00-codebase-004 @BR-f00-codebase-005
Given('một tài khoản admin', function (this: E2EWorld) { this.account = ADMIN; });
Given('một tài khoản học viên', function (this: E2EWorld) { this.account = STUDENT; });

When(/^(admin|học viên) đăng nhập web (quản trị|học viên) bằng email và mật khẩu đúng$/,
  async function (this: E2EWorld, _who: string, app: WebApp) {
    await this.page.goto(`${urlOf(app)}/login`);
    await this.login(this.page, this.account!.email, this.account!.password);
  });

Then('admin vào được khu vực coach', async function (this: E2EWorld) {
  await expect(this.page.getByText('Khu vực coach', { exact: true })).toBeVisible();
  await expect(this.page.getByRole('heading', { name: /^Xin chào,/ })).toBeVisible();
});

Then('học viên vào được khu vực học viên', async function (this: E2EWorld) {
  await expect(this.page.getByRole('heading', { name: /^Chào / })).toBeVisible();
});

Then('hệ thống từ chối với thông báo {string}', async function (this: E2EWorld, message: string) {
  await expect(this.page.getByRole('alert')).toHaveText(message);
});

Then(/^(admin|học viên) vẫn ở trạng thái chưa đăng nhập web (quản trị|học viên)$/,
  async function (this: E2EWorld, _who: string, app: WebApp) {
    await expect(this.page).toHaveURL(/\/login/);
    const key = app === 'quản trị' ? 'dbx.admin.rt' : 'dbx.client.rt';
    expect(await this.page.evaluate((k) => localStorage.getItem(k), key)).toBeNull();
  });
