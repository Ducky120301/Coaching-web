// @ADR-f00-codebase-006: ghi config.json từ env API_BASE_URL khi build trên Render
import { writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

const outDir = process.argv[2];
const apiBaseUrl = (process.env.API_BASE_URL ?? '').replace(/\/+$/, '');
if (!outDir || !apiBaseUrl) {
  console.error('Cần: node scripts/write-config.mjs <outDir> và env API_BASE_URL');
  process.exit(1);
}
writeFileSync(resolve(outDir, 'config.json'), JSON.stringify({ apiBaseUrl }));
console.log(`config.json → ${apiBaseUrl}`);
