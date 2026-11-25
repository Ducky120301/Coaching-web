// @ADR-f00-codebase-009: chụp cùng vùng ở prototype và app, tô kín mask trên cả hai, so bằng pixelmatch
import { Page } from 'playwright';
import pixelmatch from 'pixelmatch';
import { PNG } from 'pngjs';

export interface Rect { x: number; y: number; width: number; height: number; }

export async function settle(page: Page): Promise<void> {
  await page.waitForLoadState('networkidle');
  await page.evaluate(() => document.fonts.ready);
  await page.addStyleTag({ content: '#proto-ui{display:none!important} *{caret-color:transparent!important}' });
}

// `normalizeCss`: pins the size of masked containers whose content legitimately differs (P0 has fewer nav items),
// so the surrounding chrome is compared at the same coordinates instead of being shifted by content height.
export async function captureRegion(page: Page, regionSelector: string, maskSelectors: string[], normalizeCss = '') {
  await settle(page);
  if (normalizeCss) await page.addStyleTag({ content: normalizeCss });
  const region = page.locator(regionSelector).first();
  const box = await region.boundingBox();
  if (!box) throw new Error(`Vùng ${regionSelector} không hiển thị`);
  const masks: Rect[] = [];
  for (const sel of maskSelectors) {
    for (const el of await region.locator(sel).all()) {
      const b = await el.boundingBox();
      if (b) masks.push({ x: b.x - box.x, y: b.y - box.y, width: b.width, height: b.height });
    }
  }
  const png = PNG.sync.read(await region.screenshot({ animations: 'disabled' }));
  return { png, masks };
}

function padTo(src: PNG, width: number, height: number): PNG {
  const out = new PNG({ width, height });
  out.data.fill(255);
  PNG.bitblt(src, out, 0, 0, src.width, src.height, 0, 0);
  return out;
}

function fill(img: PNG, rect: Rect): void {
  const x0 = Math.max(0, Math.floor(rect.x)), y0 = Math.max(0, Math.floor(rect.y));
  const x1 = Math.min(img.width, Math.ceil(rect.x + rect.width)), y1 = Math.min(img.height, Math.ceil(rect.y + rect.height));
  for (let y = y0; y < y1; y++) for (let x = x0; x < x1; x++) {
    const i = (img.width * y + x) << 2;
    img.data[i] = 255; img.data[i + 1] = 0; img.data[i + 2] = 255; img.data[i + 3] = 255;
  }
}

export function diffRatio(a: PNG, b: PNG, masks: Rect[]): { ratio: number; diff: PNG; sizeNote: string } {
  const width = Math.max(a.width, b.width), height = Math.max(a.height, b.height);
  const pa = padTo(a, width, height), pb = padTo(b, width, height);
  for (const m of masks) { fill(pa, m); fill(pb, m); }
  const diff = new PNG({ width, height });
  const changed = pixelmatch(pa.data, pb.data, diff.data, width, height, { threshold: 0.1 });
  const sizeNote = Math.abs(a.width - b.width) > 2 || Math.abs(a.height - b.height) > 2
    ? `kích thước khác: ${a.width}x${a.height} vs ${b.width}x${b.height}` : '';
  return { ratio: changed / (width * height), diff, sizeNote };
}

export interface Measure { visible: boolean; width: number; height: number; }

// Đo hình học thật (không phải ảnh) để so app với prototype trong cùng một lần chạy.
export async function measure(page: Page, selector: string): Promise<Measure> {
  const loc = page.locator(selector).first();
  if ((await loc.count()) === 0 || !(await loc.isVisible())) return { visible: false, width: 0, height: 0 };
  const b = await loc.boundingBox();
  return b ? { visible: true, width: b.width, height: b.height } : { visible: false, width: 0, height: 0 };
}
