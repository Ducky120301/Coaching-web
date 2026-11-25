import { After, AfterAll, Before, BeforeAll, World, setDefaultTimeout, setWorldConstructor } from '@cucumber/cucumber';
import { Browser, BrowserContext, Page, chromium } from 'playwright';
import { ADMIN_URL, CLIENT_URL } from './env';

setDefaultTimeout(60_000);
let browser: Browser;

export type WebApp = 'quản trị' | 'học viên';
export const urlOf = (app: WebApp) => (app === 'quản trị' ? ADMIN_URL : CLIENT_URL);

export class E2EWorld extends World {
  context!: BrowserContext;
  page!: Page;
  pages: Page[] = [];
  account: { email: string; password: string } | null = null;
  expectedUrl = '';
  visual: Record<string, unknown> = {};

  async openPage(url: string): Promise<Page> {
    const page = await this.context.newPage();
    this.pages.push(page);
    await page.goto(url);
    return page;
  }

  async login(page: Page, email: string, password: string): Promise<void> {
    await page.getByLabel('Email').fill(email);
    await page.getByLabel('Mật khẩu').fill(password);
    await page.getByRole('button', { name: 'Đăng nhập' }).click();
  }
}

setWorldConstructor(E2EWorld);
BeforeAll(async () => { browser = await chromium.launch(); });
AfterAll(async () => { await browser?.close(); });
Before(async function (this: E2EWorld) {
  this.context = await browser.newContext({ reducedMotion: 'reduce' });
  this.page = await this.context.newPage();
  this.pages = [this.page];
});
After(async function (this: E2EWorld) { await this.context?.close(); });
