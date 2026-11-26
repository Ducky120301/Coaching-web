import { After, AfterAll, Before, BeforeAll, Status, World, setDefaultTimeout, setWorldConstructor } from '@cucumber/cucumber';
import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { Browser, BrowserContext, Page, chromium } from 'playwright';
import { ADMIN_URL, CLIENT_URL } from './env';
import type { ImportedLearner, ImportFile, ImportResultBody, SkippedLine } from '../steps/import.steps';

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
  rejectedOnce = false;
  refreshStatus: number | null = null;
  /** F01-P1: học viên tạo trong scenario (email thật đã có hậu tố +e2e…). */
  student: { id: string; fullName: string; email: string } | null = null;
  /** F01-P1: email viết trong feature (chuẩn hoá) → hậu tố +e2e… để không trùng dữ liệu của lần chạy trước. */
  emailSuffix: Record<string, string> = {};
  tempPassword = '';
  /** F02 import Excel: file sinh trong scenario, các dòng học viên trong file, dòng dự kiến bị bỏ qua, kết quả API. */
  importFile: ImportFile | null = null;
  importRows: ImportedLearner[] = [];
  importSkipped: SkippedLine[] = [];
  importResult: ImportResultBody | null = null;
  /** F03: hậu tố " e2e…" gắn vào mọi tên bài trong scenario (DB compose giữ dữ liệu + có 99 bài nạp sẵn). */
  exerciseSuffix = '';

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
After(async function (this: E2EWorld, { pickle, result }) {
  if (result?.status === Status.FAILED) {
    const dir = join(__dirname, '..', 'reports', 'screenshots');
    mkdirSync(dir, { recursive: true });
    const name = pickle.name.replace(/[^\p{L}\p{N}]+/gu, '-').slice(0, 80);
    for (const [i, page] of this.pages.entries()) {
      try {
        const buffer = await page.screenshot({ fullPage: true });
        this.attach(buffer, 'image/png');
        writeFileSync(join(dir, `${name}${i ? `-${i + 1}` : ''}.png`), buffer);
      } catch { /* page already closed */ }
    }
  }
  await this.context?.close();
});
