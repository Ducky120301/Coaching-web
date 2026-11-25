import { Given, Then, When } from '@cucumber/cucumber';
import { mkdirSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import { PNG } from 'pngjs';
import { ADMIN, ADMIN_URL, CLIENT_URL, STUDENT } from '../support/env';
import { expect } from '../support/expect';
import { Rect, captureRegion, diffRatio } from '../support/visual';
import { E2EWorld } from '../support/world';

const PROTOTYPE = pathToFileURL(resolve(process.cwd(), '../docs/duobexiuuu-coaching-design.html')).href;
type Screen = 'màn đăng nhập' | 'khung web quản trị' | 'khung web học viên';

interface Target { protoPage: string; protoRegion: string; protoMasks: string[]; appRegion: string; appMasks: string[]; protoCss?: string; appCss?: string; }

// nav items and the user block hold different content in P0 (masked) → pin their heights so the rail is compared at equal geometry
const RAIL_NORMALIZE_PROTO = 'aside.rail{height:900px!important;min-height:0!important} aside.rail>nav{height:120px!important;overflow:hidden!important} aside.rail>div:last-child{height:60px!important;overflow:hidden!important}';
const RAIL_NORMALIZE_APP = 'aside.rail{height:900px!important;min-height:0!important} aside.rail>nav{height:120px!important;overflow:hidden!important} aside.rail>.user-block{height:60px!important;overflow:hidden!important}';

function targetFor(screen: Screen, width: number): Target {
  if (screen === 'màn đăng nhập') {
    return { protoPage: 'Main', protoRegion: 'body', protoMasks: ['section[aria-label="Đăng nhập"] button[type="button"]', '#dbx-ed-toggle'],
      appRegion: 'body', appMasks: [] };
  }
  if (screen === 'khung web quản trị') {
    return { protoPage: 'Admin-Dashboard', protoRegion: 'aside.rail', protoMasks: ['nav', ':scope > div:last-child'],
      appRegion: 'aside.rail', appMasks: ['nav', '.user-block'], protoCss: RAIL_NORMALIZE_PROTO, appCss: RAIL_NORMALIZE_APP };
  }
  return width >= 880
    ? { protoPage: 'HvHomNay', protoRegion: 'aside.rail', protoMasks: ['nav', ':scope > div:last-child', ':scope > a'],
        appRegion: 'aside.rail', appMasks: ['nav', '.user-block'], protoCss: RAIL_NORMALIZE_PROTO, appCss: RAIL_NORMALIZE_APP }
    : { protoPage: 'HvHomNay', protoRegion: 'nav[aria-label="Điều hướng chính"]', protoMasks: ['a'],
        appRegion: 'nav.tabbar', appMasks: ['a', 'dbx-user-menu'] };
}

Given('thiết kế đã duyệt của {string}', function (this: E2EWorld, screen: Screen) {
  this.visual['screen'] = screen;
});

When('{string} được hiển thị ở độ rộng {string} px', async function (this: E2EWorld, screen: Screen, w: string) {
  const width = Number(w);
  const t = targetFor(screen, width);
  const viewport = { width, height: 900 };

  const proto = await this.context.newPage();
  this.pages.push(proto);
  await proto.setViewportSize(viewport);
  await proto.goto(`${PROTOTYPE}#${t.protoPage}&frame`);
  if (screen === 'màn đăng nhập') {
    await proto.locator('#em').fill('quan.pham@gmail.com');
    await proto.locator('#pw').fill('tamthoi-8K2q');
  }
  const a = await captureRegion(proto, t.protoRegion, t.protoMasks, t.protoCss);

  const app = this.page;
  await app.setViewportSize(viewport);
  if (screen === 'màn đăng nhập') {
    await app.goto(`${ADMIN_URL}/login`);
    await app.getByLabel('Email').fill('quan.pham@gmail.com');
    await app.getByLabel('Mật khẩu').fill('tamthoi-8K2q');
  } else {
    const [url, acc] = screen === 'khung web quản trị' ? [ADMIN_URL, ADMIN] : [CLIENT_URL, STUDENT];
    await app.goto(`${url}/login`);
    await this.login(app, acc.email, acc.password);
    await expect(app).not.toHaveURL(/\/login/);
  }
  const b = await captureRegion(app, t.appRegion, t.appMasks, t.appCss);

  const masks: Rect[] = [...a.masks, ...b.masks];
  this.visual = { screen, width, ...diffRatio(a.png, b.png, masks) };
});

Then('phần khác biệt so với thiết kế không quá {string} phần trăm diện tích', function (this: E2EWorld, pct: string) {
  const { ratio, diff, screen, width } = this.visual as { ratio: number; diff: PNG; screen: string; width: number };
  if (ratio * 100 > Number(pct)) {
    const dir = resolve(process.cwd(), 'e2e/reports/visual');
    mkdirSync(dir, { recursive: true });
    writeFileSync(resolve(dir, `${screen.replace(/\s+/g, '-')}-${width}.png`), PNG.sync.write(diff));
  }
  this.log(`${screen} @${width}px: ${(ratio * 100).toFixed(2)}%`);
  expect(ratio * 100, `${screen} @${width}px lệch ${(ratio * 100).toFixed(2)}%`).toBeLessThanOrEqual(Number(pct));
});
